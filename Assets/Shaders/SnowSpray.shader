Shader "Biathlon/SnowSpray"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Name "Spray" Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
            V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 Frag(V i):SV_Target
            {
                // Small crystalline clumps, with a crisp centre instead of smoke-like disks.
                float2 p=abs(i.uv-.5);float diamond=max(p.x,p.y)+min(p.x,p.y)*.38;
                float radial=1-smoothstep(.26,.44,diamond);
                Light key=GetMainLight();half3 light=SampleSH(half3(0,1,0))+key.color*saturate(key.direction.y)*.6;
                return half4(MixFog(i.color.rgb*light,i.fog),i.color.a*radial);
            }
            ENDHLSL
        }
    }
}
