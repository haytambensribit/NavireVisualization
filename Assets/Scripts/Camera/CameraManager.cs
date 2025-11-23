using UnityEngine;
using UnityEngine.InputSystem; // Nouveau système d'entrée

public class CameraManager : MonoBehaviour
{
    [Header("Liste des caméras disponibles")]
    [Tooltip("Place ici toutes tes caméras dans l'ordre (0 → 5)")]
    public Camera[] cameras;

    [Header("Options de debug")]
    public bool showDebug = true;

    private int currentCamIndex = 0;

    void Start()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogError("❌ Aucune caméra assignée dans CameraManager !");
            return;
        }

        // Désactive toutes les caméras sauf la première
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].gameObject.SetActive(i == 0);

        currentCamIndex = 0;

        if (showDebug)
            Debug.Log($"🎥 Caméra active : {cameras[currentCamIndex].name}");
    }

    void Update()
    {
        if (Keyboard.current == null) return; // sécurité pour Input System

        // 🔄 Basculer entre caméras avec la touche C
        if (Keyboard.current.cKey.wasPressedThisFrame)
            SwitchCamera();

        // 🎯 Sélection directe avec les chiffres 1 → 6
        if (Keyboard.current.digit1Key.wasPressedThisFrame) ActivateCamera(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) ActivateCamera(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ActivateCamera(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) ActivateCamera(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) ActivateCamera(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) ActivateCamera(5);
    }

    // 🔄 Passe à la caméra suivante (boucle)
    void SwitchCamera()
    {
        if (cameras.Length == 0) return;

        cameras[currentCamIndex].gameObject.SetActive(false);
        currentCamIndex = (currentCamIndex + 1) % cameras.Length;
        cameras[currentCamIndex].gameObject.SetActive(true);

        if (showDebug)
            Debug.Log($"🎬 Caméra changée : {cameras[currentCamIndex].name}");
    }

    // 🎯 Active une caméra précise
    void ActivateCamera(int index)
    {
        if (index < 0 || index >= cameras.Length) return;

        for (int i = 0; i < cameras.Length; i++)
            cameras[i].gameObject.SetActive(i == index);

        currentCamIndex = index;

        if (showDebug)
            Debug.Log($"🎯 Caméra activée : {cameras[currentCamIndex].name}");
    }
}
