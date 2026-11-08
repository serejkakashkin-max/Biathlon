Shader "Biathlon/AthleteBib"
{
 Properties
 {
  [MainColor] _BaseColor("Fabric",Color)=(.88,.91,.86,1)
  _SecondaryColor("Combined leader fabric",Color)=(.88,.91,.86,1)
  _ColorSplit("Combined leader bib",Range(0,1))=0
  _InkColor("Printed ink",Color)=(.015,.045,.07,1)
  _Atlas("Original number atlas",2D)="white"{}
  _BibNumber("Registered entry ordinal",Float)=1
  _HeroPages("Front/back UV pages",Float)=0
  _BumpMap("Audited fabric normal",2D)="bump"{}
  _RoughnessMap("Audited fabric roughness",2D)="white"{}
  _BumpScale("Normal strength",Float)=.3
  _DetailTiling("Material repeats in UV",Float)=6
  _Smoothness("Minimum smoothness",Range(0,1))=.12
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "VenueIllumination.hlsl"
  TEXTURE2D(_Atlas);SAMPLER(sampler_Atlas);
  TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
  TEXTURE2D(_RoughnessMap);SAMPLER(sampler_RoughnessMap);
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor,_SecondaryColor,_InkColor;float _ColorSplit,_BibNumber,_BumpScale,_DetailTiling,_Smoothness,_HeroPages;
  CBUFFER_END
  struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;half4 tangentWS:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A a)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
   o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.tangentWS=half4(TransformObjectToWorldDir(a.tangentOS.xyz),a.tangentOS.w*GetOddNegativeScale());o.uv=a.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
  }
  half Glyph(float2 uv,float digit)
  {
   float2 cell=float2(fmod(digit,5),digit<5?2:1);
   return SAMPLE_TEXTURE2D(_Atlas,sampler_Atlas,(cell+saturate(uv))/float2(5,3)).r;
  }
  half4 Frag(V i):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
   half white=1;float2 uv=i.uv;float number=clamp(round(_BibNumber),1,60);
   if(_HeroPages>.5)uv.x=frac(uv.x*2);
   if(uv.y>.22&&uv.y<.73&&uv.x>.22&&uv.x<.78)
   {
    float2 local=(uv-float2(.22,.22))/float2(.56,.51);
    float digit=local.x<.5?floor(number/10):fmod(number,10);
    white=Glyph(float2(frac(local.x*2),local.y),digit);
   }
   if(uv.y>.78&&uv.y<.93&&uv.x>.18&&uv.x<.82)
    white=SAMPLE_TEXTURE2D(_Atlas,sampler_Atlas,float2((uv.x-.18)/.64,(uv.y-.78)/.15/3)).r;
   // Mip-filtered source fabric replaces the aliased, screen-frequency sine weave.
   half3 normal=normalize(i.normalWS),tangent=normalize(i.tangentWS.xyz);
   half3 detail=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv*_DetailTiling),_BumpScale);
   normal=normalize(tangent*detail.x+cross(normal,tangent)*i.tangentWS.w*detail.y+normal*detail.z);
   half roughness=SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,uv*_DetailTiling).r;
   half3 fabric=lerp(_BaseColor.rgb,_SecondaryColor.rgb,step(.5,uv.x)*_ColorSplit);
   SurfaceData s=(SurfaceData)0;s.albedo=lerp(_InkColor.rgb,fabric,white);s.smoothness=saturate(1-roughness)*(1-_Smoothness)+_Smoothness;s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);
   s.emission=s.albedo*VenueIllumination(i.positionWS,normal);
            InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=normal;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
   d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(d.normalWS);d.shadowMask=1;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
   half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
  }
  float3 _LightDirection;
  V ShadowVert(A a)
  {
   V o=Vert(a);o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,_LightDirection));
   #if UNITY_REVERSED_Z
   o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #else
   o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #endif
   return o;
  }
  half Depth(V i):SV_Target{return 0;}
  ENDHLSL
  Pass
  {
   Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex ShadowVert
   #pragma fragment Depth
   #pragma multi_compile_instancing
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Depth
   #pragma multi_compile_instancing
   ENDHLSL
  }
 }
}
