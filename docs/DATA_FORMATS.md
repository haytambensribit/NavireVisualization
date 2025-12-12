# Formats de Données

NavireVisualization s'appuie sur deux formats de fichiers principaux pour ses données d'entrée : **CSV** pour les étapes de simulation et **YAML** pour la configuration statique.

## 1. Données de Simulation CSV
Le fichier CSV contient les données de séries temporelles pour la simulation. Il doit être placé dans le dossier `Assets/StreamingAssets/`.

### Structure du Fichier
*   **Délimiteur** : Virgule (`,`)
*   **En-tête** : La première ligne doit contenir les noms des colonnes.
*   **Séparateur Décimal** : Point (`.`)

### Colonnes Requises
Le système recherche des en-têtes de colonnes spécifiques (insensible à la casse).

| Clé de Colonne | Description | Unité |
| :--- | :--- | :--- |
| `t` | Temps de Simulation | Secondes (s) |
| `x(ship)` | Position X (NED) | Mètres (m) |
| `y(ship)` | Position Y (NED) | Mètres (m) |
| `z(ship)` | Position Z (NED) | Mètres (m) |
| `phi(ship)` | Angle de Roulis (Roll) | Radians (rad) |
| `theta(ship)` | Angle de Tangage (Pitch) | Radians (rad) |
| `psi(ship)` | Angle de Lacet (Yaw) | Radians (rad) |
| `u(ship)` | Vitesse de Cavalement (Surge) | m/s |
| `v(ship)` | Vitesse d'Embardée (Sway) | m/s |
| `w(ship)` | Vitesse de Pilonnement (Heave) | m/s |

### Colonnes de Force (Optionnel)
Le système est préconfiguré pour lire et stocker les composantes de forces suivantes si elles sont présentes dans le CSV (insensible à la casse) :

*   `fx(gravity ship ship)`, `fy(...)`, `fz(...)`
*   `fx(non-linear hydrostatic (fast) ship ship)`, ...
*   `fx(non-linear froude-krylov ship ship)`, ...
*   `fx(diffraction ship ship)`, ...
*   `fx(radiation damping ship ship)`, ...
*   `fx(holtrop & mennen ship ship)`, ...
*   `fx(propellerandrudder ship propellerandrudder)`, ...

Ces données sont stockées dans la structure `FrameData` mais ne sont pas nécessairement toutes visualisées par défaut.

**Exemple :** `fx(sum of forces ship ship)` représente la force totale dans la direction X.

## 2. Configuration YAML
Le fichier YAML définit les propriétés statiques du modèle de navire.

### Exemple de Structure
```yaml
position of propeller frame:
  x: {value: -3.5}
  y: {value: 0.0}
  z: {value: 1.2}

initial position of body frame:
  x: {value: 0.0}
  y: {value: 0.0}
  z: {value: 0.0}
  phi: {value: 0.0}
  theta: {value: 0.0}
  psi: {value: 0.0}
```

### Paramètres Clés
*   **`position of propeller frame`** : Définit où la flèche de force de l'hélice sera dessinée par rapport au centre du navire.
*   **`initial position of body frame`** : Le décalage de départ du navire dans le monde de simulation.
