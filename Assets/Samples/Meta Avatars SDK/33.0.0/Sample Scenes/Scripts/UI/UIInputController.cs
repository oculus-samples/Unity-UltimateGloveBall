#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System.Collections;
using System.Collections.Generic;
using Oculus.Avatar2;
using UnityEngine;

public class UIInputController : MonoBehaviour, IUIControllerInterface
{
    private const string logScope = "UIInputController";

#if USING_XR_SDK
    private readonly UIInputControllerButton _navigateMenuInput = new()
    {
        axis2d = OVRInput.Axis2D.PrimaryThumbstick,
        controller = OVRInput.Controller.LTouch,
        description = "Move Left-thumbstick left and right to navigate UI tabs",
        scope = logScope
    };
    private readonly UIInputControllerButton _navigateSubMenuInput = new()
    {
        axis2d = OVRInput.Axis2D.PrimaryThumbstick,
        controller = OVRInput.Controller.RTouch,
        description = "Move Right-thumbstick left and right to navigate sub menu items",
        scope = logScope
    };
    private readonly UIInputControllerButton _selectButton = new()
    {
        button = OVRInput.Button.One,
        controller = OVRInput.Controller.RTouch,
        description = "Select the current sub menu item",
        scope = logScope
    };
    private readonly UIInputControllerButton _menuButton = new()
    {
        button = OVRInput.Button.Start,
        controller = OVRInput.Controller.LTouch,
        description = "Toggle Meta Avatars SDK UI",
        scope = logScope
    };
    private readonly UIInputControllerButton _scrollButton = new UIInputControllerButton
    {
        axis2d = OVRInput.Axis2D.PrimaryThumbstick,
        controller = OVRInput.Controller.RTouch,
        description = "Scroll up and down",
        scope = logScope
    };
    private readonly UIInputControllerButton _returnButton = new UIInputControllerButton
    {
        button = OVRInput.Button.Two,
        controller = OVRInput.Controller.RTouch,
        description = "Return to upper settings menu",
        scope = logScope
    };
#endif

#if UNITY_EDITOR
    private readonly KeyCode _editorMenuButton = KeyCode.Escape;
#endif

    private UIWindowController? _uiWindowController;

    private const float MENU_AXIS_INPUT_INTERVAL = 0.5f;
    private const float SCENE_AXIS_INPUT_INTERVAL = 0.5f;
    private const float AXIS_DEADZONE = 0.2f;
    private const float CONTROLLER_SCROLL_MULTIPLIER = 0.005f;
    private const float CONTROLLER_SCROLL_DEADZONE = 0.25f;
    private bool _navigateMenuAxisInputEnabled = true;
    private bool _navigateSubMenuAxisInputEnabled = false;
    private bool _menuNavigationEnabled = true;

    private void Start()
    {
        if (UIManager.Instance != null && UIManager.Instance.Initialized)
        {
            _uiWindowController = UIManager.Instance.GetUIWindowController();
        }
    }

    private void NavigateMenu(Vector2 axisValue)
    {
        _navigateSubMenuAxisInputEnabled = false;
        switch (axisValue.x)
        {
            case < -AXIS_DEADZONE when _uiWindowController != null:
                _uiWindowController.MoveSectionIndex(-1);
                break;
            case > AXIS_DEADZONE when _uiWindowController != null:
                _uiWindowController.MoveSectionIndex(1);
                break;
        }

        StartCoroutine(AddIntervalDelayToMenuAxisInput());
    }

    private void NavigateSubMenu(Vector2 axisValue)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::NavigateSubMenu : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        switch (axisValue.x)
        {
            case < -AXIS_DEADZONE:
                switch (sectionType)
                {
                    case UIWindowController.UISectionType.Scenes:
                        SelectPreviousScene();
                        break;
                    case UIWindowController.UISectionType.Settings:
                        SelectPreviousAvatarSetting();
                        break;
                    case UIWindowController.UISectionType.Logs:
                        HandleUILoggerSelection();
                        break;
                }
                break;
            case > AXIS_DEADZONE:
                switch (sectionType)
                {
                    case UIWindowController.UISectionType.Scenes:
                        SelectNextScene();
                        break;
                    case UIWindowController.UISectionType.Settings:
                        SelectNextAvatarSetting();
                        break;
                    case UIWindowController.UISectionType.Logs:
                        HandleUILoggerSelection();
                        break;
                }
                break;
        }

        StartCoroutine(AddIntervalDelayToSubMenuAxisInput());
    }

    public bool IsMenuNavigationEnabled()
    {
        return _menuNavigationEnabled;
    }

    private IEnumerator AddIntervalDelayToMenuAxisInput()
    {
        if (_navigateMenuAxisInputEnabled)
        {
            _navigateMenuAxisInputEnabled = false;
            yield return new WaitForSecondsRealtime(MENU_AXIS_INPUT_INTERVAL);
            _navigateMenuAxisInputEnabled = true;
        }
    }

    private IEnumerator AddIntervalDelayToSubMenuAxisInput()
    {
        if (!_navigateSubMenuAxisInputEnabled)
        {
            yield break;
        }
        _navigateSubMenuAxisInputEnabled = false;
        yield return new WaitForSecondsRealtime(SCENE_AXIS_INPUT_INTERVAL);
        _navigateSubMenuAxisInputEnabled = true;
    }

    public void SetMenuNavigationEnabled(bool isEnabled)
    {
        _navigateMenuAxisInputEnabled = isEnabled;
        _menuNavigationEnabled = _navigateMenuAxisInputEnabled || _navigateSubMenuAxisInputEnabled;
    }

    public void SetSubMenuNavigationEnabled(bool isEnabled)
    {
        _navigateSubMenuAxisInputEnabled = isEnabled;
        _menuNavigationEnabled = _navigateMenuAxisInputEnabled || _navigateSubMenuAxisInputEnabled;
    }

    private void CheckInput()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }
#if UNITY_EDITOR
        // Menu button
        if (!OvrAvatarUtility.IsHeadsetActive() && Input.GetKeyUp(_editorMenuButton))
        {
            MenuButtonPressed();
        }
#endif
#if USING_XR_SDK
        // Menu button
        if (OVRInput.GetUp(_menuButton.button))
        {
            MenuButtonPressed();
        }

        // Don't check for input if UI is not enabled
        if (!UIManager.IsPaused)
        {
            return;
        }

        if (_menuNavigationEnabled && _navigateMenuAxisInputEnabled)
        {
            var controllerInput = OVRInput.Get(_navigateMenuInput.axis2d);
            if (Mathf.Abs(controllerInput.x) >= AXIS_DEADZONE)
            {
                NavigateMenu(controllerInput);
            }
        }

        if (_menuNavigationEnabled && _navigateSubMenuAxisInputEnabled)
        {
            var controllerInput = OVRInput.Get(_navigateSubMenuInput.axis2d, _navigateSubMenuInput.controller);
            if (Mathf.Abs(controllerInput.x) >= AXIS_DEADZONE)
            {
                NavigateSubMenu(controllerInput);
            }
        }

        // Load currently selected Scene OR open currently selected Avatar's settings
        if (_menuNavigationEnabled && OVRInput.GetUp(_selectButton.button, _selectButton.controller))
        {
            SelectCurrentSubMenuItem();
        }

        // return
        if (_menuNavigationEnabled && OVRInput.GetUp(_returnButton.button, _returnButton.controller))
        {
            ReturnFromSubMenuItem();
        }
#endif
    }

    private void MenuButtonPressed()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::MenuButtonPressed : UIManager instance not found.", logScope);
            return;
        }

        if (UIManager.IsPaused)
        {
            ResumeScene();
        }
        else
        {
            PauseScene();
        }
    }

    private void PauseScene()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::PauseScene : UIManager instance not found.", logScope);
            return;
        }

        UIManager.Instance.Pause();
    }

    private void ResumeScene()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::ResumeScene : UIManager instance not found.", logScope);
            return;
        }

        UIManager.Instance.Resume();
    }

    private void SelectNextScene()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectNextScene : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType != UIWindowController.UISectionType.Scenes)
        {
            return;
        }

        UISceneSwitcher? sceneSwitcher = UIManager.Instance.GetUISceneSwitcher();

        if (sceneSwitcher == null)
        {
            OvrAvatarLog.LogError("UIInputController::SelectNextScene : Failed to get UISceneSwitcher from UIManager.", logScope);
            return;
        }

        sceneSwitcher.SelectNextSceneButton();
    }

    private void SelectPreviousScene()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectPreviousScene : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType != UIWindowController.UISectionType.Scenes)
        {
            return;
        }

        UISceneSwitcher? sceneSwitcher = UIManager.Instance.GetUISceneSwitcher();

        if (sceneSwitcher == null)
        {
            OvrAvatarLog.LogError("UIInputController::SelectPreviousScene : Failed to get UISceneSwitcher from UIManager.", logScope);
            return;
        }

        sceneSwitcher.SelectPreviousSceneButton();
    }

    private void SelectCurrentSubMenuItem()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectCurrentSubMenuItem : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType == UIWindowController.UISectionType.Invalid)
        {
            return;
        }

        switch (sectionType)
        {
            case UIWindowController.UISectionType.Scenes:
                UISceneSwitcher? sceneSwitcher = UIManager.Instance.GetUISceneSwitcher();
                if (sceneSwitcher == null)
                {
                    OvrAvatarLog.LogError("UIInputController::SelectCurrentSubMenuItem : Failed to get UISceneSwitcher from UIManager.", logScope);
                    return;
                }
                sceneSwitcher.LoadSelectedScene();
                break;
            case UIWindowController.UISectionType.Settings:
                UISettingsManager? settingsManager = UIManager.Instance.GetUISettingsManager();
                if (settingsManager == null)
                {
                    OvrAvatarLog.LogError("UIInputController::SelectCurrentSubMenuItem : Failed to get UISettingsManager from UIManager.", logScope);
                    return;
                }
                settingsManager.SelectSettingsItem();
                break;
            case UIWindowController.UISectionType.Logs:
                UILogger? uiLogger = UIManager.Instance.GetUILogger();
                if (uiLogger == null)
                {
                    OvrAvatarLog.LogError("UIInputController::SelectCurrentSubMenuItem : Failed to get UILogger from UIManager.", logScope);
                    return;
                }
                uiLogger.ApplyCurrentButton();
                break;
        }
    }

    private void SelectNextAvatarSetting()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectNextAvatarSetting : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType != UIWindowController.UISectionType.Settings)
        {
            return;
        }

        UISettingsManager? settingsManager = UIManager.Instance.GetUISettingsManager();

        if (settingsManager == null)
        {
            OvrAvatarLog.LogError("UIInputController::SelectNextAvatarSetting : Failed to get settingsManager from UIManager.", logScope);
            return;
        }

        settingsManager.SelectNextSettingsSection();
    }

    private void SelectPreviousAvatarSetting()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectPreviousAvatarSetting : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType != UIWindowController.UISectionType.Settings)
        {
            return;
        }

        UISettingsManager? settingsManager = UIManager.Instance.GetUISettingsManager();

        if (settingsManager == null)
        {
            OvrAvatarLog.LogError("UIInputController::SelectPreviousAvatarSetting : Failed to get settingsManager from UIManager.", logScope);
            return;
        }

        settingsManager.SelectPreviousSettingsSection();
    }

    private void HandleUILoggerSelection()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectPreviousAvatarSetting : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType != UIWindowController.UISectionType.Logs)
        {
            return;
        }

        UILogger? uiLogger = UIManager.Instance.GetUILogger();

        if (uiLogger == null)
        {
            OvrAvatarLog.LogError("UIInputController::ToggleUILoggerSelection : Failed to get UILogger from UIManager.", logScope);
            return;
        }

        uiLogger.SelectNextLogExportOption();
    }

    private void ReturnFromSubMenuItem()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputController::SelectCurrentSubMenuItem : UIManager instance not found.", logScope);
            return;
        }

        var sectionType = UIManager.Instance.GetActiveSectionType();

        if (sectionType != UIWindowController.UISectionType.Settings)
        {
            return;
        }

        UISettingsManager? settingsManager = UIManager.Instance.GetUISettingsManager();
        if (settingsManager == null)
        {
            OvrAvatarLog.LogError("UIInputController::SelectCurrentSubMenuItem : Failed to get UISettingsManager from UIManager.", logScope);
            return;
        }
        settingsManager.ReturnFromSubMenu();
    }

    private void CheckScrollInput()
    {
        if (!UIManager.IsPaused)
        {
            return;
        }
#if USING_XR_SDK

        if (_uiWindowController == null)
        {
            return;
        }

        var scrollRect = _uiWindowController.GetActiveScrollArea();

        if (scrollRect == null)
        {
            return;
        }

        Vector2 thumbstickMovement = OVRInput.Get(_scrollButton.axis2d, _scrollButton.controller);

        if (Mathf.Abs(thumbstickMovement.y) < CONTROLLER_SCROLL_DEADZONE)
        {
            return;
        }

        var newPos = Mathf.Clamp(scrollRect.normalizedPosition.y + thumbstickMovement.y * scrollRect.scrollSensitivity * CONTROLLER_SCROLL_MULTIPLIER, 0.0f, 1.0f);
        scrollRect.verticalNormalizedPosition = newPos;
#endif
    }

    public static void SetUINavigationEnabled(bool isEnabled)
    {
        if (UIManager.Instance != null)
        {
            var inputController = UIManager.Instance.GetUIInputController();
            if (inputController != null)
            {
                inputController.SetMenuNavigationEnabled(isEnabled);
            }
        }
    }

    public static void SetUISubMenuNavigationEnabled(bool isEnabled)
    {
        if (UIManager.Instance != null)
        {
            var inputController = UIManager.Instance.GetUIInputController();
            if (inputController != null)
            {
                inputController.SetSubMenuNavigationEnabled(isEnabled);
            }
        }
    }

    private void Update()
    {
        CheckInput();
        CheckScrollInput();
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var buttons = new List<UIInputControllerButton>
        {
            _navigateMenuInput,
            _navigateSubMenuInput,
            _selectButton,
            _menuButton,
            _scrollButton,
            _returnButton,
        };

        return buttons;
    }
#endif
}
