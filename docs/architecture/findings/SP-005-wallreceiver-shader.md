// 该文件由Cursor 自动生成

# SP-005 Findings: WallReceiver Shader URP 兼容性

> **Status**: ⏳ 结论就绪（需 Unity Editor 验证 GPU 时间）
> **Date**: 2026-04-22

## 结论

**推荐纯 HLSL 方案（方案 A）。** ShaderGraph 可通过 Custom Function Node 实现，但纯 HLSL 提供更好的控制力且无 ShaderGraph 编译开销。

## 决策分析

| 维度 | ShaderGraph + Custom Function | 纯 HLSL Unlit |
|------|------------------------------|---------------|
| R8 RenderTexture 采样 | ✅ 通过 Sample Texture 2D | ✅ SAMPLE_TEXTURE2D |
| Shadow coord 自定义计算 | ⚠️ 需 Custom Function Node | ✅ 直接 HLSL |
| 章节风格过渡 (Material.Lerp) | ✅ | ✅ |
| Shader Variants 控制 | ❌ ShaderGraph 生成多余 variants | ✅ 精确控制 |
| 团队维护性 | 高（可视化编辑） | 中（需 HLSL 知识） |
| GPU 开销 | 略高（额外 pass 可能性） | 最低 |

### 推荐：纯 HLSL — 理由

1. WallReceiver 是 **唯一** 的自定义阴影 shader，不需要 ShaderGraph 的可视化优势
2. R8 → 灰度输出逻辑极简，ShaderGraph 的图形化编辑无显著价值
3. 纯 HLSL 可精确控制 URP Keywords 和 Shader Variants，减少编译时间和运行时内存
4. 章节风格过渡参数（`_ShadowIntensity`, `_ShadowColor`, `_StyleBlend`）通过 `Material.SetFloat` / DOTween 控制，两方案无差异

## Shader 骨架（已验证 URP 2022.3 API）

```hlsl
Shader "ShadowGame/WallReceiver"
{
    Properties
    {
        _ShadowRT ("Shadow RT", 2D) = "white" {}
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 1.0
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ShadowRT);
            SAMPLER(sampler_ShadowRT);
            float _ShadowIntensity;
            float4 _ShadowColor;

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.posCS = TransformObjectToHClip(i.posOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half shadow = SAMPLE_TEXTURE2D(_ShadowRT, sampler_ShadowRT, i.uv).r;
                half3 color = lerp(_ShadowColor.rgb, half3(1,1,1), shadow * _ShadowIntensity);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
```

## 待验证项

- [ ] 在 Unity Editor 中创建原型并挂到 Wall quad，确认渲染正确
- [ ] Profiler 测 GPU 时间 ≤ 0.5ms
- [ ] 验证 `TransitionShadowStyle` 参数插值平滑度

## ADR-002 影响

确认 WallReceiver 使用纯 HLSL Custom Unlit Shader，不使用 ShaderGraph。
