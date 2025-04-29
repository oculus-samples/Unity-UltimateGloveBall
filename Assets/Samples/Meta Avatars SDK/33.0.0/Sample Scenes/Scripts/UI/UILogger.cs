#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.UI;

public class UILogger : MonoBehaviour, IUIControllerInterface
{
    private static readonly int s_logCacheSize = 1000;
    private static readonly int s_displayLogCacheSize = 50;
    private static readonly Queue<string> s_logCacheQueue = new Queue<string>(s_logCacheSize + 1);
    private static readonly string logScope = "UILogger";
    [SerializeField] private Text? logText;
    [SerializeField] private Dropdown? logLevelDropdown;
    private bool _isActive = false;
    [SerializeField] private OvrAvatarLog.ELogLevel minLogLevel = OvrAvatarLog.ELogLevel.Error;
    [SerializeField] private int logViewCapacity = 10;  // TODO: add character limit instead of log count limit
    [SerializeField] private Button? exportLogsButton;
    [SerializeField] private Button? exportLogsFilteredButton;
    private Button? _selectedButton;
    private const float SELECTED_SUB_MENU_SCALE_FACTOR = 1.15f;
    private static string? s_logQueueText;
    private static readonly Queue<string> s_displayLogCacheQueue = new Queue<string>(s_displayLogCacheSize + 1);
    private bool _displayLogCacheQueueChanged = false;

#if USING_XR_SDK
    private readonly UIInputControllerButton _increaseLogLevelButton = new()
    {
        button = OVRInput.Button.PrimaryIndexTrigger,
        controller = OVRInput.Controller.RTouch,
        description = "Increase Log Level",
        scope = logScope
    };
    private readonly UIInputControllerButton _decreaseLogLevelButton = new()
    {
        button = OVRInput.Button.PrimaryIndexTrigger,
        controller = OVRInput.Controller.LTouch,
        description = "Decrease Log Level",
        scope = logScope
    };
#endif

    private void Awake()
    {
        if (logLevelDropdown == null)
        {
            OvrAvatarLog.LogError("UILogger::Awake : Null logLevelDropdown reference. Make sure this field is assigned in Editor.", logScope);
            return;
        }

        logLevelDropdown.ClearOptions();
        var logOptions = new List<string>();
        foreach (OvrAvatarLog.ELogLevel logLevel in Enum.GetValues(typeof(OvrAvatarLog.ELogLevel)))
        {
            if (logLevel > OvrAvatarLog.ELogLevel.Silent)
            {
                logOptions.Add(AddColorToTextFromLogLevel(logLevel.ToString(), logLevel));
            }
        }
        logLevelDropdown.AddOptions(logOptions);
        logLevelDropdown.SetValueWithoutNotify((int)minLogLevel >= 0 ? (int)minLogLevel : logOptions.Count - 1);
        logLevelDropdown.onValueChanged.AddListener(HandleDropdownValueChanged);

        if (exportLogsButton == null)
        {
            OvrAvatarLog.LogError("UILogger::Awake : Null exportLogsButton reference. Make sure this field is assigned in Editor.", logScope);
            return;
        }

        if (exportLogsFilteredButton == null)
        {
            OvrAvatarLog.LogError("UILogger::Awake : Null exportLogsFilteredButton reference. Make sure this field is assigned in Editor.", logScope);
            return;
        }

        exportLogsButton.onClick.AddListener(ExportAllLogs);
        exportLogsFilteredButton.onClick.AddListener(ExportFilteredLogs);
    }

    private void Start()
    {
        OvrAvatarLog.UILogListener += ReceiveLogMessageForUI;
        _isActive = true;
    }

    private void OnDestroy()
    {
        if (logLevelDropdown != null)
        {
            logLevelDropdown.onValueChanged.RemoveListener(HandleDropdownValueChanged);
        }
    }

    private void HandleDropdownValueChanged(int selectedIndex)
    {
        if (logLevelDropdown == null)
        {
            OvrAvatarLog.LogError("UILogger::HandleDropdownValueChanged : Null logLevelDropdown reference. Make sure this field is assigned in Editor.", logScope);
            return;
        }

        var selectedOption = logLevelDropdown.options[selectedIndex].text;
        selectedOption = RemoveColorTag(selectedOption);

        if (!Enum.TryParse(selectedOption, out OvrAvatarLog.ELogLevel logLevel))
        {
            OvrAvatarLog.LogError($"UILogger::HandleDropdownValueChanged : Log level could not be parsed from logLevelDropdown options.", logScope);
            return;
        }

        SetMinLogLevel(logLevel);
    }

    private void SetMinLogLevel(OvrAvatarLog.ELogLevel level)
    {
        minLogLevel = level;
        s_displayLogCacheQueue.Clear();
        foreach (var log in s_logCacheQueue)
        {
            if (IsAtOrAboveMinLogLevel(log))
            {
                s_displayLogCacheQueue.Enqueue(log);
            }
        }
        _displayLogCacheQueueChanged = true;
        UpdateLoggerUI(true);
    }

    public void ActivateUILogger()
    {
        _isActive = true;
        UpdateLoggerUI(true);
        UIInputController.SetUISubMenuNavigationEnabled(true);
    }

    public void DeactivateUILogger()
    {
        _isActive = false;
        UIInputController.SetUISubMenuNavigationEnabled(false);
    }

    public void SelectNextLogExportOption()
    {
        if (_selectedButton != null)
        {
            _selectedButton.transform.localScale = Vector3.one;
        }
        _selectedButton = _selectedButton == null || _selectedButton == exportLogsFilteredButton
            ? exportLogsButton
            : exportLogsFilteredButton;

        if (_selectedButton == null)
        {
            OvrAvatarLog.LogError("UILogger::SelectNextLogExportOption : Null selected button.", logScope);
            return;
        }

        _selectedButton.transform.localScale = Vector3.one * SELECTED_SUB_MENU_SCALE_FACTOR;
    }

    public void ApplyCurrentButton()
    {
        if (_selectedButton == null)
        {
            return;
        }
        _selectedButton.onClick.Invoke();
    }

    private string GetHTMLColorCodeFromLogLevel(OvrAvatarLog.ELogLevel logLevel)
    {
        var colorString = logLevel switch
        {
            OvrAvatarLog.ELogLevel.Verbose => "white",
            OvrAvatarLog.ELogLevel.Debug => "white",
            OvrAvatarLog.ELogLevel.Info => "white",
            OvrAvatarLog.ELogLevel.Warn => "yellow",
            OvrAvatarLog.ELogLevel.Error => "red",
            _ => "white"
        };

        return colorString;
    }

    private string GetPostPrefixFromLogLevel(OvrAvatarLog.ELogLevel logLevel)
    {
        var postPrefix = logLevel switch
        {
            OvrAvatarLog.ELogLevel.Verbose => "[Verbose]",
            OvrAvatarLog.ELogLevel.Debug => "[Debug]",
            _ => ""
        };

        return postPrefix;
    }

    private string AddColorToTextFromLogLevel(string text, OvrAvatarLog.ELogLevel logLevel)
    {
        var textColor = GetHTMLColorCodeFromLogLevel(logLevel);
        return $"<color=\"{textColor}\">{text}</color>";
    }

    private string RemoveColorTag(string input)
    {
        var pattern = @"<color=""[^""]*"">|</color>";
        var output = Regex.Replace(input, pattern, "");
        return output;
    }

    public void DetachUILogger()
    {
        OvrAvatarLog.UILogListener -= ReceiveLogMessageForUI;
    }

    private void ReceiveLogMessageForUI(OvrAvatarLog.ELogLevel logLevel, string msg, string prefix)
    {
        if (!_isActive)
        {
            return;
        }

        var postPrefix = GetPostPrefixFromLogLevel(logLevel);
        var formattedLogText = $"{logLevel}|" + AddColorToTextFromLogLevel($"{prefix}{postPrefix} {msg}", logLevel);
        s_logCacheQueue.Enqueue(formattedLogText);
        if (s_logCacheQueue.Count > s_logCacheSize)
        {
            s_logCacheQueue.Dequeue();
        }

        if (IsAtOrAboveMinLogLevel(formattedLogText))
        {
            if (s_displayLogCacheQueue.Count > s_displayLogCacheSize)
            {
                s_displayLogCacheQueue.Dequeue();
            }
            s_displayLogCacheQueue.Enqueue(formattedLogText);
            _displayLogCacheQueueChanged = true;
        }
    }

    private void UpdateLoggerUI(bool forceUpdate = false)
    {
        if (!_displayLogCacheQueueChanged && !forceUpdate)
        {
            return;
        }

        var logs = new string[s_displayLogCacheQueue.Count];
        s_displayLogCacheQueue.CopyTo(logs, 0);

        var start = Math.Max(0, logs.Length - logViewCapacity);
        s_logQueueText = "\n";

        for (var i = start; i < logs.Length; i++)
        {
            s_logQueueText += logs[i].Substring(logs[i].IndexOf('|') + 1) + "\n";
        }

        if (logText != null)
        {
            logText.text = s_logQueueText;
        }

        _displayLogCacheQueueChanged = false;
    }

    private bool IsAtOrAboveMinLogLevel(string log)
    {
        if (string.IsNullOrEmpty(log))
        {
            return false;
        }

        string[] parts = log.Split('|');
        if (parts.Length < 2)
        {
            OvrAvatarLog.LogError($"UILogger::IsAtOrAboveMinLogLevel : Log message is not in the expected format {log}", logScope);
            return false;
        }

        if (!Enum.TryParse(parts[0], out OvrAvatarLog.ELogLevel logLevel))
        {
            OvrAvatarLog.LogError($"UILogger::IsAtOrAboveMinLogLevel : Log level could not be parsed from the following log: {log}", logScope);
            return false;
        }

        return logLevel >= minLogLevel;
    }

    private string GetLogExportFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "AvatarsSDKUI_Logs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
    }

    private void ExportAllLogs()
    {
        var path = GetLogExportFilePath();
        using (StreamWriter writer = new StreamWriter(path))
        {
            string logQueueText = "\n";
            foreach (var log in s_logCacheQueue)
            {
                logQueueText += RemoveColorTag(log.Substring(log.IndexOf('|') + 1)) + "\n";
            }
            writer.Write(logQueueText);
        }
        OvrAvatarLog.LogWarning($"UILogger::ExportLogs : Logs exported to \"{path}\"");
    }

    private void ExportFilteredLogs()
    {
        var path = GetLogExportFilePath();
        using (StreamWriter writer = new StreamWriter(path))
        {
            string logQueueText = "\n";
            foreach (var log in s_logCacheQueue)
            {
                if (IsAtOrAboveMinLogLevel(log))
                {
                    logQueueText += RemoveColorTag(log.Substring(log.IndexOf('|') + 1)) + "\n";
                }
            }
            writer.Write(logQueueText);
        }
        OvrAvatarLog.LogWarning($"UILogger::ExportFilteredLogs : Logs exported to \"{path}\"");
    }

    private void Update()
    {
        if (!UIManager.IsPaused)
        {
            return;
        }

        if (logText != null && _displayLogCacheQueueChanged)
        {
            UpdateLoggerUI();
        }

#if USING_XR_SDK
        if (OVRInput.GetUp(_increaseLogLevelButton.button, _increaseLogLevelButton.controller))   // increase log level
        {
            if (logLevelDropdown != null)
            {
                logLevelDropdown.value++;
            }
        }

        if (OVRInput.GetUp(_decreaseLogLevelButton.button, _decreaseLogLevelButton.controller))   // decrease log level
        {
            if (logLevelDropdown != null)
            {
                logLevelDropdown.value--;
            }
        }
#endif
    }

#if USING_XR_SDK
    public List<UIInputControllerButton> GetControlSchema()
    {
        var buttons = new List<UIInputControllerButton>
        {
            _increaseLogLevelButton,
            _decreaseLogLevelButton,
        };

        return buttons;
    }
#endif
}
