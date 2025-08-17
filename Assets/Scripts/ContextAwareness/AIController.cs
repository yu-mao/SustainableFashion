using System;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private EnvDetectionController envDetectionController;

    private void Start()
    {
        envDetectionController.OnWebcamScreenshotCollected += RecognizeUserEnv;
    }

    private void RecognizeUserEnv(Texture2D passthroughCamTexture2D)
    {
        Debug.Log($"~~~ received snapshot pixels: ({passthroughCamTexture2D.width}, {passthroughCamTexture2D.height})");
    }
    
    
}
