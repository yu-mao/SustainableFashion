// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
public class WebcamController : MonoBehaviour
{
    [SerializeField] private RawImage webcamImage;
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private Text debugText;
    
    private Color32[] pixelsBuffer;

    public Texture2D MakeCameraSnapshot()
    {
        var webCamTexture = webCamTextureManager.WebCamTexture;
        if (webCamTexture == null || !webCamTexture.isPlaying)
            return null;

        Texture2D cameraSnapshot = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

        // Copy the last available image from WebCamTexture to a separate object
        pixelsBuffer ??= new Color32[webCamTexture.width * webCamTexture.height];
        _ = webCamTextureManager.WebCamTexture.GetPixels32(pixelsBuffer);
        cameraSnapshot.SetPixels32(pixelsBuffer);
        cameraSnapshot.Apply();

        return cameraSnapshot;
    }
    
    public void ResumeStreamingFromCamera()
    {
        webcamImage.texture = webCamTextureManager.WebCamTexture;
    }


    private IEnumerator Start()
    {
        while (webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
        }
        if (debugText) debugText.text = "WebCamTexture Object ready and playing.";
        ResumeStreamingFromCamera();
    }

    private void Update()
    {
        if (PassthroughCameraPermissions.HasCameraPermission != true)
        {
            if (debugText) debugText.text = "No permission granted.";
        }
    }
}