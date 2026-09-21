Shader "Biathlon/AuditedRock"
{
 Properties
 {
  _BaseMap("Audited rock albedo",2D)="white"{}
  _BumpMap("Audited rock normal",2D)="bump"{}
  _RoughnessMap("Audited rock roughness",2D)="white"{}
  _SnowColor("Snow albedo",Color)=(.83,.88,.93,1)
  _SnowCoverage("Snow coverage",Range(0,1))=.85
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
  TEXTURE2D(_RoughnessMap);SAMPLER(sampler_RoughnessMap);
  CBUFFER_START(UnityPerMaterial)
   float4 _SnowColor;float _SnowCoverage;
  CBUFFER_END
  struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half fog:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A a)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
   o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.tangentWS=half4(TransformObjectToWorldDir(a.tangentOS.xyz),a.tangentOS.w*GetOddNegativeScale());
   o.uv=a.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
  }
  half4 Frag(V i):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
   half3 normal=normalize(i.normalWS),tangent=normalize(i.tangentWS.xyz),bitangent=cross(normal,tangent)*i.tangentWS.w;
   half3 mapped=normalize(TransformTangentToWorld(UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv)),half3x3(tangent,bitangent,normal)));
   float patch=sin(i.positionWS.x*5.7+i.positionWS.z*2.1)*sin(i.positionWS.z*4.3-i.positionWS.x*1.9);
   float snow=_SnowCoverage*smoothstep(.12,.65,normal.y+patch*.14);
   half3 n=normalize(lerp(mapped,normal,snow*.8));
   SurfaceData s=(SurfaceData)0;
   s.albedo=lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb,_SnowColor.rgb,snow);
   s.smoothness=1-lerp(clamp(SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,i.uv).r,.4,1),.88,snow);
   s.alpha=1;s.occlusion=1;s.normalTS=half3(0,0,1);
   InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
   d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.shadowMask=1;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
   half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
  }
  float3 _LightDirection,_LightPosition;
  V ShadowVert(A a)
  {
   V o=Vert(a);float3 direction=_LightDirection;
   #if _CASTING_PUNCTUAL_LIGHT_SHADOW
   direction=normalize(_LightPosition-o.positionWS);
   #endif
   o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction));
   #if UNITY_REVERSED_Z
   o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #else
   o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #endif
   return o;
  }
  half Depth(V i):SV_Target {return 0;}
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
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
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
