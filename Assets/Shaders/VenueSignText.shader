Shader "Biathlon/VenueSignText"
{
 Properties {_MainTex("Font atlas",2D)="white"{}}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Name "SignText" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
   V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.vertex.xyz);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target{return half4(MixFog(i.color.rgb,i.fog),i.color.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a);}
   ENDHLSL
  }
 }
}
