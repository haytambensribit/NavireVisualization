using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDShortcuts : MonoBehaviour
{
    public GameObject panel;          // HUDShortcutsPanel
    public Button toggleButton;       // ToggleButton
    public TextMeshProUGUI shortcutsText;

    private bool isVisible = false;

    void Start()
    {
        // Texte affiché dans le HUD
        shortcutsText.text =
            "<b></b>\n" +
            "C - Switch Camera\n" +
            "1 - Chase Camera\n" +
            "2 - Top Camera\n" +
            "3 - Right Camera\n" +
            "4 - Left Camera\n" +
            "5 - Top Camera\n" +
            "6 - Bottom Camera\n" +
            "Space - Pause/Resume Simulation\n" +
            "CTRL + H - Toggle Main HUD\n" +
            "CTRL + J - Toggle Secondary HUD\n" +
            "F - Toggle Force Display\n" +
            "M - Toggle Moment Display\n" +
            "O - Toggle Ship Opacity\n";

        panel.SetActive(isVisible);

        toggleButton.onClick.AddListener(ToggleHUD);
    }

    void Update()
    {
        // FERMER / OUVRIR AVEC F1 PAR EXEMPLE
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleHUD();
        }
    }

    void ToggleHUD()
    {
        isVisible = !isVisible;
        panel.SetActive(isVisible);
    }
}
