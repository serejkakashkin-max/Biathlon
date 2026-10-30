Shader "Hidden/Biathlon/LiveShootingReviewComposite"
{
    Properties { _MainTex("World",2D)="black"{} _Overlay("Native UI",2D)="black"{} }
    SubShader { Tags {"RenderPipeline"="UniversalPipeline"} Pass { ZTest Always ZWrite Off Cull Off
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        sampler2D _MainTex,_Overlay;
        struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
        V vert(appdata_img a){V o;o.p=UnityObjectToClipPos(a.vertex);o.uv=a.texcoord;return o;}
        half4 frag(V i):SV_Target{half4 world=tex2D(_MainTex,i.uv),ui=tex2D(_Overlay,i.uv);return half4(world.rgb*(1-ui.a)+ui.rgb,1);}
        ENDHLSL
    }}
}
