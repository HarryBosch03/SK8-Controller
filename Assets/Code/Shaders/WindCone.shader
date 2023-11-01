Shader "Unlit/WindCone"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : SCREEN_POS;
                float3 positionOS : POSITION_OS;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.vertex = TransformObjectToHClip(input.vertex.xyz);
                output.uv.y = atan2(input.vertex.y, input.vertex.x) / 20.0;
                output.uv.x = input.vertex.z / 800.0 + floor(_Time[1] * 16) / 16 / 40;
                output.screenPos = ComputeScreenPos(output.vertex);
                output.positionOS = input.vertex.xyz;
                return output;
            }

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Unity_Dither_float(uint2 uv)
            {
                float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
                };
                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                return DITHER_THRESHOLDS[index];
            }


            half4 frag(Varyings input) : SV_Target
            {
                float dither = Unity_Dither_float(input.screenPos.xy / input.screenPos.w * _ScreenParams.xy);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip (col.r - 0.999);

                float taper = min(input.positionOS.z + 2.4, 2.8 - input.positionOS.z) * 0.5;

                clip(taper - dither);
                return 1.0;
            }
            ENDHLSL
        }
    }
}