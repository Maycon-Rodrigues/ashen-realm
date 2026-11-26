Shader "AshenRealm/Ruins Embers"
{
    Properties
    {
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _Veil("Gate Veil", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Veil;
            CBUFFER_END

            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            Varyings Vert(float3 positionOS : POSITION)
            {
                Varyings result;
                result.world = TransformObjectToWorld(positionOS);
                result.clip = TransformWorldToHClip(result.world);
                return result;
            }

            float Hash(float2 cell)
            {
                cell = frac(cell * float2(0.1031, 0.11369));
                cell += dot(cell, cell.yx + 19.19);
                return frac((cell.x + cell.y) * cell.x);
            }

            float Noise(float2 p)
            {
                float2 q = floor(p);
                float2 f = smoothstep(0.0, 1.0, frac(p));
                return lerp(lerp(Hash(q), Hash(q + float2(1, 0)), f.x),
                            lerp(Hash(q + float2(0, 1)), Hash(q + 1), f.x), f.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 drift = _Time.y * float2(0.009, -0.017);
                float2 p = input.world.xz * 0.105 + drift;
                float ridges = Noise(p) * 0.68 + Noise(p * 3.67 + 2.7) * 0.26 + Noise(p * 9.0) * 0.06;
                float ash = smoothstep(0.24, 0.74, ridges);
                float glow = ash * ash * (0.45 + 0.55 * Noise(p * 0.48));
                half3 abyss = lerp(half3(0.012, 0.003, 0.019), half3(0.38, 0.025, 0.048), glow);
                float curtains = Noise(float2(input.world.x * 5.1, input.world.y * 0.73) - drift * 8.0);
                float heat = exp(-1.5 * max(0.0, input.world.y)) * (0.35 + 0.65 * curtains);
                half3 veil = lerp(half3(0.028, 0.007, 0.021), half3(0.66, 0.044, 0.028), heat);
                return half4(lerp(abyss, veil, _Veil) * _BaseColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
