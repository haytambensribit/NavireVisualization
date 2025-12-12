using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Composant principal gérant la lecture des données de simulation (CSV) et l'animation du navire.
/// Il charge également la configuration statique du navire depuis un fichier YAML.
/// </summary>
public class ShipCSVPlayer : MonoBehaviour
{
    [Header("CSV Settings")]
    /// <summary>
    /// Nom du fichier CSV par défaut (utilisé si aucun fichier n'est sélectionné via le menu).
    /// </summary>
    public string csvFileName = "";

    /// <summary>
    /// Vitesse de lecture de la simulation (1.0 = temps réel).
    /// </summary>
    public float playbackSpeed = 1.0f;

    /// <summary>
    /// Si vrai, la simulation redémarre automatiquement à la fin.
    /// </summary>
    public bool loop = true;

    [Header("YAML Settings")]
    /// <summary>
    /// Nom du fichier YAML de configuration du navire.
    /// </summary>
    public string yamlFileName = "";

    /// <summary>
    /// Position de l'hélice lue depuis le YAML (dans le repère du navire).
    /// </summary>
    public Vector3 propellerPosition_ship { get; private set; } = Vector3.zero;


    [Header("Ship Transform Settings")]
    /// <summary>
    /// Facteur d'échelle appliqué aux positions lues du CSV.
    /// </summary>
    public float positionScale = 1.0f;

    /// <summary>
    /// Décalage de position appliqué au navire dans la scène Unity.
    /// </summary>
    public Vector3 positionOffset = Vector3.zero;

    /// <summary>
    /// Décalage de rotation appliqué au navire (en degrés, Euler).
    /// </summary>
    public Vector3 rotationOffset = Vector3.zero;
    
    private Vector3 initialPos_NED;
    private Vector3 initialRot_ship_NED;

    private Quaternion shipFrameInitialRotationUnity;
    private Vector3 shipFrameInitialPositionUnity;


    // ======================================================
    /// <summary>
    /// Structure de données représentant une ligne du fichier CSV (un pas de temps).
    /// Contient toutes les données cinématiques et dynamiques du navire.
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

        // Forces secondaires (Composantes détaillées)
        /// <summary>Force de gravité</summary>
        public float fx_grav, fy_grav, fz_grav;
        /// <summary>Force hydrostatique non-linéaire</summary>
        public float fx_hydro, fy_hydro, fz_hydro;
        /// <summary>Force de Froude-Krylov non-linéaire</summary>
        public float fx_froude, fy_froude, fz_froude;
        /// <summary>Force de diffraction</summary>
        public float fx_diff, fy_diff, fz_diff;
        /// <summary>Amortissement de radiation</summary>
        public float fx_rad, fy_rad, fz_rad;
        /// <summary>Force de Holtrop & Mennen (résistance)</summary>
        public float fx_hm, fy_hm, fz_hm;
        /// <summary>Force de l'hélice et du gouvernail</summary>
        public float fx_prop, fy_prop, fz_prop;
    }

    /// <summary>
    /// Données interpolées pour la frame courante affichée à l'écran.
    /// </summary>
    public FrameData CurrentFrame { get; private set; }

    /// <summary>
    /// Données de la frame précédente (pour calculs différentiels si besoin).
    /// </summary>
    public FrameData PreviousFrame { get; private set; }

    /// <summary>
    /// Indique si une frame valide est chargée.
    /// </summary>
    public bool HasValidFrame => CurrentFrame != null;

    private List<FrameData> data = new();
    private int currentIndex = 0;
    private float elapsedTime = 0f;
    private bool isPlaying = true;
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// Chemin absolu du fichier CSV chargé.
    /// </summary>
    public string LoadedCSVPath { get; private set; }

    // ======================================================
    void Start()
    {
        // Vérification des chemins transmis par le menu principal (SimulationPaths)
        if (string.IsNullOrEmpty(SimulationPaths.SelectedCSV) ||
            string.IsNullOrEmpty(SimulationPaths.SelectedYAML))
        {
            Debug.LogError("❌ Aucun chemin transmis depuis le menu de démarrage.");
            isPlaying = false;
            return;
        }

        csvFileName = SimulationPaths.SelectedCSV;
        yamlFileName = SimulationPaths.SelectedYAML;

        LoadYAML();
        LoadCSV();
        Debug.Log($"📂 CSV chargé depuis le menu : {csvFileName}");
        Debug.Log($"📂 YAML chargé depuis le menu : {yamlFileName}");
    }


    // ======================================================
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

        // Extraction de la position de l'hélice et conversion de repère (Y Unity = -Z NED, Z Unity = Y NED)
        propellerPosition_ship = new Vector3(
            ExtractYamlFloat(txt, "position of propeller frame", "x"),
            -ExtractYamlFloat(txt, "position of propeller frame", "z"),   // ← devient Y Unity
            ExtractYamlFloat(txt, "position of propeller frame", "y")    // ← devient Z Unity
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

        shipFrameInitialPositionUnity = new Vector3(
            initialPos_NED.y,
            -initialPos_NED.z,
            initialPos_NED.x
        );

        shipFrameInitialRotationUnity = ConvertNEDToUnityRot(initialRot_ship_NED);


        Debug.Log($"📌 Propeller position (ship frame) = {propellerPosition_ship}");
    }

    /// <summary>
    /// Extrait une valeur flottante d'une section spécifique du YAML via Regex.
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
    /// Convertit une rotation Euler du repère NED vers le repère Unity.
    /// </summary>
    Quaternion ConvertNEDToUnityRot(Vector3 rNED)
    {
        float phi = rNED.x * Mathf.Rad2Deg;    // roll
        float theta = rNED.y * Mathf.Rad2Deg;  // pitch
        float psi = rNED.z * Mathf.Rad2Deg;    // yaw

        Quaternion q = Quaternion.Euler(phi, psi, theta);

        // Conversion NED->Unity (Xned,Yned,Zned->Z,X,-Y)
        return new Quaternion(
            q.y,
            -q.z,
            q.x,
            q.w
        );
    }

    // ========== PLAY / PAUSE / RESTART API ==========

    /// <summary>
    /// Met en pause la simulation et fige le temps de la scène.
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
        Time.timeScale = 0f;   // ← fige toute la scène
    }

    /// <summary>
    /// Reprend la lecture de la simulation.
    /// </summary>
    public void Play()
    {
        if (data.Count < 2) return;

        float lastTime = data[^1].time;

        if (elapsedTime >= lastTime - 0.01f)
            Restart();
        else
            isPlaying = true;

        Time.timeScale = 1f;  // ← relance la scène
    }

    public void TogglePlayPause()
    {
        if (isPlaying)
            Pause();
        else
            Play();
    }

    /// <summary>
    /// Redémarre la simulation depuis le début (t=0).
    /// </summary>
    public void Restart()
    {
        elapsedTime = 0f;
        currentIndex = 0;

        if (data.Count > 0)
        {
            CurrentFrame = data[0];
            PreviousFrame = CurrentFrame;
        }

        isPlaying = true;
        playbackSpeed = 1f;

        Vector3 csvPos = CurrentFrame.position;
        Quaternion csvRot = Quaternion.Euler(CurrentFrame.rotation);

        Vector3 posUnity = shipFrameInitialPositionUnity + shipFrameInitialRotationUnity * csvPos;
        Quaternion rotUnity = shipFrameInitialRotationUnity * csvRot;

        transform.position = positionOffset + posUnity * positionScale;
        transform.rotation = rotUnity;
    }


    void Update()
    {
        // ✅ Le clavier est géré ICI (toujours au même endroit)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

            
        if (!isPlaying || data.Count < 2) 
            return;

        elapsedTime += Time.deltaTime * playbackSpeed;

        // Avance dans les données pour trouver l'intervalle correspondant au temps écoulé
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
                // On reste à la fin, mais on ne bloque plus le Play()
                isPlaying = false;
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

        Vector3 posUnity = shipFrameInitialPositionUnity + shipFrameInitialRotationUnity * csvPos;
        Quaternion rotUnity = shipFrameInitialRotationUnity * csvRot;

        transform.position = positionOffset + posUnity * positionScale;
        transform.rotation = rotUnity;
    }

    // ======================================================
    /// <summary>
    /// Interpole linéairement entre deux trames de données.
    /// </summary>
    FrameData LerpFrame(FrameData a, FrameData b, float t)
    {
        FrameData f = new FrameData
        {
            time = Mathf.Lerp(a.time, b.time, t),
            position = Vector3.Lerp(a.position, b.position, t),

            // 🔥 Correction YAW → conversion en degrés si nécessaire
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

    // ======================================================
    /// <summary>
    /// Lit et parse le fichier CSV de données.
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

                // 🔥 psi est en radians cumulatifs → on normalise en degrés
                float psi = psiRaw * Mathf.Rad2Deg;
                

                f.rotation = new Vector3(phi, psi, theta);

                // vitesses
                f.u = TryParse(h, p, "u(ship)");
                f.v = TryParse(h, p, "v(ship)");
                f.w = TryParse(h, p, "w(ship)");

                // forces
                f.totalFx = TryParse(h, p, "fx(sum of forces ship ship)");
                f.totalFy = TryParse(h, p, "fy(sum of forces ship ship)");
                f.totalFz = TryParse(h, p, "fz(sum of forces ship ship)");

                f.totalMx = TryParse(h, p, "mx(sum of forces ship ship)");
                f.totalMy = TryParse(h, p, "my(sum of forces ship ship)");
                f.totalMz = TryParse(h, p, "mz(sum of forces ship ship)");

                // autres forces
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

    // ======================================================
    /// <summary>
    /// Tente de parser une valeur float depuis une colonne CSV donnée.
    /// Retourne 0 si la colonne n'existe pas ou si la valeur est invalide.
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

    // ======================================================
    public float GetElapsedTime() => elapsedTime;
    public float GetLastFrameTime() => data.Count > 0 ? data[^1].time : 0f;

    /// <summary>
    /// Définit le temps écoulé actuel (Seek).
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
