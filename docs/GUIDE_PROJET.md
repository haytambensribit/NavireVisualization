# 🗺️ Guide du Projet NavireVisualization

Ce document sert de **plan d'orientation** pour naviguer dans la documentation du projet. Il vous aidera à trouver rapidement l'information recherchée selon votre rôle (Utilisateur ou Développeur).

---

## 🚀 1. Pour Commencer (Utilisateurs)
*Je veux juste lancer la simulation et visualiser des données.*

*   **[README.md](../README.md)** : **Commencez ici !**
    *   Comment installer et ouvrir le projet.
    *   Les contrôles clavier (Caméra, Pause, Timeline).
    *   Comment lancer une démo rapide.

---

## 🏗 2. Comprendre le Fonctionnement (Développeurs / Architectes)
*Je veux comprendre comment le système est conçu et comment les données circulent.*

*   **[Architecture du Système (docs/ARCHITECTURE.md)](ARCHITECTURE.md)** :
    *   Diagrammes de flux de données.
    *   **Crucial** : Explication de la conversion des coordonnées (NED $\leftrightarrow$ Unity).
    *   Gestion de la boucle de temps (Update loop).
*   **[Formats de Données (docs/DATA_FORMATS.md)](DATA_FORMATS.md)** :
    *   Spécifications techniques des fichiers CSV (colonnes, unités).
    *   Paramétrage du navire via YAML.

---

## 💻 3. Travailler sur le Code (Programmeurs)
*Je dois modifier des scripts ou comprendre l'implémentation C#.*

*   **[Référence des Composants (docs/COMPONENTS.md)](COMPONENTS.md)** :
    *   Détail des scripts principaux (`ShipCSVPlayer`, `Visualizers`).
    *   Explication des paramètres visibles dans l'Inspecteur Unity.
*   **Documentation du Code Source** :
    *   Consultez directement les scripts C# dans `Assets/Scripts/`. Ils disposent de commentaires XML complets (infobulles IntelliSense).
    *   Scripts clés : `ShipCSVPlayer.cs`, `ForceVisualizer.cs`, `MomentVisualizer.cs`.

---

## 🎨 4. L'Environnement Visuel (Artistes / Intégrateurs)
*Je travaille sur la scène Unity, les lumières ou l'océan.*

*   **[Documentation des Scènes (docs/SCENES.md)](SCENES.md)** :
    *   Détail de la `SampleScene`.
    *   Organisation de la hiérarchie (Ship, Cameras, Lights).
*   **[Plugins & Dépendances (docs/PLUGINS.md)](PLUGINS.md)** :
    *   Configuration de l'océan (**Crest Ocean System**).
    *   Outils externes (YamlDotNet, FileBrowser).

---

## 🤝 5. Contribuer au Projet
*Je veux soumettre des modifications ou signaler un bug.*

*   **[Guide de Contribution (docs/CONTRIBUTING.md)](CONTRIBUTING.md)** :
    *   Règles de nommage et standards de code.
    *   Procédure pour ajouter de nouvelles visualisations.

---

*Ce guide a été généré le 12 Décembre 2025 pour faciliter la prise en main du projet.*
