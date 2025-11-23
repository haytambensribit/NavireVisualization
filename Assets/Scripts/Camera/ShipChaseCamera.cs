using UnityEngine;

public class ShipChaseCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;
    public TimeSliderController slider;

    [Header("Position relative (vue gauche)")]
    [Tooltip("Hauteur au-dessus du navire")]
    public float heightAbove = 15f;
    [Tooltip("Décalage arrière (positif = derrière, négatif = devant)")]
    public float distanceBehind = 30f;

    [Header("Lissage")]
    public float followSmooth = 5f;
    public float lookSmooth = 10f;

    private Vector3 velocity = Vector3.zero;
    float ContinuousYaw(float yaw)
    {
        // garde un yaw continu sans saut
        return Mathf.Repeat(yaw + 360f, 360f);
    }
    void LateUpdate()
    {
        if (ship == null)
        {
            Debug.LogWarning("🚫 ShipLeftCamera: aucune référence au navire !");
            return;
        }

        // Calcul de la position cible : sur le côté gauche
        Vector3 targetPosition =
            ship.position
            + ship.right * - distanceBehind  // gauche = -right
            + Vector3.up * heightAbove;

        Vector3 lookTarget = ship.position;
        Vector3 dir = lookTarget - targetPosition;

        // empêcher les sauts de 360° sur yaw
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        yaw = ContinuousYaw(yaw);

        // reconstruire la rotation stable
        Quaternion targetRot = Quaternion.Euler(0f, yaw, 0f);


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
