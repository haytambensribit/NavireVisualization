using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class DownloadVideoButton : MonoBehaviour
{
    public Button downloadButton;

    void Start()
    {
        downloadButton.onClick.AddListener(OnDownloadClicked);
    }

    void OnDownloadClicked()
    {
        string txt = Application.persistentDataPath + "/last_video.txt";

        if (!File.Exists(txt))
        {
            Debug.LogError("❌ Aucun enregistrement trouvé !");
            return;
        }

        string video = File.ReadAllText(txt);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(video);
#else
        string dest = Path.Combine(Application.persistentDataPath, Path.GetFileName(video));
        File.Copy(video, dest, true);
#endif
    }
}
