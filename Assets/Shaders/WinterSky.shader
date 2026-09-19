Shader "Biathlon/WinterSky"
{
 Properties { _SkyColor("Sky",Color)=(.2,.36,.56,1) _HorizonColor("Horizon",Color)=(.57,.67,.76,1) _CloudCover("Cloud cover",Range(0,1))=.5 _Drift("Wind and explicit time",Vector)=(0,0,0,0) }
 SubShader
 {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _SkyColor,_HorizonColor,_Drift;float _CloudCover;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;};struct V {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
   V Vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
   half4 Frag(V i):SV_Target
   {
    float3 d=normalize(i.direction);float horizon=saturate(d.y*2);
    float2 p=d.xz/max(.18,d.y)*2-_Drift.xy*_Drift.z*.0005;
    float n=(Noise(p)+.5*Noise(p*2.03)+.25*Noise(p*4.07))/1.75;
    float clouds=smoothstep(1-_CloudCover,1-_CloudCover+.22,n)*smoothstep(0,.18,d.y);
    float3 base=lerp(_HorizonColor.rgb,_SkyColor.rgb,horizon);
    float3 cloud=_HorizonColor.rgb*(.9+.22*n);
    return half4(lerp(base,cloud,clouds),1);
   }
   ENDHLSL
  }
 }
}
