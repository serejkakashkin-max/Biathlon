Shader "Biathlon/ShotImpact"
{
    Properties { _CoreColor("Lead impact",Color)=(.045,.052,.057,1) _ScuffColor("Chipped paint",Color)=(.48,.51,.53,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest+10" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor,_ScuffColor;
                float _Spacing,_StandingRadius,_ScoringRadius,_ClosedMask,_CoreRatio;
                float4 _PlateBounds;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float2 planeXY:TEXCOORD1;float fog:TEXCOORD2;};
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.planeXY=i.positionOS.xy;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 Frag(Varyings i):SV_Target
            {
                float r=length(i.uv);clip(1-r);
                clip(min(min(i.planeXY.x-_PlateBounds.x,_PlateBounds.z-i.planeXY.x),min(i.planeXY.y-_PlateBounds.y,_PlateBounds.w-i.planeXY.y)));
                int mask=(int)_ClosedMask;
                [unroll]for(int j=0;j<5;j++)
                {
                    float radius=(mask&(1<<j))!=0?_StandingRadius:_ScoringRadius;
                    clip(length(i.planeXY-float2((j-2)*_Spacing,0))-radius);
                }
                float aa=max(fwidth(r),.005);
                half3 color=lerp(_CoreColor.rgb,_ScuffColor.rgb,smoothstep(_CoreRatio-aa,_CoreRatio+aa,r));
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
