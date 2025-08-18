using System;
using System.Collections;
using UnityEngine;

public enum UserEnvType
{
    Office,
    Festival,
    Others
}

public class EnvDetectionController : MonoBehaviour
{
    public event Action<Texture2D> OnWebcamScreenshotCollected;
    public event Action<UserEnvType> OnUserEnvChanged;
    public bool IsDetectingEnvironment { get; set; }

    [Header("PCA reference")]
    [SerializeField] private float webcamDetectionTimeInterval = 2f;
    [SerializeField] private WebcamController webcamController;
    
    [Header("AI reference")]
    [SerializeField] private AIController aiController;
    
    private UserEnvType userEnvType = UserEnvType.Others;

    private void Start()
    {
        IsDetectingEnvironment = true;
        aiController.OnAIResponded += ParseUserEnvType;
        
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
            Texture2D snapshot = webcamController.MakeCameraSnapshot();
            
            if(snapshot != null)
                OnWebcamScreenshotCollected?.Invoke(snapshot);
            yield return new WaitForSeconds(webcamDetectionTimeInterval);
        }
    }
    
    private void ParseUserEnvType(string aiResponse)
    {
        if (aiResponse.Contains("office", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Office)
        {
            userEnvType = UserEnvType.Office;
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("festival", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Festival;
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        // ignore the case when user env type is unclear/others 
    }
}

