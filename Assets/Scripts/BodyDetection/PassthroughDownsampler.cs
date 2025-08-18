using UnityEngine;

public class PassthroughDownsampler : MonoBehaviour
{
    [Header("Input (assign at runtime)")]
    public Texture passthroughTexture;

    [Header("Output")]
    public RenderTexture downsampled256;

    void Start()
    {
        downsampled256 = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32)
        { useMipMap = false, antiAliasing = 1 };
        downsampled256.Create();
    }

    void LateUpdate()
    {
        if (passthroughTexture == null || downsampled256 == null) return;
        Graphics.Blit(passthroughTexture, downsampled256);
    }
}
