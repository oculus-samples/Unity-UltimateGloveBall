# Avatars

We integrated Meta Avatars into this project to ensure continuity. By reusing the platform Avatar, users can recognize each other across different applications, enhancing user identity and social interaction.

The Meta Avatar SDK, downloaded from the [developer website](https://developers.meta.com/horizon/downloads/package/meta-avatars-sdk/), was combined with the Meta Multiplayer for Netcode and Photon package located in the Packages directory ([Packages/com.meta.multiplayer.netcode-photon](../Packages/com.meta.multiplayer.netcode-photon)).

For the integration, we followed the guidelines on the [developer website](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/). The [AvatarEntity.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarEntity.cs) file shows how we set up the Avatar for body, lip sync, face, and eye tracking. This setup is used in the [PlayerAvatarEntity Prefab](../Assets/UltimateGloveBall/Prefabs/Arena/Player/PlayerAvatarEntity.prefab), which contains all the behaviors and settings for in-game Avatar use. We track the Camera Rig root to keep the avatar synchronized with the user's position.

More information on face and eye tracking is available [here](https://developers.meta.com/horizon/documentation/unity/meta-avatars-face-eye-pose/).

## Networking

Implementing a networking solution for the Avatar is essential for our multiplayer game. This is handled in [AvatarNetworking.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarNetworking.cs). We use the `RecordStreamData` function on the avatar entity to get the data to stream over the network. This data is sent via RPC and received by other clients. On the receiving end, we apply the data using the `ApplyStreamData` function, which applies the state of the Avatar. We also implemented a frequency to send different levels of detail (LOD) to reduce bandwidth while maintaining Avatar motion fidelity.

## Custom Shader

To add effects to models while retaining the same visuals as the provided shader in the Avatar SDK's shader, we copied the shader files to our project ([here](../Assets/UltimateGloveBall/VFX/Shaders/CustomAvatar)) for modifications. We implemented new functionalities in `.cginc` files to minimize differences from the original shader files.

- [AvatarGhostEffect.cginc](../Assets/UltimateGloveBall/VFX/Shaders/CustomAvatar/AvatarGhostEffect.cginc): This effect triggers when the player takes the ghost ball, creating transparency by cutting small holes in the mesh and adding a fresnel effect and colors.

- [AvatarDisolveEffect.cginc](../Assets/UltimateGloveBall/VFX/Shaders/CustomAvatar/AvatarDisolveEffect.cginc): This effect triggers when the player spawns or despawns, creating a dissolving illusion using alpha cutting and coloring.

The main functions from these effects are called in [app_functions.hlsl](../Assets/UltimateGloveBall/VFX/Shaders/CustomAvatar/app_specific/app_functions.hlsl) and are triggered through keywords. The shader file was also modified to include the necessary properties for these effects to work.

We made minimal changes to the copied files. This approach ensures easy updates to the shader files with the latest shaders in newer versions of the Meta Avatar SDK.

More information is available in the associated [Custom Avatars README](../Assets/UltimateGloveBall/VFX/Shaders/CustomAvatar/README.md).
