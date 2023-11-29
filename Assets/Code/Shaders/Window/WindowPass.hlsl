#include "Assets/Code/Shaders/Common.hlsl"

TEX(_Reflection);

struct Attributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 normal : NORMAL;
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 normalWS : NORMAL_WS;
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
    float attenuation;
    float3 specular;
};

Varyings vert(Attributes input)
{
    Varyings output;

    output.vertex = TransformObjectToHClip(input.vertex.xyz);
    output.uv = input.uv;
    output.normalWS = TransformObjectToWorldNormal(input.normal);

    return output;
}

void ApplyDecal(Varyings input, inout Surface surface)
{
    ApplyDecalToBaseColor(input.vertex, surface.albedo);
}

Surface InitSurface(Varyings input)
{
    Surface res;

    half4 col = 1.0;
    res.albedo = col.rgb;
    res.alpha = col.a;
    res.normal = normalize(input.normalWS);
    res.light = normalize(_MainLightPosition);

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
    lighting.ambient = unity_AmbientSky;
    lighting.color = _MainLightColor;
    lighting.attenuation = CalcAttenuation(input, surface);
    lighting.specular = 0.0;
    
    return lighting;
}

half4 frag(Varyings input) : SV_Target
{
    // Calculate Surface Data
    Surface surface = InitSurface(input);

    Lighting lighting = CalcLighting(input, surface);
    
    // Init Final Color
    half4 color = SAMPLE_TEX(_Reflection, input.vertex.xy / _ScreenSize.x);
    return color;
}