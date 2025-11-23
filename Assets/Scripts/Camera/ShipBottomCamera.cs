using UnityEngine;

public class ShipBottomCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;
    public TimeSliderController slider;

    [Header("Position relative (vue du dessous)")]
    [Tooltip("Profondeur sous le navire (valeur positive = plus bas)")]
    public float depthBelow = 10f;


    [Header("Lissage")]
    public float followSmooth = 5f;
    public float lookSmooth = 8f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (ship == null)
        {
            Debug.LogWarning("🚫 ShipBottomCamera: aucune référence au navire !");
            return;
        }

        // 📍 Position cible : sous le navire
        Vector3 targetPosition =
            ship.position
            - Vector3.up * depthBelow; // en dessous du navire

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
