#include "Assets/Code/Shaders/Common.hlsl"

float4 _BaseColor;
float4 _EmissiveColor;
float _Brightness;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
float4 _NormalMap_ST;

float _NormalStrength;
float _Triplanar;
