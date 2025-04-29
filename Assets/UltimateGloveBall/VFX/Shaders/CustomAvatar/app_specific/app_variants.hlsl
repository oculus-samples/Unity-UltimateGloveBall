// This file is for controlling the variants made for the shader. Some will be options and others hard-coded.
// If your app needs its own variants, rename this file with your app's name and place them here.
// This file should persist across succesive integrations.

// In each of these scenarios, choose the variant option with
// your application in mind. More variants provide versatility
// and allow display of all avatar profiles. However most apps
// only use one of the option, and doing so will save a large
// number of variations, and compiled shader memory.

#pragma multi_compile STYLE_1_LIGHT STYLE_1_STANDARD STYLE_2_LIGHT STYLE_2_STANDARD STYLE_2_EXPERIMENTAL

// SKIN_ON: Enhanced skin extension support
// EYE_GLINTS_ON: Enhanced eye extension support
// HAS_NORMAL_MAP_ON: Detail addition via Normal mapping:
// ENABLE_HAIR_ON: Enhanced hair extension support
// ENABLE_RIM_LIGHT_ON: Rim light extension support
// SIMPLE_OCCLUSION: A more optimized occlusion model for light shaders

#if defined(STYLE_1_LIGHT)
    #define EYE_GLINTS_ON
    #define SKIN_ON
    // #define HAS_NORMAL_MAP_ON
    // #define ENABLE_HAIR_ON
    // #define ENABLE_RIM_LIGHT_ON
#elif defined(STYLE_1_STANDARD)
    #define EYE_GLINTS_ON
    #define SKIN_ON
    #define HAS_NORMAL_MAP_ON
    // #define ENABLE_HAIR_ON
    // #define ENABLE_RIM_LIGHT_ON
#elif defined(STYLE_2_LIGHT)
    #define EYE_GLINTS_ON
    #define SKIN_ON
    #define HAS_NORMAL_MAP_ON
    // #define ENABLE_HAIR_ON
    // #define ENABLE_RIM_LIGHT_ON
    #define SIMPLE_OCCLUSION
#elif defined(STYLE_2_STANDARD)
    #define EYE_GLINTS_ON
    #define SKIN_ON
    #define HAS_NORMAL_MAP_ON
    #define ENABLE_HAIR_ON
    #define ENABLE_RIM_LIGHT_ON
    #define SSS_CURVATURE
    #define SSS_PER_VERTEX_CURVATURE
#else // STYLE_2_EXPERIMENTAL
    #define EYE_GLINTS_ON
    #define SKIN_ON
    #define HAS_NORMAL_MAP_ON
    #define ENABLE_HAIR_ON
    #define ENABLE_RIM_LIGHT_ON
    #define SSS_CURVATURE
    #define SSS_PER_VERTEX_CURVATURE
#endif

// Material mode support for FastLoads; Vertex base color
// All variants supported:
#pragma multi_compile MATERIAL_MODE_TEXTURE MATERIAL_MODE_VERTEX
// Only one chosen:
// #define MATERIAL_MODE_TEXTURE

// External Buffer support for Compute Skinning (Quest only uses external).
// All variants supported:
#pragma multi_compile EXTERNAL_BUFFERS_ENABLED EXTERNAL_BUFFERS_DISABLED
// Only one chosen:
// #define EXTERNAL_BUFFERS_ENABLED

// Defining a "debug mode toggle" to remove all debug code completely
// to increase performance
#pragma multi_compile __ DEBUG_MODE_ON
// Only one chosen:
// #define DEBUG_MODE_ON
