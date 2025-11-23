using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class ForceVisualizer : MonoBehaviour
{
    [Header("CSV Settings")]
    [Tooltip("Nom du fichier CSV contenant les forces (doit être dans StreamingAssets)")]
        public float playbackSpeed = 1f;
    public Transform shipTransform;
    public ShipCSVPlayer player;
    
    [Header("Force Scaling (automatique par axe)")]
    [Tooltip("Valeurs de référence par axe (max absolu dans le CSV)")]
    public Vector3 Fref = Vector3.one;
    [Tooltip("Facteur d’échelle global appliqué après normalisation")]
    public float globalScale = 1f;

    [Header("Fluidité")]
    [Range(0f, 1f)] public float smoothFactor = 1f;
    private Vector3 smoothedForce = Vector3.zero;

    [Header("Flèche Settings")]
    public float shaftRadius = 0.3f;
    public float fixedHeadLength = 1.5f;
    public float headRadiusFactor = 1.0f;
    public float forceThreshold = 0.05f;

    [Header("Scaling manuel")]
    public float Scaling = 10f;

    private Arrow3D arrowProp;
    
    private class Arrow3D
    {
        public GameObject root, shaft, head;
        private Material mat;

        public Arrow3D(string name, Color color, Transform parent, float shaftR, float headR)
        {
            root = new GameObject(name);
            root.transform.SetParent(parent, false);

            // Corps (cylindre)
            shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.transform.SetParent(root.transform, false);
            Object.Destroy(shaft.GetComponent<Collider>());

            // Tête (cône)
            head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            var mf = head.AddComponent<MeshFilter>();
            mf.sharedMesh = ConeMesh(24);
            var mr = head.AddComponent<MeshRenderer>();

            mat = new Material(Shader.Find("Standard"))
            {
                color = color,
                enableInstancing = true
            };
            shaft.GetComponent<Renderer>().material = mat;
            mr.material = mat;
        }

        static Mesh ConeMesh(int seg)
        {
            Mesh m = new Mesh();
            var v = new List<Vector3>();
            var t = new List<int>();
            v.Add(Vector3.up);
            for (int i = 0; i < seg; i++)
                v.Add(new Vector3(Mathf.Cos(i * 2 * Mathf.PI / seg), 0, Mathf.Sin(i * 2 * Mathf.PI / seg)));
            for (int i = 0; i < seg; i++)
            {
                t.Add(0); t.Add(1 + ((i + 1) % seg)); t.Add(1 + i);
            }
            int baseCenter = v.Count;
            v.Add(Vector3.zero);
            for (int i = 0; i < seg; i++)
            {
                int i1 = 1 + i, i2 = 1 + ((i + 1) % seg);
                t.Add(baseCenter); t.Add(i2); t.Add(i1);
            }
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return m;
        }

        public void Set(Vector3 origin, Vector3 dir, float shaftR, float headLen, float headRadFactor, float threshold)
        {
            float L = dir.magnitude;
            if (L < threshold) { root.SetActive(false); return; }

            root.SetActive(true);
            Vector3 n = dir.normalized;
            root.transform.position = origin;
            root.transform.rotation = Quaternion.FromToRotation(Vector3.up, n);

            // Ajustement des longueurs
            float shaftLen = Mathf.Max(L - headLen, 1e-3f);
            shaft.transform.localScale = new Vector3(shaftR * 2f, shaftLen * 0.5f, shaftR * 2f);
            shaft.transform.localPosition = new Vector3(0f, shaftLen * 0.5f, 0f);

            // Cône collé au cylindre
            head.transform.localScale = new Vector3(headRadFactor * shaftR * 2f, headLen, headRadFactor * shaftR * 2f);
            head.transform.localPosition = new Vector3(0f, shaftLen, 0f);
        }

        public void SetActive(bool visible)
        {
            if (root) root.SetActive(visible);
        }
    }

    private List<Vector4> data = new();
    private Arrow3D arrowFx, arrowFy, arrowFz;
    private bool visible = true;

    void Start()
    {
        arrowFx = new Arrow3D("Arrow_Fx", new Color(1f, 0.3f, 0f), transform, shaftRadius, shaftRadius * headRadiusFactor); // orange (Fx)
        arrowFy = new Arrow3D("Arrow_Fy", Color.green, transform, shaftRadius, shaftRadius * headRadiusFactor);            // vert (Fy)
        arrowFz = new Arrow3D("Arrow_Fz", Color.magenta, transform, shaftRadius, shaftRadius * headRadiusFactor);          // magenta (Fz)
        arrowProp = new Arrow3D("Arrow_Propeller", Color.cyan, transform, shaftRadius, shaftRadius * headRadiusFactor);
        Invoke(nameof(LoadCSV), 0.2f);

    }

    void Update()
    {
        HandleVisibilityToggle();
        if (!visible || data.Count == 0 || shipTransform == null || player == null) return;



        int index = playerIndex();
        Vector4 f = data[index];
        
        // Normalisation par Fref
        float FxNorm = (Mathf.Abs(Fref.x) > 1e-6f) ? f.y / Fref.x * Scaling : f.y;
        float FyNorm = (Mathf.Abs(Fref.y) > 1e-6f) ? f.z / Fref.y * Scaling : f.z;
        float FzNorm = (Mathf.Abs(Fref.z) > 1e-6f) ? f.w / Fref.z * Scaling : f.w;

        Vector3 propWorld = shipTransform.TransformPoint(player.propellerPosition_ship);


        // Calcul du vecteur force
        Vector3 targetForce = new Vector3(FxNorm, FyNorm, FzNorm);
        
        float FxPropNorm = (Mathf.Abs(Fref.x) > 1e-6f) ? player.CurrentFrame.fx_prop / Fref.x * Scaling : player.CurrentFrame.fx_prop;
        float FyPropNorm = (Mathf.Abs(Fref.y) > 1e-6f) ? player.CurrentFrame.fy_prop / Fref.y * Scaling : player.CurrentFrame.fy_prop;
        float FzPropNorm = (Mathf.Abs(Fref.z) > 1e-6f) ? player.CurrentFrame.fz_prop / Fref.z * Scaling : player.CurrentFrame.fz_prop;


        Vector3 Fprop = 
            -shipTransform.right * FxPropNorm * globalScale
            +  shipTransform.forward * FyPropNorm * globalScale
            + -shipTransform.up * FzPropNorm * globalScale;

        // Lissage visuel
        float k = Mathf.Lerp(1f, 0.02f, smoothFactor);
        smoothedForce = Vector3.Lerp(smoothedForce, targetForce, k);
        // On utilise la force lissée
        float FxS = smoothedForce.x;
        float FyS = smoothedForce.y;
        float FzS = smoothedForce.z;

        Vector3 origin = shipTransform.position;
        Vector3 Fx = - shipTransform.right * FxS * globalScale;
        Vector3 Fy = shipTransform.forward * FyS * globalScale;  
        Vector3 Fz = -shipTransform.up * FzS * globalScale;


        arrowFx.Set(origin, Fx, shaftRadius, fixedHeadLength, headRadiusFactor, forceThreshold);
        arrowFy.Set(origin, Fy, shaftRadius, fixedHeadLength, headRadiusFactor, forceThreshold);
        arrowFz.Set(origin, Fz, shaftRadius, fixedHeadLength, headRadiusFactor, forceThreshold);
        arrowProp.Set(propWorld, Fprop, shaftRadius, fixedHeadLength, headRadiusFactor, forceThreshold);

    }
    

    int playerIndex()
    {
        float target = player.GetElapsedTime();
        if (float.IsNaN(target)) return 0;

        float minDiff = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < data.Count; i++)
        {
            float diff = Mathf.Abs(data[i].x - target);
            if (diff < minDiff)
            {
                minDiff = diff;
                bestIndex = i;
            }
        }

        return bestIndex;
    }


    // fallback (aucun player connecté)
    int defaultIndex()
    {
        return Mathf.FloorToInt(Time.time * playbackSpeed) % data.Count;
    }


    void HandleVisibilityToggle()
    {
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Comma))
        {
            visible = !visible;
            arrowFx.SetActive(visible);
            arrowFy.SetActive(visible);
            arrowFz.SetActive(visible);
            Debug.Log(visible ? "🟢 Forces visibles" : "🔴 Forces masquées");
        }
    }

    void LoadCSV()
    {
        if (player == null)
        {
            Debug.LogError("❌ ForceVisualizer : aucun ShipCSVPlayer assigné !");
            return;
        }

        string path = player.LoadedCSVPath;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogError("❌ ForceVisualizer : CSV introuvable → " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV vide.");
            return;
        }

        string[] headers = lines[0].Split(',');
        int tCol = -1, fxCol = -1, fyCol = -1, fzCol = -1;

        for (int i = 0; i < headers.Length; i++)
        {
            string h = headers[i].Trim().ToLower();

            if (h == "t" || h.Contains("time")) tCol = i;

            if (h.Contains("fx")) fxCol = i;
            if (h.Contains("fy")) fyCol = i;
            if (h.Contains("fz")) fzCol = i;
        }

        data.Clear();
        float fxMax = 0, fyMax = 0, fzMax = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] p = lines[i].Split(',');

            float t = SafeParse(p, tCol);
            float fx = SafeParse(p, fxCol);
            float fy = SafeParse(p, fyCol);
            float fz = SafeParse(p, fzCol);

            fxMax = Mathf.Max(fxMax, Mathf.Abs(fx));
            fyMax = Mathf.Max(fyMax, Mathf.Abs(fy));
            fzMax = Mathf.Max(fzMax, Mathf.Abs(fz));

            data.Add(new Vector4(t, fx, fy, fz));
        }

        Fref = new Vector3(fxMax, fyMax, fzMax);

        Debug.Log($"📥 ForceVisualizer : {data.Count} lignes chargées depuis {path}");
    }

    float SafeParse(string[] row, int index)
    {
        if (index < 0 || index >= row.Length) return 0f;
        string val = row[index].Trim();
        if (string.IsNullOrEmpty(val)) return 0f;
        if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;
        return 0f;
    }
}
