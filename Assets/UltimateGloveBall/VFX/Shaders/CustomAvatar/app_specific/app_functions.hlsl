// App specific functions that can be called from the exported Library shader
// If your app needs its own declarations, rename this file with your app's name and place them here.
// This file should persist across succesive integrations.

#pragma multi_compile __ ENABLE_CUSTOM_EFFECT
#pragma multi_compile __ INVERT
#pragma multi_compile __ ENABLE_GHOST_EFFECT

#if defined(ENABLE_CUSTOM_EFFECT)
#include "AvatarDisolveEffect.cginc"
#endif

#if defined(ENABLE_GHOST_EFFECT)
#include "AvatarGhostEffect.cginc"
#endif

// This requires MetaAvatarCore.hlsl
void AlexVert(AvatarVertexInput stage_input)
{
    
}

// This function allows for app specific operations at the beginning of the fragment shader
void AppSpecificPreManipulation(inout avatar_FragmentInput i) {
    // Call app specific functions from here.
}

// This function allows for app specific operations at the beginning of the fragment shader
void AppSpecificPostManipulation(avatar_FragmentInput i, inout avatar_FragmentOutput o) {
    
    #if defined(ENABLE_CUSTOM_EFFECT)
    o.color = ApplyDissolveEffect(o.color, i.geometry.positionInWorldSpace, i.geometry.normal);
    #endif

    #if defined(ENABLE_GHOST_EFFECT)
    o.color = ApplyGhostEffect(o.color, i.geometry.normal, i.geometry.positionInWorldSpace, i.geometry.screenPos, i.geometry.texcoord_0);
    #endif
    
    #if defined(ENABLE_CUSTOM_EFFECT) || defined(ENABLE_GHOST_EFFECT)
    clip(o.color.a - 0.5);
    #endif
}
