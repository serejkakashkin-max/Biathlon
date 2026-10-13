Shader "Biathlon/WinterWood"
{
 Properties{_Bark("Audited fir bark",2D)="white"{} _Normal("Bark normal",2D)="bump"{} }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Tags{"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Bark);SAMPLER(sampler_Bark);TEXTURE2D(_Normal);SAMPLER(sampler_Normal);
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID};
   V Vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.n=TransformObjectToWorldNormal(a.normalOS);o.t=float4(TransformObjectToWorldDir(a.tangentOS.xyz),a.tangentOS.w);o.uv=a.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);float3 n=normalize(i.n);half snow=smoothstep(.35,.75,n.y);
    half3 bark=SAMPLE_TEXTURE2D(_Bark,sampler_Bark,i.uv).rgb;
    half3 detail=UnpackNormal(SAMPLE_TEXTURE2D(_Normal,sampler_Normal,i.uv));
    n=normalize(n+(.25*(normalize(i.t.xyz)*detail.x+cross(n,i.t.xyz)*detail.y))*(1-snow));
    SurfaceData s=(SurfaceData)0;s.albedo=lerp(bark,half3(.88,.9,.93),snow);s.smoothness=.12;s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);
    InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.shadowMask=1;
    half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
