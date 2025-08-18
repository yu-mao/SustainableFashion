using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

public enum UserEnvType
{
    Office,
    Festival,
    Supermarket,
    Home,
    Park,
    Transport,
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

    [Header("UI reference")]
    [SerializeField] private RawImage envDetectionNotification;
    [SerializeField] private Texture imgEnvOffice;
    [SerializeField] private Texture imgEnvFestival;
    [SerializeField] private Texture imgEnvSupermarket;
    [SerializeField] private Texture imgEnvTransport;
    [SerializeField] private Texture imgEnvPark;
    
    private UserEnvType userEnvType = UserEnvType.Others;

    [Button]
    public void ChangeUserEnv()
    {
        int currIndexUserEnv = (int)userEnvType;
        int totalCount = Enum.GetValues(typeof(UserEnvType)).Length;
        if (currIndexUserEnv >= totalCount - 1)
            currIndexUserEnv = 0;
        else
            currIndexUserEnv += 1;
        userEnvType = (UserEnvType)currIndexUserEnv;
        OnUserEnvChanged?.Invoke(userEnvType);
    }

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
        else if (aiResponse.Contains("supermarket", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Supermarket;
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("home", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Home;
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("park", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Park;
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("transport", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Transport;
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        // ignore the case when user env type is unclear/others 
    }
}


// TODO: add left pinch to switch between user env type 
