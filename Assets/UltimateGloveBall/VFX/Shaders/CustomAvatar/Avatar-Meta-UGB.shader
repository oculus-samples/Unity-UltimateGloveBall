// MOD START Ultimate-GloveBall: Custom Avatar-Meta.shader
// This file is a copy from the Avatar-Meta.shader found in the Meta Avatars SDK. So that we can apply the necessary
// modifications for the additional effects.
// Shader "Avatar/Meta"
Shader "Avatar/Ultimate Glove Ball"
// MOD START Ultimate-GloveBall
{
	Properties
	{
		// NOTE: This texture can be visualized in the Unity editor, just expand in inspector and manually change "Dimension" to "2D" on top line
		u_AttributeTexture("Vertex Attribute map", 3D) = "white" {}

	u_PropertyTexBias("Property Mip Bias (-1 to 1)", float) = 0

		[NoScaleOffset] u_NormalSampler("Normal map", 2D) = "white" {}
		u_NormalScale("Normal map scale", Float) = 1.0
		u_NormalUVSet("Normal UV Set", Int) = 0

		[NoScaleOffset] u_EmissiveSampler("Emissive map", 2D) = "black" {}
		u_EmissiveSet("Emissive UV Set", Int) = 0
		u_EmissiveFactor("Emissive factor", Color) = (1, 1, 1, 1)

		[NoScaleOffset] u_OcclusionSampler("Occlusion map", 2D) = "white" {}
		u_OcclusionSet("Occlusion UV Set", Int) = 0
		u_OcclusionStrength("Occlusion scale", Float) = 1.0

		[NoScaleOffset] u_BaseColorSampler("Base Color", 2D) = "white" {}
		u_BaseColorUVSet("Base Color UV Set", Int) = 0

		u_BaseColorFactor("Base Color factor", Color) = (1, 1, 1, 1)

		[NoScaleOffset]  u_MetallicRoughnessSampler("Metallic Roughness", 2D) = "white" {}
		u_MetallicRoughnessUVSet("Metallic Roughness UV Set", Int) = 0

		u_MetallicFactor("Metallic Factor", Range(0, 2)) = 1.0
		u_RoughnessFactor("Roughness Factor", Range(0, 2)) = 1.0
		u_F0Factor("F0 Factor", Range(0, 2)) = 1.0
		u_OcclusionStrength("Occlusion Strength", Range(0, 2)) = 1.0
		u_ThicknessFactor("Thickness Factor", Range(0, 2)) = 1.0

		u_Exposure("Material Exposure", Range(0, 2)) = 1.0

		[ShowIfKeyword(SKIN_ON)]
		u_SubsurfaceColor("Skin Sub-Surface Color", Color) = (1, 1, 1, 1)
		[ShowIfKeyword(SKIN_ON)]
		u_SkinORMFactor("Skin-only ORM Factor", Vector) = (1, 1, 1)

		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSubsurfaceColor("Hair Sub-Surface Color", Color) = (1, 1, 1, 1)
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularColorFactor("Hair Specular Color Factor", Color) = (1, 1, 1, 1)
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairScatterIntensity("Hair Scatter intensity", Range(0, 1)) = 1.
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularShiftIntensity("Hair Specular Shift Intensity", Range(-1,1)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularWhiteIntensity("Hair Specular White Intensity", Range(0,10)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularColorIntensity("Hair Specular Color Intensity", Range(0,10)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularColorOffset("Hair Specular Color Offset", Range(-1,1)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairRoughness("Hair Roughness", Range(0,1)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairColorRoughness("Hair Color Roughness", Range(0,1)) = .4
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairAnisotropicIntensity("Hair Anistropic Intensity", Range(-1,1)) = .5
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularNormalIntensity("Hair Specular Normal Intensity", Range(0,1)) = 1.
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairSpecularGlint("HairSpecularGlint", Range(0,1)) = .1
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_HairDiffusedIntensity("Hair Diffuse Intensity", Range(0,10)) = .25

		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSubsurfaceColor("Facial Hair Sub-Surface Color", Color) = (1, 1, 1, 1)
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularColorFactor("Facial Hair Specular Color Factor", Color) = (1, 1, 1, 1)
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairScatterIntensity("Facial Hair Scatter intensity", Range(0, 1)) = 1.
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularShiftIntensity("Facial Hair Specular Shift Intensity", Range(-1,1)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularWhiteIntensity("Facial Hair Specular White Intensity", Range(0,10)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularColorIntensity("Facial Hair Specular Color Intensity", Range(0,10)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularColorOffset("Facial Hair Specular Color Offset", Range(-1,1)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairRoughness("Facial Hair Roughness", Range(0,1)) = .2
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairColorRoughness("Facial Hair Color Roughness", Range(0,1)) = .4
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairAnisotropicIntensity("Facial Hair Anistropic Intensity", Range(-1,1)) = .5
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularNormalIntensity("Facial Hair Specular Normal Intensity", Range(0,1)) = 1.
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairSpecularGlint("Facial HairSpecularGlint", Range(0,1)) = .1
		[ShowIfKeyword(ENABLE_HAIR_ON)]
		u_FacialHairDiffusedIntensity("Facial Hair Diffuse Intensity", Range(0,10)) = .25

		[ShowIfKeyword(ENABLE_RIM_LIGHT_ON)]
		u_RimLightIntensity("Rim Light Intensity", Range(0,1)) = 0.6
		[ShowIfKeyword(ENABLE_RIM_LIGHT_ON)]
		u_RimLightBias("Rim Light Bias", Range(0.0,1.0)) = 0.5
		[ShowIfKeyword(ENABLE_RIM_LIGHT_ON)]
		u_RimLightColor("Rim Light Color", Color) = (1,1,1)
		[ShowIfKeyword(ENABLE_RIM_LIGHT_ON)]
		u_RimLightTransition("Rim Light Transition", Range(0,0.2)) = 0.0000001
		[ShowIfKeyword(ENABLE_RIM_LIGHT_ON)]
		u_RimLightStartPosition("Rim Light Start Position", Range(0,1)) = 0.1
		[ShowIfKeyword(ENABLE_RIM_LIGHT_ON)]
		u_RimLightEndPosition("Rim Light End Position", Range(0,1)) = 0.5

		[ShowIfKeyword(EYE_GLINTS_ON)]
		u_EyeGlintFactor("Eye Glint Factor", Range(0, 4.0)) = 2.0
		[ShowIfKeyword(EYE_GLINTS_ON)]
		u_EyeGlintColorFactor("Eye Glint Color Factor", Range(0, 1.0)) = 0.5


			// These should not exist here, since they should be in global shader scope and handeled by an external manager:
			//
			//u_DiffuseEnvSampler("IBL Diffuse Cubemap Texture", Cube) = "white" {}
			//u_MipCount("IBL Diffuse Texture Mip Count", Int) = 10
			//u_SpecularEnvSampler ("IBL Specular Cubemap Texture", Cube) = "white" {}
			//u_brdfLUT ("BRDF LUT Texture", 2D) = "Assets/Oculus/Avatar2/Example/Scenes/BRDF_LUT" {}

			// SHADER OPTIONS: These must match the options specified in options_common.hlsl
			[Toggle] HAS_NORMAL_MAP("Has Normal Map", Float) = 0  // NOTE: This value is set programatically on the existence of the hair quality flag.
			[Toggle] SKIN("Skin", Float) = 1    // NOTE: This default value only works on editor.
			[Toggle] EYE_GLINTS("Eye Glints", Float) = 1    // NOTE: This default value only works on editor.
			[Toggle] ENABLE_HAIR("Enable Hair", Float) = 0  // NOTE: This value is set programatically on the existence of the hair quality flag.
			[Toggle] ENABLE_RIM_LIGHT("Enable Rim Light", Float) = 0    // NOTE: This value is still untested.

			// DEBUG_MODES: Uncomment to use Debug modes, do not create a multi_compile for this, as it takes up permutations and memory. Instead static branch on DEBUG_NONE and the value of floating point uniform "Debug".
			[KeywordEnum(None, BaseColor, Occlusion, Roughness, Metallic, Thickness, Normal, Normal Map, Emissive, View, Punctual, Punctual Specular, Punctual Diffuse, Ambient, Ambient Specular, Ambient Diffuse, No Tone Map, SubSurface Scattering, Submeshes, Tangent, Bitangent, SSAO, Mesh Normal, Alpha, Material Type)] Debug("Debug Render", Float) = 0

			// MATERIAL_MODES: Uncomment to use Material modes, must match the multi_compile defined below
			[KeywordEnum(Texture, Vertex)] Material_Mode("Material Mode", Float) = 0

			// Cull mode (Off, Front, Back)
			[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
		
		// MOD START Ultimate-GloveBall: Add property header
		[Header (Ultimate GloveBall)]
		// MOD End Ultimate-GloveBall
		// MOD START Ultimate-GloveBall: Ambient Diffuse Factor
		u_AmbientDiffuseFactor("Ambient Diffuse Factor", Range(0,1)) = 1
		// MOD End Ultimate-GloveBall
		
		// MOD START Ultimate-GloveBall: Dissolve Effect
		[Header (Dissolve Effect)]
		[Toggle(ENABLE_CUSTOM_EFFECT)]ENABLE_CUSTOM_EFFECT("Enable custom effect", Float) = 0
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_DisAmount("_DisAmount", Range(-2, 2)) = 2
		Noise_Scale("Noise_Scale", Range(0, 10)) = 2.5
		Noise_Scroll_Speed("Noise_Scroll_Speed", Range(0, 1)) = 1
		Noise_Cutoff("Noise_Cutoff", Range(0, 1)) = 0.715
		Noise_Cutoff_Smoothness("Noise_Cutoff_Smoothness", Range(0, 1)) = 0.22
		Edge_Color("Edge_Color", Color) = (0, 1, 1, 0)
		Edge_Width("Edge_Width", Range(0, 1)) = 0.113
		Edge_Brightness("Edge_Brightness", Range(0, 10)) = 10
		[Toggle(INVERT)]INVERT("Invert", Float) = 0
		// MOD END Ultimate-GloveBall

		// MOD START Ultimate-GloveBall: Ghost Effect
		[Header (Ghost Effect)]
		[Toggle(ENABLE_GHOST_EFFECT)]ENABLE_GHOST_EFFECT("Enable ghost effect", Float) = 0
		[HDR]Interior_Color("InteriorColor", Color) = (0.9433962, 0.9433962, 0.9433962, 1)
		[HDR]Fresnel_Color("FresnelColor", Color) = (1, 1, 1, 1)
		Caustics_Color("CausticsColor", Color) = (1, 1, 1, 1)
		Fresnel_Power("FresnelPower", Float) = 0.4
		[NoScaleOffset]FresnelNoise("FresnelNoise", 2D) = "white" {}
		Fresnel_Noise_Scale("FresnelNoiseScale", Vector) = (2, 2, 0, 0)
		Fresnel_Noise_Speed("FresnelNoiseSpeed", Vector) = (0, 0.2, 0, 0)
		_Opacity("Opacity", Float) = 0.85
		// MOD END Ultimate-GloveBall
	}

		// Universal Render Pipeline (URP), shader target 5.0
			SubShader
		{
			Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
			Pass
			{
				PackageRequirements
				{
				  "com.unity.render-pipelines.universal" : "10.1.0"
				}
				Tags { "LightMode" = "UniversalForward" }

				HLSLPROGRAM
				#pragma editor_sync_compilation // avoid showing the "invalid" teal Unity shader during loading

				#pragma vertex Vertex_main_instancing
				#pragma fragment Fragment_main

			// 5.0 required for SV_Coverage, 3.5 required for SV_VertexID
			#pragma target 5.0
			#pragma shader_feature UNITY_PIPELINE_URP   // Works before Unity 2021
            #define UNITY_PIPELINE_URP                  // Works after Unity 2021

			#include "app_specific/app_declarations.hlsl"   // replace this with an app_specific declarations file
			#include_with_pragmas "MetaAvatarCore.hlsl"
			#include "app_specific/app_functions.hlsl"   // replace this with an app_specific functions file
			ENDHLSL
		}
		}

			// Unity Built-in Render Pipeline, shader target 5.0
			SubShader
		{
			Tags { "RenderPipeline" = "" "RenderType" = "Opaque" }
			LOD 100
			Cull[_Cull]

			// Single Light
			Pass
			{
				Tags { "LightMode" = "ForwardBase" }

				CGPROGRAM
				#pragma editor_sync_compilation // avoid showing the "invalid" teal Unity shader during loading

				#pragma vertex Vertex_main_instancing
				#pragma fragment Fragment_main

			// 5.0 required for SV_Coverage, 3.5 required for SV_VertexID
			#pragma target 5.0
			#include "app_specific/app_declarations.hlsl"   // replace this with an app_specific declarations file
			#include_with_pragmas "MetaAvatarCore.hlsl"
			#include "app_specific/app_functions.hlsl"   // replace this with an app_specific functions file
			ENDCG
		}

			// Up to 4 Additive Lights
			Pass
			{
				Tags { "LightMode" = "ForwardAdd" }
				Blend One One
				ZWrite Off

				CGPROGRAM
				#pragma editor_sync_compilation // avoid showing the "invalid" teal Unity shader during loading

				#pragma vertex Vertex_main_instancing
				#pragma fragment Fragment_main

			// 5.0 required for SV_Coverage, 3.5 required for SV_VertexID
			#pragma target 5.0
			#include "app_specific/app_declarations.hlsl"   // replace this with an app_specific declarations file
			#include_with_pragmas "MetaAvatarCore.hlsl"
			#include "app_specific/app_functions.hlsl"   // replace this with an app_specific functions file
			ENDCG
		}
		}

			// Universal Render Pipeline (URP), shader target 3.5 compatibility mode
			SubShader
		{
			Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
			Pass
			{
				PackageRequirements
				{
				  "com.unity.render-pipelines.universal" : "10.1.0"
				}
				Tags { "LightMode" = "UniversalForward" }

				HLSLPROGRAM
				#pragma editor_sync_compilation // avoid showing the "invalid" teal Unity shader during loading

				#pragma vertex Vertex_main_instancing
				#pragma fragment Fragment_main

				#pragma target 3.5

                #pragma shader_feature UNITY_PIPELINE_URP   // Works before Unity 2021
                #define UNITY_PIPELINE_URP                  // Works after Unity 2021

				#include "app_specific/app_declarations.hlsl"   // replace this with an app_specific declarations file
				#include_with_pragmas "MetaAvatarCore.hlsl"
				#include "app_specific/app_functions.hlsl"   // replace this with an app_specific functions file
				ENDHLSL
			}
		}

			// Unity Built-in Render Pipeline, shader target 3.5 compatibility mode
			SubShader
		{
			Tags { "RenderPipeline" = "" "RenderType" = "Opaque" }
			LOD 100
			Cull[_Cull]

			// Single Light
			Pass
			{
				Tags { "LightMode" = "ForwardBase" }

				CGPROGRAM
				#pragma editor_sync_compilation // avoid showing the "invalid" teal Unity shader during loading

				#pragma vertex Vertex_main_instancing
				#pragma fragment Fragment_main

				#pragma target 3.5
				#include "app_specific/app_declarations.hlsl"   // replace this with an app_specific declarations file
				#include_with_pragmas "MetaAvatarCore.hlsl"
				#include "app_specific/app_functions.hlsl"   // replace this with an app_specific functions file
				ENDCG
			}

			// Up to 4 Additive Lights
			Pass
			{
				Tags { "LightMode" = "ForwardAdd" }
				Blend One One
				ZWrite Off

				CGPROGRAM
				#pragma editor_sync_compilation // avoid showing the "invalid" teal Unity shader during loading

				#pragma vertex Vertex_main_instancing
				#pragma fragment Fragment_main

				#pragma target 3.5
				#include "app_specific/app_declarations.hlsl"   // replace this with an app_specific declarations file
				#include_with_pragmas "MetaAvatarCore.hlsl"
				#include "app_specific/app_functions.hlsl"   // replace this with an app_specific functions file
				ENDCG
			}
		}
}
