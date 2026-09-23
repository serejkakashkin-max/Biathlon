Shader "Biathlon/AuditedSpectator"
{
 Properties {_FaceMap("Local licensed face",2D)="white"{} _Motion("Time amplitude frequency LOD",Vector)=(0,.9,2.4,0)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  float4 _Motion;
  CBUFFER_END
  TEXTURE2D(_FaceMap);SAMPLER(sampler_FaceMap);
  struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float4 pivot:TEXCOORD0;float3 face:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half3 color:TEXCOORD2;half fog:TEXCOORD3;float3 face:TEXCOORD4;float4 cloth:TEXCOORD5;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A v)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   float3 root=TransformObjectToWorld(float3(0,0,0));float rank=frac(sin(dot(root,float3(12.9898,19.37,78.233)))*43758.5453);float phase=rank*6.2831853;
   float cheer=smoothstep(.35,.85,sin(_Motion.x*.32+phase));
   float active=(1-_Motion.w)*_Motion.y,side=sign(v.pivot.x);
   float angle=saturate(v.pivot.w)*active*(.08+cheer*(.45+.22*sin(_Motion.x*_Motion.z+phase+side*.65)));
   float3 p=v.positionOS.xyz,n=v.normalOS;
   // Forearms bend around their own elbow before the whole arm moves at the shoulder.
   float elbow=saturate(v.pivot.w-1)*active*cheer*(.28+.22*sin(_Motion.x*_Motion.z*1.3+phase+side));
   float sn=sin(elbow),cs=cos(elbow);p-=v.pivot.xyz;
   p.yz=float2(cs*p.y-sn*p.z,sn*p.y+cs*p.z);n.yz=float2(cs*n.y-sn*n.z,sn*n.y+cs*n.z);p+=v.pivot.xyz;
   float3 shoulder=float3(side*.20,.96,0);p-=shoulder;sn=sin(angle);cs=cos(angle);
   p.yz=float2(cs*p.y-sn*p.z,sn*p.y+cs*p.z);n.yz=float2(cs*n.y-sn*n.z,sn*n.y+cs*n.z);p+=shoulder;
   // Head and hat turn together; the collar boundary fades to zero displacement.
   float head=smoothstep(1.08,1.18,v.positionOS.y)*(.12*sin(phase)+active*.06*sin(_Motion.x*.45+phase));
   sn=sin(head);cs=cos(head);p.xz=float2(cs*p.x-sn*p.z,sn*p.x+cs*p.z);n.xz=float2(cs*n.x-sn*n.z,sn*n.x+cs*n.z);
   float width=.94+.10*rank;p.x*=width;n.x/=width;
   half3 coat=rank<.2?half3(.42,.07,.075):rank<.4?half3(.06,.19,.38):rank<.6?half3(.55,.34,.10):rank<.8?half3(.66,.61,.51):half3(.10,.32,.23);
   o.cloth=float4(v.positionOS.xyz,v.color.a);o.face=v.face;o.color=lerp(v.color.rgb,v.color.rgb*coat,v.color.a);o.positionWS=TransformObjectToWorld(p);o.normalWS=TransformObjectToWorldNormal(n);o.positionCS=TransformWorldToHClip(o.positionWS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
  }
  half4 Frag(V i):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half3 n=normalize(i.normalWS);
   float seam=1-smoothstep(.025,.025+max(fwidth(i.cloth.y*12),.035),abs(frac(i.cloth.y*12)-.5));
   float torso=step(.4,i.cloth.y)*step(i.cloth.y,1.015)*step(i.cloth.z,-.11)*i.cloth.w;
   float zip=(1-smoothstep(.003,.006+fwidth(i.cloth.x),abs(i.cloth.x)))*torso;
   half3 fabric=i.color*lerp(1,.68,seam*i.cloth.w);fabric=lerp(fabric,half3(.1,.12,.13),zip);
   SurfaceData s=(SurfaceData)0;s.albedo=lerp(fabric,SAMPLE_TEXTURE2D(_FaceMap,sampler_FaceMap,i.face.xy).rgb,saturate(i.face.z));s.smoothness=.12;s.occlusion=.9;s.alpha=1;s.normalTS=half3(0,0,1);
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
