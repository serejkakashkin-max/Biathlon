Shader "Biathlon/Snow"
{
    Properties
    {
        _BaseColor("Snow albedo", Color) = (0.83,0.88,0.93,1)
        _Roughness("Roughness", Range(0.08,1)) = 0.8
        _RoughnessVariation("Roughness variation", Range(0,1)) = 0.12
        _NormalStrength("Grain normal strength", Range(0,1)) = 0.24
        _SlopeDarkening("Slope darkening", Range(0,0.5)) = 0.13
        _AlbedoVariation("Albedo variation", Range(0,0.3)) = 0.08
        _MacroScale("Macro scale metres", Float) = 1.7
        _GrainScale("Grain scale metres", Float) = 0.045
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Roughness, _RoughnessVariation, _NormalStrength, _SlopeDarkening;
            float _AlbedoVariation, _MacroScale, _GrainScale;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
        Varyings Vert(Attributes v)
        {
            Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(v.normalOS);
            o.fog=ComputeFogFactor(p.positionCS.z); return o;
        }
        // Repository-authored value noise with analytic derivatives; world units prevent UV stretching.
        float Hash(float3 p) { float3 q=frac(p*0.1031); q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z); }
        float4 Noise(float3 p)
        {
            float3 i=floor(p), f=frac(p), u=f*f*(3-2*f), du=6*f*(1-f);
            float a=Hash(i), b=Hash(i+float3(1,0,0)), c=Hash(i+float3(0,1,0)), d=Hash(i+float3(1,1,0));
            float e=Hash(i+float3(0,0,1)), f1=Hash(i+float3(1,0,1)), g=Hash(i+float3(0,1,1)), h=Hash(i+1);
            float k1=b-a,k2=c-a,k3=e-a,k4=a-b-c+d,k5=a-c-e+g,k6=a-b-e+f1,k7=-a+b+c-d+e-f1-g+h;
            float value=a+k1*u.x+k2*u.y+k3*u.z+k4*u.x*u.y+k5*u.y*u.z+k6*u.z*u.x+k7*u.x*u.y*u.z;
            return float4(value,du*float3(k1+k4*u.y+k6*u.z+k7*u.y*u.z,k2+k4*u.x+k5*u.z+k7*u.x*u.z,k3+k5*u.y+k6*u.x+k7*u.x*u.y));
        }
        half4 Frag(Varyings i):SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            float3 uv=i.positionWS;
            float macro=Noise(uv/max(.001,_MacroScale)).x;
            float4 grain=Noise(uv/max(.001,_GrainScale));
            float footprint=max(length(ddx(uv)),length(ddy(uv)))/max(.001,_GrainScale);
            float grainFade=1-smoothstep(.35,1,footprint);
            float3 n=normalize(i.normalWS);
            float slope=1-saturate(n.y);
            float3 gradient=grain.yzw;
            n=normalize(n-(gradient-n*dot(gradient,n))*_NormalStrength*grainFade);
            SurfaceData s=(SurfaceData)0;
            s.albedo=_BaseColor.rgb*(1-_AlbedoVariation*(1-macro)-_SlopeDarkening*slope);
            s.metallic=0; s.specular=0; s.smoothness=1-clamp(_Roughness+(macro-.5)*_RoughnessVariation*2,.08,1);
            s.occlusion=1; s.alpha=1; s.normalTS=half3(0,0,1);
            InputData input=(InputData)0;
            input.positionWS=i.positionWS; input.normalWS=n; input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
            input.shadowCoord=TransformWorldToShadowCoord(i.positionWS); input.bakedGI=SampleSH(n);
            input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS); input.shadowMask=1;
            input.vertexLighting=VertexLighting(i.positionWS,n);
            half4 color=UniversalFragmentPBR(input,s); color.rgb=MixFog(color.rgb,i.fog); return color;
        }
        float3 _LightDirection, _LightPosition;
        Varyings ShadowVert(Attributes v)
        {
            Varyings o=Vert(v);
            float3 direction=_LightDirection;
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
        half DepthFrag(Varyings i):SV_Target { return i.positionCS.z; }
        half4 NormalFrag(Varyings i):SV_Target
        {
            float3 n=normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
            return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n)*.5+.5)),0);
            #else
            return half4(n,0);
            #endif
        }
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags {"LightMode"="DepthNormals"} ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
}
