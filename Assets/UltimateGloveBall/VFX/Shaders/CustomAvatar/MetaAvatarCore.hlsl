// MOD START Ultimate-GloveBall
// Copy from Packages/com.meta.xr.sdk.avatars/Scripts/Common/Shaders/Recommended/MetaAvatarCore.hlsl
// added modification to support Ulitmate-Gloveball custom effects see "MOD START Ultimate-GloveBall"
// * update links to package scripts
// * added screenPos support for the effects
// MOD END Ultimate-GloveBall


// Output from shpp V1.0 at 2023-10-17 16:34:26.625953
// Builtin macros by shpp
#define GLSL_LANG 1
#define HLSL_LANG 2
#define SPARKSL_LANG 3
#define GLES_LANG 4
#define __SL_LANG__ HLSL_LANG
// Language is HLSL
// This should be comming from the shpp command line, hardcode while testing.

#define HOST_CONFIG UNITY_CONFIG_GENERIC
// configs.glsl

    #ifndef DEC_bool_useSubmesh
 #define DEC_bool_useSubmesh
 static const bool useSubmesh = true;
 #endif

// eof configs.glsl
#ifdef UNITY_PIPELINE_URP

    #pragma multi_compile_instancing

    // Per vertex is faster than per pixel, and almost indistinguishable for our purpose
    #define USE_SH_PER_VERTEX

    // This is the URP pass so set this define to activate OvrUnityGlobalIllumination headers
    #define USING_URP

    // URP includes
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
    // MOD START Ultimate-Gloveball: link to package scripts
    #include "Packages/com.meta.xr.sdk.avatars/Scripts/ShaderUtils/OvrUnityLightsURP.hlsl"
    #include "Packages/com.meta.xr.sdk.avatars/Scripts/ShaderUtils/OvrUnityGlobalIlluminationURP.hlsl"
    // MOD END Ultimate-Gloveball

#else

    #pragma multi_compile DIRECTIONAL POINT SPOT

    // Per vertex is faster than per pixel, and almost indistinguishable for our purpose
    #define LIGHTPROBE_SH 1

    // Built-in rendering includes
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"
    #include "UnityStandardInput.cginc"
    // MOD START Ultimate-Gloveball: link to package scripts
    #include "Packages/com.meta.xr.sdk.avatars/Scripts/ShaderUtils/OvrUnityGlobalIlluminationBuiltIn.hlsl"
    // MOD END Ultimate-Gloveball

#endif

// VERTEX COLORS: activate this to transmit lo-fi model vert colors or sub mesh information in the alpha channel
#define HAS_VERTEX_COLOR_float4

// DEBUG_MODE_ON: this connects our important DEBUG_MODE_ON define to the one created by Keyword the Unity HLSL wrapper
#define DEBUG_MODE_ON (defined(DEBUG_NONE))

// SHADER OPTIONS: For the Shader Library System, include either the next set or pragmas OR
// the #defines below them:

// In order to toggle these values in editor, uncomment the following set:
#pragma multi_compile _ HAS_NORMAL_MAP_ON
#pragma multi_compile _ SKIN_ON
#pragma multi_compile _ EYE_GLINTS_ON
#pragma multi_compile _ ENABLE_HAIR_ON
#pragma multi_compile _ ENABLE_RIM_LIGHT_ON

// To reduce permutations in the final product, hard code these as shown in this example:
// #define HAS_NORMAL_MAP_ONk
// #define SKIN_ON
// #define EYE_GLINTS_ON
// #define ENABLE_HAIR_ON
// #define ENABLE_RIM_LIGHT_ON

// MATERIAL_MODES: Must match the Properties specified above
#pragma multi_compile MATERIAL_MODE_TEXTURE MATERIAL_MODE_VERTEX

// Some platforms don't support reading from external buffers, provide a keyword to toggle
#pragma multi_compile EXTERNAL_BUFFERS_ENABLED EXTERNAL_BUFFERS_DISABLED

// Include app-specific dependencies
// App specific declarations that have to happen before the exported Library shader
// If your app needs its own declarations, rename this file with your app's name and place them here.
// This file should persist across succesive integrations.

// Keyword Definitions

// Scoped Variables (Uniforms)

// MOD START Ultimate-Gloveball: link to package scripts
#include "Packages/com.meta.xr.sdk.avatars/Scripts/ShaderUtils/AvatarCustom.cginc"
// MOD END Ultimate-Gloveball

// On/Off switches for various options.

#ifdef HAS_NORMAL_MAP_ON
#ifndef DEC_bool_enableNormalMapping
 #define DEC_bool_enableNormalMapping
 static const bool enableNormalMapping = true;
 #endif
#else
#ifndef DEC_bool_enableNormalMapping
 #define DEC_bool_enableNormalMapping
 static const bool enableNormalMapping = false;
 #endif
#endif

#ifdef SKIN_ON
#ifndef DEC_bool_enableSkin
 #define DEC_bool_enableSkin
 static const bool enableSkin = true;
 #endif
#else
#ifndef DEC_bool_enableSkin
 #define DEC_bool_enableSkin
 static const bool enableSkin = false;
 #endif
#endif

#ifdef EYE_GLINTS_ON
#ifndef DEC_bool_enableEyeGlint
 #define DEC_bool_enableEyeGlint
 static const bool enableEyeGlint = true;
 #endif
#else
#ifndef DEC_bool_enableEyeGlint
 #define DEC_bool_enableEyeGlint
 static const bool enableEyeGlint = false;
 #endif
#endif

#ifdef ENABLE_HAIR_ON
#ifndef DEC_bool_enableHair
 #define DEC_bool_enableHair
 static const bool enableHair = true;
 #endif
#else
#ifndef DEC_bool_enableHair
 #define DEC_bool_enableHair
 static const bool enableHair = false;
 #endif
#endif

#ifdef ENABLE_RIM_LIGHT_ON
#ifndef DEC_bool_enableRimLight
 #define DEC_bool_enableRimLight
 static const bool enableRimLight = true;
 #endif
#else
#ifndef DEC_bool_enableRimLight
 #define DEC_bool_enableRimLight
 static const bool enableRimLight = false;
 #endif
#endif

#ifdef HAS_ALPHA_TO_COVERAGE_ON
#ifndef DEC_bool_enableAlphaToCoverage
 #define DEC_bool_enableAlphaToCoverage
 static const bool enableAlphaToCoverage = true;
 #endif
#else
#ifndef DEC_bool_enableAlphaToCoverage
 #define DEC_bool_enableAlphaToCoverage
 static const bool enableAlphaToCoverage = false;
 #endif
#endif

#ifdef DEBUG_MODE_ON

#ifdef ENABLE_PREVIEW_COLOR_RAMP_ON
#ifndef DEC_bool_enablePreviewColorRamp
 #define DEC_bool_enablePreviewColorRamp
 static const bool enablePreviewColorRamp = true;
 #endif
#else
#ifndef DEC_bool_enablePreviewColorRamp
 #define DEC_bool_enablePreviewColorRamp
 static const bool enablePreviewColorRamp = false;
 #endif
#endif

#endif

float4 getVertexInClipSpace(float3 pos) {
    #ifdef USING_URP
        return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4 (pos,1.0)));
    #else
        return UnityObjectToClipPos(pos);
    #endif
}

#ifdef USING_URP
struct VertexInput {
    float2 uv0;
    float2 uv1;
};

float4 VertexGIForward(VertexInput v, float3 posWorld, float3 normalWorld){
      return float4(SampleSHVertex(normalWorld), 1.0);
}
#endif

// Maximum Number of lights for this host target.
#define MAX_LIGHT_COUNT 4
// avatar_structs.vert.glsl

struct avatar_VertOptions {
    bool enableNormalMapping;

    bool enableAlphaToCoverage;
    bool useSkinning;

    bool enableSkin;
    bool enableEyeGlint;
};

struct avatar_AvatarVertexInput {
    float4 position;
    float3 normal;
    float3 tangent;
    float2 texcoord0;
    float2 texcoord1;
    float4 color;
    float4 ormt;
    float3 ambient;
    // uint vertexId; // GLES doesn't support uint. Until we remove swiftshader, we can't use uints.
    avatar_VertOptions options;
};

struct avatar_VertexOutput {
    float4 positionInClipSpace;
    float3 positionInWorldSpace;
    float3 normal;
    float3 tangent;
    float2 texcoord0;
    float2 texcoord1;
    float4 color;
};

struct avatar_Transforms {
    float4x4 viewProjectionMatrix;
    float4x4 modelMatrix;
};
// eof avatar_structs.vert.glsl
// Start of include: avatar_structs.frag.glsl

struct avatar_Light {
    float3 direction;
    float3 color;
    float intensity;
    float shadowTerm;
    int type;
    int eyeGlint;
};

struct avatar_FragOptions {
    // global
    bool useIBLRotation;
    bool enableNormalMapping;
    bool enableAlphaToCoverage;
    bool enableRimLight;

    bool enableSkin;
    bool enableEyeGlint;
    bool enableHeadHair;
    bool enableFacialHair;
    bool enableAnyHair;
    bool enableShadows;
    bool enableLightMask;
#ifdef DEBUG_MODE_ON
    bool hasBaseColorMap;
    bool hasMetallicRoughnessMap;
    bool enablePreviewColorRamp;
#endif
};

struct avatar_LightLookParams {
    /* **** multi-sample specular values **** */
    // how many light samples to take in the x and y directions
    // ex. if this value is 5, we'll take 25 samples
    // default: 5
    int lightSampleCount;
    // the size of the light. Informs how far apart to space each sample
    // default: 0.5
    float lightSize;
    // for very shiny materials we see each sample directly reflected in a grid
    // to remove this artifact we set a roughness value below which we significantly
    // reduce the light size
    // default: 0.2
    float roughnessCutOff;
    // decrease the lightsize for very shiny surfaces to
    // mix(lowerBound, lightSize, saturate((material.roughness - reduceRoughnessByValue) * reducedLightSizeMultiplier)
    // defaults:
    // lowerBound: 0.05
    // reduceRoughnessByValue: 0.1
    // reducedLightSizeMultiplier: 10.0
    float lowerBound;
    float reduceRoughnessByValue;
    float reducedLightSizeMultiplier;
    // final multiplier for the overall multi sample specular contribution
    // default: 0.45
    float multiSampleBrightnessMultiplier;

    /* **** single-sample specular values **** */
    // final multiplier for the overall single sample specular contribution
    // default: 0.075
    float singleSampleBrightnessMultiplier;

};

struct avatar_SkinLookParams {
    // 4 IBL samples are summed up and then multiplied by this number
    // for ambient contribution to skin
    // default: 0.5
    float SSSIBLMultiplier;
    // artificially increase occlusion for subsurface color contribution
    // default: 0.4;
    float increaseOcclusion;
};

struct avatar_SkinMaterial {
    float3 subsurface_color;
    float3 skin_ORM_factor;
};

struct avatar_HairMaterial {
    float3 subsurface_color;
    float scatter_intensity;
    float3 specular_color_factor;
    float specular_shift_intensity;
    float specular_white_intensity;
    float specular_white_roughness;
    float specular_color_intensity;
    float specular_color_offset;
    float specular_color_roughness;
    float anisotropic_intensity;
    float diffused_intensity;
    float normal_intensity;
    float specular_glint;
    float ao_intensity;
    float flow_sample;
    float flow_angle;
    float shift;
    float blend;
    float aniso_blend;
};

struct avatar_RimLightMaterial {
    float intensity;
    float bias;
    float3 color;
    float transition;
    float start;
    float end;
};

struct avatar_Material {
    float3 base_color;
    float alpha;
    float exposure;
    float metallic;
    float occlusion;
    float roughness;
    float thickness;

    float ambient_diffuse_factor;
    float ambient_specular_factor;

    float eye_glint_factor;
    float eye_glint_color_factor;

    avatar_SkinMaterial skin_material;
    avatar_HairMaterial hair_material;
    avatar_RimLightMaterial rim_light_material;

    float ramp_selector;
    int color_selector_lod;

    // The cosine of the angle lighting should contribute (wrap)
    // past 90 degrees (where light would normally stop contributing)
    // Ex. for a value of -.15 means wrapping 8.629 degrees past 90 degrees
    // cos( 8.629) = -0.15
    float SSSWrapAngle;
    float diffuseWrapAngle;

    float f0;
    float f90;
};

struct avatar_Matrices {
    float4x4 objectToWorld;
    float3x3 worldToObject;
    float4x4 viewProjection;
    float3x3 environmentRotation;
};

struct avatar_TangentSpace {
    float3 normal;
    float3 tangent;
    float3 bitangent;
};

struct avatar_Geometry {
    float3 camera;
    float3 positionInClipSpace;
    float3 positionInWorldSpace;
    // MOD START Ultimate-GloveBall: add screenPos support
    float4 screenPos;
    // MOD END Ultimate-GloveBall

    float3 normal;
    float3 normalMap;

    avatar_TangentSpace tangentSpace;

    float2 texcoord_0;
    float2 texcoord_1;
    float4 color;
    float4 ormt;
    float normalScale;
    bool invertNormalSpaceEyes;

    float3 worldViewDir;

    float lod;
};

struct avatar_AmbientLighting {
    float3 diffuse;
    float3 specular;
};

struct avatar_FragmentInput {

    avatar_FragOptions options;

    avatar_Matrices matrices;

    avatar_Geometry geometry;
    avatar_Material material;
    avatar_LightLookParams lightLookParams;
    avatar_SkinLookParams skinLookParams;

    int lightsCount;
    avatar_Light lights[MAX_LIGHT_COUNT];

    int lightMask;
    float eyeGlintExponential;
    float eyeGlintIntensity;

    float3 ambient_color;
    float ambient_occlusion;
#ifdef DEBUG_MODE_ON
    int debugMode;
#endif
};

struct avatar_FragmentOutput {
    float4 color;
    float3 p_specular;
    float3 p_diffuse;
    float3 a_specular;
    float3 a_diffuse;
    float3 subSurfaceColor;
    uint alphaCoverage;
};

// End of include: avatar_structs.frag.glsl
// platform_frag.glsl
float4 SRGBtoLINEAR(float4 srgbIn);
float3 SRGBtoLINEAR(float3 srgbIn);
float4 LINEARtoSRGB(float4 color);
float3 LINEARtoSRGB(float3 color);

float4 ConvertInputColorSpaceToLinear(float4 unknownInput)
{
#ifdef UNITY_COLORSPACE_GAMMA

  return SRGBtoLINEAR(unknownInput);
#else

  return unknownInput;
#endif
}

float3 ConvertOutputColorSpaceFromSRGB(float3 srgbInput)
{
#ifdef UNITY_COLORSPACE_GAMMA
  return srgbInput;
#else
  return SRGBtoLINEAR(srgbInput);
#endif
}

float3 ConvertOutputColorSpaceFromLinear(float3 linearInput)
{
#ifdef UNITY_COLORSPACE_GAMMA
  return LINEARtoSRGB(linearInput);
#else
  return linearInput;
#endif
}

int getLightCount() {
#ifdef USING_URP
    return GetAdditionalLightsCount() + 1;
#else
    return 1;
#endif
}

float3 getLightDirection() {
  #ifdef USING_URP
    return -GetMainLight().direction;
  #else
      return -_WorldSpaceLightPos0;
  #endif
}

float3 getLightColor() {
#ifdef USING_URP
    return _MainLightColor;
#else
    return _LightColor0;
#endif
}

float3 getLightPosition() {
#ifdef USING_URP
    return _MainLightPosition;
#else
    return _WorldSpaceLightPos0;
#endif
}

OvrLight getAdditionalLight(int idx, float3 worldPos){
#ifdef USING_URP
    return OvrGetAdditionalLight(idx, worldPos);
#else
    OvrLight dummy;
    return dummy;
#endif
}

// StaticSelectMaterialMode functions are available to enable FastLoad avatars.
// Upon defining MATERIAL_MODE_VERTEX, colors/properties will be read from vertex attributes
// instead of textures. This allows for a fast and compact avatar model, although less detailed.

float4 StaticSelectMaterialModeColor(sampler2D texSampler, float2 texCoords, float4 vertexColor) {
#if defined(MATERIAL_MODE_VERTEX)
  return vertexColor;
#else
  float4 colorSample = tex2D(texSampler, texCoords);

  return ConvertInputColorSpaceToLinear(colorSample);
#endif
}

float4 StaticSelectMaterialModeProperty(sampler2D texSampler, float2 texCoords, float4 vertexColor, float bias) {
#if defined(MATERIAL_MODE_VERTEX)
  return vertexColor;
#else
  return tex2Dlod(texSampler, float4(texCoords, 0.0, bias));
#endif
}

// eof platform_frag.glsl
// submesh.frag.glsl
static const float avatar_SUBMESH_TYPE_NONE      = 0.0    / 256.0;
static const float avatar_SUBMESH_TYPE_OUTFIT    = 1.0    / 256.0;
static const float avatar_SUBMESH_TYPE_BODY      = 2.0    / 256.0;
static const float avatar_SUBMESH_TYPE_HEAD      = 4.0    / 256.0;
static const float avatar_SUBMESH_TYPE_HAIR      = 8.0    / 256.0;
static const float avatar_SUBMESH_TYPE_EYEBROW   = 16.0   / 256.0;
static const float avatar_SUBMESH_TYPE_L_EYE     = 32.0   / 256.0;
static const float avatar_SUBMESH_TYPE_R_EYE     = 64.0   / 256.0;
static const float avatar_SUBMESH_TYPE_LASHES    = 128.0  / 256.0;
static const float avatar_SUBMESH_TYPE_FACIALHAIR= 256.0  / 256.0;
static const float avatar_SUBMESH_TYPE_HEADWEAR  = 512.0  / 256.0;
static const float avatar_SUBMESH_TYPE_EARRINGS  = 1024.0 / 256.0;

static const float avatar_SUBMESH_TYPE_BUFFER= 0.5 / 256.0;

bool avatar_WithinSubmeshRange(float idChannel, float lowBound, float highBound) {
    bool condition = ((idChannel > (lowBound - avatar_SUBMESH_TYPE_BUFFER)) &&
                        (idChannel < (highBound + avatar_SUBMESH_TYPE_BUFFER)));
    return condition;
}

bool avatar_IsSubmeshType(float idChannel, float subMeshType) {
    return avatar_WithinSubmeshRange(idChannel, subMeshType, subMeshType);
}

// eof submesh.frag.glsl
// material_type.frag.glsl
static const float avatar_MATERIAL_TYPE_CLOTHES     = 1.0   / 255.0;
static const float avatar_MATERIAL_TYPE_EYES        = 2.0   / 255.0;
static const float avatar_MATERIAL_TYPE_SKIN        = 3.0   / 255.0;
static const float avatar_MATERIAL_TYPE_HEAD_HAIR   = 4.0   / 255.0;
static const float avatar_MATERIAL_TYPE_FACIAL_HAIR = 5.0   / 255.0;

static const float avatar_MATERIAL_TYPE_BUFFER      = 0.5   / 255.0;

bool avatar_WithinMaterialRange(float idChannel, float lowBound, float highBound) {
    bool condition = ((idChannel > (lowBound - avatar_SUBMESH_TYPE_BUFFER)) &&
                        (idChannel < (highBound + avatar_SUBMESH_TYPE_BUFFER)));
    return condition;
}

bool avatar_IsMaterialType(float idChannel, float materialTypeID) {
    return avatar_WithinSubmeshRange(idChannel, materialTypeID, materialTypeID);
}

bool avatar_UseEyeGlint(float idChannel) {
  return avatar_IsMaterialType(idChannel, avatar_MATERIAL_TYPE_EYES);
}

bool avatar_UseSkin(float idChannel) {
  return avatar_IsMaterialType(idChannel, avatar_MATERIAL_TYPE_SKIN);
}

bool avatar_UseHeadHair(float idChannel) {
  return avatar_IsMaterialType(idChannel, avatar_MATERIAL_TYPE_HEAD_HAIR);
}

bool avatar_UseFacialHair(float idChannel) {
  return avatar_IsMaterialType(idChannel, avatar_MATERIAL_TYPE_FACIAL_HAIR);
}
// eof material_type.frag.glsl
// colors.frag.glsl
static const float GAMMA = 2.2;
static const float INV_GAMMA = 1.0 / GAMMA;

// Tone Mapping
// sRGB => XYZ => D65_2_D60 => AP1 => RRT_SAT
static const float3x3 ACESInputMat = float3x3 (
  0.59719, 0.07600, 0.02840,
  0.35458, 0.90834, 0.13383,
  0.04823, 0.01566, 0.83777
);

// ODT_SAT => XYZ => D60_2_D65 => sRGB
static const float3x3 ACESOutputMat = float3x3 (
  1.60475, -0.10208, -0.00327,
  -0.53108,  1.10813, -0.07276,
  -0.07367, -0.00605,  1.07602
);

// -----------------------------------------------------------------------------------
// ACES filmic tone map approximation
// see https://github.com/TheRealMJP/BakingLab/blob/master/BakingLab/ACES.hlsl
float3 RRTAndODTFit(float3 color) {
    float3 a = color * (color + 0.0245786) - 0.000090537;
    float3 b = color * (0.983729 * color + 0.4329510) + 0.238081;
    return a / b;
}

float3 linearTosRGB(float3 color) {
    return pow(color, ((float3)(INV_GAMMA)));
}

// -----------------------------------------------------------------------------------
float3 sRGBToLinear(float3 srgbIn) {
    return float3(pow(srgbIn.rgb, ((float3)(GAMMA))));
}

// sRGB to linear approximation
// see http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html
float4 SRGBtoLINEAR(float4 srgbIn) {
  return float4(pow(srgbIn.rgb, ((float3)(GAMMA))), srgbIn.a);
}

float3 SRGBtoLINEAR(float3 srgbIn) {
  return pow(srgbIn.rgb, ((float3)(GAMMA)));
}

// linear to sRGB approximation
// see http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html
float4 LINEARtoSRGB(float4 color) {
  return float4(pow(color.rgb, ((float3)(INV_GAMMA))), color.a);
}

float3 LINEARtoSRGB(float3 color) {
  return pow(color.rgb, ((float3)(INV_GAMMA)));
}

float LINEARtoSRGB(float color) {
  return pow(color, INV_GAMMA);
}

#ifdef DEBUG_MODE_ON
// Hue sat and value are all in range [0,1]
float3 RGBtoHSV(float3 rgb)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = (lerp((float4(rgb.bg, K.wz)), (float4(rgb.gb, K.xy)), (step(rgb.b, rgb.g))));
    float4 q = (lerp((float4(p.xyw, rgb.r)), (float4(rgb.r, p.yzx)), (step(p.x, rgb.r))));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HSVtoRGB(float3 hsv)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac((hsv.xxx + K.xyz)) * 6.0 - K.www);
    return hsv.z * (lerp((K.xxx), (clamp(p - K.xxx, 0.0, 1.0)), (hsv.y)));
}

// https://github.com/tobspr/GLSL-Color-Spaces/blob/master/ColorSpaces.inc.glsl
static const float3x3 RGB_2_CIE = (float3x3(
    0.49000, 0.31000, 0.20000,
    0.17697, 0.81240, 0.01063,
    0.00000, 0.01000, 0.99000
));

static const float3x3 CIE_2_RGB = (float3x3(
     2.36461385,-0.89654057, -0.46807328,
    -0.51516621, 1.4264081,   0.0887581,
     0.0052037, -0.01440816,  1.00920446
));

static const float3x3 RGB_2_XYZ = (float3x3(
    0.4124564, 0.2126729, 0.0193339,
    0.3575761, 0.7151522, 0.1191920,
    0.1804375, 0.0721750, 0.9503041
));

static const float3x3 XYZ_2_RGB = (float3x3(
     3.2404542,-0.9692660, 0.0556434,
    -1.5371385, 1.8760108,-0.2040259,
    -0.4985314, 0.0415560, 1.0572252
));

// Converts a color from linear RGB to XYZ space
float3 rgb_to_xyz(float3 rgb) {
  return mul(rgb, RGB_2_XYZ);
}

// Converts a color from XYZ to linear RGB space
float3 xyz_to_rgb(float3 xyz) {
  return mul(xyz, XYZ_2_RGB);
}

// Converts a color from linear RGB to XYZ space
float3 rgb_to_cie(float3 rgb) {
  return mul(rgb, RGB_2_CIE);
}

// Converts a color from XYZ to linear RGB space
float3 cie_to_rgb(float3 xyz) {
  return mul(xyz, CIE_2_RGB);
}
#endif

// eof colors.frag.glsl
// prehost.frag.glsl

uniform sampler2D u_BaseColorSampler;
uniform sampler2D u_MetallicRoughnessSampler;
uniform sampler2D u_SphereMapEnvSampler;
uniform sampler2D u_NormalSampler;

// specific to the IBL implementation, could they be moved to ambient_image_based.frag.sca ?
uniform samplerCUBE u_LambertianEnvSampler;
uniform samplerCUBE u_GGXEnvSampler;

// specific to the VertexGI implementation, could they be moved to ambient_vertex_gi.frag.sca ?
uniform sampler2D _ICMap;  // NOTE: VertexGI only uses this in the case of Normal mapping. Else it uses l1 from the vert shader.
uniform samplerCUBE _ReflectionCubeMap; // Same old map, different name

uniform sampler2D u_ColorGradientSampler;

uniform float u_NormalScale;
uniform float4 u_BaseColorFactor;

uniform float u_Exposure;
uniform float u_MetallicFactor;
uniform float u_OcclusionStrength;

uniform float u_RoughnessFactor;
uniform float u_ThicknessFactor;
uniform float3 u_SubsurfaceColor;
uniform float3 u_SkinORMFactor;

uniform float u_EyeGlintFactor;
uniform float u_EyeGlintColorFactor;

uniform float3 u_HairSubsurfaceColor;
uniform float u_HairScatterIntensity;
uniform float3 u_HairSpecularColorFactor;
uniform float u_HairSpecularShiftIntensity;
uniform float u_HairSpecularWhiteIntensity;
uniform float u_HairSpecularColorIntensity;
uniform float u_HairSpecularColorOffset;
uniform float u_HairRoughness;
uniform float u_HairColorRoughness;
uniform float u_HairAnisotropicIntensity;
uniform float u_HairSpecularNormalIntensity;
uniform float u_HairDiffusedIntensity;
uniform float u_HairSpecularGlint;

uniform float3 u_FacialHairSubsurfaceColor;
uniform float u_FacialHairScatterIntensity;
uniform float3 u_FacialHairSpecularColorFactor;
uniform float u_FacialHairSpecularShiftIntensity;
uniform float u_FacialHairSpecularWhiteIntensity;
uniform float u_FacialHairSpecularColorIntensity;
uniform float u_FacialHairSpecularColorOffset;
uniform float u_FacialHairRoughness;
uniform float u_FacialHairColorRoughness;
uniform float u_FacialHairAnisotropicIntensity;
uniform float u_FacialHairSpecularNormalIntensity;
uniform float u_FacialHairDiffusedIntensity;
uniform float u_FacialHairSpecularGlint;

uniform float u_RimLightIntensity;
uniform float u_RimLightBias;
uniform float3 u_RimLightColor;
uniform float u_RimLightTransition;
uniform float u_RimLightStartPosition;
uniform float u_RimLightEndPosition;

uniform int u_RampSelector;

// MOD START Ultimate-GloveBall: Ambient Factor
uniform float u_AmbientDiffuseFactor;
// MOD End Ultimate-GloveBall


// These define the "look" of avatars. Everyone should use the same ones.
static const int lightSampleCount = 5;
static const float lightSize = 0.5;
static const float roughnessCutOff = 0.2;
static const float lowerBound = 0.05;
static const float reduceRoughnessByValue = 0.1;
static const float reducedLightSizeMultiplier = 10.0;
static const float multiSampleBrightnessMultiplier = 0.45;
static const float singleSampleBrightnessMultiplier = 0.225;
static const float SSSIBLMultiplier = 0.5;
static const float increaseOcclusion =  0.4;
static const float SSSWrapAngle = -0.65;
static const float diffuseWrapAngle = -0.15;
static const float f0 = 0.04;
static const float f90 = 1.0;

// eof prehost.frag.glsl
#ifdef DEBUG_MODE_ON
// debug.frag.glsl

static const int debug_None = 0;
static const int debug_BaseColor = 1;
static const int debug_Occlusion = 2;
static const int debug_Roughness = 3;
static const int debug_Metallic = 4;
static const int debug_Thickness = 5;
static const int debug_Normal = 6;
static const int debug_NormalMap = 7;
static const int debug_Emissive = 8;
static const int debug_View = 9;
static const int debug_Punctual = 10;
static const int debug_Punctual_Specular = 11;
static const int debug_Punctual_Diffuse = 12;
static const int debug_Ambient = 13;
static const int debug_Ambient_Specular = 14;
static const int debug_Ambient_Diffuse = 15;
static const int debug_No_Tone_Map = 16;
static const int debug_Subsurface_Scattering = 17;
static const int debug_Submeshes = 18;
static const int debug_Tangent = 19;
static const int debug_Bitangent = 20;
static const int debug_Ambient_Occlusion = 21;
static const int debug_MeshNormal = 22;
static const int debug_Alpha = 23;
static const int debug_MaterialType = 24;

float4 avatar_finalOutputColor(avatar_FragmentInput i, avatar_FragmentOutput o) {
    float4 color = float4(1,0,1,1);
    if (i.debugMode == debug_BaseColor) {
    color = float4(i.material.base_color.rgb, 1.0);
    }
    if (i.debugMode == debug_Occlusion) {
    color.rgb = float3(i.material.occlusion.rrr);
    }
    if (i.debugMode == debug_Roughness) {
    color.rgb = ConvertOutputColorSpaceFromSRGB(float3(i.material.roughness.rrr));
    if (i.options.enableAnyHair)
    {
        color.rgb = ConvertOutputColorSpaceFromSRGB(float3(i.material.hair_material.flow_sample.rrr));
    }
    }
    if (i.debugMode == debug_Metallic) {
    color.rgb = ConvertOutputColorSpaceFromSRGB(float3(i.material.metallic.rrr));
    }
    if (i.debugMode == debug_Thickness) {
    color.rgb = float3(i.material.thickness.rrr);
    }
    if (i.debugMode == debug_Normal) {
    color.rgb = i.geometry.normal * 0.5 + 0.5;
    }
    if (i.debugMode == debug_MeshNormal) {
    color.rgb = i.geometry.tangentSpace.normal * 0.5 + 0.5;
    }
    if (i.debugMode == debug_Tangent) {
    color.rgb = i.geometry.tangentSpace.tangent * 0.5 + 0.5;
    }
    if (i.debugMode == debug_Bitangent) {
    color.rgb = i.geometry.tangentSpace.bitangent * 0.5 + 0.5;
    }
    if (i.debugMode == debug_NormalMap) {
    color.rgb = i.geometry.normalMap.rgb;
    }
    if (i.debugMode == debug_Emissive) {

    }
    if (i.debugMode == debug_View) {
    color.rgb = i.geometry.worldViewDir * 0.5 + 0.5;
    }
    if (i.debugMode == debug_Punctual) {
    color.rgb = o.p_specular + o.p_diffuse;
    }
    if (i.debugMode == debug_Punctual_Specular) {
    color.rgb = o.p_specular;
    }
    if (i.debugMode == debug_Punctual_Diffuse) {
    color.rgb = o.p_diffuse;
    }
    if (i.debugMode == debug_Ambient) {
    color.rgb = o.a_specular + o.a_diffuse;
    }
    if (i.debugMode == debug_Ambient_Specular) {
    color.rgb = o.a_specular;
    }
    if (i.debugMode == debug_Ambient_Diffuse) {
    color.rgb = o.a_diffuse;
    }
    if (i.debugMode == debug_No_Tone_Map) {

    }
    if (i.debugMode == debug_Subsurface_Scattering) {
    color.rgb = o.subSurfaceColor;
    }
    if (i.debugMode == debug_Ambient_Occlusion) {
    color.rgb = float3(i.ambient_occlusion.rrr);
    }
    if (i.debugMode == debug_Submeshes) {
        float subMeshId = i.material.alpha;
        color.rgb = float3(0,0,0);
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_OUTFIT)) {
            color.rgb = float3(0.2,0.2,0.2);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_BODY)) {
            color.rgb = float3(0.77,0.65,0.65);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_HEAD)) {
            color.rgb = float3(0.77,0.65,0.65);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_HAIR)) {
            color.rgb = float3(0.345,0.27,0.11);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_EYEBROW)) {
            color.rgb = float3(0.24,0.19,0.08);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_L_EYE)) {
            color.rgb = float3(0.0,0.0,1.0);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_R_EYE)) {
            color.rgb = float3(0.0,1.0,0.0);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_LASHES)) {
            color.rgb = float3(0.5,0.0,0.0);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_FACIALHAIR)) {
            color.rgb = float3(0.2,0.1,0.05);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_HEADWEAR)) {
            color.rgb = float3(0.1,0.1,0.1);
        }
        if(avatar_IsSubmeshType(subMeshId, avatar_SUBMESH_TYPE_EARRINGS)) {
            color.rgb = float3(1.0,1.0,1.0);
        }
    }
    if (i.debugMode == debug_MaterialType) {
      float materialType = i.material.alpha;
      color.rgb = float3(1,0,1);
      if (avatar_IsMaterialType(materialType, avatar_MATERIAL_TYPE_CLOTHES)) {
        color.rgb = float3(0.2,0.2,0.2);
      }
      if (avatar_IsMaterialType(materialType, avatar_MATERIAL_TYPE_EYES)) {
        color.rgb = float3(0.9,0.9,0.9);
      }
      if (avatar_IsMaterialType(materialType, avatar_MATERIAL_TYPE_SKIN)) {
        color.rgb = float3(0.77,0.65,0.65);
      }
      if (avatar_IsMaterialType(materialType, avatar_MATERIAL_TYPE_HEAD_HAIR)) {
        color.rgb = float3(0.345,0.27,0.11);
      }
      if (avatar_IsMaterialType(materialType, avatar_MATERIAL_TYPE_FACIAL_HAIR)) {
        color.rgb = float3(0.2,0.1,0.05);
      }
    }
    return color;
}

float3 avatar_debugBaseColor(avatar_FragmentInput i) {
    if(i.options.enablePreviewColorRamp) {
      float2 rampCoord = float2(LINEARtoSRGB(i.material.base_color.r), i.material.ramp_selector);
      return tex2Dlod(u_ColorGradientSampler, float4(rampCoord, 0.0, float(i.material.color_selector_lod))).rgb;
    }
    return i.material.base_color;
}

// eof debug.frag.glsl
#else
// release.frag.glsl

float4 avatar_finalOutputColor(avatar_FragmentInput i, avatar_FragmentOutput fragmentOutput) {
    float4 color = fragmentOutput.color;
    return color;
  }

float3 avatar_debugBaseColor(avatar_FragmentInput i) {
    return i.material.base_color;
  }

// eof release.frag.glsl
#endif

void AppSpecificPreManipulation(inout avatar_FragmentInput i);
void AppSpecificPostManipulation(avatar_FragmentInput i, inout avatar_FragmentOutput o);
// pbr.vert.glsl

avatar_VertexOutput avatar_computeVertex(avatar_AvatarVertexInput i, avatar_Transforms transforms) {
    avatar_VertexOutput vout;

    float4 pos = mul(i.position, transforms.modelMatrix);
    float3 worldPos = float3(pos.xyz) / pos.w;

    vout.positionInClipSpace = mul(pos, transforms.viewProjectionMatrix);
    vout.positionInWorldSpace = worldPos;
    vout.normal = normalize(mul(i.normal, ((float3x3)(transforms.modelMatrix))));
    vout.texcoord0 = i.texcoord0;
    vout.texcoord1 = i.texcoord1;
    vout.color = i.color;
    vout.tangent = normalize(mul(i.tangent, ((float3x3)(transforms.modelMatrix))));

    return vout;
}
// unity.vert.glsl

// Options flags
// NOTE: For now in Unity these remain commented in the Vert shader.
// We do not yet have a way of using them in the vert shader and redefining them in the frag...

// Global Options vert

#ifndef DEC_bool_useSkinning
 #define DEC_bool_useSkinning
 static const bool useSkinning = false;
 #endif

avatar_VertOptions getVertOptions() {
  avatar_VertOptions options;
  options.enableNormalMapping = enableNormalMapping;
  options.enableAlphaToCoverage = enableAlphaToCoverage;
  options.enableSkin = enableSkin;
  options.enableEyeGlint = enableEyeGlint;
  options.useSkinning = useSkinning;
  return options;
}

uniform float4x4 u_ViewProjectionMatrix;
uniform float4x4 u_ModelMatrix;

static float4 a_Position;    // should be POSITION in Unity
static float4 a_Normal;      // should be NORMAL in Unity
static float4 a_Tangent;     // should be TANGENT in Unity
static uint a_vertexID;    // should be SV_VertexID in Unity
static float2 a_UV1;
static float2 a_UV2;
static float4 a_Color;
static float4 a_ORMT;

 float4 _output_FragColor;
static float4 v_Vertex;
static float3 v_WorldPos;
static float3 v_Normal;
static float4 v_Tangent;
static float2 v_UVCoord1;
static float2 v_UVCoord2;
static float4 v_Color;
static float4 v_ORMT;
// per-vertex ambient value calculated with spherical harmonics
static float3 v_SH;

// MOD START Ultimate-GloveBall: add screenPos support
static float4 v_ScreenPos;
// MOD END Ultimate-Gloveball

// eof unity.vert.glsl

// This is to fix the "redefinition of 'UnityDisplayOrientationPreTransform'" issue. The "HLSLSupport.cginc" and
// the "com.unity.render-pipelines.core/ShaderLibrary/API/Vulkan.hlsl" (which included in "com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl")
// can't be both included in 2021.3.30f1 or later version because they both define UnityDisplayOrientationPreTransform. In 2021.3.29f1 it is ok because
// "Vulkan.hlsl" in 2021.3.29f1 doesn't define UnityDisplayOrientationPreTransform.
// The fix is removing HLSLSupport.cginc here. The HLSLSupport.cginc is only needed for the UNITY_INITIALIZE_OUTPUT. So, I copied the
// UNITY_INITIALIZE_OUTPUT define to here.
//#include <HLSLSupport.cginc>
#ifndef UNITY_INITIALIZE_OUTPUT
    #if defined(UNITY_COMPILER_HLSL) || defined(SHADER_API_PSSL) || defined(UNITY_COMPILER_HLSLCC)
        #define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0;
    #else
        #define UNITY_INITIALIZE_OUTPUT(type,name)
    #endif
#endif

// Mapping of variables to shader semantics.

struct AvatarVertexInput
{
    float4 a_Color : COLOR;
    float4 a_ORMT : TEXCOORD3;
    float4 a_Normal : NORMAL;
    float4 a_Position : POSITION;
    float4 a_Tangent : TANGENT;
    float2 a_UV1 : TEXCOORD0;
    float2 a_UV2 : TEXCOORD1;
    uint a_vertexID : SV_VertexID;

    UNITY_VERTEX_INPUT_INSTANCE_ID

};

struct VertexToFragment
{
    float4 v_Color : COLOR;
    float4 v_ORMT : TEXCOORD5;

    float3 v_Normal : TEXCOORD2;
    float4 v_Tangent : TEXCOORD3;
    float2 v_UVCoord1 : TEXCOORD0;
    float2 v_UVCoord2 : TEXCOORD1;
    float4 v_Vertex : SV_POSITION;
    float3 v_WorldPos : TEXCOORD4;
    float3 v_SH : TEXCOORD6;
    // MOD START Ultimate-GloveBall: add screenPos support
    float4 v_ScreenPos : TEXCOORD7;
    // MOD END Ultimate-Gloveball

    UNITY_VERTEX_OUTPUT_STEREO

};

void vert_Vertex_main() {
  OvrVertexData ovrData = OvrCreateVertexData(
    float4(a_Position.xyz, 1.0),
    a_Normal.xyz,
    a_Tangent,
    a_vertexID
  );
  avatar_AvatarVertexInput vin;
  vin.position = ovrData.position;
  vin.normal = ovrData.normal;
  vin.tangent = ovrData.tangent.xyz;
  VertexInput vertexInput;
  vertexInput.uv0 = a_UV1;
  vertexInput.uv1 = a_UV2;
  vin.ambient = VertexGIForward(vertexInput, vin.position.rgb, vin.normal).rgb;

  vin.texcoord0 = a_UV1;
  vin.texcoord1 = a_UV2;
  vin.ormt = a_ORMT.rgba;
  vin.options = getVertOptions();

  vin.color.a = a_Color.a;
#if MATERIAL_MODE_VERTEX
  vin.color.rgb = SRGBtoLINEAR(a_Color.rgb); // vertex color are stored in s_rgb, convert to linear.
#else
  vin.color.rgb = a_Color.rgb; // Vertex colors are only used in MODE_VERTEX, skip conversion for speed.
#endif

  avatar_Transforms transforms;
  transforms.viewProjectionMatrix = UNITY_MATRIX_VP;
  transforms.modelMatrix = unity_ObjectToWorld;

  // common vertex stuff:

  // skip the vout structure, since it doesn't understand VR
  // APFX(VertexOutput) vout = APFX(computeVertex)(vin, transforms);

  v_UVCoord1 = vin.texcoord0;
  v_UVCoord2 = vin.texcoord1;
  v_Color = vin.color.rgba;
  v_ORMT = vin.ormt.rgba;
  v_SH = vin.ambient;

  // replacement for UnityObjectToClipPos
  //  v_Vertex = (vin.position * unity_ObjectToWorld) * UNITY_MATRIX_VP;
  v_Vertex = getVertexInClipSpace(vin.position.xyz/vin.position.w);

  // replacement for o.worldPos = mul(unity_ObjectToWorld, vertexData.position).xyz;
  float4 worldPos = mul(unity_ObjectToWorld, vin.position);
  v_WorldPos = worldPos.xyz / worldPos.w;

  v_Normal = normalize((mul(float4(vin.normal.xyz, 0.0), unity_WorldToObject)).xyz);
  v_Tangent = normalize(mul(float4(vin.tangent.xyz, 0.0), unity_WorldToObject));
  // MOD START Ultimate-GloveBall: add screenPos support
  v_ScreenPos = ComputeScreenPos(v_Vertex);
  // MOD END Ultimate-Gloveball
}

VertexToFragment Vertex_main_instancing(AvatarVertexInput stage_input)
{

    a_Position = stage_input.a_Position;
    a_Normal = stage_input.a_Normal;
    a_Tangent = stage_input.a_Tangent;
    a_vertexID = stage_input.a_vertexID;
    a_UV1 = stage_input.a_UV1;
    a_UV2 = stage_input.a_UV2;
    a_Color = stage_input.a_Color;
    a_ORMT = stage_input.a_ORMT;

    VertexToFragment stage_output;

    UNITY_SETUP_INSTANCE_ID(stage_input);
    UNITY_INITIALIZE_OUTPUT(VertexToFragment, stage_output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(stage_output);

    vert_Vertex_main();

    stage_output.v_Vertex = v_Vertex;
    stage_output.v_WorldPos = v_WorldPos;
    stage_output.v_Normal = v_Normal;
    stage_output.v_Tangent = v_Tangent;
    stage_output.v_UVCoord1 = v_UVCoord1;
    stage_output.v_UVCoord2 = v_UVCoord2;
    stage_output.v_Color = v_Color;
    stage_output.v_ORMT = v_ORMT;
    stage_output.v_SH = v_SH;
    // MOD START Ultimate-GloveBall: add screenPos support
    stage_output.v_ScreenPos = v_ScreenPos;
    // MOD END Ultimate-Gloveball

    return stage_output;
}
// UNITY VERT CONFIG LOADED

// eof pbr.vert.glsl
// Start of integration of replacement/platform_frag.hlsl

// End of integration of replacement/platform_frag.hlsl
// Begin features_unity.frag.glsl

avatar_FragmentOutput avatar_zeroFragmentOutput();

float3 avatar_tonemap(float3 color);
void avatar_addSubsurfaceContribution(avatar_FragmentInput i, avatar_AmbientLighting ambient, float3 diffuseWrap, inout avatar_FragmentOutput o);
float3 avatar_getGlintSpecularPerCamera(avatar_FragmentInput i);
float3 avatar_recalculateSpecularForEyeGlint(float3 halfVector, float3 specular, avatar_Light light, avatar_FragmentInput i);
float3 avatar_getGlintSpecularPerLight(avatar_Light light, avatar_FragmentInput i);
float3 avatar_getGlintSpecularPerAmbient(avatar_FragmentInput i);
bool avatar_eyeGlintEnableSpecularFactor(avatar_FragmentInput i);
bool avatar_eyeGlintAddAmbientSpecular(avatar_FragmentInput i);
bool avatar_eyeGlintConsiderLightMask(avatar_FragmentInput i);
float3 avatar_getEnvironmentSphereMap(avatar_FragmentInput i);
float3 avatar_addRimLight(avatar_Geometry geometry, avatar_Material material);
float3 avatar_computeDiffuse(avatar_Geometry geometry, avatar_Light light, avatar_Material material, out float3 diffuseWrap);
float3 avatar_computeSpecular(float3 normal, float3 worldViewDir, avatar_Light light, avatar_LightLookParams lightLookParams, float roughness, float exposure, avatar_FragOptions options);
float3 avatar_computeAmbientDiffuseLighting(avatar_FragmentInput i, float3 normal);
float3 avatar_computeAmbientDiffuseLighting(avatar_FragmentInput i);
float3 avatar_computeAmbientSpecularLighting(avatar_FragmentInput i, float3 specular_contribution);
avatar_AmbientLighting avatar_computeAmbientLighting(avatar_FragmentInput i, float3 specular_contribution);
void avatar_addHairLightContribution(avatar_FragmentInput i, avatar_AmbientLighting ambient, inout avatar_FragmentOutput o);
// tonemap_passtrough.frag.glsl
// utils.frag.glsl
// noise.frag.glsl
// -- Noise
// https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
float mod289(float x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
float4 mod289(float4 x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
float4 perm(float4 x){return mod289(((x * 34.0) + 1.0) * x);}

float noise(float3 p){

    float3 a = floor(p);
    float3 d = p - a;
    d = d * d * (3.0 - 2.0 * d);

    float4 b = a.xxyy + float4(0.0, 1.0, 0.0, 1.0);
    float4 k1 = perm(b.xyxy);
    float4 k2 = perm(k1.xyxy + b.zzww);

    float4 c = k2 + a.zzzz;
    float4 k3 = perm(c);
    float4 k4 = perm(c + 1.0);

    float4 o1 = frac((k3 * (1.0 / 41.0)));
    float4 o2 = frac((k4 * (1.0 / 41.0)));

    float4 o3 = o2 * d.z + o1 * (1.0 - d.z);
    float2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

    return o4.y * d.y + o4.x * (1.0 - d.y);
}

// The following noise functions are from Blender

float noise_scale3(float result)
{
  return 0.9820 * result;
}

float noise_fade(float t)
{
  return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float noise_grad(uint hash, float x, float y, float z)
{
  uint h = hash & 15u;
  float u = h < 8u ? x : y;
  float vt = ((h == 12u) || (h == 14u)) ? x : z;
  float v = h < 4u ? y : vt;
  return (((h & 1u) != 0u) ? -u : u) + (((h & 2u) != 0u) ? -v : v);
}

float noise_lerp(float t, float a, float b)
{
  return (1.0 - t) * a + t * b;
}
float floorfrac(float x, out uint i)
{
  float x_floor = floor(x);
  i = uint(x_floor);
  return x - x_floor;
}

uint rot(uint x, uint k) {
  return (x << k) | (x >> (32u - k));
}

uint hash(uint kx, uint ky, uint kz)
{
  uint a, b, c, len = 3u;
  a = b = c = 0xdeadbeefu + (len << 2u) + 13u;

  c += kz;
  b += ky;
  a += kx;
  c ^= b;
  c -= rot(b, 14u);
  a ^= c;
  a -= rot(c, 11u);
  b ^= a;
  b -= rot(a, 25u);
  c ^= b;
  c -= rot(b, 16u);
  a ^= c;
  a -= rot(c, 4u);
  b ^= a;
  b -= rot(a, 14u);
  c ^= b;
  c -= rot(b, 24u);
  return c;
}
float noise_perlin(float x, float y, float z) {
  uint X;
  float fx = floorfrac(x, X);
  uint Y;
  float fy = floorfrac(y, Y);
  uint Z;
  float fz = floorfrac(z, Z);

  float u = noise_fade(fx);
  float v = noise_fade(fy);
  float w = noise_fade(fz);

  float noise_u[2], noise_v[2];

  noise_u[0] = noise_lerp(
      u, noise_grad(hash(X, Y, Z), fx, fy, fz), noise_grad(hash(X + 1u, Y, Z), fx - 1.0, fy, fz));

  noise_u[1] = noise_lerp(u,
                          noise_grad(hash(X, Y + 1u, Z), fx, fy - 1.0, fz),
                          noise_grad(hash(X + 1u, Y + 1u, Z), fx - 1.0, fy - 1.0, fz));

  noise_v[0] = noise_lerp(v, noise_u[0], noise_u[1]);

  noise_u[0] = noise_lerp(u,
                          noise_grad(hash(X, Y, Z + 1u), fx, fy, fz - 1.0),
                          noise_grad(hash(X + 1u, Y, Z + 1u), fx - 1.0, fy, fz - 1.0));

  noise_u[1] = noise_lerp(u,
                          noise_grad(hash(X, Y + 1u, Z + 1u), fx, fy - 1.0, fz - 1.0),
                          noise_grad(hash(X + 1u, Y + 1u, Z + 1u), fx - 1.0, fy - 1.0, fz - 1.0));

  noise_v[1] = noise_lerp(v, noise_u[0], noise_u[1]);

  float r = noise_scale3(noise_lerp(w, noise_v[0], noise_v[1]));

  return (isinf(r)) ? 0.0 : r;
}

float turbulent_noise(float3 coord, float scale, float detail) {
  float3 p = coord * scale;
  float amplitude = 1.0;
  float sum = 0.0;
  detail = clamp(detail, 0.0, 16.0);
  int n = int(detail);
  scale = 1.0;
  for (int i = 0; i <= n; i++) {
      float3 scaledP = scale * p;
      float t =  0.5 * noise_perlin(scaledP.x, scaledP.y, scaledP.z) + 0.5;
      sum += t * amplitude;
      amplitude *= 0.5;
      scale *= 2.0;
    }
    float rmd = detail - floor(detail);
    if (rmd != 0.0) {
      float3 scaledP = scale * p;
      float t = 0.5 * noise_perlin(scaledP.x, scaledP.y, scaledP.z) + 0.5;
      float sum2 = sum + t * amplitude;
      sum *= (float(1 << n) / float((1 << (n + 1)) - 1));
      sum2 *= (float(1 << (n + 1)) / float((1 << (n + 2)) - 1));
      return (1.0 - rmd) * sum + rmd * sum2;
    }
    else {
      sum *= (float(1 << n) / float((1 << (n + 1)) - 1));
      return sum;
    }

}

void mixUints(out uint a, out uint b, out uint c)
  {
    a -= c;
    a ^= rot(c, 4u);
    c += b;
    b -= a;
    b ^= rot(a, 6u);
    a += c;
    c -= b;
    c ^= rot(b, 8u);
    b += a;
    a -= c;
    a ^= rot(c, 16u);
    c += b;
    b -= a;
    b ^= rot(a, 19u);
    a += c;
    c -= b;
    c ^= rot(b, 4u);
    b += a;
  }
uint hashUint4(uint kx, uint ky, uint kz, uint kw)
{
  uint a, b, c;
  a = b = c = 0xdeadbeefu + (4u << 2u) + 13u;

  a += kx;
  b += ky;
  c += kz;
  mixUints(a, b, c);

  a += kw;
  c ^= b;
  c -= rot(b, 14u);
  a ^= c;
  a -= rot(c, 11u);
  b ^= a;
  b -= rot(a, 25u);
  c ^= b;
  c -= rot(b, 16u);
  a ^= c;
  a -= rot(c, 4u);
  b ^= a;
  b -= rot(a, 14u);
  c ^= b;
  c -= rot(b, 24u);

  return c;
}
uint hashUint3(uint kx, uint ky, uint kz)
{
  uint a, b, c;
  a = b = c = 0xdeadbeefu + (3u << 2u) + 13u;

  c += kz;
  b += ky;
  a += kx;
  c ^= b;
  c -= rot(b, 14u);
  a ^= c;
  a -= rot(c, 11u);
  b ^= a;
  b -= rot(a, 25u);
  c ^= b;
  c -= rot(b, 16u);
  a ^= c;
  a -= rot(c, 4u);
  b ^= a;
  b -= rot(a, 14u);
  c ^= b;
  c -= rot(b, 24u);

  return c;
}
float hashUint3ToFloat(uint kx, uint ky, uint kz)
{
  return float(hashUint3(kx, ky, kz)) / float(0xFFFFFFFFu);
}
float hashUint4ToFloat(uint kx, uint ky, uint kz, uint kw)
{
  return float(hashUint4(kx, ky, kz, kw)) / float(0xFFFFFFFFu);
}
float hashVec4ToFloat(float4 k)
{
  return hashUint4ToFloat(
      uint(k.x), uint(k.y), uint(k.z), uint(k.w));
}
float hashVec3ToFloat(float3 k)
{
  return hashUint3ToFloat(uint(k.x), uint(k.y), uint(k.z));
}
float3 hashVec3ToVec3(float3 k)
{
  return float3(
      hashVec3ToFloat(k), hashVec4ToFloat(float4(k, 1.0)), hashVec4ToFloat(float4(k, 2.0)));
}

// F1 - the distance to the closest feature point
float voronoiF1 (float3 coord, float randomness, float scale) {

  randomness = clamp(randomness, 0.0, 1.0);

  float3 scaledCoord = coord * scale;
  float3 cellPosition = floor(scaledCoord);
  float3 localPosition = scaledCoord - cellPosition;

  float minDistance = 8.0;
  float3 targetOffset, targetPosition;
  for (int k = -1; k <= 1; k++) {
    for (int j = -1; j <= 1; j++) {
      for (int i = -1; i <= 1; i++) {
        float3 cellOffset = float3(i, j, k);
        float3 pointPosition = cellOffset +
                             hashVec3ToVec3(cellPosition + cellOffset) * randomness;
        float distanceToPoint = length(pointPosition - localPosition);
        if (distanceToPoint < minDistance) {
          targetOffset = cellOffset;
          minDistance = distanceToPoint;
          targetPosition = pointPosition;
        }
      }
    }
  }
  return minDistance;

}

float voronoi(float3 coord, float scale, float randomness) {
  randomness = clamp(randomness, 0.0, 1.0);
  float3 scaledCoord = coord * scale;
  float3 cellPosition = floor(scaledCoord);
  float3 localPosition = scaledCoord - cellPosition;
  float3 vectorToClosest;
    float minDistance = 8.0;
    int k;
    for (k = -1; k <= 1; k++) {
      for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
          float3 cellOffset = float3(i, j, k);
          float3 vectorToPoint = cellOffset +
                              hashVec3ToVec3(cellPosition + cellOffset) * randomness -
                              localPosition;
          float distanceToPoint = dot(vectorToPoint, vectorToPoint);
          if (distanceToPoint < minDistance) {
            minDistance = distanceToPoint;
            vectorToClosest = vectorToPoint;
          }
        }
      }
    }

    minDistance = 8.0;
    for (k = -1; k <= 1; k++) {
      for (int j = -1; j <= 1; j++) {
        for (int i = -1; i <= 1; i++) {
          float3 cellOffset = float3(i, j, k);
          float3 vectorToPoint = cellOffset +
                              hashVec3ToVec3(cellPosition + cellOffset) * randomness -
                              localPosition;
          float3 perpendicularToEdge = vectorToPoint - vectorToClosest;
          if (dot(perpendicularToEdge, perpendicularToEdge) > 0.0001) {
            float distanceToEdge = dot((vectorToClosest + vectorToPoint) / 2.0,
                                      normalize(perpendicularToEdge));
            minDistance = min(minDistance, distanceToEdge);
          }
        }
      }
    }
    return minDistance;
}

// eof noise.frag.glsl
#if !defined(M_PI)
static const float M_PI = 3.141592653589793;
#endif
static const float M_PI_2 = 2.0 * M_PI;
static const float INV_M_PI = 1.0 / M_PI;
static const float c_MinReflectance = 0.04;
static const float EPSILON = 0.001;
static const float PI_OVER_180 = M_PI/180.0;

static const float3 SubSurfaceScatterValue = float3(1.0,.3,.2);

float3 sampleTexCube(samplerCUBE cube, float3 normal) {

  return texCUBE(cube, normal).rgb;

}

float3 sampleTexCube(samplerCUBE cube, float3 normal, float mip) {

  float4 normalWithLOD = float4(normal, mip);
  return texCUBE(cube, normalWithLOD).rgb;

}

float computeNdotV(float3 normal, float3 world_view_dir){
    return saturate(-dot(normal, world_view_dir));
}

float3 computeViewDir(float3 camera, float3 position) {
  return normalize(position - camera);
}

float3 reflection(float3 normal, float3 world_view_dir) {
  return world_view_dir - 2.0 * normal * dot(world_view_dir, normal);
}

float deg2rad(float angle) {
  return angle * PI_OVER_180;
}

float3 ConvertOutputColorSpaceFromSRGB(float3 srgbInput);
float3 ConvertOutputColorSpaceFromLinear(float3 linearInput);

float inverselerp(float a, float b, float v) {
  return (v-a)/(b-a);
}

float  getBias(float time,float bias)
{
   return (time / ((((0.0/bias) - 2.0)*(1.0 - time))+1.0));
}

float getGain(float time,float gain)
{
  if(time < -1.5)
    return getBias(time * 1.0, gain)/2.0;
  else
    return getBias(time * 1.0 - 1.0, 1.0 - gain) / 2.0 + 0.5;
}

// eof utils.frag.glsl
float3 avatar_tonemap(float3 color) {
    return color;
}
// eof tonemap_passtrough.frag.glsl
// sub_surface_scatter.frag.glsl
// hemisphere_normal_offsets.frag.glsl

struct avatar_HemisphereNormalOffsets {
    float3 nn;
    float3 bb;
    float3 tt;
    float3 lv1;
    float3 lv2;
    float3 lv3;
};

avatar_HemisphereNormalOffsets avatar_computeHemisphereNormalOffsets(avatar_FragmentInput i) {
    avatar_HemisphereNormalOffsets hno;

    float3 worldNormal = i.geometry.tangentSpace.normal;
    float3 worldTangent = i.geometry.tangentSpace.tangent;
    float3 worldBitangent = i.geometry.tangentSpace.bitangent;
    hno.nn = mul(i.matrices.environmentRotation, (worldNormal * 0.7071));
    hno.tt = mul(i.matrices.environmentRotation, (worldTangent * (0.7071 * .5)));
    hno.bb = mul(i.matrices.environmentRotation, (worldBitangent * (0.7071 * 0.866)));
    hno.lv1 = (hno.nn + hno.tt * 2.);
    hno.lv2 = (hno.nn + hno.bb - hno.tt);
    hno.lv3 = (hno.nn - hno.bb - hno.tt);
    return hno;
}
// eof hemisphere_normal_offsets.frag.glsl
  float3 avatar_computeSubsurfaceContribution(avatar_FragmentInput i) {
      avatar_HemisphereNormalOffsets hemisphereNormalOffsets = avatar_computeHemisphereNormalOffsets(i);
      float3 subsurfaceColor = float3(0.0, 0.0, 0.0);
      float3 ibl_diffuse1 = saturate(avatar_computeAmbientDiffuseLighting(i, hemisphereNormalOffsets.lv1));
      float3 ibl_diffuse2 = saturate(avatar_computeAmbientDiffuseLighting(i, hemisphereNormalOffsets.lv2));
      float3 ibl_diffuse3 = saturate(avatar_computeAmbientDiffuseLighting(i, hemisphereNormalOffsets.lv3));
      float3 ibl_diffuseN = saturate(avatar_computeAmbientDiffuseLighting(i, hemisphereNormalOffsets.nn));
      float3 diffuse = float3(0.0, 0.0, 0.0);
      float3 accumulatedDiffuseColor = float3(0.0, 0.0, 0.0);

      float3 lightColor;
      float3 directionalLightColor;

#if MAX_LIGHT_COUNT > 1
      for(int lightIdx = 0; lightIdx < MAX_LIGHT_COUNT; lightIdx++) {
#else
      int lightIdx = 0;
#endif
          if(lightIdx < i.lightsCount) {
            float3 worldSpaceLightDir = -i.lights[lightIdx].direction;
            lightColor = float3(i.lights[lightIdx].color * i.lights[lightIdx].intensity * (1./M_PI));
            directionalLightColor = lightColor * (1.0/i.material.exposure);
            float directionalSoftDiffuseValue = saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv1)) +
              saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv2)) +
              saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv3));
            diffuse += float3(directionalSoftDiffuseValue.xxx) * directionalLightColor;
            accumulatedDiffuseColor += directionalLightColor * saturate(dot(worldSpaceLightDir,hemisphereNormalOffsets.nn));
          }
#if MAX_LIGHT_COUNT > 1
      }
#endif

      float3 softDiffuseLight = (ibl_diffuse1 + ibl_diffuse2 + ibl_diffuse3 + diffuse) * (1. / 3.);
      float thinness = 1.0 - i.material.thickness + 0.00001;
      float3 baseColor = i.material.base_color;

      subsurfaceColor = (softDiffuseLight * (SubSurfaceScatterValue) * saturate(i.material.occlusion + i.skinLookParams.increaseOcclusion) +
                        (accumulatedDiffuseColor + ibl_diffuseN) * (1. - SubSurfaceScatterValue) * i.material.occlusion);

      return subsurfaceColor;
  }

void avatar_addSubsurfaceContribution(avatar_FragmentInput i, avatar_AmbientLighting ambient, float3 diffuseWrap, inout avatar_FragmentOutput o) {
      if(i.options.enableSkin || i.options.enableAnyHair) {

        o.p_diffuse = diffuseWrap * SubSurfaceScatterValue + o.p_diffuse * (1. - SubSurfaceScatterValue);

        o.subSurfaceColor = avatar_computeSubsurfaceContribution(i);
        o.subSurfaceColor -= o.p_diffuse * i.material.occlusion;
        o.subSurfaceColor = saturate(o.subSurfaceColor);
      }
  }
// EOF sub_surface_scatter.frag.glsl
bool avatar_eyeGlintCondition(avatar_FragmentInput i, avatar_Light light) {
    return i.options.enableEyeGlint;
}

float3 avatar_getGlintSpecularPerCamera(avatar_FragmentInput i) {

    avatar_Light glintLight = i.lights[0];
    glintLight.direction = i.geometry.worldViewDir;
    float3 glintSpecular = avatar_computeSpecular(i.geometry.tangentSpace.normal, i.geometry.worldViewDir, glintLight, i.lightLookParams, 0.025f * i.material.eye_glint_factor, i.material.exposure, i.options);
    return glintSpecular;
}

float3 avatar_recalculateSpecularForEyeGlint(float3 halfVector, float3 specular, avatar_Light light, avatar_FragmentInput i) {
    return specular;
}

float3 avatar_getGlintSpecularPerLight(avatar_Light light, avatar_FragmentInput i) {

    float3 glintSpecular = avatar_computeSpecular(i.geometry.tangentSpace.normal, i.geometry.worldViewDir, light, i.lightLookParams, 0.025f * i.material.eye_glint_factor, i.material.exposure, i.options);
    return glintSpecular;
}

float3 avatar_getGlintSpecularPerAmbient(avatar_FragmentInput i) {

    return float3(0,0,0);
}

bool avatar_eyeGlintEnableSpecularFactor(avatar_FragmentInput i) {
    return true;
}

bool avatar_eyeGlintAddAmbientSpecular(avatar_FragmentInput i) {
    return true;
}

bool avatar_eyeGlintConsiderLightMask(avatar_FragmentInput i) {
    return true;
}
float3 avatar_getEnvironmentSphereMap(avatar_FragmentInput i) {
    return float3(0,0,0);
}
float3 avatar_addRimLight(avatar_Geometry geometry, avatar_Material material){
    float3 up = float3(0.0, 1.0, 0.0);
    float3 right = float3(1.0, 0.0, 0.0);
    float NdotUp = dot(geometry.normal, up);
    float NdotRight = dot(geometry.normal, right);

    float angle = atan2((NdotRight), (NdotUp));
    float gradient = angle / M_PI_2 + 0.5;
    float multiplier = 1.0;

    if(gradient < material.rim_light_material.start) {
        multiplier = 0.0;
    }
    if(gradient >= material.rim_light_material.start && gradient <= material.rim_light_material.end) {
        multiplier = smoothstep(material.rim_light_material.start, material.rim_light_material.start + material.rim_light_material.transition, gradient);
        multiplier *= smoothstep(material.rim_light_material.end, material.rim_light_material.end - material.rim_light_material.transition, gradient);
    }
    if(gradient > material.rim_light_material.end) {
        multiplier = 0.0;
    }
    if(material.rim_light_material.bias == 0.0) {
      material.rim_light_material.bias = EPSILON;
    }
    float fresnel = pow(clamp(1. - computeNdotV(geometry.normal, geometry.worldViewDir), 0., 1.), 1.0/(material.rim_light_material.bias));
    fresnel *= multiplier;
    fresnel *= material.hair_material.ao_intensity;

    return material.rim_light_material.color * material.rim_light_material.intensity * fresnel;
  }
float3 avatar_computeDiffuse(avatar_Geometry geometry, avatar_Light light, avatar_Material material, out float3 diffuseWrap) {
    float3 directional_light_color = light.color * (1.0/material.exposure) * INV_M_PI * light.intensity;
    float3 point_to_light = -light.direction;
    float3 l = normalize(point_to_light);

    float NdotL = dot(geometry.normal,l);
    float3 p_diffuse = saturate(smoothstep(material.diffuseWrapAngle, 1.0, NdotL)) * directional_light_color;
    diffuseWrap = saturate(smoothstep(material.SSSWrapAngle, 1.0, NdotL)) * directional_light_color;

    return p_diffuse;
}
float3 avatar_computeSpecular(float3 normal, float3 worldViewDir, avatar_Light light, avatar_LightLookParams lightLookParams,
float roughness, float exposure, avatar_FragOptions options) {
    float3 directional_light_color = (light.color) * (1.0/exposure) * INV_M_PI *  light.intensity;
    float NdotV = computeNdotV(normal, worldViewDir);
    float roughPow2 = roughness * roughness;
    float roughPow4 = roughPow2 * roughPow2;
    float invRoughPow4 = 1. - roughPow4;
    float3 worldSpaceLightDir = normalize(-light.direction) ;
    float3 h = normalize(worldSpaceLightDir - worldViewDir);

    float expandFactor = 0.0;

    float NdotL = saturate(dot(normal, worldSpaceLightDir));
    float NdotH = saturate(dot(normal, h));

    if(options.enableSkin || options.enableAnyHair) {
        NdotL = smoothstep(0.0,1.0,NdotL+expandFactor);
        NdotH = smoothstep(0.0,1.0,NdotH+expandFactor);
    }
    float ggx = NdotL * sqrt(NdotV * NdotV * invRoughPow4 + roughPow2) +
            NdotV * sqrt(NdotL * NdotL * invRoughPow4 + roughPow2);
    ggx = ggx > 0. ? .5 / ggx : 0.;

    float t = 1./(1. - NdotH * NdotH * invRoughPow4);
    float punctualSpec = NdotL * t * t * roughPow4 * ggx;
    float3 p_specular = punctualSpec * directional_light_color;
    return p_specular * lightLookParams.singleSampleBrightnessMultiplier;
}
  float3 avatar_computeAmbientDiffuseLighting(avatar_FragmentInput i, float3 normal) {
    float3 diffuse = float3(0.0, 0.0, 0.0);
    float3 specular_contribution = float3(0.0, 0.0, 0.0);

    OvrGetUnityDiffuseGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.positionInWorldSpace, -i.geometry.worldViewDir,
      1.0, i.ambient_color, 1.0 - i.material.roughness, i.material.metallic,
      i.material.occlusion, i.material.base_color, normal, specular_contribution, diffuse);
    return diffuse;
  }

  float3 avatar_computeAmbientDiffuseLighting(avatar_FragmentInput i) {
    return avatar_computeAmbientDiffuseLighting(i, i.geometry.normal);
  }

  float3 avatar_computeAmbientSpecularLighting(avatar_FragmentInput i, float3 specular_contribution) {
    float3 diffuse = float3(0.0, 0.0, 0.0);
    float3 specular = float3(0.0, 0.0, 0.0);

    float roughness = i.material.roughness;
    float metallic = i.material.metallic;
    float3 normal = i.geometry.normal;
    if (i.options.enableEyeGlint) {
        roughness = 0.0;
        metallic = 0.0;
        normal = i.geometry.tangentSpace.normal;
    }

    OvrGetUnityGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.positionInWorldSpace, -i.geometry.worldViewDir,
      1.0, i.ambient_color, 1.0 - roughness, metallic,
      i.material.occlusion, i.material.base_color, normal, specular_contribution, diffuse, specular);
    return specular;
  }

  avatar_AmbientLighting avatar_computeAmbientLighting(avatar_FragmentInput i, float3 specular_contribution) {
    avatar_AmbientLighting ambient;
    ambient.diffuse = avatar_computeAmbientDiffuseLighting(i);
    ambient.specular = avatar_computeAmbientSpecularLighting(i, specular_contribution);
    return ambient;
  }
// hair.frag.glsl

float3 avatar_computeSpecularHighlight(float anisotropicIntensity, float roughness, float3 L, float3 E, float3 N, float3 tangent, float3 bitangent, float offset) {
    float3 H = float3(0.0, 0.0, 0.0);
    float3 R = float3(0.0, 0.0, 0.0);
    float kspec = 0.0;
    float kexp = 0.0;
    float3 spec = float3(0.0, 0.0, 0.0);

    N = normalize(N +  bitangent * offset);

    if (anisotropicIntensity > 0.0)
    {
      float3 isotropicH = normalize(E + L);
      H = normalize(E + L - dot(E + L, tangent) * tangent);
      H = normalize((lerp((isotropicH), (H), (anisotropicIntensity))));

      kspec = dot(H, N);
      kexp = 1.0 / (roughness * roughness + EPSILON);
    }
    else
    {
      H = normalize(E + L);

      kspec = dot( N, H );
      kexp = 3.0 / (roughness * roughness + EPSILON);
    }

    if (kspec > -EPSILON)
    {
      float specular = pow(max(0.0, kspec), kexp);
      spec += float3(specular, specular, specular);
    }

    return spec;
  }

  float3 avatar_getHairTangent(avatar_FragmentInput i) {
    avatar_HairMaterial hair_mat = i.material.hair_material;
    float2 flow = float2(cos(hair_mat.flow_angle), sin(hair_mat.flow_angle));

    float3 hairTangent = i.geometry.tangentSpace.tangent.xyz * flow.x + i.geometry.tangentSpace.bitangent * flow.y;

    return normalize(hairTangent);
  }

  float3 avatar_computeHairSpecular(avatar_FragmentInput i, float3 lightVector, float3x3 hairCoordinateSystem, float anisotropicBlend ) {
    avatar_HairMaterial hair_mat = i.material.hair_material;

    float3 normal = hairCoordinateSystem[0];
    float3 hairTangent = hairCoordinateSystem[1];
    float3 bitangent = hairCoordinateSystem[2];

    float3 E = -i.geometry.worldViewDir;

    float3 L = normalize(-lightVector);
    float anisotropy = (lerp((0.0), (hair_mat.anisotropic_intensity), (anisotropicBlend)));
    float localOffset = hair_mat.specular_shift_intensity * (hair_mat.shift - .5) * anisotropicBlend;

    float3 spec1 = avatar_computeSpecularHighlight(anisotropy, hair_mat.specular_white_roughness, L, E, normal,
                    bitangent, hairTangent, localOffset) *  hair_mat.specular_white_intensity;

    float3 spec2 = avatar_computeSpecularHighlight(anisotropy,  hair_mat.specular_color_roughness, L, E, normal,
                    bitangent, hairTangent, hair_mat.specular_color_offset + localOffset)
                    * hair_mat.specular_color_factor * hair_mat.specular_color_intensity;
    return (spec1 + spec2) * hair_mat.ao_intensity;
  }

  float3 avatar_blendPunctualSpecularWithHair(float3 punctualSpec, float3 hairPunctualSpec, float hairBlend) {
    return (lerp((punctualSpec), (hairPunctualSpec), (hairBlend)));
  }

  float3 avatar_blendSubSurfaceColorWithHair(float3 hairSubsurfaceColor, float3 skinSubsurfaceColor, float hairBlend) {
    return (lerp((hairSubsurfaceColor), (skinSubsurfaceColor), (hairBlend)));
  }

void avatar_addHairLightContribution(avatar_FragmentInput i, avatar_AmbientLighting ambient, inout avatar_FragmentOutput o) {
  if (i.options.enableAnyHair) {
      float hairBlend = i.material.hair_material.blend;
      float3 hairTangent = avatar_getHairTangent(i);
      float3 hairNormal = (lerp((i.geometry.tangentSpace.normal), (i.geometry.normal), (i.material.hair_material.normal_intensity)));
      float3x3 hairCoordinateSystem = float3x3(hairNormal, hairTangent, normalize(cross(hairNormal, hairTangent)));

      float3 totalhairPunctualSpec = float3(0.0, 0.0, 0.0);

#if MAX_LIGHT_COUNT > 1
    for (int lightIdx = 0; lightIdx < MAX_LIGHT_COUNT; lightIdx++) {
#else
      int lightIdx = 0;
#endif
        if (lightIdx < i.lightsCount) {
          avatar_Light light = i.lights[lightIdx];
          if (light.eyeGlint == 0) {
            float3 directional_light_color = light.color * (1.0/i.material.exposure) * INV_M_PI * light.intensity;
            totalhairPunctualSpec += (avatar_computeHairSpecular(i, light.direction, hairCoordinateSystem, i.material.hair_material.aniso_blend) * directional_light_color * INV_M_PI);
          }
        }

#if MAX_LIGHT_COUNT > 1
    }
#endif

      o.p_specular = avatar_blendPunctualSpecularWithHair(o.p_specular, totalhairPunctualSpec, hairBlend);

      o.subSurfaceColor = avatar_blendSubSurfaceColorWithHair(i.material.hair_material.subsurface_color, SubSurfaceScatterValue, hairBlend);
      o.a_specular *= 1.0-hairBlend;
      o.p_diffuse *= (lerp((1.0), (i.material.hair_material.diffused_intensity), (hairBlend)));
    }
  }

// EOF hair.frag.glsl
// End features_unity.frag.glsl
// Start of include: pbr-combined.frag.glsl
// zero_structs.frag.glsl

avatar_Geometry avatar_zeroGeometry() {
  avatar_Geometry geometry;

  geometry.camera = float3(0.0, 0.0, 0.0);
  geometry.positionInWorldSpace = float3(0.0, 0.0, 0.0);
  geometry.positionInClipSpace = float3(0.0, 0.0, 0.0);
  // MOD START Ultimate-GloveBall: add screenPos support
  geometry.screenPos = float4(0.0, 0.0, 0.0, 0.0);
  // MOD END Ultimate-GloveBall

  geometry.normal = float3(0.0, 0.0, 0.0);
  geometry.normalMap = float3(0.0, 0.0, 0.0);

  geometry.tangentSpace.normal = float3(0.0, 0.0, 0.0);;
  geometry.tangentSpace.tangent = float3(0.0, 0.0, 0.0);;
  geometry.tangentSpace.bitangent = float3(0.0, 0.0, 0.0);

  geometry.texcoord_0 = float2(0.0, 0.0);
  geometry.texcoord_1 = float2(0.0, 0.0);
  geometry.color = float4(0.0, 0.0, 0.0, 0.0);
  geometry.ormt = float4(0.0, 0.0, 0.0, 0.0);
  geometry.normalScale = 0.0;
  geometry.invertNormalSpaceEyes = false;

  geometry.worldViewDir = 0.0f.xxx;

  geometry.lod = 0.0f;

  return geometry;
}

avatar_SkinMaterial avatar_zeroSkinMaterial() {
  avatar_SkinMaterial material;
  material.subsurface_color = float3(0.0, 0.0, 0.0);
  material.skin_ORM_factor = float3(0.0, 0.0, 0.0);
  return material;
}

avatar_HairMaterial avatar_zeroHairMaterial() {
  avatar_HairMaterial material;
  material.subsurface_color = float3(0.0, 0.0, 0.0);
  material.scatter_intensity = 0.0;
  material.specular_color_factor = float3(0.0, 0.0, 0.0);
  material.specular_shift_intensity = 0.0;
  material.specular_white_intensity = 0.0;
  material.specular_white_roughness = 0.0;
  material.specular_color_intensity = 0.0;
  material.specular_color_offset = 0.0;
  material.specular_color_roughness = 0.0;
  material.anisotropic_intensity = 0.0;
  material.diffused_intensity = 0.0;
  material.normal_intensity = 0.0;
  material.specular_glint = 0.0;
  material.ao_intensity = 0.0;
  material.flow_sample = 0.0;
  material.flow_angle = 0.0;
  material.shift = 0.0;
  material.blend = 0.0;
  material.aniso_blend = 0.0;
  return material;
}

avatar_RimLightMaterial avatar_zeroRimLightMaterial() {
    avatar_RimLightMaterial material;
    material.intensity = 0.0f;
    material.bias = 0.0f;
    material.color = 0.0f.xxx;
    material.transition = 0.0f;
    material.start = 0.0f;
    material.end = 0.0f;
    return material;
}

avatar_Material avatar_zeroMaterial() {
  avatar_Material material;
  material.base_color = float3(0.0, 0.0, 0.0);
  material.alpha = 0.0;
  material.exposure = 0.0;
  material.metallic = 0.0;
  material.occlusion = 0.0;
  material.roughness = 0.0;
  material.thickness = 0.0;

  material.ambient_diffuse_factor = 1.0;
  material.ambient_specular_factor = 1.0;

  material.eye_glint_factor = 0.0;
  material.eye_glint_color_factor = 0.0f;

  material.skin_material = avatar_zeroSkinMaterial();
  material.hair_material = avatar_zeroHairMaterial();
  material.rim_light_material = avatar_zeroRimLightMaterial();
  material.ramp_selector = 0.0;
  material.color_selector_lod = 0;
  material.SSSWrapAngle = 0.0;
  material.diffuseWrapAngle = 0.0;

  material.f0 = 0.0;
  material.f90 = 0.0;

  return material;
}

avatar_Light avatar_zeroLight() {
  avatar_Light light;
  light.direction = float3(0.0, 0.0, 0.0);
  light.color = float3(0.0, 0.0, 0.0);
  light.intensity = 0.0;
  light.shadowTerm = 1.0f;
  light.type = 0;
  light.eyeGlint = 0;
  return light;
}

avatar_FragOptions avatar_zeroOptions() {
  avatar_FragOptions options;
  options.useIBLRotation = false;

  options.enableNormalMapping = false;
  options.enableAlphaToCoverage = false;
  options.enableRimLight = false;

  options.enableSkin = false;
  options.enableEyeGlint = false;
  options.enableHeadHair = false;
  options.enableFacialHair = false;
  options.enableAnyHair = false;
  options.enableShadows = false;
  options.enableLightMask = false;
#ifdef DEBUG_MODE_ON
  options.hasBaseColorMap = false;
  options.hasMetallicRoughnessMap = false;
  options.enablePreviewColorRamp = false;
#endif

  return options;
}

avatar_LightLookParams avatar_zeroLightLookParams() {
  avatar_LightLookParams lightLookParams;

     lightLookParams.lightSampleCount = 0;
     lightLookParams.lightSize = 0.0;
     lightLookParams.roughnessCutOff = 0.0;
     lightLookParams.lowerBound = 0.0;
     lightLookParams.reduceRoughnessByValue = 0.0;
     lightLookParams.reducedLightSizeMultiplier = 0.0;
     lightLookParams.multiSampleBrightnessMultiplier = 0.0;
     lightLookParams.singleSampleBrightnessMultiplier = 0.0;
     return lightLookParams;
}

avatar_SkinLookParams avatar_zeroSkinLookParams() {
  avatar_SkinLookParams skinLookParams;
  skinLookParams.SSSIBLMultiplier = 0.0;
  skinLookParams.increaseOcclusion =  0.0;
  return skinLookParams;
}

avatar_Matrices avatar_zeroMatrices() {
  avatar_Matrices matrices;
  matrices.objectToWorld = float4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
  matrices.worldToObject = float3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);
  matrices.viewProjection = float4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
  matrices.environmentRotation = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
  return matrices;
}

float4 avatar_zeroSampler2d(float2 uv) {
    return float4(0.0, 0.0, 0.0,0.0);
  };

float4 avatar_zeroSampler3d(float3 v) {
    return float4(0.0, 0.0, 0.0,0.0);
  };

float4 avatar_zeroSampler3dLod(float3 v, float lod) {
    return float4(0.0, 0.0, 0.0,0.0);
  };

avatar_FragmentInput avatar_zeroFragmentInput(int maxLightCount) {
  avatar_FragmentInput i;

  i.options = avatar_zeroOptions();

  i.matrices = avatar_zeroMatrices();

  i.geometry = avatar_zeroGeometry();
  i.material = avatar_zeroMaterial();
  i.lightLookParams = avatar_zeroLightLookParams();
  i.skinLookParams = avatar_zeroSkinLookParams();

  i.lightsCount = 1;
  #if MAX_LIGHT_COUNT > 1
  for(int idx = 0; idx < maxLightCount; idx++) {
     i.lights[idx] = avatar_zeroLight();
  }
  #else
    i.lights[0] = avatar_zeroLight();
  #endif

  i.lightMask = 0;
  i.eyeGlintExponential = 0.0;
  i.eyeGlintIntensity = 0.0;

  i.ambient_color = float3(0.0, 0.0, 0.0);
  i.ambient_occlusion = 1.0;
#ifdef DEBUG_MODE_ON
  i.debugMode = 0;
#endif
  return i;
}

avatar_FragmentOutput avatar_zeroFragmentOutput() {
  avatar_FragmentOutput o;

  o.color = float4(0.0, 0.0, 0.0,0.0);
  o.p_specular = float3(0.0, 0.0, 0.0);
  o.p_diffuse = float3(0.0, 0.0, 0.0);
  o.a_specular = float3(0.0, 0.0, 0.0);
  o.a_diffuse = float3(0.0, 0.0, 0.0);
  o.subSurfaceColor = float3(0.0, 0.0, 0.0);
  o.alphaCoverage = 255u;
  return o;
}
// eof zero_structs.frag.glsl
avatar_FragOptions getFragOptions(float materialTypeChannel);
avatar_FragmentOutput avatar_computeFragment(avatar_FragmentInput i);

void updateRimLightMaterial(inout avatar_FragmentInput i) {
  i.material.rim_light_material.intensity = u_RimLightIntensity;
  i.material.rim_light_material.bias = u_RimLightBias;
  i.material.rim_light_material.color = u_RimLightColor;
  i.material.rim_light_material.transition = u_RimLightTransition;
  i.material.rim_light_material.start = u_RimLightStartPosition;
  i.material.rim_light_material.end = u_RimLightEndPosition;
}

void setShaderParams(inout avatar_FragmentInput i){
  /* **** multi-sample specular values **** */
  i.lightLookParams.lightSampleCount = lightSampleCount;
  i.lightLookParams.lightSize = lightSize;
  i.lightLookParams.roughnessCutOff = roughnessCutOff;
  i.lightLookParams.lowerBound = lowerBound;
  i.lightLookParams.reduceRoughnessByValue = reduceRoughnessByValue;
  i.lightLookParams.reducedLightSizeMultiplier = reducedLightSizeMultiplier;
  i.lightLookParams.multiSampleBrightnessMultiplier = multiSampleBrightnessMultiplier;
  /* **** single-sample specular values **** */
  i.lightLookParams.singleSampleBrightnessMultiplier = singleSampleBrightnessMultiplier;
  /* **** SSS values **** */
  i.skinLookParams.SSSIBLMultiplier = SSSIBLMultiplier;
  i.skinLookParams.increaseOcclusion =  increaseOcclusion;
  /* **** wrap lighting diffuse values **** */
  i.material.SSSWrapAngle = SSSWrapAngle;
  i.material.diffuseWrapAngle = diffuseWrapAngle;
  /* **** fresnel values **** */
  i.material.f0 = f0;
  i.material.f90 = f90;
}

void updateHairMaterial(inout avatar_FragmentInput i, float4 ormtSample) {
  i.material.hair_material.subsurface_color = u_HairSubsurfaceColor;
  i.material.hair_material.scatter_intensity = u_HairScatterIntensity;
  i.material.hair_material.specular_color_factor = u_HairSpecularColorFactor;
  i.material.hair_material.specular_shift_intensity = u_HairSpecularShiftIntensity;
  i.material.hair_material.specular_white_intensity = u_HairSpecularWhiteIntensity;
  i.material.hair_material.specular_white_roughness = u_HairRoughness;
  i.material.hair_material.specular_color_intensity = u_HairSpecularColorIntensity;
  i.material.hair_material.specular_color_offset = u_HairSpecularColorOffset;
  i.material.hair_material.specular_color_roughness = u_HairColorRoughness;
  i.material.hair_material.anisotropic_intensity = u_HairAnisotropicIntensity;
  i.material.hair_material.diffused_intensity = u_HairDiffusedIntensity;
  i.material.hair_material.normal_intensity = u_HairSpecularNormalIntensity;
  i.material.hair_material.specular_glint = u_HairSpecularGlint;

  if (i.options.enableFacialHair) {
    i.material.hair_material.subsurface_color = u_FacialHairSubsurfaceColor;
    i.material.hair_material.scatter_intensity = u_FacialHairScatterIntensity;
    i.material.hair_material.specular_color_factor = u_FacialHairSpecularColorFactor;
    i.material.hair_material.specular_shift_intensity = u_FacialHairSpecularShiftIntensity;
    i.material.hair_material.specular_white_intensity = u_FacialHairSpecularWhiteIntensity;
    i.material.hair_material.specular_white_roughness = u_FacialHairRoughness;
    i.material.hair_material.specular_color_intensity = u_FacialHairSpecularColorIntensity;
    i.material.hair_material.specular_color_offset = u_FacialHairSpecularColorOffset;
    i.material.hair_material.specular_color_roughness = u_FacialHairColorRoughness;
    i.material.hair_material.anisotropic_intensity = u_FacialHairAnisotropicIntensity;
    i.material.hair_material.diffused_intensity = u_FacialHairDiffusedIntensity;
    i.material.hair_material.normal_intensity = u_FacialHairSpecularNormalIntensity;
    i.material.hair_material.specular_glint = u_FacialHairSpecularGlint;
  }

  i.material.hair_material.ao_intensity = ormtSample.r;

  i.material.hair_material.flow_sample = i.geometry.ormt.g;

  i.material.hair_material.flow_angle = ((1.0 - i.material.hair_material.flow_sample) - .25) * M_PI_2;

  i.material.hair_material.shift = ormtSample.b;
  i.material.hair_material.blend = clamp(ormtSample.a * 2.0, 0.0, 1.0);
  i.material.hair_material.aniso_blend = clamp((ormtSample.a - .5) * 2.0, 0.0, 1.0);

  if (i.options.enableAnyHair) {

    i.material.roughness = (162.0/256.0) * (162.0/256.0);
    i.material.metallic = 0.0;
    i.material.occlusion = 1.0;

    i.geometry.normal = (lerp((i.geometry.tangentSpace.normal), (i.geometry.normal), (i.material.hair_material.blend)));

    i.material.hair_material.subsurface_color = (lerp((SubSurfaceScatterValue), (i.material.hair_material.subsurface_color), (i.material.hair_material.blend)));

  }
}
// unity.frag.glsl
// normal_perturbation.frag.glsl

float3 avatar_sampleNormalMap(avatar_FragmentInput i) {

    float2 sampledXY = 2.0 * tex2D(u_NormalSampler, i.geometry.texcoord_0).xy - 1.0;
    float3 normalMapSample = float3(sampledXY, sqrt(1.0 - dot(sampledXY, sampledXY)));
    if(i.geometry.invertNormalSpaceEyes && i.options.enableEyeGlint) {
        normalMapSample.y = -normalMapSample.y;
    }
    return normalMapSample;
}

float3 avatar_perturbNormal(avatar_FragmentInput i, float3 normalMapSample) {
    float3 scaledNormalSample = normalMapSample * float3(i.geometry.normalScale, i.geometry.normalScale, 1.0);
    return mul(normalize(scaledNormalSample), float3x3(i.geometry.tangentSpace.tangent.xyz, i.geometry.tangentSpace.bitangent, i.geometry.tangentSpace.normal));
}

// eof normal_perturbation.frag.glsl
// alpha_coverage.frag.glsl

// create MSAA sample mask from alpha value for to emulate transparency
// used by DithercoverageFromMaskMSAA4(), don't call this function
// for 4x MSAA, also works for more MSAA but at same quality, FFR friendly
// @param alpha 0..1
uint avatar_alphaToCoverage(float alpha) {
    // can be optimized futher
    uint Coverage = 0u; // 0x00;
    if (alpha > (0.0 / 4.0)) Coverage = 136u; // 0x88;
    if (alpha > (1.0 / 4.0)) Coverage = 153u; // 0x99;
    if (alpha > (2.0 / 4.0)) Coverage = 221u; // 0xDD;
    if (alpha > (3.0 / 4.0)) Coverage = 255u; // 0xFF;
    return Coverage;
}

// see https://developer.oculus.com/blog/tech-note-shader-snippets-for-efficient-2d-dithering
float avatar_dither17b(float2 svPosition, float frameIndexMod4) {
    float3 k0 = float3(2, 7, 23);
    float Ret = dot(float3(svPosition, frameIndexMod4), k0 / 17.0f);
    return frac((Ret));
}

// create MSAA sample mask from alpha value for to emulate transparency,
// for 4x MSAA,  can show artifacts with FFR
// @param alpha 0..1, outside range should behave the same as saturate(alpha)
// @param svPos xy from SV_Position
// @param true: with per pixel dither, false: few shades
uint avatar_coverageFromMaskMSAA4(float alpha, float2 svPos, bool dither) {
    // using a constant in the parameters means the dynamic branch (if) gets compiled out,
    if (dither) {
        // the pattern is not animated over time, no extra perf cost
        float frameIndexMod4 = 0.0;
        // /4: to have the dithering happening with less visual impact as 4
        //     shades are already implemented with MSAA subsample rejection.
        // -=: subtraction because the function coverageFromMaskMSAA4() shows visuals
        //     effect >0 and we want to have some pixels not have effect depending on dithering.
        alpha -= (avatar_dither17b(svPos, frameIndexMod4) / 4.0);
    }
    else {
        // no dithering, no FFR artifacts with Quest
        alpha -= (0.5f / 4.0);
    }
    return avatar_alphaToCoverage(alpha);
}

uint avatar_calculateAlphaCoverage(avatar_FragmentInput i, float3 positionInScreenSpace) {
    if (!i.options.enableAlphaToCoverage) {
    return 255u;
    }
    return avatar_coverageFromMaskMSAA4(i.material.alpha, positionInScreenSpace.xy, true);
}

// eof alpha_coverage.frag.glsl
// geometry.frag.glsl

  float3 avatar_calculateBitangent(avatar_TangentSpace tangentSpace) {
    return normalize(cross(tangentSpace.normal, tangentSpace.tangent));
  }

  void avatar_fillInputGeometry(
    inout avatar_FragmentInput i,
    float3 positionInWorldSpace,
    float3 positionInClipSpace,
    float3 normalInWorldSpace,
    float3 tangentInWorldSpace,
    float2 texcoord0,
    float2 texcoord1,
    float normalScale,
    bool invertNormalSpaceEyes) {
    i.geometry.positionInWorldSpace = positionInWorldSpace;
    i.geometry.positionInClipSpace = positionInClipSpace;
    i.geometry.texcoord_0 = texcoord0;
    i.geometry.texcoord_1 = texcoord1;
    i.geometry.normalScale = normalScale;
    i.geometry.invertNormalSpaceEyes = invertNormalSpaceEyes;
    i.geometry.tangentSpace.normal = normalInWorldSpace;
    i.geometry.tangentSpace.tangent = tangentInWorldSpace;
    i.geometry.tangentSpace.bitangent = avatar_calculateBitangent(i.geometry.tangentSpace);
    i.geometry.normal = i.geometry.tangentSpace.normal;

    if (i.options.enableNormalMapping) {
      float3 normalMapSample  = avatar_sampleNormalMap(i);
      i.geometry.normalMap = normalMapSample;
      i.geometry.normal = avatar_perturbNormal(i, normalMapSample);
    }
  }

// eof geometry.frag.glsl
// UNITY SPECIFIC START
// All these Unity specific functions are intended to be sanitized, then replaced with :

// Options flags:
// NOTE: These should naturally exist in uncombined rendering.
// In combined rendering they're filtered by the subMeshID/vertex color alpha.

// Global Options frag:

#ifndef DEC_bool_enableNormalMapping
 #define DEC_bool_enableNormalMapping
 static const bool enableNormalMapping = false;
 #endif
#ifndef DEC_bool_enableAlphaToCoverage
 #define DEC_bool_enableAlphaToCoverage
 static const bool enableAlphaToCoverage = false;
 #endif
#ifndef DEC_bool_enableRimLight
 #define DEC_bool_enableRimLight
 static const bool enableRimLight = false;
 #endif
#ifndef DEC_bool_useSubmesh
 #define DEC_bool_useSubmesh
 static const bool useSubmesh = false;
 #endif
#ifdef DEBUG_MODE_ON
#ifndef DEC_bool_enablePreviewColorRamp
 #define DEC_bool_enablePreviewColorRamp
 static const bool enablePreviewColorRamp = false;
 #endif
#ifndef DEC_bool_hasBaseColorMap
 #define DEC_bool_hasBaseColorMap
 static const bool hasBaseColorMap = true;
 #endif
#ifndef DEC_bool_hasMetallicRoughnessMap
 #define DEC_bool_hasMetallicRoughnessMap
 static const bool hasMetallicRoughnessMap = true;
 #endif
#endif

// Local Options (per Primitive/Material):
#ifndef DEC_bool_enableSkin
 #define DEC_bool_enableSkin
 static const bool enableSkin = false;
 #endif
#ifndef DEC_bool_enableEyeGlint
 #define DEC_bool_enableEyeGlint
 static const bool enableEyeGlint = false;
 #endif
#ifndef DEC_bool_enableHair
 #define DEC_bool_enableHair
 static const bool enableHair = false;
 #endif

// Output semantics

struct FragmentOutput
{
  float4 _output_FragColor : COLOR0; // Should be target0 if multiple outputs. Maybe use TARGET0 always.
};

// Unity Specific Functions
// If compiled in a non internal unity environment, define this stub functions

uniform int u_MipCount; // must be manually set by the IBL lighting system to the mip count of the diffuse Lambertian sampler cubemap

// UNITY SPECIFIC END

#ifdef DEBUG_MODE_ON
uniform int Debug;
#endif

uniform int u_lightCount;

void frag_Fragment_main() {
  /// INITILIZATION OF INPUT STRUCTURE ///

  avatar_FragmentInput i = avatar_zeroFragmentInput(MAX_LIGHT_COUNT);
  setShaderParams(i);

  i.options = getFragOptions(v_Color.a);
#ifdef DEBUG_MODE_ON
  i.debugMode = Debug;
#endif

  float3 lightColor = getLightColor();
  float3 lightPos = getLightPosition();
  float3 lightDirection = getLightDirection();
  int lightCount = getLightCount();

  i.lightsCount = lightCount;

  i.lights[0].intensity = 1.0;
  i.lights[0].direction = lightDirection;
  i.lights[0].color = lightColor;

  for(int idx = 1; idx < MAX_LIGHT_COUNT; idx++) {
    if(idx < lightCount) {
      OvrLight ovrLight = getAdditionalLight(idx, v_WorldPos);
      avatar_Light avatarLight;
      avatarLight.direction = -ovrLight.direction;
      avatarLight.intensity = 1.0;
      avatarLight.color = ovrLight.color;
      i.lights[idx] = avatarLight;
    }
  }

  i.geometry.camera = _WorldSpaceCameraPos;

  avatar_fillInputGeometry(i,
    v_WorldPos, v_Vertex.xyz/v_Vertex.w, normalize(v_Normal), normalize(v_Tangent.xyz),
    v_UVCoord1, v_UVCoord2,
    u_NormalScale,
    true);

    
  // MOD START Ultimate-GloveBall: add screenPos support
  i.geometry.screenPos = v_ScreenPos;
  // MOD END Ultimate-Gloveball
  i.geometry.color = v_Color;
  i.geometry.ormt = v_ORMT;

  float4 baseColor = StaticSelectMaterialModeColor(u_BaseColorSampler, i.geometry.texcoord_0, float4(i.geometry.color.rgb, 1.0));
  i.material.base_color = baseColor.rgb * u_BaseColorFactor.rgb;

  i.material.ramp_selector = 1.0 - float (u_RampSelector)/45.0;
  i.material.color_selector_lod = 0;
  i.material.base_color = avatar_debugBaseColor(i);

  i.material.alpha = i.geometry.color.a;

  float4 ormt = StaticSelectMaterialModeColor(u_MetallicRoughnessSampler, i.geometry.texcoord_0, i.geometry.ormt);
  i.material.occlusion = (lerp((1.0), (ormt.r), (u_OcclusionStrength)));
  i.material.roughness = ormt.g *  u_RoughnessFactor;
  i.material.metallic = ormt.b * u_MetallicFactor;
  i.material.f0 = 0.04;
  i.material.f90 = (lerp((1.0), (i.material.f0), (sqrt(i.material.roughness))));

  // MOD START Ultimate-GloveBall: Ambient Factor
  //  i.material.ambient_diffuse_factor = 1.0;
  i.material.ambient_diffuse_factor = u_AmbientDiffuseFactor;
  // MOD End Ultimate-GloveBall
  i.material.ambient_specular_factor = 1.0;
  i.ambient_color = v_SH;

  i.material.thickness = ormt.a * u_ThicknessFactor;
  i.material.thickness = u_ThicknessFactor;

  if(i.options.enableSkin) {
      i.material.occlusion *=  u_SkinORMFactor.r;
      i.material.roughness *= u_SkinORMFactor.g;
      i.material.metallic *= u_SkinORMFactor.b;
  }

  updateHairMaterial(i, ormt);
  updateRimLightMaterial(i);

  i.material.exposure = u_Exposure;
  i.material.skin_material.subsurface_color = u_SubsurfaceColor;
  i.material.skin_material.skin_ORM_factor = u_SkinORMFactor;

  i.material.eye_glint_factor = u_EyeGlintFactor;
  i.material.eye_glint_color_factor = u_EyeGlintColorFactor;

  i.matrices.objectToWorld = unity_ObjectToWorld;
  i.matrices.worldToObject = float3x3(unity_WorldToObject[0].xyz, unity_WorldToObject[1].xyz, unity_WorldToObject[2].xyz);

  // VERY IMPORTANT: Cannot use v_WorldPos here, must use i.geometry.positionInWorldSpace
  float3 world_view_dir = computeViewDir(i.geometry.camera, i.geometry.positionInWorldSpace);
  i.geometry.worldViewDir = world_view_dir;
  // NOTE: Historically Unity does mipCount instead of mipCount-1:
  float mipCount = 1.0 * float(u_MipCount);
  i.geometry.lod = clamp(i.material.roughness * (mipCount), 0.0, mipCount);

  float3 reflectionVector = reflection(i.geometry.normal, i.geometry.worldViewDir);
  // the winding of the cube maps sourced from Khronos is flipped in Unity:
  float3 unityIblDiffuseVector = float3(-i.geometry.normal.x, i.geometry.normal.y, i.geometry.normal.z);
  float3 unityIblSpecularVector = float3(-reflectionVector.x, reflectionVector.y, reflectionVector.z);

  // allow the integrating application to do extra operations on the io structures at this point
  AppSpecificPreManipulation(i);

  /// CALCULATION OF OUTPUT STRUCTURE ///

  avatar_FragmentOutput o = avatar_computeFragment(i);

  // conduct the optional tonemapping operation based on the shader configuration
  o.color.rgb = avatar_tonemap(o.color.rgb);

  // compute the alpha coverage at the end to match the alpha for this pixel
  o.alphaCoverage = avatar_calculateAlphaCoverage(i, i.geometry.positionInClipSpace);

  // allow the integrating application to do extra operations on the io structures at this point
  AppSpecificPostManipulation(i, o);

  // this function is intended for final operations beyond the physical rendering, like the debuggers
  float4 finalColor = float4(o.color.rgb, 1.0);
#ifdef DEBUG_MODE_ON
  if(i.debugMode > 0)
  {
    finalColor = avatar_finalOutputColor(i, o);
  }
#endif

  // color space correction:
  // In Unity the final output is governed by UNITY_COLORSPACE_GAMMA, so call this function
  // If the integrating engine does not need this, we can just return the rgb from the input,
  // but this is the unity.frag.sca, so We assume that it is being used.
  finalColor.rgb = ConvertOutputColorSpaceFromLinear(finalColor.rgb);

  // hlsl should not use this gl_FragColor keyword, but it has to for the cross compilation

  _output_FragColor = finalColor;

}

FragmentOutput Fragment_main(VertexToFragment stage_input)
{
  v_Vertex = stage_input.v_Vertex;
  v_WorldPos = stage_input.v_WorldPos;
  v_Normal = stage_input.v_Normal;
  v_Tangent = stage_input.v_Tangent;
  v_UVCoord1 = stage_input.v_UVCoord1;
  v_UVCoord2 = stage_input.v_UVCoord2;
  v_Color = stage_input.v_Color;
  v_ORMT = stage_input.v_ORMT;
  v_SH = stage_input.v_SH;
  // MOD START Ultimate-GloveBall: add screenPos support
  v_ScreenPos = stage_input.v_ScreenPos;
  // MOD END Ultimate-Gloveball
  frag_Fragment_main();
  FragmentOutput stage_output;
  stage_output._output_FragColor = _output_FragColor;
  return stage_output;
}

// eof unity.frag.glsl
// UNITY FRAG CONFIG LOADED

avatar_FragOptions getFragOptions(float materialTypeChannel) {
  avatar_FragOptions options = avatar_zeroOptions();
  options.enableNormalMapping = enableNormalMapping;
  options.enableAlphaToCoverage = enableAlphaToCoverage;
  options.enableRimLight = enableRimLight;
  if(useSubmesh) {
    options.enableSkin = enableSkin && avatar_UseSkin(materialTypeChannel);
    options.enableEyeGlint = enableEyeGlint && avatar_UseEyeGlint(materialTypeChannel);
    options.enableHeadHair = enableHair && avatar_UseHeadHair(materialTypeChannel);
    options.enableFacialHair = enableHair && avatar_UseFacialHair(materialTypeChannel);
    options.enableAnyHair = options.enableHeadHair || options.enableFacialHair;
  } else {
    options.enableSkin = enableSkin;
    options.enableEyeGlint = enableEyeGlint;
    // NOTE: For permutaion purposes, during uncombined rendering we treat head hair and
    // facial hair as equivalent. The only difference is in which constants we use. But
    // for uncombined rendering we can just load them all into the base hair constants.
    // Only combined rendering needs to distinguish at execution time, and there is a
    // separate set of facial hair constants that will override the base hair constants
    // when combined rendering is on and the material type points to facial hair.
    options.enableHeadHair = enableHair;
    options.enableFacialHair = false;
    options.enableAnyHair = enableHair;
  }
#ifdef DEBUG_MODE_ON
  options.enablePreviewColorRamp = enablePreviewColorRamp;
  options.hasBaseColorMap = hasBaseColorMap;
  options.hasMetallicRoughnessMap = hasMetallicRoughnessMap;
#endif

  return options;
}

avatar_FragmentOutput avatar_computeFragment(avatar_FragmentInput i) {
    avatar_FragmentOutput o = avatar_zeroFragmentOutput();
    float3 punctualDiffuseWrap = float3(0.0, 0.0, 0.0);
    float3 glintSpecular = float3(0.0, 0.0, 0.0);
    // Calculate diffuse and specular radiance
    int isEyeGlintLightUsed = 0;
    if (i.options.enableEyeGlint)
    {
        glintSpecular = avatar_getGlintSpecularPerCamera(i);
        isEyeGlintLightUsed = 1;
    }

    #if MAX_LIGHT_COUNT > 1
    for(int lightIdx = 0; lightIdx < MAX_LIGHT_COUNT; lightIdx++) {
    #else
    int lightIdx = 0;
    #endif
        if(lightIdx < i.lightsCount) {
            avatar_Light light = i.lights[lightIdx];

            float3 diffuseWrap = float3(0.0, 0.0, 0.0);
            // Only compute diffuse lighting from non-glint lights
            float3 diffuseRadiance = light.eyeGlint == 0 ? avatar_computeDiffuse(i.geometry, light, i.material, diffuseWrap) : float3(0, 0, 0);

            if (i.options.enableShadows) {
                    diffuseRadiance = diffuseRadiance * light.shadowTerm;
                    diffuseWrap = diffuseWrap * light.shadowTerm;
            }
            o.p_diffuse += diffuseRadiance;
            punctualDiffuseWrap += diffuseWrap;
                // enableLightMasks indicates that some of the lights will have a light mask,
                // such as l_eye_glint, r_eye_glint or eye_glint. If light masks are enabled
                // and the light doesn't match the light mask for the given shader i.e. the eye,
                // the shader bypasses the glint and spec computations so that the eye
                // only gets diffuse contributions from the non glint lights
            if (i.options.enableLightMask && ((1 << lightIdx) & i.lightMask) == 0) {
                continue;
            }

            float3 specular = float3(0.0, 0.0, 0.0);

            if (avatar_eyeGlintCondition(i, light))
            {
                float3 glintSpecular0 = avatar_getGlintSpecularPerLight(light, i);
                glintSpecular += glintSpecular0;
                isEyeGlintLightUsed = 1;
            }

            if(!i.options.enableEyeGlint) {
                specular = avatar_computeSpecular(i.geometry.normal, i.geometry.worldViewDir, light, i.lightLookParams, i.material.roughness, i.material.exposure, i.options);
            }

            if (i.options.enableShadows) {
                specular = specular * light.shadowTerm;
            }

            o.p_specular += specular;
        }
#if MAX_LIGHT_COUNT > 1
    }
#endif

    if (i.options.enableEyeGlint)
    {
        i.material.roughness = 0.0f;
        i.material.metallic = 0.0f;
        i.geometry.normal = i.geometry.tangentSpace.normal;
    }

    avatar_AmbientLighting ambient = avatar_computeAmbientLighting(i, o.p_specular);
    o.a_diffuse = ambient.diffuse * i.material.ambient_diffuse_factor * i.ambient_occlusion;
    float f0 = i.material.f0;

    float f90 = i.material.f90;
    float NdotV = computeNdotV(i.geometry.normal, i.geometry.worldViewDir);
    float fresnelPower = 1.0 - NdotV;
    fresnelPower = fresnelPower * fresnelPower * fresnelPower * fresnelPower * fresnelPower;
    float f = (f0 + (f90 - f0) * saturate(fresnelPower));

    float3 specularFactor = (lerp((f.xxx), (i.material.base_color), (i.material.metallic))) * i.material.occlusion;

    if (avatar_eyeGlintEnableSpecularFactor(i)) {
      o.p_specular *= specularFactor;
    }

    if(avatar_eyeGlintAddAmbientSpecular(i)) {
      o.a_specular = (i.options.enableEyeGlint) ? (f * ambient.specular) : float3(0.0, 0.0, 0.0);
    }

    o.a_specular += specularFactor * i.material.ambient_specular_factor * ambient.specular;

    avatar_addHairLightContribution(i, ambient, o);
    float3 specularColor = o.p_specular + o.a_specular;

    avatar_addSubsurfaceContribution(i, ambient, punctualDiffuseWrap, o);
    o.subSurfaceColor *= i.material.base_color;
    // For eye treat surface as non-metallic for diffuse shading
    float metallic = i.options.enableLightMask == true && isEyeGlintLightUsed > 0 ? 1.0 : 1.0 - i.material.metallic;
    float3 diffuseColor = i.material.occlusion * metallic * i.material.base_color;
    float3 diffuseContribution = (o.p_diffuse + o.a_diffuse) * diffuseColor;

    float3 rimLight = float3(0.0, 0.0, 0.0);
    if(i.options.enableRimLight) {
        rimLight += avatar_addRimLight(i.geometry, i.material);
    }

    float3 finalColor = float3(0.0, 0.0, 0.0);
    finalColor += diffuseContribution;
    finalColor += rimLight;
    finalColor += specularColor;
    finalColor += o.subSurfaceColor;
    finalColor *= i.material.exposure;
    finalColor += glintSpecular;

    o.color.rgb = finalColor;
    o.color.a = 1.0;

    return o;
}

// End of include: pbr-combined.frag.glsl
