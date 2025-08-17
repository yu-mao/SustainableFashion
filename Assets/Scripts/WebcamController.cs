// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
public class WebcamController : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private Text debugText;
    [SerializeField] private RawImage webcamImage;
    
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