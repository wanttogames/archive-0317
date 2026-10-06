Shader "Archive0317/CCTVImage"
{
    Properties { _MainTex("Frame",2D)="white"{} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float grey=dot(tex2D(_MainTex,i.uv).rgb,float3(.299,.587,.114));
                float noise=frac(sin(dot(floor(i.uv*float2(512,288))+floor(_Time.y*4),float2(12.9898,78.233)))*43758.5453)-.5;
                grey=saturate(grey+noise*.006-sin(i.uv.y*288*3.14159)*.0015);
                return fixed4(grey,grey,grey,1);
            }
            ENDHLSL
        }
    }
}
