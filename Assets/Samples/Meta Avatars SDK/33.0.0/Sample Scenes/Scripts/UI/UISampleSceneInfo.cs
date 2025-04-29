#nullable enable

using UnityEngine;

[CreateAssetMenu(fileName = "UISampleSceneInfo",
    menuName = "MetaAvatarsSDK/[Internal] Create UISampleSceneInfo scriptable object", order = 2)]
public class UISampleSceneInfo : ScriptableObject
{
    public string? sceneName;
    [TextArea]
    public string? sceneDescription;
}
