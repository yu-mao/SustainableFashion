// BodyPixPreviewUI.cs
using PassthroughCameraSamples;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class BodyPixPreviewUI : MonoBehaviour
{
    public PassthroughDownsampler downsampler; // drag your Downsampler GO
    public BodyPixDriver driver;               // drag your BodyPix GO
    public RawImage inputView;                 // 256x256 preview (optional)
    public RawImage maskView;                  // segmentation preview (optional)


    public TMP_Text debugText;
    public WebCamTextureManager camMgr;         // from your scene
    public BodyPixDriver bodypix;               // from your scene

    void Update()
    {
        if (downsampler && inputView)
            inputView.texture = downsampler.downsampled256;

        if (driver && maskView)
            maskView.texture = driver.personMask;


        if (!debugText) return;
        debugText.text =
            $"Cam tex: {(camMgr && camMgr.WebCamTexture ? "OK" : "NULL")}\n" +
            $"Downsample RT: {(downsampler && downsampler.downsampled256 ? "OK" : "NULL")}\n" +
            $"BodyPix resource: {(bodypix && bodypix.resourceSet ? "OK" : "NULL")}\n" +
            $"BodyPix input: {(bodypix && bodypix.inputTexture ? "OK" : "NULL")}\n" +
            $"Mask tex: {(bodypix && bodypix.personMask ? "OK" : "NULL")}";
    }
}
