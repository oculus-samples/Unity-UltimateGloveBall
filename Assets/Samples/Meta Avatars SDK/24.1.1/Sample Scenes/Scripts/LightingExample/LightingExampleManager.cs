#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System;
using System.Linq;
using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 * Main logic for the LightingExample scene
 */
public class LightingExampleManager : MonoBehaviour
{
    [System.Serializable]
    public class SdkManagerWithDeprecationStatus
    {
        private enum DeprecationStatus
        {
            Recommended,
            Deprecated,
            ReferenceOnly
        }

        private string[] DeprecationStatusString =
        {
            "Recommended",
            "Deprecated",
            "ReferenceOnly"
        };
        [SerializeField]
        public OvrAvatarManager manager;

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
        //        public GameObject LightRigPrefab;
        // sorry but this doesn't work because the scene is not Serializable:
        // public Scene LightingScene;
        // Instead we have to manage the scene name:
        public string SceneName;

        [ColorUsage(false, true)] public Color AmbientColor;
        [NonSerialized] public Button Button;

        public LightingConfig(string name, string sceneName, Color ambientColor, Button button)
        {
            this.Name = name;
            this.SceneName = sceneName;
            this.AmbientColor = ambientColor;
            this.Button = button;
        }
    }

    [SerializeField] private LightingConfig[] _lightingConfigs;
    [SerializeField] private SdkManagerWithDeprecationStatus[] _sdkManagers;
    [SerializeField] private GameObject _avatarsShowcasePrefab;

    [Header("UI")][SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _lightingNameGroup;
    [SerializeField] private Button _lightingButton;
    [SerializeField] private Transform _shaderNameGroup;
    [SerializeField] private Button _shaderButton;
    [SerializeField] private Text _infoText;

    [Header("LOD Override")]

    [Tooltip("Enable overriding Avatar LOD levels using controller/keyboard input.")]
    [SerializeField] private bool _enableLODOverride = true;

    [Tooltip("Enable using keyboard buttons for increasing/decreasing LOD levels. " +
             "Default buttons:\nG to increase LOD levels\nF to decrease LOD levels.")]
    [SerializeField]
    private bool _keyboardDebugLODLevels;

    [Tooltip("Display LOD Levels next to each Avatar (see AvatarLODManager::displayLODLabels).")]
    [SerializeField]
    private bool _displayLODLabels = true;

    private static int _currentLighting;
    private static int _currentShader;

    //    private GameObject _instantiatedLightRig;
    [SerializeField]
    private String _currentSceneName;

    private void Start()
    {
#if USING_XR_SDK
        // Show in world space in VR
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.transform.position = new Vector3(0, 1, -1);
        ((RectTransform)_canvas.transform).sizeDelta = new Vector2(900, 400);
        // Adjust WorldSpace canvas size, since changing renderMode to WorldSpace
        // places an oversized canvas in the world
        _canvas.transform.localScale = Vector3.one * 0.005f;
#endif

        SetupUI();

        Instantiate(_sdkManagers[_currentShader].manager);

        var sceneNames = _lightingConfigs.Select(config => config.SceneName).ToArray();
        var lightingScenesAvailable = AreAllLightingScenesAvailable(sceneNames);

        if (lightingScenesAvailable)
        {
            SetLightingConfig(_currentLighting);
        }
        else
        {
            var errorMsg =
                "Unable to load environments. Please use AvatarSDK2 => Lighting Example => Add Environments, or manually include the following scenes in your build under File => Build Settings: " +
                string.Join(", ", sceneNames) + ".";
            OvrAvatarLog.LogError(errorMsg);
            _infoText.text = errorMsg;
            _infoText.color = Color.red;
        }

        if (_avatarsShowcasePrefab)
        {
            GameObject showcase = Instantiate(_avatarsShowcasePrefab);
            showcase.transform.SetPositionAndRotation(transform.localPosition, transform.localRotation);
        }

        if (_enableLODOverride)
        {
            AddLODOverrideToChildren();
        }
    }

    private void AddLODOverrideToChildren()
    {
        if (_displayLODLabels)
        {
            AvatarLODManager.Instance.debug.displayLODLabels = true;
        }

        OvrAvatarEntity[] entities = GetComponentsInChildren<OvrAvatarEntity>(true);
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
        if (OVRInput.GetActiveController() != OVRInput.Controller.Hands)
        {
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                SetLightingConfig(WrapArrayIndex(_currentLighting, -1, _lightingConfigs.Length));
            }
            else if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            {
                SetLightingConfig(WrapArrayIndex(_currentLighting, 1, _lightingConfigs.Length));
            }
            else if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                SetShader(WrapArrayIndex(_currentShader, -1, _sdkManagers.Length));
            }
            else if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                SetShader(WrapArrayIndex(_currentShader, 1, _sdkManagers.Length));
            }
        }
#endif
    }

    private void SetupUI()
    {
        for (int i = 0; i < _lightingConfigs.Length; ++i)
        {
            Button button = i == 0 ? _lightingButton : Instantiate(_lightingButton, _lightingNameGroup);
            int lightingIndex = i;
            button.onClick.AddListener(() => SetLightingConfig(lightingIndex));
            _lightingConfigs[i].Button = button;
            button.GetComponentInChildren<Text>().text = _lightingConfigs[i].Name;
        }

        for (int i = 0; i < _sdkManagers.Length; ++i)
        {
            Button button = i == 0 ? _shaderButton : Instantiate(_shaderButton, _shaderNameGroup);
            int shaderIndex = i;
            button.onClick.AddListener(() => SetShader(shaderIndex));
            Text buttonText = button.GetComponentInChildren<Text>();
            var deprecationStatus = _sdkManagers[i].GetDeprecationStatus();
            buttonText.text = $"{_sdkManagers[i].manager.name} ({deprecationStatus})";

            button.interactable = i != _currentShader;
            buttonText.fontStyle = i == _currentShader ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    private bool IsSceneLoading()
    {
        return !OvrAvatarManager.hasInstance || OvrAvatarManager.Instance.IsLoadingAvatar || OvrAvatarManager.Instance.IsLoadingResources;
    }

    private static bool AreAllLightingScenesAvailable(string[] sceneNamesToCheck)
    {
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
        _currentSceneName = _lightingConfigs[index].SceneName;

        SceneManager.LoadScene(_currentSceneName, LoadSceneMode.Additive);
        RenderSettings.ambientLight = _lightingConfigs[index].AmbientColor;

        for (int i = 0; i < _lightingConfigs.Length; ++i)
        {
            _lightingConfigs[i].Button.interactable = i != _currentLighting;
            _lightingConfigs[i].Button.GetComponentInChildren<Text>().fontStyle =
                i == _currentLighting ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    private void SetShader(int index)
    {
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

    private static int WrapArrayIndex(int current, int offset, int length)
    {
        return (current + offset + length) % length;
    }
}
