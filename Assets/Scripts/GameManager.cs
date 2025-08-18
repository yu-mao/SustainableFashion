using Oculus.Interaction.Input;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;

    [Header("Debug Reference")]
    [SerializeField] private Hand leftHand;

    private void Start()
    {
        envDetectionController.OnUserEnvChanged += ChangeUserOutlook;
    }

    private void Update()
    {
        if (leftHand.GetFingerIsPinching(HandFinger.Index))
            envDetectionController.ChangeUserEnv();
    }

    private void ChangeUserOutlook(UserEnvType envType)
    {
        Debug.Log("~~~ detected user env type: " + envType);
    }
}
