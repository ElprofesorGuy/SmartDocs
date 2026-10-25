using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Data;
using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Endpoints;

/// <summary>Gestion des comptes et des invitations : réservée au rôle Administrateur.</summary>
public static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app)
    {
        var g = app.MapGroup("/api/admin").RequireAuthorization();
        g.MapGet("/users", ListUsers);
        g.MapPut("/users/{id:int}", UpdateUser);
        g.MapGet("/invitations", ListInvitations);
        g.MapPost("/invitations", CreateInvitation);
        g.MapPost("/invitations/{id:int}/email", SendInvitationEmail);
        g.MapDelete("/invitations/{id:int}", RevokeInvitation);
    }

    private static IResult Forbidden() => Results.Json(new { error = "Action réservée aux administrateurs." }, statusCode: StatusCodes.Status403Forbidden);
    private static IResult Bad(string message) => Results.BadRequest(new { error = message });

    private static async Task<IResult> ListUsers(ClaimsPrincipal principal, AppDbContext db)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.IsAdmin(me)) return Forbidden();

        var users = await db.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        return Results.Ok(users.Select(u => u.ToDto()).ToList());
    }

    private static async Task<IResult> UpdateUser(int id, UpdateUserRequest req, ClaimsPrincipal principal, AppDbContext db)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.IsAdmin(me)) return Forbidden();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return Results.NotFound(new { error = "Utilisateur introuvable." });

        // Le service dépend du rôle
        if (!RoleRules.TryResolve(req.Role, req.Department, out var role, out var department, out var error))
            return Bad(error);

        var title = (req.JobTitle ?? "").Trim();
        if (title.Length > 60) return Bad("L'intitulé du poste est limité à 60 caractères.");
        if (title.Length == 0) title = RoleRules.DefaultTitle(role);

        // Garde-fou : ne pas retirer le dernier administrateur
        if (user.Role == Role.Admin && role != Role.Admin && await db.Users.CountAsync(u => u.Role == Role.Admin) <= 1)
            return Bad("Impossible de retirer le dernier administrateur.");

        Audit.Add(db, me, "RoleChange", null,
            $"{user.FullName} : {RoleRules.Show(user.Role, user.Department)} → {RoleRules.Show(role, department)}");

        user.Role = role;
        user.Department = department;
        user.JobTitle = title;
        await db.SaveChangesAsync();
        return Results.Ok(user.ToDto());
    }

    // ------------------------------------------------------------------ Invitations

    private static async Task<IResult> ListInvitations(ClaimsPrincipal principal, AppDbContext db)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.IsAdmin(me)) return Forbidden();

        var now = DateTime.UtcNow;
        var list = await db.Invitations.AsNoTracking().OrderByDescending(i => i.Id).Take(200).ToListAsync();
        return Results.Ok(list.Select(i => i.ToDto(now)).ToList());
    }

    private static async Task<IResult> CreateInvitation(CreateInvitationRequest req, ClaimsPrincipal principal, AppDbContext db, EmailService mail)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.IsAdmin(me)) return Forbidden();

        if (!RoleRules.TryResolve(req.Role, req.Department, out var role, out var department, out var error))
            return Bad(error);

        var title = (req.JobTitle ?? "").Trim();
        if (title.Length > 60) return Bad("L'intitulé du poste est limité à 60 caractères.");
        if (title.Length == 0) title = RoleRules.DefaultTitle(role);

        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length > 0)
        {
            if (email.Length > 254 || !AuthEndpoints.EmailRx.IsMatch(email)) return Bad("Adresse e-mail invalide.");
            if (await db.Users.AnyAsync(u => u.Email == email)) return Bad("Un compte existe déjà avec cette adresse e-mail.");
        }

        var days = req.ValidDays ?? 7;
        if (days is < 1 or > 30) return Bad("La durée de validité doit être comprise entre 1 et 30 jours.");

        string code;
        do { code = InviteCodes.Generate(); }
        while (await db.Invitations.AnyAsync(i => i.Code == code));

        var now = DateTime.UtcNow;
        var invitation = new Invitation
        {
            Code = code, Role = role, Department = department, JobTitle = title, Email = email,
            CreatedAt = now, ExpiresAt = now.AddDays(days), CreatedBy = me.FullName
        };
        db.Invitations.Add(invitation);
        // Le code n'est volontairement pas recopié dans le journal
        Audit.Add(db, me, "InviteCreate", null,
            $"{RoleRules.Show(role, department)}{(email.Length > 0 ? " pour " + email : "")}, valable {days} jour(s)");
        await db.SaveChangesAsync();

        // Envoi automatique par e-mail si une adresse est saisie et si le serveur SMTP est configuré
        var emailStatus = "NoAddress";
        var emailError = "";
        if (email.Length > 0)
        {
            if (!mail.IsConfigured) emailStatus = "NotConfigured";
            else
            {
                var failure = await mail.SendInvitationAsync(invitation, me.FullName);
                if (failure == null)
                {
                    invitation.EmailSentAt = DateTime.UtcNow;
                    Audit.Add(db, me, "InviteEmail", null, $"Invitation n°{invitation.Id} envoyée à {email}");
                    await db.SaveChangesAsync();
                    emailStatus = "Sent";
                }
                else
                {
                    emailStatus = "Failed";
                    emailError = failure;
                }
            }
        }

        return Results.Created($"/api/admin/invitations/{invitation.Id}",
            new CreateInvitationResponse(invitation.ToDto(now), emailStatus, emailError));
    }

    /// <summary>Envoie (ou renvoie) le code par e-mail à l'adresse de l'invitation, tant qu'elle est active.</summary>
    private static async Task<IResult> SendInvitationEmail(int id, ClaimsPrincipal principal, AppDbContext db, EmailService mail)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.IsAdmin(me)) return Forbidden();

        var invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Id == id);
        if (invitation == null) return Results.NotFound(new { error = "Invitation introuvable." });

        var now = DateTime.UtcNow;
        if (invitation.StatusAt(now) != "Active") return Bad("Seule une invitation active peut être envoyée.");
        if (invitation.Email.Length == 0) return Bad("Cette invitation n'est associée à aucune adresse e-mail.");
        if (!mail.IsConfigured)
            return Bad("L'envoi d'e-mails n'est pas configuré sur le serveur (section Smtp de appsettings.json).");

        var failure = await mail.SendInvitationAsync(invitation, me.FullName);
        if (failure != null)
            return Results.Json(new { error = failure }, statusCode: StatusCodes.Status502BadGateway);

        invitation.EmailSentAt = DateTime.UtcNow;
        Audit.Add(db, me, "InviteEmail", null, $"Invitation n°{invitation.Id} envoyée à {invitation.Email}");
        await db.SaveChangesAsync();
        return Results.Ok(invitation.ToDto(now));
    }

    private static async Task<IResult> RevokeInvitation(int id, ClaimsPrincipal principal, AppDbContext db)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.IsAdmin(me)) return Forbidden();

        var invitation = await db.Invitations.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (invitation == null) return Results.NotFound(new { error = "Invitation introuvable." });
        if (invitation.UsedAt != null) return Bad("Cette invitation a déjà été utilisée.");
        if (invitation.RevokedAt != null) return Results.NoContent(); // déjà révoquée : sans effet

        // Atomique : une inscription qui utiliserait le code au même instant l'emporte ou échoue, jamais les deux
        var now = DateTime.UtcNow;
        var done = await db.Invitations
            .Where(i => i.Id == id && i.UsedAt == null && i.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.RevokedAt, now));
        if (done == 0) return Bad("Cette invitation vient d'être utilisée.");

        Audit.Add(db, me, "InviteRevoke", null,
            $"Invitation n°{id} ({RoleRules.Show(invitation.Role, invitation.Department)}" +
            $"{(invitation.Email.Length > 0 ? ", " + invitation.Email : "")})");
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
