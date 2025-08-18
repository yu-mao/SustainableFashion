// WebcamController.cs
// Copyright (c) Meta Platforms, Inc.

using System.Collections;

using Meta.XR.Samples;

using PassthroughCameraSamples;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    [Header("Meta Sample Objects")]
    [SerializeField] private WebCamTextureManager webCamTextureManager;

    [Header("Debug UI (optional)")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text debugText2;

    [Header("Optional Preview")]
    [SerializeField] private RawImage webcamImage;

    [Header("Hook-ups")]
    [SerializeField] private PassthroughDownsampler downsampler; // assign in Inspector
    [SerializeField] private BodyPixDriver bodypix;              // assign in Inspector (if present)

    private Color32[] pixelsBuffer;

    bool _loggedNoPermission, _loggedNoCamMgr, _loggedNoTex, _loggedFedDownsampler, _loggedFedBodyPix;

    void Awake()
    {
        Debug.Log("[WebcamController] Awake");
        if (!webCamTextureManager) Debug.LogError("[WebcamController] WebCamTextureManager is NOT assigned.");
        if (!downsampler) Debug.LogWarning("[WebcamController] PassthroughDownsampler is NOT assigned.");
        if (!bodypix) Debug.LogWarning("[WebcamController] BodyPixDriver is NOT assigned (ok during bring-up).");
    }

    private IEnumerator Start()
    {
        Debug.Log("[WebcamController] Start: waiting for WebCamTexture...");
        // Wait until the Meta sample produces a WebCamTexture
        while (webCamTextureManager == null || webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
        }

        var tex = webCamTextureManager.WebCamTexture;
        Debug.Log($"[WebcamController] WebCamTexture ready. size={tex.width}x{tex.height} isPlaying={tex.isPlaying}");

        ResumeStreamingFromCamera();
        debugText?.SetText("WebCamTexture Object ready and playing.");
    }

    public Texture2D MakeCameraSnapshot()
    {
        if (webCamTextureManager == null || webCamTextureManager.WebCamTexture == null)
        {
            Debug.LogWarning("[WebcamController] MakeCameraSnapshot: no WebCamTexture.");
            return null;
        }

        var w = webCamTextureManager.WebCamTexture.width;
        var h = webCamTextureManager.WebCamTexture.height;

        Texture2D cameraSnapshot = new Texture2D(w, h, TextureFormat.RGBA32, false);
        pixelsBuffer ??= new Color32[w * h];
        webCamTextureManager.WebCamTexture.GetPixels32(pixelsBuffer);
        cameraSnapshot.SetPixels32(pixelsBuffer);
        cameraSnapshot.Apply();

        Debug.Log($"[WebcamController] Snapshot taken: {w}x{h}");
        return cameraSnapshot;
    }

    public void ResumeStreamingFromCamera()
    {
        if (webCamTextureManager == null)
        {
            Debug.LogError("[WebcamController] ResumeStreamingFromCamera: WebCamTextureManager is NULL.");
            return;
        }

        var tex = webCamTextureManager.WebCamTexture;
        if (tex == null)
        {
            Debug.LogError("[WebcamController] ResumeStreamingFromCamera: WebCamTexture is NULL.");
            return;
        }

        // Optional on-screen preview
        if (webcamImage) webcamImage.texture = tex;

        // Feed the downsampler
        if (downsampler)
        {
            downsampler.passthroughTexture = tex;
            if (!_loggedFedDownsampler)
            {
                Debug.Log($"[WebcamController] Fed downsampler with camera tex {tex.width}x{tex.height}");
                _loggedFedDownsampler = true;
            }
        }
        else
        {
            Debug.LogWarning("[WebcamController] No downsampler assigned.");
        }

        // Hand 256x256 to BodyPix when available
        if (bodypix && downsampler && downsampler.downsampled256 != null)
        {
            bodypix.inputTexture = downsampler.downsampled256;
            if (!_loggedFedBodyPix)
            {
                Debug.Log("[WebcamController] BodyPix inputTexture set to downsampler.downsampled256 (256x256).");
                _loggedFedBodyPix = true;
            }
        }
        else
        {
            Debug.Log("[WebcamController] BodyPix not yet wired (either bodypix/downsampler/downsampled256 missing) — will retry in Update.");
        }

        debugText?.SetText("WebCamTexture ready.");
    }

    private void Update()
    {
        if (PassthroughCameraPermissions.HasCameraPermission != true)
        {
            if (!_loggedNoPermission)
            {
                Debug.LogError("[WebcamController] No CAMERA permission. Request permission in your scene or via Meta sample.");
                _loggedNoPermission = true;
            }
            debugText?.SetText("No permission granted.");
        }

        if (webCamTextureManager == null)
        {
            if (!_loggedNoCamMgr)
            {
                Debug.LogError("[WebcamController] WebCamTextureManager is NULL.");
                _loggedNoCamMgr = true;
            }
            return;
        }

        if (webCamTextureManager.WebCamTexture == null)
        {
            if (!_loggedNoTex)
            {
                Debug.LogWarning("[WebcamController] WebCamTexture is NULL (still initializing?).");
                _loggedNoTex = true;
            }
            return;
        }

        // Keep UI text alive (optional)
        debugText?.SetText("webcam texture working");

        // Retry wiring BodyPix if we couldn't earlier
        if (bodypix && downsampler && downsampler.downsampled256 != null && bodypix.inputTexture != downsampler.downsampled256)
        {
            bodypix.inputTexture = downsampler.downsampled256;
            Debug.Log("[WebcamController] Rewired BodyPix inputTexture to downsampled256.");
        }
    }
}
