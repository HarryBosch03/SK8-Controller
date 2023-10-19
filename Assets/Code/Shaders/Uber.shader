Shader "Uber"
{
	Properties
	{
		[MainTex]_MainTex ("Texture", 2D) = "white" {}
		[MainColor]_Albedo("Albedo", Color) = (1, 1, 1, 1)
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
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"

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
				float3 position : WORLDPOS;
			};

			Varyings vert (Attributes input)
			{
				Varyings output;
				output.position = TransformObjectToWorld(input.vertex.xyz);
				output.vertex = TransformWorldToHClip(output.position);
				output.uv = input.uv;
				output.normal = TransformObjectToWorldNormal(input.normal);
				return output;
			}

			static const float3 Ambient = float3(0.42, 0.478, 0.627	) * 0.5;
			
			float4 _Albedo;
			float4 _Specular;
			float _SpecularSize;
			
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			float smoothstep(float x)
			{
				x = saturate(x);
				return x * x * x * (x * (6.0f * x - 15.0f) + 10.0f);
			}
			
			half4 frag (Varyings input) : SV_Target
			{
				float3 light = normalize(_MainLightPosition.xyz);
				float3 normal = normalize(input.normal);
				float3 view = normalize(_WorldSpaceCameraPos - input.position);
				
				float ndl = dot(normal, light);
				float shadow = ndl;
				
				float dither = InterleavedGradientNoise(input.vertex, 0);
				shadow = smoothstep(shadow * 0.5 + 0.5) * 2 - 1 > (dither * 2 - 1) / 2;

				half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Albedo.rgb; 
				half3 col = 0.0;
				col += albedo * Ambient;
				col += albedo * _MainLightColor.rgb * saturate(shadow);

				float3 h = normalize(light + view);
				float ndh = saturate(dot(normal, h));

				float specular = saturate(ndh) - 0.996 > 0;
				col += _Specular.rgb * specular * _Specular.a;
				
				return float4(col, 1.0);
			}
			ENDHLSL
		}
	}
}