namespace SmartDocs.Api.Dtos;

public record RegisterRequest(string? FullName, string? Email, string? Password);
public record LoginRequest(string? Email, string? Password);
public record UserDto(int Id, string FullName, string Email, string Department, string Role, string JobTitle);
public record UpdateUserRequest(string? Role, string? Department, string? JobTitle);
public record AuthResponse(string Token, UserDto User);

public record DocumentDto(
    string Id, string Name, string Category, string CategoryPath, string Extension,
    string Subtitle, string Description, string Owner, string OwnerRole, string Confidentiality,
    List<string> Tags, long SizeBytes, DateTime ModifiedAt, string ModifiedBy,
    bool IsArchived, DateTime? ArchivedAt, bool IsFavorite, string CurrentVersion, int VersionsCount,
    bool CanArchive, bool CanAddVersion,
    bool IsDeleted, DateTime? DeletedAt, string DeletedBy, bool CanDelete, bool CanPurge);

public record VersionDto(
    string Id, string DocumentId, string Number, long SizeBytes, DateTime CreatedAt,
    string Author, string Comment, bool IsCurrent, bool IsInitial);
