Shader "AshenRealm/Ruins Illustrated Stone"
{
    Properties
    {
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Stone Painting", 2D) = "white" {}
        _Masonry("Wall Projection", Range(0, 1)) = 0
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            half4 _BaseColor;
            float _Masonry;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 pigment : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half3 pigment : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings result;
                result.world = TransformObjectToWorld(input.positionOS.xyz);
                result.positionCS = TransformWorldToHClip(result.world);
                result.normal = TransformObjectToWorldNormal(input.normalOS);
                result.pigment = input.pigment.rgb;
                return result;
            }

            half3 Paint(float2 metres, float width)
            {
                // Mirroring keeps the edges continuous when the projection repeats.
                float2 uv = abs(frac(metres / (2.0 * width) + 0.5) * 2.0 - 1.0);
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, 0.002 + uv * 0.996).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 n = NormalizeNormalPerPixel(input.normal);
                half3 paving = Paint(input.world.xz, 5.9);
                half3 wallPaint = Paint(lerp(input.world.xy, input.world.zy,
                    smoothstep(0.25, 0.75, abs(n.x))), 3.8);
                half3 masonry = lerp(wallPaint, Paint(input.world.xz, 3.8), saturate(abs(n.y) * 1.35));
                half3 stone = lerp(paving, masonry, _Masonry);
                half3 vertexTint = lerp(input.pigment, 0.60h + input.pigment * 0.80h, _Masonry);

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                half exposed = smoothstep(0.12h, 0.48h, dot(n, sun.direction));
                half3 shadow = lerp(half3(0.31h, 0.26h, 0.43h), half3(0.46h, 0.39h, 0.55h), saturate(n.y));
                half3 light = shadow + half3(0.60h, 0.44h, 0.35h) * exposed * sun.shadowAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                for (uint index = 0; index < GetAdditionalLightsCount(); index++)
                {
                    Light torch = GetAdditionalLight(index, input.world);
                    light += torch.color * torch.distanceAttenuation * saturate(dot(n, torch.direction)) * 0.85h;
                }
                #endif
                return half4(stone * vertexTint * _BaseColor.rgb * light, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
