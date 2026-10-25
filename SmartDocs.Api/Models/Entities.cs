namespace SmartDocs.Api.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    /// <summary>Service (RH, Finance, Direction, Juridique) ou vide pour Admin, Auditeur et Invité.</summary>
    public string Department { get; set; } = "";
    public Role Role { get; set; } = Role.Guest;
    public string JobTitle { get; set; } = "Invité";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryPath { get; set; } = "";
    public string Extension { get; set; } = "pdf";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";
    public int? OwnerId { get; set; }
    public string OwnerName { get; set; } = ""; 
    public string OwnerRole { get; set; } = "";
    public string Confidentiality { get; set; } = "Interne";
    /// <summary>Tags séparés par « | ».</summary>
    public string Tags { get; set; } = "";
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = "";
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    /// <summary>Suppression logique : le document est dans la corbeille (fichiers conservés) jusqu'à restauration ou suppression définitive.</summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = "";
    public List<DocumentVersion> Versions { get; set; } = new();
}

public class DocumentVersion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DocumentId { get; set; } = "";
    public Document? Document { get; set; }
    public string Number { get; set; } = "1.0";
    public long SizeBytes { get; set; }
    public string StoredFileName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Author { get; set; } = "";
    public string Comment { get; set; } = "";
    public bool IsCurrent { get; set; }
}

public class Favorite
{
    public int UserId { get; set; }
    public string DocumentId { get; set; } = "";
}

/// <summary>
/// Entrée du journal d'activité. Les noms (utilisateur, document) sont copiés au moment de l'action :
/// l'entrée reste lisible même si le document est supprimé définitivement. Aucune clé étrangère, volontairement.
/// </summary>
public class ActivityLog
{
    public int Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public string UserName { get; set; } = "";
    public string UserRole { get; set; } = "";
    /// <summary>Deposit, Download, AddVersion, RestoreVersion, Archive, Unarchive, Trash, Undelete, Purge, Login, Register, RoleChange, InviteCreate, InviteRevoke, InviteEmail.</summary>
    public string Action { get; set; } = "";
    public string DocumentId { get; set; } = "";
    public string DocumentName { get; set; } = "";
    public string DocumentCategory { get; set; } = "";
    /// <summary>Niveau du document au moment de l'action : sert à masquer l'entrée à l'administrateur si le document est confidentiel.</summary>
    public string Confidentiality { get; set; } = "";
    public string Details { get; set; } = "";
}

/// <summary>
/// Invitation à créer un compte (l'inscription libre n'existe pas). Le code est à usage unique, expire,
/// et attribue directement le rôle, le service et le poste au futur compte. Créée et révoquée par un administrateur.
/// </summary>
public class Invitation
{
    public int Id { get; set; }
    /// <summary>Code normalisé (majuscules, sans tiret). Unique.</summary>
    public string Code { get; set; } = "";
    public Role Role { get; set; } = Role.Guest;
    public string Department { get; set; } = "";
    public string JobTitle { get; set; } = "";
    /// <summary>Adresse e-mail à laquelle l'invitation est réservée (en minuscules). Vide = n'importe quelle adresse.</summary>
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? UsedAt { get; set; }
    public string UsedBy { get; set; } = "";
    public DateTime? RevokedAt { get; set; }
    /// <summary>Date du dernier envoi du code par e-mail (null = jamais envoyé par l'application).</summary>
    public DateTime? EmailSentAt { get; set; }
}
