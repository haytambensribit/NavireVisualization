# NavireVisualization

**NavireVisualization** est un outil de visualisation 3D haute fidélité construit sous Unity pour l'analyse de l'hydrodynamique des navires. Il comble le fossé entre la simulation numérique et la compréhension visuelle en rejouant les données de simulation dans un environnement océanique réaliste.



## 🚀 Fonctionnalités Clés

*   **Lecture Guidée par les Données** : Rejouez des mouvements de navires complexes à partir de données de simulation CSV.
*   **Visualisation Physique** : Visualisation en temps réel en 3D des forces et des moments agissant sur la coque.
*   **Environnement Réaliste** : Utilise le **Crest Ocean System** pour un rendu océanique de haute qualité.
*   **HUD Détaillé** : Affichage tête haute montrant la télémétrie en temps réel (vitesse, position, forces).
*   **Exportation de Données** : Outils intégrés pour tracer et exporter des graphiques de forces directement depuis l'application.

## 🛠️ Pour Commencer

### Prérequis
*   **Unity 2021.3 LTS** ou version ultérieure (recommandé).
*   **Crest Ocean System** (inclus dans `Packages/` ou `Assets/`).

### Installation
1.  Clonez le dépôt :
    ```bash
    git clone https://github.com/votre-repo/NavireVisualization.git
    ```
2.  Ouvrez le projet dans Unity Hub.
3.  Attendez qu'Unity importe les assets et résolve les paquets.

### Lancer la Démo
1.  Ouvrez la scène `Assets/Scenes/MainScene.unity` (ou similaire).
2.  Appuyez sur **Play** dans l'éditeur Unity.
3.  Le navire devrait commencer à bouger en fonction des données CSV par défaut trouvées dans `StreamingAssets`.

## 🎮 Utilisation Rapide

| Action | Contrôle |
| :--- | :--- |
| **Basculer le HUD** | `Ctrl + H` |
| **Basculer le HUD Secondaire** | `Ctrl + J` |
| **Basculer les Flèches de Force** | `F` ou `,` |
| **Basculer les Arcs de Moment** | `M` ou `;` |
| **Contrôle de la Caméra** | Contrôles standard de la scène Unity ou scripts de caméra personnalisés (si actifs). |

## 📂 Structure du Répertoire

*   **`Assets/Scripts/`** : Scripts C# principaux pour la logique de visualisation.
*   **`Assets/StreamingAssets/`** : Placez vos fichiers de données de simulation `.csv` et de configuration `.yml` ici.
*   **`Assets/Crest/`** : Fichiers du système de rendu de l'océan.
*   **`docs/`** : Documentation détaillée du projet.

## 📚 Documentation

Pour plus d'informations détaillées, veuillez consulter la documentation dans le dossier `docs/` :

*   [**Vue d'Ensemble de l'Architecture**](docs/ARCHITECTURE.md) : Comprendre la conception de haut niveau et le flux de données.
*   **[Formats de Données](docs/DATA_FORMATS.md)** : Apprenez à formater vos fichiers CSV et YAML.
*   **[Référence des Composants](docs/COMPONENTS.md)** : Guide détaillé des scripts principaux et de leurs paramètres.
*   **[Contribuer](docs/CONTRIBUTING.md)** : Directives pour étendre le projet.
