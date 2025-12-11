using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gère la lecture des données de simulation CSV et déplace le navire en conséquence.
/// Lit également la configuration initiale à partir d'un fichier YAML.
/// </summary>
public class ShipCSVPlayer : MonoBehaviour
{
    [Header("Paramètres CSV")]
    /// <summary>
    /// Nom du fichier CSV situé dans le dossier StreamingAssets.
    /// </summary>
    public string csvFileName = "";

    /// <summary>
    /// Vitesse de lecture de la simulation (1.0 = temps réel).
    /// </summary>
    public float playbackSpeed = 1.0f;

    /// <summary>
    /// Si vrai, la simulation boucle indéfiniment.
    /// </summary>
    public bool loop = true;

    [Header("Paramètres YAML")]
    /// <summary>
    /// Nom du fichier YAML de configuration situé dans StreamingAssets.
    /// </summary>
    public string yamlFileName = "";

    /// <summary>
    /// Position de l'hélice dans le repère du navire (lue depuis le YAML).
    /// </summary>
    public Vector3 propellerPosition_ship { get; private set; } = Vector3.zero;

    [Header("Paramètres du Transform du Navire")]
    /// <summary>
    /// Échelle appliquée à la position lue depuis le CSV.
    /// </summary>
    public float positionScale = 1.0f;

    /// <summary>
    /// Décalage ajouté à la position finale du navire dans Unity.
    /// </summary>
    public Vector3 positionOffset = Vector3.zero;

    /// <summary>
    /// Décalage ajouté à la rotation finale du navire.
    /// </summary>
    public Vector3 rotationOffset = Vector3.zero;
    
    // Position et rotation initiales dans le repère NED (North-East-Down)
    private Vector3 initialPos_NED;
    private Vector3 initialRot_ship_NED;

    // Position et rotation initiales converties dans le repère Unity
    private Quaternion shipFrameInitialRotationUnity;
    private Vector3 shipFrameInitialPositionUnity;

    /// <summary>
    /// Structure contenant toutes les données d'un pas de temps de simulation.
    /// </summary>
    public class FrameData
    {
        public float time;
        public Vector3 position;
        public Vector3 rotation;

        // Vitesses instantanées
        public float u, v, w;

        // Forces totales
        public float totalFx, totalFy, totalFz;
        public float totalMx, totalMy, totalMz;

        // Forces secondaires
        public float fx_grav, fy_grav, fz_grav;
        public float fx_hydro, fy_hydro, fz_hydro;
        public float fx_froude, fy_froude, fz_froude;
        public float fx_diff, fy_diff, fz_diff;
        public float fx_rad, fy_rad, fz_rad;
        public float fx_hm, fy_hm, fz_hm;
        public float fx_prop, fy_prop, fz_prop;
    }

    /// <summary>
    /// La trame de données actuelle interpolée.
    /// </summary>
    public FrameData CurrentFrame { get; private set; }

    /// <summary>
    /// La trame de données précédente (utile pour les comparaisons).
    /// </summary>
    public FrameData PreviousFrame { get; private set; }

    /// <summary>
    /// Indique si une trame valide est actuellement chargée.
    /// </summary>
    public bool HasValidFrame => CurrentFrame != null;

    private List<FrameData> data = new();
    private int currentIndex = 0;
    private float elapsedTime = 0f;
    private bool isPlaying = true;

    /// <summary>
    /// Chemin complet du fichier CSV chargé.
    /// </summary>
    public string LoadedCSVPath { get; private set; }

    
    void Start()
    {
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(csvFileName))
        {
            csvFileName = EditorUtility.OpenFilePanel("Sélectionner un fichier CSV", Application.streamingAssetsPath, "csv");

            if (string.IsNullOrEmpty(csvFileName))
            {
                Debug.LogError("❌ Aucun fichier CSV sélectionné.");
                isPlaying = false;
                return;
            }
        }
#else
        if (string.IsNullOrWhiteSpace(csvFileName))
        {
            Debug.LogError("❌ Aucun fichier CSV défini.");
            isPlaying = false;
            return;
        }
#endif
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(yamlFileName))
        {
            yamlFileName = EditorUtility.OpenFilePanel("Sélectionner un fichier YAML", Application.streamingAssetsPath, "yml");

            if (string.IsNullOrEmpty(yamlFileName))
            {
                Debug.LogError("❌ Aucun fichier YAML sélectionné.");
                return;
            }
        }
#endif
        LoadYAML();
        LoadCSV();
    }

    /// <summary>
    /// Charge et parse le fichier de configuration YAML.
    /// </summary>
    void LoadYAML()
    {
        if (string.IsNullOrWhiteSpace(yamlFileName)) return;

        string full = yamlFileName;
        if (!Path.IsPathRooted(full))
            full = Path.Combine(Application.streamingAssetsPath, full);
        full = Path.GetFullPath(full);

        if (!File.Exists(full))
        {
            Debug.LogError("❌ YAML introuvable : " + full);
            return;
        }

        string txt = File.ReadAllText(full);

        // Extraction de la position de l'hélice et conversion vers le repère Unity (Y <-> Z inversé)
        propellerPosition_ship = new Vector3(
            ExtractYamlFloat(txt, "position of propeller frame", "x"),
            -ExtractYamlFloat(txt, "position of propeller frame", "z"), 
            ExtractYamlFloat(txt, "position of propeller frame", "y")
        );

        initialPos_NED = new Vector3(
            ExtractYamlFloat(txt, "initial position of body frame", "x"),
            ExtractYamlFloat(txt, "initial position of body frame", "y"),
            ExtractYamlFloat(txt, "initial position of body frame", "z")
        );

        initialRot_ship_NED = new Vector3(
            ExtractYamlFloat(txt, "initial position of body frame", "phi"),
            ExtractYamlFloat(txt, "initial position of body frame", "theta"),
            ExtractYamlFloat(txt, "initial position of body frame", "psi")
        );

        // Conversion de la position initiale NED vers Unity
        shipFrameInitialPositionUnity = new Vector3(
            initialPos_NED.y,
            -initialPos_NED.z,
            initialPos_NED.x
        );

        shipFrameInitialRotationUnity = ConvertNEDToUnityRot(initialRot_ship_NED);

        Debug.Log($"📌 Position de l'hélice (repère navire) = {propellerPosition_ship}");
    }

    /// <summary>
    /// Extrait une valeur flottante d'une section spécifique du YAML.
    /// </summary>
    float ExtractYamlFloat(string yaml, string section, string key)
    {
        string pattern =
            section + @"[\s\S]*?" +
            key + @"\:\s*\{value:\s*([-0-9\.eE]+)";

        var match = System.Text.RegularExpressions.Regex.Match(yaml, pattern);
        if (!match.Success) return 0f;

        return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Convertit une rotation du repère NED (North-East-Down) vers le repère Unity.
    /// </summary>
    /// <param name="rNED">Rotation en radians (phi, theta, psi).</param>
    /// <returns>Quaternion Unity correspondant.</returns>
    Quaternion ConvertNEDToUnityRot(Vector3 rNED)
    {
        float phi = rNED.x * Mathf.Rad2Deg;    // roll
        float theta = rNED.y * Mathf.Rad2Deg;  // pitch
        float psi = rNED.z * Mathf.Rad2Deg;    // yaw

        Quaternion q = Quaternion.Euler(phi, psi, theta);

        // Conversion NED->Unity (Xned,Yned,Zned -> Z,X,-Y)
        return new Quaternion(
            q.y,
            -q.z,
            q.x,
            q.w
        );
    }

    void Update()
    {
        if (!isPlaying || data.Count < 2) 
            return;

        elapsedTime += Time.deltaTime * playbackSpeed;

        // Avance dans les données jusqu'à trouver l'intervalle de temps correct
        while (currentIndex < data.Count - 2 && elapsedTime > data[currentIndex + 1].time)
            currentIndex++;

        if (currentIndex >= data.Count - 1)
        {
            if (loop)
            {
                currentIndex = 0;
                elapsedTime = 0f;
            }
            else
            {
                isPlaying = false;
                CurrentFrame = data[^1];
                PreviousFrame = CurrentFrame;
                return;
            }
        }
        
        var a = data[currentIndex];
        var b = data[currentIndex + 1];
        float t = Mathf.InverseLerp(a.time, b.time, elapsedTime);

        PreviousFrame = CurrentFrame;
        CurrentFrame = LerpFrame(a, b, t);

        Vector3 csvPos = CurrentFrame.position;
        Quaternion csvRot = Quaternion.Euler(CurrentFrame.rotation);

        // Application de la transformation au GameObject
        Vector3 posUnity = shipFrameInitialPositionUnity + shipFrameInitialRotationUnity * csvPos;
        Quaternion rotUnity = shipFrameInitialRotationUnity * csvRot;

        transform.position = positionOffset + posUnity * positionScale;
        transform.rotation = rotUnity;
    }

    /// <summary>
    /// Interpole linéairement entre deux trames de données.
    /// </summary>
    FrameData LerpFrame(FrameData a, FrameData b, float t)
    {
        FrameData f = new FrameData
        {
            time = Mathf.Lerp(a.time, b.time, t),
            position = Vector3.Lerp(a.position, b.position, t),

            // Correction YAW -> conversion en degrés si nécessaire
            rotation = new Vector3(
                Mathf.Lerp(a.rotation.x, b.rotation.x, t),
                Mathf.Lerp(a.rotation.y, b.rotation.y, t),
                Mathf.Lerp(a.rotation.z, b.rotation.z, t)
            ),

            u = Mathf.Lerp(a.u, b.u, t),
            v = Mathf.Lerp(a.v, b.v, t),
            w = Mathf.Lerp(a.w, b.w, t),

            totalFx = Mathf.Lerp(a.totalFx, b.totalFx, t),
            totalFy = Mathf.Lerp(a.totalFy, b.totalFy, t),
            totalFz = Mathf.Lerp(a.totalFz, b.totalFz, t),

            totalMx = Mathf.Lerp(a.totalMx, b.totalMx, t),
            totalMy = Mathf.Lerp(a.totalMy, b.totalMy, t),
            totalMz = Mathf.Lerp(a.totalMz, b.totalMz, t),

            fx_grav = Mathf.Lerp(a.fx_grav, b.fx_grav, t),
            fy_grav = Mathf.Lerp(a.fy_grav, b.fy_grav, t),
            fz_grav = Mathf.Lerp(a.fz_grav, b.fz_grav, t),

            fx_hydro = Mathf.Lerp(a.fx_hydro, b.fx_hydro, t),
            fy_hydro = Mathf.Lerp(a.fy_hydro, b.fy_hydro, t),
            fz_hydro = Mathf.Lerp(a.fz_hydro, b.fz_hydro, t),

            fx_froude = Mathf.Lerp(a.fx_froude, b.fx_froude, t),
            fy_froude = Mathf.Lerp(a.fy_froude, b.fy_froude, t),
            fz_froude = Mathf.Lerp(a.fz_froude, b.fz_froude, t),

            fx_diff = Mathf.Lerp(a.fx_diff, b.fx_diff, t),
            fy_diff = Mathf.Lerp(a.fy_diff, b.fy_diff, t),
            fz_diff = Mathf.Lerp(a.fz_diff, b.fz_diff, t),

            fx_rad = Mathf.Lerp(a.fx_rad, b.fx_rad, t),
            fy_rad = Mathf.Lerp(a.fy_rad, b.fy_rad, t),
            fz_rad = Mathf.Lerp(a.fz_rad, b.fz_rad, t),

            fx_hm = Mathf.Lerp(a.fx_hm, b.fx_hm, t),
            fy_hm = Mathf.Lerp(a.fy_hm, b.fy_hm, t),
            fz_hm = Mathf.Lerp(a.fz_hm, b.fz_hm, t),

            fx_prop = Mathf.Lerp(a.fx_prop, b.fx_prop, t),
            fy_prop = Mathf.Lerp(a.fy_prop, b.fy_prop, t),
            fz_prop = Mathf.Lerp(a.fz_prop, b.fz_prop, t),
        };

        return f;
    }

    /// <summary>
    /// Charge et parse le fichier CSV.
    /// </summary>
    void LoadCSV()
    {
        string fullPath = csvFileName;

        if (!Path.IsPathRooted(fullPath))
            fullPath = Path.Combine(Application.streamingAssetsPath, fullPath);

        fullPath = Path.GetFullPath(fullPath);
        LoadedCSVPath = fullPath;

        if (!File.Exists(fullPath))
        {
            Debug.LogError("❌ Fichier introuvable : " + fullPath);
            isPlaying = false;
            return;
        }

        string[] lines = File.ReadAllLines(fullPath);

        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV vide.");
            isPlaying = false;
            return;
        }

        string[] headers = lines[0].Split(',');
        Dictionary<string, int> h = new();

        for (int i = 0; i < headers.Length; i++)
            h[headers[i].Trim().ToLower()] = i;

        data.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] p = line.Split(',');

            FrameData f = new FrameData();
            try
            {
                f.time = TryParse(h, p, "t");

                f.position = new Vector3(
                    TryParse(h, p, "x(ship)"),
                    - TryParse(h, p, "z(ship)"),
                    TryParse(h, p, "y(ship)")
                );

                float phi = TryParse(h, p, "phi(ship)");
                float theta = TryParse(h, p, "theta(ship)");
                float psiRaw = TryParse(h, p, "psi(ship)");

                // psi est en radians cumulatifs -> on normalise en degrés
                float psi = psiRaw * Mathf.Rad2Deg;
                
                f.rotation = new Vector3(phi, psi, theta);

                f.u = TryParse(h, p, "u(ship)");
                f.v = TryParse(h, p, "v(ship)");
                f.w = TryParse(h, p, "w(ship)");

                f.totalFx = TryParse(h, p, "fx(sum of forces ship ship)");
                f.totalFy = TryParse(h, p, "fy(sum of forces ship ship)");
                f.totalFz = TryParse(h, p, "fz(sum of forces ship ship)");

                f.totalMx = TryParse(h, p, "mx(sum of forces ship ship)");
                f.totalMy = TryParse(h, p, "my(sum of forces ship ship)");
                f.totalMz = TryParse(h, p, "mz(sum of forces ship ship)");

                f.fx_grav = TryParse(h, p, "fx(gravity ship ship)");
                f.fy_grav = TryParse(h, p, "fy(gravity ship ship)");
                f.fz_grav = TryParse(h, p, "fz(gravity ship ship)");

                f.fx_hydro = TryParse(h, p, "fx(non-linear hydrostatic (fast) ship ship)");
                f.fy_hydro = TryParse(h, p, "fy(non-linear hydrostatic (fast) ship ship)");
                f.fz_hydro = TryParse(h, p, "fz(non-linear hydrostatic (fast) ship ship)");

                f.fx_froude = TryParse(h, p, "fx(non-linear froude-krylov ship ship)");
                f.fy_froude = TryParse(h, p, "fy(non-linear froude-krylov ship ship)");
                f.fz_froude = TryParse(h, p, "fz(non-linear froude-krylov ship ship)");

                f.fx_diff = TryParse(h, p, "fx(diffraction ship ship)");
                f.fy_diff = TryParse(h, p, "fy(diffraction ship ship)");
                f.fz_diff = TryParse(h, p, "fz(diffraction ship ship)");

                f.fx_rad = TryParse(h, p, "fx(radiation damping ship ship)");
                f.fy_rad = TryParse(h, p, "fy(radiation damping ship ship)");
                f.fz_rad = TryParse(h, p, "fz(radiation damping ship ship)");

                f.fx_hm = TryParse(h, p, "fx(holtrop & mennen ship ship)");
                f.fy_hm = TryParse(h, p, "fy(holtrop & mennen ship ship)");
                f.fz_hm = TryParse(h, p, "fz(holtrop & mennen ship ship)");

                f.fx_prop = TryParse(h, p, "fx(propellerandrudder ship propellerandrudder)");
                f.fy_prop = TryParse(h, p, "fy(propellerandrudder ship propellerandrudder)");
                f.fz_prop = TryParse(h, p, "fz(propellerandrudder ship propellerandrudder)");
            }
            catch
            {
                Debug.LogWarning($"⚠️ Ligne {i + 1} ignorée (valeurs invalides)");
                continue;
            }

            data.Add(f);
        }

        Debug.Log($"✅ {data.Count} lignes chargées depuis : {LoadedCSVPath}");
        Debug.Log($"ℹ️ Colonnes reconnues : {h.Count}");
    }

    /// <summary>
    /// Essaie de parser une valeur float à partir d'une colonne CSV donnée.
    /// </summary>
    float TryParse(Dictionary<string, int> h, string[] p, string key)
    {
        key = key.ToLower();
        if (!h.ContainsKey(key)) return 0f;

        int index = h[key];
        if (index < 0 || index >= p.Length) return 0f;

        string raw = p[index].Trim();
        if (string.IsNullOrWhiteSpace(raw)) return 0f;

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
            return val;

        return 0f;
    }

    /// <summary>
    /// Retourne le temps écoulé dans la simulation.
    /// </summary>
    public float GetElapsedTime() => elapsedTime;

    /// <summary>
    /// Retourne le temps de la dernière frame de données disponible.
    /// </summary>
    public float GetLastFrameTime() => data.Count > 0 ? data[^1].time : 0f;

    /// <summary>
    /// Définit le temps actuel de la simulation (pour le seeking).
    /// </summary>
    public void SetElapsedTime(float value)
    {
        if (data.Count < 2) return;

        elapsedTime = Mathf.Clamp(value, 0f, data[^1].time);
        currentIndex = 0;

        while (currentIndex < data.Count - 2 && elapsedTime > data[currentIndex + 1].time)
            currentIndex++;

        var a = data[currentIndex];
        var b = data[currentIndex + 1];
        float t = Mathf.InverseLerp(a.time, b.time, elapsedTime);
        CurrentFrame = LerpFrame(a, b, t);
        PreviousFrame = CurrentFrame;

        transform.position = positionOffset + CurrentFrame.position * positionScale;
        transform.rotation = Quaternion.Euler(CurrentFrame.rotation + rotationOffset);
    }
}
