using UnityEngine;

public class ShipRightCamera : MonoBehaviour
{
    [Header("Références")]
    public Transform ship;  // le navire à suivre
    public TimeSliderController slider; 
    
    [Header("Position relative")]
    [Tooltip("Décalage local derrière le navire (Z négatif = derrière)")]
    public float distance = 20f;
    [Tooltip("Hauteur de la caméra au-dessus du navire")]
    public float heightAbove = 8f;


    [Header("Comportement dynamique")]
    [Tooltip("Vitesse de rattrapage de la caméra")]
    public float followSmooth = 5f;
    [Tooltip("Vitesse de rotation de la caméra")]
    public float lookSmooth = 6f;
    [Tooltip("Inclinaison automatique de la caméra pendant les virages")]
    public float tiltAmount = 2f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (ship == null) return;

        // 🧭 Calcul de la position cible derrière le navire
        Vector3 targetPosition =
            ship.position
            - ship.forward * distance
            + ship.up * heightAbove;

        Vector3 lookTarget = ship.position + ship.up * 2f;
        float tilt = Mathf.Sin(ship.eulerAngles.y * Mathf.Deg2Rad) * tiltAmount;
        Quaternion baseRot = Quaternion.LookRotation(lookTarget - targetPosition, ship.up);

        bool instant = (slider != null && slider.IsDragging);

        if (instant)
        {
            // 🔒 Pendant le drag : coller direct
            transform.position = targetPosition;
            transform.rotation = baseRot;
            transform.Rotate(Vector3.forward, tilt, Space.Self);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                1f / Mathf.Max(0.01f, followSmooth)
            );

            Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position, ship.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * lookSmooth
            );

            transform.Rotate(Vector3.forward, tilt, Space.Self);
        }

    }
}
