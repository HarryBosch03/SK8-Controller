Shader "Skybox/Skybox BiTone"
{
	Properties
	{
		_Bottom ("Bottom Color", Color) = (0.6, 0.6, 0.6, 1.0)
		_Top ("Top Color", Color) = (0.8, 0.8, 0.8, 1.0)
	}
	SubShader
	{
		Tags 
		{ 
			"RenderType"="Background"
			"Queue"="Background"
			"PreviewType"="Skybox"
		}
		Cull Off ZWrite Off

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

			float4 _Bottom, _Top;
			
			half4 frag (Varyings input) : SV_Target
			{
				return lerp(_Bottom, _Top, saturate(input.uv.y));
			}
			ENDHLSL
		}
	}
}