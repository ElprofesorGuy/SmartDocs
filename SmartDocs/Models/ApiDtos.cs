namespace SmartDocs.Models;

// Formes JSON échangées avec l'API SmartDocs.Api (camelCase côté réseau)
public record UserDto(int Id, string FullName, string Email, string Department, string Role, string JobTitle);
public record AuthResponse(string Token, UserDto User);

public record DocumentDto(
    string Id, string Name, string Category, string CategoryPath, string Extension,
    string Subtitle, string Description, string Owner, string OwnerRole, string Confidentiality,
    List<string> Tags, long SizeBytes, DateTime ModifiedAt, string ModifiedBy,
    bool IsArchived, DateTime? ArchivedAt, bool IsFavorite, string CurrentVersion, int VersionsCount,
    bool CanArchive, bool CanAddVersion,
    bool IsDeleted, DateTime? DeletedAt, string? DeletedBy, bool CanDelete, bool CanPurge);

public record VersionDto(
    string Id, string DocumentId, string Number, long SizeBytes, DateTime CreatedAt,
    string Author, string Comment, bool IsCurrent, bool IsInitial);

public record ActivityDto(
    int Id, DateTime At, string UserName, string UserRole, string Action,
    string DocumentId, string DocumentName, string DocumentCategory, string Details);

public record InvitationDto(
    int Id, string Code, string Role, string Department, string JobTitle, string Email,
    DateTime CreatedAt, DateTime ExpiresAt, string CreatedBy, string Status, DateTime? UsedAt, string UsedBy, DateTime? EmailSentAt);

/// <summary>EmailStatus : Sent, Failed, NotConfigured ou NoAddress.</summary>
public record InvitationCreatedDto(InvitationDto Invitation, string EmailStatus, string? EmailError);

public record FavoriteResponse(bool IsFavorite);
