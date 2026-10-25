using SmartDocs.Models;

namespace SmartDocs.Services;

/// <summary>Journal d'activité (lecture seule). Les droits sont contrôlés par le serveur : Direction, Auditeur, Administrateur.</summary>
public class ActivityService(ApiClient api)
{
    public const int PageSize = 50;

    /// <param name="actions">Actions séparées par des virgules (null = toutes).</param>
    /// <param name="before">Identifiant de la dernière entrée déjà reçue (page suivante).</param>
    public async Task<List<ActivityItem>> GetAsync(string? actions = null, string? search = null, int? before = null)
    {
        var query = new List<string> { $"take={PageSize}" };
        if (!string.IsNullOrWhiteSpace(actions)) query.Add("action=" + Uri.EscapeDataString(actions));
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        if (before.HasValue) query.Add($"before={before.Value}");

        var list = await api.GetAsync<List<ActivityDto>>("/api/activity?" + string.Join("&", query));
        return list.Select(a => new ActivityItem
        {
            Id = a.Id, At = a.At.ToLocalTime(), UserName = a.UserName, UserRole = a.UserRole,
            Action = a.Action, DocumentName = a.DocumentName, Details = a.Details
        }).ToList();
    }
}
