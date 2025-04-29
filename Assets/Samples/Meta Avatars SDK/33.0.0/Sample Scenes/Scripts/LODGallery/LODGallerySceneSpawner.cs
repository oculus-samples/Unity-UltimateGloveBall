#nullable enable

using UnityEngine;
using Oculus.Avatar2;
using UnityEngine.Serialization;
using System.Collections;
using UnityEngine.XR;

[RequireComponent(typeof(LODGallerySceneOrganizer))]
public class LODGallerySceneSpawner : MonoBehaviour
{
    private GameObject[][]? _containers;
    private const string LOG_SCOPE = "LODGalleryScene";
    private static LODGallerySceneSpawner? s_instance;
    private const float DELAY_BETWEEN_SPAWNS = 0.5f;

    [Header("Tracking Input")]
    [SerializeField]
    [FormerlySerializedAs("_bodyTracking")]
    private OvrAvatarInputManagerBehavior? _inputManager;

    [FormerlySerializedAs("_faceTrackingBehavior")]
    [SerializeField]
    private OvrAvatarFacePoseBehavior? _facePoseBehavior;

    [FormerlySerializedAs("_eyeTrackingBehavior")]
    [SerializeField]
    private OvrAvatarEyePoseBehavior? _eyePoseBehavior;

    [SerializeField]
    private OvrAvatarLipSyncBehavior? _lipSync;

    [SerializeField]
    private bool _displayLODLabels = true;

    [SerializeField]
    private GameObject? labelPrefab;

    [SerializeField]
    private Vector3 lodLabelOffset = new Vector3(0.4f, 0, 0);

    public static LODGallerySceneSpawner? Instance => s_instance;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            s_instance = this;
        }
    }

    private void Start()
    {
        _containers = GetComponent<LODGallerySceneOrganizer>().GetArrangedGameObjects();
        StartSpawning();
    }

    private void StartSpawning()
    {
        StartCoroutine(PopulateAvatarsOfType(0, LODGalleryUtils.LODGalleryAvatarType.Standard));
        StartCoroutine(PopulateAvatarsOfType(1, LODGalleryUtils.LODGalleryAvatarType.Light));
        StartCoroutine(PopulateAvatarsOfType(2, LODGalleryUtils.LODGalleryAvatarType.UltraLight));
    }

    private IEnumerator PopulateAvatarsOfType(int row, LODGalleryUtils.LODGalleryAvatarType avatarType)
    {
        if (_containers is not null)
        {
            for (int lodLevel = 0; lodLevel < 5; lodLevel++)
            {
                GameObject currentContainer = _containers[row][lodLevel];

                OvrAvatarEntity? entity = null;

                switch (avatarType)
                {
                    case LODGalleryUtils.LODGalleryAvatarType.UltraLight:
                        // TODO: T192538677
                        // if (lodLevel == 2 || lodLevel == 4)
                        // {
                        //     currentContainer = _containers[row][lodLevel == 2 ? 0 : 1];
                        //     entity = currentContainer.AddComponent<LODGallerySceneFastLoadAvatarEntity>();
                        //     currentContainer.name = $"UltraLight Avatar LOD {lodLevel}";
                        // }
                        break;
                    case LODGalleryUtils.LODGalleryAvatarType.Light:
                        entity = currentContainer.AddComponent<LODGallerySceneAvatarEntity>();
                        currentContainer.name = $"Light Avatar LOD {lodLevel}";
                        break;
                    case LODGalleryUtils.LODGalleryAvatarType.Standard:
                        entity = currentContainer.AddComponent<LODGallerySceneHQAvatarEntity>();
                        currentContainer.name = $"Standard Avatar LOD {lodLevel}";
                        break;
                    default:
                        OvrAvatarLog.LogWarning("Invalid Avatar type.");
                        yield break;
                }

                if (entity != null)
                {
                    AddTrackingInputs(entity);
                    AlignAvatarEntityOrientation(entity);
                    SetAvatarLODLevel(entity, lodLevel);

                    if (_displayLODLabels)
                    {
                        AlignLODLabels(entity, lodLevel);
                    }
                }

                OvrAvatarLog.LogInfo("LODGallerySceneSpawner spawned " + currentContainer.name, LOG_SCOPE, this);

                yield return new WaitForSeconds(DELAY_BETWEEN_SPAWNS);
            }
        }
        else
        {
            OvrAvatarLog.LogError("No container found");
            yield return new WaitForSeconds(DELAY_BETWEEN_SPAWNS);
        }
    }

    private void AddTrackingInputs(OvrAvatarEntity entity)
    {
        entity.SetInputManager(_inputManager);
        entity.SetFacePoseProvider(_facePoseBehavior);
        entity.SetEyePoseProvider(_eyePoseBehavior);
        entity.SetLipSync(_lipSync);
    }

    private void SetAvatarLODLevel(OvrAvatarEntity entity, int lodLevel)
    {
        entity.AvatarLOD.overrideLOD = true;
        entity.AvatarLOD.overrideLevel = lodLevel;
    }

    private void AlignLODLabels(OvrAvatarEntity entity, int lodLevel)
    {
        // add label prefab
        if (labelPrefab is null)
        {
            OvrAvatarLog.LogError("Failed to align LOD labels, no label prefab found");
            return;
        }
        GameObject label = Instantiate(labelPrefab, entity.gameObject.transform);
        label.transform.localPosition = Vector3.up + lodLabelOffset;

        if (OvrAvatarUtility.IsHeadsetActive())
        {
            // fix label text rotation in headset
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            label.transform.localRotation = Quaternion.identity;
        }

        TextMesh textMesh = label.GetComponent<TextMesh>();

        if (textMesh)
        {
            textMesh.text = lodLevel.ToString();
        }
    }

    // Rotate avatars 180 degrees if in editor (and not PC VR),
    // so that avatars are facing the correct direction.
    private void AlignAvatarEntityOrientation(OvrAvatarEntity entity)
    {
#if UNITY_EDITOR
        if (XRSettings.loadedDeviceName != "oculus display")
        {
            entity.transform.Rotate(Vector3.up, 180f);
        }
#endif
    }
}
