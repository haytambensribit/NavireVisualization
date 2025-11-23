using UnityEngine;

public class ShipTopCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;
    public TimeSliderController slider; 
    
    [Header("Position relative (vue du dessus)")]
    [Tooltip("Hauteur au-dessus du navire")]
    public float heightAbove = 40f;


    [Header("Lissage")]
    public float followSmooth = 5f;
    public float lookSmooth = 8f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (ship == null) return;

        // 📍 Position directement au-dessus du navire
        Vector3 targetPosition =
            ship.position
            + Vector3.up * heightAbove;

        // Orientation vers le navire (vue plongeante)
        Vector3 lookTarget = ship.position;
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPosition, Vector3.up);

        bool instant = (slider != null && slider.IsDragging);

        if (instant)
        {
            // 🔒 Pendant le drag du slider : pas de smoothing
            transform.position = targetPosition;
            transform.rotation = targetRot;
        }
        else
        {
            // 🎬 Lecture normale : suivi fluide
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
