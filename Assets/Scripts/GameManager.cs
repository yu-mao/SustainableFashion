using System;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<Dictionary<UserEnvType, List<GameObject>>> ChangeClothesOverlay; 
    
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;

    [Header("Clothes Overlay reference")] 
    [SerializeField] private List<GameObject> officClothesOverlayA;
    [SerializeField] private List<GameObject> officClothesOverlayB;

    
    [Header("Debug Reference")]
    [SerializeField] private Hand leftHand;

    private void Start()
    {
        envDetectionController.OnUserEnvChanged += ChangeClothesOverlayWrapper;
    }

    // private void Update()
    // {
    //     if (leftHand.GetFingerIsPinching(HandFinger.Index))
    //         envDetectionController.ChangeUserEnv();
    // }

    private void ChangeClothesOverlayWrapper(UserEnvType envType)
    {
        Debug.Log("~~~ detected user env type: " + envType);
        GetClothesOverlay(envType);
    }

    private void GetClothesOverlay(UserEnvType envType)
    {
        // TODO: get clothes prefab according to given envType 
        List<GameObject> chosenClothesOverlay = officClothesOverlayA;

        ChangeClothesOverlay?.Invoke(new Dictionary<UserEnvType, List<GameObject>>()
            { { envType, chosenClothesOverlay } });
    }
}
