using UnityEngine;
using UnityEngine.UI;

public class VideoViewBinder : MonoBehaviour
{
    public PassthroughDownsampler downsampler; // drag your Downsampler GO
    public RawImage videoView;                 // drag your VideoView RawImage

    void Update()
    {
        if (downsampler && downsampler.downsampled256 && videoView &&
            videoView.texture != downsampler.downsampled256)
        {
            videoView.texture = downsampler.downsampled256;
        }
    }
}
