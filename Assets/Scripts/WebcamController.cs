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

    // NEW: hook points
    [Header("Hook-ups")]
    [SerializeField] private PassthroughDownsampler downsampler; // assign in Inspector
    [SerializeField] private BodyPixDriver bodypix;              // assign in Inspector (if you have it)

    public void ResumeStreamingFromCamera()
    {
        var tex = webCamTextureManager.WebCamTexture;
        if (tex == null) return;

        // show on UI (optional)
        if (webcamImage) webcamImage.texture = tex;

        // FEED the downsampler
        if (downsampler) downsampler.passthroughTexture = tex;

        // OPTIONAL: once downsampler has created its 256x256 RT, hand it to BodyPix
        if (bodypix && downsampler && downsampler.downsampled256 != null)
            bodypix.inputTexture = downsampler.downsampled256;

        if (debugText) debugText.text = "WebCamTexture ready.";
    }

    private IEnumerator Start()
    {
        // Wait until the WebCamTexture exists and is playing
        while (webCamTextureManager.WebCamTexture == null)
            yield return null;

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
