using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Data;
using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Endpoints;

public static class AuthEndpoints
{
    internal static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);
    // Hash factice : égalise le temps de réponse quand l'e-mail n'existe pas
    private static readonly string DummyHash = PasswordService.Hash("dummy-password-for-timing");

    public static void MapAuth(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth").RequireRateLimiting("auth");
        g.MapPost("/register", Register);
        g.MapPost("/login", Login);
        g.MapGet("/me", Me).RequireAuthorization();
    }

    private static IResult Bad(string message) => Results.BadRequest(new { error = message });

    private const string InvalidInvite = "Code d'invitation invalide, expiré ou déjà utilisé.";

    /// <summary>Inscription sur invitation uniquement : le compte reçoit le rôle, le service et le poste prévus par l'invitation.</summary>
    private static async Task<IResult> Register(RegisterRequest req, AppDbContext db, TokenService tokens)
    {
        var name = (req.FullName ?? "").Trim();
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";
        var code = InviteCodes.Normalize(req.InviteCode);

        if (code.Length == 0) return Bad("Un code d'invitation est obligatoire pour créer un compte.");
        if (name.Length < 2 || name.Length > 80) return Bad("Le nom complet doit contenir entre 2 et 80 caractères.");
        if (email.Length > 254 || !EmailRx.IsMatch(email)) return Bad("Adresse e-mail invalide.");
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit))
            return Bad("Le mot de passe doit contenir au moins 8 caractères, une majuscule et un chiffre.");

        // Message volontairement identique pour un code inconnu, expiré, utilisé ou révoqué
        var invitation = await db.Invitations.AsNoTracking().FirstOrDefaultAsync(i => i.Code == code);
        if (invitation == null || invitation.StatusAt(DateTime.UtcNow) != "Active") return Bad(InvalidInvite);
        if (invitation.Email.Length > 0 && invitation.Email != email)
            return Bad("Cette invitation est réservée à une autre adresse e-mail.");

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Results.Conflict(new { error = "Un compte existe déjà avec cette adresse e-mail." });

        // Création du compte et consommation de l'invitation dans une seule transaction :
        // si le code vient d'être utilisé par quelqu'un d'autre, rien n'est enregistré.
        await using var tx = await db.Database.BeginTransactionAsync();

        var user = new User
        {
            FullName = name, Email = email, PasswordHash = PasswordService.Hash(password),
            Role = invitation.Role, Department = invitation.Department,
            JobTitle = invitation.JobTitle.Length > 0 ? invitation.JobTitle : RoleRules.DefaultTitle(invitation.Role)
        };
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "Un compte existe déjà avec cette adresse e-mail." });
        }

        var now = DateTime.UtcNow;
        var claimed = await db.Invitations
            .Where(i => i.Id == invitation.Id && i.UsedAt == null && i.RevokedAt == null && i.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedAt, now).SetProperty(i => i.UsedBy, name));
        if (claimed == 0) return Bad(InvalidInvite);

        Audit.Add(db, user, "Register", null, $"Invitation n°{invitation.Id}");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Results.Created("/api/auth/me", new AuthResponse(tokens.Create(user), user.ToDto()));
    }

    private static async Task<IResult> Login(LoginRequest req, AppDbContext db, TokenService tokens)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        var ok = PasswordService.Verify(password, user?.PasswordHash ?? DummyHash);
        if (user == null || !ok)
            return Results.Json(new { error = "E-mail ou mot de passe incorrect." }, statusCode: StatusCodes.Status401Unauthorized);

        Audit.Add(db, user, "Login");
        await db.SaveChangesAsync();
        return Results.Ok(new AuthResponse(tokens.Create(user), user.ToDto()));
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, AppDbContext db)
    {
        var user = await db.Users.FindAsync(principal.UserId());
        return user == null ? Results.Unauthorized() : Results.Ok(user.ToDto());
    }
}
