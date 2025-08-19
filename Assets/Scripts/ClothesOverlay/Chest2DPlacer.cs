using UnityEngine;
using UnityEngine.UI;
using BodyPix;

public class Chest2DPlacer : MonoBehaviour
{
    [Header("Refs")]
    public BodyPixDriver driver;
    public RectTransform hudCanvas;     // HUD Canvas RectTransform
    public RectTransform UIObject;      // tie Image RectTransform (pivot 0.5, 1)
    public RectTransform videoRect;     // SAME rect you use in KeypointOverlay
    public Camera canvasCamera;         // CenterEyeAnchor (add this)

    [Header("Input coords")]
    public bool coordsAreNormalized = true;
    public bool mirrorX = false, flipY = false;

    [Header("Calibration (match overlay)")]
    public Vector2 normScale = Vector2.one;   // e.g. (1.02, 0.98) if you used it
    public Vector2 normOffset = Vector2.zero; // e.g. (0.00, -0.01)

    [Header("Behavior")]
    public bool useFixedSizeFirst = true;
    public Vector2 fixedSize = new Vector2(100, 200);
    public float heightFromShoulder = 0.9f;
    public float widthFromShoulder = 0.25f;
    public Vector2 heightClamp = new Vector2(80, 360);
    public Vector2 widthClamp = new Vector2(60, 220);

    [Header("Placement & smoothing")]
    [Tooltip("Pixels to move DOWN from the neck (mid-shoulders) along the torso direction.")]
    public float neckAlongTorso = 12f;        // try 8–18
    public float smoothRate = 12f;
    [Range(0, 1)] public float minScore = 0.25f;

    // state
    Vector2 _posSmoothed;
    float _angSmoothed;
    bool _hasPrev;

    void Awake()
    {
        // Ensure knot/pivot feels right (top-center)
        if (UIObject)
        {
            UIObject.anchorMin = UIObject.anchorMax = new Vector2(0.5f, 0.5f);
            UIObject.pivot = new Vector2(0.5f, 1.0f);
        }
    }

    public void GenerateImageOnChest(RectTransform chest2dDesign)
    {
        UIObject = chest2dDesign;

        // Ensure knot/pivot feels right (top-center)
        if (UIObject)
        {
            UIObject.anchorMin = UIObject.anchorMax = new Vector2(0.5f, 0.5f);
            UIObject.pivot = new Vector2(0.5f, 1.0f);
        }
    }

    // --- SAME mapping as overlay: norm -> screen inside videoRect -> canvas local
    bool MapToCanvasLocal(Vector2 kp, out Vector2 local)
    {
        local = Vector2.zero;
        if (!driver || driver.personMask == null || !hudCanvas || !videoRect || !canvasCamera) return false;

        float texW = driver.personMask.width, texH = driver.personMask.height;

        // normalized
        float nx = coordsAreNormalized ? kp.x : kp.x / texW;
        float ny = coordsAreNormalized ? kp.y : kp.y / texH;

        nx = Mathf.Clamp01(nx); ny = Mathf.Clamp01(ny);
        if (mirrorX) nx = 1f - nx;
        if (flipY) ny = 1f - ny;

        // apply the same calibration knobs as overlay
        nx = 0.5f + (nx - 0.5f) * normScale.x + normOffset.x;
        ny = 0.5f + (ny - 0.5f) * normScale.y + normOffset.y;
        nx = Mathf.Clamp01(nx); ny = Mathf.Clamp01(ny);

        // videoRect in SCREEN space
        Vector3[] corners = new Vector3[4];
        videoRect.GetWorldCorners(corners);
        Vector2 bl = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
        Vector2 tr = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]);
        Vector2 rectPos = bl, rectSize = tr - bl;

        // into that rect
        Vector2 screen = new Vector2(rectPos.x + nx * rectSize.x,
                                     rectPos.y + ny * rectSize.y);

        // screen -> canvas local
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(hudCanvas, screen, canvasCamera, out local);
    }

    void LateUpdate()
    {
        if (!driver || driver.keypoints == null || driver.personMask == null || !UIObject || !hudCanvas)
        { if (UIObject) UIObject.gameObject.SetActive(false); _hasPrev = false; return; }

        int L = (int)Body.KeypointID.LeftShoulder;
        int R = (int)Body.KeypointID.RightShoulder;
        int LH = (int)Body.KeypointID.LeftHip;
        int RH = (int)Body.KeypointID.RightHip;

        float sL = driver.keypointScores[L], sR = driver.keypointScores[R];
        if (Mathf.Max(sL, sR) < minScore) { UIObject.gameObject.SetActive(false); _hasPrev = false; return; }

        if (!MapToCanvasLocal(driver.keypoints[L], out var ls) ||
            !MapToCanvasLocal(driver.keypoints[R], out var rs))
        { UIObject.gameObject.SetActive(false); _hasPrev = false; return; }

        // neck & shoulder vector in CANVAS local
        Vector2 sMid = (ls + rs) * 0.5f;
        Vector2 v = rs - ls;
        float shoulderLen = v.magnitude;

        // Rotation: use torso tilt if hips available, else perpendicular to shoulders
        float angle;
        if (MapToCanvasLocal(driver.keypoints[LH], out var lh) &&
            MapToCanvasLocal(driver.keypoints[RH], out var rh))
        {
            Vector2 hMid = (lh + rh) * 0.5f;
            Vector2 torso = hMid - sMid;
            float torsoAngle = Mathf.Atan2(torso.y, torso.x) * Mathf.Rad2Deg;
            angle = torsoAngle - 90f;  // RectTransform long axis is +Y
        }
        else
        {
            float shoulderAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            angle = shoulderAngle - 90f; // point down from neck
        }

        angle += 180;


        // Position: step from neck down along torso a few pixels
        Vector2 targetPos;
        if (MapToCanvasLocal(driver.keypoints[LH], out var lh2) &&
            MapToCanvasLocal(driver.keypoints[RH], out var rh2))
        {
            Vector2 hMid = (lh2 + rh2) * 0.5f;
            Vector2 torsoDir = (hMid - sMid).normalized;
            targetPos = sMid + torsoDir * neckAlongTorso;
        }
        else
        {
            Vector2 downFromShoulders = new Vector2(-v.y, v.x).normalized; // +90°
            targetPos = sMid + downFromShoulders * neckAlongTorso;
        }

        // Smoothing
        float a = 1f - Mathf.Exp(-smoothRate * Time.deltaTime);
        if (!_hasPrev) { _posSmoothed = targetPos; _angSmoothed = angle; _hasPrev = true; }
        else
        {
            _posSmoothed = Vector2.Lerp(_posSmoothed, targetPos, a);
            _angSmoothed = _angSmoothed + Mathf.DeltaAngle(_angSmoothed, angle) * a;
        }

        // Apply
        UIObject.gameObject.SetActive(true);
        UIObject.anchoredPosition = _posSmoothed;
        UIObject.rotation = Quaternion.Euler(0, 0, _angSmoothed);

        if (useFixedSizeFirst)
        {
            UIObject.sizeDelta = fixedSize;
        }
        else
        {
            float H = Mathf.Clamp(shoulderLen * heightFromShoulder, heightClamp.x, heightClamp.y);
            float W = Mathf.Clamp(shoulderLen * widthFromShoulder, widthClamp.x, widthClamp.y);
            UIObject.sizeDelta = new Vector2(W, H);
        }

        UIObject.SetAsLastSibling();

        if (Time.frameCount % 90 == 0)
            Debug.Log($"[Tie] len={shoulderLen:0.0} pos={UIObject.anchoredPosition} size={UIObject.sizeDelta} angle={_angSmoothed:0.0}");
    }
}
