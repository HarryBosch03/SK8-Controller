#include "Assets/Code/Shaders/Common.hlsl"

float4 _BaseColor;
TEX(_MainTex);
float _TexBlend;

TEX(_Normal);
float _NormalStrength;

float4 _Specular;
float _SpecExp;
            
float4 _GCool;
float4 _GHot;
