# Guide de Contribution

Merci de l'intérêt que vous portez à contribuer à NavireVisualization !

## Comment Étendre le Projet

### Ajouter de Nouvelles Forces
Si votre CSV de simulation contient de nouvelles composantes de force (par exemple, `Wind Force`) :
1.  **Mettre à jour `ShipCSVPlayer.cs`** :
    *   Ajoutez un nouveau champ à la classe `FrameData`.
    *   Mettez à jour la méthode `LoadCSV` pour analyser la nouvelle colonne.
    *   Mettez à jour la méthode `LerpFrame` pour interpoler la nouvelle valeur.
2.  **Mettre à jour la Visualisation** :
    *   Modifiez `SecondaryHUD.cs` pour afficher la nouvelle valeur.
    *   (Optionnel) Ajoutez une nouvelle flèche dans `ForceVisualizer.cs` si vous souhaitez la voir en 3D.

### Personnaliser le Navire
Pour utiliser un modèle 3D différent pour le navire :
1.  Importez votre modèle dans Unity.
2.  Remplacez le mesh enfant du GameObject `Ship`.
3.  Assurez-vous que le point de pivot de votre mesh s'aligne avec le centre de mouvement défini dans vos données de simulation.

## Normes de Codage
*   **Langage** : C# (Unity).
*   **Formatage** : Conventions standard C# (PascalCase pour les membres publics, camelCase pour les privés).
*   **Commentaires** : Veuillez commenter la logique complexe, en particulier les conversions de coordonnées.

## Signaler des Problèmes
Si vous trouvez un bug ou avez une suggestion, veuillez ouvrir une "issue" dans le dépôt avec :
*   La description du problème.
*   Les étapes pour reproduire.
*   Un exemple de données CSV (si applicable).
