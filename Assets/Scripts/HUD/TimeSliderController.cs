using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimeSliderController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Références")]
    public ShipCSVPlayer player;
    public Slider timeSlider;

    [Header("Options")]
    public bool isInteractive = true;

    private bool userDragging = false;
    private bool initialized = false;
    private float maxTime = 0f;
    private float previousTimeScale = 1f;
    
    [Header("Play / Pause")]
    public Button playPauseBtn;

    private bool isPaused = false;
    public bool IsDragging => userDragging;
    
    
    void Start()
    {
        if (playPauseBtn != null)
            playPauseBtn.onClick.AddListener(TogglePlayPause);
    }


    void Update()
    {
        if (player == null || timeSlider == null) return;

        // 🔵 Initialisation différée (CSV chargé)
        if (!initialized && player.GetLastFrameTime() > 0f)
        {
            maxTime = player.GetLastFrameTime();
            timeSlider.minValue = 0f;
            timeSlider.maxValue = maxTime;
            initialized = true;
        }

        if (!isInteractive || !initialized) return;

        // 🔵 Update automatique si pas en drag
        if (!userDragging)
        {
            timeSlider.value = player.GetElapsedTime();
        }
        else
        {
            // 🟠 En mode drag, on met à jour manuellement
            player.SetElapsedTime(timeSlider.value);
        }
         // --- Pause/reprise avec ESPACE ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }
    }

    // ============================================================
    //                    GESTION DU DRAG
    // ============================================================
    public void TogglePlayPause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Debug.Log("⏸ Pause activée");
        }
        else
        {
            Time.timeScale = previousTimeScale != 0 ? previousTimeScale : 1f;
            Debug.Log("▶ Lecture reprise");
        }
    }



    public void OnPointerDown(PointerEventData eventData)
    {
        if (!initialized) return;

        userDragging = true;

        // 🟥 Freeze total de la scène
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Debug.Log("⏸ Scene FREEZED (drag)");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!initialized) return;

        userDragging = false;

        // 🟩 Défreeze
        Time.timeScale = previousTimeScale;

        // force la position finale
        player.SetElapsedTime(timeSlider.value);

        Debug.Log("▶ Scene UNFREEZED");
    }
}
