Shader "Biathlon/AuditedFir"
{
 Properties
 {
  _BarkAlbedo("Audited bark",2D)="white"{} _LeafAlbedo("Audited foliage alpha",2D)="white"{}
  _BarkNormal("Bark normal",2D)="bump"{} _LeafNormal("Foliage normal",2D)="bump"{}
  _Cutoff("Alpha threshold",Range(0,1))=.4 _SnowCoverage("Winter coverage",Range(0,1))=.72
  _Wind("Time amplitude frequency",Vector)=(0,.16,.8,0) _WindDirection("Visual wind XZ",Vector)=(1,.35,0,0)
  [HideInInspector] _ForestLodEnabled("Forest blend",Float)=0
  [HideInInspector] _ForestLodLevel("Forest level",Float)=0
  [HideInInspector] _ForestLodRanges("Forest blend ranges",Vector)=(0,1,0,1)
  [HideInInspector] _ForestLodCamera("Forest view",Vector)=(0,0,0,1)
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BarkAlbedo);SAMPLER(sampler_BarkAlbedo);TEXTURE2D(_LeafAlbedo);SAMPLER(sampler_LeafAlbedo);
  TEXTURE2D(_BarkNormal);SAMPLER(sampler_BarkNormal);TEXTURE2D(_LeafNormal);SAMPLER(sampler_LeafNormal);
  CBUFFER_START(UnityPerMaterial)
  float4 _Wind,_WindDirection;float _SnowCoverage,_Cutoff;
  float4 _ForestLodCamera,_ForestLodRanges;float _ForestLodLevel,_ForestLodEnabled;
  CBUFFER_END
  struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half kind:TEXCOORD4;half fog:TEXCOORD5;float2 lod:TEXCOORD6;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A a)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   float3 root=TransformObjectToWorld(float3(0,0,0));float phase=_Wind.x*_Wind.z+dot(root.xz,float2(.31,.17));float height=saturate(a.positionOS.y/8);
   if(_ForestLodEnabled>.5)
   {
    float distance=length(root-_ForestLodCamera.xyz)*_ForestLodCamera.w;
    o.lod=float2(smoothstep(_ForestLodRanges.x,_ForestLodRanges.x+_ForestLodRanges.y,distance),smoothstep(_ForestLodRanges.z,_ForestLodRanges.z+_ForestLodRanges.w,distance));
   }
   float bend=_Wind.y*sin(phase)*height*height;
   o.positionWS=TransformObjectToWorld(a.positionOS.xyz)+float3(bend*_WindDirection.x,0,bend*_WindDirection.y);
   float3 n=TransformObjectToWorldNormal(a.normalOS);n.y-=dot(n.xz,_WindDirection.xy)*(_Wind.y*sin(phase)*2*height/8);
   o.normalWS=normalize(n);o.tangentWS=half4(TransformObjectToWorldDir(a.tangentOS.xyz),a.tangentOS.w*GetOddNegativeScale());
   o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=a.uv;o.kind=a.color.r;o.fog=ComputeFogFactor(o.positionCS.z);return o;
  }
  half4 Albedo(V i)
  {
   half4 bark=SAMPLE_TEXTURE2D(_BarkAlbedo,sampler_BarkAlbedo,i.uv);
   half4 leaf=SAMPLE_TEXTURE2D(_LeafAlbedo,sampler_LeafAlbedo,i.uv);
   return lerp(bark,leaf,step(.25,i.kind));
  }
  half3 Normal(V i)
  {
   half4 bark=SAMPLE_TEXTURE2D(_BarkNormal,sampler_BarkNormal,i.uv);
   half4 leaf=SAMPLE_TEXTURE2D(_LeafNormal,sampler_LeafNormal,i.uv);
   return UnpackNormal(lerp(bark,leaf,step(.25,i.kind)));
  }
  void ClipForestLod(V i)
  {
   if(_ForestLodEnabled<.5)return;
   // Stable interleaved gradient samples; the adjacent meshes split the same
   // coverage interval. No alpha blending, textures, per-tree state or time noise.
   float sample=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));
   if(_ForestLodLevel<.5)clip(sample-i.lod.x);
   else if(_ForestLodLevel<1.5){clip(i.lod.x-sample);clip(sample-i.lod.y);}
   else clip(i.lod.y-sample);
  }
  half4 Frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
   ClipForestLod(i);
   half4 base=Albedo(i);clip(base.a-_Cutoff);
   half3 tangent=normalize(i.tangentWS.xyz),normal=normalize(i.normalWS),bitangent=cross(normal,tangent)*i.tangentWS.w;
   half3 n=normalize(TransformTangentToWorld(Normal(i),half3x3(tangent,bitangent,normal)));
   n*=IS_FRONT_VFACE(face,1,-1);
   float foliage=step(.25,i.kind);float patch=.7+.3*sin(i.positionWS.x*2.4+i.positionWS.z*1.7)*sin(i.positionWS.y*3.1);
   float snow=foliage*_SnowCoverage*smoothstep(-.15,.6,n.y)*patch;
   SurfaceData s=(SurfaceData)0;s.albedo=lerp(base.rgb*half3(.83,.9,.91),half3(.80,.86,.91),snow);s.smoothness=.12;s.occlusion=.85;s.alpha=1;s.normalTS=half3(0,0,1);
   InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.shadowMask=1;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
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
  half Depth(V i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);ClipForestLod(i);clip(Albedo(i).a-_Cutoff);return 0;}
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
