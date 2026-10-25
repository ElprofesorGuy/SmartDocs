using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> Versions => Set<DocumentVersion>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        b.Entity<Favorite>().HasKey(f => new { f.UserId, f.DocumentId });
        b.Entity<DocumentVersion>()
            .HasOne(v => v.Document)
            .WithMany(d => d.Versions)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Document>().HasIndex(d => d.IsArchived);
        b.Entity<Document>().HasIndex(d => d.IsDeleted);
    }
}
