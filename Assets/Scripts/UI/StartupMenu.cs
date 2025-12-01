using UnityEngine;
using UnityEngine.SceneManagement;
using SFB; // StandaloneFileBrowser
using System.IO;
public class StartupMenu : MonoBehaviour
{
    public void OnRunSimulation()
    {
        // 1) Sélection du CSV
        var csv = StandaloneFileBrowser.OpenFilePanel("Choisir un CSV", "", "csv", false);
        if (csv.Length == 0) return;
        SimulationPaths.SelectedCSV = csv[0];

        // 2) Sélection du YAML
        var yaml = StandaloneFileBrowser.OpenFilePanel("Choisir un YAML", "", "yml", false);
        if (yaml.Length == 0) return;
        SimulationPaths.SelectedYAML = yaml[0];

        // 3) Charger la scène Simulation
        SceneManager.LoadScene("SimulationScene");
    }

    public void OnOpenRecordings()
    {
        SceneManager.LoadScene("VideosScene");
    }
}
