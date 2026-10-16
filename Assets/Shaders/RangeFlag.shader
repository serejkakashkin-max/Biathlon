Shader "Biathlon/RangeFlag"
{
    Properties { [MainTexture] _BaseMap("National pattern",2D)="white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _FlagWind;
        float _FlagTime,_FlagPhase;
        CBUFFER_END
        struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
        struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;float fog:TEXCOORD3;};
        float3 Cloth(float3 p)
        {
            float u=p.x,s=_FlagWind.x;
            float phase=u*8-_FlagTime*_FlagWind.z*6.283185+_FlagPhase;
            p.x*=lerp(.18,1,sqrt(s));
            p.y-=u*u*(1-s)*.75;
            p.z+=u*u*_FlagWind.y*s*(sin(phase)+.23*sin(phase*1.73+p.y*2));
            return p;
        }
        V Vert(A a)
        {
            V o;float3 p=Cloth(a.positionOS.xyz);
            float3 dx=Cloth(a.positionOS.xyz+float3(.001,0,0))-p;
            float3 dy=Cloth(a.positionOS.xyz+float3(0,.001,0))-p;
            o.normalWS=TransformObjectToWorldNormal(normalize(cross(dx,dy)));
            o.positionWS=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=a.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            half4 Frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                float3 normal=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                float diffuse=.18+.82*saturate(dot(normal,light.direction));
                half3 color=base*(SampleSH(normal)+light.color*diffuse*lerp(.35,1,light.shadowAttenuation));
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment Shadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            V ShadowVert(A a)
            {
                V o=Vert(a);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction=normalize(_LightPosition-o.positionWS);
                #else
                    float3 direction=_LightDirection;
                #endif
                float3 normal=o.normalWS*(dot(o.normalWS,direction)>=0?1:-1);
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,normal,direction));
                #if UNITY_REVERSED_Z
                    o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
                #else
                    o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
                #endif
                return o;
            }
            half4 Shadow(V i):SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            half4 Depth(V i):SV_Target{return 0;}
            ENDHLSL
        }
    }
}
