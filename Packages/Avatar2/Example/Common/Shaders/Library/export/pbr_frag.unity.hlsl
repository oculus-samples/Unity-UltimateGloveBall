#ifdef ENABLE_enableEnvironmentMap
  static const bool enableEnvironmentMap = true;
#else
  static const bool enableEnvironmentMap = false;
#endif

static const bool enableHairStraight = false;
static const bool enableHairCoily = false;

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
    bool enableHairStraight;
    bool enableHairCoily;
};

struct avatar_HairMaterial
{
    float3 specular_color_factor;
    float specular_intensity;
    float specular_shift_intensity;
    float fresnel_power;
    float fresnel_offset;
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
    float3 subsurface_color;
    float3 skin_ORM_factor;
    float eye_glint_factor;
    float eye_glint_color_factor;
    avatar_HairMaterial hair_material;
};

struct avatar_Matrices
{
    float4x4 normal_matrix;
    float4x4 model_matrix;
};

struct avatar_Geometry
{
    float3 camera;
    float3 position;
    float3 normal;
    float3 bitangent;
    float2 texcoord_0;
    float2 texcoord_1;
    float4 tangent;
    float4 color;
    float4 ormt;
    float normalScale;
    float3 reflectionVector;
    float lod;
    float3 worldViewDir;
};

struct avatar_IndirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct avatar_HairParams
{
    float3 flow;
    float fade;
    float specularPower;
    float shift;
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

struct avatar_FragmentInput
{
    float3 ambient_color;
    avatar_Geometry geometry;
    avatar_Material material;
    int lightsCount;
    avatar_Light lights[8];
    avatar_Matrices matrices;
    avatar_FragOptions options;
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



uniform int Debug;
uniform sampler2D u_BaseColorSampler;
uniform sampler2D u_MetallicRoughnessSampler;
uniform sampler2D u_SphereMapEnvSampler;
uniform sampler2D u_FlowSampler;
uniform sampler2D u_NormalSampler;
uniform samplerCUBE u_LambertianEnvSampler;
uniform samplerCUBE u_GGXEnvSampler;
uniform float u_NormalScale;
uniform float4 u_BaseColorFactor;
uniform int u_MipCount;
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
uniform float3 u_SpecularColorFactor;
uniform float u_SpecularWhiteIntensity;
uniform float u_SpecularShiftIntensity;
uniform float u_SpecularIntensity;
uniform float u_FresnelPower;
uniform float u_FresnelOffset;

static float4 _16;



struct FragmentOutput
{
    float4 _16 : COLOR0;
};


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

avatar_FragOptions getFragOptions(float subMeshIdChannel)
{
    avatar_FragOptions options;
    options.enableNormalMapping = enableNormalMapping;
    options.enableSkin = enableSkin && avatar_UseSkin(subMeshIdChannel);
    bool _2460 = avatar_UseEyeGlint(subMeshIdChannel);
    options.enableEyeGlint = enableEyeGlint && _2460;
    options.enableEnvironmentMap = enableEnvironmentMap && _2460;
    options.enableHairStraight = enableHairStraight;
    options.enableHairCoily = enableHairCoily;
    return options;
}

float3 computeViewDir(float3 camera, float3 position)
{
    return normalize(position - camera);
}

float3 reflection(float3 normal, float3 world_view_dir)
{
    return world_view_dir - ((normal * 2.0f) * dot(world_view_dir, normal));
}

avatar_HemisphereNormalOffsets avatar_computeHemisphereNormalOffsets(avatar_FragmentInput i)
{
    avatar_HemisphereNormalOffsets hno;
    hno.nn = i.geometry.normal * 0.707099974155426025390625f;
    hno.tt = i.geometry.tangent.xyz * 0.3535499870777130126953125f;
    hno.bb = normalize(cross(i.geometry.normal, i.geometry.tangent.xyz)) * 0.612348616123199462890625f;
    hno.lv1 = hno.nn + (hno.tt * 2.0f);
    hno.lv2 = (hno.nn + hno.bb) - hno.tt;
    hno.lv3 = (hno.nn - hno.bb) - hno.tt;
    return hno;
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
    float _1326 = d2_1.z;
    float4 o3 = (frac(perm(c + 1.0f.xxxx) * 0.024390242993831634521484375f) * _1326) + (frac(perm(c) * 0.024390242993831634521484375f) * (1.0f - _1326));
    float _1338 = d2_1.x;
    float2 o4 = (o3.yw * _1338) + (o3.xz * (1.0f - _1338));
    float _1350 = d2_1.y;
    return (o4.y * _1350) + (o4.x * (1.0f - _1350));
}

float saturate(float x)
{
    return clamp(x, 0.0f, 1.0f);
}

float computeNdotV(float3 normal, float3 world_view_dir)
{
    return saturate(-dot(normal, world_view_dir));
}

float3 avatar_computeSpecular(avatar_Geometry geometry, avatar_Light light, avatar_Material t)
{
    bool _return = false;
    float3 area_specular = 0.0f.xxx;
    avatar_Material t_1 = t;
    float3 point_to_light_1 = -light.direction;
    float3 point_to_light = point_to_light_1;
    bool _1515 = light.type == 3;
    if (_1515)
    {
        float nz = _noise(geometry.texcoord_0.xyx * 900.0f);
        nz *= 0.02999999932944774627685546875f;
        t_1.roughness = min(1.0f, (t.roughness + nz) * 1.7999999523162841796875f);
    }
    if (light.type != 0)
    {
        point_to_light = light.position - geometry.position;
        if (light.range <= 0.0f)
        {
        }
        else
        {
        }
    }
    if (light.type != 2)
    {
        float actualCos = dot(normalize(light.direction), normalize(-point_to_light));
        if (actualCos > light.outerConeCos)
        {
            if (actualCos < light.innerConeCos)
            {
            }
        }
    }
    float3 l = normalize(point_to_light);
    float3 world_view_dir = computeViewDir(geometry.camera, geometry.position);
    float NdotL = saturate(dot(geometry.normal, l));
    float NdotH = saturate(dot(geometry.normal, normalize(l - world_view_dir)));
    if (light.range <= 0.0f)
    {
    }
    else
    {
    }
    float roughPow2 = t_1.roughness * t_1.roughness;
    float roughPow4 = roughPow2 * roughPow2;
    float invRoughPow4 = 1.0f - roughPow4;
    float NdotV = computeNdotV(geometry.normal, world_view_dir);
    float ggx = (NdotL * sqrt(((NdotV * NdotV) * invRoughPow4) + roughPow2)) + (NdotV * sqrt(((NdotL * NdotL) * invRoughPow4) + roughPow2));
    float t0;
    if (ggx > 0.0f)
    {
        t0 = 0.5f / ggx;
    }
    else
    {
        t0 = 0.0f;
    }
    float t1 = 1.0f / (1.0f - ((NdotH * NdotH) * invRoughPow4));
    float3 p_specular = (light.color * (1.0f / t.exposure)) * ((((NdotL * t1) * t1) * roughPow4) * t0);
    float3 _returnValue;
    if (_1515)
    {
        float3 Ln = normalize(point_to_light_1);
        float3 tangent = cross(Ln, float3(1.0f, 0.0f, 0.0f));
        float3 bitangent = cross(tangent, Ln);
        for (float i = 0.0f; i < 5.0f; i += 1.0f)
        {
            for (float j = 0.0f; j < 5.0f; j += 1.0f)
            {
                point_to_light += ((tangent * 0.1599999964237213134765625f) * (i - 2.5f));
                point_to_light += ((bitangent * 0.1599999964237213134765625f) * (j - 2.5f));
            }
        }
        area_specular = 0.0f.xxx + p_specular;
        _return = true;
        _returnValue = area_specular / 25.0f.xxx;
    }
    if (!_return)
    {
        _return = true;
        _returnValue = p_specular;
    }
    return _returnValue;
}

float3 avatar_computeDiffuse(avatar_Geometry geometry, avatar_Light light, avatar_Material material)
{
    return (light.color * (1.0f / material.exposure)) * saturate(dot(geometry.normal, normalize(-light.direction)));
}

float3 avatar_getEyeGlintPunctualSpecular(avatar_FragmentInput i, avatar_FragmentOutput o, avatar_Light t)
{
    avatar_Light t_1 = t;
    t_1.direction.x = -t.direction.x;
    t_1.direction.z = -t_1.direction.z;
    float3 _1906 = avatar_computeSpecular(i.geometry, t_1, i.material);
    float3 glintSpecular = o.p_specular + _1906;
    t_1.direction.x = -t_1.direction.x;
    t_1.direction.z = -t_1.direction.z;
    return lerp(1.0f.xxx, t_1.color, i.material.eye_glint_color_factor.xxx) * (((glintSpecular.x + glintSpecular.y) + glintSpecular.z) * i.material.eye_glint_factor);
}

float3 saturate(float3 v)
{
    return clamp(v, 0.0f.xxx, 1.0f.xxx);
}

float3 RRTAndODTFit(float3 color)
{
    return ((color * (color + 0.02457859925925731658935546875f.xxx)) - 9.0537003416102379560470581054688e-05f.xxx) / ((color * ((color * 0.98372900485992431640625f) + 0.4329510033130645751953125f.xxx)) + 0.23808099329471588134765625f.xxx);
}

float3 avatar_tonemap(float3 t)
{
    return saturate(mul(RRTAndODTFit(mul(t / 0.60000002384185791015625f.xxx, float3x3(float3(0.59719002246856689453125f, 0.075999997556209564208984375f, 0.0284000001847743988037109375f), float3(0.354579985141754150390625f, 0.908339977264404296875f, 0.13382999598979949951171875f), float3(0.048229999840259552001953125f, 0.0156599991023540496826171875f, 0.837769985198974609375f)))), float3x3(float3(1.60475003719329833984375f, -0.10208000242710113525390625f, -0.00326999998651444911956787109375f), float3(-0.5310800075531005859375f, 1.108129978179931640625f, -0.07276000082492828369140625f), float3(-0.0736699998378753662109375f, -0.00604999996721744537353515625f, 1.0760200023651123046875f))));
}

float3 SRGBtoLINEAR(float3 srgbIn)
{
    return pow(srgbIn, 2.2000000476837158203125f.xxx);
}

bool avatar_IsOfType(float idChannel, float subMeshType)
{
    return avatar_WithinRange(idChannel, subMeshType, subMeshType);
}

void frag_Fragment_main()
{
    avatar_FragmentInput i;
    i.options = getFragOptions(v_Color.w);
    int lightCount = getLightCount();
    i.ambient_color = 0.250999987125396728515625f.xxx;
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
    i.geometry.normalScale = u_NormalScale;
    i.geometry.normal = normalize(v_Normal);
    i.geometry.tangent = v_Tangent;
    bool _return = false;
    float3 _returnValue;
    if (!i.options.enableNormalMapping)
    {
        _return = true;
        _returnValue = i.geometry.normal;
    }
    if (!_return)
    {
        float3 shadingNormal = ((tex2D(u_NormalSampler, i.geometry.texcoord_0) * 2.0f) - 1.0f.xxxx).xyz;
        shadingNormal *= float3(i.geometry.normalScale, i.geometry.normalScale, 1.0f);
        float3 bitangent = normalize(cross(i.geometry.normal, i.geometry.tangent.xyz));
        _return = true;
        _returnValue = mul(normalize(shadingNormal), float3x3(i.geometry.tangent.xyz, bitangent, i.geometry.normal));
    }
    i.geometry.normal = _returnValue;
    i.geometry.color = v_Color;
    i.geometry.ormt = v_ORMT;
    i.material.base_color = StaticSelectMaterialModeColor(u_BaseColorSampler, i.geometry.texcoord_0, float4(i.geometry.color.x, i.geometry.color.y, i.geometry.color.z, float2(0.0f, 1.0f).y)).xyz;
    i.material.alpha = i.geometry.color.w;
    float4 ormt = StaticSelectMaterialModeColor(u_MetallicRoughnessSampler, i.geometry.texcoord_0, i.geometry.ormt);
    i.material.occlusion = lerp(1.0f, ormt.x, u_OcclusionStrength);
    i.material.roughness = ormt.y * u_RoughnessFactor;
    i.material.metallic = ormt.z * u_MetallicFactor;
    i.material.thickness = ormt.w * u_ThicknessFactor;
    if (i.options.enableSkin)
    {
        i.material.occlusion *= u_SkinORMFactor.x;
        i.material.roughness *= u_SkinORMFactor.y;
        i.material.metallic *= u_SkinORMFactor.z;
    }
    i.material.hair_material.specular_color_factor = u_SpecularColorFactor;
    if (i.options.enableHairStraight)
    {
        i.material.hair_material.specular_intensity = u_SpecularWhiteIntensity;
        i.material.hair_material.specular_shift_intensity = u_SpecularShiftIntensity;
    }
    else
    {
        if (i.options.enableHairCoily)
        {
            i.material.hair_material.specular_intensity = u_SpecularIntensity;
            i.material.hair_material.fresnel_power = u_FresnelPower;
            i.material.hair_material.fresnel_offset = u_FresnelOffset;
        }
    }
    i.material.exposure = u_Exposure;
    i.material.subsurface_color = u_SubsurfaceColor;
    i.material.skin_ORM_factor = u_SkinORMFactor;
    i.material.eye_glint_factor = u_EyeGlintFactor;
    i.material.eye_glint_color_factor = u_EyeGlintColorFactor;
    i.matrices.normal_matrix = unity_WorldToObject;
    i.matrices.model_matrix = unity_ObjectToWorld;
    float3 world_view_dir_1 = computeViewDir(i.geometry.camera, i.geometry.position);
    i.geometry.worldViewDir = world_view_dir_1;
    i.geometry.reflectionVector = normalize(reflection(i.geometry.normal, world_view_dir_1));
    float mipCount = 1.0f * float(u_MipCount);
    i.geometry.lod = clamp(i.material.roughness * mipCount, 0.0f, mipCount);
    float3 finalColor = 0.0f.xxx;
    avatar_FragmentOutput o_1;
    o_1.p_specular = 0.0f.xxx;
    o_1.p_diffuse = 0.0f.xxx;
    avatar_HemisphereNormalOffsets hemisphereNormalOffsets = avatar_computeHemisphereNormalOffsets(i);
    for (int idx_1 = 0; idx_1 < 8; idx_1++)
    {
        if (idx_1 < i.lightsCount)
        {
            avatar_Light light = i.lights[idx_1];
            float3 punctualSpec = avatar_computeSpecular(i.geometry, light, i.material);
            o_1.p_specular += punctualSpec;
            o_1.p_diffuse += avatar_computeDiffuse(i.geometry, light, i.material);
            if (i.options.enableEyeGlint)
            {
                o_1.p_specular += avatar_getEyeGlintPunctualSpecular(i, o_1, light);
            }
        }
    }
    if (i.options.enableEnvironmentMap)
    {
        float3 n = i.geometry.normal;
        float3 v = computeViewDir(i.geometry.camera, i.geometry.position);
        float roughness = i.material.roughness;
        float3 r = normalize(reflect(-v, n));
        float m = 2.0f * sqrt((pow(r.x, 2.0f) + pow(r.y, 2.0f)) + pow(r.z + 1.0f, 2.0f));
        float2 uv = (float2(r.x, -r.y) / m.xx) + 0.5f.xx;
        float3 sphereMapColor = tex2D(u_SphereMapEnvSampler, float3(uv.x, uv.y, float2(0.0f, 1.0f).x).xy).xyz;
        float3 sphereWeight = (1.0f - roughness).xxx;
        o_1.p_specular += ((sphereMapColor * sphereWeight) * 1.0f);
    }
    float3 diffuse = 0.0f.xxx;
    float3 specular = 0.0f.xxx;
    OvrGetUnityGlobalIllumination(i.lights[0].color, i.lights[0].direction, i.geometry.position, -i.geometry.worldViewDir, 1.0f, i.ambient_color, 1.0f - i.material.roughness, i.material.metallic, i.material.occlusion, i.material.base_color, i.geometry.normal, o_1.p_specular, diffuse, specular);
    avatar_IndirectLighting indirect_lighting;
    indirect_lighting.diffuse = diffuse;
    indirect_lighting.specular = specular;
    float _2923 = 1.0f - i.material.metallic;
    float3 specularFactor = ((_2923 * 0.039999999105930328369140625f).xxx + (i.material.base_color * i.material.metallic)) * i.material.occlusion;
    o_1.p_specular = specularFactor * o_1.p_specular;
    o_1.a_specular = specularFactor * indirect_lighting.specular;
    o_1.a_specular = o_1.a_specular;
    avatar_FragmentOutput _2955 = o_1;
    float3 diffuseFactor = i.material.base_color * (i.material.occlusion * _2923);
    o_1.p_diffuse = diffuseFactor * _2955.p_diffuse;
    o_1.a_diffuse = diffuseFactor * indirect_lighting.diffuse;
    avatar_FragmentOutput _2984 = o_1;
    o_1.subSurfaceColor = 0.0f.xxx;
    if (i.options.enableSkin)
    {
        float3 subsurfaceColor = 0.0f.xxx;
        float3 p_diffuse = 0.0f.xxx;
        float3 accumulatedDiffuseColor = 0.0f.xxx;
        float3 _3244 = texCUBE(u_LambertianEnvSampler, hemisphereNormalOffsets.lv1);
        float3 ibl_diffuse1 = float4(_3244.x, _3244.y, _3244.z, float2(0.0f, 1.0f).x).xyz;
        float3 _3248 = texCUBE(u_LambertianEnvSampler, hemisphereNormalOffsets.lv2);
        float3 ibl_diffuse2 = float4(_3248.x, _3248.y, _3248.z, float2(0.0f, 1.0f).x).xyz;
        float3 _3252 = texCUBE(u_LambertianEnvSampler, hemisphereNormalOffsets.lv3);
        float3 ibl_diffuse3 = float4(_3252.x, _3252.y, _3252.z, float2(0.0f, 1.0f).x).xyz;
        for (int lightIdx = 0; lightIdx < 8; lightIdx++)
        {
            if (lightIdx < i.lightsCount)
            {
                float3 worldSpaceLightDir = -i.lights[lightIdx].direction;
                float3 lightColor = (i.lights[lightIdx].color * i.lights[lightIdx].intensity) * (1.0f / 3.1415927410125732421875f);
                float3 directionalLightColor = lightColor * (1.0f / i.material.exposure);
                float directionalSoftDiffuseValue = (saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv1)) + saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv2))) + saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.lv3));
                p_diffuse += (directionalSoftDiffuseValue.xxx * directionalLightColor);
                accumulatedDiffuseColor += (directionalLightColor * saturate(dot(worldSpaceLightDir, hemisphereNormalOffsets.nn)));
            }
        }
        float3 softDiffuseLight = (((ibl_diffuse1 + ibl_diffuse2) + ibl_diffuse3) + p_diffuse) * 0.3333333432674407958984375f;
        float3 baseColor = i.material.base_color;
        subsurfaceColor = (saturate(softDiffuseLight - (accumulatedDiffuseColor * saturate(i.material.occlusion + 0.4000000059604644775390625f))) * float3(1.0f, 0.300000011920928955078125f, 0.20000000298023223876953125f)) * baseColor;
        o_1.subSurfaceColor = subsurfaceColor;
        o_1.subSurfaceColor = o_1.subSurfaceColor;
        finalColor = 0.0f.xxx + o_1.subSurfaceColor;
    }
    float3 _3007 = finalColor;
    float3 finalColor_1 = _3007 + (_2984.p_diffuse + _2984.a_diffuse);
    finalColor = finalColor_1;
    float3 finalColor_2 = finalColor_1 + (_2955.p_specular + _2955.a_specular);
    finalColor = finalColor_2;
    float3 finalColor_3 = finalColor_2 * i.material.exposure;
    finalColor = finalColor_3;
    o_1.color = float4(finalColor_3.x, finalColor_3.y, finalColor_3.z, o_1.color.w);
    o_1.color.w = 1.0f;
    avatar_FragmentOutput o = o_1;
    float3 _2733 = avatar_tonemap(o_1.color.xyz);
    o.color = float4(_2733.x, _2733.y, _2733.z, o.color.w);
    float4 color = o.color;
    if (Debug == 1)
    {
        color = float4(i.material.base_color.x, i.material.base_color.y, i.material.base_color.z, float2(0.0f, 1.0f).y);
    }
    if (Debug == 2)
    {
        float3 _3272 = i.material.occlusion.xxx;
        color = float4(_3272.x, _3272.y, _3272.z, color.w);
    }
    if (Debug == 3)
    {
        float3 _3282 = ConvertOutputColorSpaceFromSRGB(i.material.roughness.xxx);
        color = float4(_3282.x, _3282.y, _3282.z, color.w);
    }
    if (Debug == 4)
    {
        float3 _3292 = ConvertOutputColorSpaceFromSRGB(i.material.metallic.xxx);
        color = float4(_3292.x, _3292.y, _3292.z, color.w);
    }
    if (Debug == 5)
    {
        float3 _3301 = i.material.thickness.xxx;
        color = float4(_3301.x, _3301.y, _3301.z, color.w);
    }
    if (Debug == 6)
    {
        color = float4(i.geometry.normal.x, i.geometry.normal.y, i.geometry.normal.z, color.w);
    }
    if (Debug == 7)
    {
    }
    if (Debug == 8)
    {
    }
    if (Debug == 9)
    {
        float3 world_view_dir = computeViewDir(i.geometry.camera, i.geometry.position);
        color = float4(world_view_dir.x, world_view_dir.y, world_view_dir.z, float2(0.0f, 1.0f).y);
    }
    if (Debug == 10)
    {
        float3 _3337 = o.p_specular + o.p_diffuse;
        color = float4(_3337.x, _3337.y, _3337.z, color.w);
    }
    if (Debug == 11)
    {
        color = float4(o.p_specular.x, o.p_specular.y, o.p_specular.z, color.w);
    }
    if (Debug == 12)
    {
        color = float4(o.p_diffuse.x, o.p_diffuse.y, o.p_diffuse.z, color.w);
    }
    if (Debug == 13)
    {
        float3 _3360 = o.a_specular + o.a_diffuse;
        color = float4(_3360.x, _3360.y, _3360.z, color.w);
    }
    if (Debug == 14)
    {
        color = float4(o.a_specular.x, o.a_specular.y, o.a_specular.z, color.w);
    }
    if (Debug == 15)
    {
        color = float4(o.a_diffuse.x, o.a_diffuse.y, o.a_diffuse.z, color.w);
    }
    if (Debug == 16)
    {
    }
    if (Debug == 17)
    {
        color = float4(o.subSurfaceColor.x, o.subSurfaceColor.y, o.subSurfaceColor.z, color.w);
    }
    if (Debug == 18)
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
    _16 = float4(color.xyz.x, color.xyz.y, color.xyz.z, color.w);
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
    frag_Fragment_main();
    FragmentOutput stage_output;
    stage_output._16 = float4(_16);
    return stage_output;
}

// Generated by AvatarShaderLibrary

