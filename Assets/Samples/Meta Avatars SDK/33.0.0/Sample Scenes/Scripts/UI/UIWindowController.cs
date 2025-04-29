#nullable enable

using System.Collections;
using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIWindowController : MonoBehaviour
{
    private const string logScope = "UIWindowController";

    [System.Serializable]
    public enum UISectionType
    {
        Overview,
        Scenes,
        Settings,
        Controls,
        Logs,
        [HideInInspector] Invalid,
    }

    [System.Serializable]
    public struct UISection
    {
        public UISectionType sectionType;
        public GameObject sectionPanel;
        public GameObject tabButton;
        public ScrollRect scrollArea;
    }

    [SerializeField] private UISection[]? sections;
    [SerializeField] private Text? sceneNameText;
    [SerializeField] private GameObject? uiInstructionTextGameObject;
    [SerializeField] private GameObject? uiSectionTabsParentGameObject;

    private int _currentActiveSectionIndex = 0;
    private bool _initialized;


    private void Awake()
    {
        if (sections == null || sections.Length < 1)
        {
            OvrAvatarLog.LogError("UIWindowController::Awake : Empty 'Sections'. This could be due to an empty field from the inspector.", logScope);
            return;
        }

        if (uiInstructionTextGameObject == null)
        {
            OvrAvatarLog.LogError("UIWindowController::Awake : Null uiInstructionTextGameObject. This could be due to an empty field from the inspector.", logScope);
            return;
        }

        if (uiSectionTabsParentGameObject == null)
        {
            OvrAvatarLog.LogError("UIWindowController::Awake : Null uiSectionTabsParentGameObject. This could be due to an empty field from the inspector.", logScope);
            return;
        }

        uiInstructionTextGameObject.SetActive(true);
        uiSectionTabsParentGameObject.SetActive(true);

        foreach (var section in sections)
        {
            var textComponent = section.tabButton.GetComponentInChildren<Text>();
            if (textComponent)
            {
                textComponent.text = section.sectionType.ToString();
            }
            section.sectionPanel.SetActive(false);
        }

        if (sceneNameText != null)
        {
            sceneNameText.gameObject.SetActive(true);
            sceneNameText.text = SceneManager.GetActiveScene().name;
        }
    }

    private void UpdateTabHighlight(int index)
    {
        if (sections == null || sections.Length < 1)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateTabHighlight : Empty 'Sections'. This could be due to an empty 'Sections' field from the inspector.", logScope);
            return;
        }

        if (index >= sections.Length || index < 0)
        {
            OvrAvatarLog.LogError($"UIWindowController::UpdateTabHighlights : index '{index}' out of bounds.", logScope);
            return;
        }

        var button = sections[index].tabButton.GetComponentInChildren<Button>();
        if (button == null)
        {
            OvrAvatarLog.LogError($"UIWindowController::UpdateTabHighlights : Could not find a Button component under game object {sections[index].tabButton.name}", logScope);
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        button.Select();
    }

    public void UpdateSceneNameText(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateSceneNameText : Failed to retrieve UIManager instance.", logScope);
            return;
        }

        var sceneSwitcher = UIManager.Instance.GetUISceneSwitcher();
        if (sceneSwitcher == null)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateSceneNameText : Failed to retrieve SceneSwitcher from UIManager.", logScope);
            return;
        }
        if (sceneNameText != null)
        {
            sceneNameText.text = sceneSwitcher.GetCurrentSceneName();
        }
    }

    private void Start()
    {
        ActivateSection(0);
        _initialized = true;

        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIWindowController::Start : Failed to retrieve UIManager instance.", logScope);
            return;
        }

        UIManager.Instance.AddOnPauseEvent(ResetTabs);
    }

    private void ResetTabs()
    {
        ActivateSection(0);
    }

    public void ActivateSection(int index)
    {
        // Don't activate a section when clicked on by mouse.
        // As clicking on a menu button directly calls this method, this check is
        // needed to prevent unwanted behavior.
        if (UIManager.Instance != null && UIManager.Instance.Initialized)
        {
            var uiInputController = UIManager.Instance.GetUIInputController();
            if (uiInputController != null && !uiInputController.IsMenuNavigationEnabled())
            {
                return;
            }
        }
        if (_initialized && index == _currentActiveSectionIndex)
        {
            UpdateTabHighlight(index);
            return;
        }

        if (sections == null)
        {
            OvrAvatarLog.LogError("UIWindowController::ActivateSection : Empty 'Sections'. This could be due to an empty 'Sections' field from the inspector.", logScope);
            return;
        }

        sections[_currentActiveSectionIndex].sectionPanel.SetActive(false);

        sections[index].sectionPanel.SetActive(true);

        UpdateSectionFromType(sections[_currentActiveSectionIndex].sectionType, false);
        _currentActiveSectionIndex = index;
        UpdateSectionFromType(sections[_currentActiveSectionIndex].sectionType, true);

        UpdateTabHighlight(index);
    }

    private void UpdateSectionFromType(UISectionType sectionType, bool isActive)
    {
        switch (sectionType)
        {
            case UISectionType.Scenes:
                UpdateSceneSection(isActive);
                break;
            case UISectionType.Overview:
                UpdateOverviewSection();
                break;
            case UISectionType.Logs:
                UpdateLogSection(isActive);
                break;
            case UISectionType.Settings:
                UpdateSettingsSection(isActive);
                break;
            default:
                break;
        }

        // reset scrolls to top
        var activeScrollArea = GetActiveScrollArea();

        if (activeScrollArea != null)
        {
            activeScrollArea.verticalNormalizedPosition = 1;
        }
    }

    private void UpdateSettingsSection(bool isActive)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateSettingsSection : Failed to retrieve UIManager instance.", logScope);
            return;
        }

        var settingsManager = UIManager.Instance.GetUISettingsManager();

        if (settingsManager == null)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateSettingsSection : Failed to retrieve SettingsManager from UI Manager.", logScope);
            return;
        }

        // search for avatars only if the Settings section is activated
        if (isActive)
        {
            settingsManager.SearchForAvatarsInScene();
            settingsManager.ResetIndex();
        }
        else
        {
            settingsManager.MoveOutOfSettings();
        }
    }

    private void UpdateLogSection(bool isActive)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateLogSection : Failed to retrieve UIManager instance.", logScope);
            return;
        }

        var uiLogger = UIManager.Instance.GetUILogger();
        if (uiLogger == null)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateLogSection : Failed to retrieve UILogger from UIManager.", logScope);
            return;
        }

        if (isActive)
        {
            uiLogger.ActivateUILogger();
        }
        else
        {
            uiLogger.DeactivateUILogger();
        }
    }

    private void UpdateOverviewSection()
    {
        if (sections == null || sections.Length < 1)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateOverviewSection : Null or empty sections.", logScope);
            return;
        }

        var section = sections[_currentActiveSectionIndex];
        var textComponent = section.sectionPanel.GetComponentInChildren<Text>();
        if (textComponent)
        {
            SetOverviewText(textComponent);
        }
    }

    private void SetOverviewText(Text text)
    {
        StartCoroutine(SetOverviewTextWithDelay(text));
    }
    private IEnumerator SetOverviewTextWithDelay(Text text)
    {
        yield return null;
        text.text = GetCurrentSceneInfo();
    }

    private string GetCurrentSceneInfo()
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIWindowController::GetCurrentSceneInfo : Failed to retrieve UIManager instance.", logScope);
            return "Could not retrieve information for this scene.";
        }

        var sceneSwitcher = UIManager.Instance.GetUISceneSwitcher();
        if (sceneSwitcher == null)
        {
            OvrAvatarLog.LogError("UIWindowController::GetCurrentSceneInfo : Failed to retrieve SceneSwitcher from UIManager.", logScope);
            return "Could not retrieve information for this scene.";
        }

        return sceneSwitcher.GetCurrentSceneDescription();
    }

    private void UpdateSceneSection(bool isActive)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateSceneSection : Failed to retrieve UIManager instance.", logScope);
            return;
        }

        var sceneSwitcher = UIManager.Instance.GetUISceneSwitcher();
        if (sceneSwitcher == null)
        {
            OvrAvatarLog.LogError("UIWindowController::UpdateSceneSection : Failed to retrieve UISceneSwitcher from UIManager.", logScope);
            return;
        }

        if (isActive)
        {
            sceneSwitcher.OnSectionEnable();
        }
        else
        {
            sceneSwitcher.OnSectionDisable();
        }
    }

    public void MoveSectionIndex(int index)
    {
        if (sections == null)
        {
            OvrAvatarLog.LogError("UIWindowController::MoveSectionIndex : Empty 'Sections'. This could be due to an empty 'Sections' field from the inspector.", logScope);
            return;
        }

        int newIndex = WrapIndex(_currentActiveSectionIndex + index, sections.Length);
        ActivateSection(newIndex);
    }

    private int WrapIndex(int index, int arraySize)
    {
        return ((index % arraySize) + arraySize) % arraySize;
    }

    public void ResetIndex()
    {
        if (sections == null)
        {
            OvrAvatarLog.LogError("UIWindowController::ResetIndex : Empty 'Sections'. This could be due to an empty 'Sections' field from the inspector.", logScope);
            return;
        }

        ActivateSection(0);
    }

    public UISectionType GetActiveSectionType()
    {
        if (sections == null)
        {
            OvrAvatarLog.LogError("UIWindowController::GetActiveSectionType : Empty 'Sections'. This could be due to an empty 'Sections' field from the inspector.", logScope);
            return UISectionType.Invalid;
        }

        return sections[_currentActiveSectionIndex].sectionType;
    }

    public ScrollRect? GetActiveScrollArea()
    {
        return sections?[_currentActiveSectionIndex].scrollArea != null ? sections[_currentActiveSectionIndex].scrollArea : null;
    }
}
