Shader "Uber"
{
    Properties
    {
        [MainTex]_MainTex ("Texture", 2D) = "white" {}
        _MainTexBlend("Texture Blend", Range(0, 1)) = 1.0
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [Toggle]_Triplanar("Triplanar", float) = 0
        _ID("ID", int) = 0
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

            #define MAIN_LIGHT_CALCULATE_SHADOWS
            #define _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

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
                float3 normalOS : NORMAL_OS;
                float3 normalWS : NORMAL_WS;
                float3 positionOS : POSITION_OS;
                float3 positionWS : POSITION_WS;
                float4 screenPos : SCREEN_POS;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.vertex = TransformObjectToHClip(input.vertex.xyz);

                output.uv = input.uv;
                output.normalOS = input.normal;
                output.normalWS = TransformObjectToWorldNormal(input.normal);

                float3 worldScale = float3
                (
                    length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)), // scale x axis
                    length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)), // scale y axis
                    length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)) // scale z axis
                );
                output.positionOS = input.vertex * worldScale;
                output.positionWS = TransformObjectToWorld(input.vertex.xyz);

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
                    input.positionOS.zy * triplanarWeights.x +
                    input.positionOS.zx * triplanarWeights.y +
                    input.positionOS.xy * triplanarWeights.z, 
                    _Triplanar
                );
                
                half4 albedo = lerp(1.0, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw), _MainTexBlend) * _BaseColor * input.color;
                half3 decal = albedo.rgb;
                ApplyDecalToBaseColor(input.vertex, decal);
                albedo.rgb = decal;

                half4 color;
                color.rgb = 0.0;
                color.a = albedo.a;

                float3 normal = normalize(input.normalWS);
                float3 light = normalize(_MainLightPosition);
                float attenuation = saturate(dot(normal, light));

                float lightmapResolution = 32;
                float4 shadowCoords = TransformWorldToShadowCoord(floor(input.positionWS * lightmapResolution) / lightmapResolution);
                attenuation *= lerp(MainLightRealtimeShadow(shadowCoords), 1, GetMainLightShadowFade(input.positionWS));
                
                color.rgb += albedo * Ambient;
                color.rgb += albedo * attenuation * _MainLightColor;

                clip(Unity_Dither_float(color.a, float4(input.screenPos.xy / input.screenPos.w, 0, 0)));

                return half4(color.rgb, 1.0);
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
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

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