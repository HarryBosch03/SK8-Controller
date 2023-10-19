Shader "Uber"
{
	Properties
	{
		[MainTex]_MainTex ("Texture", 2D) = "white" {}
		[MainColor]_AlbedoHigh("Albedo High", Color) = (1, 1, 1, 1)
		_AlbedoLow("Albedo Low", Color) = (0.5, 0.5, 0.5, 1)
		_Specular("Specular", Color) = (1, 1, 1, 1.0)
		_SpecularSize("Specular Size", Range(0.0, 90.0)) = 5.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define MAIN_LIGHT_CALCULATE_SHADOWS
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 positionWS : WORLDPOS;
				float3 positionOS : OBJPOS;
				float4 shadowCoord : SHADOWCOORD;
			};

			Varyings vert (Attributes input)
			{
				Varyings output;
				output.positionOS = input.vertex.xyz;
				output.positionWS = TransformObjectToWorld(input.vertex.xyz);
				output.vertex = TransformWorldToHClip(output.positionWS);
				output.uv = input.uv;
				output.normal = TransformObjectToWorldNormal(input.normal);
				output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
				return output;
			}

			static const float Steps = 8;
			
			float4 _AlbedoHigh;
			float4 _AlbedoLow;
			float4 _Specular;
			float _SpecularSize;
			
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			half4 frag (Varyings input) : SV_Target
			{
				float3 normal = normalize(input.normal);
				float3 view = UNITY_MATRIX_IT_MV[2].xyz;
				
				Light light = GetMainLight(input.shadowCoord);
				float3 lightDir = normalize(light.direction);
				
				float ndl = dot(normal, lightDir);
				float attenuation = saturate(ndl) * light.shadowAttenuation;

				half3 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
				half3 col = base * lerp(_AlbedoLow, _AlbedoHigh, floor(attenuation * Steps) / Steps);

				float3 h = normalize(lightDir + view);
				float ndh = saturate(dot(normal, h));

				float specular = saturate(ndh) - cos(_SpecularSize * PI / 180.0) > 0;
				col = lerp(col, _Specular.rgb, specular * _Specular.a);
				
				return float4(col, 1.0);
			}
			ENDHLSL
		}
		Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
        	Cull Front

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 2.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
		Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 2.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
	}
}