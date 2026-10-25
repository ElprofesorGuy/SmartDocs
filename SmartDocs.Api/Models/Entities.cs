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
