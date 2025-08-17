using System.Collections;
using UnityEngine;

public class EnvDetectionController : MonoBehaviour
{
    public bool IsDetectingEnvironment { get; set; }

    [SerializeField] private float webcamDetectionTimeInterval = 0.5f;
    [SerializeField] private WebcamController webcamController;

    private void Start()
    {
        IsDetectingEnvironment = true;
        
        StartCoroutine(KeepQueringWebcamImage());
    }

    private IEnumerator KeepQueringWebcamImage()
    {
        while (IsDetectingEnvironment)
        {
            GetWebcamImage();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void GetWebcamImage()
    {
        Debug.Log("Getting webcam image");
    }
}
