using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

public static class Mapping
{
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    public static UserDto ToDto(this User u) => new(u.Id, u.FullName, u.Email, u.Department, u.Role.ToString(), u.JobTitle);

    public static ActivityDto ToDto(this ActivityLog l)
        => new(l.Id, Utc(l.At), l.UserName, l.UserRole, l.Action, l.DocumentId, l.DocumentName, l.DocumentCategory, l.Details);

    public static string StatusAt(this Invitation i, DateTime now)
        => i.UsedAt != null ? "Used" : i.RevokedAt != null ? "Revoked" : i.ExpiresAt <= now ? "Expired" : "Active";

    public static InvitationDto ToDto(this Invitation i, DateTime now)
        => new(i.Id, InviteCodes.Format(i.Code), i.Role.ToString(), i.Department, i.JobTitle, i.Email,
               Utc(i.CreatedAt), Utc(i.ExpiresAt), i.CreatedBy, i.StatusAt(now),
               i.UsedAt.HasValue ? Utc(i.UsedAt.Value) : null, i.UsedBy,
               i.EmailSentAt.HasValue ? Utc(i.EmailSentAt.Value) : null);

    public static VersionDto ToDto(this DocumentVersion v)
        => new(v.Id, v.DocumentId, v.Number, v.SizeBytes, Utc(v.CreatedAt), v.Author, v.Comment, v.IsCurrent, v.Number == "1.0");

    public static DocumentVersion CurrentVersion(this Document d)
        => d.Versions.FirstOrDefault(v => v.IsCurrent) ?? d.Versions.OrderByDescending(v => v.CreatedAt).First();

    public static DocumentDto ToDto(this Document d, ISet<string> favorites, User viewer)
    {
        var current = d.CurrentVersion();
        return new DocumentDto(
            d.Id, d.Name, d.Category, d.CategoryPath, d.Extension, d.Subtitle, d.Description,
            d.OwnerName, d.OwnerRole, d.Confidentiality,
            d.Tags.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            current.SizeBytes, Utc(d.ModifiedAt), d.ModifiedBy, d.IsArchived,
            d.ArchivedAt.HasValue ? Utc(d.ArchivedAt.Value) : null,
            favorites.Contains(d.Id), current.Number, d.Versions.Count,
            Access.CanArchive(viewer, d), Access.CanAddVersion(viewer, d),
            d.IsDeleted, d.DeletedAt.HasValue ? Utc(d.DeletedAt.Value) : null, d.DeletedBy,
            Access.CanDelete(viewer, d), Access.CanPurge(viewer, d));
    }
}
