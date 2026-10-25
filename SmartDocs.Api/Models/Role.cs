namespace SmartDocs.Api.Models;

/// <summary>Postes types en entreprise. Le rôle est relu en base à chaque requête : un changement de rôle est immédiat.</summary>
public enum Role
{
    Guest,        // Invité : prestataire, stagiaire
    Collaborator, // Collaborateur d'un service
    Manager,      // Responsable de service (DRH, DAF, responsable juridique…)
    Auditor,      // Auditeur : lecture seule, tous services
    Direction,    // Direction : lecture de tous les documents
    Admin         // Administrateur SI : gère les comptes, ne lit pas les confidentiels d'autrui
}
