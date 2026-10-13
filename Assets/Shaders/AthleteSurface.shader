Shader "Biathlon/AthleteSurface"
{
 Properties
 {
  [MainColor] _BaseColor("Surface color",Color)=(.08,.12,.18,1)
  _KitColor("Fictional kit",Color)=(.08,.3,.45,1)
  _TrimColor("Seam trim",Color)=(.7,.8,.9,1)
  _KitWeight("Kit influence",Range(0,1))=0
  _SurfaceKind("Cloth 0 / skin 1 / lens 2 / rubber 3 / boot 4",Float)=0
  _Smoothness("Smoothness",Range(0,1))=.3
  _BumpMap("Audited micro normal",2D)="bump"{}
  _BumpScale("Normal strength",Float)=.2
  _RoughnessMap("Audited cloth roughness",2D)="white"{}
  _DetailTiling("Material repeats in UV",Float)=16
  _SkinVariation("Skin variation",Range(0,1))=.5
  [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Cull [_Cull]
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
  TEXTURE2D(_RoughnessMap);SAMPLER(sampler_RoughnessMap);
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor,_KitColor,_TrimColor;float _KitWeight,_SurfaceKind,_Smoothness,_BumpScale,_SkinVariation,_DetailTiling;
  CBUFFER_END
  struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float3 rest:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;float3 rest:TEXCOORD4;half fog:TEXCOORD5;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
  V Vert(A a)
  {
   V o=(V)0;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);
   o.tangentWS=half4(TransformObjectToWorldDir(a.tangentOS.xyz),a.tangentOS.w*GetOddNegativeScale());o.uv=a.uv;o.rest=a.rest;o.fog=ComputeFogFactor(o.positionCS.z);return o;
  }
  half4 Frag(V i):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
   float3 p=i.rest;half3 base=lerp(_BaseColor.rgb,_KitColor.rgb,_KitWeight);
   half3 n=normalize(i.normalWS),t=normalize(i.tangentWS.xyz),b=cross(n,t)*i.tangentWS.w;
   half smoothness=_Smoothness,metal=0;
   if(_SurfaceKind<.5)
   {
    // Bind-space garment seams remain attached through deformation; no world-space swimming.
    float path=lerp(.105,.212,smoothstep(1.0,1.2,p.y));
    float stripe=(1-smoothstep(.0015,.003+fwidth(p.x),abs(abs(p.x)-path)))*smoothstep(.015,.035,p.z);
    float cuff=1-smoothstep(.003,.008+fwidth(p.y),abs(p.y-.24));
    float seams=max(stripe,cuff);
    float stitches=.5+.5*sin(p.y*1900);stitches=lerp(stitches,.5,saturate(fwidth(p.y)*1900));
    base=lerp(base,_TrimColor.rgb,seams*(.25+.18*stitches));
    // Broad compression-knit panels, anchored in the mesh bind coordinates.
    float knee=exp(-pow((p.y-.49)/.085,4))*smoothstep(.09,.13,abs(p.x));
    float underarm=smoothstep(.20,.26,abs(p.x))*smoothstep(1.0,1.15,p.y)*(1-smoothstep(1.3,1.4,p.y));
    base*=1-.08*max(knee,underarm);
    half3 detail=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv*_DetailTiling),_BumpScale);
    n=normalize(t*detail.x+b*detail.y+n*detail.z);
    smoothness=_Smoothness*lerp(.65,1,saturate(1-SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,i.uv*_DetailTiling).r));
   }
   else if(_SurfaceKind<1.5)
   {
    base*=lerp(half3(.83,.77,.72),half3(1.10,1.05,1.02),_SkinVariation);
    float cheeks=exp(-pow((abs(p.x)-.056)/.031,2)-pow((p.y-1.673)/.029,2))*smoothstep(.045,.09,p.z);
    float lips=exp(-pow(p.x/.028,4)-pow((p.y-1.632)/.009,2))*smoothstep(.075,.105,p.z);
    base=lerp(base,base*half3(1.12,.76,.72),cheeks*.55);base=lerp(base,base*half3(.83,.51,.49),lips*.65);
    half3 detail=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv*_DetailTiling),_BumpScale);
    n=normalize(t*detail.x+b*detail.y+n*detail.z);smoothness+=.09*cheeks;
   }
   else if(_SurfaceKind<2.5)
   {
    float facing=saturate(dot(n,GetWorldSpaceNormalizeViewDir(i.positionWS)));
    // Coated-lens angular tint; lighting still comes from the scene's PBR lighting.
    base=lerp(half3(.18,.12,.32),half3(.12,.42,.52),facing);metal=.35;
    half3 reflected=SampleSH(reflect(-GetWorldSpaceNormalizeViewDir(i.positionWS),n));
    base=lerp(base,max(reflected,half3(.08,.1,.13)),.18+.38*pow(1-facing,3));
   }
   else
   {
    // Closed glove leather and boot polymer retain distinct, restrained highlights.
    float rib=(.5+.5*sin(p.y*720))*smoothstep(.08,.13,p.y)*(1-smoothstep(.25,.29,p.y));
    rib=lerp(rib,.5,saturate(fwidth(p.y)*720));
    base*=lerp(1,.91,rib);smoothness=_Smoothness;
    if(_SurfaceKind>3.5){smoothness*=.8;metal=.06;}
   }
   SurfaceData s=(SurfaceData)0;s.albedo=base;s.metallic=metal;s.smoothness=smoothness;s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);
   InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.shadowMask=1;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
   half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
  }
  float3 _LightDirection;
  V ShadowVert(A a){V o=Vert(a);o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,_LightDirection));
   #if UNITY_REVERSED_Z
   o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #else
   o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
   #endif
   return o;}
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
