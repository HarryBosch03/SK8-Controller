Shader "Unlit/PaletteShader"
{
    Properties
    {
        _Palette("Palette", 2D) = "white" {}
        _Brightness("Brightness", Range(-1, 1)) = 0
        _Contrast("Contrast", Range(-1, 1)) = 0
        _Slope("Slope", float) = 0
        [Toggle]_FalseColor("False Color", int) = 0
        _Downscale("Dither Downscale", int) = 2
        _BluePoint("Blue Point", Color) = (0, 0, 1)
        _Passthrough("Passthrough", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPosition : POSITION_CS;
            };

            static const float4 verts[] =
            {
                float4(-1, -1, 0, 1),
                float4(3, -1, 0, 1),
                float4(-1, 3, 0, 1),
            };

            Varyings vert(uint id : SV_VertexID)
            {
                Varyings output;
                output.vertex = verts[id];
                output.uv = (verts[id].xy + 1.0) / 2.0;
                output.uv.y = 1 - output.uv.y;
                output.screenPosition = ComputeScreenPos(output.vertex);
                return output;
            }

            TEXTURE2D(_Palette);
            SAMPLER(sampler_Palette);
            float4 _Palette_TexelSize;

            float _Brightness;
            float _Contrast;

            float Unity_Dither_float(float In, uint2 uv)
            {
                float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
                };
                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                return In - DITHER_THRESHOLDS[index];
            }

            float3 rgb2okLab(float3 c)
            {
                float l0 = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
                float m0 = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
                float s0 = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

                float l1 = pow(l0, 1.0 / 3.0);
                float m1 = pow(m0, 1.0 / 3.0);
                float s1 = pow(s0, 1.0 / 3.0);

                return float3
                (
                    0.2104542553f * l1 + 0.7936177850f * m1 - 0.0040720468f * s1,
                    1.9779984951f * l1 - 2.4285922050f * m1 + 0.4505937099f * s1,
                    0.0259040371f * l1 + 0.7827717662f * m1 - 0.8086757660f * s1
                );
            }

            float3 okLab2rgb(float3 c)
            {
                float l1 = c.r + 0.3963377774f * c.g + 0.2158037573f * c.b;
                float m1 = c.r - 0.1055613458f * c.g - 0.0638541728f * c.b;
                float s1 = c.r - 0.0894841775f * c.g - 1.2914855480f * c.b;

                float l0 = l1 * l1 * l1;
                float m0 = m1 * m1 * m1;
                float s0 = s1 * s1 * s1;

                return float3
                (
                    +4.0767416621f * l0 - 3.3077115913f * m0 + 0.2309699292f * s0,
                    -1.2684380046f * l0 + 2.6097574011f * m0 - 0.3413193965f * s0,
                    -0.0041960863f * l0 - 0.7034186147f * m0 + 1.7076147010f * s0
                );
            }

            float smootherstep(float x)
            {
                return x * x * x * (x * (6.0f * x - 15.0f) + 10.0f);
            }

            int _FalseColor;
            float _Slope;
            int _Downscale;
            float3 _BluePoint;
            float _Passthrough;

            float sampleDepth(float2 uv)
            {
                return SampleSceneDepth(uv);
            }

            float sampleOutline(float2 uv, float2 pixelSize)
            {
                float4 depth = float4(
                
                    sampleDepth(uv + float2( 0,  0) * pixelSize),
                    sampleDepth(uv + float2( 1,  1) * pixelSize),
                    sampleDepth(uv + float2( 1,  0) * pixelSize),
                    sampleDepth(uv + float2( 0,  1) * pixelSize)
                );

                float2 diff = float2(depth[1] - depth[0], depth[3] - depth[2]);
                float edge = sqrt(diff.x * diff.x + diff.y * diff.y);
                
                return edge > 0.002;
            }

            half4 frag(Varyings input) : SV_Target
            {
                clip(input.uv.x - _Passthrough);

                int2 steps = _Palette_TexelSize.zw;

                float2 uv = input.uv;
                int2 downscale = _ScreenParams.xy;

                if (_Downscale > 1)
                {
                    downscale = _ScreenParams.xy / _Downscale;
                    uv = floor(input.uv * downscale) / downscale;
                }

                float3 scene = SampleSceneColor(uv);
                float lightness = dot(scene, float3(0.299, 0.587, 0.144));
                lightness = (lightness + _Brightness) * (1 + _Contrast);
                lightness = pow(max(0.0, lightness), 1 / _Slope);

                float blueness = pow(dot(normalize(_BluePoint), normalize(scene)) * 0.5 + 0.5, 4.0);
                float2 paletteUV = float2(blueness, lightness);

                uint2 ditherUV = input.uv * (_ScreenParams.xy / _Downscale);
                float2 index;
                index.x = Unity_Dither_float(paletteUV.x * steps.x, ditherUV) / steps.x;
                index.y = Unity_Dither_float(paletteUV.y * steps.y, ditherUV) / steps.y;

                float3 col = SAMPLE_TEXTURE2D(_Palette, sampler_Palette, index);

                if (_FalseColor)
                {
                    if (paletteUV.x >= 1.0) col = float3(0, 1, 0);
                    if (paletteUV.x <= 0.0) col = float3(1, 0, 0);
                }

                float outline = sampleOutline(uv, 2.0 / downscale);
                return outline;
                col = lerp(col, 0, outline);
                
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}