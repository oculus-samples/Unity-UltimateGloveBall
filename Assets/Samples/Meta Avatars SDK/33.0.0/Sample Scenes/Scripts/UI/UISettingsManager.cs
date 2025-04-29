#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Avatar2;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISettingsManager : MonoBehaviour
{
    private const string logScope = "UISettingsManager";
    private const string PRESET_SELECTION_ASSET_NAME = "PresetSelectionAsset";
    private const string PRESET_SELECTION_NOT_FOUND = "PresetSelectionAsset not found";
    private const int DROPDOWN_WARNING_FONT_SIZE = 8;

    private static readonly string[] s_avatarQualityFlags = new[] { "Light", "Standard" };

    private Dictionary<string, SampleAvatarEntity>? _sceneAvatars;

    [SerializeField] private GameObject? settingsSubMenu;
    [SerializeField] private Dropdown? activeManifestationDropdown;
    [SerializeField] private Dropdown? activeQualityDropdown;
    [SerializeField] private Dropdown? activeViewDropdown;
    [SerializeField] private Dropdown? presetDropdown;
    [SerializeField] private Toggle? cdnToggle;
    [SerializeField] private GameObject? validationPanel;
    [SerializeField] private Button? returnButton;
    [SerializeField] private Button? continueButton;
    [SerializeField] private Button? returnToSettingsMenuButton;
    [SerializeField] private Text? validationText;

    private List<Button>? _settingsButtons;
    private int _selectedIndex = 0;
    private bool _finishedSearchingForAvatars;
    private bool _subMenuActivated;
    private int _validationButtonIndex;
    private int _selectedSubMenuIndex;
    private int _subMenuSettingsCount;
    private Transform? _selectedSubMenuTransform;
    private const float SELECTED_SUB_MENU_SCALE_FACTOR = 1.15f;

    private Dropdown? _currentActiveDropdown;

    private void Start()
    {
        if (settingsSubMenu == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::Start : Null settings submenu. Make sure this field is assigned in the editor.", logScope);
            return;
        }

        if (validationPanel == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::Start : Null validation panel. Make sure this field is assigned in the editor.", logScope);
            return;
        }

        validationPanel.SetActive(false);
        settingsSubMenu.SetActive(false);
        UIInputController.SetUINavigationEnabled(true);
        UIInputController.SetUISubMenuNavigationEnabled(false);
        _subMenuActivated = false;
        _sceneAvatars = new Dictionary<string, SampleAvatarEntity>();
        _settingsButtons = new List<Button>();
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISettingsManager::Start : Could not access UI Manager instance.", logScope);
            return;
        }

        UIManager.Instance.AddOnLoadSceneEvent(SettingsManagerOnLoadScene);

        _finishedSearchingForAvatars = false;

        // The VerticalLayoutGroup component is used as a reference to the GameObject that contains the different settings sections.
        // The child count of this layout group is used to set the _subMenuSettingsCount, which is used in navigating the submenu
        var layoutGroup = settingsSubMenu.GetComponentInChildren<VerticalLayoutGroup>();

        if (layoutGroup == null)
        {
            OvrAvatarLog.LogError($"UISettingsManager::Start : Could not find VerticalLayoutGroup under game object: {settingsSubMenu.name}");
            return;
        }

        _subMenuSettingsCount = layoutGroup.transform.childCount;

        if (returnToSettingsMenuButton == null)
        {
            OvrAvatarLog.LogError($"UISettingsManager::Start : Could not find VerticalLayoutGroup under game object: {settingsSubMenu.name}");
            return;
        }

        returnToSettingsMenuButton.onClick.AddListener(ReturnToSettings);

        UIManager.Instance.AddOnResumeEvent(MoveOutOfSettings);
    }

    private void SettingsManagerOnLoadScene()
    {
        _finishedSearchingForAvatars = false;
        ReturnToSettings();

        if (_settingsButtons != null)
        {
            foreach (var settingsButton in _settingsButtons)
            {
                if (settingsButton != null)
                {
                    Destroy(settingsButton.gameObject);
                }
            }
            _settingsButtons.Clear();
        }

        _sceneAvatars?.Clear();
    }

    private void ReturnToSettings()
    {
        _subMenuActivated = false;
        ResetSettingsSubMenuUI();
        if (settingsSubMenu == null)
        {
            OvrAvatarLog.LogError($"UISettingsManager::ReturnToSettings : Null settingsSubMenu.", logScope);
            return;
        }
        SetSettingsPanelEnabled(true);
        UIInputController.SetUINavigationEnabled(true);
        UIInputController.SetUISubMenuNavigationEnabled(true);
        settingsSubMenu.SetActive(false);
        _subMenuActivated = false;
    }

    private static string GetAvatarPlayerPrefsStr(SampleAvatarEntity entity)
    {
        return $"{logScope}_{entity.GetInstanceID()}";
    }

    public void SearchForAvatarsInScene()
    {
        if (_finishedSearchingForAvatars)
        {
            return;
        }

        if (_sceneAvatars != null)
        {
            _sceneAvatars.Clear();
        }

        if (_settingsButtons != null)
        {
            _settingsButtons.Clear();
        }

        SampleAvatarEntity[] entities = FindObjectsOfType<SampleAvatarEntity>();

        for (var i = 0; i < entities.Length; i++)
        {
            var avatarEntity = entities[i];

            var avatarEntityPlayerPrefsString = GetAvatarPlayerPrefsStr(avatarEntity);

            if (_sceneAvatars == null || _sceneAvatars.ContainsKey(avatarEntityPlayerPrefsString))
            {
                continue;
            }

            _sceneAvatars.Add(avatarEntityPlayerPrefsString, avatarEntity);
            CreateButtonForAvatar(avatarEntity);
        }

        _selectedIndex = 0;
        SelectButtonFromIndex(0);

        _finishedSearchingForAvatars = true;
    }

    private void CreateButtonForAvatar(SampleAvatarEntity entity)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISettingsManager::CreateButtonForAvatar : Could not access UI Manager instance.", logScope);
            return;
        }

        var settingsPanel = UIManager.Instance.GetSettingsPanel();
        if (settingsPanel == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::CreateButtonForAvatar : Could not retrieve settings panel from UI Manager.", logScope);
            return;
        }

        var buttonGameObject = UIManager.Instance.CreateButtonForUI(settingsPanel.transform,
            $"Button_Settings_{entity.name}",
            entity.name,
            () =>
            {
                OpenSubMenuOnClick(entity);
            });

        if (buttonGameObject == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::CreateButtonForAvatar : Error in creating UI button game object.", logScope);
            return;
        }

        var buttonScript = buttonGameObject.GetComponent<Button>();

        if (buttonScript == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::CreateButtonForAvatar : Created button has no \"Button\" component.", logScope);
            return;
        }

        _settingsButtons?.Add(buttonScript);
    }

    private void OpenSubMenuOnClick(SampleAvatarEntity entity)
    {
        if (settingsSubMenu == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OpenSupMenuOnClick : settingsSubMenu not assigned.", logScope);
            return;
        }

        if (activeManifestationDropdown == null || activeQualityDropdown == null || activeViewDropdown == null || presetDropdown == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OpenSupMenuOnClick : One or more of the dropdown fields are not assigned.", logScope);
            return;
        }

        if (cdnToggle == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OpenSupMenuOnClick : cdnToggle not assigned.", logScope);
            return;
        }

        if (entity.gameObject == null || entity == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OpenSupMenuOnClick : Null entity.", logScope);
            return;
        }

        _selectedSubMenuIndex = -1;
        _validationButtonIndex = -1;
        _selectedSubMenuTransform = null;
        UpdateSubMenu();
        _subMenuActivated = true;
        settingsSubMenu.SetActive(true);
        UIInputController.SetUINavigationEnabled(false);
        UIInputController.SetUISubMenuNavigationEnabled(true);
        SetSettingsPanelEnabled(false);

        var settings = entity.GetAvatarConfig();

        OvrAvatarLog.LogInfo($"Avatar Entity settings for <{entity.name}>:\n{entity.GetAvatarConfig()}");

        AddEnumOptionsToDropdown(activeManifestationDropdown, typeof(CAPI.ovrAvatar2EntityManifestationFlags));
        AddEnumOptionsToDropdown(activeViewDropdown, typeof(CAPI.ovrAvatar2EntityViewFlags));
        AddPresetsToDropdown();
        AddQualitiesToDropdown();

        var manifestationIndex =
            GetDropdownIndexFromString(activeManifestationDropdown, settings.ActiveManifestation.ToString());
        var qualityIndex =
            GetDropdownIndexFromString(activeQualityDropdown, settings.CreationInfo.renderFilters.quality.ToString());
        var viewIndex =
            GetDropdownIndexFromString(activeViewDropdown, settings.ActiveView.ToString());

        activeManifestationDropdown.onValueChanged.RemoveAllListeners();
        activeQualityDropdown.onValueChanged.RemoveAllListeners();
        activeViewDropdown.onValueChanged.RemoveAllListeners();
        presetDropdown.onValueChanged.RemoveAllListeners();

        activeManifestationDropdown.value = manifestationIndex;
        activeQualityDropdown.value = qualityIndex;
        activeViewDropdown.value = viewIndex;
        var result = int.TryParse(settings.Assets?[0].path, out var presetIndex);
        if (result)
        {
            presetDropdown.value = GetDropdownIndexFromString(presetDropdown, presetIndex.ToString());
        }

        activeManifestationDropdown.onValueChanged.AddListener(delegate
        {
            OnManifestationChanged(entity, (CAPI.ovrAvatar2EntityManifestationFlags)activeManifestationDropdown.value, settings);
        });

        activeQualityDropdown.onValueChanged.AddListener(delegate
        {
            OnQualityChanged(entity, (CAPI.ovrAvatar2EntityQuality)activeQualityDropdown.value, settings);
        });

        activeViewDropdown.onValueChanged.AddListener(delegate
        {
            OnViewChanged(entity, (CAPI.ovrAvatar2EntityViewFlags)activeViewDropdown.value, settings);
        });

        cdnToggle.onValueChanged.RemoveAllListeners();
        cdnToggle.onValueChanged.AddListener(delegate (bool loadFromCdn)
        {
            OnCdnChanged(entity, loadFromCdn, settings);
            if (loadFromCdn)
            {
                presetDropdown.interactable = false;
            }
            else
            {
                presetDropdown.onValueChanged.AddListener(delegate
                {
                    if (presetDropdown.options[presetDropdown.value].ToString().Equals(PRESET_SELECTION_NOT_FOUND))
                    {
                        presetDropdown.interactable = false;
                    }
                    else
                    {
                        var presetNumber = int.TryParse(presetDropdown.options[presetDropdown.value].text, out var selectedPreset) ? selectedPreset : -1;
                        if (presetNumber == -1)
                        {
                            presetDropdown.interactable = false;
                        }
                        else
                        {
                            presetDropdown.interactable = true;
                            OnPresetChanged(entity, presetNumber, settings);
                        }
                    }
                });
            }
        });
        cdnToggle.isOn = settings.LoadUserFromCdn;
    }

    private void AddEnumOptionsToDropdown(Dropdown dropdown, Type enumType)
    {
        List<Dropdown.OptionData> optionData = new List<Dropdown.OptionData>();
        foreach (var value in Enum.GetValues(enumType))
        {
            optionData.Add(new Dropdown.OptionData(Enum.GetName(enumType, value)));
        }
        dropdown.ClearOptions();
        dropdown.options = optionData;
    }

    private void AddQualitiesToDropdown()
    {
        if (activeQualityDropdown == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::AddPresetQualitiesToDropdown : null activeQualityDropdown reference", logScope);
            return;
        }
        List<Dropdown.OptionData> optionData = new List<Dropdown.OptionData>();
        foreach (var qualityFlag in s_avatarQualityFlags)
        {
            optionData.Add(new Dropdown.OptionData(qualityFlag));
        }
        activeQualityDropdown.ClearOptions();
        activeQualityDropdown.options = optionData;
    }

    private void AddPresetsToDropdown()
    {
        if (presetDropdown == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::AddPresetsToDropdown : null presetDropdown reference", logScope);
            return;
        }

        List<Dropdown.OptionData> optionData = new List<Dropdown.OptionData>();
        var presetSelection = Resources.Load<PresetSelectionInfo>(PRESET_SELECTION_ASSET_NAME);
        if (presetSelection == null)
        {
            OvrAvatarLog.LogWarning(
                "UISettingsManager::AddPresetsToDropdown : no preset selection scriptable object found. " +
                "Use \"MetaAvatarsSDK/Assets/Sample Assets/Preset Selector\" option to include preset avatars in the project.",
                logScope);
            optionData.Add(new Dropdown.OptionData(PRESET_SELECTION_NOT_FOUND));
            presetDropdown.ClearOptions();
            presetDropdown.AddOptions(optionData);
            var presetLabel = presetDropdown.gameObject.GetComponentInChildren<Text>();
            if (presetLabel != null)
            {
                presetLabel.fontSize = DROPDOWN_WARNING_FONT_SIZE;
            }

            presetDropdown.interactable = false;
            return;
        }

        if (presetSelection.avatarSelection == null)
        {
            OvrAvatarLog.LogWarning(
                "UISettingsManager::AddPresetsToDropdown : null PresetHelper avatar selection. " +
                "Use \"MetaAvatarsSDK/Assets/Sample Assets/Preset Selector\" option to include preset avatars in the project.", logScope);
            optionData.Add(new Dropdown.OptionData(PRESET_SELECTION_NOT_FOUND));
            presetDropdown.ClearOptions();
            presetDropdown.AddOptions(optionData);
            var presetLabel = presetDropdown.gameObject.GetComponentInChildren<Text>();
            if (presetLabel != null)
            {
                presetLabel.fontSize = DROPDOWN_WARNING_FONT_SIZE;
            }

            presetDropdown.interactable = false;
            return;
        }

        for (var i = 0; i < presetSelection.avatarSelection.Length; i++)
        {
            if (presetSelection.avatarSelection[i])
            {
                optionData.Add(
                    new Dropdown.OptionData(i.ToString()));
            }
        }

        presetDropdown.ClearOptions();
        presetDropdown.options = optionData;
    }

    private int GetDropdownIndexFromString(Dropdown dropdown, string optionString)
    {
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == optionString)
            {
                return i;
            }
        }
        return -1; // Return -1 if not found
    }

    private void ShowValidationPanel(string validationMessage, Action onContinue, Action onReturn)
    {
        if (validationPanel == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::ShowValidationPanel : Null validation panel. Make sure this field is assigned in the editor.", logScope);
            return;
        }
        if (continueButton == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::ShowValidationPanel : Null continue button. Make sure this field is assigned in the editor.", logScope);
            return;
        }
        if (validationText == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::ShowValidationPanel : Null validation text. Make sure this field is assigned in the editor.", logScope);
            return;
        }
        if (returnButton == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::ShowValidationPanel : Null return button. Make sure this field is assigned in the editor.", logScope);
            return;
        }
        returnButton.onClick.AddListener(() =>
        {
            ResetSettingsSubMenuUI();
            onReturn();
            UIInputController.SetUISubMenuNavigationEnabled(true);
            UIInputController.SetUINavigationEnabled(false);
            returnButton.onClick.RemoveAllListeners();
        });
        validationText.text = validationMessage;
        validationPanel.SetActive(true);
        continueButton.onClick.AddListener(() =>
        {
            onContinue();
            ResetSettingsSubMenuUI();
            UIInputController.SetUISubMenuNavigationEnabled(true);
            UIInputController.SetUINavigationEnabled(false);
            continueButton.onClick.RemoveAllListeners();
        });
    }

    private void SetSettingsPanelEnabled(bool isEnabled)
    {
        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UISettingsManager::SetSettingsPanelEnabled : Could not access UI Manager instance.", logScope);
            return;
        }

        var settingsPanel = UIManager.Instance.GetSettingsPanel();
        if (settingsPanel == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::SetSettingsPanelEnabled : Could not retrieve settings panel from UI Manager.", logScope);
            return;
        }

        settingsPanel.SetActive(isEnabled);
    }

    private void ResetDropdownValue(Dropdown dropdown, string value)
    {
        if (dropdown != null)
        {
            dropdown.SetValueWithoutNotify(GetDropdownIndexFromString(dropdown, value));
        }
    }

    private void ResetToggleValue(Toggle toggle, bool value)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
    }

    private void OnManifestationChanged(SampleAvatarEntity entity,
        CAPI.ovrAvatar2EntityManifestationFlags manifestationFlags,
        SampleAvatarConfig sampleAvatarConfig)
    {
        if (sampleAvatarConfig.ActiveManifestation == manifestationFlags)
        {
            return;
        }

        Int32 activeFlags = (Int32)(object)manifestationFlags;
        Int32 availableManifestationFlags = (Int32)(object)sampleAvatarConfig.CreationInfo.renderFilters.manifestationFlags;

        if (!OvrAvatarUtility.IsSingleEnumInFlags(activeFlags, availableManifestationFlags))
        {
            if (activeManifestationDropdown == null)
            {
                OvrAvatarLog.LogError("UISettingsManager::OnManifestationChanged : Null activeManifestationDropdown. Make sure this field is assigned in the editor.", logScope);
                return;
            }
            ShowValidationPanel($"Manifestation \"{manifestationFlags}\" does not exist in Avatar \"{entity.name}\"'s creationInfo. Applying these settings will most likely result in the Avatar disappearing." +
                                "\nPress \"Continue\" to proceed with these changes. Press \"Return\" to discard.",
                () =>
                {
                    sampleAvatarConfig.ActiveManifestation = manifestationFlags;
                    entity.ApplyConfig(sampleAvatarConfig, false);
                },
                () => ResetDropdownValue(activeManifestationDropdown, sampleAvatarConfig.ActiveManifestation.ToString()));
        }
        else
        {
            sampleAvatarConfig.ActiveManifestation = manifestationFlags;
            entity.ApplyConfig(sampleAvatarConfig, false);
        }
    }

    private void OnQualityChanged(SampleAvatarEntity entity,
        CAPI.ovrAvatar2EntityQuality quality,
        SampleAvatarConfig sampleAvatarConfig)
    {
        if (sampleAvatarConfig.CreationInfo.renderFilters.quality == quality)
        {
            return;
        }

        if (activeQualityDropdown == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OnQualityChanged : Null activeQualityDropdown. Make sure this field is assigned in the editor.", logScope);
            return;
        }

        ShowValidationPanel("Changing an avatar's quality triggers an entity teardown.\nPress \"Continue\" to proceed with the teardown. Press \"Return\" to discard.",
            () =>
            {
                sampleAvatarConfig.CreationInfo.renderFilters.quality = quality;
                entity.ApplyConfig(sampleAvatarConfig, true);
            },
            () => ResetDropdownValue(activeQualityDropdown, sampleAvatarConfig.CreationInfo.renderFilters.quality.ToString()));
    }

    private void OnViewChanged(SampleAvatarEntity entity,
        CAPI.ovrAvatar2EntityViewFlags viewFlags,
        SampleAvatarConfig sampleAvatarConfig)
    {
        if (sampleAvatarConfig.ActiveView == viewFlags)
        {
            return;
        }

        var entityViewFlags = sampleAvatarConfig.CreationInfo.renderFilters.viewFlags;

        Int32 activeFlags = (Int32)(object)viewFlags;
        Int32 validFlags = (Int32)(object)entityViewFlags;

        if (!OvrAvatarUtility.IsSingleEnumInFlags(activeFlags, validFlags))
        {
            if (activeViewDropdown == null)
            {
                OvrAvatarLog.LogError("UISettingsManager::OnViewChanged : Null activeViewDropdown. Make sure this field is assigned in the editor.", logScope);
                return;
            }
            ShowValidationPanel($"View \"{viewFlags}\" does not exist in Avatar \"{entity.name}\"'s creationInfo. Applying these settings will most likely result in the Avatar disappearing." +
                                "\nPress \"Continue\" to proceed with these changes. Press \"Return\" to discard.",
                () =>
                {
                    sampleAvatarConfig.ActiveView = viewFlags;
                    entity.ApplyConfig(sampleAvatarConfig, false);
                },
                () => ResetDropdownValue(activeViewDropdown, sampleAvatarConfig.ActiveView.ToString()));
        }
        else
        {
            sampleAvatarConfig.ActiveView = viewFlags;
            entity.ApplyConfig(sampleAvatarConfig, false);
        }
    }

    private void OnCdnChanged(SampleAvatarEntity entity,
        bool loadUserFromCdn,
        SampleAvatarConfig sampleAvatarConfig)
    {
        if (sampleAvatarConfig.LoadUserFromCdn == loadUserFromCdn)
        {
            return;
        }

        if (cdnToggle == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OnCdnChanged : Null cdnToggle. Make sure this field is assigned in the editor.", logScope);
            return;
        }

        ShowValidationPanel("Switching between CDN and Preset Avatar triggers an entity teardown.\nPress \"Continue\" to proceed with the teardown. Press \"Return\" to discard.",
            () =>
            {
                sampleAvatarConfig.LoadUserFromCdn = loadUserFromCdn;
                entity.ApplyConfig(sampleAvatarConfig, true);
            },
            () => ResetToggleValue(cdnToggle, sampleAvatarConfig.LoadUserFromCdn));
    }

    private void OnPresetChanged(SampleAvatarEntity entity,
        int presetNumber,
        SampleAvatarConfig sampleAvatarConfig)
    {
        if (sampleAvatarConfig.LoadUserFromCdn)
        {
            return;
        }

        if (presetDropdown == null)
        {
            OvrAvatarLog.LogError("UISettingsManager::OnPresetChanged : Null presetDropdown. Make sure this field is assigned in the editor.", logScope);
            return;
        }

        sampleAvatarConfig.Assets = new List<SampleAvatarConfig.AssetData>
        {
            new SampleAvatarConfig.AssetData
                { source = OvrAvatarEntity.AssetSource.Zip, path = presetNumber.ToString() }
        };

        entity.ApplyConfig(sampleAvatarConfig, true);
    }

    private void SelectButtonFromIndex(int index)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }

        if (!_finishedSearchingForAvatars)
        {
            return;
        }

#if UNITY_EDITOR
        if (!OvrAvatarUtility.IsHeadsetActive())
        {
            // using mouse
            return;
        }
#endif
        if (_settingsButtons == null || _settingsButtons.Count < 1)
        {
            OvrAvatarLog.LogError("UISettingsManager::SelectButtonFromIndex : No scene buttons found.", logScope);
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        if (_settingsButtons[index] != null)
        {
            _settingsButtons[index].Select();
        }
    }

    public void ResetIndex()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }

        _selectedIndex = 0;
        SelectButtonFromIndex(_selectedIndex);
        UIInputController.SetUISubMenuNavigationEnabled(true);
    }

    private void ToggleRetryContinue()
    {
        EventSystem.current.SetSelectedGameObject(null);
        switch (_validationButtonIndex)
        {
            case 0:
                _validationButtonIndex = 1;
                if (returnButton != null)
                {
                    returnButton.Select();
                }
                break;
            case 1:
                _validationButtonIndex = 0;
                if (continueButton != null)
                {
                    continueButton.Select();
                }
                break;
            default:
                _validationButtonIndex = 1;
                if (returnButton != null)
                {
                    returnButton.Select();
                }
                break;
        }
    }

    private void ActivateValidationButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
        switch (_validationButtonIndex)
        {
            case 0:
                if (continueButton != null)
                {
                    continueButton.onClick.Invoke();
                }
                break;
            case 1:
                if (returnButton != null)
                {
                    _validationButtonIndex = 0;
                    returnButton.onClick.Invoke();
                }
                break;
        }
    }

    private bool IsDropdownExpanded(Dropdown dropdown)
    {
        // The dropdown list is the second child of the dropdown GameObject
        Transform dropdownList = dropdown.transform.GetChild(2);
        return dropdownList.gameObject.activeInHierarchy;
    }

    public void SelectNextSettingsSection()
    {
        if (validationPanel != null && validationPanel.activeSelf)
        {
            ToggleRetryContinue();
            return;
        }
        if (_currentActiveDropdown != null && IsDropdownExpanded(_currentActiveDropdown))
        {
            _currentActiveDropdown.SetValueWithoutNotify(_currentActiveDropdown.value + 1);
            return;
        }
        if (_subMenuActivated)
        {
            SelectNextSubMenuItem();
        }
        else
        {
            SelectNextSettingsButton();
        }
    }

    public void SelectPreviousSettingsSection()
    {
        if (validationPanel != null && validationPanel.activeSelf)
        {
            ToggleRetryContinue();
            return;
        }
        if (_currentActiveDropdown != null && IsDropdownExpanded(_currentActiveDropdown))
        {
            _currentActiveDropdown.SetValueWithoutNotify(_currentActiveDropdown.value - 1);
            return;
        }
        if (_subMenuActivated)
        {
            SelectPreviousSubMenuItem();
        }
        else
        {
            SelectPreviousSettingsButton();
        }
    }

    private void SelectNextSettingsButton()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }

        if (_settingsButtons == null || _settingsButtons.Count == 0)
        {
            OvrAvatarLog.LogError("UISettingsManager::SelectNextSettingsButton : No settings buttons found.", logScope);
            return;
        }

        if (++_selectedIndex >= _settingsButtons.Count)
        {
            _selectedIndex = 0;
        }

        SelectButtonFromIndex(_selectedIndex);
    }

    private void SelectPreviousSettingsButton()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }

        if (_settingsButtons == null || _settingsButtons.Count == 0)
        {
            OvrAvatarLog.LogError("UISettingsManager::SelectPreviousSettingsButton : No settings buttons found.", logScope);
            return;
        }

        if (--_selectedIndex < 0)
        {
            _selectedIndex = _settingsButtons.Count - 1;
        }

        SelectButtonFromIndex(_selectedIndex);
    }

    private void SelectNextSubMenuItem()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }

        if (++_selectedSubMenuIndex >= _subMenuSettingsCount + 1)
        {
            _selectedSubMenuIndex = 0;
        }

        UpdateSubMenu();
    }

    private void SelectPreviousSubMenuItem()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsLoadingScene())
        {
            return;
        }

        if (--_selectedSubMenuIndex < 0)
        {
            _selectedSubMenuIndex = _subMenuSettingsCount;
        }

        UpdateSubMenu();
    }

    private void ActivateCurrentSubMenuItem()
    {
        if (validationPanel != null && validationPanel.activeSelf)
        {
            ActivateValidationButton();
            return;
        }
        EventSystem.current.SetSelectedGameObject(null);
        if (_currentActiveDropdown != null)
        {
            _currentActiveDropdown.Hide();
        }
        switch (_selectedSubMenuIndex)
        {
            case 0:
                if (activeManifestationDropdown != null)
                {
                    activeManifestationDropdown.Show();
                    _currentActiveDropdown = activeManifestationDropdown;
                }
                break;
            case 1:
                if (activeQualityDropdown != null)
                {
                    activeQualityDropdown.Show();
                    _currentActiveDropdown = activeQualityDropdown;
                }
                break;
            case 2:
                if (activeViewDropdown != null)
                {
                    activeViewDropdown.Show();
                    _currentActiveDropdown = activeViewDropdown;
                }
                break;
            case 3:
                if (cdnToggle != null)
                {
                    cdnToggle.isOn = !cdnToggle.isOn;
                }
                _currentActiveDropdown = null;
                break;
            case 4:
                if (presetDropdown != null)
                {
                    presetDropdown.Show();
                    _currentActiveDropdown = presetDropdown;
                }
                break;
            case 5:
                if (returnToSettingsMenuButton != null)
                {
                    returnToSettingsMenuButton.onClick.Invoke();
                }
                _currentActiveDropdown = null;
                break;
            default:
                OvrAvatarLog.LogError($"UISettingsManager::ActivateCurrentSubMenuItem : Invalid sub menu index: {_selectedSubMenuIndex}", logScope);
                break;
        }
    }

    private void UpdateSubMenu()
    {
#if UNITY_EDITOR
        if (!OvrAvatarUtility.IsHeadsetActive())
        {
            return;
        }
#endif
        if (_selectedSubMenuTransform != null)
        {
            _selectedSubMenuTransform.localScale = Vector3.one;
        }

        if (_currentActiveDropdown != null)
        {
            _currentActiveDropdown.Hide();
            _currentActiveDropdown = null;
        }

        switch (_selectedSubMenuIndex)
        {
            case 0:
                if (activeManifestationDropdown != null)
                {
                    _selectedSubMenuTransform = activeManifestationDropdown.transform.parent;
                }
                break;
            case 1:
                if (activeQualityDropdown != null)
                {
                    _selectedSubMenuTransform = activeQualityDropdown.transform.parent;
                }
                break;
            case 2:
                if (activeViewDropdown != null)
                {
                    _selectedSubMenuTransform = activeViewDropdown.transform.parent;
                }
                break;
            case 3:
                if (cdnToggle != null)
                {
                    _selectedSubMenuTransform = cdnToggle.transform.parent;
                }
                break;
            case 4:
                if (presetDropdown != null)
                {
                    _selectedSubMenuTransform = presetDropdown.transform;
                }
                break;
            case 5:
                if (returnToSettingsMenuButton != null)
                {
                    _selectedSubMenuTransform = returnToSettingsMenuButton.transform;
                }
                break;
            default:
                _selectedSubMenuIndex = 0;
                _selectedSubMenuTransform = activeManifestationDropdown != null ? activeManifestationDropdown.transform.parent : null;
                break;
        }

        if (_selectedSubMenuTransform != null)
        {
            _selectedSubMenuTransform.localScale = Vector3.one * SELECTED_SUB_MENU_SCALE_FACTOR;
        }
    }

    public void SelectSettingsItem()
    {
        if (_currentActiveDropdown != null && IsDropdownExpanded(_currentActiveDropdown))
        {
            _currentActiveDropdown.Hide();
            _currentActiveDropdown.onValueChanged.Invoke(_currentActiveDropdown.value);
            return;
        }

        if (_subMenuActivated)
        {
            ActivateCurrentSubMenuItem();
        }
        else
        {
            if (_settingsButtons == null || _settingsButtons.Count == 0)
            {
                OvrAvatarLog.LogError("UISettingsManager::OpenCurrentAvatarSettings : No settings buttons found.", logScope);
                return;
            }

            _settingsButtons[_selectedIndex].onClick.Invoke();
        }
    }

    private void ResetSettingsSubMenuUI()
    {
        if (validationPanel != null)
        {
            validationPanel.SetActive(false);
        }

        if (_selectedSubMenuTransform != null)
        {
            _selectedSubMenuTransform.localScale = Vector3.one;
        }
        _subMenuActivated = true;
        _selectedSubMenuIndex = -1;
        _validationButtonIndex = -1;
        _selectedSubMenuTransform = null;
        UpdateSubMenu();
    }

    public void MoveOutOfSettings()
    {
        ReturnToSettings();
        SetSettingsPanelEnabled(false);
    }

    public void ReturnFromSubMenu()
    {
        ReturnToSettings();
    }
}
