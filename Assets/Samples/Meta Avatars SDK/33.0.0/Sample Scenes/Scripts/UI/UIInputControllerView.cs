#nullable enable

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System.Collections.Generic;
using System.Linq;
using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.UI;

public class UIInputControllerView : MonoBehaviour
{
    private const string logScope = "UIInputControllerView";
    [SerializeField] private Text? controlsTextObject;

    private const int DIVIDER_PADDING = 15;
    private const int LINE_PADDING = 30;

    private bool _initialized;

    private HashSet<string>? _controlScopesRegistered;


    public void PopulateControllerView()
    {
#if USING_XR_SDK
        if (_initialized)
        {
            return;
        }

        _controlScopesRegistered = new HashSet<string>();

        if (controlsTextObject == null)
        {
            OvrAvatarLog.LogError("UIInputControllerView::CreateDividerTextObject : Null controlsTextMesh.", logScope);
            return;
        }

        controlsTextObject.text = "";

        var controllerInterfaces = FindObjectsOfType<MonoBehaviour>().OfType<IUIControllerInterface>().ToList();
        if (!controllerInterfaces.Any())
        {
            OvrAvatarLog.LogWarning("UIInputControllerView::PopulateControllerView : did not find any instances of IUIControllerInterface.", logScope);
            return;
        }

        foreach (var controllerInterface in controllerInterfaces)
        {
            var controllerList = controllerInterface.GetControlSchema();

            _controlScopesRegistered ??= new HashSet<string>();

            if (_controlScopesRegistered.Contains(controllerList[0].scope))
            {
                // skip repeated controls
                continue;
            }

            AddDividerText(controllerList[0].scope);

            for (var i = 0; i < controllerList.Count; i++)
            {
                AddControllerText(controllerList[i]);
            }
        controlsTextObject.text += "\n\n";
        }

        _initialized = true;

        if (UIManager.Instance == null || !UIManager.Instance.Initialized)
        {
            OvrAvatarLog.LogError("UIInputControllerView::PopulateControllerView : Failed to retrieve UIManager instance.", logScope);
            return;
        }

        UIManager.Instance.AddOnPauseEvent(RefreshControllerView);
#endif
    }

    private void RefreshControllerView()
    {
#if USING_XR_SDK
        var controllerInterfaces = FindObjectsOfType<MonoBehaviour>().OfType<IUIControllerInterface>().ToList();
        if (!controllerInterfaces.Any())
        {
            OvrAvatarLog.LogWarning("UIInputControllerView::RefreshControllerView : did not find any instances of IUIControllerInterface.", logScope);
            return;
        }

        foreach (var controllerInterface in controllerInterfaces)
        {
            var controllerList = controllerInterface.GetControlSchema();
            _controlScopesRegistered ??= new HashSet<string>();

            if (_controlScopesRegistered.Contains(controllerList[0].scope))
            {
                // skip repeated controls
                continue;
            }
            AddDividerText(controllerList[0].scope);
            for (var i = 0; i < controllerList.Count; i++)
            {
                AddControllerText(controllerList[i]);
            }

        if (controlsTextObject == null)
            {
                OvrAvatarLog.LogError("UIInputControllerView::RefreshControllerView : Null controlsTextMesh.", logScope);
                return;
            }

            controlsTextObject.text += "\n\n";
        }
#endif
    }

    private void AddDividerText(string scope)
    {
        _controlScopesRegistered ??= new HashSet<string>();
        _controlScopesRegistered.Add(scope);

        if (controlsTextObject == null)
        {
            OvrAvatarLog.LogError("UIInputControllerView::AddDividerText : Null controlsTextMesh.", logScope);
            return;
        }

        var divider = new string('=', DIVIDER_PADDING);
        var formattedText = $"<color=#5CFFFD><b>{divider}  {scope.Trim()}  {divider}</b></color>\n\n";
        controlsTextObject.text += formattedText;
    }

#if USING_XR_SDK
    private void AddControllerText(UIInputControllerButton controllerButton)
    {
        var controlsText = "";
        // combination buttons
        if (controllerButton.combinationButtons != null && controllerButton.combinationButtons.Count > 0)
        {
            controlsText += controllerButton.controller + " - " + controllerButton.combinationButtons[0];
            for (var i = 1; i < controllerButton.combinationButtons.Count; i++)
            {
                controlsText += " + " + controllerButton.combinationButtons[i];
            }
        }
        else
        {
            controlsText = controllerButton.controller + " - " + (controllerButton.button != default ? controllerButton.button.ToString() : controllerButton.axis2d.ToString());
        }

        if (this.controlsTextObject == null)
        {
            OvrAvatarLog.LogError("UIInputControllerView::AddControllerText : Null controlsTextMesh.", logScope);
            return;
        }

        controlsText = controlsText.Trim().PadRight(LINE_PADDING);
        this.controlsTextObject.text += controlsText + ": " + controllerButton.description.Trim() + "\n";
    }
#endif
}
