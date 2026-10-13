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
        _TerrainBlend("Use authored terrain weights", Float) = 0
        _PackedColor("Packed trail albedo", Color) = (.68,.75,.8,1)
        _IceColor("Ice albedo", Color) = (.44,.59,.67,1)
        _EdgeColor("Trail edge albedo", Color) = (.74,.79,.82,1)
        _LayerRoughness("Packed ice edge roughness", Vector) = (.58,.12,.9,0)
        _LayerNormals("Packed ice edge normal strength", Vector) = (.12,.025,.28,0)
        _GroomSpacing("Groomed groove spacing metres", Float) = .04
        _DetailAlbedo("Audited snow albedo",2D)="white"{}
        _DetailNormal("Audited snow normal",2D)="bump"{}
        _DetailRoughness("Audited snow roughness",2D)="white"{}
        _DetailWeight("Audited detail blend",Range(0,1))=0
        _DetailMeters("Detail repeat metres",Float)=4
        _DetailNormalStrength("Detail normal strength",Range(0,1))=.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_DetailAlbedo);SAMPLER(sampler_DetailAlbedo);
        TEXTURE2D(_DetailNormal);SAMPLER(sampler_DetailNormal);
        TEXTURE2D(_DetailRoughness);SAMPLER(sampler_DetailRoughness);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Roughness, _RoughnessVariation, _NormalStrength, _SlopeDarkening;
            float _AlbedoVariation, _MacroScale, _GrainScale;
            float4 _PackedColor, _IceColor, _EdgeColor, _LayerRoughness, _LayerNormals;
            float _TerrainBlend, _GroomSpacing;
            float _DetailWeight,_DetailMeters,_DetailNormalStrength;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; float4 weights:TEXCOORD3; float2 uv:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
        Varyings Vert(Attributes v)
        {
            Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(v.normalOS);
            o.fog=ComputeFogFactor(p.positionCS.z); o.weights=v.color;o.uv=v.uv;return o;
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
            float4 w=max(0,i.weights);w/=max(.0001,dot(w,1));
            w=lerp(float4(1,0,0,0),w,saturate(_TerrainBlend));
            float normalStrength=dot(w,float4(_NormalStrength,_LayerNormals.xyz));
            float roughness=dot(w,float4(_Roughness,_LayerRoughness.xyz));
            float3 albedo=_BaseColor.rgb*w.x+_PackedColor.rgb*w.y+_IceColor.rgb*w.z+_EdgeColor.rgb*w.w;
            float slope=1-saturate(n.y);
            float3 gradient=grain.yzw;
            n=normalize(n-(gradient-n*dot(gradient,n))*normalStrength*grainFade);
            if(_DetailWeight>0)
            {
                float2 detailUv=i.positionWS.xz/max(.1,_DetailMeters);
                // Continuous macro crossfade between two rotated scan projections breaks tiling.
                float2 alternateUv=float2(-detailUv.y,detailUv.x)*1.173+float2(.37,.61);
                float mixDetail=smoothstep(.2,.8,macro)*.5;
                float weight=_DetailWeight*dot(w,float4(1,.3,.08,.8));
                half3 detailColor=lerp(SAMPLE_TEXTURE2D(_DetailAlbedo,sampler_DetailAlbedo,detailUv).rgb,SAMPLE_TEXTURE2D(_DetailAlbedo,sampler_DetailAlbedo,alternateUv).rgb,mixDetail);
                // Preserve the layer palette and avoid enlarging scanned ground debris into
                // dark repeated route markings. Metre mapping is independent of the mesh UVs.
                albedo*=lerp(1,.78+.35*detailColor,weight);
                half3 detail=UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,detailUv));
                half3 alternate=UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,alternateUv));
                alternate.xy=float2(alternate.y,-alternate.x);detail=normalize(lerp(detail,alternate,mixDetail));
                // Vertical snow edges can have an exact +/-X normal. Keep the existing
                // X reference elsewhere, but use Z when its projected tangent degenerates.
                float3 tangentProjection=float3(1,0,0)-n*n.x;
                if(dot(tangentProjection,tangentProjection)<1e-4)
                    tangentProjection=float3(0,0,1)-n*n.z;
                float3 tangent=normalize(tangentProjection),bitangent=normalize(cross(tangent,n));
                float3 mapped=normalize(tangent*detail.x+bitangent*detail.y+n*detail.z);
                n=normalize(lerp(n,mapped,weight*_DetailNormalStrength));
                float detailRoughness=SAMPLE_TEXTURE2D(_DetailRoughness,sampler_DetailRoughness,detailUv).r;
                roughness=lerp(roughness,clamp(detailRoughness,.55,1),weight*.4);
            }
            SurfaceData s=(SurfaceData)0;
            float grooveFade=1-smoothstep(.3,1,fwidth(i.uv.x)/max(.005,_GroomSpacing));
            float groove=sin(i.uv.x*6.2831853/max(.005,_GroomSpacing))*.018*w.y*grooveFade;
            s.albedo=albedo*(1-_AlbedoVariation*(1-macro)-_SlopeDarkening*slope+groove);
            s.metallic=0; s.specular=0; s.smoothness=1-clamp(roughness+(macro-.5)*_RoughnessVariation*2*(1-w.z*.8),.08,1);
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
