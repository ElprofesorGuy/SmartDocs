using SmartDocs.Api.Models;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Data;

/// <summary>
/// Données de démonstration : un compte par poste type (mot de passe commun « Demo1234 ») et des documents
/// aux niveaux de confidentialité variés. Désactivable avec Seed:Enabled=false.
/// </summary>
public static class Seeder
{
    private const string DemoPassword = "Demo1234";
    private static double Mo(double mb) => mb * 1024;
    private static (string num, double kb, DateTime at, string author, string comment) V(string n, double kb, DateTime at, string a, string c)
        => (n, kb, at, a, c);

    public static void Seed(AppDbContext db, FileStorage files)
    {
        if (db.Users.Any() || db.Documents.Any()) return;
        var now = DateTime.UtcNow;

        // ----- Comptes : un par poste type -----
        User Acc(string name, string email, Role role, string dept, string title)
            => new() { FullName = name, Email = email, Role = role, Department = dept, JobTitle = title, PasswordHash = PasswordService.Hash(DemoPassword) };

        var thomas = Acc("Thomas Dubois", "thomas.dubois@entreprise.com", Role.Admin, "", "DSI – Responsable SI");
        var julien = Acc("Julien Moreau", "julien.moreau@entreprise.com", Role.Direction, "Direction", "Directeur Général");
        var sophie = Acc("Sophie Laurent", "sophie.laurent@entreprise.com", Role.Manager, "RH", "DRH");
        var marie = Acc("Marie Lefèvre", "marie.lefevre@entreprise.com", Role.Manager, "Finance", "DAF");
        var marc = Acc("Marc Lambert", "marc.lambert@entreprise.com", Role.Manager, "Juridique", "Responsable juridique");
        var claire = Acc("Claire Durand", "claire.durand@entreprise.com", Role.Collaborator, "Finance", "Contrôleuse de gestion");
        var alexandre = Acc("Alexandre Martin", "alexandre.martin@entreprise.com", Role.Collaborator, "RH", "Chargé RH");
        var nadia = Acc("Nadia Benali", "nadia.benali@entreprise.com", Role.Auditor, "", "Auditrice interne");
        var invite = Acc("Invité Prestataire", "invite@entreprise.com", Role.Guest, "", "Prestataire externe");
        db.Users.AddRange(thomas, julien, sophie, marie, marc, claire, alexandre, nadia, invite);
        db.SaveChanges(); // génère les identifiants utilisés comme propriétaires

        void Add(string name, string cat, string ext, string subtitle, User? owner, string ownerLabel, string conf,
                 (string num, double kb, DateTime at, string author, string comment)[] versions,
                 string path = "", string desc = "", string tags = "", DateTime? archivedAt = null)
        {
            var last = versions[^1];
            var doc = new Document
            {
                Name = name, Category = cat, CategoryPath = string.IsNullOrEmpty(path) ? cat : path,
                Extension = ext, Subtitle = subtitle, Description = string.IsNullOrEmpty(desc) ? subtitle : desc,
                OwnerId = owner?.Id, OwnerName = owner?.FullName ?? ownerLabel, OwnerRole = owner?.JobTitle ?? "",
                Confidentiality = conf, Tags = tags,
                ModifiedAt = last.at, ModifiedBy = last.author,
                IsArchived = archivedAt.HasValue, ArchivedAt = archivedAt
            };
            for (var i = 0; i < versions.Length; i++)
            {
                var v = versions[i];
                doc.Versions.Add(new DocumentVersion
                {
                    Number = v.num,
                    SizeBytes = (long)(v.kb * 1024),   // taille de démonstration affichée
                    StoredFileName = files.SaveBytes(PlaceholderFiles.Create(ext, name, v.num), ext),
                    CreatedAt = v.at, Author = v.author, Comment = v.comment,
                    IsCurrent = i == versions.Length - 1
                });
            }
            db.Documents.Add(doc);
        }

        DateTime Ago(double days) => now.AddDays(-days);

        // ----- Documents actifs -----
        Add("Rapport_Financier_Q3.pdf", "Finance", "pdf", "Clôture trimestrielle", claire, "", "Interne",
            new[] { V("1.0", Mo(2.4), now.AddHours(-2), claire.FullName, "Version initiale") });
        Add("Contrat_Cadre_Prestation.docx", "Juridique", "docx", "Contrat prestataire", marc, "", "Confidentiel",
            new[] { V("1.0", Mo(1.1), Ago(1), marc.FullName, "Version initiale") });
        Add("Grille_Salaires_2025.xlsx", "RH", "xlsx", "Grille de rémunération", sophie, "", "Confidentiel",
            new[] { V("1.0", 840, Ago(3), sophie.FullName, "Version initiale") });
        Add("Politique_Securite_SI.pdf", "Direction", "pdf", "Validé SSI", thomas, "", "Confidentiel",
            new[]
            {
                V("1.0", Mo(2.8), Ago(300), "Direction Générale", "Version inaugurale approuvée"),
                V("2.2", Mo(2.9), Ago(120), sophie.FullName, "Révision semestrielle des accès"),
                V("2.3", Mo(3.1), Ago(45), thomas.FullName, "Ajout des clauses télétravail"),
                V("2.4", Mo(3.2), Ago(6), thomas.FullName, "Mise à jour de la charte de télétravail et des accès distants")
            },
            path: "Direction / Sécurité", desc: "Système d'Information & Gouvernance", tags: "#Sécurité|#Gouvernance");
        Add("Note_de_Frais_Modele.xlsx", "Finance", "xlsx", "Gabarit Q4 • DAF", marie, "", "Interne",
            new[] { V("1.0", 450, Ago(9), marie.FullName, "Version initiale") });
        Add("Onboarding_Collaborateur.docx", "RH", "docx", "Livret d'accueil • Équipe People", sophie, "", "Interne",
            new[] { V("1.0", Mo(1.8), Ago(12), sophie.FullName, "Version initiale") });
        Add("Bilan_Carbone_2024.pdf", "Direction", "pdf", "Rapport RSE", julien, "", "Interne",
            new[] { V("1.0", Mo(5.6), Ago(15), julien.FullName, "Version initiale") });
        Add("Charte_Graphique_2024.pdf", "Direction", "pdf", "Communication", julien, "", "Public",
            new[] { V("1.0", Mo(6.1), Ago(20), julien.FullName, "Version initiale") });
        Add("Budget_Previsionnel_2025.xlsx", "Finance", "xlsx", "Version DAF", marie, "", "Confidentiel",
            new[] { V("1.0", Mo(1.3), Ago(25), marie.FullName, "Version initiale") });
        Add("Reglement_Interieur.docx", "RH", "docx", "Mise à jour légale", sophie, "", "Public",
            new[] { V("1.0", 720, Ago(32), sophie.FullName, "Version initiale") });
        Add("Politique_Teletravail.pdf", "RH", "pdf", "Accord d'entreprise", sophie, "", "Interne",
            new[] { V("1.0", Mo(1.5), Ago(45), sophie.FullName, "Version initiale") });
        Add("Modele_NDA.docx", "Juridique", "docx", "Accord de confidentialité", marc, "", "Interne",
            new[] { V("1.0", 310, Ago(52), marc.FullName, "Version initiale") });

        // ----- Documents archivés -----
        void Arch(string name, string cat, string ext, double kb, DateTime archivedAt)
            => Add(name, cat, ext, "Pièce archivée", null, "Archives " + cat, "Interne",
                   new[] { V("1.0", kb, archivedAt.AddMonths(-6), "Archives " + cat, "Version initiale") },
                   archivedAt: archivedAt);

        Arch("Bilan_Comptable_2022.pdf", "Finance", "pdf", Mo(4.8), new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        Arch("Contrat_Fournisseur_Telecom.docx", "Juridique", "docx", 890, new DateTime(2023, 11, 2, 0, 0, 0, DateTimeKind.Utc));
        Arch("Registre_Personnel_2022.xlsx", "RH", "xlsx", Mo(1.2), new DateTime(2023, 8, 14, 0, 0, 0, DateTimeKind.Utc));
        Arch("Audit_Interne_2022.pdf", "Direction", "pdf", Mo(3.9), new DateTime(2024, 2, 20, 0, 0, 0, DateTimeKind.Utc));
        Arch("Declaration_TVA_2023.xlsx", "Finance", "xlsx", 640, new DateTime(2024, 3, 8, 0, 0, 0, DateTimeKind.Utc));
        Arch("Avenant_Bail_2021.docx", "Juridique", "docx", 520, new DateTime(2022, 12, 5, 0, 0, 0, DateTimeKind.Utc));
        Arch("Rapport_Annuel_2021.pdf", "Direction", "pdf", Mo(7.4), new DateTime(2022, 6, 30, 0, 0, 0, DateTimeKind.Utc));
        Arch("Paie_Archive_2022.xlsx", "RH", "xlsx", Mo(2.2), new DateTime(2023, 3, 21, 0, 0, 0, DateTimeKind.Utc));

        db.SaveChanges();
    }
}
