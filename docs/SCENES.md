# Documentation des Scènes

Ce document décrit les scènes Unity du projet **NavireVisualization**, leur but et les GameObjects clés qu'elles contiennent.

---

## 🏗 SampleScene

**Chemin :** `Assets/Scenes/SampleScene.unity`

C'est la scène principale et unique de l'application. Elle contient l'environnement de visualisation, le navire, l'interface utilisateur (HUD) et les gestionnaires de données.

### 🔑 GameObjects Clés

| GameObject | Rôle & Scripts Associés |
| :--- | :--- |
| **Main Camera** | Caméra principale, gérée par `OrbitalCamera.cs` pour permettre la rotation autour du navire. |
| **Directional Light** | Lumière solaire principale pour l'éclairage de la scène. |
| **Ocean** | (Crest Ocean System) Gère le rendu de l'eau, les vagues et la physique de flottaison. Utilise les composants `OceanRenderer` et `ShapeGerstnerBatched`. |
| **Ship** | Le modèle 3D du navire. Contient :<br>- `ShipCSVPlayer.cs` : Lecture et application des mouvements.<br>- `ForceVisualizer.cs` : Affichage des vecteurs forces.<br>- `MomentVisualizer.cs` : Affichage des arcs de moments.<br>- `SphereWaterInteraction.cs` (Crest) : Interaction avec l'eau. |
| **HUD / Canvas** | Interface utilisateur affichant les données en temps réel.<br>- `ShipHUD.cs` : Affiche vitesse, position, etc.<br>- `TimeSliderController.cs` : Barre de progression temporelle. |
| **EventSystem** | Gère les entrées utilisateur pour l'UI. |

### 🌊 Configuration de l'Océan (Crest)
La scène utilise **Crest Ocean System** pour un rendu réaliste. L'objet **Ocean** est configuré pour :
- Simuler des vagues via le spectre de Gerstner.
- Gérer la réflexion et la réfraction de la lumière.
- Interagir avec le navire pour créer des sillages (via `BoatProbes`).

### 🎮 Contrôles dans la Scène
- **Caméra :** Clic droit + souris pour tourner, Molette pour zoomer.
- **Lecture :** Espace pour Pause/Lecture, Flèches Gauche/Droite pour avancer/reculer.
- **Visualisations :** 
  - `F` : Afficher/Masquer les forces.
  - `M` : Afficher/Masquer les moments.
