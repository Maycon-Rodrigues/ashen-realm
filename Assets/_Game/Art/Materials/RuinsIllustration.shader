Shader "AshenRealm/Ruins Illustration"
{
    Properties
    {
        [MainTexture] _BaseMap("Illustration", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _Waver("Flame Movement", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Waver;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings result;
                result.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                result.uv = input.uv;
                return result;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                uv.x += _Waver * uv.y * uv.y * (0.038 * sin(_Time.y * 7.0 + uv.y * 8.3)
                    + 0.021 * sin(_Time.y * 11.0 - uv.y * 5.0));
                half4 layer = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                clip(layer.a - 0.49h);
                return half4(layer.rgb, 1);
            }
            ENDHLSL
        }
    }
}
