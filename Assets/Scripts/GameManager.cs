using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;

    private void Start()
    {
        envDetectionController.OnUserEnvChanged += ChangeUserOutlook;
    }

    private void ChangeUserOutlook(UserEnvType envType)
    {
        Debug.Log("~~~ detected user env type: " + envType);
    }
}
