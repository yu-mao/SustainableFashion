using System;
using System.Collections.Generic;

using BodyPix;

using Unity.Multiplayer.Center.Common;

using UnityEngine;

public class ClothesOverlayController : MonoBehaviour
{
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;
    
    public Chest2DPlacer Chest2DPlacer;
    public Keypoint3DPlacer Keypoint3DPlacer;

    public List<ClothesPreset> clothesPresetsDatabase = new List<ClothesPreset>();
    public Dictionary<UserEnvType, List<ClothesPreset>> clothesPresetsDictionary = new Dictionary<UserEnvType, List<ClothesPreset>>();

    public UserEnvType currentUserEnvType;
    public int currentMoodID;
    public ClothesPreset chosenPreset;
    public GameObject generatedObjectsParent;

    public List <GameObject> chestImages = new List<GameObject>();


    private void Start()
    {
        envDetectionController.OnUserEnvChanged += ChangeEnvClothes;

        // populate the dictionary
        foreach (var preset in clothesPresetsDatabase)
        {
            if (!clothesPresetsDictionary.ContainsKey(preset.userEnvType))
            {
                List<ClothesPreset> envClothesPresets = new List<ClothesPreset>{preset};
                clothesPresetsDictionary.Add(preset.userEnvType, envClothesPresets);
            }
            else
            {
                clothesPresetsDictionary[preset.userEnvType].Add(preset);
            }
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            // if (leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            envDetectionController.ChangeUserEnv();

        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            // if (leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            ChangeMood();
    }

    // button a changes env, button b changes mood-or though the list
    // get new items but disable-delete the old ones!! attach clothes to a parent when generated to delete
    // start with first list item from envtype
    private void ChangeEnvClothes(UserEnvType envType)
    {
        DestroyPreviosClothes();

        // find the first clothes from the list
        if (clothesPresetsDictionary.ContainsKey(envType))
        {
            chosenPreset = clothesPresetsDictionary[envType][0];
            currentUserEnvType = envType;
            currentMoodID = 0;
            GenerateNewClothes();
        }
    }

    private void ChangeMood()
    {
        DestroyPreviosClothes();

        if (currentMoodID == 0)
        {
            chosenPreset = clothesPresetsDictionary[currentUserEnvType][1];
            currentMoodID = 1;
        }else// ==1
        {
            chosenPreset = clothesPresetsDictionary[currentUserEnvType][0];
            currentMoodID = 0;
        }

        GenerateNewClothes();
    }

    private void DestroyPreviosClothes()
    {
        //2d
        foreach (var item in chestImages)
        { 
                item.SetActive(false);
        }
        //if (chosenPreset.chest2dDesign!=null)
        //    chosenPreset.chest2dDesign.gameObject.SetActive(false);

        Chest2DPlacer.DemoTestIsAlive = false;

        // 3d 
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(generatedObjectsParent.transform.GetChild(i).gameObject);
        }

    }

    private void GenerateNewClothes()
    {
        //2d
        if (chosenPreset.chest2dDesign)
        {
            Chest2DPlacer.GenerateImageOnChest(chosenPreset.chest2dDesign);
            Chest2DPlacer.DemoTestIsAlive = true;
        }

        //3d
        foreach(Keypoint3dObjectPreset preset in chosenPreset.keypoint3DObjects)
        {
            Keypoint3DPlacer.GeneratePreset(preset);
        }

    }
}


[Serializable]
public class ClothesPreset
{
    public int presetID;
    public UserEnvType userEnvType;
    public RectTransform chest2dDesign;
    public List<Keypoint3dObjectPreset> keypoint3DObjects = new List<Keypoint3dObjectPreset>();
}

[Serializable]
public class Keypoint3dObjectPreset
{
    [Header("Target")]
    public Body.KeypointID targetKeypoint = Body.KeypointID.LeftShoulder;

    [Header("Prefab & Placement")]
    public GameObject prefab;              // your 3D object (duck/wing/etc.)
    public Transform worldParent;          // default = canvasCamera.transform
    public Vector3 worldLocalScale = Vector3.one;

    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.None;
    public Body.KeypointID otherKeypoint = Body.KeypointID.RightShoulder; // used for roll/look-at
    public float rotationOffsetDeg = 0f;

}