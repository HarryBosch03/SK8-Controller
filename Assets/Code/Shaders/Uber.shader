Shader "Uber"
{
    Properties
    {
        [MainTex]_MainTex ("Texture", 2D) = "white" {}
        _MainTexBlend("Texture Blend", Range(0, 1)) = 1.0
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [Toggle]_Triplanar("Triplanar", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        ZWrite On
        ZTest Less

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
                float3 tangent : TANGENT;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normalOS : NORMAL_WS;
                float3 position : POSITION_WS;
                float4 screenPos : SCREEN_POS;
                float4 color : COLOR;
                float attenuation : ATTENUATION;
            };

            static const float _Jitter = 150.0;

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.vertex = TransformObjectToHClip(input.vertex.xyz);
                output.vertex.xy = (round((output.vertex.xy / output.vertex.w) * _Jitter) / _Jitter) * output.vertex.w;

                output.uv = input.uv;
                output.normalOS = input.normal;
                float3 normalWS = TransformObjectToWorldNormal(input.normal);

                float3 worldScale = float3
                (
                    length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)), // scale x axis
                    length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)), // scale y axis
                    length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)) // scale z axis
                );
                output.position = input.vertex * worldScale;

                output.attenuation = saturate(dot(normalWS, normalize(_MainLightPosition)));
                output.color = input.color;
                output.screenPos = ComputeScreenPos(output.vertex);

                return output;
            }

            static const float3 Ambient = float3(0.212, 0.227, 0.259) * 0.01;

            float4 _BaseColor;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _MainTexBlend;
            float4 _MainTex_ST;

            float _Triplanar;

            float Unity_Dither_float(float In, float4 ScreenPosition)
            {
                float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                };
                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                return In - DITHER_THRESHOLDS[index];
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 triplanarWeights = abs(normalize(input.normalOS));
                float2 uv = lerp
                (
                    input.uv,
                    input.position.zy * triplanarWeights.x +
                    input.position.zx * triplanarWeights.y +
                    input.position.xy * triplanarWeights.z, 
                    _Triplanar
                );
                
                half4 albedo = lerp(1.0, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw), _MainTexBlend) * _BaseColor * input.color;
                half3 decal = albedo.rgb;
                ApplyDecalToBaseColor(input.vertex, decal);
                albedo.rgb = decal;

                half4 color;
                color.rgb = 0.0;
                color.a = albedo.a;
                
                color.rgb += albedo * Ambient;
                color.rgb += albedo * input.attenuation * _MainLightColor;

                clip(Unity_Dither_float(color.a, float4(input.screenPos.xy / input.screenPos.w, 0, 0)));

                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

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
        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}