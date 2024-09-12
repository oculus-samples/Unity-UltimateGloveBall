/**
 *
 * AvatarLODOverride class overrides the Level of Detail (LOD) settings for avatars.
 * It provides input-controlled functions to increase or reduce the LOD level of an avatar in runtime.
 *
 * Usage:
 * - Add this script to any OvrAvatarEntity
 * - Use input controls (or public functions) to increase/reduce the Avatar's LOD level.
 *
 */

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System;
using Oculus.Avatar2;
using UnityEngine;

[RequireComponent(typeof(OvrAvatarEntity))]
public class AvatarLODOverride : MonoBehaviour
{
    [Serializable]
    public struct InputMask
    {
        public OVRInput.Controller controller;
        public OVRInput.Button button;
    }

#if USING_XR_SDK
    [Header("Controller Input")]
    [SerializeField]
    private InputMask increaseLODLevelButton = new InputMask
        { controller = OVRInput.Controller.RTouch, button = OVRInput.Button.PrimaryThumbstick };

    [SerializeField]
    private InputMask decreaseLODLevelButton = new InputMask
        { controller = OVRInput.Controller.LTouch, button = OVRInput.Button.PrimaryThumbstick };
#endif

    [SerializeField]
    [Tooltip("Adds position offset to the AvatarLOD debug Label.\n" +
             "Works when AvatarLODManager::displayLODLabels is enabled.")]
    private Vector3 displayLODLabelOffset = new Vector3(0.5f, 1.0f, 0.0f);

#if UNITY_EDITOR
    [Header("Keyboard Input")]
    [Tooltip("Keyboard Debug controls only work inside Unity Editor.")]
    [SerializeField]
    private bool useKeyboardDebug = false;

    [SerializeField]
    private KeyCode increaseLODLevelKeyboard = KeyCode.G;

    [SerializeField]
    private KeyCode decreaseLODLevelKeyboard = KeyCode.F;
#endif

    private OvrAvatarEntity _avatarEntity;

    private AvatarLODManager _avatarLODManager;

    private void Awake()
    {
        if (!TryGetComponent(out _avatarEntity))
        {
            OvrAvatarLog.LogError($"AvatarLODOverride failed to get Avatar entity for {name}");
            return;
        }

        _avatarEntity.AvatarLOD.overrideLOD = true;
    }

    private void Start()
    {
        if (AvatarLODManager.hasInstance)
        {
            _avatarLODManager = AvatarLODManager.Instance;
        }
        CheckDebugLabel();
    }

    private void OverrideAvatarLODWithOffset(int offset)
    {
        int currentLODLevel;
        if (IsLODOverrideEnabled())
        {
            currentLODLevel = _avatarEntity.AvatarLOD.overrideLevel;
        }
        else
        {
            currentLODLevel = _avatarEntity.AvatarLOD.Level;
            _avatarEntity.AvatarLOD.overrideLOD = true;
        }

        int overrideLODLevel = Mathf.Clamp(currentLODLevel + offset,
            _avatarEntity.AvatarLOD.minLodLevel,
            _avatarEntity.AvatarLOD.maxLodLevel);

        _avatarEntity.AvatarLOD.overrideLevel = overrideLODLevel;

        if (overrideLODLevel != currentLODLevel)
        {
            OvrAvatarLog.LogInfo(
                $"AvatarEntity {_avatarEntity.name} LOD Changed from {currentLODLevel} to {overrideLODLevel}");
        }

        CheckDebugLabel();
    }

    private void CheckDebugLabel()
    {
        if (_avatarLODManager &&
            _avatarLODManager.debug.displayLODLabels)
        {
            _avatarLODManager.debug.displayLODLabelOffset = displayLODLabelOffset;
            _avatarEntity.AvatarLOD.UpdateDebugLabel();
        }
    }

#if UNITY_EDITOR
    public void EnableKeyboardDebug()
    {
        useKeyboardDebug = true;
    }
#endif

    public void IncreaseLODLevel()
    {
        OverrideAvatarLODWithOffset(1);
    }

    public void DecreaseLODLevel()
    {
        OverrideAvatarLODWithOffset(-1);
    }

    public bool IsLODOverrideEnabled()
    {
        return _avatarEntity.AvatarLOD.overrideLOD;
    }

    private void Update()
    {
#if USING_XR_SDK
        if (OVRInput.GetDown(increaseLODLevelButton.button, increaseLODLevelButton.controller))
        {
            IncreaseLODLevel();
        }

        if (OVRInput.GetDown(decreaseLODLevelButton.button, decreaseLODLevelButton.controller))
        {
            DecreaseLODLevel();
        }
#endif
#if UNITY_EDITOR
        if (useKeyboardDebug)
        {
            if (Input.GetKeyDown(increaseLODLevelKeyboard))
            {
                IncreaseLODLevel();
            }

            if (Input.GetKeyDown(decreaseLODLevelKeyboard))
            {
                DecreaseLODLevel();
            }
        }
#endif
    }
}
