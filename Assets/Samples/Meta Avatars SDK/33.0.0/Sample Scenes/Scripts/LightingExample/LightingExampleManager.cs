#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

// Before enabling, see note below in OnSceneLoaded:
// #define USE_TETRAHEDRALIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using Oculus.Avatar2;
using OVR.OpenVR;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 * Main logic for the LightingExample scene
 */
public class LightingExampleManager : MonoBehaviour, IUIControllerInterface
{
    [System.Serializable]
    public class SdkManagerWithDeprecationStatus
    {
        private enum DeprecationStatus
        {
            Recommended,
            Deprecated,
            ReferenceOnly,
        }

        private string[] DeprecationStatusString =
        {
            "Recommended",
            "Deprecated",
            "ReferenceOnly",
        };

        [SerializeField]
        public OvrAvatarManager? manager;

        [SerializeField]
        private DeprecationStatus status;

        public string GetDeprecationStatus()
        {
            return DeprecationStatusString[(int)status];
        }
    }

    [Serializable]
    private struct LightingConfig
    {
        public string Name;

        // sorry but this doesn't work because the scene is not Serializable:
        // public Scene LightingScene;
        // Instead we have to manage the scene name:
        public string SceneName;

        [ColorUsage(false, true)] public Color AmbientColor;

        public Material SkyboxMaterial;
        [NonSerialized] public Button Button;

        public LightingConfig(string name, string sceneName, Color ambientColor, Button button, Material skyboxMaterial)
        {
            Name = name;
            SceneName = sceneName;
            AmbientColor = ambientColor;
            SkyboxMaterial = skyboxMaterial;
            Button = button;
        }
    }

    [SerializeField] private LightingConfig[]? _lightingConfigs;
    [SerializeField] private SdkManagerWithDeprecationStatus[]? _sdkManagers;

    // these do not support URP.
    [SerializeField] private SdkManagerWithDeprecationStatus[]? _sdkManagersSupportingBuiltInPipelineOnly;

    [SerializeField] private GameObject? _avatarsShowcasePrefab;
    [SerializeField] private List<string> _preloadZipFiles = new List<string>();

    [Header("UI")][SerializeField] private Canvas? _canvas;
    [SerializeField] private Transform? _lightingNameGroup;
    [SerializeField] private Button? _lightingButton;
    [SerializeField] private Transform? _shaderNameGroup;
    [SerializeField] private Button? _shaderButton;
    [SerializeField] private Text? _infoText;

    [Header("LOD Override")]

    [Tooltip("Enable overriding Avatar LOD levels using controller/keyboard input.")]
    [SerializeField] private bool _enableLODOverride = true;

    [Tooltip("Enable using keyboard buttons for increasing/decreasing LOD levels. " +
             "Default buttons:\nG to increase LOD levels\nF to decrease LOD levels.")]
    [SerializeField]
    private bool _keyboardDebugLODLevels;

    [Tooltip("Display LOD Levels next to each Avatar (see AvatarLODManager::displayLODLabels).")]
    [SerializeField]
    private bool _displayLODLabels = false;

    private static int _currentLighting;
    private static int _currentShader;

    [Tooltip("Enable shader selection using controller/keyboard input.")]
    [SerializeField] private bool _enableShaderSelection;

    //    private GameObject _instantiatedLightRig;
    [SerializeField]
    private String? _currentSceneName;
    private bool _isPaused;

    private const string logScope = "LightingExampleManager";

#if USING_XR_SDK
    private readonly UIInputControllerButton _prevLightConfigButton = new UIInputControllerButton
    {
        button = OVRInput.Button.One,
        controller = OVRInput.Controller.LTouch,
        description = "Change to previous Light config.",
        scope = logScope
    };
    private readonly UIInputControllerButton _nextLightConfigButton = new UIInputControllerButton
    {
        button = OVRInput.Button.Two,
        controller = OVRInput.Controller.LTouch,
        description = "Change to next Light config.",
        scope = logScope
    };
    private readonly UIInputControllerButton _prevShaderButton = new UIInputControllerButton
    {
        button = OVRInput.Button.One,
        controller = OVRInput.Controller.RTouch,
        description = "Change to previous Shader config.",
        scope = logScope
    };
    private readonly UIInputControllerButton _nextShaderButton = new UIInputControllerButton
    {
        button = OVRInput.Button.Two,
        controller = OVRInput.Controller.RTouch,
        description = "Change to next Shader config.",
        scope = logScope
    };
#endif

    private void Start()
    {
#if USING_XR_SDK
        // Show in world space in VR
        if (_canvas != null)
        {
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.transform.position = new Vector3(0, 1, -1);
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(900, 400);
            // Adjust WorldSpace canvas size, since changing renderMode to WorldSpace
            // places an oversized canvas in the world
            _canvas.transform.localScale = Vector3.one * 0.005f;
        }
        else
        {
            OvrAvatarLog.LogError("No Canvas found", logScope);
        }

#endif

        if (!OvrAvatarShaderDeprecationManager.IsURPEnabled() &&
            _sdkManagers != null &&
            _sdkManagersSupportingBuiltInPipelineOnly != null)
        {
            int originalLength = _sdkManagers?.Length ?? 0;
            Array.Resize(ref _sdkManagers, originalLength + _sdkManagersSupportingBuiltInPipelineOnly.Length);
            Array.Copy(_sdkManagersSupportingBuiltInPipelineOnly, 0, _sdkManagers, originalLength, _sdkManagersSupportingBuiltInPipelineOnly.Length);
        }


        if (_sdkManagers != null)
        {
            foreach (var sdkManager in _sdkManagers)
            {
                if (sdkManager.manager != null)
                {
                    sdkManager.manager.PreloadZipFiles =
                        sdkManager.manager.PreloadZipFiles.Union(_preloadZipFiles).ToList();
                }
            }
        }

        SetupUI();

        if (_sdkManagers != null)
        {

            if (_sdkManagers[_currentShader].manager != null)
            {
                Instantiate(_sdkManagers[_currentShader].manager);
            }
            else
            {
                OvrAvatarLog.LogError($"No SDK Manager found for shader: {_sdkManagers[_currentShader]}", logScope);
            }
        }
        else
        {
            OvrAvatarLog.LogError("No SDK Managers found", logScope);
        }

        if (_lightingConfigs != null)
        {
            var sceneNames = _lightingConfigs.Select(config => config.SceneName).ToArray();
            var lightingScenesAvailable = AreAllLightingScenesAvailable(sceneNames);
            if (lightingScenesAvailable)
            {
                SetLightingConfig(_currentLighting);
            }
            else
            {
                var errorMsg =
                    "Unable to load environments. Please use MetaAvatarsSDK => Lighting Example => Add Environments, or manually include the following scenes in your build under File => Build Settings: " +
                    string.Join(", ", sceneNames) + ".";
                OvrAvatarLog.LogError(errorMsg);
                if (_infoText != null)
                {
                    _infoText.text = errorMsg;
                    _infoText.color = Color.red;
                }
                else
                {
                    OvrAvatarLog.LogError("No Info Text found. Error message is: " + errorMsg, logScope);
                }
            }
        }
        else
        {
            OvrAvatarLog.LogError("No Lighting Configs found", logScope);
        }

        if (_avatarsShowcasePrefab == null)
        {
            OvrAvatarLog.LogError("No Avatars showcase prefab found", logScope);
            return;
        }

        GameObject showcase = Instantiate(_avatarsShowcasePrefab);
        showcase.transform.SetPositionAndRotation(transform.localPosition, transform.localRotation);


        if (_enableLODOverride)
        {
            AddLODOverrideToChildren(showcase);
        }

#if USE_TETRAHEDRALIZATION
        LightProbes.needsRetetrahedralization += conductTetrahedralization;
#endif

        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogWarning("LightingExampleManager::Start : Could not retrieve UIManager instance.");
            return;
        }

        UIManager.Instance.AddOnLoadSceneEvent(HandleOnSceneWillLoad);
        UIManager.Instance.AddOnPauseEvent(HandleOnSceneWillPause);
        UIManager.Instance.AddOnResumeEvent(HandleOnSceneWillResume);
    }

    private void HandleOnSceneWillLoad()
    {
        ResetLightsAndShaders();
    }

    private void HandleOnSceneWillPause()
    {
        _isPaused = true;
    }

    private void HandleOnSceneWillResume()
    {
        _isPaused = false;
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.Initialized)
            {
                UIManager.Instance.RemoveOnResumeEvent(HandleOnSceneWillResume);
                UIManager.Instance.RemoveOnPauseEvent(HandleOnSceneWillPause);
                UIManager.Instance.RemoveOnLoadSceneEvent(HandleOnSceneWillLoad);
            }
        }

#if USE_TETRAHEDRALIZATION
        LightProbes.needsRetetrahedralization -= conductTetrahedralization;
#endif
    }

    private void AddLODOverrideToChildren(GameObject showcaseGameObject)
    {
        if (showcaseGameObject == null)
        {
            return;
        }

        if (_displayLODLabels)
        {
            AvatarLODManager.Instance.debug.displayLODLabels = true;
        }

        OvrAvatarEntity[] entities = showcaseGameObject.GetComponentsInChildren<OvrAvatarEntity>(true);

        if (entities == null || entities.Length == 0)
        {
            return;
        }

        foreach (var entity in entities)
        {
            AddLODOverrideToEntity(entity);
        }
    }

    private void AddLODOverrideToEntity(OvrAvatarEntity entity)
    {
        if (entity.GetComponent<AvatarLODOverride>())
        {
            return;
        }

        entity.gameObject.AddComponent<AvatarLODOverride>();

#if UNITY_EDITOR
        if (_keyboardDebugLODLevels)
        {
            entity.GetComponent<AvatarLODOverride>().EnableKeyboardDebug();
        }
#endif
    }

    private void Update()
    {
#if USING_XR_SDK
        if (_isPaused)
        {
            return;
        }
        // LightingExampleManager already handles UI pause,
        // so it doesn't need to explicitly check for !UIManager.IsPaused
        if (OVRInput.GetActiveController() != OVRInput.Controller.Hands)
        {
            if (_lightingConfigs != null)
            {
                if (_sdkManagers != null)
                {
                    if (OVRInput.GetUp(_prevLightConfigButton.button, _prevLightConfigButton.controller))
                    {
                        SetLightingConfig(WrapArrayIndex(_currentLighting, -1, _lightingConfigs.Length));
                    }
                    else if (OVRInput.GetUp(_nextLightConfigButton.button, _nextLightConfigButton.controller))
                    {
                        SetLightingConfig(WrapArrayIndex(_currentLighting, 1, _lightingConfigs.Length));
                    }
                    else if (_enableShaderSelection && OVRInput.GetUp(_prevShaderButton.button, _prevShaderButton.controller))
                    {
                        SetShader(WrapArrayIndex(_currentShader, -1, _sdkManagers.Length));
                    }
                    else if (_enableShaderSelection && OVRInput.GetUp(_nextShaderButton.button, _nextShaderButton.controller))
                    {
                        SetShader(WrapArrayIndex(_currentShader, 1, _sdkManagers.Length));
                    }
                }
                else
                {
                    OvrAvatarLog.LogError("No SDK Managers found", logScope);
                }
            }
            else
            {
                OvrAvatarLog.LogError("No lighting configs found", logScope);
            }
        }
#endif
    }

    private void SetupUI()
    {
        if (_lightingConfigs != null && _lightingButton != null && _lightingNameGroup != null)
        {
            for (int i = 0; i < _lightingConfigs.Length; ++i)
            {
                Button button = i == 0 ? _lightingButton : Instantiate(_lightingButton, _lightingNameGroup);
                int lightingIndex = i;
                button.onClick.AddListener(() => SetLightingConfig(lightingIndex));
                _lightingConfigs[i].Button = button;
                button.GetComponentInChildren<Text>().text = _lightingConfigs[i].Name;
            }
        }
        else
        {
            OvrAvatarLog.LogError("No lighting configs, button, or name group found", logScope);
        }

        if (!_enableShaderSelection && _sdkManagers != null && _shaderButton != null && _shaderNameGroup != null)
        {
            Button button = _shaderButton;
            button.onClick.AddListener(() => SetShader(_currentShader));
            Text buttonText = button.GetComponentInChildren<Text>();
            if (_sdkManagers[_currentShader].manager != null)
            {
                buttonText.text = (_sdkManagers[_currentShader].manager?.name ?? "Unknown");
            }
            else
            {
                OvrAvatarLog.LogError($"{_sdkManagers[_currentShader]} has a null manager", logScope);
            }
            button.interactable = false;
            buttonText.fontStyle = FontStyle.Bold;
        }
        else if (_sdkManagers != null && _shaderButton != null && _shaderNameGroup != null)
        {
            for (int i = 0; i < _sdkManagers.Length; ++i)
            {
                Button button = i == 0 ? _shaderButton : Instantiate(_shaderButton, _shaderNameGroup);
                int shaderIndex = i;
                button.onClick.AddListener(() => SetShader(shaderIndex));
                Text buttonText = button.GetComponentInChildren<Text>();
                var deprecationStatus = _sdkManagers[i].GetDeprecationStatus();
                if (_sdkManagers[i].manager != null)
                {
                    var managerName = _sdkManagers[i].manager?.name ?? "Unknown";
                    managerName = managerName.Replace("(deprecated)", string.Empty, StringComparison.OrdinalIgnoreCase);
                    buttonText.text = (managerName) + $" ({deprecationStatus})";
                }
                else
                {
                    OvrAvatarLog.LogError($"{_sdkManagers[i]} has a null manager", logScope);
                }
                button.interactable = i != _currentShader;
                buttonText.fontStyle = i == _currentShader ? FontStyle.Bold : FontStyle.Normal;
            }
        }
        else
        {
            OvrAvatarLog.LogError("No SDK Managers, shader button, or shader name group found", logScope);
        }
    }

    private bool IsSceneLoading()
    {
        return !OvrAvatarManager.hasInstance || OvrAvatarManager.Instance.IsLoadingAvatar || OvrAvatarManager.Instance.IsLoadingResources;
    }

    private static bool AreAllLightingScenesAvailable(string[]? sceneNamesToCheck)
    {
        if (sceneNamesToCheck == null)
        {
            OvrAvatarLog.LogError("Scene Names are null - No lighting configs found", logScope);
            return false;
        }
        string[] allSceneNamesInBuild = new string[SceneManager.sceneCountInBuildSettings];
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            allSceneNamesInBuild[i] = sceneName;
        }

        // Check if all scenes in sceneNamesToCheck exist in allSceneNamesInBuild
        return sceneNamesToCheck.All(name => allSceneNamesInBuild.Contains(name));
    }

    private void SetLightingConfig(int index)
    {
        if (_isPaused)
        {
            return;
        }
        if (IsSceneLoading())
        {
            // Disable lighting config changes during scene switches or when avatars are loading
            return;
        }

        _currentLighting = index;

        if (!String.IsNullOrEmpty(_currentSceneName))
        {
            SceneManager.UnloadSceneAsync(_currentSceneName);
        }
        if (_lightingConfigs != null)
        {
            _currentSceneName = _lightingConfigs[index].SceneName;

            SceneManager.LoadScene(_currentSceneName, LoadSceneMode.Additive);
            if (UIManager.Instance == null)
            {
                OvrAvatarLog.LogError("No UIManager instance found", logScope);
                return;
            }
            UIManager.Instance.AddOnSceneLoadedEvent(OnSceneLoaded);
        }
        else
        {
            OvrAvatarLog.LogError("No lighting configs found", logScope);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_lightingConfigs == null)
        {
            OvrAvatarLog.LogError("No lighting configs found", logScope);
            return;
        }

        RenderSettings.ambientLight = _lightingConfigs[_currentLighting].AmbientColor;
        RenderSettings.skybox = _lightingConfigs[_currentLighting].SkyboxMaterial;
        for (int i = 0; i < _lightingConfigs.Length; ++i)
        {
            _lightingConfigs[i].Button.interactable = i != _currentLighting;
            _lightingConfigs[i].Button.GetComponentInChildren<Text>().fontStyle =
                i == _currentLighting ? FontStyle.Bold : FontStyle.Normal;
        }

        if (UIManager.Instance == null)
        {
            OvrAvatarLog.LogError("No UIManager instance found", logScope);
            return;
        }

#if !USE_TETRAHEDRALIZATION
        // NOTE: after the load, we need to trigger the updating of the light probes to correct ambient lighting.
        // Official documentation for this is here:
        //   https://docs.unity3d.com/Manual/light-probes-and-scene-loading.html
        //   https://docs.unity3d.com/ScriptReference/LightProbes-needsRetetrahedralization.html
        // HOWEVER, calling this function after an additive scene load proves uneffective.
        // For now, it seems that this can only be corrected by a singular scene load, as triggered by SetShader:
        SetShader(_currentShader); // this reload of the shader is needed to restore ambient light to the avatars
#endif

        UIManager.Instance.RemoveOnSceneLoadedEvent(OnSceneLoaded);
    }

#if USE_TETRAHEDRALIZATION
    public void conductTetrahedralization()
    {
        LightProbes.Tetrahedralize();
    }
#endif

    private void SetShader(int index)
    {
        if (_isPaused)
        {
            return;
        }

        if (IsSceneLoading())
        {
            // Disable shader changes during scene switches or when avatars are loading
            return;
        }

        _currentShader = index;

        // Destroy OvrAvatarManager so we can recreate it with the new shader config
        var managerGameObject = OvrAvatarManager.Instance.gameObject;
        OvrAvatarManager.ResetInstance();
        Destroy(managerGameObject);

        // Reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    private void ResetLightsAndShaders()
    {
        _currentShader = 0;
        _currentLighting = 0;
        // Destroy OvrAvatarManager so we can recreate it with the new shader config
        var managerGameObject = OvrAvatarManager.Instance.gameObject;
        Destroy(managerGameObject);

        if (_sdkManagers != null)
        {
            var sdkManager = _sdkManagers[_currentShader].manager;
            if (sdkManager != null)
            {
                Instantiate(sdkManager);
            }
            else
            {
                OvrAvatarLog.LogError($"No SDK Manager found for shader: {_sdkManagers[_currentShader]}", logScope);
            }
        }
        else
        {
            OvrAvatarLog.LogError("No SDK Managers found", logScope);
        }

        if (_lightingConfigs != null)
        {
            RenderSettings.ambientLight = _lightingConfigs[_currentLighting].AmbientColor;
        }
        else
        {
            OvrAvatarLog.LogError("No lighting configs found", logScope);
        }
    }

    private static int WrapArrayIndex(int current, int offset, int length)
    {
        return (current + offset + length) % length;
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var buttons = new List<UIInputControllerButton>
        {
            _prevLightConfigButton,
            _prevShaderButton,
            _nextLightConfigButton,
            _nextShaderButton,
        };

        return buttons;
    }
#endif
}
