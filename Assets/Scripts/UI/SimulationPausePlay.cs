using UnityEngine;
using UnityEngine.UI;

public class SimulationPausePlay : MonoBehaviour
{
    public Button buttonPause;
    public Button buttonPlay;
    public ShipCSVPlayer player;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("SimulationPausePlay : ShipCSVPlayer non assigné !");
            return;
        }

        // UI initiale selon l'état de la simu
        UpdateUI(player.IsPlaying);

        // Les deux boutons font la même chose : toggler la simu
        buttonPause.onClick.AddListener(OnToggleClicked);
        buttonPlay.onClick.AddListener(OnToggleClicked);
    }

    void Update()
    {
        if (player == null) return;

        // Ici on ne gère PAS le clavier.
        // On fait juste suivre l'UI à l'état réel.
        bool isPlaying = player.IsPlaying;

        // Si l'UI ne correspond pas, on la corrige
        if (buttonPause.gameObject.activeSelf != isPlaying)
        {
            UpdateUI(isPlaying);
        }
    }

    // ====== CALLBACK UNIQUE POUR LES DEUX BOUTONS ======

    void OnToggleClicked()
    {
        if (player == null) return;

        // On demande au player de changer d'état
        player.TogglePlayPause();

        // Puis on aligne l'UI sur le nouvel état
        UpdateUI(player.IsPlaying);
    }

    // ====== HELPER UI ======

    void UpdateUI(bool isPlaying)
    {
        if (buttonPause != null) buttonPause.gameObject.SetActive(isPlaying);
        if (buttonPlay  != null) buttonPlay.gameObject.SetActive(!isPlaying);
    }
}
