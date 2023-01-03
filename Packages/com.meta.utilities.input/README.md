# Meta Input Utilities Package

This package contains utilities relating to Unity's Input System.

You can install this package by adding the following git url to [Package Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html): `https://github.com/oculus-samples/Unity-UltimateGloveBall.git?path=/Packages/com.meta.utilities.input`

## XR Toolkit for Meta Avatars

In order to use XR Toolkit with the Meta Avatars SDK, you can use the [XRInputManager](./XRInputManager.cs) in this package. Simply attach it to your camera rig prefab, and assign it to your Avatar Entity using the [`SetBodyTracking` API](https://developer.oculus.com/documentation/unity/meta-avatars-ovravatarentity/#tracking-input).

## XR Device FPS Simulator

![Demo of XR Device FPS Simulator showing Ultimate GloveBall](./Media/XRDeviceFpsSimulator.gif)

The [XRDeviceFpsSimulator](./XRDeviceFpsSimulator.cs) class works similarly to Unity's [XRDeviceSimulator](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.3/manual/xr-device-simulator.html). It drives simulated XR devices using mouse and keyboard, moving the player around similar to a first-person shooter game. This is particularly useful for testing in-editor as an alternative to Quest Link.

### Installation

If your game is already configured to use XR Toolkit, this simulator can be dropped into your project very easily. Simply add the [XRDeviceFpsSimulator prefab](./XRDeviceFpsSimulator.prefab) to your scene. If there are no real XR devices detected, the simulated devices will be loaded.

### Controls

You can modify the input actions on the simulator component to tailor the controls to your game's needs. The defaults controls are as follows:

<img src="./Media/SimulatorBindings1.png" width=400 /><img src="./Media/SimulatorBindings2.png" width=400 /> 

### Mouse capture

![Demo of XR Device FPS Simulator mouse toggle showing Ultimate GloveBall](./Media/XRDeviceFpsSimulator-Mouse.gif)

By default, the simulator will capture the mouse when you click the game view. In order to use the mouse for other things (such as interacting with the game UI), you can hold the "Release Mouse Capture Action" (in this case, the left Alt key). From there, you can use mouse input as normal in the game.

## Extra Utilities

|Utility|Description|
|-|-|
|[XRTrackedPoseDriver](./XRTrackedPoseDriver.cs)|A simple extension to `TrackedPoseDriver` that calls a `UnityEvent` when updated.|
|[InverseModifierComposite](./InverseModifierComposite.cs)|A variant of the `OneModifierComposite` action, which only activates while the modifier is not activated.|
