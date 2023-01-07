#ifdef ENABLE_enableEnvironmentMap
  static const bool enableEnvironmentMap = true;
#else
  static const bool enableEnvironmentMap = false;
#endif


struct avatar_Light
{
    float3 direction;
    float range;
    float3 color;
    float intensity;
    float3 position;
    float innerConeCos;
    float outerConeCos;
    int type;
};

struct avatar_FragOptions
{
    bool enableNormalMapping;
    bool enableSkin;
    bool enableEyeGlint;
    bool enableEnvironmentMap;
    bool enableHair;
};

struct avatar_SkinMaterial
{
    float3 subsurface_color;
    float3 skin_ORM_factor;
};

struct avatar_HairMaterial
{
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
    float flow_angle;
    float shift;
    float blend;
};

struct avatar_Material
{
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
    avatar_SkinMaterial skin;
    avatar_HairMaterial hair_material;
};

struct avatar_Matrices
{
    float4x4 objectToWorld;
    float3x3 worldToObject;
};

struct avatar_TangentSpace
{
    float3 normal;
    float3 tangent;
    float3 bitangent;
};

struct avatar_Geometry
{
    float3 camera;
    float3 position;
    float3 normal;
    avatar_TangentSpace tangentSpace;
    float2 texcoord_0;
    float2 texcoord_1;
    float4 color;
    float4 ormt;
    float normalScale;
    float3 worldViewDir;
    float lod;
};

struct avatar_AmbientLighting
{
    float3 diffuse;
    float3 specular;
};

struct avatar_FragmentInput
{
    avatar_FragOptions options;
    avatar_Matrices matrices;
    avatar_Geometry geometry;
    avatar_Material material;
    int lightsCount;
    avatar_Light lights[8];
    float3 ambient_color;
    int debugMode;
};

struct avatar_FragmentOutput
{
    float4 color;
    float3 p_specular;
    float3 p_diffuse;
    float3 a_specular;
    float3 a_diffuse;
    float3 subSurfaceColor;
};

struct avatar_HemisphereNormalOffsets
{
    float3 nn;
    float3 bb;
    float3 tt;
    float3 lv1;
    float3 lv2;
    float3 lv3;
};

struct avatar_SpecularData
{
    float3 directional_light_color;
    float range_attentuation;
    float spot_attenuation;
    float _distance;
    float3 point_to_light;
    float actualCos;
    float3 l;
    float3 h;
    float roughPow2;
    float roughPow4;
    float invRoughPow4;
    float NdotV;
};



uniform sampler2D u_BaseColorSampler;
uniform sampler2D u_MetallicRoughnessSampler;
uniform sampler2D u_SphereMapEnvSampler;
uniform sampler2D u_FlowSampler;
uniform sampler2D u_NormalSampler;
uniform samplerCUBE u_LambertianEnvSampler;
uniform samplerCUBE u_GGXEnvSampler;
uniform sampler2D _ICMap;
uniform samplerCUBE _ReflectionCubeMap;
uniform float u_NormalScale;
uniform float4 u_BaseColorFactor;
uniform int u_MipCount;
uniform int Debug;
uniform float u_Exposure;
uniform float u_MetallicFactor;
uniform float u_OcclusionStrength;
uniform float u_RoughnessFactor;
uniform float u_ThicknessFactor;
uniform float3 u_SubsurfaceColor;
uniform float3 u_SkinORMFactor;
uniform float u_EyeGlintFactor;
uniform float u_EyeGlintColorFactor;
uniform int u_lightCount;
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

static float4 _15;



struct FragmentOutput
{
    float4 _15 : COLOR0;
};


float mod(float x, float y)
{
    return x - y * floor(x / y);
}

float2 mod(float2 x, float2 y)
{
    return x - y * floor(x / y);
}

float3 mod(float3 x, float3 y)
{
    return x - y * floor(x / y);
}

float4 mod(float4 x, float4 y)
{
    return x - y * floor(x / y);
}

avatar_Geometry avatar_zeroGeometry()
{
    avatar_Geometry geometry;
    geometry.camera = 0.0f.xxx;
    geometry.position = 0.0f.xxx;
    geometry.texcoord_0 = 0.0f.xx;
    geometry.texcoord_1 = 0.0f.xx;
    geometry.color = 0.0f.xxxx;
    geometry.normalScale = 0.0f;
    geometry.tangentSpace.normal = 0.0f.xxx;
    geometry.tangentSpace.tangent = 0.0f.xxx;
    geometry.tangentSpace.bitangent = 0.0f.xxx;
    geometry.normal = 0.0f.xxx;
    return geometry;
}

avatar_HairMaterial avatar_zeroHairMaterial()
{
    avatar_HairMaterial material;
    material.subsurface_color = 0.0f.xxx;
    material.scatter_intensity = 0.0f;
    material.specular_color_factor = 0.0f.xxx;
    material.specular_shift_intensity = 0.0f;
    material.specular_white_intensity = 0.0f;
    material.specular_white_roughness = 0.0f;
    material.specular_color_intensity = 0.0f;
    material.specular_color_offset = 0.0f;
    material.specular_color_roughness = 0.0f;
    material.anisotropic_intensity = 0.0f;
    material.diffused_intensity = 0.0f;
    material.normal_intensity = 0.0f;
    material.specular_glint = 0.0f;
    material.ao_intensity = 0.0f;
    material.flow_angle = 0.0f;
    material.shift = 0.0f;
    material.blend = 0.0f;
    return material;
}

avatar_Material avatar_zeroMaterial()
{
    avatar_Material material0;
    material0.base_color = 0.0f.xxx;
    material0.alpha = 0.0f;
    material0.exposure = 0.0f;
    material0.metallic = 0.0f;
    material0.occlusion = 0.0f;
    material0.roughness = 0.0f;
    material0.thickness = 0.0f;
    material0.skin.subsurface_color = 0.0f.xxx;
    material0.skin.skin_ORM_factor = 0.0f.xxx;
    material0.eye_glint_factor = 0.0f;
    material0.eye_glint_color_factor = 0.0f;
    material0.hair_material = avatar_zeroHairMaterial();
    return material0;
}

avatar_Matrices avatar_zeroMatrices()
{
    avatar_Matrices matrices;
    matrices.objectToWorld = float4x4(0.0f.xxxx, 0.0f.xxxx, 0.0f.xxxx, 0.0f.xxxx);
    matrices.worldToObject = float3x3(0.0f.xxx, 0.0f.xxx, 0.0f.xxx);
    return matrices;
}

avatar_FragOptions avatar_zeroOptions()
{
    avatar_FragOptions options;
    options.enableNormalMapping = false;
    options.enableSkin = false;
    options.enableEyeGlint = false;
    options.enableEnvironmentMap = false;
    options.enableHair = false;
    return options;
}

avatar_Light avatar_zeroLight()
{
    avatar_Light light;
    light.direction = 0.0f.xxx;
    light.range = 0.0f;
    light.color = 0.0f.xxx;
    light.intensity = 0.0f;
    light.position = 0.0f.xxx;
    light.innerConeCos = 0.0f;
    light.outerConeCos = 0.0f;
    light.type = 0;
    return light;
}

avatar_FragmentInput avatar_zeroFragmentInput()
{
    avatar_FragmentInput i;
    i.ambient_color = 0.0f.xxx;
    i.geometry = avatar_zeroGeometry();
    i.material = avatar_zeroMaterial();
    i.matrices = avatar_zeroMatrices();
    i.debugMode = 0;
    i.options = avatar_zeroOptions();
    for (int idx = 0; idx < 8; idx++)
    {
        i.lights[idx] = avatar_zeroLight();
    }
    return i;
}

bool avatar_WithinRange(float idChannel, float lowBound, float highBound)
{
    return (idChannel > (lowBound - 0.001953125f)) && (idChannel < (highBound + 0.001953125f));
}

bool avatar_UseSkin(float idChannel)
{
    return avatar_WithinRange(idChannel, 0.0078125f, 0.015625f);
}

bool avatar_UseEyeGlint(float idChannel)
{
    return avatar_WithinRange(idChannel, 0.125f, 0.25f);
}

bool avatar_IsOfType(float idChannel, float subMeshType)
{
    return avatar_WithinRange(idChannel, subMeshType, subMeshType);
}

bool avatar_UseHair(float idChannel)
{
    return avatar_IsOfType(idChannel, 0.03125f);
}

avatar_FragOptions getFragOptions(float subMeshIdChannel)
{
    avatar_FragOptions options;
    options.enableNormalMapping = enableNormalMapping;
    options.enableSkin = enableSkin && avatar_UseSkin(subMeshIdChannel);
    bool _3238 = avatar_UseEyeGlint(subMeshIdChannel);
    options.enableEyeGlint = enableEyeGlint && _3238;
    options.enableEnvironmentMap = enableEnvironmentMap && _3238;
    options.enableHair = enableHair && avatar_UseHair(subMeshIdChannel);
    return options;
}

float3 computeViewDir(float3 camera, float3 position)
{
    return normalize(position - camera);
}

void AppSpecificPreManipulation(inout avatar_FragmentInput i);

avatar_FragmentOutput avatar_zeroFragmentOutput()
{
    avatar_FragmentOutput o;
    o.color = 0.0f.xxxx;
    o.p_specular = 0.0f.xxx;
    o.p_diffuse = 0.0f.xxx;
    o.a_specular = 0.0f.xxx;
    o.a_diffuse = 0.0f.xxx;
    o.subSurfaceColor = 0.0f.xxx;
    return o;
}

float saturate(float x)
{
    return clamp(x, 0.0f, 1.0f);
}

float computeNdotV(float3 normal, float3 world_view_dir)
{
    return saturate(-dot(normal, world_view_dir));
}

avatar_SpecularData avatar_fillInSpecularData(avatar_Geometry geometry, avatar_Light light, avatar_Material material, avatar_FragOptions options)
{
    avatar_SpecularData sd;
    sd.directional_light_color = (light.color * (1.0f / material.exposure)) * 0.3183098733425140380859375f;
    sd.point_to_light = -light.direction;
    sd.range_attentuation = 0.0f;
    sd.spot_attenuation = 0.0f;
    sd._distance = length(sd.point_to_light);
    if (light.type != 0)
    {
        sd.point_to_light = light.position - geometry.position;
        if (light.range <= 0.0f)
        {
            sd.range_attentuation = 1.0f / pow(sd._distance, 2.0f);
        }
        else
        {
            sd.range_attentuation = max(min(1.0f - pow(sd._distance / light.range, 4.0f), 1.0f), 0.0f) / pow(sd._distance, 2.0f);
        }
    }
    if (light.type != 2)
    {
        sd.actualCos = dot(normalize(light.direction), normalize(-sd.point_to_light));
        if (sd.actualCos > light.outerConeCos)
        {
            if (sd.actualCos < light.innerConeCos)
            {
                sd.spot_attenuation = smoothstep(light.outerConeCos, light.innerConeCos, sd.actualCos);
            }
        }
    }
    sd.l = normalize(sd.point_to_light);
    sd.h = normalize(sd.l - geometry.worldViewDir);
    if (light.range <= 0.0f)
    {
        sd.range_attentuation = 1.0f / pow(sd._distance, 2.0f);
    }
    else
    {
        sd.range_attentuation = max(min(1.0f - pow(sd._distance / light.range, 4.0f), 1.0f), 0.0f) / pow(sd._distance, 2.0f);
    }
    sd.roughPow2 = material.roughness * material.roughness;
    sd.roughPow4 = sd.roughPow2 * sd.roughPow2;
    sd.invRoughPow4 = 1.0f - sd.roughPow4;
    sd.NdotV = computeNdotV(geometry.normal, geometry.worldViewDir);
    return sd;
}

float3 avatar_computeSpecular(avatar_Geometry geometry, avatar_Light t, avatar_Material material, avatar_FragOptions options)
{
    float sumAreaLight = 0.0f;
    avatar_SpecularData sd = avatar_fillInSpecularData(geometry, t, material, options);
    float NdotL = saturate(dot(geometry.normal, sd.l));
    float NdotH = saturate(dot(geometry.normal, sd.h));
    float _2125 = sd.NdotV;
    float _2130 = sd.invRoughPow4;
    float _2133 = sd.roughPow2;
    float ggx = (NdotL * sqrt(((_2125 * _2125) * _2130) + _2133)) + (_2125 * sqrt(((NdotL * NdotL) * _2130) + _2133));
    float t0;
    if (ggx > 0.0f)
    {
        t0 = 0.5f / ggx;
    }
    else
    {
        t0 = 0.0f;
    }
    float t1 = 1.0f / (1.0f - ((NdotH * NdotH) * _2130));
    for (int i = 0; i < 5; i++)
    {
        for (int j = 0; j < 5; j++)
        {
            sumAreaLight += ((((NdotL * t1) * t1) * sd.roughPow4) * t0);
        }
    }
    return (sd.directional_light_color * (sumAreaLight / 25.0f)) * 0.300000011920928955078125f;
}

float3 avatar_computeDiffuse(avatar_Geometry geometry, avatar_Light light, avatar_Material material)
{
    return ((light.color * (1.0f / material.exposure)) * 0.3183098733425140380859375f) * saturate((dot(geometry.normal, normalize(-light.direction)) + 0.4799999892711639404296875f) / 1.480000019073486328125f);
}

float3 getEyeGlintPunctualSpecular(avatar_FragmentInput i, avatar_FragmentOutput o, avatar_Light t)
{
    avatar_Light t_1 = t;
    t_1.direction.x = -t.direction.x;
    t_1.direction.z = -t_1.direction.z;
    float3 _2727 = avatar_computeSpecular(i.geometry, t_1, i.material, i.options);
    float3 glintSpecular = o.p_specular + _2727;
    t_1.direction.x = -t_1.direction.x;
    t_1.direction.z = -t_1.direction.z;
    return lerp(1.0f.xxx, t_1.color, i.material.eye_glint_color_factor.xxx) * (((glintSpecular.x + glintSpecular.y) + glintSpecular.z) * i.material.eye_glint_factor);
}

float4 mod289(float4 x)
{
    return x - (floor(x * 0.00346020772121846675872802734375f) * 289.0f);
}

float4 perm(float4 x)
{
    return mod289(((x * 34.0f) + 1.0f.xxxx) * x);
}

float _noise(float3 p)
{
    float3 a1 = floor(p);
    float3 d2 = p - a1;
    float3 d2_1 = (d2 * d2) * (3.0f.xxx - (d2 * 2.0f));
    float4 b3 = a1.xxyy + float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 c = perm(perm(b3.xyxy).xyxy + b3.zzww) + a1.zzzz;
    float _1639 = d2_1.z;
    float4 o3 = (frac(perm(c + 1.0f.xxxx) * 0.024390242993831634521484375f) * _1639) + (frac(perm(c) * 0.024390242993831634521484375f) * (1.0f - _1639));
    float _1651 = d2_1.x;
    float2 o4 = (o3.yw * _1651) + (o3.xz * (1.0f - _1651));
    float _1663 = d2_1.y;
    return (o4.y * _1663) + (o4.x * (1.0f - _1663));
}

float3 computeSpecularHighlight(float anisotropicIntensity, float roughness, float3 L, float3 t, float3 t0, float3 tangent, float3 bitangent, float offset)
{
    float3 H = 0.0f.xxx;
    float kspec = 0.0f;
    float kexp = 0.0f;
    float3 spec = 0.0f.xxx;
    float3 t_1 = t;
    float3 t0_1 = normalize(t0 + (bitangent * offset));
    if (anisotropicIntensity > 0.0500000007450580596923828125f)
    {
        H = normalize((t + L) - (tangent * dot(t + L, tangent)));
        t_1 = normalize(t - (tangent * dot(t, tangent)));
        kspec = dot(H, t0_1);
        kexp = 1.0f / ((roughness * roughness) + 0.001000000047497451305389404296875f);
    }
    else
    {
        H = normalize(t_1 + L);
        kspec = dot(t0_1, H);
        kexp = 3.0f / ((roughness * roughness) + 0.001000000047497451305389404296875f);
    }
    if (kspec > (-0.001000000047497451305389404296875f))
    {
        float specular = pow(max(0.0f, kspec), kexp);
        spec = 0.0f.xxx + specular.xxx;
    }
    return spec;
}

float3 computeHairSpecular(avatar_FragmentInput i, float3 lightVector, float3x3 hairCoordinateSystem)
{
    float3 E = -i.geometry.worldViewDir;
    float3 L = normalize(-lightVector);
    float localOffset = (i.material.hair_material.specular_shift_intensity * (i.material.hair_material.shift - 0.5f)) + (((_noise(float3(i.geometry.texcoord_0.x, i.geometry.texcoord_0.y, float2(0.0f, 1.0f).x) * 2000.0f.xxx) * 2.0f) - 1.0f) * 0.00999999977648258209228515625f);
    return ((computeSpecularHighlight(i.material.hair_material.anisotropic_intensity, i.material.hair_material.specular_white_roughness, L, E, hairCoordinateSystem[0], hairCoordinateSystem[2], hairCoordinateSystem[1], localOffset) * i.material.hair_material.specular_white_intensity) + ((computeSpecularHighlight(i.material.hair_material.anisotropic_intensity, i.material.hair_material.specular_color_roughness, L, E, hairCoordinateSystem[0], hairCoordinateSystem[2], hairCoordinateSystem[1], i.material.hair_material.specular_color_offset + localOffset) * i.material.hair_material.specular_color_factor) * i.material.hair_material.specular_color_intensity)) * i.material.hair_material.ao_intensity;
}

float3 blendPunctualSpecularWithHair(float3 punctualSpec, float3 hairPunctualSpec, float hairBlend)
{
    return lerp(punctualSpec, hairPunctualSpec, hairBlend.xxx);
}

avatar_HemisphereNormalOffsets avatar_computeHemisphereNormalOffsets(avatar_FragmentInput i)
{
    avatar_HemisphereNormalOffsets hno;
    hno.nn = i.geometry.tangentSpace.normal * 0.707099974155426025390625f;
    hno.tt = i.geometry.tangentSpace.tangent * 0.3535499870777130126953125f;
    hno.bb = i.geometry.tangentSpace.bitangent * 0.612348616123199462890625f;
    hno.lv1 = hno.nn + (hno.tt * 2.0f);
    hno.lv2 = (hno.nn + hno.bb) - hno.tt;
    hno.lv3 = (hno.nn - hno.bb) - hno.tt;
    return hno;
}

float3 saturate(float3 v)
{
    return clamp(v, 0.0f.xxx, 1.0f.xxx);
}

void AppSpecificPostManipulation(avatar_FragmentInput i, inout avatar_FragmentOutput o);

float3 SRGBtoLINEAR(float3 srgbIn)
{
    return pow(srgbIn, 2.2000000476837158203125f.xxx);
}

float4 avatar_finalOutputColor(avatar_FragmentInput i, avatar_FragmentOutput o)
{
    float4 color = o.color;
    if (i.debugMode == 1)
    {
        color = float4(i.material.base_color.x, i.material.base_color.y, i.material.base_color.z, float2(0.0f, 1.0f).y);
    }
    if (i.debugMode == 2)
    {
        float3 _2813 = i.material.occlusion.xxx;
        color = float4(_2813.x, _2813.y, _2813.z, color.w);
    }
    if (i.debugMode == 3)
    {
        float3 _2824 = ConvertOutputColorSpaceFromSRGB(i.material.roughness.xxx);
        color = float4(_2824.x, _2824.y, _2824.z, color.w);
    }
    if (i.debugMode == 4)
    {
        float3 _2835 = ConvertOutputColorSpaceFromSRGB(i.material.metallic.xxx);
        color = float4(_2835.x, _2835.y, _2835.z, color.w);
    }
    if (i.debugMode == 5)
    {
        float3 _2845 = i.material.thickness.xxx;
        color = float4(_2845.x, _2845.y, _2845.z, color.w);
    }
    bool _2850 = i.debugMode == 6;
    if (_2850)
    {
        color = float4(i.geometry.normal.x, i.geometry.normal.y, i.geometry.normal.z, color.w);
    }
    if (_2850)
    {
        float3 _2865 = (i.geometry.normal * 0.5f) + 0.5f.xxx;
        color = float4(_2865.x, _2865.y, _2865.z, color.w);
    }
    if (i.debugMode == 19)
    {
        float3 _2876 = (i.geometry.tangentSpace.tangent * 0.5f) + 0.5f.xxx;
        color = float4(_2876.x, _2876.y, _2876.z, color.w);
    }
    if (i.debugMode == 20)
    {
        float3 _2887 = (i.geometry.tangentSpace.bitangent * 0.5f) + 0.5f.xxx;
        color = float4(_2887.x, _2887.y, _2887.z, color.w);
    }
    if (i.debugMode == 7)
    {
    }
    if (i.debugMode == 8)
    {
    }
    if (i.debugMode == 9)
    {
        float3 world_view_dir = computeViewDir(i.geometry.camera, i.geometry.position);
        color = float4(world_view_dir.x, world_view_dir.y, world_view_dir.z, float2(0.0f, 1.0f).y);
    }
    if (i.debugMode == 10)
    {
        float3 _2919 = o.p_specular + o.p_diffuse;
        color = float4(_2919.x, _2919.y, _2919.z, color.w);
    }
    if (i.debugMode == 11)
    {
        color = float4(o.p_specular.x, o.p_specular.y, o.p_specular.z, color.w);
    }
    if (i.debugMode == 12)
    {
        color = float4(o.p_diffuse.x, o.p_diffuse.y, o.p_diffuse.z, color.w);
    }
    if (i.debugMode == 13)
    {
        float3 _2945 = o.a_specular + o.a_diffuse;
        color = float4(_2945.x, _2945.y, _2945.z, color.w);
    }
    if (i.debugMode == 14)
    {
        color = float4(o.a_specular.x, o.a_specular.y, o.a_specular.z, color.w);
    }
    if (i.debugMode == 15)
    {
        color = float4(o.a_diffuse.x, o.a_diffuse.y, o.a_diffuse.z, color.w);
    }
    if (i.debugMode == 16)
    {
    }
    if (i.debugMode == 17)
    {
        color = float4(o.subSurfaceColor.x, o.subSurfaceColor.y, o.subSurfaceColor.z, color.w);
    }
    if (i.debugMode == 18)
    {
        float subMeshId = i.material.alpha;
        color = float4(0.0f.xxx.x, 0.0f.xxx.y, 0.0f.xxx.z, color.w);
        if (avatar_IsOfType(subMeshId, 0.00390625f))
        {
            color = float4(0.20000000298023223876953125f.xxx.x, 0.20000000298023223876953125f.xxx.y, 0.20000000298023223876953125f.xxx.z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.0078125f))
        {
            color = float4(float3(0.769999980926513671875f, 0.64999997615814208984375f, 0.64999997615814208984375f).x, float3(0.769999980926513671875f, 0.64999997615814208984375f, 0.64999997615814208984375f).y, float3(0.769999980926513671875f, 0.64999997615814208984375f, 0.64999997615814208984375f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.015625f))
        {
            color = float4(float3(0.769999980926513671875f, 0.64999997615814208984375f, 0.64999997615814208984375f).x, float3(0.769999980926513671875f, 0.64999997615814208984375f, 0.64999997615814208984375f).y, float3(0.769999980926513671875f, 0.64999997615814208984375f, 0.64999997615814208984375f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.03125f))
        {
            color = float4(float3(0.3449999988079071044921875f, 0.2700000107288360595703125f, 0.10999999940395355224609375f).x, float3(0.3449999988079071044921875f, 0.2700000107288360595703125f, 0.10999999940395355224609375f).y, float3(0.3449999988079071044921875f, 0.2700000107288360595703125f, 0.10999999940395355224609375f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.0625f))
        {
            color = float4(float3(0.23999999463558197021484375f, 0.189999997615814208984375f, 0.07999999821186065673828125f).x, float3(0.23999999463558197021484375f, 0.189999997615814208984375f, 0.07999999821186065673828125f).y, float3(0.23999999463558197021484375f, 0.189999997615814208984375f, 0.07999999821186065673828125f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.125f))
        {
            color = float4(float3(0.0f, 0.0f, 1.0f).x, float3(0.0f, 0.0f, 1.0f).y, float3(0.0f, 0.0f, 1.0f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.25f))
        {
            color = float4(float3(0.0f, 1.0f, 0.0f).x, float3(0.0f, 1.0f, 0.0f).y, float3(0.0f, 1.0f, 0.0f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 0.5f))
        {
            color = float4(float3(0.5f, 0.0f, 0.0f).x, float3(0.5f, 0.0f, 0.0f).y, float3(0.5f, 0.0f, 0.0f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 1.0f))
        {
            color = float4(float3(0.20000000298023223876953125f, 0.100000001490116119384765625f, 0.0500000007450580596923828125f).x, float3(0.20000000298023223876953125f, 0.100000001490116119384765625f, 0.0500000007450580596923828125f).y, float3(0.20000000298023223876953125f, 0.100000001490116119384765625f, 0.0500000007450580596923828125f).z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 2.0f))
        {
            color = float4(0.100000001490116119384765625f.xxx.x, 0.100000001490116119384765625f.xxx.y, 0.100000001490116119384765625f.xxx.z, color.w);
        }
        if (avatar_IsOfType(subMeshId, 4.0f))
        {
            color = float4(1.0f.xxx.x, 1.0f.xxx.y, 1.0f.xxx.z, color.w);
        }
    }
    return color;
}

void frag_Fragment_main()
{
    avatar_FragmentInput i = avatar_zeroFragmentInput();
    i.options = getFragOptions(v_Color.w);
    i.debugMode = Debug;
    int lightCount = getLightCount();
    i.lightsCount = lightCount;
    i.lights[0].intensity = 1.0f;
    i.lights[0].direction = getLightDirection();
    i.lights[0].color = getLightColor();
    i.lights[0].position = getLightPosition();
    i.lights[0].type = 0;
    i.lights[0].range = 1000.0f;
    i.lights[0].innerConeCos = 0.0f;
    i.lights[0].outerConeCos = 0.0f;
    avatar_Light avatarLight;
    for (int idx = 1; idx < 8; idx++)
    {
        if (idx < lightCount)
        {
            OvrLight ovrLight = getAdditionalLight(idx, v_WorldPos);
            avatarLight.direction = -ovrLight.direction;
            avatarLight.intensity = 1.0f;
            avatarLight.color = ovrLight.color;
            avatarLight.position = ovrLight.direction;
            avatarLight.type = 0;
            avatarLight.range = 100.0f;
            avatarLight.innerConeCos = 0.0f;
            avatarLight.outerConeCos = 0.0f;
            i.lights[idx] = avatarLight;
        }
    }
    i.geometry.camera = _WorldSpaceCameraPos;
    i.geometry.position = v_WorldPos;
    i.geometry.texcoord_0 = v_UVCoord1;
    i.geometry.texcoord_1 = v_UVCoord2;
    i.geometry.normalScale = u_NormalScale;
    i.geometry.tangentSpace.normal = normalize(v_Normal);
    i.geometry.tangentSpace.tangent = v_Tangent.xyz;
    i.geometry.tangentSpace.bitangent = normalize(cross(i.geometry.tangentSpace.normal, i.geometry.tangentSpace.tangent));
    i.geometry.normal = i.geometry.tangentSpace.normal;
    bool _return = false;
    float3 _returnValue;
    if (!i.options.enableNormalMapping)
    {
        _return = true;
        _returnValue = i.geometry.normal;
    }
    if (!_return)
    {
        float3 shadingNormal = float3(((tex2D(u_NormalSampler, i.geometry.texcoord_0) * 2.0f) - 1.0f.xxxx).xy, 1.0f);
        shadingNormal *= float3(i.geometry.normalScale, i.geometry.normalScale, 1.0f);
        _return = true;
        _returnValue = mul(normalize(shadingNormal), float3x3(i.geometry.tangentSpace.tangent, i.geometry.tangentSpace.bitangent, i.geometry.tangentSpace.normal));
    }
    i.geometry.normal = _returnValue;
    i.geometry.color = v_Color;
    i.geometry.ormt = v_ORMT;
    i.material.base_color = StaticSelectMaterialModeColor(u_BaseColorSampler, i.geometry.texcoord_0, float4(i.geometry.color.x, i.geometry.color.y, i.geometry.color.z, float2(0.0f, 1.0f).y)).xyz;
    i.material.alpha = i.geometry.color.w;
    float4 ormt = StaticSelectMaterialModeColor(u_MetallicRoughnessSampler, i.geometry.texcoord_0, i.geometry.ormt);
    float _3445 = ormt.x;
    i.material.occlusion = lerp(1.0f, _3445, u_OcclusionStrength);
    float _3450 = ormt.y;
    i.material.roughness = _3450 * u_RoughnessFactor;
    float _3455 = ormt.z;
    i.material.metallic = _3455 * u_MetallicFactor;
    i.material.ambient_diffuse_factor = 1.0f;
    i.material.ambient_specular_factor = 1.0f;
    i.ambient_color = v_SH;
    float _3464 = ormt.w;
    i.material.thickness = _3464 * u_ThicknessFactor;
    i.material.thickness = u_ThicknessFactor;
    if (i.options.enableSkin)
    {
        i.material.occlusion *= u_SkinORMFactor.x;
        i.material.roughness *= u_SkinORMFactor.y;
        i.material.metallic *= u_SkinORMFactor.z;
    }
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
    i.material.hair_material.ao_intensity = _3445;
    i.material.hair_material.flow_angle = _3450;
    i.material.hair_material.shift = _3455;
    i.material.hair_material.blend = _3464;
    i.material.exposure = u_Exposure;
    i.material.skin.subsurface_color = u_SubsurfaceColor;
    i.material.skin.skin_ORM_factor = u_SkinORMFactor;
    i.material.eye_glint_factor = u_EyeGlintFactor;
    i.material.eye_glint_color_factor = u_EyeGlintColorFactor;
    i.matrices.objectToWorld = unity_ObjectToWorld;
    i.matrices.worldToObject = float3x3(unity_WorldToObject[0].xyz, unity_WorldToObject[1].xyz, unity_WorldToObject[2].xyz);
    i.geometry.worldViewDir = computeViewDir(i.geometry.camera, i.geometry.position);
    float mipCount = 1.0f * float(u_MipCount);
    i.geometry.lod = clamp(i.material.roughness * mipCount, 0.0f, mipCount);
    AppSpecificPreManipulation(i);
    avatar_FragmentOutput o_1 = avatar_zeroFragmentOutput();
    for (int lightIdx = 0; lightIdx < 8; lightIdx++)
    {
        if (lightIdx < i.lightsCount)
        {
            avatar_Light light = i.lights[lightIdx];
            o_1.p_specular += avatar_computeSpecular(i.geometry, light, i.material, i.options);
            o_1.p_diffuse += avatar_computeDiffuse(i.geometry, light, i.material);
        }
    }
    if ((i.options.enableEyeGlint && (!i.options.enableSkin)) && (!i.options.enableHair))
    {
        for (int lightIdx_1 = 0; lightIdx_1 < 8; lightIdx_1++)
        {
            if (lightIdx_1 < i.lightsCount)
            {
                o_1.p_specular += getEyeGlintPunctualSpecular(i, o_1, i.lights[lightIdx_1]);
            }
        }
    }
    if ((i.options.enableEyeGlint && (!i.options.enableSkin)) && (!i.options.enableHair))
    {
        o_1.p_specular += 0.0f.xxx;
    }
    float3 diffuse = 0.0f.xxx;
    float3 _3818 = -i.geometry.worldViewDir;
    float _3823 = 1.0f - i.material.roughness;
    OvrGetUnityDiffuseGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, _3818, 1.0f, i.ambient_color, _3823, i.material.metallic, i.material.occlusion, i.material.base_color, i.geometry.normal, 0.0f.xxx, diffuse);
    avatar_AmbientLighting ambient;
    ambient.diffuse = diffuse;
    float3 diffuse0 = 0.0f.xxx;
    float3 specular = 0.0f.xxx;
    OvrGetUnityGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, _3818, 1.0f, i.ambient_color, _3823, i.material.metallic, i.material.occlusion, i.material.base_color, i.geometry.normal, o_1.p_specular, diffuse0, specular);
    ambient.specular = specular;
    float3 totalhairPunctualSpec = 0.0f.xxx;
    if (i.options.enableHair)
    {
        float3 hairTangent = 0.0f.xxx;
        avatar_HairMaterial hair_mat = i.material.hair_material;
        float flowAngle = (hair_mat.flow_angle * 6.283185482025146484375f) - 3.1415927410125732421875f;
        flowAngle = mod(flowAngle, 3.1415927410125732421875f);
        float2 flow = float2(cos(flowAngle), 0.0f);
        flow.y = sin(flowAngle);
        hairTangent = (i.geometry.tangentSpace.tangent * flow.x) + (i.geometry.tangentSpace.bitangent * flow.y);
        float3 hairTangent3 = normalize(hairTangent);
        float3 hairNormal = lerp(i.geometry.tangentSpace.normal, i.geometry.normal, i.material.hair_material.normal_intensity.xxx);
        float3x3 hairCoordinateSystem = float3x3(hairNormal, hairTangent3, normalize(cross(hairNormal, hairTangent3)));
        float hairBlend = i.material.hair_material.blend;
        for (int lightIdx_2 = 0; lightIdx_2 < 8; lightIdx_2++)
        {
            if (lightIdx_2 < i.lightsCount)
            {
                avatar_Light light_1 = i.lights[lightIdx_2];
                totalhairPunctualSpec = computeHairSpecular(i, light_1.direction, hairCoordinateSystem);
            }
        }
        o_1.p_specular = blendPunctualSpecularWithHair(o_1.p_specular, totalhairPunctualSpec, hairBlend);
    }
    float3 specularFactor = (((1.0f - i.material.metallic) * 0.039999999105930328369140625f).xxx + (i.material.base_color * i.material.metallic)) * i.material.occlusion;
    if ((!i.options.enableHair) && (!i.options.enableEyeGlint))
    {
        o_1.p_specular *= specularFactor;
    }
    o_1.a_specular = (specularFactor * i.material.ambient_specular_factor) * ambient.specular;
    if (i.options.enableHair)
    {
        o_1.p_diffuse *= i.material.hair_material.diffused_intensity;
    }
    avatar_FragmentOutput _3725 = o_1;
    if (i.options.enableSkin)
    {
        float3 subsurfaceColor = 0.0f.xxx;
        float3 diffuse_1 = 0.0f.xxx;
        float3 accumulatedDiffuseColor = 0.0f.xxx;
        avatar_HemisphereNormalOffsets hemisphereNormalOffsets = avatar_computeHemisphereNormalOffsets(i);
        float3 diffuse_2 = 0.0f.xxx;
        OvrGetUnityDiffuseGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, -i.geometry.worldViewDir, 1.0f, i.ambient_color, 1.0f - i.material.roughness, i.material.metallic, i.material.occlusion, i.material.base_color, hemisphereNormalOffsets.lv1, 0.0f.xxx, diffuse_2);
        float3 ibl_diffuse1 = saturate(diffuse_2);
        float3 diffuse_3 = 0.0f.xxx;
        OvrGetUnityDiffuseGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, -i.geometry.worldViewDir, 1.0f, i.ambient_color, 1.0f - i.material.roughness, i.material.metallic, i.material.occlusion, i.material.base_color, hemisphereNormalOffsets.lv2, 0.0f.xxx, diffuse_3);
        float3 ibl_diffuse2 = saturate(diffuse_3);
        float3 diffuse_4 = 0.0f.xxx;
        OvrGetUnityDiffuseGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, -i.geometry.worldViewDir, 1.0f, i.ambient_color, 1.0f - i.material.roughness, i.material.metallic, i.material.occlusion, i.material.base_color, hemisphereNormalOffsets.lv3, 0.0f.xxx, diffuse_4);
        float3 ibl_diffuse3 = saturate(diffuse_4);
        float3 diffuse_5 = 0.0f.xxx;
        OvrGetUnityDiffuseGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, -i.geometry.worldViewDir, 1.0f, i.ambient_color, 1.0f - i.material.roughness, i.material.metallic, i.material.occlusion, i.material.base_color, hemisphereNormalOffsets.nn, 0.0f.xxx, diffuse_5);
        float3 ibl_diffuseN = saturate(diffuse_5);
        for (int lightIdx_3 = 0; lightIdx_3 < 8; lightIdx_3++)
        {
            if (lightIdx_3 < i.lightsCount)
            {
                float3 worldSpaceLightDir = -i.lights[lightIdx_3].direction;
                float3 lightColor = (i.lights[lightIdx_3].color * i.lights[lightIdx_3].intensity) * (1.0f / 3.1415927410125732421875f);
                float3 directionalLightColor = lightColor * (1.0f / i.material.exposure);
                float directionalSoftDiffuseValue = (saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv1)) + saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv2))) + saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv3));
                diffuse_1 += (directionalSoftDiffuseValue.xxx * directionalLightColor);
                accumulatedDiffuseColor += (directionalLightColor * saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.nn)));
            }
        }
        float3 softDiffuseLight = (((ibl_diffuse1 + ibl_diffuse2) + ibl_diffuse3) + diffuse_1) * 0.3333333432674407958984375f;
        subsurfaceColor = ((softDiffuseLight * float3(1.0f, 0.300000011920928955078125f, 0.20000000298023223876953125f)) * saturate(i.material.occlusion + 0.4000000059604644775390625f)) + (((accumulatedDiffuseColor + ibl_diffuseN) * float3(0.0f, 0.699999988079071044921875f, 0.800000011920928955078125f)) * i.material.occlusion);
        o_1.subSurfaceColor = subsurfaceColor;
        o_1.subSurfaceColor -= (o_1.p_diffuse * i.material.occlusion);
        o_1.subSurfaceColor = saturate(o_1.subSurfaceColor);
    }
    float3 diffuseFactor = i.material.base_color * (i.material.occlusion * (1.0f - i.material.metallic));
    o_1.p_diffuse = diffuseFactor * o_1.p_diffuse;
    o_1.a_diffuse = (diffuseFactor * i.material.ambient_diffuse_factor) * ambient.diffuse;
    if (i.options.enableSkin)
    {
        o_1.a_diffuse *= 0.0f;
    }
    o_1.subSurfaceColor *= i.material.base_color;
    float3 finalColor = (((0.0f.xxx + ((o_1.p_diffuse * 3.1415927410125732421875f) + o_1.a_diffuse)) + (_3725.p_specular + _3725.a_specular)) + o_1.subSurfaceColor) * i.material.exposure;
    o_1.color = float4(finalColor.x, finalColor.y, finalColor.z, o_1.color.w);
    o_1.color.w = 1.0f;
    avatar_FragmentOutput o = o_1;
    o.color = float4(o_1.color.xyz.x, o_1.color.xyz.y, o_1.color.xyz.z, o.color.w);
    AppSpecificPostManipulation(i, o);
    float4 finalColor_1 = avatar_finalOutputColor(i, o);
    float3 _3567 = finalColor_1.xyz;
    _15 = float4(_3567.x, _3567.y, _3567.z, finalColor_1.w);
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
    frag_Fragment_main();
    FragmentOutput stage_output;
    stage_output._15 = float4(_15);
    return stage_output;
}

// Generated by AvatarShaderLibrary cc2a82bc6dba+


