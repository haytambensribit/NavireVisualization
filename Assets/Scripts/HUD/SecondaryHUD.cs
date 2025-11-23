using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // pour clavier moderne

public class SecondaryHUD : MonoBehaviour
{
    [Header("Références")]
    public ShipCSVPlayer shipData;
    public TMP_Text hudText;
    public CanvasGroup hudGroup;

    [Header("Options")]
    public bool enableToggle = true;

    private bool isVisible = true;
    private bool togglePressed = false;

    // =========================================================
    // 🔹 Formatage intelligent des forces
    // =========================================================
    string FormatForce(float value)
    {
        float abs = Mathf.Abs(value);

        if (abs < 1e3f)         return $"{value:F0} N";        // Newton
        if (abs < 1e6f)         return $"{value / 1e3f:F1} kN";  // kilo Newton
        if (abs < 1e9f)         return $"{value / 1e6f:F2} MN";  // méga Newton
        if (abs < 1e12f)        return $"{value / 1e9f:F2} GN";  // giga Newton

        return $"{value / 1e12f:F2} TN";                          // téra Newton
    }

    void Update()
    {
        if (enableToggle)
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

        hudText.text =
            $"<b><color=#000000>Forces détaillées</color></b>\n" +
            $"<b>Gravité :</b> Fx={FormatForce(f.fx_grav)}   Fy={FormatForce(f.fy_grav)}   Fz={FormatForce(f.fz_grav)}\n" +
            $"<b>Hydrostatique :</b> Fx={FormatForce(f.fx_hydro)}   Fy={FormatForce(f.fy_hydro)}   Fz={FormatForce(f.fz_hydro)}\n" +
            $"<b>Froude-Krylov :</b> Fx={FormatForce(f.fx_froude)}   Fy={FormatForce(f.fy_froude)}   Fz={FormatForce(f.fz_froude)}\n" +
            $"<b>Diffraction :</b> Fx={FormatForce(f.fx_diff)}   Fy={FormatForce(f.fy_diff)}   Fz={FormatForce(f.fz_diff)}\n" +
            $"<b>Radiation :</b> Fx={FormatForce(f.fx_rad)}   Fy={FormatForce(f.fy_rad)}   Fz={FormatForce(f.fz_rad)}\n" +
            $"<b>Holtrop-Mennen :</b> Fx={FormatForce(f.fx_hm)}   Fy={FormatForce(f.fy_hm)}   Fz={FormatForce(f.fz_hm)}\n" +
            $"<b>Propulseur & Gouvernail :</b> Fx={FormatForce(f.fx_prop)}   Fy={FormatForce(f.fy_prop)}   Fz={FormatForce(f.fz_prop)}";
    }

    // =========================================================
    // 🔹 Raccourci clavier Ctrl + J
    // =========================================================
    void HandleHUDToggle()
    {
        bool ctrlPressed =
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            (Keyboard.current != null &&
             (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed));

        bool jPressed =
            Input.GetKey(KeyCode.J) ||
            (Keyboard.current != null && Keyboard.current.jKey.isPressed);

        if (ctrlPressed && jPressed)
        {
            if (!togglePressed)
            {
                togglePressed = true;
                isVisible = !isVisible;
                Debug.Log($"🔁 SecondaryHUD visibilité : {(isVisible ? "affiché" : "masqué")}");
            }
        }
        else
        {
            togglePressed = false;
        }
    }

    // =========================================================
    // 🔹 Active / désactive tout le HUD
    // =========================================================
    void SetHUDVisibility(bool visible)
    {
        if (hudGroup == null) return;

        hudGroup.alpha = visible ? 1f : 0f;
        hudGroup.interactable = visible;
        hudGroup.blocksRaycasts = visible;
    }
}
