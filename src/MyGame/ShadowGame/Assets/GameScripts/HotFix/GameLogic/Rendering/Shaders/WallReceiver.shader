// 该文件由Cursor 自动生成
// WallReceiver — URP Custom Unlit Shader (ADR-002, SP-005)
// Receives shadow from ShadowRT (R8 grayscale) and applies chapter style parameters.
// SRP Batcher compatible: all per-material properties in CBUFFER.

Shader "ShadowGame/WallReceiver"
{
    Properties
    {
        [Header(Shadow)]
        _ShadowRT ("Shadow RT", 2D) = "white" {}
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 1.0
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.12, 1)
        _ShadowContrast ("Shadow Contrast", Range(1, 3)) = 2.0

        [Header(Wall)]
        _WallColor ("Wall Base Color", Color) = (0.96, 0.94, 0.91, 1)

        [Header(Glow)]
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.0
        _GlowColor ("Glow Color", Color) = (1, 0.8, 0.4, 1)

        [Header(Style)]
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.3
        _ColorTemperature ("Color Temperature", Range(-1, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "WallReceiverForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex WallVert
            #pragma fragment WallFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // --- SRP Batcher: all per-material properties in CBUFFER ---
            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float4 _WallColor;
                float4 _GlowColor;
                float  _ShadowIntensity;
                float  _ShadowContrast;
                float  _GlowIntensity;
                float  _EdgeSoftness;
                float  _ColorTemperature;
            CBUFFER_END

            TEXTURE2D(_ShadowRT);
            SAMPLER(sampler_ShadowRT);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings WallVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 WallFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Sample ShadowRT (R8 grayscale: 1 = lit, 0 = shadow)
                half shadowSample = SAMPLE_TEXTURE2D(_ShadowRT, sampler_ShadowRT, input.uv).r;

                // Edge softness: smoothstep to control shadow edge transition
                half softEdge = smoothstep(0.5 - _EdgeSoftness * 0.5, 0.5 + _EdgeSoftness * 0.5, shadowSample);

                // Apply contrast curve
                half shadowMask = saturate(pow(softEdge, _ShadowContrast));

                // Mix wall color and shadow color based on shadow mask
                half3 wallBase = _WallColor.rgb;
                half3 shadowTint = _ShadowColor.rgb;
                half3 color = lerp(shadowTint, wallBase, shadowMask * _ShadowIntensity);

                // Color temperature shift (warm positive, cool negative)
                color.r += _ColorTemperature * 0.05;
                color.b -= _ColorTemperature * 0.05;

                // Glow overlay (additive, only affects main camera render, not ShadowRT)
                half3 glow = _GlowColor.rgb * _GlowIntensity * (1.0 - shadowMask);
                color += glow;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
