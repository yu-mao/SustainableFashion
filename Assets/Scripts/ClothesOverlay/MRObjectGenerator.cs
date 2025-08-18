using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BodyPix;

public class MRObjectGenerator_Debug : MonoBehaviour
{
    [Header("BodyPix & View Mapping")]
    public BodyPixDriver driver;
    public RectTransform hudCanvas;
    public RectTransform videoRect;      // SAME full-screen rect you used for overlay/tie
    public Camera canvasCamera;          // CenterEyeAnchor

    [Header("Keypoint coordinates (match your overlay)")]
    public bool coordsAreNormalized = true;
    public bool mirrorX = false;
    public bool flipY = false;
    public Vector2 normScale = Vector2.one;     // e.g. (1.02, 0.98)
    public Vector2 normOffset = Vector2.zero;   // e.g. (0.00, -0.01)

    [Header("Follow & Quality")]
    [Range(0, 1)] public float minScore = 0.25f;
    public bool followEveryFrame = true;
    public float followSmoothRate = 15f;

    [Header("Rules")]
    public SpawnRule[] rules;

    [Header("Events")]
    public UnityEvent<GameObject> OnAnyGenerated;

    [Header("Debug")]
    public bool verbose = true;
    [Tooltip("Throttle verbose logs to once every N frames")]
    public int logEveryNFrames = 30;

    // ---------- Types ----------
    [Serializable] public enum AssetMode { UI2D, World3D }
    [Serializable] public enum RotationMode { None, AlongToOtherKeypoint, PerpendicularToOtherKeypoint }

    [Serializable]
    public class SpawnRule
    {
        public string label = "Item";

        [Header("Target Keypoint")]
        public Body.KeypointID keypoint = Body.KeypointID.Nose;

        [Header("Asset Type")]
        public AssetMode mode = AssetMode.UI2D;

        [Header("UI (2D) Settings")]
        public Sprite uiSprite;
        public Vector2 uiSize = new Vector2(100, 100);
        public Vector2 uiPivot = new Vector2(0.5f, 0.5f);
        public RectTransform uiParent;      // default = hudCanvas
        public Vector2 uiPixelOffset;

        [Header("3D (World) Settings")]
        public GameObject worldPrefab;
        public Transform worldParent;       // default = canvasCamera.transform
        public float worldDepthMeters = 1.5f;
        public Vector3 worldRotationOffsetEuler;
        public Vector3 worldLocalOffset;
        public Vector3 worldLocalScale = Vector3.one;

        [Header("Rotation (optional)")]
        public RotationMode rotationMode = RotationMode.None;
        public Body.KeypointID otherKeypoint = Body.KeypointID.RightShoulder;
        public float rotationOffsetDeg = 0f;

        [Header("Spawn gating")]
        [Range(0, 1)] public float spawnWhenScoreAbove = 0.25f;
        public bool generateOnStart = true;
        public bool oneShot = false;

        [Header("Per-rule fine tune (normalized)")]
        public Vector2 normOffsetXY; // tiny nudge before mapping (e.g. (0,-0.02))

        [Header("Per-rule event")]
        public UnityEvent<GameObject> OnGenerated;

        // Runtime
        [NonSerialized] public GameObject instance;
        [NonSerialized] public RectTransform uiRT;
        [NonSerialized] public Vector2 smoothedUIPos;
        [NonSerialized] public bool hasSmoothed;
        [NonSerialized] public Vector2 lastScreen;
        [NonSerialized] public float lastScore;
        [NonSerialized] public float lastAngle;
        [NonSerialized] public string lastNote;
    }

    // ---------- Lifecycle ----------
    void Awake()
    {
        Log($"Awake on {Hierarchy(gameObject.transform)} / scene='{gameObject.scene.name}'");
        if (!driver) Warn("driver (BodyPixDriver) is NOT assigned.");
        if (!hudCanvas) Warn("hudCanvas is NOT assigned.");
        if (!videoRect) Warn("videoRect is NOT assigned.");
        if (!canvasCamera) Warn("canvasCamera is NOT assigned.");
    }

    void Start()
    {
        // Reference sizes
        if (hudCanvas) Log($"Canvas rect: {hudCanvas.rect.size}");
        if (videoRect)
        {
            var rect = videoRect.rect.size;
            Log($"videoRect local size: {rect}");
            var (bl, tr) = VideoRectScreen();
            Log($"videoRect screen BL={bl} TR={tr} size={(tr - bl)}");
        }

        // Initialize rules
        if (rules != null)
        {
            foreach (var r in rules)
            {
                if (r.mode == AssetMode.UI2D && r.uiParent == null) r.uiParent = hudCanvas;
                if (r.mode == AssetMode.World3D && r.worldParent == null) r.worldParent = canvasCamera ? canvasCamera.transform : null;
                //DescribeRule(r);
                if (r.generateOnStart) Generate(r);
            }
        }
    }

    void LateUpdate()
    {
        if (!followEveryFrame || rules == null) return;

        foreach (var r in rules)
        {
            if (r.instance == null || r.oneShot) continue;

            if (!TryGetKeypointScreen(r.keypoint, out var screenPos, out var score, r))
            {
                r.lastNote = "No screen pos";
                r.instance.SetActive(false);
                continue;
            }
            r.lastScreen = screenPos;
            r.lastScore = score;

            if (score < Mathf.Max(minScore, r.spawnWhenScoreAbove))
            {
                r.lastNote = $"Hidden (score {score:0.00} < gate)";
                r.instance.SetActive(false);
                continue;
            }

            // Rotation from other keypoint if requested
            float rotZ = 0f;
            if (r.rotationMode != RotationMode.None &&
                TryGetKeypointScreen(r.otherKeypoint, out var screenB, out var scoreB, r))
            {
                Vector2 dir = screenB - screenPos;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rotZ = (r.rotationMode == RotationMode.PerpendicularToOtherKeypoint) ? (angle + 90f) : angle;
                rotZ += r.rotationOffsetDeg;
            }
            r.lastAngle = rotZ;

            // Apply
            switch (r.mode)
            {
                case AssetMode.UI2D:
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hudCanvas, screenPos, canvasCamera, out var local)) { r.lastNote = "SP->Local fail"; break; }
                    local += r.uiPixelOffset;

                    float a = 1f - Mathf.Exp(-followSmoothRate * Time.deltaTime);
                    if (!r.hasSmoothed) { r.smoothedUIPos = local; r.hasSmoothed = true; }
                    else r.smoothedUIPos = Vector2.Lerp(r.smoothedUIPos, local, a);

                    r.uiRT.anchoredPosition = r.smoothedUIPos;
                    if (r.rotationMode != RotationMode.None) r.uiRT.rotation = Quaternion.Euler(0, 0, rotZ);
                    r.instance.SetActive(true);
                    break;

                case AssetMode.World3D:
                    var wp = canvasCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, r.worldDepthMeters));
                    r.instance.transform.position = wp;
                    if (r.rotationMode != RotationMode.None)
                        r.instance.transform.rotation = Quaternion.LookRotation(canvasCamera.transform.forward) * Quaternion.Euler(0, 0, rotZ);
                    r.instance.transform.position += r.instance.transform.TransformVector(r.worldLocalOffset);
                    break;
            }

            if (verbose && FrameGate())
                Log($"Follow '{r.label}': screen={V(screenPos)} local={V(r.uiRT ? r.uiRT.anchoredPosition : Vector2.zero)} score={score:0.00} rotZ={rotZ:0.0} note={r.lastNote}");
        }
    }

    // ---------- Public API ----------
    [ContextMenu("Generate All")]
    public void GenerateAll()
    {
        if (rules == null) return;
        foreach (var r in rules) Generate(r);
    }

    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        if (rules == null) return;
        foreach (var r in rules)
        {
            if (r.instance) Destroy(r.instance);
            r.instance = null; r.uiRT = null; r.hasSmoothed = false;
        }
        Log("Cleared all instances.");
    }

    public void Generate(SpawnRule r)
    {
        if (r.instance != null) { if (verbose) Log($"'{r.label}' already spawned"); return; }
        if (!TryGetKeypointScreen(r.keypoint, out var screenPos, out var score, r))
        {
            Warn($"Generate '{r.label}': keypoint mapping failed.");
            return;
        }
        if (score < Mathf.Max(minScore, r.spawnWhenScoreAbove))
        {
            Warn($"Generate '{r.label}': low score {score:0.00} < gate.");
            return;
        }

        switch (r.mode)
        {
            case AssetMode.UI2D:
                var parent = r.uiParent ? r.uiParent : hudCanvas;
                var go = new GameObject($"UI_{r.label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var img = go.GetComponent<Image>();
                img.sprite = r.uiSprite;
                img.raycastTarget = false;

                r.uiRT = (RectTransform)go.transform;
                r.uiRT.sizeDelta = r.uiSize;
                r.uiRT.pivot = r.uiPivot;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(hudCanvas, screenPos, canvasCamera, out var local))
                    r.uiRT.anchoredPosition = local + r.uiPixelOffset;

                r.instance = go;
                Log($"Generated UI '{r.label}' at screen={V(screenPos)} local={V(r.uiRT.anchoredPosition)} size={r.uiSize}");
                break;

            case AssetMode.World3D:
                if (!r.worldPrefab) { Warn($"'{r.label}': worldPrefab missing"); return; }
                var worldParent = r.worldParent ? r.worldParent : canvasCamera.transform;
                var inst = Instantiate(r.worldPrefab, worldParent);
                inst.name = $"3D_{r.label}";
                inst.transform.localScale = r.worldLocalScale;

                var wp = canvasCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, r.worldDepthMeters));
                inst.transform.position = wp;
                inst.transform.rotation = Quaternion.LookRotation(canvasCamera.transform.forward) * Quaternion.Euler(r.worldRotationOffsetEuler);
                inst.transform.position += inst.transform.TransformVector(r.worldLocalOffset);

                r.instance = inst;
                Log($"Generated 3D '{r.label}' at worldPos={inst.transform.position} depth={r.worldDepthMeters}");
                break;
        }

        if (r.instance)
        {
            r.OnGenerated?.Invoke(r.instance);
            OnAnyGenerated?.Invoke(r.instance);
        }
    }

    // ---------- Mapping ----------
    bool TryGetKeypointScreen(Body.KeypointID kpID, out Vector2 screen, out float score, SpawnRule contextRule = null)
    {
        screen = default; score = 0f;

        if (!driver || driver.keypoints == null || driver.personMask == null) { Warn("BodyPix not ready."); return false; }
        if (!videoRect || !canvasCamera || !hudCanvas) { Warn("Mapping refs missing (videoRect/canvasCamera/hudCanvas)."); return false; }

        int idx = (int)kpID;
        Vector2 p = driver.keypoints[idx];
        score = driver.keypointScores[idx];

        float texW = driver.personMask.width, texH = driver.personMask.height;
        float nx = coordsAreNormalized ? p.x : p.x / texW;
        float ny = coordsAreNormalized ? p.y : p.y / texH;

        // mirror/flip
        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);
        if (mirrorX) nx = 1f - nx;
        if (flipY) ny = 1f - ny;

        // global calibration
        nx = 0.5f + (nx - 0.5f) * normScale.x + normOffset.x;
        ny = 0.5f + (ny - 0.5f) * normScale.y + normOffset.y;
        nx = Mathf.Clamp01(nx); ny = Mathf.Clamp01(ny);

        // per-rule nudge
        if (contextRule != null)
        {
            nx = Mathf.Clamp01(nx + contextRule.normOffsetXY.x);
            ny = Mathf.Clamp01(ny + contextRule.normOffsetXY.y);
        }

        // videoRect -> screen rect
        var (bl, tr) = VideoRectScreen();
        Vector2 rectPos = bl, rectSize = tr - bl;
        screen = new Vector2(rectPos.x + nx * rectSize.x, rectPos.y + ny * rectSize.y);

        if (verbose && FrameGate())
        {
            Log($"Map {kpID}: raw={V(p)} score={score:0.00} norm=({nx:0.000},{ny:0.000}) rectBL={V(bl)} rectTR={V(tr)} -> screen={V(screen)} rule='{contextRule?.label}'");
        }
        return true;
    }

    (Vector2 bl, Vector2 tr) VideoRectScreen()
    {
        Vector3[] corners = new Vector3[4];
        videoRect.GetWorldCorners(corners);
        var bl = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]); // bottom-left
        var tr = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]); // top-right
        return (bl, tr);
    }

    // ---------- Debug helpers ----------
    bool FrameGate() => logEveryNFrames <= 1 || (Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0);

    static string Hierarchy(Transform t)
    {
        var sb = new StringBuilder();
        while (t != null) { sb.Insert(0, "/" + t.name); t = t.parent; }
        return sb.ToString();
    }
    static string V(Vector2 v) => $"({v.x:0.##},{v.y:0.##})";

    void Log(string msg) => Debug.Log($"[MRGen] {msg}");
    void Warn(string msg) => Debug.LogWarning($"[MRGen] {msg}");
    void Err(string msg) => Debug.LogError($"[MRGen] {msg}");
}
