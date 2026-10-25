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
- **Comptes de démonstration** : un par poste, mot de passe commun `Demo1234` (voir « Rôles » ci-dessous). À désactiver : `"Seed": { "Enabled": false }` dans `appsettings.json`. **L'inscription se fait uniquement sur invitation** (voir « Inscription sur invitation » ci-dessous) : le compte reçoit le rôle prévu par l'invitation.
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
| **Administrateur** (DSI) | Public + Interne, ses propres confidentiels | — | — | Gère les comptes, les rôles et les invitations. Ne lit pas les confidentiels des autres |
| **Direction** (DG) | Tout | — | Tous les documents | |
| **Responsable de service** (DRH, DAF, juridique) | Public + Interne + confidentiels de **son** service | Dans son service | Documents de son service | |
| **Collaborateur** | Public + Interne + confidentiels de son service | Dans son service | Ses propres documents | |
| **Auditeur** | Tout | — | — | Lecture seule |
| **Invité** (prestataire) | Documents **Public** uniquement | — | — | Rôle typique d'une invitation à un prestataire |

Confidentialité : **Public** = tous ; **Interne** = tous sauf Invité ; **Confidentiel** = Direction, Auditeur, membres du service concerné, propriétaire. Un document non autorisé n'apparaît pas dans les listes et son téléchargement est refusé.

### Corbeille
- **Mettre à la corbeille / restaurer** : mêmes droits que l'archivage (Direction : tout ; Responsable : son service ; Collaborateur : ses documents). Un document supprimé disparaît des listes et son fichier reste conservé ; une fois restauré, il retrouve son état d'avant (actif ou archivé).
- **Supprimer définitivement** (depuis la corbeille uniquement) : **Direction** (tout) et **Administrateur** (documents qu'il peut voir). Supprime le document, toutes ses versions et leurs fichiers.
- Accès : menu ☰ de l'accueil > « Corbeille » (Direction, Responsable, Collaborateur, Administrateur). La corbeille ne montre que les documents sur lesquels le rôle peut agir.

### Journal d'activité
- **Ce qui est enregistré** : dépôt, consultation / téléchargement, nouvelle version, restauration de version, archivage et restauration, mise à la corbeille, suppression définitive, connexion, inscription, changement de rôle, création et révocation d'invitation (qui, quand, quel document, détail).
- **Qui le lit** : **Direction** et **Auditeur** (tout) ; **Administrateur** (tout sauf les actions sur les documents confidentiels, pour respecter la règle « l'administrateur ne lit pas les confidentiels »). Les autres rôles n'y ont pas accès (403).
- Lecture seule : l'application et l'API ne permettent ni de modifier ni de supprimer une entrée. Les noms sont copiés dans l'entrée : elle reste lisible après une suppression définitive.
- Accès : menu ☰ / icône de compte > « Journal d'activité » ; filtres par type d'action, recherche et pagination.
- Les échecs de connexion ne sont volontairement pas enregistrés (évite de révéler quels comptes existent).

### Inscription sur invitation
- **Il n'existe plus d'inscription libre** : pour créer un compte, il faut un **code d'invitation** (10 caractères, affiché `XXXXX-XXXXX`, majuscules et tiret facultatifs à la saisie). Sans code valide, le serveur refuse l'inscription.
- **Qui invite** : l'**Administrateur**, depuis le menu ☰ / icône de compte > « Invitations » (ou l'enveloppe en haut de « Gérer les utilisateurs »). Il choisit le rôle, le service (si nécessaire), le poste (facultatif), une adresse e-mail (facultatif : l'invitation n'est alors utilisable qu'avec cette adresse) et la durée de validité (24 heures, 7 ou 30 jours). Si une adresse e-mail est saisie et que l'envoi d'e-mails est configuré (voir ci-dessous), le code est **envoyé automatiquement** à cette personne ; sinon l'administrateur copie ou partage le code lui-même. Il peut aussi envoyer ou renvoyer l'e-mail plus tard, ou **révoquer** une invitation non utilisée.
- **À l'inscription** : le compte reçoit directement le rôle, le service et le poste de l'invitation, qui devient **Utilisée** (un code ne sert qu'une fois). Le contrôle (code inconnu, expiré, déjà utilisé ou révoqué, adresse e-mail) est fait par le serveur ; le message d'erreur est le même quelle que soit la cause, pour ne pas révéler quels codes existent. La création du compte et la consommation du code sont une seule transaction : deux personnes ne peuvent pas utiliser le même code.
- **Journal** : création, envoi par e-mail et révocation d'une invitation, et inscription (avec le numéro de l'invitation utilisée). Le code lui-même n'est jamais écrit dans le journal.
- **Premier démarrage sans compte de démonstration** (`Seed:Enabled = false`) : l'API génère une invitation **Administrateur** valable 7 jours et affiche son code **dans la console du serveur** (ligne « Aucun compte : utilisez ce code… »). Si elle expire avant usage, relancer l'API en l'absence de compte en génère une nouvelle.
- **Démonstration** : deux invitations sont créées par le jeu de démonstration (valables 1 an, usage unique) : `ALPHA-FIN25` (Collaborateur, Finance) et `BRAVO-INV25` (Invité). Thomas Dubois en crée d'autres depuis l'écran « Invitations ».

### Envoi des invitations par e-mail (facultatif)
L'API envoie le code à l'adresse de l'invitation via un serveur SMTP. **Rien n'est obligatoire** : tant que la section `Smtp` de `appsettings.json` n'est pas activée, l'application fonctionne comme avant (l'administrateur copie ou partage le code) et l'écran l'indique après la création. Au démarrage, la console de l'API indique si l'envoi est activé.

```json
"Smtp": {
  "Enabled": true,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "User": "votre.adresse@gmail.com",
  "Password": "",
  "FromAddress": "votre.adresse@gmail.com",
  "FromName": "SmartDocs"
}
```
- **Mot de passe** : ne pas l'écrire dans le fichier ; le définir en variable d'environnement `Smtp__Password` (Visual Studio : propriétés du projet > Débogage > Variables d'environnement).
- **Pas de serveur SMTP ?** Trois solutions : (1) **Gmail** avec un « mot de passe d'application » (validation en 2 étapes requise sur le compte Google ; hôte `smtp.gmail.com`, port 587, l'expéditeur doit être l'adresse Gmail) ; (2) un service d'envoi à offre gratuite comme **Brevo** (les paramètres SMTP sont dans le compte : hôte, port 587, identifiant et clé SMTP, expéditeur à valider) ; (3) pour **tester sans rien envoyer**, l'outil gratuit **smtp4dev** (`dotnet tool install -g Rnwood.Smtp4dev`, puis `smtp4dev`) simule un serveur sur le PC et affiche les e-mails reçus dans une page web : `Host` = `localhost`, `EnableSsl` = `false`, `User` vide, `Port` = celui indiqué par l'outil (25 par défaut).
- **Port 465** (SSL implicite) non pris en charge : utiliser le port **587** (STARTTLS, `EnableSsl` = `true`).
- Si l'envoi échoue, l'invitation reste créée et le code est montré à l'administrateur ; le détail technique de l'erreur est dans la console de l'API (jamais le mot de passe).
- Pour de la vraie production : un domaine d'envoi avec SPF/DKIM, sinon les messages risquent d'arriver en courrier indésirable.

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
| `POST /api/auth/register` (`fullName`, `email`, `password`, `inviteCode`), `/login` — `GET /api/auth/me` | Inscription **sur invitation** (code obligatoire), connexion (JWT), profil |
| `GET /api/documents` (`search`, `category`, `extension`, `archived`, `sort`, `year`) | Liste, recherche, filtres, tri |
| `GET /api/documents/{id}` | Détail (404 si le rôle ne permet pas de le voir) |
| `GET /api/admin/users` — `PUT /api/admin/users/{id}` | Liste des comptes, changement de rôle / service / poste (Administrateur) |
| `GET /api/admin/invitations` — `POST` (`role`, `department`, `jobTitle`, `email`, `validDays` 1 à 30) — `POST /api/admin/invitations/{id}/email` — `DELETE /api/admin/invitations/{id}` | Liste, création (avec envoi automatique de l'e-mail si configuré), envoi ou renvoi de l'e-mail, révocation (Administrateur) |
| `POST /api/documents` (multipart : `file`, `name`, `category`, `confidentiality`, `tags`) | Dépôt (PDF/DOCX/XLSX, 10 Mo max) |
| `GET /api/documents/{id}/file` | Téléchargement de la version courante |
| `POST /api/documents/{id}/archive` / `unarchive` | Archivage, restauration depuis les archives |
| `DELETE /api/documents/{id}` — `POST .../undelete` — `DELETE .../purge` — `GET /api/documents?trash=true` | Mise à la corbeille (suppression logique), restauration, suppression définitive, liste de la corbeille |
| `GET /api/activity` (`action` = une ou plusieurs actions séparées par des virgules, `search`, `before`, `take`) | Journal d'activité (Direction, Auditeur, Administrateur) |
| `POST /api/documents/{id}/favorite` | Favori (propre à chaque utilisateur) |
| `GET /api/documents/{id}/versions` — `POST` (multipart `file`, `comment`) | Historique, nouvelle version (bouton « Ajouter une nouvelle version » de l'écran Historique) |
| `GET .../versions/{versionId}/file` — `POST .../versions/{versionId}/restore` | Téléchargement, restauration (crée une nouvelle version active) |

## Sécurité mise en place
Rôles et visibilité appliqués côté serveur (le rôle est relu en base à chaque requête) ; mots de passe hachés (PBKDF2-SHA256, sel aléatoire) ; jeton JWT signé, durée 8 h ; toutes les routes documents exigent un jeton ; limitation à 20 requêtes/minute/IP sur la connexion et l'inscription ; inscription uniquement sur invitation (code aléatoire cryptographique à 32^10 combinaisons, usage unique, expiration, révocable, consommé dans la même transaction que la création du compte) ; contrôle d'extension **et** de signature des fichiers, taille 10 Mo ; noms de fichiers générés par le serveur (pas de chemin fourni par l'utilisateur) ; erreurs internes sans détails techniques ; jeton conservé dans le stockage sécurisé de l'appareil.

## Limites connues (à mentionner dans le rapport)
- **Développement local** : HTTP en clair (autorisé dans le manifeste Android), clé JWT de démonstration dans `appsettings.json`. En production : HTTPS, clé secrète en variable d'environnement (`Jwt__Key`), base serveur, sauvegardes.
- Pas encore : modification des métadonnées, vidage automatique de la corbeille (la suppression définitive est manuelle), purge ou archivage du journal d'activité (il grossit sans limite).
- Recherche, filtres et tri de l'écran s'appliquent sur la liste déjà chargée depuis l'API ; l'API sait aussi filtrer côté serveur (paramètres ci-dessus).
- Les tailles affichées pour les documents de démonstration sont fictives ; leurs fichiers sont de petits PDF/DOCX/XLSX générés.
- L'envoi des invitations par e-mail demande un serveur SMTP configuré (sinon l'administrateur transmet le code lui-même) ; l'envoi n'a pas pu être testé avec un vrai serveur. Les codes sont stockés en clair en base (l'administrateur doit pouvoir les relire) ; les invitations expirées ou utilisées restent en base.
- Non implémentés : mot de passe oublié, SSO Microsoft 365 / SAML, recherche vocale, aperçu intégré (le bouton Consulter ouvre le fichier avec une application de l'appareil), notifications.
- Pas de migrations de base (`EnsureCreated`) : un changement de modèle impose de supprimer `smartdocs.db`.

## Dépannage
- « Serveur injoignable » : API non démarrée, mauvaise adresse (téléphone : IP du PC), pare-feu Windows.
- Android : redémarrer l'application après avoir changé l'adresse du serveur si l'état reste « Hors ligne ».
- Erreur de restauration `$(MauiVersion)` : remplacer par la version MAUI installée ou supprimer la ligne `PackageReference`.

## Polices (hors ligne)
Inter (`Inter-LICENSE.txt`) et Material Icons Outlined (`Material-Icons-LICENSE.txt`) embarquées dans `SmartDocs/Resources/Fonts`.
