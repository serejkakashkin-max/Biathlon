Shader "Biathlon/ProneTargetFace"
{
    Properties
    {
        _BaseColor ("Outer black aiming mark", Color) = (0.015,0.02,0.024,1)
        _CenterColor ("Recessed scoring aperture", Color) = (0.003,0.004,0.005,1)
        _RimColor ("Visible aperture boundary", Color) = (0.24,0.26,0.27,1)
        _RimWidth ("Boundary width / outer diameter", Range(0.002,0.03)) = 0.012
        _ScoringRadius ("Scoring radius / outer diameter", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _CenterColor, _RimColor;
                float _RimWidth, _ScoringRadius;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 face : TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.face=i.positionOS.xy; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float r=length(i.face), aa=max(fwidth(r),0.0001);
                float inside=1-smoothstep(_ScoringRadius-aa,_ScoringRadius+aa,r);
                // The rim lies OUTSIDE the scoring radius; the dark centre remains the true aperture.
                float rim=smoothstep(_ScoringRadius-aa,_ScoringRadius+aa,r)
                    *(1-smoothstep(_ScoringRadius+_RimWidth-aa,_ScoringRadius+_RimWidth+aa,r));
                return half4(lerp(lerp(_BaseColor.rgb,_CenterColor.rgb,inside),_RimColor.rgb,rim),1);
            }
            ENDHLSL
        }
    }
}
