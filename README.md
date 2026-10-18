# SmartDocs — application mobile de gestion documentaire (.NET MAUI)

Intégration frontend des maquettes Google Stitch. Cibles : **Android** (`net10.0-android`) et **Windows Machine** (`net10.0-windows10.0.19041.0`, ajouté sous Windows uniquement).

> ⚠️ **Ce projet n'a pas pu être compilé ni exécuté par son auteur (environnement sans .NET).** Il a été vérifié statiquement uniquement. La première compilation chez vous peut révéler de petites corrections : voir « Dépannage ».

## Prérequis (déjà satisfaits sur votre machine d'après vos captures)
- SDK .NET 10 (`dotnet --list-sdks` → 10.0.x) ; workload `android` (`dotnet workload list`)
- Visual Studio avec la charge de travail « Développement .NET Multiplateforme (MAUI) »
- Un émulateur Android (Android Studio / Android Device Manager de Visual Studio), API 24 minimum

## Ouvrir et lancer dans Visual Studio
1. Dézipper, ouvrir `SmartDocs.sln`.
2. Choisir la cible **Android Emulator** (barre d'outils), créer un émulateur si besoin.
3. Appuyer sur **F5** (ou Ctrl+F5 sans débogage). La première compilation est longue.

## Ligne de commande
```
dotnet restore SmartDocs/SmartDocs.csproj
dotnet build SmartDocs/SmartDocs.csproj -f net10.0-android -c Debug
dotnet build SmartDocs/SmartDocs.csproj -f net10.0-android -t:Run     # émulateur démarré
```

## Lancer sur « Machine Windows »
Dans la barre d'outils de Visual Studio, choisir **Windows Machine** puis F5. Le projet est lancé sans empaquetage MSIX (`WindowsPackageType=None`), la fenêtre s'ouvre au format mobile (420×880). iOS / Mac Catalyst exigent un Mac : non ajoutés.

## Arborescence
```
SmartDocs.sln
SmartDocs/
  SmartDocs.csproj  MauiProgram.cs  App.xaml(.cs)  AppShell.xaml(.cs)
  Models/     DocumentItem, DocumentVersion, ChipItem, ObservableObject
  Services/   DocumentService (données de démo), AuthService (simulé)
  Helpers/    Icons, Palette, Validation, FieldFocus, DocumentActions
  Controls/   AppButton, Badge, FileTile
  Views/      Login, Register, Home, Documents, Upload, Archives, DocumentDetail, Versions
  Resources/  Fonts (Inter + Material Icons, hors ligne), Styles, AppIcon, Splash
  Platforms/Android/
```

## Polices (hors ligne)
Inter (Regular/Medium/SemiBold/Bold) et Material Icons Outlined sont **embarquées** dans `Resources/Fonts` : aucun accès réseau. Licences : `Inter-LICENSE.txt` (SIL OFL), `Material-Icons-LICENSE.txt` (Apache 2.0).

## Fonctionnalités implémentées (données locales en mémoire)
- Connexion et inscription : validation des champs, jauge de robustesse du mot de passe, règles en direct.
- Accueil : accès rapides avec compteurs calculés, documents récents, recherche.
- Documents : recherche, filtre par catégorie, filtre par type (PDF/DOCX/XLSX), tri, menu ⋮.
- Détail d'un document, favori, partage (feuille de partage Android).
- Versions : historique, **restauration** (crée une nouvelle version active, l'historique est conservé).
- Dépôt : sélecteur de fichiers Android réel (nom, taille, contrôle format et 10 Mo), tags, confidentialité ; le document est ajouté à la liste locale.
- Archives : recherche, filtre par année, tri par date, restauration. Archivage depuis le menu ⋮ ou le détail.

## Ce qui nécessiterait un backend (simulé aujourd'hui)
- Authentification (tout e-mail valide + mot de passe non vide est accepté), SSO Microsoft 365 / SAML, mot de passe oublié.
- Stockage réel des fichiers : le fichier choisi n'est **pas** envoyé ni conservé ; Consulter / Télécharger affichent une alerte.
- Aperçu de document, rétention légale, droits d'accès, persistance (tout est réinitialisé à chaque lancement).
- Recherche vocale (icône micro), notifications.

## Écarts assumés par rapport aux maquettes
- Texte « Chiffrement AES-256 certifié » remplacé par une mention d'aperçu indisponible (rien n'est chiffré).
- « 100 % téléversé » devient « 100 % chargé » (rien n'est envoyé).
- Compteurs (142 / 38 / 12) calculés sur le jeu de démo, donc plus petits.
- Ajout d'un bouton « Archiver ce document » sur le détail (accès à l'archivage).
- Barre d'onglets : icônes et couleurs fournies par le Shell Android (police des libellés non personnalisable).

## Dépannage
- Barre d'état qui chevauche l'en-tête : à signaler, ajustement de marge à faire.
- Erreur `$(MauiVersion)` à la restauration : remplacer par la version affichée par `dotnet workload list` ou supprimer la ligne PackageReference.
- Émulateur lent : activer l'accélération matérielle (Hyper-V / HAXM).

## Git (à faire par vous, rien n'est publié)
Copier le contenu dans votre dépôt existant puis un commit par phase : `git add` ciblé, `git commit -m "Phase A : ..."`, etc.
