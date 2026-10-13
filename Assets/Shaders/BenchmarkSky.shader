Shader "Biathlon/BenchmarkSky"
{
 Properties
 {
  _Panorama("Audited upper sky",2D)="white"{} _PanoramaExposure("Panorama exposure",Range(0,2))=.65
  _SkyColor("Sky",Color)=(.2,.36,.56,1) _HorizonColor("Horizon",Color)=(.57,.67,.76,1)
  _CloudCover("Cloud cover",Range(0,1))=.5 _Drift("Wind and explicit time",Vector)=(0,0,0,0)
  _SunDirection("Observed directional light",Vector)=(0,1,0,0)
 }
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
   TEXTURE2D(_Panorama);SAMPLER(sampler_Panorama);
   CBUFFER_START(UnityPerMaterial)
   float4 _SkyColor,_HorizonColor,_Drift,_SunDirection;float _CloudCover,_PanoramaExposure;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
   V Vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.direction=v.positionOS.xyz;return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
   half4 Frag(V i):SV_Target
   {
    float3 d=normalize(i.direction);float2 uv=float2(atan2(d.z,d.x)/6.2831853+.5,asin(clamp(d.y,-1,1))/3.14159265+.5);
    // Map the low view direction into the photograph's upper sky, never its trees/ground.
    float2 skyUv=float2(uv.x,lerp(.66,1,saturate(d.y)));
    float3 photo=SAMPLE_TEXTURE2D(_Panorama,sampler_Panorama,skyUv).rgb*_PanoramaExposure;
    // The photographed horizon/ground never appears in the playable venue.
    float3 clearSky=lerp(_HorizonColor.rgb,photo*_SkyColor.rgb/float3(.19,.36,.56),smoothstep(.04,.32,d.y));
    float2 p=d.xz/max(.18,d.y)*2-_Drift.xy*_Drift.z*.0005;
    float n=(Noise(p)+.5*Noise(p*2.03)+.25*Noise(p*4.07))/1.75;
    float clouds=smoothstep(1-_CloudCover,1-_CloudCover+.2,n)*smoothstep(0,.18,d.y);
    float3 overcast=lerp(_HorizonColor.rgb,_SkyColor.rgb,saturate(d.y))*(.96+.08*n);
    float3 sky=lerp(clearSky,overcast,saturate(_CloudCover*.7+clouds));
    float alignment=saturate(dot(d,normalize(_SunDirection.xyz)));float visibility=(1-_CloudCover)*(1-_CloudCover)*smoothstep(0,.06,d.y);
    float sunDisc=smoothstep(.99994,.99998,alignment);float halo=pow(alignment,250)*.07;
    return half4(sky+(sunDisc*2+halo)*visibility*float3(1,.95,.86),1);
   }
   ENDHLSL
  }
 }
}
