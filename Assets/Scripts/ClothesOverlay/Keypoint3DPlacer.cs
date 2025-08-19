using System;
using UnityEngine;
using UnityEngine.Events;
using BodyPix;
public enum RotationMode { None, FaceCamera, FaceCameraPlusScreenRoll, LookAtOtherKeypoint }

public class Keypoint3DPlacer : MonoBehaviour
{
    [Header("BodyPix & View Mapping")]
    public BodyPixDriver driver;
    public RectTransform videoRect;    // SAME rect you use in your KeypointOverlay/Tie
    public Camera canvasCamera;        // CenterEyeAnchor

    [Header("Keypoint coordinates (match your overlay)")]
    public bool coordsAreNormalized = true;
    public bool mirrorX = false;
    public bool flipY = false;
    public Vector2 normScale = Vector2.one;   // e.g. (1.02, 0.98) if you used it
    public Vector2 normOffset = Vector2.zero; // e.g. (0.00, -0.01)

    [Header("Target")]
    public Body.KeypointID targetKeypoint = Body.KeypointID.LeftShoulder;
    [Range(0f, 1f)] public float minScore = 0.25f;

    [Header("Prefab & Placement")]
    public GameObject prefab;              // your 3D object (duck/wing/etc.)
    public Transform worldParent;          // default = canvasCamera.transform
    public float worldDepthMeters = 1.6f;  // distance along camera forward
    public Vector3 worldLocalOffset = Vector3.zero; // small tweak in local space after placement
    public Vector3 worldLocalScale = Vector3.one;

    
    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.FaceCamera;
    public Body.KeypointID otherKeypoint = Body.KeypointID.RightShoulder; // used for roll/look-at
    public float rotationOffsetDeg = 0f;

    [Header("Follow")]
    public bool generateOnStart = false;
    public bool followEveryFrame = true;
    public float followSmoothRate = 15f;

    [Header("Events")]
    public UnityEvent<GameObject> OnSpawned;

    // runtime
    GameObject _instance;
    Vector2 _smoothedScreen;
    bool _hasSmooth;
    Vector3[] _corners = new Vector3[4];

    //void Start()
    //{
    //    if (generateOnStart) Spawn();
    //}

    void Start()
    {
        if (generateOnStart) StartCoroutine(WaitForBodyPixThenSpawn());
    }

    public void GeneratePreset(Keypoint3dObjectPreset preset)
    {
        targetKeypoint = preset.targetKeypoint;
        prefab = preset.prefab;
        worldParent = preset.worldParent;
        worldLocalOffset = preset.worldLocalOffset;
        worldLocalScale = preset.worldLocalScale;
        rotationMode = preset.rotationMode;
        otherKeypoint = preset.otherKeypoint;
        rotationOffsetDeg = preset.rotationOffsetDeg;

        StartCoroutine(WaitForBodyPixThenSpawn());
    }

    System.Collections.IEnumerator WaitForBodyPixThenSpawn()
    {
        float t = 0f;
        Debug.Log("[KP3D] Waiting for BodyPix to become ready...");
        while (t < 5f)
        {
            if (driver && driver.personMask != null && driver.keypoints != null)
            {
                Debug.Log("[KP3D] BodyPix ready. Spawning.");
                Spawn();
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
        Debug.LogWarning("[KP3D] Timeout: BodyPix never became ready (personMask still null).");
    }


    [ContextMenu("Spawn")]
    public void Spawn()
    {
        if (_instance != null || prefab == null) return;

        if (!TryGetKeypointScreen(targetKeypoint, out var screen, out var score))
        {
            Debug.LogWarning("[KP3D] Spawn: keypoint mapping failed.");
            return;
        }
        if (score < minScore)
        {
            Debug.LogWarning($"[KP3D] Spawn: low score {score:0.00} < {minScore:0.00}");
            return;
        }

        var parent = worldParent ? worldParent : canvasCamera.transform;
        _instance = Instantiate(prefab, parent);
        _instance.name = $"KP3D_{targetKeypoint}_{prefab.name}";
        _instance.transform.localScale = worldLocalScale;

        // initial place
        var world = canvasCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, worldDepthMeters));
        _instance.transform.position = world;
        ApplyRotation(screen);

        // small local-space nudge (e.g., lift above shoulder)
        _instance.transform.position += _instance.transform.TransformVector(worldLocalOffset);

        OnSpawned?.Invoke(_instance);
        Debug.Log($"[KP3D] Spawned '{_instance.name}' at {world} (depth {worldDepthMeters} m)");
    }

    void LateUpdate()
    {
        if (!followEveryFrame || _instance == null) return;

        if (!TryGetKeypointScreen(targetKeypoint, out var screen, out var score) || score < minScore)
        {
            _instance.SetActive(false);
            return;
        }

        // smoothing (screen space)
        float a = 1f - Mathf.Exp(-followSmoothRate * Time.deltaTime);
        if (!_hasSmooth) { _smoothedScreen = screen; _hasSmooth = true; }
        else _smoothedScreen = Vector2.Lerp(_smoothedScreen, screen, a);

        var world = canvasCamera.ScreenToWorldPoint(new Vector3(_smoothedScreen.x, _smoothedScreen.y, worldDepthMeters));
        _instance.transform.position = world;

        ApplyRotation(_smoothedScreen);

        // local nudge
        _instance.transform.position += _instance.transform.TransformVector(worldLocalOffset);

        _instance.SetActive(true);
    }

    void ApplyRotation(Vector2 screenA)
    {
        if (_instance == null) return;

        switch (rotationMode)
        {
            case RotationMode.None:
                // keep prefab rotation
                break;

            case RotationMode.FaceCamera:
                _instance.transform.rotation =
                    Quaternion.LookRotation(canvasCamera.transform.forward) *
                    Quaternion.Euler(0, 0, rotationOffsetDeg);
                break;

            case RotationMode.FaceCameraPlusScreenRoll:
                {
                    // roll around camera forward by screen-space direction to "other" kp
                    if (TryGetKeypointScreen(otherKeypoint, out var screenB, out _))
                    {
                        Vector2 dir = screenB - screenA;
                        float rollDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        _instance.transform.rotation =
                            Quaternion.LookRotation(canvasCamera.transform.forward) *
                            Quaternion.Euler(0, 0, rollDeg + rotationOffsetDeg);
                    }
                    else
                    {
                        _instance.transform.rotation =
                            Quaternion.LookRotation(canvasCamera.transform.forward) *
                            Quaternion.Euler(0, 0, rotationOffsetDeg);
                    }
                    break;
                }

            case RotationMode.LookAtOtherKeypoint:
                {
                    // look from this point towards "other" kp on the same depth plane
                    if (TryGetKeypointScreen(otherKeypoint, out var screenB, out _))
                    {
                        var a = canvasCamera.ScreenToWorldPoint(new Vector3(screenA.x, screenA.y, worldDepthMeters));
                        var b = canvasCamera.ScreenToWorldPoint(new Vector3(screenB.x, screenB.y, worldDepthMeters));
                        var fwd = (b - a).normalized;
                        if (fwd.sqrMagnitude < 1e-6f) fwd = canvasCamera.transform.forward;
                        _instance.transform.rotation = Quaternion.LookRotation(fwd) *
                                                       Quaternion.Euler(0, 0, rotationOffsetDeg);
                    }
                    else
                    {
                        _instance.transform.rotation = Quaternion.LookRotation(canvasCamera.transform.forward);
                    }
                    break;
                }
        }
    }


    [Header("Debug")]
    public bool verbose = true;
    [Tooltip("Throttle verbose logs: prints once every N frames.")]
    public int logEveryNFrames = 30;

    bool FrameGate() => logEveryNFrames <= 1 || (Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0);

    bool TryGetKeypointScreen(Body.KeypointID id, out Vector2 screen, out float score)
    {
        screen = default; score = 0f;

        // ---- Deep null / readiness checks with exact reasons ----
        if (!driver) { Debug.LogWarning("[KP3D] driver is NULL."); return false; }
        if (driver.keypoints == null) { Debug.LogWarning("[KP3D] driver.keypoints is NULL (BodyPix not producing yet)."); return false; }
        if (driver.keypointScores == null) { Debug.LogWarning("[KP3D] driver.keypointScores is NULL."); return false; }
        if (driver.personMask == null) { Debug.LogWarning("[KP3D] driver.personMask is NULL."); return false; }
        if (!videoRect) { Debug.LogWarning("[KP3D] videoRect is NULL (assign the same full-screen rect you use in the overlay)."); return false; }
        if (!canvasCamera) { Debug.LogWarning("[KP3D] canvasCamera is NULL (assign CenterEyeAnchor)."); return false; }

        int i = (int)id;
        if (i < 0 || i >= driver.keypoints.Length)
        {
            Debug.LogWarning($"[KP3D] Keypoint index out of range: {i} for {id}. keypoints.Length={driver.keypoints.Length}");
            return false;
        }

        Vector2 p = driver.keypoints[i];
        score = driver.keypointScores[i];


            var pm = driver.personMask;
            Debug.Log($"[KP3D] Raw {id}: kp={p} score={score:0.00} mask={pm.width}x{pm.height}");
        

        float texW = driver.personMask.width, texH = driver.personMask.height;

        // normalize
        float nx = coordsAreNormalized ? p.x : p.x / Mathf.Max(1f, texW);
        float ny = coordsAreNormalized ? p.y : p.y / Mathf.Max(1f, texH);
        float nxBefore = nx, nyBefore = ny;

        // clamp + mirror/flip
        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);
        if (mirrorX) nx = 1f - nx;
        if (flipY) ny = 1f - ny;

        // global calibration (match your overlay)
        nx = 0.5f + (nx - 0.5f) * normScale.x + normOffset.x;
        ny = 0.5f + (ny - 0.5f) * normScale.y + normOffset.y;
        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);

        if (verbose && FrameGate())
            Debug.Log($"[KP3D] {id} norm=({nxBefore:0.000},{nyBefore:0.000}) -> after flip/mirror/cal=({nx:0.000},{ny:0.000})");

        // map into SCREEN rect of videoRect
        videoRect.GetWorldCorners(_corners);
        var bl = RectTransformUtility.WorldToScreenPoint(canvasCamera, _corners[0]); // bottom-left
        var tr = RectTransformUtility.WorldToScreenPoint(canvasCamera, _corners[2]); // top-right
        Vector2 rectPos = bl, rectSize = tr - bl;

        if (rectSize.x < 2 || rectSize.y < 2)
        {
            Debug.LogWarning($"[KP3D] videoRect screen size looks tiny: {rectSize}. Is it disabled, not stretched full-screen, or on another camera?");
            return false;
        }

        screen = new Vector2(rectPos.x + nx * rectSize.x,
                             rectPos.y + ny * rectSize.y);

        if (verbose && FrameGate())
            Debug.Log($"[KP3D] Map {id}: rectBL={bl} rectTR={tr} size={rectSize} -> screen={screen}");

        return true;
    }

}
