// TiePlacer.cs
using UnityEngine;
using UnityEngine.UI;
using BodyPix;

public class TiePlacer : MonoBehaviour
{
    public BodyPixDriver driver;     // drag your BodyPix GO
    public RectTransform tieUI;      // a UI Image on a Screen Space - Overlay Canvas
    public Vector2 inputSize = new Vector2(256, 256); // match your BodyDetector size

    void LateUpdate()
    {
        if (driver == null || driver.keypoints == null) { tieUI.gameObject.SetActive(false); return; }

        int L = (int)Body.KeypointID.LeftShoulder;
        int R = (int)Body.KeypointID.RightShoulder;

        // hide if low confidence
        if (driver.keypointScores[L] < 0.3f || driver.keypointScores[R] < 0.3f)
        { tieUI.gameObject.SetActive(false); return; }

        Vector2 ls = driver.keypoints[L];
        Vector2 rs = driver.keypoints[R];

        // map from input space (e.g., 256x256) to screen space
        Vector2 toScreen(Vector2 p)
            => new Vector2(p.x / inputSize.x * Screen.width,
                           p.y / inputSize.y * Screen.height);

        Vector2 sLS = toScreen(ls);
        Vector2 sRS = toScreen(rs);

        Vector2 mid = (sLS + sRS) * 0.5f;
        Vector2 dir = sRS - sLS;

        tieUI.gameObject.SetActive(true);
        tieUI.position = mid;
        tieUI.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        tieUI.sizeDelta = new Vector2(tieUI.sizeDelta.x, Mathf.Max(10f, dir.magnitude * 0.6f));
    }
}
