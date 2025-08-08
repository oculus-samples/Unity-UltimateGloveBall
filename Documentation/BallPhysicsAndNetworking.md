# Ball Physics And Networking

## Implementation Overview

We aimed to enable physics prediction on all clients to resolve issues like floating balls and delayed shooting/throwing for non-host clients. We adopted State Synchronization, inspired by Glenn Fiedler's blog: [State Synchronization](https://www.gafferongames.com/post/state_synchronization/). Each client runs Unity Physics locally and synchronizes with server data.

## Main Scripts Involved

- **[BallNetworking.cs](../Assets/UltimateGloveBall/Scripts/Arena/Balls/BallNetworking.cs)**
  - Manages throwing, collisions, and ownership.

- **[BallStateSync.cs](../Assets/UltimateGloveBall/Scripts/Arena/Balls/BallStateSync.cs)**
  - Collaborates with BallNetworking.cs to send data packets with data (if server) and apply them (if client).
  - Includes gradual position, rotation, and linear velocity correction to prevent pops and jerky movements.
  - Uses a jitter buffer to apply packets in order and discard late ones.

- **[BallSpawner.cs](../Assets/UltimateGloveBall/Scripts/Arena/Balls/BallSpawner.cs)**
  - Manages ball spawning and despawning of dead balls.

- **[SpawnPoint.cs](../Assets/UltimateGloveBall/Scripts/Arena/Balls/SpawnPoint.cs)**
  - Ensures a ball claims a spawn point, preventing others from spawning there.

## State Syncing Balls

### BallPacket

Packets from the server are applied to clients based on who threw the ball. Each packet includes:

- **(uint) Sequence**: Server frame number when packet is sent.
- **State Update**:
  - **(bool) IsGrabbed**: Indicates tp client if the ball is assigned to a glove.
  - **(ulong) GrabbersNetworkObjectId**: Identifies to client the glove for assignment.
  - **(Vector3) Position**: Ball's server position.
  - **(Quaternion) Orientation**: Ball's server rotation.
  - **(bool) SyncVelocity**: Indicates to client if velocity data is included in the packet.
  - **(Vector3) LinearVelocity**: Ball's server linear velocity.
  - **(Vector3) AngularVelocity**: Ball's server angular velocity.

### Why Assign the Ball to the Glove?

We initially synchronized the position while someone held the ball. However, the update rate and smoothing of avatars did not match those of the balls. Next, we tried to reparent the game object to the glove, but this caused issues with local position and rotation. To address these challenges, we gave the glove a reference to the ball, allowing it to control the ball's position while holding it.

### Key Takeaways

Using Netcode's "Auto Parent Sync" was too slow and lacked server control, causing issues for the player throwing the ball. Attaching the ball to the glove game object and syncing its local position resulted in inaccurate outcomes, likely due to the event timing and the ball's relative position to the glove. Moreover, the parenting and unparenting process incurred unnecessary costs since we already had logic in place for the ball to follow the glove.

## Applying Packets

Packet application depends on four factors:

### Grabbed Ball?

When a ball is grabbed, we:

- Attach it to the glove.
- Disable physics.
- Reset local transform.

Upon release, we:

- Detach from the glove.
- Enable physics.
- Sync position/rotation.

### Owner of the Ball?

If you own the ball, no extra grabbing rules apply.

### Did I Throw the Ball?

Others will snap the ball to new values and apply glove release rules.

### Is the Ball Still?

The server omits velocity data for still balls to save bandwidth. Clients snap position and rotation to zero for still balls.

# Grabbing Balls

Ball grabbing is server-authoritative, causing a slight lag between glove collision and ball capture. This choice reduces player frustration from server decisions. We welcome suggestions for gameplay mechanics to improve this experience. See our [CONTRIBUTING](../CONTRIBUTING.md) information.

# Collisions

Balls have different states and types, posing collision detection challenges. Spawned balls can't be hit by in-play balls, so we created a separate physics layer. The electric ball can pass through obstacles and shields, disabling them. We created a specific physics layer for it and used triggers to detect contact without applying physics.

# Noticing Desynchronization

Thrown balls are short-lived, minimizing desynchronization. Visual disparities may occur, but large colliders and game speed reduce perceived issues.
