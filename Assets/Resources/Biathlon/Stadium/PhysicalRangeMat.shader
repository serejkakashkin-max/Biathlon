Shader "Biathlon/PhysicalRangeMat"
{
    Properties { _BaseColor("Rubber colour", Color) = (0.04,0.04,0.04,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForward" }
            // The physical mat is flush with the packed apron. Bias only raster depth;
            // no geometry/athlete displacement or authoritative height changes.
            ZWrite On ZTest LEqual Cull Back Offset -1,-1
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 normal=normalize(input.normalWS);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lighting=SampleSH(normal)+light.color*saturate(dot(normal,light.direction))*light.shadowAttenuation;
                return half4(_BaseColor.rgb*lighting,1);
            }
            ENDHLSL
        }
    }
}
