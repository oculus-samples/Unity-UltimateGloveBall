# Meta Utilities Package

This package contains general utilities for Unity development.

|Utility|Description|
|-|-|
|[AutoSet](./AutoSet.cs) attributes|<p>This attribute is useful for eliminating calls to `GetComponent`. By annotating a serialized field with `[AutoSet]`, every instance of that field in a Prefab or Scene will automatically be assigned in editor (by calling `GetComponent`). This assignment is done both in the inspector (using a [property drawer](./Editor/AutoSetDrawer.cs)) as well as every time the object is saved (using an [asset postprocessor](./Editor/AutoSetPostprocessor.cs)).</p>Note, you can also use `[AutoSetFromParent]` or `[AutoSetFromChildren]`.|
|[Singleton](./Singleton.cs)|Simple implementation of the singleton pattern for `MonoBehaviour`s. Access the global instance through the static `Instance` property. Utilize in your own class through inheritance, for example:<br />`public class MyBehaviour : Singleton<MyBehaviour>`|
|[Multiton](./Multiton.cs)|Similar to `Singleton`, this class gives global access to *all* enabled instances of a `MonoBehaviour` through its static `Instances` property. Utilize in your own class through inheritance, for example:<br />`public class MyBehaviour : Multiton<MyBehaviour>`|
|[EnumDictionary](./EnumDictionary.cs)|This is an optimized Dictionary class for use with enum keys. It works by allocating an array that is indexed by the enum key. It can be used as a serialized field, unlike `System.Dictionary`.|
|[Extension Methods](./ExtensionMethods.cs)|A library of useful extension methods for Unity classes.|
|[Netcode Hash Fixer](./Editor/NetcodeHashFixer.cs)|The `NetworkObject` component uses a unique id (`GlobalObjectIdHash`) to identify objects across the network. However, certain instances (for example, instances of prefab variants) do not generate these IDs properly. This asset postprocessor ensures that the IDs are always regenerated, which prevents issues networking between the Editor and builds.|
|[Network Settings Toolbar](./Editor/NetworkSettingsToolbar.cs)|<img src="./Media/NetworkSettingsToolbar.png" width="512" /><br />This toolbar allows for improved iteration speed while working with [ParrelSync](https://github.com/brogan89/ParrelSync) clones. By consuming the properties set in the [NetworkSettings](./NetworkSettings.cs) class, multiple Editor instances of the project can automatically join the same instance.|
|[Settings Warning Toolbar](./Editor/SettingsWarningsToolbar.cs)|<img src="./Media/SettingsWarningsToolbar.png" width="512" /><br />This toolbar gives a helpful warning when the build platform is not set to Android, and gives an option to switch it. This is useful for ensuring that the build platform is Android while doing Quest development.|
|[Build Tools](./Editor/BuildTools.cs)|The `BuildTools` class contains methods for use by Continuous Integration systems.|
|[Menu Helpers](./Editor/MenuHelpers.cs)|When the [Unity Search Extensions package](https://github.com/Unity-Technologies/com.unity.search.extensions) is enabled, this adds a helpful context menu item "Graph Dependencies" and adds the "Tools/Find MIssing Dependencies" menu item.|
|[Android Helpers](./AndroidHelpers.cs)|This class gives access to Android [Intent](https://developer.android.com/reference/android/content/Intent) extras.|
|Animation State [Triggers](./AnimationStateTriggers.cs) / [Listeners](./AnimationStateTriggerListener.cs)|These classes enable any `Object` to bind methods to respond to its `Animator`'s `OnStateEnter` and `OnStateExit` events.|
|[Camera Facing](./CameraFacing.cs)|Simple component for billboarding a renderer.|
|[Dont Destroy On Load (On Enable)](./DontDestroyOnLoadOnEnable.cs)|Simple component that calls `DontDestroyOnLoad` in its `OnEnable`.|
|[Set Material Properties (On Enable)](./SetMaterialPropertiesOnEnable.cs)|Simple component that sets up a `MaterialPropertyBlock` for a renderer in its `OnEnable`.|
|[Nullable Float](./NullableFloat.cs)|A serializeable wrapper around `float` that exposes a `float?` through its `Value` property. It uses `NaN` as a sentinel for `null`.|
