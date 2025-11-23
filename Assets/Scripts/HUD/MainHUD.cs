using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class MainHUD : MonoBehaviour
{
    [Header("Références")]
    public ShipCSVPlayer shipData;
    public TMP_Text hudText;
    public CanvasGroup hudGroup;

    [Header("Affichage de la vitesse")]
    [Range(0f, 1f)] public float smoothFactor = 0.2f;

    [Header("Affichage des angles")]
    [Tooltip("Facteur multiplicatif appliqué aux angles pour l’affichage uniquement")]
    public float rotationDisplayFactor = 1f; // ✅ ton multiplicateur d’affichage
    
    private float initialYaw = float.NaN;

    private float smoothedSpeed = 0f;
    private bool isVisible = true;
    private bool togglePressed = false;

    void Update()
    {
        HandleHUDToggle();

        if (!isVisible)
        {
            if (hudGroup != null)
                SetHUDVisibility(false);
            return;
        }
        else if (hudGroup != null)
        {
            SetHUDVisibility(true);
        }

        if (shipData == null || shipData.CurrentFrame == null)
            return;

        var f = shipData.CurrentFrame;
        bool hasUVW = f.u != 0f || f.v != 0f || f.w != 0f;

        float speedMs = 0f;
        if (hasUVW)
        {
            speedMs = Mathf.Sqrt(f.u * f.u + f.v * f.v + f.w * f.w);
        }
        else if (shipData.PreviousFrame != null)
        {
            Vector3 delta = f.position - shipData.PreviousFrame.position;
            speedMs = delta.magnitude / Time.deltaTime;
        }

        float speedKmh = speedMs * 3.6f;
        float speedKnots = speedMs * 1.94384f;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speedMs, 1f - smoothFactor);

        // ✅ Multiplication uniquement pour l’affichage
        float phiDisplay   =  f.rotation.x * rotationDisplayFactor;   // roll
        float thetaDisplay =  f.rotation.z * rotationDisplayFactor;   // pitch (venant de theta NED → Unity z)
        // ====================================================
        // Yaw relatif (0° au départ, 0–360° ensuite)
        // ====================================================
        float yawUnity = -f.rotation.y;   // ton yaw Unity déjà inversé

        // Mémorise le yaw de départ
        if (float.IsNaN(initialYaw))
            initialYaw = yawUnity;

        // Yaw relatif
        float yawRelative = yawUnity - initialYaw;

        // Conversion dans [0,360]
        yawRelative = Mathf.Repeat(yawRelative, 360f);

        float psiDisplay   =  yawRelative * rotationDisplayFactor;   // yaw (venant de psi NED → Unity y inversé)

        hudText.text =
            $"<b><color=#000000>Mesures principales</color></b>\n" +
            $"<b>Temps :</b> {f.time:F2} s\n" +
            $"<b>Vitesse :</b> {smoothedSpeed:F2} m/s   ({speedKmh:F1} km/h  |  {speedKnots:F1} kn)\n\n" +
            $"<b>Position :</b>\n" +
            $"X = {f.position.x:F2}   Y = {f.position.z:F2}   Z = {f.position.y:F2}\n\n" +
            $"<b>Rotation (multipliées par 1000) :</b>\n" +
            $"Phi (roll) = {phiDisplay:F2}°   Theta (pitch) = {thetaDisplay:F2}°   Psi (yaw) = {psiDisplay:F2}°\n\n" +
            $"<b>Forces totales [N]</b>\n" +
            $"Fx = {f.totalFx:F1}   Fy = {f.totalFy:F1}   Fz = {f.totalFz:F1}\n\n" +
            $"<b>Moments totaux [N·m]</b>\n" +
            $"Mx = {f.totalMx:F1}   My = {f.totalMy:F1}   Mz = {f.totalMz:F1}";
    }

    // =========================================================
    // 🔹 Gère le raccourci clavier Ctrl + H
    // =========================================================
    void HandleHUDToggle()
    {
        bool ctrlPressed =
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            (Keyboard.current != null &&
             (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed));

        bool hPressed =
            Input.GetKey(KeyCode.H) ||
            (Keyboard.current != null && Keyboard.current.hKey.isPressed);

        if (ctrlPressed && hPressed)
        {
            if (!togglePressed)
            {
                togglePressed = true;
                isVisible = !isVisible;
                Debug.Log($"🔁 HUD visibilité : {(isVisible ? "affiché" : "masqué")}");
            }
        }
        else
        {
            togglePressed = false;
        }
    }

    // =========================================================
    // 🔹 Active/désactive tout le HUD
    // =========================================================
    void SetHUDVisibility(bool visible)
    {
        if (hudGroup == null) return;

        hudGroup.alpha = visible ? 1f : 0f;
        hudGroup.interactable = visible;
        hudGroup.blocksRaycasts = visible;
    }
}
