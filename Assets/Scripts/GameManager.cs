using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<Dictionary<UserEnvType, List<GameObject>>> ChangeClothesOverlay; 
    
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;

    [Header("Clothes Overlay reference")] 
    [SerializeField] private List<GameObject> officeClothesOverlayA;
    [SerializeField] private List<GameObject> officeClothesOverlayB;

    
    [Header("Debug Reference")]
    [SerializeField] private OVRHand leftHand;

    private void Start()
    {
        envDetectionController.OnUserEnvChanged += ChangeClothesOverlayWrapper;
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        // if (leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            envDetectionController.ChangeUserEnv();
    }

    private void ChangeClothesOverlayWrapper(UserEnvType envType)
    {
        Debug.Log("~~~ detected user env type: " + envType);
        GetClothesOverlay(envType);
    }

    private void GetClothesOverlay(UserEnvType envType)
    {
        // TODO: get clothes prefab according to given envType 
        List<GameObject> chosenClothesOverlay = officeClothesOverlayA;

        ChangeClothesOverlay?.Invoke(new Dictionary<UserEnvType, List<GameObject>>()
            { { envType, chosenClothesOverlay } });
    }
}
