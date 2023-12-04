#include "UberInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 color : COLOR;
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 normalWS : NORMAL_WS;
    float3 tangentWS : TANGENT_WS;
    float3 positionWS : POSITION_WS;
    float4 color : COLOR;
};

struct Surface
{
    half3 albedo;
    half alpha;
    float3 normal;
    float3 light;
};

struct Lighting
{
    float3 ambient;
    float3 color;
    float3 specular;
};

Varyings vert(Attributes input)
{
    Varyings output;

    output.vertex = TransformObjectToHClip(input.vertex.xyz);
    output.uv = input.uv;
    output.normalWS = TransformObjectToWorldNormal(input.normal);
    output.tangentWS = TransformObjectToWorldNormal(input.tangent);
    output.color = input.color;
    output.positionWS = TransformObjectToWorld(input.vertex.xyz);

    return output;
}

void ApplyDecal(Varyings input, inout Surface surface)
{
    ApplyDecalToBaseColor(input.vertex, surface.albedo);
}

half2 CalcUV(Varyings varyings, float4 tex_st)
{
    float3 weights = abs(varyings.normalWS);
    return lerp
    (
        varyings.uv,
        varyings.positionWS.zy * weights.x +
        varyings.positionWS.xz * weights.y +
        varyings.positionWS.xy * weights.z,
        _Triplanar
    ) * tex_st.xy + tex_st.zw;
}

half4 SampleTexture(TEXTURE2D_PARAM(tex, sampler_tex), float4 tex_st, Varyings varyings)
{
    return SAMPLE_TEXTURE2D(tex, sampler_tex, CalcUV(varyings, tex_st));
}

Surface InitSurface(Varyings input)
{
    Surface res;

    half4 col = _BaseColor * input.color * SampleTexture(_MainTex, sampler_MainTex, _MainTex_ST, input);
    res.albedo = col.rgb;
    res.alpha = col.a;
    res.light = normalize(_MainLightPosition);

    float3 normalTS = UnpackNormalScale(SampleTexture(_NormalMap, sampler_NormalMap, _NormalMap_ST, input), _NormalStrength);
    res.normal = TransformTangentToWorld(normalTS, CreateTangentToWorld(input.normalWS, input.tangentWS, 1.0f));
    
    ApplyDecal(input, res);
    
    return res;
}

float CalcAttenuation(Varyings input, Surface surface)
{
    float attenuation = saturate(dot(surface.normal, surface.light));
    return attenuation;
}

Lighting CalcLighting(Varyings input, Surface surface)
{
    Lighting lighting;
    lighting.ambient = float4(0.1, 0.1, 0.1, 1.0);
    
    lighting.color = _MainLightColor * (CalcAttenuation(input, surface) * 0.5 + 0.5);
    lighting.specular = 0.0;

    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, input.positionWS);
        lighting.color += light.color * light.distanceAttenuation;
    }
    
    return lighting;
}

half4 frag(Varyings input) : SV_Target
{
    input.normalWS = normalize(input.normalWS);
    input.tangentWS = normalize(input.tangentWS);
    
    // Calculate Surface Data
    Surface surface = InitSurface(input);

    Lighting lighting = CalcLighting(input, surface);
    
    // Init Final Color
    half4 color = 0.0;

    // Apply Lighting
    color.rgb += surface.albedo * lighting.color;
    color.a = surface.alpha;

    color += _EmissiveColor * input.color * _Brightness;
    
    return color;
}