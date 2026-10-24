using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

public static class Mapping
{
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    public static UserDto ToDto(this User u) => new(u.Id, u.FullName, u.Email, u.Department, u.Role.ToString(), u.JobTitle);

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
