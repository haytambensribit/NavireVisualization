using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class ShipTrajectoryPlayer : MonoBehaviour
{
    [Header("CSV File Settings")]
    public string csvFileName = "KVLCC2xCN-AeroModels.csv";
    public bool useStreamingAssets = true;

    [Header("Simulation")]
    public float playbackSpeed = 1f;  // 1 = temps réel
    public bool loop = true;

    private List<ShipFrame> frames = new();
    private int currentFrame = 0;
    private float timeAccumulator = 0f;
    private float frameInterval = 0.01f; // 10 ms entre mesures

    private bool isPlaying = true;

    void Start()
    {
        LoadCSV();
    }

    void Update()
    {
        if (frames.Count == 0 || !isPlaying) return;

        timeAccumulator += Time.deltaTime * playbackSpeed;

        while (timeAccumulator >= frameInterval)
        {
            timeAccumulator -= frameInterval;
            currentFrame++;

            if (currentFrame >= frames.Count)
            {
                if (loop) currentFrame = 0;
                else { isPlaying = false; return; }
            }

            ApplyFrame(frames[currentFrame]);
        }
    }

    void LoadCSV()
    {
        frames.Clear();
        string path = useStreamingAssets
            ? Path.Combine(Application.streamingAssetsPath, csvFileName)
            : Path.Combine(Application.dataPath, csvFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"❌ CSV file not found at {path}");
            return;
        }

        string[] lines = File.ReadAllLines(path);
        Debug.Log($"✅ Loaded {lines.Length} lines from CSV.");

        CultureInfo ci = CultureInfo.InvariantCulture;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            if (parts.Length < 7) continue; // skip malformed lines

            try
            {
                // ⚠️ Adapter les index selon ton fichier
                float time = float.Parse(parts[0], ci);
                float x = float.Parse(parts[1], ci);
                float y = float.Parse(parts[2], ci);
                float z = float.Parse(parts[3], ci);
                float roll = float.Parse(parts[4], ci);
                float pitch = float.Parse(parts[5], ci);
                float yaw = float.Parse(parts[6], ci);

                frames.Add(new ShipFrame(time, new Vector3(x, y, z), new Vector3(roll, pitch, yaw)));
            }
            catch { }
        }

        Debug.Log($"📊 Parsed {frames.Count} valid frames.");
    }

    void ApplyFrame(ShipFrame f)
    {
        transform.localPosition = f.position;
        transform.localRotation = Quaternion.Euler(f.rotation);
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;
    public void Stop() { isPlaying = false; currentFrame = 0; }
}

[System.Serializable]
public class ShipFrame
{
    public float time;
    public Vector3 position;
    public Vector3 rotation;

    public ShipFrame(float t, Vector3 pos, Vector3 rot)
    {
        time = t;
        position = pos;
        rotation = rot;
    }
}
