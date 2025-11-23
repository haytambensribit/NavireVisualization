using UnityEngine;

public class ShipFrontCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;
    public TimeSliderController slider; 
    
    [Header("Position relative (vue droite)")]
    [Tooltip("Hauteur au-dessus du navire")]
    public float heightAbove = 8f;
    [Tooltip("Décalage arrière (positif = derrière, négatif = devant)")]
    public float distanceBehind = 15f;


    [Header("Lissage")]
    public float followSmooth = 5f;
    public float lookSmooth = 8f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (ship == null)
        {
            Debug.LogWarning("🚫 ShipRightCamera: aucune référence au navire !");
            return;
        }

        // Calcul de la position cible : sur le côté droit
        Vector3 targetPosition =
            ship.position
            + ship.right * distanceBehind  // droite = +right
            + Vector3.up * heightAbove;

        Vector3 lookTarget = ship.position;
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPosition, Vector3.up);

        bool instant = (slider != null && slider.IsDragging);

        if (instant)
        {
            transform.position = targetPosition;
            transform.rotation = targetRot;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                1f / Mathf.Max(0.01f, followSmooth)
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * lookSmooth
            );
        }

    }
}
