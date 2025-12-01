using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour
{
    public void GoToMainMenu()
    {
        // Remplace par le nom exact de ta scène
        SceneManager.LoadScene("StartupMenu");
    }
}
