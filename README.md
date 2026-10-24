# SmartDocs — gestion documentaire d'entreprise

Application mobile **.NET MAUI** (Android + Windows) et **API ASP.NET Core + SQLite**.

> ⚠️ Code écrit sans pouvoir être compilé par son auteur : il a été vérifié statiquement uniquement. Les premières compilations peuvent révéler de petites corrections.

## Structure
```
SmartDocs.sln
SmartDocs/        application MAUI (Views, Models, Services/ApiClient, Controls, Resources, Platforms)
SmartDocs.Api/    API REST (Program.cs, Endpoints, Data, Services, Models, Dtos, SmartDocs.Api.http)
```

## 1. Démarrer l'API (toujours en premier)
Prérequis : SDK .NET 10.
```
cd SmartDocs.Api
dotnet run
```
- L'API écoute sur **http://0.0.0.0:5080**. Vérification : ouvrir http://localhost:5080/health → `{"status":"ok"}`.
- Au premier lancement : création du fichier SQLite `smartdocs.db`, du dossier `Storage/` (fichiers déposés) et des données de démonstration.
- **Comptes de démonstration** : un par poste, mot de passe commun `Demo1234` (voir « Rôles » ci-dessous). À désactiver : `"Seed": { "Enabled": false }` dans `appsettings.json`. Un compte créé depuis l'écran d'inscription est **Invité** tant qu'un administrateur ne lui attribue pas un rôle.
- Réinitialiser les données : arrêter l'API, supprimer `smartdocs.db` et `Storage/`, relancer. **À faire obligatoirement après une mise à jour qui change le modèle** (l'API le signale au démarrage).
- Tests sans l'application : fichier `SmartDocs.Api/SmartDocs.Api.http` (Visual Studio).
- Au premier démarrage, Windows peut demander d'autoriser le pare-feu : accepter (réseau privé) si le test se fait depuis un téléphone.

## 2. Lancer l'application
Ouvrir `SmartDocs.sln`, cible **Windows Machine** ou émulateur Android, F5. L'écran de connexion affiche l'état du serveur (« Portail Actif » ou « Hors ligne »).

| Cible | Adresse de l'API utilisée par défaut |
|---|---|
| Windows Machine | `http://localhost:5080` |
| Émulateur Android | `http://10.0.2.2:5080` (le PC hôte) |
| Téléphone Android réel | à saisir : toucher la pastille d'état sur l'écran de connexion, puis `http://<IP du PC>:5080` (même Wi-Fi) |

IP du PC : `ipconfig` (adresse IPv4).

## Rôles et droits
Chaque compte a un **rôle** (poste type) et, selon le rôle, un **service** (RH, Finance, Direction, Juridique). Les droits sont contrôlés **par le serveur** à chaque requête ; l'application masque seulement les boutons non autorisés.

| Rôle | Voir | Déposer / ajouter une version | Archiver / restaurer | Autres |
|---|---|---|---|---|
| **Administrateur** (DSI) | Public + Interne, ses propres confidentiels | — | — | Gère les comptes et les rôles. Ne lit pas les confidentiels des autres |
| **Direction** (DG) | Tout | — | Tous les documents | |
| **Responsable de service** (DRH, DAF, juridique) | Public + Interne + confidentiels de **son** service | Dans son service | Documents de son service | |
| **Collaborateur** | Public + Interne + confidentiels de son service | Dans son service | Ses propres documents | |
| **Auditeur** | Tout | — | — | Lecture seule |
| **Invité** (prestataire) | Documents **Public** uniquement | — | — | Rôle par défaut des nouveaux comptes |

Confidentialité : **Public** = tous ; **Interne** = tous sauf Invité ; **Confidentiel** = Direction, Auditeur, membres du service concerné, propriétaire. Un document non autorisé n'apparaît pas dans les listes et son téléchargement est refusé.

### Corbeille
- **Mettre à la corbeille / restaurer** : mêmes droits que l'archivage (Direction : tout ; Responsable : son service ; Collaborateur : ses documents). Un document supprimé disparaît des listes et son fichier reste conservé ; une fois restauré, il retrouve son état d'avant (actif ou archivé).
- **Supprimer définitivement** (depuis la corbeille uniquement) : **Direction** (tout) et **Administrateur** (documents qu'il peut voir). Supprime le document, toutes ses versions et leurs fichiers.
- Accès : menu ☰ de l'accueil > « Corbeille » (Direction, Responsable, Collaborateur, Administrateur). La corbeille ne montre que les documents sur lesquels le rôle peut agir.

### Comptes de démonstration (mot de passe `Demo1234`)
| E-mail | Poste | Rôle |
|---|---|---|
| thomas.dubois@entreprise.com | DSI | Administrateur |
| julien.moreau@entreprise.com | Directeur Général | Direction |
| sophie.laurent@entreprise.com | DRH | Responsable (RH) |
| marie.lefevre@entreprise.com | DAF | Responsable (Finance) |
| marc.lambert@entreprise.com | Responsable juridique | Responsable (Juridique) |
| claire.durand@entreprise.com | Contrôleuse de gestion | Collaboratrice (Finance) |
| alexandre.martin@entreprise.com | Chargé RH | Collaborateur (RH) |
| nadia.benali@entreprise.com | Auditrice interne | Auditeur |
| invite@entreprise.com | Prestataire | Invité |

Scénario de démonstration : `Grille_Salaires_2025.xlsx` (RH, Confidentiel) est visible par Sophie, Alexandre, Julien et Nadia, mais pas par Claire, Marie, Thomas ni l'invité. Thomas ouvre le menu ☰ > « Gérer les utilisateurs » pour changer un rôle.

## API
| Méthode et route | Rôle |
|---|---|
| `POST /api/auth/register`, `/login` — `GET /api/auth/me` | Inscription, connexion (JWT), profil |
| `GET /api/documents` (`search`, `category`, `extension`, `archived`, `sort`, `year`) | Liste, recherche, filtres, tri |
| `GET /api/documents/{id}` | Détail (404 si le rôle ne permet pas de le voir) |
| `GET /api/admin/users` — `PUT /api/admin/users/{id}` | Liste des comptes, changement de rôle / service / poste (Administrateur) |
| `POST /api/documents` (multipart : `file`, `name`, `category`, `confidentiality`, `tags`) | Dépôt (PDF/DOCX/XLSX, 10 Mo max) |
| `GET /api/documents/{id}/file` | Téléchargement de la version courante |
| `POST /api/documents/{id}/archive` / `unarchive` | Archivage, restauration depuis les archives |
| `DELETE /api/documents/{id}` — `POST .../undelete` — `DELETE .../purge` — `GET /api/documents?trash=true` | Mise à la corbeille (suppression logique), restauration, suppression définitive, liste de la corbeille |
| `POST /api/documents/{id}/favorite` | Favori (propre à chaque utilisateur) |
| `GET /api/documents/{id}/versions` — `POST` (multipart `file`, `comment`) | Historique, nouvelle version (bouton « Ajouter une nouvelle version » de l'écran Historique) |
| `GET .../versions/{versionId}/file` — `POST .../versions/{versionId}/restore` | Téléchargement, restauration (crée une nouvelle version active) |

## Sécurité mise en place
Rôles et visibilité appliqués côté serveur (le rôle est relu en base à chaque requête) ; mots de passe hachés (PBKDF2-SHA256, sel aléatoire) ; jeton JWT signé, durée 8 h ; toutes les routes documents exigent un jeton ; limitation à 20 requêtes/minute/IP sur la connexion et l'inscription ; contrôle d'extension **et** de signature des fichiers, taille 10 Mo ; noms de fichiers générés par le serveur (pas de chemin fourni par l'utilisateur) ; erreurs internes sans détails techniques ; jeton conservé dans le stockage sécurisé de l'appareil.

## Limites connues (à mentionner dans le rapport)
- **Développement local** : HTTP en clair (autorisé dans le manifeste Android), clé JWT de démonstration dans `appsettings.json`. En production : HTTPS, clé secrète en variable d'environnement (`Jwt__Key`), base serveur, sauvegardes.
- Pas encore : modification des métadonnées, journal d'activité, vidage automatique de la corbeille (la suppression définitive est manuelle).
- Recherche, filtres et tri de l'écran s'appliquent sur la liste déjà chargée depuis l'API ; l'API sait aussi filtrer côté serveur (paramètres ci-dessus).
- Les tailles affichées pour les documents de démonstration sont fictives ; leurs fichiers sont de petits PDF/DOCX/XLSX générés.
- Non implémentés : mot de passe oublié, SSO Microsoft 365 / SAML, recherche vocale, aperçu intégré (le bouton Consulter ouvre le fichier avec une application de l'appareil), notifications.
- Pas de migrations de base (`EnsureCreated`) : un changement de modèle impose de supprimer `smartdocs.db`.

## Dépannage
- « Serveur injoignable » : API non démarrée, mauvaise adresse (téléphone : IP du PC), pare-feu Windows.
- Android : redémarrer l'application après avoir changé l'adresse du serveur si l'état reste « Hors ligne ».
- Erreur de restauration `$(MauiVersion)` : remplacer par la version MAUI installée ou supprimer la ligne `PackageReference`.

## Polices (hors ligne)
Inter (`Inter-LICENSE.txt`) et Material Icons Outlined (`Material-Icons-LICENSE.txt`) embarquées dans `SmartDocs/Resources/Fonts`.
