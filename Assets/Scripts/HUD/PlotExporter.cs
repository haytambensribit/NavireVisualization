using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.IO.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlotExporter : MonoBehaviour
{
    [Header("Toggles Efforts")]
    public Toggle Fx_T;
    public Toggle Fy_T;
    public Toggle Fz_T;
    public Toggle Mx_T;
    public Toggle My_T;
    public Toggle Mz_T;

    [Header("Boutons")]
    public Button generateBtn;
    public Button downloadBtn;

    [Header("Référence CSV du Player")]
    public ShipCSVPlayer player;

    private string csvPath = "";
    private Dictionary<string, List<float>> forceData = new();
    private List<float> time = new();

    void Start()
    {
        generateBtn.onClick.AddListener(GenerateGraphs);
        downloadBtn.onClick.AddListener(DownloadZip);

        
    }

    // ------------------ CHARGEMENT CSV ---------------------
    void LoadCSV()
    {
        csvPath = player != null ? player.LoadedCSVPath : "";

        if (string.IsNullOrEmpty(csvPath))
        {
            Debug.LogError("❌ PlotExporter : LoadedCSVPath est vide !");
            return;
        }

        forceData.Clear();
        time.Clear();

        if (!File.Exists(csvPath))
        {
            Debug.LogError("❌ Impossible de charger le CSV : chemin invalide → " + csvPath);
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV vide ou corrompu.");
            return;
        }

        // Lecture flexible des colonnes
        string[] header = lines[0].Split(',');

        int t = FindColumn(header, "t");
        int fx = FindColumn(header, "fx");
        int fy = FindColumn(header, "fy");
        int fz = FindColumn(header, "fz");
        int mx = FindColumn(header, "mx");
        int my = FindColumn(header, "my");
        int mz = FindColumn(header, "mz");

        // On initialise les listes
        forceData["Fx"] = new();
        forceData["Fy"] = new();
        forceData["Fz"] = new();
        forceData["Mx"] = new();
        forceData["My"] = new();
        forceData["Mz"] = new();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] p = lines[i].Split(',');
            if (p.Length < header.Length) continue;

            float tt = SafeParse(p, t);

            // Évite les doublons (très courant dans les CSV CFD)
            if (time.Count > 0 && Mathf.Approximately(tt, time[time.Count - 1]))
                continue;

            time.Add(tt);
            forceData["Fx"].Add(SafeParse(p, fx));
            forceData["Fy"].Add(SafeParse(p, fy));
            forceData["Fz"].Add(SafeParse(p, fz));
            forceData["Mx"].Add(SafeParse(p, mx));
            forceData["My"].Add(SafeParse(p, my));
            forceData["Mz"].Add(SafeParse(p, mz));
        }

        Debug.Log($"📥 CSV chargé : {time.Count} points lus.");
    }

    // Trouve une colonne via recherche flexible
    int FindColumn(string[] header, string key)
    {
        key = key.ToLower();
        for (int i = 0; i < header.Length; i++)
        {
            string h = header[i].Trim().ToLower();
            if (h.StartsWith(key))
                return i;
        }
        Debug.LogWarning("⚠ Colonne non trouvée : " + key);
        return -1; // Non bloquant
    }

    float SafeParse(string[] row, int index)
    {
        if (index < 0 || index >= row.Length) return 0f;
        string v = row[index].Trim();
        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        return 0f;
    }

    // ----------------- GÉNÉRATION DES FICHIERS ---------------------
    void GenerateGraphs()
    {
        LoadCSV();

        if (time.Count == 0)
        {
            Debug.LogError("❌ Aucune donnée chargée. Abandon.");
            return;
        }

        List<string> selected = new();

        if (Fx_T.isOn) selected.Add("Fx");
        if (Fy_T.isOn) selected.Add("Fy");
        if (Fz_T.isOn) selected.Add("Fz");
        if (Mx_T.isOn) selected.Add("Mx");
        if (My_T.isOn) selected.Add("My");
        if (Mz_T.isOn) selected.Add("Mz");

        if (selected.Count == 0)
        {
            Debug.LogWarning("❌ Aucun effort sélectionné.");
            return;
        }

        if (Directory.Exists("GraphExport"))
            Directory.Delete("GraphExport", true);

        Directory.CreateDirectory("GraphExport");

        foreach (string s in selected)
        {
            Texture2D plot = DrawPlot(time, forceData[s], s);
            byte[] png = plot.EncodeToPNG();
            File.WriteAllBytes($"GraphExport/{s}.png", png);
        }

        Debug.Log("📈 Graphiques PNG générés !");
    }

    // ----------------- ZIP AVEC CHOIX EMPLACEMENT ---------------------
    void DownloadZip()
    {
        GenerateGraphs();

        string tempZip = Application.temporaryCachePath + "/export.zip";

        if (File.Exists(tempZip))
            File.Delete(tempZip);

        ZipFile.CreateFromDirectory("GraphExport", tempZip);

#if UNITY_EDITOR
        string savePath = EditorUtility.SaveFilePanel(
            "Enregistrer ZIP",
            "",
            "ForcesExport.zip",
            "zip"
        );

        if (savePath == "") return;

        File.Copy(tempZip, savePath, true);
#else
        Debug.LogWarning("📦 Pour standalone, utiliser StandaloneFileBrowser.");
#endif

        Debug.Log("📦 ZIP exporté avec succès !");
    }




    Texture2D DrawPlot(List<float> time, List<float> values, string title)
    {
        int W = 1200;
        int H = 700;

        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);

        // Fond blanc
        Color[] fill = new Color[W * H];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
        tex.SetPixels(fill);

        // Détection min/max
        float minX = time[0];
        float maxX = time[time.Count - 1];

        float minY = Mathf.Min(values.ToArray());
        float maxY = Mathf.Max(values.ToArray());

        float margin = 80f;

        // Dessine les axes
        DrawLine(tex, new Vector2(margin, margin), new Vector2(margin, H - margin), Color.black);
        DrawLine(tex, new Vector2(margin, margin), new Vector2(W - margin, margin), Color.black);

        // Petits ticks Y
        for (int i = 0; i < 6; i++)
        {
            float y = Mathf.Lerp(minY, maxY, i / 5f);
            float py = Mathf.Lerp(margin, H - margin, (y - minY) / (maxY - minY));
            DrawLine(tex, new Vector2(margin - 10, py), new Vector2(margin + 10, py), Color.black);
        }

        // Courbe
        for (int i = 1; i < time.Count; i++)
        {
            float x1 = Mathf.Lerp(margin, W - margin, (time[i - 1] - minX) / (maxX - minX));
            float y1 = Mathf.Lerp(margin, H - margin, (values[i - 1] - minY) / (maxY - minY));

            float x2 = Mathf.Lerp(margin, W - margin, (time[i] - minX) / (maxX - minX));
            float y2 = Mathf.Lerp(margin, H - margin, (values[i] - minY) / (maxY - minY));

            DrawLine(tex, new Vector2(x1, y1), new Vector2(x2, y2), Color.blue);
        }

        // Titre
        DrawText(tex, title + " (Y) — Temps (X)", 20, H - 40, Color.black);

        tex.Apply();
        return tex;
    }

void DrawLine(Texture2D tex, Vector2 a, Vector2 b, Color c)
{
    int x0 = (int)a.x;
    int y0 = (int)a.y;
    int x1 = (int)b.x;
    int y1 = (int)b.y;

    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);

    int sx = x0 < x1 ? 1 : -1;
    int sy = y0 < y1 ? 1 : -1;

    int err = dx - dy;

    while (true)
    {
        tex.SetPixel(x0, y0, c);

        if (x0 == x1 && y0 == y1) break;
        int e2 = 2 * err;

        if (e2 > -dy) { err -= dy; x0 += sx; }
        if (e2 < dx) { err += dx; y0 += sy; }
    }
}

void DrawText(Texture2D tex, string text, int x, int y, Color c)
{
    // Version simplifiée : un petit marqueur
    // (si tu veux du vrai texte, je peux te générer un système Bitmap Font)
    foreach (char ch in text)
    {
        tex.SetPixel(x, y, c);
        x += 8;
    }
}

}
