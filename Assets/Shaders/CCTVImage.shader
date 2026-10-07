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
                float scanRow=floor(i.uv.y*288);
                float time=floor(_Time.y*4);
                float drift=sin(scanRow*.15+time*1.7)*.00065;
                float tracking=step(.985,frac(sin(time*4.13)*437.58))*exp(-abs(i.uv.y-frac(time*.173))*120)*.002;
                float2 uv=float2(saturate(i.uv.x+drift+tracking),i.uv.y);
                float3 rgb=tex2D(_MainTex,uv).rgb;
                rgb.r=lerp(rgb.r,tex2D(_MainTex,uv+float2(.0012,0)).r,.18);
                rgb.b=lerp(rgb.b,tex2D(_MainTex,uv-float2(.0012,0)).b,.18);
                float grey=dot(rgb,float3(.299,.587,.114));
                float noise=frac(sin(dot(floor(i.uv*float2(512,288))+floor(_Time.y*4),float2(12.9898,78.233)))*43758.5453)-.5;
                float scan=1-step(.5,frac(i.uv.y*144))*.045;
                float3 color=lerp(grey.xxx,rgb,.025)*scan+noise*.006*smoothstep(.015,.15,grey);
                return fixed4(saturate(color),1);
            }
            ENDHLSL
        }
    }
}
