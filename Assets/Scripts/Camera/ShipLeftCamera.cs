using UnityEngine;

public class ShipLeftCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;
    public TimeSliderController slider;

    [Header("Position relative (vue avant)")]
    [Tooltip("Hauteur au-dessus du navire")]
    public float heightAbove = 6f;
    [Tooltip("Distance devant le navire (Z négatif = plus proche)")]
    public float distanceInFront = 20f;

    [Header("Lissage")]
    public float followSmooth = 5f;
    public float lookSmooth = 8f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (ship == null)
        {
            Debug.LogWarning("🚫 ShipFrontCamera: aucune référence au navire !");
            return;
        }

        // 📍 Position cible : devant le navire (vers la proue)
        Vector3 targetPosition =
            ship.position
            + ship.forward * distanceInFront   // devant le navire
            + Vector3.up * heightAbove;        // hauteur

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
