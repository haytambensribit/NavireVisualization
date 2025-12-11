# Documentation des Plugins

Ce document détaille les plugins et librairies externes utilisés dans le projet **NavireVisualization**.

---

## 🌊 Crest Ocean System

**Version :** 4.15 (URP)
**Dossier :** `Assets/Crest/`
**Lien :** [Crest GitHub / Asset Store](https://github.com/wave-harmonic/crest)

### Description
Crest est un système avancé de rendu d'océan pour Unity. Il est utilisé ici pour simuler une surface d'eau réaliste et gérer les interactions physiques visuelles.

### Utilisation dans le Projet
- **OceanRenderer** : Composant principal sur le GameObject `Ocean` dans la scène. Il gère la géométrie de l'eau, les LODs (Level of Detail) et les shaders.
- **ShapeGerstnerBatched** : Génère les vagues selon un spectre physique.
- **SphereWaterInteraction** : Script situé sur le navire (`Ship`) pour simuler l'interaction de la coque avec l'eau (génération d'écume et de vagues locales).

### Configuration Clé (`Ocean`)
- **Base Mesh Resolution :** Définit la qualité du maillage de l'eau.
- **Ocean Material :** Shader URP personnalisé pour l'eau.
- **Lod Data Resolution :** Résolution des textures de données (écume, vagues).

---

## 📄 YamlDotNet

**Version :** 16.3.0
**Dossier :** `Assets/Packages/YamlDotNet.16.3.0/`
**Lien :** [YamlDotNet GitHub](https://github.com/aaubry/YamlDotNet)

### Description
Une bibliothèque .NET populaire pour parser et générer du YAML.

### Utilisation dans le Projet
Utilisé par le script `ShipCSVPlayer.cs` pour lire le fichier de configuration du navire (`.yml`). Ce fichier contient des paramètres statiques comme :
- La position initiale du corps (`initial position of body frame`).
- La position de l'hélice (`position of propeller frame`).

**Extrait de code (`ShipCSVPlayer.cs`) :**
```csharp
float ExtractYamlFloat(string yaml, string section, string key) { ... }
```
*Note : Le projet utilise actuellement une extraction manuelle via Regex pour plus de simplicité, mais la librairie est incluse pour des parsing plus complexes si nécessaire.*

---

## 📂 StandaloneFileBrowser

**Version :** 1.0
**Dossier :** `Assets/StandaloneFileBrowser/`
**Lien :** [GitHub](https://github.com/gkngkc/UnityStandaloneFileBrowser)

### Description
Un wrapper permettant d'ouvrir des boîtes de dialogue de système natif (Windows, macOS, Linux) pour sélectionner des fichiers au runtime.

### Utilisation dans le Projet
Permet à l'utilisateur de sélectionner ses fichiers de données au lancement de l'application (ou via l'éditeur).

**Scripts :** `ShipCSVPlayer.cs`
**Fonctions Clés :**
- `OpenFilePanel` : Ouvre une fenêtre pour choisir les fichiers CSV et YAML.

```csharp
csvFileName = EditorUtility.OpenFilePanel("Sélectionner un fichier CSV", Application.streamingAssetsPath, "csv");
```
*Note : En build autonome, `StandaloneFileBrowser` remplace `EditorUtility` qui n'est disponible que dans l'éditeur Unity.*
