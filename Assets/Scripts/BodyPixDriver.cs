// BodyPixDriver.cs
using UnityEngine;
using BodyPix; // <-- from the package

public class BodyPixDriver : MonoBehaviour
{
    [Header("Resources")]
    public ResourceSet resourceSet;      // drag your BodyPix_ResNet50 asset here

    [Header("Input (set from your Downsampler)")]
    public Texture inputTexture;         // set to PassthroughDownsampler.downsampled256

    [Header("Outputs (read-only at runtime)")]
    public RenderTexture personMask;     // segmentation mask texture
    public Vector2[] keypoints;          // 2D keypoint positions (pixels, see note below)
    public float[] keypointScores;       // confidence scores [0..1]

    BodyDetector _detector;
    int _initW = 256, _initH = 256;      // default; will use inputTexture size if available

    void OnEnable()
    {
        TryInitDetector();
    }

    void OnDisable()
    {
        _detector?.Dispose();
        _detector = null;
    }

    void TryInitDetector()
    {
        if (_detector != null) return;
        if (resourceSet == null) return;

        // Use the input texture size if we already have it; otherwise 256x256.
        var w = (inputTexture != null) ? inputTexture.width : _initW;
        var h = (inputTexture != null) ? inputTexture.height : _initH;

        _detector = new BodyDetector(resourceSet, w, h);

        // Prepare output arrays
        keypoints = new Vector2[Body.KeypointCount];
        keypointScores = new float[Body.KeypointCount];
    }

    void Update()
    {
        // Late assignment? Initialize when input/asset becomes available.
        if (_detector == null && resourceSet != null) TryInitDetector();
        if (_detector == null || inputTexture == null) return;

        // Run inference
        _detector.ProcessImage(inputTexture);

        // Grab the mask texture
        personMask = _detector.MaskTexture;

        // Copy keypoints into a simple Vector2[] for easy use
        var span = _detector.Keypoints; // ReadOnlySpan<Keypoint>
        for (int i = 0; i < Body.KeypointCount; i++)
        {
            keypoints[i] = span[i].Position;     // pixel coords (see note)
            keypointScores[i] = span[i].Score;   // confidence
        }
    }
}
