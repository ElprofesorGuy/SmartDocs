using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Data;
using SmartDocs.Api.Dtos;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Endpoints;

/// <summary>Journal d'activité en lecture seule : Direction, Auditeur et Administrateur.</summary>
public static class ActivityEndpoints
{
    public static void MapActivity(this WebApplication app)
        => app.MapGroup("/api/activity").RequireAuthorization().MapGet("/", List);

    /// <param name="action">Une ou plusieurs actions séparées par des virgules (ex. Archive,Unarchive).</param>
    /// <param name="before">Pagination : entrées d'identifiant inférieur (le client passe le dernier Id reçu).</param>
    private static async Task<IResult> List(ClaimsPrincipal principal, AppDbContext db,
        string? action, string? search, int? before, int? take)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());
        if (me == null) return Results.Unauthorized();
        if (!Access.CanSeeActivity(me))
            return Results.Json(new { error = "Le journal d'activité est réservé à la Direction, à l'Auditeur et à l'Administrateur." },
                statusCode: StatusCodes.Status403Forbidden);

        var q = db.ActivityLogs.AsNoTracking().Where(Access.ActivityVisible(me));

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actions = action.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            q = q.Where(l => actions.Contains(l.Action));
        }
        if (before.HasValue) q = q.Where(l => l.Id < before.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim().Replace("%", "").Replace("_", "")}%";
            q = q.Where(l => EF.Functions.Like(l.UserName, like) || EF.Functions.Like(l.DocumentName, like) ||
                             EF.Functions.Like(l.Details, like));
        }

        var size = Math.Clamp(take ?? 50, 1, 100);
        var items = await q.OrderByDescending(l => l.Id).Take(size).ToListAsync();
        return Results.Ok(items.Select(l => l.ToDto()).ToList());
    }
}
