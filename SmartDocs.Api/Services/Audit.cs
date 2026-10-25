using SmartDocs.Api.Data;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

/// <summary>Écriture dans le journal d'activité. Add n'enregistre pas : l'entrée part avec le SaveChanges de l'action (même transaction).</summary>
public static class Audit
{
    public static void Add(AppDbContext db, User? user, string action, Document? doc = null, string details = "")
        => db.ActivityLogs.Add(new ActivityLog
        {
            UserId = user?.Id,
            UserName = user?.FullName ?? "",
            UserRole = user?.Role.ToString() ?? "",
            Action = action,
            DocumentId = doc?.Id ?? "",
            DocumentName = doc?.Name ?? "",
            DocumentCategory = doc?.Category ?? "",
            Confidentiality = doc?.Confidentiality ?? "",
            Details = details.Length > 300 ? details[..300] : details
        });
}
