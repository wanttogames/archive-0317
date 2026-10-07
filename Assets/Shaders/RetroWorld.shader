Shader "Archive0317/RetroWorld"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _PixelGrid, _RetroTone, _RetroTint;
            float Hash(float2 p){return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453);}
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 cell=floor(input.texcoord*_PixelGrid.xy);
                float2 pixelUV=(cell+.5)/_PixelGrid.xy;
                float2 uv=lerp(input.texcoord,pixelUV,_PixelGrid.z);
                half3 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv).rgb;
                half luminance=dot(color,half3(.2126,.7152,.0722));
                color=lerp(luminance.xxx,color,_RetroTone.y)*_RetroTint.rgb;
                float pattern=(fmod(cell.x,2)*2+fmod(cell.y,2))/4-.375;
                float visibility=smoothstep(.015,.12,luminance);
                half3 stepped=floor(color*96+.5+pattern*_PixelGrid.w)/96;
                color=lerp(color,stepped,_PixelGrid.w*visibility);
                color+=(Hash(cell+floor(_Time.y*24))-.5)*_RetroTone.x*visibility;
                return half4(max(color,0),1);
            }
            ENDHLSL
        }
    }
}
