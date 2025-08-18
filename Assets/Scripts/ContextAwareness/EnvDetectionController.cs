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
    [SerializeField] private Texture imgEnvHome;
    [SerializeField] private Texture imgEnvPark;
    
    private UserEnvType userEnvType = UserEnvType.Others;
    private bool isEnvDetectionUIChanging = false;

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
            UpdateEnvDetectionUI(userEnvType);
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("festival", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Festival;
            UpdateEnvDetectionUI(userEnvType);
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("supermarket", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Supermarket;
            UpdateEnvDetectionUI(userEnvType);
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("home", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Home;
            UpdateEnvDetectionUI(userEnvType);
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("park", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Park;
            UpdateEnvDetectionUI(userEnvType);
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        else if (aiResponse.Contains("transport", StringComparison.OrdinalIgnoreCase) && userEnvType != UserEnvType.Festival)
        {
            userEnvType = UserEnvType.Transport;
            UpdateEnvDetectionUI(userEnvType);
            OnUserEnvChanged?.Invoke(userEnvType);
        }
        // ignore the case when user env type is unclear/others 
    }

    private void UpdateEnvDetectionUI(UserEnvType envType)
    {
        switch (envType)
        {
            case UserEnvType.Office:
                envDetectionNotification.texture = imgEnvOffice;
                break;
            case UserEnvType.Festival:
                envDetectionNotification.texture = imgEnvFestival;
                break;
            case UserEnvType.Home:
                envDetectionNotification.texture = imgEnvHome;
                break;
            case UserEnvType.Park:
                envDetectionNotification.texture = imgEnvPark;
                break;
            case UserEnvType.Transport:
                envDetectionNotification.texture = imgEnvTransport;
                break;
            case UserEnvType.Supermarket:
                envDetectionNotification.texture = imgEnvSupermarket;
                break;
        }

        DisplayEnvDetectionUI();
    }

    private IEnumerator DisplayEnvDetectionUI()
    {
        if (isEnvDetectionUIChanging) yield break;
        
        envDetectionNotification.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        
        envDetectionNotification.gameObject.SetActive(false);
        yield break;
    }
}


// TODO: add left pinch to switch between user env type 
