# Référence des Composants

Ce guide détaille les principaux scripts utilisés dans le projet et leurs options de configuration dans l'Inspecteur Unity.

## 1. ShipCSVPlayer
**Script :** `Assets/Scripts/ShipCSVPlayer.cs`

C'est le contrôleur principal. Il lit les données et déplace le navire.

### Paramètres de l'Inspecteur
*   **Réglages CSV** :
    *   `Csv File Name` : Nom du fichier dans `StreamingAssets` (par exemple, `simulation.csv`).
    *   `Playback Speed` : Multiplicateur de temps (1.0 = temps réel).
    *   `Loop` : Si coché, la simulation redémarre lorsqu'elle est terminée.
*   **Réglages YAML** :
    *   `Yaml File Name` : Nom du fichier de configuration du navire.
*   **Réglages du Transform du Navire** :
    *   `Position Scale` : Facteur d'échelle pour les coordonnées de position (généralement 1.0).
    *   `Position Offset` : Décalage ajouté à la position finale Unity.
    *   `Rotation Offset` : Décalage de rotation (Euler X, Y, Z) ajouté à l'orientation du navire.

## 2. ForceVisualizer
**Script :** `Assets/Scripts/ForceVisualizer.cs`

Visualise les forces linéaires sous forme de flèches 3D.

### Paramètres de l'Inspecteur
*   **Ship Transform** : Référence à l'objet navire.
*   **Player** : Référence au `ShipCSVPlayer`.
*   **Mise à l'échelle des Forces** :
    *   `Fref` : Vecteur de force de référence (x, y, z) utilisé pour normaliser la longueur des flèches. Si (0,0,0), il est calculé automatiquement à partir des valeurs maximales du CSV.
    *   `Global Scale` : Multiplicateur de taille globale pour les flèches.
*   **Réglages de la Flèche** :
    *   `Shaft Radius` : Épaisseur du corps de la flèche.
    *   `Head Length` : Longueur de la pointe de la flèche.

## 3. MomentVisualizer
**Script :** `Assets/Scripts/MomentVisualizer.cs`

Visualise les moments de rotation (couple) sous forme d'arcs 3D courbés autour du navire.

### Paramètres de l'Inspecteur
*   **Réglages Visuels** :
    *   `Base Radius` : Rayon du cercle de l'arc.
    *   `Tube Radius` : Épaisseur de l'arc.
    *   `Smooth Factor` : Lissage appliqué aux données pour réduire les tremblements (0.0 - 1.0).
*   **Couleurs** : Couleurs personnalisées pour les moments de Roulis (Roll), Tangage (Pitch) et Lacet (Yaw).

## 4. Système HUD
**Scripts :** `MainHUD.cs`, `SecondaryHUD.cs`

Affiche les données en temps réel à l'écran.

### Contrôles
*   **HUD Principal** : Basculer avec `Ctrl + H`. Affiche la vitesse, la position et les forces totales.
*   **HUD Secondaire** : Basculer avec `Ctrl + J`. Affiche une ventilation détaillée des composantes de force.

## 5. PlotExporter
**Script :** `Assets/Scripts/HUD/PlotExporter.cs`

Permet à l'utilisateur de générer et d'exporter des graphiques PNG des forces.

### Utilisation
1.  Sélectionnez les forces à tracer en utilisant les boutons (Fx, Fy, Fz, etc.).
2.  Cliquez sur **Générer des Graphiques** pour créer les textures en mémoire.
3.  Cliquez sur **Télécharger ZIP** pour les enregistrer sur le disque.
