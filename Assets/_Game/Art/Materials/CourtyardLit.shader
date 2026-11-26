Shader "AshenRealm/Courtyard Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Rim("Rim Strength", Range(0, 1)) = 0.18
        _Grain("Paint Variation", Range(0, 1)) = 0.18
        _Polish("Metal Sheen", Range(0, 1)) = 0.0
        _VertexPaint("Vertex Paint", Range(0, 1)) = 0.0
        [HideInInspector] _HitFlash("Hit Flash", Range(0, 1)) = 0.0
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
            float _Grain;
            float _Polish;
            float _VertexPaint;
            float _HitFlash;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
                float3 pigmentPosition : TEXCOORD3;
                half3 paint : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(position.positionCS.z);
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float3 scale = float3(length(objectToWorld._m00_m10_m20),
                    length(objectToWorld._m01_m11_m21), length(objectToWorld._m02_m12_m22));
                output.pigmentPosition = input.positionOS.xyz * scale;
                output.paint = input.color.rgb;
                return output;
            }

            float Pigment(float3 position)
            {
                position = frac(position * 0.1031);
                position += dot(position, position.yzx + 33.33);
                return frac((position.x + position.y) * position.z);
            }

            float PaintNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 f = frac(position);
                f = f * f * (3.0 - 2.0 * f);
                float nearPlane = lerp(
                    lerp(Pigment(cell), Pigment(cell + float3(1, 0, 0)), f.x),
                    lerp(Pigment(cell + float3(0, 1, 0)), Pigment(cell + float3(1, 1, 0)), f.x), f.y);
                float farPlane = lerp(
                    lerp(Pigment(cell + float3(0, 0, 1)), Pigment(cell + float3(1, 0, 1)), f.x),
                    lerp(Pigment(cell + float3(0, 1, 1)), Pigment(cell + 1), f.x), f.y);
                return lerp(nearPlane, farPlane, f.z);
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
                float3 p = input.pigmentPosition;
                half pigment = PaintNoise(p * float3(3.1, 7.3, 4.7)) * 0.72h
                    + PaintNoise(p * 17.0) * 0.28h;
                half3 albedo = _BaseColor.rgb * (1.0h + (pigment - 0.5h) * _Grain);
                albedo *= lerp(half3(1.0h, 1.0h, 1.0h), input.paint, _VertexPaint);
                half3 halfDirection = SafeNormalize(view + key.direction);
                half sheen = pow(saturate(dot(normal, halfDirection)), 24.0h)
                    * _Polish * saturate(dot(normal, key.direction)) * key.shadowAttenuation;
                half3 result = albedo * irradiance * 0.74h + rim + sheen * half3(0.44h, 0.53h, 0.62h);
                result = lerp(result, half3(0.86h, 0.55h, 0.29h), _HitFlash * 0.67h);
                return half4(MixFog(result, input.fog), 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
