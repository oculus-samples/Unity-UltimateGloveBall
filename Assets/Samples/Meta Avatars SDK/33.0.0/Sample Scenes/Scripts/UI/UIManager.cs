#nullable enable

using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(UICanvasController), typeof(UIWindowController))]
[RequireComponent(typeof(UIInputController), typeof(UISceneSwitcher))]
[RequireComponent(typeof(UIInputControllerView), typeof(UILogger))]
[RequireComponent(typeof(UISettingsManager))]
public class UIManager : MonoBehaviour
{
    private const string logScope = "UIManager";

    private UICanvasController? _canvasController;
    private UIWindowController? _windowController;
    private UIInputController? _inputController;
    private UISceneSwitcher? _uiSceneSwitcher;
    private UIInputControllerView? _inputControllerView;
    private UILogger? _uiLogger;
    private UISettingsManager? _uiSettingsManager;
    public bool Initialized { get; private set; }

    private static UIManager? s_instance;

    public static UIManager? Instance => s_instance;

    private float _fixedDeltaTime;
    private bool _isPaused;
    public static bool IsPaused => s_instance != null && s_instance._isPaused;
    private bool _isLoadingScene;

    [SerializeField] private GameObject? overviewPanel;
    [SerializeField] private GameObject? scenesPanel;
    [SerializeField] private GameObject? settingsPanel;
    [SerializeField] private GameObject? controllersPanel;
    [SerializeField] private GameObject? menuGameObject;
    [SerializeField] private GameObject? overlayGameObject;
    [SerializeField] private Text? overlayText;
    [SerializeField] private GameObject? buttonPrefab;
    [SerializeField] private GameObject? unityEventSystemGameObject;

    public delegate void PauseEvent();
    public event PauseEvent? OnPause;
    public event PauseEvent? OnResume;

    public delegate void LoadSceneEvent();

    public event LoadSceneEvent? OnLoadScene;



    private void Awake()
    {
        if (!Initialized)
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                EnableEventSystemGameObject();
                _canvasController = GetComponent<UICanvasController>();
                _windowController = GetComponent<UIWindowController>();
                _inputController = GetComponent<UIInputController>();
                _uiSceneSwitcher = GetComponent<UISceneSwitcher>();
                _inputControllerView = GetComponent<UIInputControllerView>();
                _uiLogger = GetComponent<UILogger>();
                _uiSettingsManager = GetComponent<UISettingsManager>();
                s_instance = this;
                Initialized = true;
                _isPaused = false;
                _fixedDeltaTime = Time.fixedDeltaTime;
                _isLoadingScene = false;
                AddOnLoadSceneEvent(() =>
                {
                    SetIsLoadingScene(true);
                });
                AddOnSceneLoadedEvent((scene, mode) =>
                {
                    SetIsLoadingScene(false);
                });
                if (overlayGameObject != null)
                {
                    overlayGameObject.SetActive(true);
                }

                if (menuGameObject != null)
                {
                    menuGameObject.SetActive(false);
                }
            }
        }
    }

    private void Start()
    {
        if (_uiSceneSwitcher != null)
        {
            _uiSceneSwitcher.Initialize();
        }

        if (_inputControllerView != null)
        {
            AddOnPauseEvent(_inputControllerView.PopulateControllerView);
        }

        var buttonIdentifier = "`\u2630`";

#if UNITY_EDITOR
        if (!OvrAvatarUtility.IsHeadsetActive())
        {
            buttonIdentifier = "'ESC'";
        }
#endif
        if (overlayText != null)
        {
            overlayText.text = $"Press {buttonIdentifier} to toggle Meta Avatars SDK UI";
        }
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0.0f;
        Time.fixedDeltaTime = 0.0f;
        if (overlayGameObject != null)
        {
            overlayGameObject.SetActive(false);
        }

        if (menuGameObject != null)
        {
            menuGameObject.SetActive(true);
        }

        OnPause?.Invoke();
    }

    public void Resume()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = this._fixedDeltaTime * Time.timeScale;
        if (overlayGameObject != null)
        {
            overlayGameObject.SetActive(true);
        }

        if (menuGameObject != null)
        {
            menuGameObject.SetActive(false);
        }

        _isPaused = false;

        OnResume?.Invoke();
    }

    public GameObject? GetScenesPanel()
    {
        if (scenesPanel != null)
        {
            return scenesPanel;
        }

        OvrAvatarLog.LogError("UIManager::GetScenesPanel : No 'Scenes' panel found.", logScope);
        return null;
    }

    public UIWindowController? GetUIWindowController()
    {
        if (_windowController != null)
        {
            return _windowController;
        }

        OvrAvatarLog.LogError("UIManager::GetUIWindowController : No 'UIWindowController' component found.", logScope);
        return null;
    }

    public UIInputController? GetUIInputController()
    {
        if (_inputController != null)
        {
            return _inputController;
        }

        OvrAvatarLog.LogError("UIManager::GetUIInputController : No 'UIInputController' component found.", logScope);
        return null;
    }

    public UISceneSwitcher? GetUISceneSwitcher()
    {
        if (_uiSceneSwitcher != null)
        {
            return _uiSceneSwitcher;
        }

        OvrAvatarLog.LogError("UIManager:: GetUISceneSwitcher : No 'UISceneSwitcher' component found.", logScope);
        return null;
    }

    public UILogger? GetUILogger()
    {
        if (_uiLogger != null)
        {
            return _uiLogger;
        }

        OvrAvatarLog.LogError("UIManager::GetUILogger : No 'UILogger' component found.", logScope);
        return null;
    }

    public GameObject? GetSettingsPanel()
    {
        if (settingsPanel != null)
        {
            return settingsPanel;
        }

        OvrAvatarLog.LogError("UIManager::GetSettingsPanel : No 'Settings' panel found.", logScope);
        return null;
    }

    public UISettingsManager? GetUISettingsManager()
    {
        if (_uiSettingsManager != null)
        {
            return _uiSettingsManager;
        }

        OvrAvatarLog.LogError("UIManager::GetUISettingsManager : No 'UISettingsManager' component found.", logScope);
        return null;
    }

    public GameObject? CreateButtonForUI(Transform parent, string buttonName, string buttonText, UnityAction buttonCallBack)
    {
        if (buttonPrefab == null)
        {
            OvrAvatarLog.LogError("UIManager::CreateButtonPrefab : Null Button Prefab. Make sure the field is set in the inspector.", logScope);
            return null;
        }

        GameObject buttonGameObject = Instantiate(buttonPrefab, parent.transform, false);
        buttonGameObject.name = buttonName;
        buttonGameObject.transform.localScale = Vector3.one;

        Button button = buttonGameObject.GetComponent<Button>();

        if (button == null)
        {
            OvrAvatarLog.LogError("UIManager::CreateButtonPrefab : Button prefab has no <Button> component.", logScope);
            return null;
        }

        button.onClick.AddListener(buttonCallBack);

        Text textComponent = buttonGameObject.GetComponentInChildren<Text>();
        textComponent.text = buttonText;
        textComponent.alignment = TextAnchor.MiddleCenter;
        // textComponent.fontSize = 15;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.resizeTextMinSize = 10;
        textComponent.resizeTextMaxSize = 13;
        textComponent.resizeTextForBestFit = true;

        return buttonGameObject;
    }

    public void ResetUI()
    {
        if (_windowController == null)
        {
            OvrAvatarLog.LogError("UIManager::ResetUI : No 'UIWindowController' component found.", logScope);
            return;
        }

        _windowController.ResetIndex();
    }

    public void AddOnPauseEvent(PauseEvent listener)
    {
        OnPause += listener;
    }

    public void RemoveOnPauseEvent(PauseEvent listener)
    {
        OnPause -= listener;
    }

    public void AddOnResumeEvent(PauseEvent listener)
    {
        OnResume += listener;
    }

    public void RemoveOnResumeEvent(PauseEvent listener)
    {
        OnResume -= listener;
    }

    public void AddOnLoadSceneEvent(LoadSceneEvent listener)
    {
        OnLoadScene += listener;
    }

    public void RemoveOnLoadSceneEvent(LoadSceneEvent listener)
    {
        OnLoadScene -= listener;
    }

    public void InvokeOnLoadScene()
    {
        OnLoadScene?.Invoke();
    }

    public void AddOnSceneLoadedEvent(UnityAction<Scene, LoadSceneMode> listener)
    {
        SceneManager.sceneLoaded += listener;
    }

    public void RemoveOnSceneLoadedEvent(UnityAction<Scene, LoadSceneMode> listener)
    {
        SceneManager.sceneLoaded -= listener;
    }

    public UIWindowController.UISectionType GetActiveSectionType()
    {
        if (_windowController == null)
        {
            OvrAvatarLog.LogError("UIManager::GetActiveSectionType : No 'UIWindowController' component found.", logScope);
            return UIWindowController.UISectionType.Invalid;
        }
        return _windowController.GetActiveSectionType();
    }

    private void SetIsLoadingScene(bool isLoadingScene)
    {
        _isLoadingScene = isLoadingScene;
    }

    public bool IsLoadingScene()
    {
        return _isLoadingScene;
    }

    private void EnableEventSystemGameObject()
    {
        if (unityEventSystemGameObject != null)
        {
            unityEventSystemGameObject.SetActive(true);
        }
    }
}
