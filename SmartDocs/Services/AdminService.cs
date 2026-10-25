using SmartDocs.Models;

namespace SmartDocs.Services;

/// <summary>Gestion des comptes et des invitations (rôle Administrateur uniquement, contrôlé par le serveur).</summary>
public class AdminService(ApiClient api)
{
    public async Task<List<UserItem>> GetUsersAsync()
    {
        var users = await api.GetAsync<List<UserDto>>("/api/admin/users");
        return users.Select(u => new UserItem
        {
            Id = u.Id, FullName = u.FullName, Email = u.Email,
            Role = u.Role, Department = u.Department, JobTitle = u.JobTitle
        }).ToList();
    }

    public async Task UpdateUserAsync(int id, string role, string department, string jobTitle)
        => await api.PutAsync<UserDto>($"/api/admin/users/{id}", new { role, department, jobTitle });

    public async Task<List<InvitationItem>> GetInvitationsAsync()
        => (await api.GetAsync<List<InvitationDto>>("/api/admin/invitations")).Select(InvitationItem.From).ToList();

    /// <summary>Résultat d'une création : l'invitation et le sort de l'e-mail (Sent, Failed, NotConfigured, NoAddress).</summary>
    public record InvitationCreated(InvitationItem Item, string EmailStatus, string EmailError);

    public async Task<InvitationCreated> CreateInvitationAsync(string role, string department, string jobTitle, string email, int validDays)
    {
        var r = await api.PostAsync<InvitationCreatedDto>("/api/admin/invitations",
            new { role, department, jobTitle, email, validDays });
        return new InvitationCreated(InvitationItem.From(r.Invitation), r.EmailStatus, r.EmailError ?? "");
    }

    /// <summary>Envoie ou renvoie le code par e-mail (le serveur doit avoir un SMTP configuré).</summary>
    public async Task SendInvitationEmailAsync(int id) => await api.PostAsync<InvitationDto>($"/api/admin/invitations/{id}/email");

    public async Task RevokeInvitationAsync(int id) => await api.DeleteAsync($"/api/admin/invitations/{id}");
}
