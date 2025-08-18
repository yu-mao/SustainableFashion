using System;
using System.Collections;
using UnityEngine;

public class EnvDetectionController : MonoBehaviour
{
    public event Action<Texture2D> OnWebcamScreenshotCollected;
    public bool IsDetectingEnvironment { get; set; }

    [SerializeField] private float webcamDetectionTimeInterval = 2f;
    [SerializeField] private WebcamController webcamController;

    private void Start()
    {
        IsDetectingEnvironment = true;
        
        StartCoroutine(RepeatGettingCameraScreenshot());
    }

    private void OnDisable()
    {
        StopCoroutine(RepeatGettingCameraScreenshot());
    }

    private IEnumerator RepeatGettingCameraScreenshot()
    {
        while (IsDetectingEnvironment)
        {
            Texture2D screenshot = webcamController.MakeCameraSnapshot();
            
            if(screenshot != null)
                OnWebcamScreenshotCollected?.Invoke(screenshot);
            yield return new WaitForSeconds(webcamDetectionTimeInterval);
        }
    }
}
