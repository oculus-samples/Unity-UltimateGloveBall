# Meta Multiplayer for Netcode and Photon Package

This package contains core implementation to start a multiplayer project while using Netcode for gameobject and Photon as the transport layer.

## Avatar
|Script|Description|
|-|-|
|[AvatarEntity](./Avatar/AvatarEntity.cs)|Implementation of the OvrAvatarEntity that sets up the avatar based on the user ID, integrates the body tracking, events on joints loaded, hide and show avatar, tracks camera rig, as well as local and remote setup.|
|[AvatarNetworking](./Avatar/AvatarNetworking.cs)|Combined with the AvatarEntity, this script handles the networked updates of the avatar state. For a local avatar it will send the data to other players and for a remote avatar it will receive and apply the updates. You can send the data LOD frequency based on your needs.| 

## Core
|Script|Description|
|-|-|
|[BlockUserManager](./Core/BlockUserManager.cs)|Handles the [platform blocking APIs](https://developer.oculus.com/documentation/unity/ps-blockingsdk/). On initialize you get the list of blocked users and it centralizes the logic to block, unblock and query if a user is blocked.|
|[CameraRigRef](./Core/CameraRigRef.csm)|Singleton that keeps a reference to the camera rig and OvrAvatarInputManager for easy access through the application.|
|[ClientNetworkTransform](./Core/ClientNetworkTransform.cs)|Based on the [netcode networktransform documentation](https://docs-multiplayer.unity3d.com/netcode/current/components/networktransform/index.html#clientnetworktransform), it handles client authoritative transform synchronization.|
|[GroupPresenceState](./Core/GroupPresenceState.cs)|Handles the [platform GroupPresence API](https://developer.oculus.com/documentation/unity/ps-group-presence-overview/) and keeps track of the user presence state. This is used for social platform functionalities like invites, rosters and join.|
|[NetworkLayer](./Core/NetworkLayer.cs)|This is the core for handling the networking state. It handles connection as Host or Client, disconnection and reconnection flows. It supplies multiple callback for different state changes that can be handled at the application implementation level, keeping this agnostic from the application implementation.|
|[NetworkSession](./Core/NetworkSession.cs)|A network behaviour spawned by the host to sync information about the current session. It syncs the photon voice room name and contains logic to detect and sync which client would be the fallback host if the host disconnects.|
|[SceneLoader](./Core/SceneLoader.cs)|Handles loading the scene based on the networking context. It also keeps track of which scene is loaded and when the load is completed.|
|[VoipController](./Core/VoipController.cs)|Controls the creation of the speaker(remote) or the recorder(local) as well as microphone permissions. It also connects the voip to the right room when it is set.|
|[VoipHandler](./Core/VoipHandler.cs)|Keeps a reference of the speaker or recorder and handles muting the right component.|