// 该文件由Cursor 自动生成

# Story 002: WallReceiver HLSL Shader

> **Epic**: URP Shadow Rendering
> **Status**: Complete (Visual 完整验证延后到集成阶段)
> **Layer**: Foundation
> **Type**: Visual/Feel
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-007`, `TR-render-001`, `TR-render-002`, `TR-render-008`, `TR-render-011`, `TR-render-020`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-007: WallReceiver 自定义 shader 接收阴影
- TR-render-001: URP Forward + SRP Batcher 兼容
- TR-render-002: HDR off, MSAA off，SMAA 抗锯齿
- TR-render-008: 影子与墙面明暗比强制 ≥ 3:1（推荐 5:1）
- TR-render-011: 影子特效接口（Glow、Freeze Snapshot、Style Preset）
- TR-render-020: NearMatch 发光效果不影响 ShadowRT 的灰度值

**ADR Governing Implementation**: ADR-002: URP Rendering Pipeline；SP-005 Findings
**ADR Decision Summary**: WallReceiver 使用纯 HLSL Custom Unlit Shader（非 ShaderGraph），直接 `#include` URP ShaderLibrary，使用 CBUFFER 声明 per-material properties 以兼容 SRP Batcher。章节风格参数通过 `Material.SetFloat` / DOTween 插值控制。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: MEDIUM
**Engine Notes**: SP-005 已确认纯 HLSL 方案在 URP 2022.3 中可行（见 `docs/architecture/findings/SP-005-wallreceiver-shader.md`）。需在 Editor 中实测 GPU 时间 ≤ 0.5ms。URP 大版本升级时阴影采样 API 可能变更，需对照变更日志。

**Control Manifest Rules (this layer)**:
- Required: WallReceiver shader 纯 HLSL Custom Unlit，不使用 ShaderGraph（ADR-002, SP-005）
- Required: 所有 per-material properties 在 CBUFFER 中声明，兼容 SRP Batcher（ADR-002）
- Required: shader `#include` 使用 URP package 相对路径（`Packages/com.unity.render-pipelines.universal/ShaderLibrary/...`）（ADR-002）
- Required: 章节风格参数（`_ShadowColor`, `_GlowIntensity`, `_EdgeSoftness`, `_ColorTemperature`）通过 Luban config 驱动（ADR-007）
- Required: NearMatch 发光必须在 ShadowSampleCamera 渲染之后叠加——不影响 ShadowRT 灰度值（TR-render-020）
- Forbidden: 不使用 ShaderGraph 实现 WallReceiver（ADR-002）
- Forbidden: 不硬编码章节风格参数数值（ADR-007）
- Guardrail: WallReceiver shader GPU 时间 < 0.5ms（ADR-002）；影子明暗比 ≥ 3:1（GDD）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md`，scoped to this story:*

- [ ] **AC-1**: WallReceiver.shader 作为纯 HLSL Unlit Shader 存在于 `Assets/` 目录，能够接收 URP 方向光阴影（`MainLightRealtimeShadow()` 正确采样）
- [ ] **AC-2**: 材质通过 Frame Debugger 验证为 "SRP Batch" 合并（CBUFFER 声明正确）
- [ ] **AC-3**: 影子区域与非影子区域的明暗比在 Medium 档达到 ≥ 5:1（Shader 采样值验证），在 Low 档 ≥ 3:1
- [ ] **AC-4**: Shader 暴露以下可配置属性：`_ShadowColor`、`_GlowIntensity`（Range 0-1）、`_EdgeSoftness`（Range 0-1）、`_ColorTemperature`（Range -1 to 1）、`_ShadowContrast`（Range 1-3）
- [ ] **AC-5**: `SetShadowGlow(float intensity, Color color)` 接口调用时，发光效果在投影墙面上可见；`ClearShadowGlow()` 调用后发光消失
- [ ] **AC-6**: NearMatch 发光效果不影响 ShadowSampleCamera 输出的 ShadowRT 像素值（ShadowRT 读取时发光不计入灰度）
- [ ] **AC-7**: 墙面材质 Specular/Smoothness = 0（不产生任何高光），通过 Unity Material Inspector 验证
- [ ] **AC-8**: GPU 渲染时间 < 0.5ms（Unity GPU Profiler 或 Xcode Metal Debugger 测量）

---

## Implementation Notes

*Derived from ADR-002, SP-005, control-manifest.md §5.1:*

**Shader 文件路径**: `Assets/GameScripts/HotFix/GameLogic/Rendering/Shaders/WallReceiver.shader`

**Shader 骨架（来自 SP-005 验证结果）：**
```hlsl
Shader "ShadowGame/WallReceiver"
{
    Properties
    {
        _ShadowRT ("Shadow RT", 2D) = "white" {}
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 1.0
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.0
        _GlowColor ("Glow Color", Color) = (1, 0.8, 0.4, 1)
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.3
        _ColorTemperature ("Color Temperature", Range(-1, 1)) = 0.0
        _ShadowContrast ("Shadow Contrast", Range(1, 3)) = 2.0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float4 _GlowColor;
                float _ShadowIntensity;
                float _GlowIntensity;
                float _EdgeSoftness;
                float _ColorTemperature;
                float _ShadowContrast;
            CBUFFER_END
            // ... vert/frag 实现
            ENDHLSL
        }
    }
}
```

**NearMatch 发光隔离策略：**
- 发光效果通过一个额外的 Pass 或后期叠加实现，而非修改 `_ShadowRT` 的采样逻辑
- `ShadowSampleCamera` 渲染不包括发光 Pass（Culling Mask 排除发光 FX Layer）
- 发光通过 `Material.SetFloat("_GlowIntensity", intensity)` 控制，仅影响主 Camera 的可视渲染输出

**效果接口实现（由 `ShadowRenderingModule` 持有材质引用调用）：**
```csharp
public void SetShadowGlow(float intensity, Color color)
{
    _wallMaterial.SetFloat("_GlowIntensity", intensity);
    _wallMaterial.SetColor("_GlowColor", color);
}
public void ClearShadowGlow() => _wallMaterial.SetFloat("_GlowIntensity", 0f);
```

**对比度约束（GDD 公式）：**
```
finalShadowColor = lerp(wallBaseColor, shadowTintColor, shadowIntensity * contrastMultiplier)
contrastRatio = luminance(wallBaseColor) / luminance(finalShadowColor)
```
`contrastRatio` 必须 ≥ 3:1（Low 档）/ ≥ 5:1（Medium/High 档）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 001**: ShadowRT 的创建和 Camera 配置（本 story 假设 RT 已存在，Shader 读取它）
- **Story 005**: 章节风格预设的数据定义和 DOTween 过渡动画（本 story 只实现 Shader 属性接口）
- **Story 007**: 不同档位下的 EdgeSoftness 参数分级（本 story 只实现接口，数值来自 config）

---

## QA Test Cases

*Visual/Feel story — 人工验证步骤:*

- **AC-1**: WallReceiver shader 正确接收 URP 阴影
  - Setup: 在测试场景中放置一个 Directional Light + 一个 Cube（Cast Shadows On）+ 一个使用 WallReceiver 材质的 Quad（Receive Shadows On）
  - Verify: Cube 的影子投射到 Quad 上；Frame Debugger 的 Shadow Map 阶段包含 Cube 的阴影数据；WallReceiver Pass 在 Forward Lit 阶段执行
  - Pass condition: Quad 上可见 Cube 投影的清晰影子轮廓，无全黑或全白异常

- **AC-2**: SRP Batcher 兼容性
  - Setup: 场景中放置 2+ 个使用 WallReceiver 材质的物体，打开 Frame Debugger
  - Verify: Frame Debugger 显示 "SRP Batch" 分组，WallReceiver draw calls 合并
  - Pass condition: 看到 "SRP Batch" 标签，非 "Static Batching" 也非 "Dynamic Batching"

- **AC-3**: 明暗比 ≥ 5:1 (Medium 档)
  - Setup: 固定光源角度，使用 WallReceiver 材质的白墙（#F5F0E8），开启 Medium 档阴影
  - Verify: 使用 Unity Profiler 的 GPU Capture 读取影子区域像素值（R 通道）和非影子区域像素值
  - Pass condition: `luminance(wallColor) / luminance(shadowColor) ≥ 5.0`；Low 档 ≥ 3.0

- **AC-5 & AC-6**: NearMatch 发光不污染 ShadowRT
  - Setup: 调用 `SetShadowGlow(0.8f, Color.yellow)`，同时捕获 ShadowRT 的像素数据（通过测试代码同步读取）
  - Verify: (1) 主 Camera 渲染的墙面上有发光光晕；(2) ShadowRT 像素值与无发光时相同（误差 ≤ 1/255）
  - Pass condition: 两项均通过；`ClearShadowGlow()` 后发光消失，ShadowRT 无变化

- **AC-8**: GPU 时间预算
  - Setup: 在 iPhone 13 Mini 上使用 Medium 档，运行包含 2 个光源 + 10 个阴影物体的测试场景
  - Verify: Xcode Metal Debugger 或 Unity Profiler GPU module 显示 WallReceiver Pass 的 GPU 时间
  - Pass condition: WallReceiver GPU 时间 < 0.5ms，稳定（5 帧平均值）

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/wallreceiver-shader-evidence.md` — 包含截图（影子效果）、Frame Debugger 截图（SRP Batch）、GPU profiler 截图（< 0.5ms）、对比度计算验证
- 美术/TA 签字确认视觉效果符合 GDD §4 要求

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（ShadowRT 和 Camera 就绪，以便测试 Shader 的 RT 采样部分）
- Unlocks: Story 005（章节风格过渡需要 Shader 属性接口已就绪）
