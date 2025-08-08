![Ultimate Glove Ball Banner](./Documentation/Media/banner.png "Ultimate Glove Ball")

# Ultimate Glove Ball

The VR Developer Tools team created Ultimate Glove Ball to showcase how to build an ESport game using the Oculus Social Platform API. It is based on our [SharedSpaces](https://github.com/oculus-samples/Unity-SharedSpaces) project, with expanded functionalities for an ESport game context. This project demonstrates how VR games can offer asymmetric experiences featuring both players and spectators.

This codebase serves as a reference and template for multiplayer VR games. You can try the game on the [Meta Horizon Store - Ultimate Glove Ball](https://www.meta.com/en-gb/experiences/ultimate-glove-ball/5704438046269164/).

## Project Description

This application for Meta Quest devices showcases a fast-paced sports game playable with friends or strangers. It integrates user connections for joining random games or specific rooms, inviting friends, launching group parties in the same arena, or joining as a spectator. The project includes Meta Avatars for player representation and voice chat for easy communication.

Built with the Unity engine, the project uses [Photon Realtime](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime) as the transport layer and [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects). It also includes [Meta Utilities](./Packages/com.meta.utilities/README.md) and [Meta Input Utilities](./Packages/com.meta.utilities.input/README.md) packages, offering useful tools and methods.

## How to Run the Project in Unity

1. [Configure the project](./Documentation/Configuration.md) with Meta Quest and Photon.
2. Use Unity 6000.0.50f1 or newer.
3. Load the [Assets/UltimateGloveBall/Scenes/Startup](./Assets/UltimateGloveBall/Scenes/Startup.unity) scene.
4. Test in the editor using two methods:

    <details>
      <summary><b>Quest Link</b></summary>

      + Enable Quest Link: Put on your headset, go to "Quick Settings," and select "Quest Link" (or "Quest Air Link" if using Air Link).
      + Choose your desktop from the list and select "Launch" to control your desktop from your headset.
      + With the headset on, select "Desktop" from the control panel. You should see your desktop in VR.
      + Navigate to Unity and press "Play" to launch the application on your headset.
    </details>

    <details>
      <summary><b>XR FPS Simulator</b></summary>

      + In Unity, press "Play" to enjoy the simulated XR controls.
      + Review the [XR FPS Simulator documentation](./Packages/com.meta.utilities.input/README.md#xr-device-fps-simulator) for more information. The mouse is [captured by the simulator](./Packages/com.meta.utilities.input/README.md#mouse-capture) in play mode. To use the mouse in-game, hold Left Alt.
    </details>

## Dependencies

This project uses the following plugins and software:

- [Unity](https://unity.com/download) 6000.0.50f1 or newer
- [Dependencies Hunter](https://github.com/AlexeyPerov/Unity-Dependencies-Hunter.git#upm)
- [Meta Avatars SDK](https://developers.meta.com/horizon/downloads/package/meta-avatars-sdk/)
- [Meta XR Utilities](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.utilities)
- [Oculus Integration SDK](https://developers.meta.com/horizon/downloads/package/unity-integration/): released under the *[Oculus SDK License Agreement](./Assets/Oculus/LICENSE.txt)*.
- [ParrelSync](https://github.com/brogan89/ParrelSync)
- [Photon Realtime for Netcode](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime)
- [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518)
- [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- [Unity Toolbar Extender](https://github.com/marijnz/unity-toolbar-extender.git)

To test this project in Unity, you need [The Meta Quest App](https://www.meta.com/quest/setup/).

# Getting the Code

First, ensure Git LFS is installed by running:

```sh
git lfs install
```

Then, clone this repository using the "Code" button above or this command:

```sh
git clone https://github.com/oculus-samples/Unity-UltimateGloveBall.git
```

# Documentation

More information is available in the [Documentation](./Documentation) section.

- [Avatars](./Documentation/Avatars.md)
- [Ball Physics And Networking](./Documentation/BallPhysicsAndNetworking.md)
- [Code Structure](./Documentation/CodeStructure.md)
- [Configuration](./Documentation/Configuration.md)
- [In-App Purchases (IAP)](./Documentation/IAP.md)
- [Light Baking](./Documentation/LightBaking.md)
- [Multiplayer](./Documentation/Multiplayer.md)

Custom Packages:

- [Meta Multiplayer for Netcode and Photon](./Packages/com.meta.multiplayer.netcode-photon/README.md)
- [Meta Utilities](./Packages/com.meta.utilities/README.md)
- [Meta Input Utilities](./Packages/com.meta.utilities.input/README.md)

# Where are the Meta Avatar SDK and Photon Packages?

The [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) package is stored in the [Packages](./Packages) folder. To update them, import their updated Asset Store packages and copy them into their respective `Packages` folders.

The *Photon Voice 2* package is released under the *[License Agreement for Exit Games Photon](./Packages/Photon/Photon/license.txt)*.

The [Photon Realtime for Netcode](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime) package is copied in the [Packages](./Packages) folder as `com.community.netcode.transport.photon-realtime@b28923aa5d` since it was modified to fit our needs.

# License

Most of Ultimate GloveBall is licensed under [MIT LICENSE](./LICENSE). Files from [Text Mesh Pro](https://unity.com/legal/licenses/unity-companion-license), [Photon Voice](./Packages/Photon/Photon/license.txt), and [Photon SDK](./Packages/com.community.netcode.transport.photon-realtime@b28923aa5d/Runtime/Photon/LICENSE.md) are licensed under their respective terms.

# Contribution

See the [CONTRIBUTING](./CONTRIBUTING.md) file for how to contribute.
