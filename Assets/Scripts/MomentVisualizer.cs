using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;   // ✅ Nouveau système d’input

[RequireComponent(typeof(Transform))]
public class MomentVisualizer : MonoBehaviour
{
    [Header("CSV Settings")]
    public float playbackSpeed = 1.0f;
    public bool loop = true;
    public ShipCSVPlayer player;


    [Header("Références de moments (calculées automatiquement)")]
    public Vector3 Mref = Vector3.one;

    [Header("Paramètres visuels")]
    public float baseRadius = 7f;
    [Range(0f, 1f)] public float smoothFactor = 0.15f;
    public float tubeRadius = 0.25f;
    public int arcSegments = 60;
    public int tubeSegments = 16;
    public float arrowHeadLength = 1.2f;
    public float arrowHeadRadius = 0.35f;

    [Header("Couleurs fixes par axe")]
    public Color rollColor = Color.red;
    public Color yawColor = Color.green;
    public Color pitchColor = Color.yellow;

    [Header("Valeurs courantes (lecture seule)")]
    [SerializeField] private Vector3 currentMoments;

    private Vector3 smoothedMoments = Vector3.zero;
    private GameObject[] arcMeshes = new GameObject[3];
    private GameObject[] arrowHeads = new GameObject[3];
    private GameObject[] referenceDiscs = new GameObject[3];
    private readonly string[] axisNames = { "Roll (X)", "Yaw (Y)", "Pitch (Z)" };

    private class MomentFrame { public float t; public Vector3 m; }
    private List<MomentFrame> frames = new();
    private int currentIndex = 0;
    private float elapsed = 0f;
    private bool playing = true;

    private int visibilityState = 0; // 0 = tout, 1 = arcs, 2 = rien

    public TimeSliderController slider;
    // =========================================================
    void Awake()
    {
        // === Arcs toriques et flèches ===
        for (int i = 0; i < 3; i++)
        {
            GameObject arc = new GameObject("Arc3D_" + axisNames[i]);
            arc.transform.SetParent(transform, false);

            var mf = arc.AddComponent<MeshFilter>();
            var mr = arc.AddComponent<MeshRenderer>();

            Color c = (i == 0) ? rollColor : (i == 1) ? yawColor : pitchColor;
            var mat = new Material(Shader.Find("Standard"))
            {
                color = c,
                enableInstancing = true
            };
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mr.material = mat;

            mf.mesh = CreateArcTubeMesh(baseRadius, tubeRadius, 180f, arcSegments, tubeSegments);

            switch (i)
            {
                case 0: arc.transform.localRotation = Quaternion.Euler(0, 90, 90); break;
                case 1: arc.transform.localRotation = Quaternion.identity; break;
                default: arc.transform.localRotation = Quaternion.Euler(90, 0, 0); break;
            }

            arcMeshes[i] = arc;

            GameObject head = new GameObject("ArrowHead_" + axisNames[i]);
            head.transform.SetParent(arc.transform, false);
            var hmf = head.AddComponent<MeshFilter>();
            var hmr = head.AddComponent<MeshRenderer>();
            hmf.sharedMesh = CreateConeMesh(24);
            hmr.sharedMaterial = mr.material;
            head.transform.localScale = new Vector3(arrowHeadRadius, arrowHeadLength, arrowHeadRadius);
            arrowHeads[i] = head;
        }

        // === Disques de référence ===
        for (int i = 0; i < 3; i++)
        {
            GameObject ring = new GameObject("ReferenceRing_" + axisNames[i]);
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.zero;

            var mf = ring.AddComponent<MeshFilter>();
            var mr = ring.AddComponent<MeshRenderer>();
            mf.mesh = CreateRingMesh(0.9f * baseRadius, baseRadius, 80);

            Color baseColor = (i == 0) ? rollColor : (i == 1) ? yawColor : pitchColor;
            baseColor.a = 0.25f;

            var mat = new Material(Shader.Find("Standard"))
            {
                color = baseColor,
                enableInstancing = true
            };
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mr.material = mat;

            switch (i)
            {
                case 0: ring.transform.localRotation = Quaternion.Euler(0, 0, 90); break;
                case 1: ring.transform.localRotation = Quaternion.identity; break;
                default: ring.transform.localRotation = Quaternion.Euler(90, 0, 0); break;
            }

            referenceDiscs[i] = ring;
        }

        Invoke(nameof(LoadCSV), 0.2f);
    }

    // =========================================================
    void Update()
    {
        HandleVisibilityToggle();

        if (frames.Count < 2) return;

        // ⛔ Mode lecture via ShipCSVPlayer (slider actif)
        if (player != null)
        {
            if (!player.HasValidFrame) return;

            float target = player.GetElapsedTime();
            UpdateMomentsFromTime(target);
            return;
        }

        // 🎞 Lecture automatique normale
        elapsed += Time.deltaTime * playbackSpeed;

        while (currentIndex < frames.Count - 2 && elapsed > frames[currentIndex + 1].t)
            currentIndex++;

        if (currentIndex >= frames.Count - 1)
        {
            if (loop)
            {
                currentIndex = 0;
                elapsed = 0f;
            }
            else
            {
                playing = false;
                return;
            }
        }

        var a = frames[currentIndex];
        var b = frames[currentIndex + 1];
        float t = Mathf.InverseLerp(a.t, b.t, elapsed);

        // 🟢 Moments interpolés
        Vector3 moments = Vector3.Lerp(a.m, b.m, t);

        // 🌊 Smoothing fluide
        float k = Mathf.Lerp(1f, 0.02f, smoothFactor);
        smoothedMoments = Vector3.Lerp(smoothedMoments, moments, k);
        currentMoments = smoothedMoments;

        // 🔁 Mise à jour visuelle
        UpdateArc(0, smoothedMoments.x, rollColor);
        UpdateArc(1, smoothedMoments.y, pitchColor);
        UpdateArc(2, smoothedMoments.z, yawColor);
    }





    void UpdateMomentsFromTime(float targetTime)
    {
        // Trouver la frame la plus proche
        float minDiff = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < frames.Count; i++)
        {
            float diff = Mathf.Abs(frames[i].t - targetTime);
            if (diff < minDiff)
            {
                minDiff = diff;
                bestIndex = i;
            }
        }

        // Moments correspondants
        Vector3 rawMoment = frames[bestIndex].m;

        // Lissage (comme avant)
        float k = Mathf.Lerp(1f, 0.02f, smoothFactor);
        smoothedMoments = Vector3.Lerp(smoothedMoments, rawMoment, k);
        currentMoments = smoothedMoments;

        // Mise à jour des arcs
        UpdateArc(0, smoothedMoments.x, rollColor);
        UpdateArc(1, smoothedMoments.y, pitchColor);
        UpdateArc(2, smoothedMoments.z, yawColor);
    }



    // =========================================================
    void HandleVisibilityToggle()
    {
        // 🔹 Lecture du caractère réel (ancien système, respecte AZERTY)
        if (!string.IsNullOrEmpty(Input.inputString))
        {
            if (Input.inputString.ToLower().Contains("m"))
            {
                CycleVisibility();
                return;
            }
        }

        // 🔹 Fallback (nouveau système) — si le projet est configuré en "Both"
        if (Keyboard.current != null)
        {
            if ((Keyboard.current.mKey != null && Keyboard.current.mKey.wasPressedThisFrame) ||
                (Keyboard.current.commaKey != null && Keyboard.current.commaKey.wasPressedThisFrame))
            {
                CycleVisibility();
                return;
            }
        }
    }

    // Sépare la logique pour plus de clarté
    void CycleVisibility()
    {
        visibilityState = (visibilityState + 1) % 3;

        bool showArcs = (visibilityState == 0 || visibilityState == 1);
        bool showRings = (visibilityState == 0);

        foreach (var arc in arcMeshes) if (arc != null) arc.SetActive(showArcs);
        foreach (var head in arrowHeads) if (head != null) head.SetActive(showArcs);
        foreach (var ring in referenceDiscs) if (ring != null) ring.SetActive(showRings);

        string stateName = visibilityState switch
        {
            0 => "Moments + Disques visibles",
            1 => "Seulement moments",
            2 => "Tout masqué",
            _ => ""
        };

        Debug.Log($"🔁 [M] État d'affichage : {stateName}");
    }


    // =========================================================
    private void UpdateArc(int index, float value, Color color)
    {
        GameObject arc = arcMeshes[index];
        GameObject head = arrowHeads[index];
        if (arc == null || head == null) return;

        float refVal = Mathf.Max(1e-6f, Mref.x);
        float normalized = Mathf.Clamp01(Mathf.Abs(value) / refVal);
        float newAngle = 180f * normalized;
        float direction = Mathf.Sign(value);

        var mf = arc.GetComponent<MeshFilter>();
        mf.mesh = CreateArcTubeMesh(baseRadius, tubeRadius, newAngle, arcSegments, tubeSegments, direction);
        arc.GetComponent<Renderer>().material.color = color;

        float endAngle = Mathf.Deg2Rad * newAngle * direction;
        Vector3 endCenter = new Vector3(
            Mathf.Cos(endAngle) * baseRadius,
            Mathf.Sin(endAngle) * baseRadius,
            0f
        );
        Vector3 tangent = new Vector3(
            -Mathf.Sin(endAngle),
            Mathf.Cos(endAngle),
            0f
        ) * Mathf.Sign(direction);

        head.transform.localPosition = endCenter;
        head.transform.localRotation = Quaternion.FromToRotation(Vector3.up, tangent);
        tangent = tangent.sqrMagnitude > 0f ? tangent.normalized : Vector3.right;

        head.transform.localPosition = endCenter;
        head.transform.localRotation = Quaternion.FromToRotation(Vector3.up, tangent);
    }

    // =========================================================
    void LoadCSV()
    {
        if (player == null)
        {
            Debug.LogError("❌ MomentVisualizer : aucun ShipCSVPlayer assigné !");
            return;
        }

        string path = player.LoadedCSVPath;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogError("❌ MomentVisualizer : CSV introuvable → " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV vide !");
            return;
        }

        string[] headers = lines[0].Split(',');
        Dictionary<string, int> h = new();

        for (int i = 0; i < headers.Length; i++)
            h[headers[i].Trim().ToLower()] = i;

        string[] keys =
        {
            "t",
            "mx(sum of forces ship ship)",
            "my(sum of forces ship ship)",
            "mz(sum of forces ship ship)"
        };

        foreach (string k in keys)
            if (!h.ContainsKey(k.ToLower()))
            {
                Debug.LogError("❌ Colonne manquante : " + k);
                return;
            }

        frames.Clear();
        float mxMax = 0f, myMax = 0f, mzMax = 0f;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var p = lines[i].Split(',');

            float t = float.Parse(p[h["t"]], CultureInfo.InvariantCulture);
            float mx = float.Parse(p[h["mx(sum of forces ship ship)"]], CultureInfo.InvariantCulture);
            float my = float.Parse(p[h["my(sum of forces ship ship)"]], CultureInfo.InvariantCulture);
            float mz = -float.Parse(p[h["mz(sum of forces ship ship)"]], CultureInfo.InvariantCulture);

            frames.Add(new MomentFrame
            {
                t = t,
                m = new Vector3(mx, my, mz)
            });

            mxMax = Mathf.Max(mxMax, Mathf.Abs(mx));
            myMax = Mathf.Max(myMax, Mathf.Abs(my));
            mzMax = Mathf.Max(mzMax, Mathf.Abs(mz));
        }

        float globalMax = Mathf.Max(mxMax, Mathf.Max(myMax, mzMax));
        Mref = new Vector3(globalMax, globalMax, globalMax);

        Debug.Log($"📥 MomentVisualizer : {frames.Count} lignes chargées depuis {path}");
    }


    // =========================================================
    private Mesh CreateConeMesh(int seg)
    {
        Mesh m = new Mesh();
        var v = new List<Vector3>();
        var t = new List<int>();
        v.Add(Vector3.up);
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
        }
        for (int i = 0; i < seg; i++)
        {
            int i1 = 1 + i;
            int i2 = 1 + ((i + 1) % seg);
            t.Add(0); t.Add(i2); t.Add(i1);
        }
        int baseCenter = v.Count;
        v.Add(Vector3.zero);
        for (int i = 0; i < seg; i++)
        {
            int i1 = 1 + i;
            int i2 = 1 + ((i + 1) % seg);
            t.Add(baseCenter); t.Add(i1); t.Add(i2);
        }
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        return m;
    }

    private Mesh CreateRingMesh(float innerRadius, float outerRadius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new();
        List<int> triangles = new();

        float step = 2f * Mathf.PI / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * step;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices.Add(new Vector3(x * outerRadius, 0f, z * outerRadius));
            vertices.Add(new Vector3(x * innerRadius, 0f, z * innerRadius));

            if (i < segments)
            {
                int start = i * 2;
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 3);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
        }

        int offset = vertices.Count;
        for (int i = 0; i < offset; i++) vertices.Add(vertices[i]);
        for (int i = 0; i < segments; i++)
        {
            int start = offset + i * 2;
            triangles.Add(start + 2);
            triangles.Add(start + 3);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh CreateArcTubeMesh(float radius, float thick, float arcAngle, int arcSeg, int tubeSeg, float direction = 1f)
    {
        Mesh mesh = new Mesh();
        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var t = new List<int>();

        float arcStep = Mathf.Deg2Rad * (arcAngle / arcSeg) * direction;
        float tubeStep = 2f * Mathf.PI / tubeSeg;

        for (int i = 0; i <= arcSeg; i++)
        {
            float a = i * arcStep;
            Vector3 center = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);

            for (int j = 0; j <= tubeSeg; j++)
            {
                // ✅ la variable manquante :
                float tt = j * tubeStep;

                Vector3 normal = new Vector3(
                    Mathf.Cos(a) * Mathf.Cos(tt),
                    Mathf.Sin(a) * Mathf.Cos(tt),
                    Mathf.Sin(tt)
                );

                Vector3 vert = center + normal * thick;
                v.Add(vert);
                n.Add((vert - center).normalized);
            }
        }

        int perRing = tubeSeg + 1;
        for (int i = 0; i < arcSeg; i++)
        {
            for (int j = 0; j < tubeSeg; j++)
            {
                int a = i * perRing + j;
                int b = (i + 1) * perRing + j;
                int c = a + 1;
                int d = b + 1;
                t.Add(a); t.Add(b); t.Add(c);
                t.Add(c); t.Add(b); t.Add(d);
            }
        }

        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    public Vector3 GetCurrentMoments() => currentMoments;
}
