# Notepad Commander

Editeur de texte avance construit avec **Avalonia UI 11.2** et **.NET 8**, offrant une interface a ruban, la coloration syntaxique, le multi-onglets et de nombreux outils d'edition.

## Fonctionnalites

### Edition
- Coloration syntaxique via TextMate (20+ langages : C#, JavaScript, Python, TypeScript, HTML, CSS, JSON, XML, etc.)
- Multi-onglets avec indicateur de modification et sauvegarde automatique
- Recherche et remplacement avec support regex, sensibilite a la casse, mot entier
- Aller a la ligne (Ctrl+G)
- Zoom (Ctrl+molette, Ctrl++/-)

### Transformations de texte
- Tri de lignes (ascendant/descendant), suppression de doublons
- Conversions de casse (MAJUSCULES, minuscules, Titre, Inverser)
- Rogner les espaces, supprimer les lignes vides, joindre, inverser
- Commenter/decommenter selon le langage detecte
- Formatage JSON/XML, minification JSON
- Encodage/decodage Base64 et URL

### Outils avances
- Comparaison de fichiers avec diff colore (DiffPlex)
- Enregistrement et lecture de macros
- Gestionnaire de snippets avec declencheurs
- Apercu Markdown en direct (Markdig)
- Calculateur d'expressions mathematiques (NCalc)
- Auto-completion (Ctrl+Space) avec mots-cles du langage et mots du document

### Interface
- Ruban avec 5 onglets : Accueil, Edition, Affichage, Outils, Aide
- Backstage (Fichier) avec sections Accueil, Ouvrir, Enregistrer, Info
- Panneau lateral explorateur de fichiers
- Barre d'etat : ligne:colonne, encodage, fin de ligne, langage
- Themes clair et sombre
- Dialogue de preferences complet

### Gestion de fichiers
- Detection automatique de l'encodage (UTF-8, UTF-8 BOM, ASCII, Latin-1, UTF-16)
- Detection et conversion des fins de ligne (CRLF, LF, CR)
- Surveillance des modifications externes avec prompt de rechargement
- Sauvegarde automatique configurable
- Restauration de session au redemarrage
- Fichiers recents avec epinglage
- Drag & drop de fichiers

## Architecture

```
NotepadCommander.sln
src/
  NotepadCommander.Core/     # Logique metier, modeles, services
  NotepadCommander.UI/       # Avalonia UI, ViewModels, Views, Ruban
  NotepadCommander.Cli/      # Interface ligne de commande
  NotepadCommander.Tests/    # Tests unitaires xUnit (159 tests)
```

**Patterns** : MVVM (CommunityToolkit.Mvvm), Injection de dependances (Microsoft.Extensions.DI), Services singleton, ViewModels transients.

## Prerequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Demarrage rapide

```bash
# Cloner le depot
git clone https://github.com/phuetz/NotepadCommander.git
cd NotepadCommander

# Compiler
dotnet build

# Lancer l'application
dotnet run --project src/NotepadCommander.UI

# Lancer les tests
dotnet test
```

## CLI

```bash
# Formater un fichier JSON ou XML
dotnet run --project src/NotepadCommander.Cli -- format fichier.json json

# Convertir l'encodage
dotnet run --project src/NotepadCommander.Cli -- convert fichier.txt utf-8

# Comparer deux fichiers
dotnet run --project src/NotepadCommander.Cli -- diff fichier1.txt fichier2.txt

# Transformer du texte
dotnet run --project src/NotepadCommander.Cli -- transform fichier.txt sort-asc
dotnet run --project src/NotepadCommander.Cli -- transform fichier.txt uppercase
dotnet run --project src/NotepadCommander.Cli -- transform fichier.txt encode-base64
```

Operations disponibles : `sort-asc`, `sort-desc`, `uppercase`, `lowercase`, `titlecase`, `trim`, `remove-empty`, `remove-duplicates`, `reverse`, `encode-base64`, `decode-base64`, `encode-url`, `decode-url`.

## Raccourcis clavier

| Raccourci | Action |
|-----------|--------|
| Ctrl+N | Nouveau fichier |
| Ctrl+O | Ouvrir |
| Ctrl+S | Enregistrer |
| Ctrl+Shift+S | Enregistrer sous |
| Ctrl+W | Fermer l'onglet |
| Ctrl+F | Rechercher |
| Ctrl+H | Remplacer |
| Ctrl+G | Aller a la ligne |
| Ctrl+Z / Ctrl+Y | Annuler / Refaire |
| Ctrl+/ | Commenter/Decommenter |
| Ctrl+Tab | Onglet suivant |
| Ctrl++ / Ctrl+- | Zoom avant / arriere |
| Ctrl+0 | Reinitialiser le zoom |
| Ctrl+Space | Auto-completion |
| Ctrl+1..5 | Onglet du ruban |
| F11 | Plein ecran |

## Stack technique

| Composant | Version |
|-----------|---------|
| Avalonia UI | 11.2.1 |
| AvaloniaEdit | 11.2.0 |
| TextMateSharp.Grammars | 1.0.65 |
| CommunityToolkit.Mvvm | 8.2.2 |
| DiffPlex | 1.7.2 |
| Markdig | 0.37.0 |
| NCalcSync | 5.3.0 |
| xUnit | 2.6.6 |

## Licence

Ce projet est sous licence MIT.
