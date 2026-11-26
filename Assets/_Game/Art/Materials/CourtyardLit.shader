Shader "AshenRealm/Courtyard Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Rim("Rim Strength", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // The borrowed URP shadow and depth passes have a different material layout.
            float4 _BaseColor;
            float _Rim;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = NormalizeNormalPerPixel(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light key = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half sky = saturate(normal.y * 0.5h + 0.5h);
                half3 irradiance = lerp(half3(0.20h, 0.22h, 0.26h), half3(0.52h, 0.61h, 0.76h), sky);
                irradiance += min(key.color, 1.4h) * saturate(dot(normal, key.direction))
                    * (0.15h + 0.56h * key.shadowAttenuation);

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint index = 0; index < count; index++)
                {
                    Light localLight = GetAdditionalLight(index, input.positionWS);
                    irradiance += localLight.color * saturate(dot(normal, localLight.direction))
                        * localLight.distanceAttenuation * 0.55h;
                }
                #endif

                half edge = 1.0h - saturate(dot(normal, view));
                half3 rim = half3(0.23h, 0.38h, 0.54h) * edge * edge * sky * _Rim;
                half3 result = _BaseColor.rgb * irradiance * 0.74h + rim;
                return half4(MixFog(result, input.fog), 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
