namespace SmartDocs.Api.Dtos;

public record RegisterRequest(string? FullName, string? Email, string? Password, string? InviteCode);
public record LoginRequest(string? Email, string? Password);
public record UserDto(int Id, string FullName, string Email, string Department, string Role, string JobTitle);
public record UpdateUserRequest(string? Role, string? Department, string? JobTitle);
public record AuthResponse(string Token, UserDto User);

public record CreateInvitationRequest(string? Role, string? Department, string? JobTitle, string? Email, int? ValidDays);
/// <summary>Status : Active, Used, Expired ou Revoked (calculé par le serveur).</summary>
public record InvitationDto(
    int Id, string Code, string Role, string Department, string JobTitle, string Email,
    DateTime CreatedAt, DateTime ExpiresAt, string CreatedBy, string Status, DateTime? UsedAt, string UsedBy, DateTime? EmailSentAt);

/// <summary>EmailStatus : Sent (envoyé), Failed (échec, voir EmailError), NotConfigured (SMTP non configuré) ou NoAddress (aucune adresse saisie).</summary>
public record CreateInvitationResponse(InvitationDto Invitation, string EmailStatus, string EmailError);

public record DocumentDto(
    string Id, string Name, string Category, string CategoryPath, string Extension,
    string Subtitle, string Description, string Owner, string OwnerRole, string Confidentiality,
    List<string> Tags, long SizeBytes, DateTime ModifiedAt, string ModifiedBy,
    bool IsArchived, DateTime? ArchivedAt, bool IsFavorite, string CurrentVersion, int VersionsCount,
    bool CanArchive, bool CanAddVersion,
    bool IsDeleted, DateTime? DeletedAt, string DeletedBy, bool CanDelete, bool CanPurge);

public record ActivityDto(
    int Id, DateTime At, string UserName, string UserRole, string Action,
    string DocumentId, string DocumentName, string DocumentCategory, string Details);

public record VersionDto(
    string Id, string DocumentId, string Number, long SizeBytes, DateTime CreatedAt,
    string Author, string Comment, bool IsCurrent, bool IsInitial);
