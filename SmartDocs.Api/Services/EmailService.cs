using System.Net;
using System.Net.Mail;
using System.Text;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

/// <summary>
/// Envoi des e-mails d'invitation via un serveur SMTP (section « Smtp » de appsettings.json).
/// Facultatif : si Smtp:Enabled est faux ou incomplet, IsConfigured vaut faux et l'administrateur transmet le code lui-même.
/// Le mot de passe SMTP ne doit pas être écrit dans le fichier en production : variable d'environnement Smtp__Password.
/// </summary>
public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly bool _ssl;
    private readonly string _user;
    private readonly string _password;
    private readonly string _from;
    private readonly string _fromName;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _logger = logger;
        var s = config.GetSection("Smtp");
        _host = (s["Host"] ?? "").Trim();
        _port = s.GetValue<int?>("Port") ?? 587;
        _ssl = s.GetValue<bool?>("EnableSsl") ?? true;
        _user = (s["User"] ?? "").Trim();
        _password = s["Password"] ?? "";
        _from = (s["FromAddress"] ?? "").Trim();
        _fromName = (s["FromName"] ?? "SmartDocs").Trim();
        IsConfigured = s.GetValue<bool>("Enabled") && _host.Length > 0 && _from.Length > 0;
    }

    public bool IsConfigured { get; }
    public string Host => _host;

    /// <summary>Envoie le code à l'adresse de l'invitation. Retourne null si l'envoi a réussi, sinon un message d'erreur lisible (les détails techniques restent dans les journaux du serveur).</summary>
    public async Task<string?> SendInvitationAsync(Invitation invitation, string inviterName)
    {
        if (!IsConfigured) return "L'envoi d'e-mails n'est pas configuré sur le serveur (section Smtp de appsettings.json).";
        if (invitation.Email.Length == 0) return "Cette invitation n'est associée à aucune adresse e-mail.";

        try
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _ssl,
                Timeout = 15_000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            if (_user.Length > 0) client.Credentials = new NetworkCredential(_user, _password);

            using var message = new MailMessage
            {
                From = new MailAddress(_from, _fromName, Encoding.UTF8),
                Subject = "Invitation à rejoindre SmartDocs",
                SubjectEncoding = Encoding.UTF8,
                Body = BuildBody(invitation, inviterName),
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(invitation.Email));

            await client.SendMailAsync(message);
            return null;
        }
        catch (Exception ex)
        {
            // Aucun secret n'est journalisé : seulement le type d'erreur et le message du serveur SMTP
            _logger.LogWarning("Échec d'envoi de l'invitation n°{Id} via {Host}:{Port} — {Type} : {Message}",
                invitation.Id, _host, _port, ex.GetType().Name, ex.GetBaseException().Message);
            return "L'e-mail n'a pas pu être envoyé. Vérifiez la configuration SMTP du serveur (voir ses journaux).";
        }
    }

    private static string BuildBody(Invitation i, string inviterName)
        => $"Bonjour,\n\n" +
           $"{inviterName} vous invite à rejoindre SmartDocs, la gestion documentaire de l'entreprise.\n\n" +
           $"Votre code d'invitation : {InviteCodes.Format(i.Code)}\n" +
           $"Votre rôle : {RoleRules.Label(i.Role)}{(i.Department.Length > 0 ? " (" + i.Department + ")" : "")}\n" +
           $"Valable jusqu'au {i.ExpiresAt:dd/MM/yyyy} à {i.ExpiresAt:HH:mm} (UTC).\n\n" +
           "Pour créer votre compte : ouvrez l'application SmartDocs, touchez « S'inscrire » sur l'écran de connexion " +
           $"et saisissez ce code. Utilisez cette adresse e-mail pour vous inscrire : {i.Email}\n\n" +
           "Ce code est personnel et ne peut servir qu'une seule fois. " +
           "Si vous n'attendiez pas cette invitation, ignorez simplement ce message.\n";
}
