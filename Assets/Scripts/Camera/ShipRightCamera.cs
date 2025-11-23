using UnityEngine;

public class ShipRightCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;  
    public TimeSliderController slider;

    [Header("Position relative")]


    [Tooltip("Distance derrière le navire (positive = recule)")]
    public float distanceBehind = 20f;

    [Tooltip("Hauteur de la caméra")]
    public float heightAbove = 8f;

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

        // 📌 1. Position cible — EXACTEMENT comme LeftCamera mais à droite
        Vector3 targetPosition =
            ship.position
            - ship.forward * distanceBehind          // décalage à droite
            + Vector3.up * heightAbove;         // hauteur (vertical global)

        // 📌 2. La caméra regarde TOUJOURS le navire depuis une verticale global stable
        Vector3 lookTarget = ship.position;
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPosition, Vector3.up);

        bool instant = (slider != null && slider.IsDragging);

        if (instant)
        {
            // 🔒 Pendant drag → position instantanée sans lissage
            transform.position = targetPosition;
            transform.rotation = targetRot;
        }
        else
        {
            // 🎞 Position lissée
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                1f / Mathf.Max(0.01f, followSmooth)
            );

            // 🎯 Rotation lissée
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * lookSmooth
            );
        }
    }
}
