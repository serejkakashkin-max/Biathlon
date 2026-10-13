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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half3 normal:TEXCOORD1;half3 side:TEXCOORD2;};
            V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;o.normal=TransformObjectToWorldNormal(a.normalOS);o.side=TransformObjectToWorldDir(a.tangentOS.xyz);return o;}
            half4 Frag(V i):SV_Target
            {
                float edge=smoothstep(0,.22,i.uv.x)*smoothstep(0,.22,1-i.uv.x);
                edge*=smoothstep(0,.06,i.uv.y)*smoothstep(0,.06,1-i.uv.y);
                // Shallow compacted centre with displaced snow ridges at both edges.
                float ridge=exp(-pow((i.uv.x-.18)/.07,2))+exp(-pow((i.uv.x-.82)/.07,2));
                float slope=(exp(-pow((i.uv.x-.22)/.13,2))-exp(-pow((i.uv.x-.78)/.13,2)))*.25;
                half3 n=normalize(i.normal+i.side*slope);Light key=GetMainLight();
                half illumination=saturate(dot(n,key.direction));
                half3 colour=i.color.rgb*lerp(.85,1.2,illumination)+ridge*.09;
                return half4(colour,i.color.a*edge);
            }
            ENDHLSL
        }
    }
}
