#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Avatar2;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISceneSwitcher : MonoBehaviour
{
    private const string logScope = "UISceneSwitcher";

    private List<string>? _sceneNames;
    private List<Button>? _sceneButtons;
    private int _currentButtonIndex = 0;
    [SerializeField] private UISampleSceneInfo[]? sceneInfos;
    private UISampleSceneInfo? _currentSceneInfo;
    private string? _currentSceneName;
    private void Awake()
    {
        if (sceneInfos == null || sceneInfos.Length < 1)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::Awake : Empty 'Scene Infos'. This could be due to an empty field from the inspector.", logScope);
            return;
        }
        _sceneNames = new List<string>();
        _sceneButtons = new List<Button>();

        _currentSceneName = SceneManager.GetActiveScene().name;
    }

    private void SetActiveScene(Scene scene, LoadSceneMode loadSceneMode)
    {
        SceneManager.SetActiveScene(scene);
        _currentSceneName = scene.name;
        if (UIManager.Instance == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::SetActiveScene Could not retrieve UIManager instance.", logScope);
            return;
        }
        UIManager.Instance.RemoveOnSceneLoadedEvent(SetActiveScene);
    }

    public List<string>? GetScenes()
    {
        if (_sceneNames != null)
        {
            return _sceneNames;
        }
        OvrAvatarLog.LogError("UISceneSwitcher::GetScenes Could not retrieve scenes list from UISceneSwitcher.", logScope);
        return null;
    }

    public string GetCurrentSceneName()
    {
        if (_currentSceneName == null)
        {
            return "INVALID_SCENE";
        }

        return _currentSceneName;
    }

    public string GetCurrentSceneDescription()
    {
        if (sceneInfos == null || sceneInfos.Length < 1)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::GetCurrentSceneDescription : Empty 'Scene Infos'. This could be due to an empty field from the inspector.", logScope);
            return "Could not retrieve current scene's description.";
        }

        foreach (var sceneInfo in sceneInfos)
        {
            if (sceneInfo.sceneName == _currentSceneName)
            {
                if (sceneInfo.sceneDescription == null)
                {
                    OvrAvatarLog.LogError($"UISceneSwitcher::GetCurrentSceneDescription : Null exception in retrieving scene description for {sceneInfo.sceneName}.", logScope);
                    return "Could not retrieve current scene's description.";
                }
                return sceneInfo.sceneDescription;
            }
        }
        return "Could not retrieve current scene's description.";
    }

    private void ResetButtonIndex()
    {
        _currentButtonIndex = 0;
        SelectButtonFromIndex(_currentButtonIndex);
    }

    public void OnSectionEnable()
    {
        UIInputController.SetUISubMenuNavigationEnabled(true);
        ResetButtonIndex();
    }

    public void OnSectionDisable()
    {
        ResetButtonIndex();
    }

    public void Initialize()
    {
        PopulateScenes();
        if (UIManager.Instance == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::Initialize : No UIManager instance found.", logScope);
            return;
        }

        if (!UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::Initialize : No UIManager not initialized.", logScope);
            return;
        }

        UIManager.Instance.AddOnResumeEvent(ResetButtonIndex);
    }

    private void PopulateScenes()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        if (sceneCount < 1)
        {
            OvrAvatarLog.LogWarning("UISceneSwitcher::PopulateScenes : no scenes found in the build.", logScope);
            return;
        }

        if (_sceneNames == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::PopulateScenes : empty scene names.", logScope);
            return;
        }

        for (var i = 0; i < sceneCount; i++)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            // skip environment scenes
            if (OvrAvatarUtility.IsScenePathAnEnvironment(scenePath))
            {
                continue;
            }
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            _sceneNames.Add(sceneName);
        }
        CreateButtons();
        ResetButtonIndex();
    }

    private void CreateButtons()
    {
        if (_sceneNames == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::CreateButtons : No scenes found.", logScope);
            return;
        }
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::CreateButtons : UIManager instance not found.", logScope);
            return;
        }

        var layoutGroup = UIManager.Instance.GetScenesPanel()?.GetComponentInChildren<GridLayoutGroup>();
        var parent = layoutGroup != null ? layoutGroup.gameObject : null;
        if (parent != null)
        {
            foreach (var sceneName in _sceneNames)
            {
                CreateButtonForScene(sceneName, parent);
            }
        }
    }

    private void SelectButtonFromIndex(int index)
    {
#if UNITY_EDITOR
        if (!OvrAvatarUtility.IsHeadsetActive())
        {
            // In this situation, we're using a mouse and don't need to "Select" a button.
            // This prevents weird behavior when OnPause/OnResume is invoked from UIManager
            return;
        }
#endif
        if (_sceneButtons == null || _sceneButtons.Count < 1)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::SelectButtonFromIndex : No scene buttons found.", logScope);
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        _sceneButtons[index].Select();
    }

    private void LoadScene(string sceneName)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::LoadScene : UIManager instance not found.", logScope);
            return;
        }
        UIManager.Instance.Resume();
        UIManager.Instance.InvokeOnLoadScene();
        UIManager.Instance.AddOnSceneLoadedEvent(SetActiveScene);

        OvrAvatarLog.LogInfo($"UISceneSwitcher::LoadScene : Loading scene '{sceneName}'", logScope);

        var windowController = UIManager.Instance.GetUIWindowController();

        if (windowController == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::LoadScene : Failed to retrieve UIWindowController from UIManager.", logScope);
            return;
        }

        SceneManager.sceneLoaded += windowController.UpdateSceneNameText;

        var uiLogger = UIManager.Instance.GetUILogger();
        if (uiLogger == null)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateLogSection : Failed to retrieve UILogger from UIManager.", logScope);
            return;
        }

        uiLogger.DeactivateUILogger();
        uiLogger.DetachUILogger();

        var managerGameObject = OvrAvatarManager.Instance.gameObject;
        OvrAvatarManager.ResetInstance();
        Destroy(managerGameObject);
#if USING_XR_SDK
        OvrPlatformInit.ResetOvrPlatformInitState();
#endif
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void CreateButtonForScene(string sceneName, GameObject parent)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::CreateButtonForScene : UIManager instance not found.", logScope);
            return;
        }

        var buttonGameObject = UIManager.Instance.CreateButtonForUI(parent.transform,
            $"Button_Scenes_{sceneName}",
            sceneName,
            () =>
            {
                LoadScene(sceneName);
            });

        if (buttonGameObject == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::CreateButtonForScene : Error in creating UI button game object.", logScope);
            return;
        }

        Button button = buttonGameObject.GetComponent<Button>();

        if (button == null)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::CreateButtonForScene : Created button GameObject has no <Button> component", logScope);
            return;
        }

        _sceneButtons?.Add(button);
    }

    public void SelectNextSceneButton()
    {
        if (_sceneButtons == null || _sceneButtons.Count == 0)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::SelectNextSceneButton : No scene buttons found.", logScope);
            return;
        }

        if (++_currentButtonIndex >= _sceneButtons.Count)
        {
            _currentButtonIndex = 0;
        }

        SelectButtonFromIndex(_currentButtonIndex);
    }

    public void SelectPreviousSceneButton()
    {
        if (_sceneButtons == null || _sceneButtons.Count == 0)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::SelectPreviousSceneButton : No scene buttons found.", logScope);
            return;
        }

        if (--_currentButtonIndex < 0)
        {
            _currentButtonIndex = _sceneButtons.Count - 1;
        }

        SelectButtonFromIndex(_currentButtonIndex);
    }

    public void LoadSelectedScene()
    {
        if (_sceneButtons == null || _sceneButtons.Count == 0 || _sceneNames == null || _sceneNames.Count == 0)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::LoadSelectedScene : No scene buttons or names found.", logScope);
            return;
        }

        if (_currentButtonIndex < 0 || _currentButtonIndex >= _sceneButtons.Count)
        {
            OvrAvatarLog.LogError("UISceneSwitcher::LoadSelectedScene : Current button index is out of range.", logScope);
            return;
        }

        LoadScene(_sceneNames[_currentButtonIndex]);
    }
}
