Shader "Unlit/OutlineBlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		
		ZWrite Off
		ZTest Always

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
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			Varyings vert (Attributes input)
			{
				Varyings output;
				output.vertex = TransformObjectToHClip(input.vertex.xyz);
				output.uv = input.uv;
				return output;
			}

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			float4 _MainTex_TexelSize;
			
			TEXTURE2D(_OutlineRT);
			SAMPLER(sampler_OutlineRT);

			float rand(int x)
			{
				float v = sin(x);
				v *= 12.9898;
				v = frac(sin(v) * 143758.5453);
				return v;
			}
			
			float fRand(float x)
			{
				int x0 = floor(x);
				int x1 = x0 + 1;
				float xp = x - x0;

				float r0 = rand(x0);
				float r1 = rand(x1);
				return lerp(r0, r1, xp);
			}
			
			half4 frag (Varyings input) : SV_Target
			{
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				
				float2 offset = (3.0 + fRand(_Time.y * 14) * 3.0) * _MainTex_TexelSize.xy;
				half4 outline0 = SAMPLE_TEXTURE2D(_OutlineRT, sampler_OutlineRT, input.uv);
				half4 outline1 = SAMPLE_TEXTURE2D(_OutlineRT, sampler_OutlineRT, input.uv + offset);
				half4 outline2 = SAMPLE_TEXTURE2D(_OutlineRT, sampler_OutlineRT, input.uv - offset);

				outline1 = saturate(outline1 - outline0);
				outline2 = saturate(outline2 - outline0);

				col.rgb = lerp(col.rgb, float3(0.0, 1.0, 1.0), outline2);
				col.rgb = lerp(col.rgb, float3(1.0, 0.0, 1.0), outline1);
				
				return col;
			}
			ENDHLSL
		}
	}
}