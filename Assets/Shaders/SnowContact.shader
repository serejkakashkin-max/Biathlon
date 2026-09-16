Shader "Biathlon/SnowContact"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Name "Contact" Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off Offset -1,-1
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;return o;}
            half4 Frag(V i):SV_Target
            {
                float edge=smoothstep(0,.22,i.uv.x)*smoothstep(0,.22,1-i.uv.x);
                edge*=smoothstep(0,.06,i.uv.y)*smoothstep(0,.06,1-i.uv.y);
                return half4(i.color.rgb,i.color.a*edge);
            }
            ENDHLSL
        }
    }
}
