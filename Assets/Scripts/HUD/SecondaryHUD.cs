using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // ✅ pour la compatibilité clavier moderne

public class SecondaryHUD : MonoBehaviour
{
    [Header("Références")]
    public ShipCSVPlayer shipData;
    public TMP_Text hudText;
    public CanvasGroup hudGroup; // ✅ le Panel parent

    [Header("Options")]
    [Tooltip("Permet de masquer/afficher le HUD secondaire avec Ctrl+J")]
    public bool enableToggle = true;

    private bool isVisible = true;
    private bool togglePressed = false;

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
            $"<b><color=#000000>Forces détaillées [N]</color></b>\n" +
            $"<b>Gravité :</b> Fx={f.fx_grav:F1}   Fy={f.fy_grav:F1}   Fz={f.fz_grav:F1}\n" +
            $"<b>Hydrostatique :</b> Fx={f.fx_hydro:F1}   Fy={f.fy_hydro:F1}   Fz={f.fz_hydro:F1}\n" +
            $"<b>Froude-Krylov :</b> Fx={f.fx_froude:F1}   Fy={f.fy_froude:F1}   Fz={f.fz_froude:F1}\n" +
            $"<b>Diffraction :</b> Fx={f.fx_diff:F1}   Fy={f.fy_diff:F1}   Fz={f.fz_diff:F1}\n" +
            $"<b>Radiation :</b> Fx={f.fx_rad:F1}   Fy={f.fy_rad:F1}   Fz={f.fz_rad:F1}\n" +
            $"<b>Holtrop-Mennen :</b> Fx={f.fx_hm:F1}   Fy={f.fy_hm:F1}   Fz={f.fz_hm:F1}\n" +
            $"<b>Propulseur & Gouvernail :</b> Fx={f.fx_prop:F1}   Fy={f.fy_prop:F1}   Fz={f.fz_prop:F1}";
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
