# AvatarSDKManager Prefabs

For almost all use cases, we recommend including `Recommended/MetaAvatarSDKManager.prefab` in your scene to manage Meta Avatars.

The other prefabs in this folder are deprecated or for reference / testing only. The difference between the prefabs is the `ShaderManager` that they contain. `MetaAvatarSDKManager` contains `MetaAvatarShaderManager` (which in turn points uses the shader `Common/Shaders/Recommended/Avatar-Shader`).

## Improvements

The major improvement from our previous default, `AvatarSDKManagerLibrary`, is the new shader as mentioned above.

The previous default shader, `Common/Shaders/Deprecated/Library/Avatar-Library.shader` was machine generated and difficult to read and modify. The new shader (`Common/Shaders/Recommended/Avatar-Shader`) is still machine generated, but with significant improvements to readability.

## Deprecated

The prefabs in the `Deprecated` folder are provided as convenience and will be removed in future versions of the SDK.

## ReferenceOnly

The `ReferenceOnly` prefabs are such as `UnityStandard` are provided for reference and debugging. These are simple shaders with poor performance.

## Example

See the `LightingExample.unity` scene to toggle through the different shaders via the `AvatarSDKManager` prefabs.
