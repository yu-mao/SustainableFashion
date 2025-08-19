using System;
using System.Collections.Generic;

using BodyPix;

using UnityEngine;

public class ClothesOverlayController : MonoBehaviour
{
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;

    public List<ClothesPreset> clothesPresetsDatabase = new List<ClothesPreset>();
    public Dictionary<UserEnvType, List<ClothesPreset>> clothesPresetsDictionary = new Dictionary<UserEnvType, List<ClothesPreset>>();

    public UserEnvType currentUserEnvType;
    public int currentMoodID;

    private void Start()
    {
        envDetectionController.OnUserEnvChanged += ChangeClothesOverlayWrapper;

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

    // button a changes env, button b changes mood-or though the list
    // get new items but disable-delete the old ones!! attach clothes to a parent when generated to delete
    // start with first list item from envtype
    private void ChangeClothesOverlayWrapper(UserEnvType envType)
    {
        Debug.Log("~~~ detected user env type: " + envType);
        GetClothesOverlay(envType);
    }

    private void GetClothesOverlay(UserEnvType envType)
    {

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
    [Range(0f, 1f)] public float minScore = 0.25f;

    [Header("Prefab & Placement")]
    public GameObject prefab;              // your 3D object (duck/wing/etc.)
    public Transform worldParent;          // default = canvasCamera.transform
    public float worldDepthMeters = 1.6f;  // distance along camera forward
    public Vector3 worldLocalOffset = Vector3.zero; // small tweak in local space after placement
    public Vector3 worldLocalScale = Vector3.one;

    public enum RotationMode { None, FaceCamera, FaceCameraPlusScreenRoll, LookAtOtherKeypoint }
    [Header("Rotation")]
    public RotationMode rotationMode = RotationMode.FaceCamera;
    public Body.KeypointID otherKeypoint = Body.KeypointID.RightShoulder; // used for roll/look-at
    public float rotationOffsetDeg = 0f;

}