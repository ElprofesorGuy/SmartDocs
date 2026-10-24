using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Data;
using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Endpoints;

/// <summary>Gestion des comptes : réservée au rôle Administrateur.</summary>
public static class AdminEndpoints
{
    private static readonly string[] Departments = { "RH", "Finance", "Direction", "Juridique" };

    public static void MapAdmin(this WebApplication app)
    {
        var g = app.MapGroup("/api/admin").RequireAuthorization();
        g.MapGet("/users", ListUsers);
        g.MapPut("/users/{id:int}", UpdateUser);
    }

    private static IResult Forbidden() => Results.Json(new { error = "Action réservée aux administrateurs." }, statusCode: StatusCodes.Status403Forbidden);

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

        if (!Enum.TryParse<Role>(req.Role, ignoreCase: false, out var role) || !Enum.IsDefined(role))
            return Results.BadRequest(new { error = "Rôle invalide." });

        // Le service dépend du rôle
        var department = (req.Department ?? "").Trim();
        switch (role)
        {
            case Role.Manager or Role.Collaborator:
                if (!Departments.Contains(department))
                    return Results.BadRequest(new { error = "Un service valide est obligatoire pour ce rôle." });
                break;
            case Role.Direction:
                department = "Direction";
                break;
            default:
                department = "";
                break;
        }

        var title = (req.JobTitle ?? "").Trim();
        if (title.Length > 60) return Results.BadRequest(new { error = "L'intitulé du poste est limité à 60 caractères." });
        if (title.Length == 0) title = DefaultTitle(role);

        // Garde-fou : ne pas retirer le dernier administrateur
        if (user.Role == Role.Admin && role != Role.Admin && await db.Users.CountAsync(u => u.Role == Role.Admin) <= 1)
            return Results.BadRequest(new { error = "Impossible de retirer le dernier administrateur." });

        user.Role = role;
        user.Department = department;
        user.JobTitle = title;
        await db.SaveChangesAsync();
        return Results.Ok(user.ToDto());
    }

    private static string DefaultTitle(Role role) => role switch
    {
        Role.Admin => "Administrateur SI",
        Role.Direction => "Direction",
        Role.Manager => "Responsable de service",
        Role.Collaborator => "Collaborateur",
        Role.Auditor => "Auditeur interne",
        _ => "Invité"
    };
}
