using UnityEngine;
using UnityEngine.UI;
using BodyPix;

public class KeypointOverlay : MonoBehaviour
{
    [Header("Refs")]
    public BodyPixDriver driver;
    public RectTransform hudCanvas;     // Screen Space - Camera (CenterEyeAnchor) or World Space
    public RectTransform videoRect;     // Rect of the visible video: full-screen rect for Underlay, or the RawImage rect
    public RectTransform dotPrefab;     // small opaque UI Image (e.g., 16x16)
    public Camera canvasCamera;         // CenterEyeAnchor

    [Header("Input coords")]
    public bool coordsAreNormalized = true;  // BodyPix keypoints look 0..1 in your logs
    public bool mirrorX = false;
    public bool flipY = false;
    public float minScore = 0.0f;

    [Header("Visuals")]
    public Vector2 dotSize = new Vector2(16, 16);
    public bool smooth = true;
    public float smoothRate = 15f;

    [Header("Calibration (only if needed)")]
    [Tooltip("Scales normalized coords around 0.5,0.5 to compensate FOV mismatch")]
    public Vector2 normScale = Vector2.one;     // e.g., (1.02, 0.98)
    [Tooltip("Adds a tiny normalized offset after scaling")]
    public Vector2 normOffset = Vector2.zero;   // e.g., (0.00, -0.01)

    RectTransform[] dots;
    Vector2[] smoothed;
    Vector3[] corners = new Vector3[4];

    void Start()
    {
        if (!driver || !hudCanvas || !dotPrefab || !canvasCamera || !videoRect)
        {
            Debug.LogError("[KPO-Upscaled] Missing references"); enabled = false; return;
        }

        dots = new RectTransform[Body.KeypointCount];
        smoothed = new Vector2[Body.KeypointCount];
        for (int i = 0; i < dots.Length; i++)
        {
            var d = Instantiate(dotPrefab, hudCanvas, false);
            d.name = "KP_" + ((Body.KeypointID)i);
            d.sizeDelta = dotSize;
            d.gameObject.SetActive(false);
            dots[i] = d;
        }
    }

    void LateUpdate()
    {
        if (driver.personMask == null || driver.keypoints == null) return;

        // Get the SCREEN-SPACE rectangle of the visible video area
        videoRect.GetWorldCorners(corners);
        var bl = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]); // bottom-left
        var tr = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]); // top-right
        Vector2 rectPos = bl;
        Vector2 rectSize = tr - bl;

        float texW = driver.personMask.width, texH = driver.personMask.height;
        float a = 1f - Mathf.Exp(-smoothRate * Time.deltaTime);

        for (int i = 0; i < dots.Length; i++)
        {
            float score = driver.keypointScores[i];
            if (score < minScore) { dots[i].gameObject.SetActive(false); continue; }

            Vector2 p = driver.keypoints[i];

            // Normalize to 0..1 if needed
            float nx, ny;
            if (coordsAreNormalized) { nx = p.x; ny = p.y; }
            else { nx = p.x / texW; ny = p.y / texH; }

            // Clamp & apply mirror/flip
            nx = Mathf.Clamp01(nx);
            ny = Mathf.Clamp01(ny);
            if (mirrorX) nx = 1f - nx;
            if (flipY) ny = 1f - ny;

            // Optional calibration (around center)
            nx = 0.5f + (nx - 0.5f) * normScale.x + normOffset.x;
            ny = 0.5f + (ny - 0.5f) * normScale.y + normOffset.y;
            nx = Mathf.Clamp01(nx);
            ny = Mathf.Clamp01(ny);

            // Map into the visible video rectangle in SCREEN space
            Vector2 screen = new Vector2(rectPos.x + nx * rectSize.x,
                                         rectPos.y + ny * rectSize.y);

            // Convert screen  canvas local
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(hudCanvas, screen, canvasCamera, out var local))
            {
                if (smooth)
                {
                    smoothed[i] = Vector2.Lerp(smoothed[i], local, a);
                    dots[i].anchoredPosition = smoothed[i];
                }
                else
                {
                    dots[i].anchoredPosition = local;
                }

                dots[i].gameObject.SetActive(true);
                dots[i].SetAsLastSibling();
            }
            else
            {
                dots[i].gameObject.SetActive(false);
            }
        }
    }
}
