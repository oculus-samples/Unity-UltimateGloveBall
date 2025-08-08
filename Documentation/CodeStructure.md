# Overview

This project consists of two main structures. The first is the [Meta Multiplayer for Netcode and Photon](../Packages/com.meta.multiplayer.netcode-photon) package, which provides core reusable code for starting a new multiplayer game project. The second is [UltimateGloveBall](../Assets/UltimateGloveBall), which builds on the Meta Multiplayer base to implement specific game logic.

We also have a package of common utility functions that accelerated our project implementation. These utilities are in from our utility package repo [meta-quest/Unity-UtilityPackages](https://github.com/meta-quest/Unity-UtilityPackages).

To extend Photon Realtime for Netcode, we copied the package to [Packages/com.community.netcode.transport.photon-realtime](../Packages/com.community.netcode.transport.photon-realtime@b28923aa5d).

# Meta Multiplayer for Netcode and Photon

This package contains reusable logic for any networked multiplayer project. It includes essential elements and key features from our Platform Social API.

- [BlockUserManager.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/BlockUserManager.cs) implements the blocking flow API.
- [GroupPresenceState.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/GroupPresenceState.cs) uses the group presence API, enabling players to play together easily.
- [NetworkLayer.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/NetworkLayer.cs) manages client/host connection flow, disconnection handling, and host migration.

The networked Avatar implementation is crucial for integrating personality into a project. It demonstrates how avatars can be easily integrated ([Avatars](../Packages/com.meta.multiplayer.netcode-photon/Avatar)).

# Ultimate Glove Ball

This section covers the specific game implementation. We highlight key components and encourage you to explore the code.

## Application

The application starts with the [UGBApplication](../Assets/UltimateGloveBall/Scripts/App/UGBApplication.cs) script, which instantiates the main systems for the application's lifetime. It handles app navigation, network logic, user group presence setup, and initial user loading.

In [UltimateGloveBall/Scripts/App](../Assets/UltimateGloveBall/Scripts/App), you'll find the core application elements.

## Main Menu

The [MainMenu directory](../Assets/UltimateGloveBall/Scripts/MainMenu) contains controllers and views for the Main Menu scene. It's designed for easy menu extension and navigation. The [MainMenuController.cs](../Assets/UltimateGloveBall/Scripts/MainMenu/MainMenuController.cs) manages scene logic, states, transitions, and core service communication.

## Arena

The [Arena directory](../Assets/UltimateGloveBall/Scripts/Arena) contains all gameplay logic for the Arena.

### Services

We have two modes for joining the Arena: player or spectator. To prevent exceeding the maximum number of users for each role, we utilize the [ArenaApprovalController](../Assets/UltimateGloveBall/Scripts/Arena/Services/ArenaApprovalController.cs). The [ArenaPlayerSpawningManager](../Assets/UltimateGloveBall/Scripts/Arena/Services/ArenaPlayerSpawningManager.cs) then manages the spawning of players in the correct locations and on the appropriate teams.

### Players

Player construction involves multiple networked objects. The player avatar is the core element, with glove armatures and gloves interacting separately on a network level. These are in [Scripts/Arena/Player](../Assets/UltimateGloveBall/Scripts/Arena/Player).

### Spectators

In spectator mode, we simplify networking by reusing crowd bodies and animations. Users control item display and a firework launcher. To reduce network events, item changes are synchronized after a delay. The spectator code is in [Scripts/Arena/Spectator](../Assets/UltimateGloveBall/Scripts/Arena/Spectator).

### Balls

The main networking element is the [balls](../Assets/UltimateGloveBall/Scripts/Arena/Balls), consisting of the ball network, state synchronizer, and specific behaviors. The ball network manages states like ownership and gameplay logic. The state synchronizer handles position and movement, detailed in the [ball physics and networking](./BallPhysicsAndNetworking.md) documentation.

# Photon Realtime Transport for Netcode

We modified this package for more flexibility in navigating Photon rooms. We added lobby connection support and different connection intents. We also integrated room properties for specific room types, allowing easy reuse across projects by assigning callbacks and data handlers.
