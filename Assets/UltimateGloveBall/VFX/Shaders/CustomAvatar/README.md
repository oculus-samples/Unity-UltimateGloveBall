# CUSTOM AVATAR

Let's look on how we integrated custom effect to the avatar for the game.


In order to integrate custom effects on the Avatar we needed to make a copy of the Avatar-Meta.shader and the MetaAvatarCore.hlsl.

## Avatar-Meta-UGB.shader
[Avatar-Meta-UGB.shader](./Avatar-Meta-UGB.shader)is a copy of the Avatar-Meta.shader from the package. We needed this because of the relative path to the app_specific implementation and additional shader properties. 
You can see that we have the [app_specific](./app_specific) directory which contains [app_declarations.hlsl](./app_specific/app_declarations.hlsl) and [app_functions.hlsl](./app_specific/app_functions.hlsl).

[app_functions.hlsl](./app_specific/app_functions.hlsl) is where we were able to insert the calls for the [AvatarDisolveEffect.cginc](./AvatarDisolveEffect.cginc) and [AvatarGhostEffect.cginc](./AvatarGhostEffect.cginc).

We also needed to update the list of properties in the shader so we could use them to customize the 2 effects from the material.

## MetaAvatarCore.hlsl
As for [MetaAvatarCore.hlsl](./MetaAvatarCore.hlsl), it's a copy of the file in the package, but for the Ghost effect we needed to screen position which wasn't offered so we needed to include it. 

## Modifications
For all modifications from the original we wrapped them in comments so that it will be easier to update in the future.
```
// MOD START Ultimate-Gloveball: {description}
{modification}
// MOD END Ultimate-Gloveball
```

## EFFECTS

### Dissolve

The dissolve effect is implemented in [AvatarDisolveEffect.cginc](./AvatarDisolveEffect.cginc). This is the effect we apply when a player spawns in and out.

### Ghost

The ghos effect is implemented in [AvatarGhostEffect.cginc](./AvatarGhostEffect.cginc). This is the effect we apply when the user is invulnerable, either after spawning or when holding the ghost ball.

### Shader Graph functions

The effects were generated using shader graph, and it uses some specific functions that we included in [ShaderGraphFunctions.cginc](ShaderGraphFunctions.cginc) so it's decoupled from shader graph it self.

### Alpha Clipping
Since we didn't want to use transparency for the avatars we use clipping, to generate holes in the mesh and keep it on the opaque layer. This is integrated in the [app_functions.hlsl](./app_specific/app_functions.hlsl) for both effects.
