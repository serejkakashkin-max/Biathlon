Shader "Biathlon/Spectator"
{
 Properties {_Motion("Time amplitude frequency LOD",Vector)=(0,.9,2.4,0)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  float4 _Motion;
  CBUFFER_END
  struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float4 pivot:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half3 color:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A v)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   float3 root=TransformObjectToWorld(float3(0,0,0));float rank=frac(sin(dot(root,float3(12.9898,19.37,78.233)))*43758.5453);float phase=rank*6.2831853;
   float cheer=smoothstep(.35,.85,sin(_Motion.x*.32+phase));
   float angle=v.pivot.w*(1-_Motion.w)*_Motion.y*(.15+cheer*(.55+.3*sin(_Motion.x*_Motion.z+phase+sign(v.pivot.x)*.35)));
   float sn=sin(angle),cs=cos(angle);float3 p=v.positionOS.xyz-v.pivot.xyz,n=v.normalOS;
   p.yz=float2(cs*p.y-sn*p.z,sn*p.y+cs*p.z);n.yz=float2(cs*n.y-sn*n.z,sn*n.y+cs*n.z);p+=v.pivot.xyz;
   half3 coat=rank<.2?half3(.42,.07,.075):rank<.4?half3(.06,.19,.38):rank<.6?half3(.55,.34,.10):rank<.8?half3(.66,.61,.51):half3(.10,.32,.23);
   o.color=lerp(v.color.rgb,v.color.rgb*coat,v.color.a);o.positionWS=TransformObjectToWorld(p);o.normalWS=TransformObjectToWorldNormal(n);o.positionCS=TransformWorldToHClip(o.positionWS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
  }
  half4 Frag(V i):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half3 n=normalize(i.normalWS);
   SurfaceData s=(SurfaceData)0;s.albedo=i.color;s.smoothness=.12;s.occlusion=.9;s.alpha=1;s.normalTS=half3(0,0,1);
   InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.shadowMask=1;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
   half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
  }
  float3 _LightDirection;
  V ShadowVert(A v){V o=Vert(v);o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,_LightDirection));
   #if UNITY_REVERSED_Z
   o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #else
   o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #endif
   return o;}
  half Depth(V i):SV_Target{return i.positionCS.z;}
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
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Depth
   #pragma multi_compile_instancing
   ENDHLSL
  }
 }
}
