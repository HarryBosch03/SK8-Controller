#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#define TEX(name) TEXTURE2D(name); SAMPLER(sampler ## name); float4 name ## _TexelSize
#define SAMPLE_TEX(name, uv) SAMPLE_TEXTURE2D(name, sampler ## name, uv)

float Shadows(float3 positionWS)
{
    float4 shadowCoords = TransformWorldToShadowCoord(positionWS);
    return lerp(MainLightRealtimeShadow(shadowCoords), 1, GetMainLightShadowFade(positionWS));
}