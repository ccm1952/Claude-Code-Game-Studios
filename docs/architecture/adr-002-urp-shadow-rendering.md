// 该文件由Cursor 自动生成

# ADR-002: URP Rendering Pipeline for Shadow Projection

> **Status**: Proposed
> **Date**: 2026-04-22
> **Author**: Technical Director
> **Engine**: Unity 2022.3.62f2 LTS — URP (Universal Render Pipeline)
> **Knowledge Risk**: LOW (core URP APIs) / MEDIUM (WallReceiver custom HLSL shader)

---

## Context

影子回忆 (Shadow Memory) 的核心玩法是**影子匹配**——玩家排列场景中的物体，使其投射的影子与目标轮廓匹配。这一机制对渲染管线提出了五个关键需求：

1. **精确可控的实时影子**：影子必须在"墙壁"接收面上准确投射，且玩家可通过移动物体/光源实时操纵影子形态。
2. **移动端质量分级**：支持 3 个质量档位（High / Medium / Low），iPhone 12+ 作为基准设备，必须维持 60fps。
3. **CPU 侧影子采样**：匹配评分算法需要在 CPU 侧读取影子像素数据，要求 ShadowRT + AsyncGPUReadback 管线。
4. **5 章视觉风格**：每章有独立的光照氛围（暖色/自然/冷色/消沉/空灵），影子渲染需支持运行时风格切换。
5. **帧预算约束**：ShadowRT CPU 回读 ≤ 1.5ms；整体 Shadow Pass 额外 draw calls < 20。

### 当前项目状态

- 项目已配置 URP（`UniversalRenderPipelineAsset` 存在于项目中）。
- Master Architecture Document 将 URP Shadow Rendering 归入 **Foundation Layer**。
- Shadow Puzzle System（Feature Layer）依赖本模块提供的 `ShadowRT` 数据进行匹配评分。
- 本 ADR 需在 Sprint 1 前 Accepted，阻塞 Shadow Puzzle 系统实现。

---

## Decision

采用 **URP 原生方向光级联阴影贴图 + 自定义 WallReceiver Shader + ShadowSampleCamera 独立渲染** 的三层架构。

### 架构总览

```
Directional Light
       │
       ├──> URP Shadow Map Cascades ──> WallReceiver Shader (custom HLSL)
       │                                     │
       │                                     └── 墙面上的可视影子渲染
       │
       └──> ShadowSampleCamera (Overlay/独立相机)
                   │
                   └──> ShadowRT (R8 RenderTexture, 512×512)
                               │
                               └──> AsyncGPUReadback.Request()
                                           │
                                           └──> CPU NativeArray<byte>
                                                       │
                                                       └──> Shadow Puzzle System
                                                            (匹配评分计算)
```

### 核心组件

#### 1. URP 方向光级联阴影贴图

利用 URP 原生的 `MainLightShadowCasterPass` 生成阴影贴图，通过 `UniversalRenderPipelineAsset` 配置级联参数。所有 shadow caster 物体使用标准 URP shadow pass，无需自定义 caster 逻辑。

#### 2. WallReceiver Shader（自定义 HLSL）

**不使用 ShaderGraph**——因为需要直接采样 URP 的 `_MainLightShadowmapTexture` 并施加章节风格化效果（发光、边缘柔化、色温调色），ShaderGraph 对 shadow map 直接采样的支持有限。

Shader 职责：
- 采样 URP shadow map，计算接收面上的阴影衰减
- 施加章节风格参数（`_ShadowColor`, `_GlowIntensity`, `_EdgeSoftness`, `_ColorTemperature`）
- 支持 shadow-only 渲染模式（仅输出阴影通道，用于调试和 ShadowRT 对比）
- 兼容 SRP Batcher（使用 `CBUFFER` 声明 per-material properties）

```hlsl
// WallReceiver.shader 伪结构
Shader "ShadowMemory/WallReceiver"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.15, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.3
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.5
        _ColorTemperature ("Color Temperature", Range(-1, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "WallReceiver"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // MainLightRealtimeShadow() 采样 → 风格化处理 → 输出
            ENDHLSL
        }
    }
}
```

#### 3. ShadowSampleCamera + ShadowRT

独立 Camera 组件，仅渲染阴影到 R8 格式的 RenderTexture（灰度，0=全阴影，255=无阴影）。

| 参数 | 值 | 说明 |
|------|----|------|
| RenderTexture 格式 | R8 (8-bit grayscale) | 最小内存占用，匹配评分只需阴影/非阴影二值 |
| 分辨率 | 512×512 | 在精度和 readback 性能间取平衡 |
| 内存占用 | ~256 KB | R8 @ 512×512 = 262,144 bytes |
| 更新频率 | 每帧或隔帧 (可配置) | 低档设备可降为隔帧更新 |
| Culling Mask | 仅 ShadowCaster layer 物体 + Wall surface | 最小化 draw calls |

#### 4. AsyncGPUReadback 管线

```csharp
// 伪代码 — 每帧或隔帧执行
AsyncGPUReadback.Request(_shadowRT, 0, TextureFormat.R8, OnReadbackComplete);

void OnReadbackComplete(AsyncGPUReadbackRequest request)
{
    if (request.hasError) return;
    NativeArray<byte> data = request.GetData<byte>();
    // 传递给 Shadow Puzzle System 进行匹配评分
    // 注意：data 仅在回调内有效，需立即消费或拷贝
}
```

**关键约束**：
- AsyncGPUReadback 引入 **1 帧延迟**（GPU 帧 N 的数据在帧 N+1 的回调中可用）。对于本项目可接受——玩家操纵物体的帧率远低于渲染帧率，1 帧延迟不影响体感。
- 回调在主线程 `Update()` 之后执行，需确保 Shadow Puzzle System 在下一帧消费数据。
- **HybridCLR 兼容性**：AsyncGPUReadback 的回调是 `Action<AsyncGPUReadbackRequest>`，需在 Sprint 0 验证 HybridCLR 对此 delegate 在 AOT 场景下的支持（Open Question #7）。

### 质量分级策略

三个预设档位，运行时可切换：

| 参数 | High | Medium | Low |
|------|:----:|:------:|:---:|
| Shadow Map Resolution | 2048 | 1024 | 512 |
| Shadow Cascades | 4 | 2 | 1 |
| Shadow Type | Soft Shadows | Soft Shadows | Hard Shadows |
| ShadowRT Update | 每帧 | 每帧 | 隔帧 |
| WallReceiver Edge Softness | 0.5 | 0.3 | 0.0 |

**切换方式**：通过运行时修改 `UniversalRenderPipelineAsset` 的 shadow 属性实现。URP 2022.3 支持运行时修改以下属性：
- `shadowResolution`
- `shadowCascadeCount`
- `softShadows`

**自动降级规则**：
- 监控最近 5 帧的帧时间
- 连续 5 帧 > 20ms → 自动降一档
- 连续 30 帧 < 12ms（在非 High 档位下）→ 自动升一档
- 降级/升级有 3 秒冷却期，防止抖动
- 管理归属：由 URP Shadow Rendering 模块自行管理（局部降级），全局性能监控由 ADR-018 定义

### 章节视觉风格预设

每章通过 Luban 配置表定义一组影子风格参数，运行时通过 Material Property Block 或 shader global 切换：

| 章节 | 色温 | 阴影色 | 发光强度 | 边缘柔化 | 氛围描述 |
|:----:|:----:|:------:|:--------:|:--------:|---------|
| Ch.1 | +0.3 (暖) | `#2A1810` | 0.4 | 0.5 | 温暖童年记忆 |
| Ch.2 | 0.0 (自然) | `#1A1A20` | 0.3 | 0.4 | 自然校园时光 |
| Ch.3 | -0.2 (冷) | `#101828` | 0.2 | 0.3 | 冷调城市疏离 |
| Ch.4 | -0.1 (消沉) | `#181515` | 0.1 | 0.2 | 压抑的沉寂 |
| Ch.5 | +0.1 (空灵) | `#201825` | 0.5 | 0.6 | 超现实空灵光影 |

**风格切换**：章节过渡时通过 DOTween 在 1.2s 内 lerp 所有参数，避免视觉跳变。

---

## Alternatives Considered

### Alternative 1: Custom SRP Render Pass（拒绝）

编写完全自定义的 SRP render pass，在物体轮廓基础上投射阴影，绕过 URP 标准阴影管线。

| 维度 | 评估 |
|------|------|
| **控制力** | ★★★★★ 完全控制影子外观 |
| **实现成本** | ★☆☆☆☆ 极高——需要从零实现 shadow caster/receiver 逻辑 |
| **URP 兼容性** | ★★☆☆☆ 自定义 pass 可能与 URP 的 SRP Batcher、shadow cascade 系统冲突 |
| **维护性** | ★★☆☆☆ URP 版本升级时需要同步维护 |
| **性能** | ★★★☆☆ 无法利用 URP 的 shadow cascade 分级优化 |

**拒绝原因**：实现成本与本项目的影子需求不匹配。我们需要的是"精确的标准阴影"而非"风格化非标阴影"。URP 原生阴影已经提供了足够的精度，仅需在接收端做风格化处理。

### Alternative 2: 烘焙/混合阴影（拒绝）

预烘焙基础阴影到 lightmap，运行时仅计算玩家移动物体的增量阴影。

| 维度 | 评估 |
|------|------|
| **性能** | ★★★★★ 静态部分零运行时成本 |
| **适用性** | ★☆☆☆☆ 完全不适用——本游戏中所有物体都是可移动的 |
| **开发体验** | ★★☆☆☆ 每次修改场景布局需重新烘焙 |
| **匹配评分** | ★★☆☆☆ 混合阴影的 CPU 回读复杂度远高于单一实时源 |

**拒绝原因**：核心玩法要求所有物体都可以被玩家自由操纵，烘焙方案在根本上与需求矛盾。

### Alternative 3: HDRP（拒绝）

使用 High Definition Render Pipeline，获取最佳阴影质量（area shadows、contact shadows）。

| 维度 | 评估 |
|------|------|
| **阴影质量** | ★★★★★ 最佳视觉效果 |
| **移动端兼容** | ☆☆☆☆☆ HDRP 不支持移动平台 |
| **性能** | ☆☆☆☆☆ 在目标设备上无法达到 60fps |
| **项目匹配** | ★★☆☆☆ 过度设计——本项目不需要 AAA 级渲染品质 |

**拒绝原因**：HDRP 不支持 iOS/Android，这与 Mobile-First 平台策略（ADR-003）直接矛盾。

---

## Consequences

### Positive

- **原生 URP 集成**：利用 URP 的 SRP Batcher、shadow cascade 系统，不重复造轮子
- **移动端优化成熟**：URP 2022.3 的移动端阴影管线经过广泛验证
- **灵活质量分级**：3 档配置可覆盖从低端到高端的移动设备谱系
- **实时可操纵**：影子随物体/光源移动实时变化，支撑核心玩法
- **CPU 可采样**：ShadowRT + AsyncGPUReadback 为匹配评分提供可靠的像素级数据源
- **风格化可扩展**：WallReceiver shader 的参数化设计支持未来增加更多章节风格

### Negative

- **自定义 Shader 维护负担**：WallReceiver.shader 使用 HLSL 而非 ShaderGraph，需要 shader 专家维护。URP 大版本升级时（如从 2022.3 到 6000.x），阴影采样 API 可能变更。
- **AsyncGPUReadback 1 帧延迟**：匹配评分数据比渲染晚 1 帧，在极端情况下（快速甩动物体）可能产生短暂评分不一致。缓解：temporal smoothing（0.2s 滑动窗口）已在 Shadow Puzzle System 中设计。
- **质量分级跨设备测试成本**：3 个档位 × 多个目标设备 = 大量组合需要测试，确保阴影匹配评分在不同质量档位下产生一致的结果。
- **ShadowRT 额外 draw calls**：ShadowSampleCamera 的独立渲染增加 < 20 个额外 draw calls，占用部分移动端 draw call 预算。

### Risks

| 风险 | 等级 | 缓解措施 |
|------|:----:|---------|
| WallReceiver shader 需随 URP 版本更新调整 | MEDIUM | 锁定 Unity 2022.3 LTS 至 1.0 发布；shader 中 `#include` 使用 URP package 相对路径，便于追踪 API 变更 |
| AsyncGPUReadback 在低端设备上性能不稳定 | MEDIUM | Low 档位降为隔帧回读；设置 1.5ms 超时，超时则跳过本帧评分更新 |
| HybridCLR 对 AsyncGPUReadback callback 的 AOT 兼容性未确认 | MEDIUM | Sprint 0 spike 验证（Open Question #7）；备选方案：将回调逻辑放入 Default 程序集 |
| 不同质量档位下影子边缘差异导致匹配评分不一致 | LOW | 匹配算法使用阈值化（threshold-based），而非像素级精确匹配；评分公式包含容差 |
| ShadowSampleCamera 与 URP 的 Camera Stack 冲突 | LOW | ShadowSampleCamera 使用 `CameraType.Game` + 独立 render target，不加入 Camera Stack |

---

## GDD Requirements Addressed

### urp-shadow-rendering.md

| TR ID | Requirement | Coverage |
|-------|------------|:--------:|
| TR-render-001 | URP 方向光阴影贴图作为基础阴影源 | ✅ Section: URP 方向光级联阴影贴图 |
| TR-render-002 | 级联阴影配置（1/2/4 cascades） | ✅ 质量分级策略表 |
| TR-render-003 | WallReceiver 自定义 shader 接收阴影 | ✅ Section: WallReceiver Shader |
| TR-render-004 | Shadow map 分辨率 512/1024/2048 | ✅ 质量分级策略表 |
| TR-render-005 | Soft/Hard shadow 切换 | ✅ 质量分级策略表 |
| TR-render-006 | 运行时质量档位切换 | ✅ UniversalRenderPipelineAsset 运行时修改 |
| TR-render-007~011 | 章节风格参数（色温、阴影色、发光、边缘柔化） | ✅ 章节视觉风格预设表 |
| TR-render-012 | ShadowSampleCamera 独立渲染 | ✅ Section: ShadowSampleCamera + ShadowRT |
| TR-render-013 | ShadowRT R8 格式 | ✅ ShadowRT 参数表 |
| TR-render-014 | ShadowRT 512×512 分辨率 | ✅ ShadowRT 参数表 |
| TR-render-015 | AsyncGPUReadback 回读管线 | ✅ Section: AsyncGPUReadback 管线 |
| TR-render-016 | 自动降级规则 (5帧 > 20ms) | ✅ 自动降级规则 |
| TR-render-017 | Shadow pass 额外 draw calls < 20 | ✅ Performance Budgets |
| TR-render-018 | ShadowRT 内存 ≤ 256KB | ✅ ShadowRT 参数表 |
| TR-render-019 | ShadowRT CPU 回读 ≤ 1.5ms | ✅ Performance Budgets |
| TR-render-020 | 风格切换无视觉跳变 | ✅ DOTween lerp 1.2s 过渡 |
| TR-render-021 | 高对比度模式支持 | ⚠️ Deferred to ADR-020 (Accessibility) |
| TR-render-022 | 色盲友好阴影色方案 | ⚠️ Deferred to ADR-020 (Accessibility) |

### shadow-puzzle-system.md

| TR ID | Requirement | Coverage |
|-------|------------|:--------:|
| TR-puzzle-001 | 多锚点评分依赖 ShadowRT 数据 | ✅ AsyncGPUReadback → CPU NativeArray → Puzzle System |

### settings-accessibility.md

| TR ID | Requirement | Coverage |
|-------|------------|:--------:|
| TR-settings-003 | 质量档位用户可选 | ✅ 3 档质量 + 自动/手动切换 |

---

## Dependencies

### Depends On

| 依赖项 | 类型 | 说明 |
|--------|------|------|
| 无 | — | 本 ADR 位于 Foundation Layer，仅依赖 URP 平台层 |

### Enables (下游依赖本 ADR)

| ADR / System | 关系 | 说明 |
|-------------|------|------|
| ADR-012: Shadow Match Algorithm | **Hard dependency** | 匹配评分算法消费本 ADR 定义的 ShadowRT 数据 |
| ADR-018: Performance Monitoring | **Soft dependency** | 全局性能监控可能覆盖本地自动降级策略 |
| ADR-021: Quality Tier Auto-Detection | **Soft dependency** | 设备能力检测可为本 ADR 的质量分级提供初始值 |

### Blocks

| System | 说明 |
|--------|------|
| Shadow Puzzle System 实现 | 需要 ShadowRT 数据接口就绪 |
| Object Interaction 视觉反馈 | 需要 WallReceiver shader 渲染影子 |
| Chapter Style 管线 | 需要 shadow style preset 接口 |

---

## Performance Budgets

| Metric | Budget | Measurement Method |
|--------|--------|--------------------|
| ShadowRT CPU readback | ≤ 1.5ms | `Profiler.BeginSample` / `EndSample` on readback callback |
| Shadow map resolution | 512–2048 (by tier) | `UniversalRenderPipelineAsset` config |
| Shadow pass additional draw calls | < 20 | Frame Debugger |
| ShadowRT memory | ~256 KB (R8 @ 512×512) | Memory Profiler |
| WallReceiver shader GPU time | < 0.5ms | GPU Profiler |
| Style transition (chapter change) | 1.2s lerp, 0 frame spike | Visual inspection + Profiler |
| Auto-degradation response | ≤ 5 frames (~83ms) | Frame timing log |

---

## API Surface (Module Ownership)

摘自 Master Architecture Document Section 4.1，此处作为规范性引用：

```csharp
public interface IShadowRendering
{
    RenderTexture GetShadowRT();

    void SetShadowGlow(float intensity, Color color);

    void FreezeShadow(RenderTexture snapshotRT);

    void SetShadowStyle(ShadowStylePreset preset);

    UniTask TransitionShadowStyle(ShadowStylePreset from, ShadowStylePreset to, float duration);

    void SetQualityTier(ShadowQualityTier tier);

    ShadowQualityTier GetCurrentQualityTier();

    void SetShadowOnlyMode(bool enabled);
}

public enum ShadowQualityTier { High, Medium, Low }

public struct ShadowStylePreset
{
    public float ColorTemperature;   // -1 to +1
    public Color ShadowColor;
    public float GlowIntensity;     // 0 to 1
    public float EdgeSoftness;      // 0 to 1
}
```

---

## Validation Criteria

以下条件全部满足时，本 ADR 视为实现验证通过：

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | iPhone 12 上 Medium 档位维持 60fps（帧时间 < 16.67ms） | Xcode Instruments GPU Profiler，连续 60s gameplay |
| 2 | ShadowRT readback 在所有档位下完成时间 ≤ 1.5ms | Unity Profiler `AsyncGPUReadback` marker |
| 3 | 影子匹配评分在 High/Medium/Low 三档下结果一致（误差 < 5%） | 自动化测试：固定物体位置，3 档分别跑评分，比较 |
| 4 | 章节风格切换过渡平滑，无视觉跳变 | 人工 QA + 录屏审查，确认参数 lerp 无突变帧 |
| 5 | WallReceiver shader 兼容 SRP Batcher | Frame Debugger 确认 "SRP Batch" 合并 |
| 6 | ShadowSampleCamera 不引入可见的渲染伪影 | 人工 QA：检查墙面阴影渲染与 ShadowRT 输出一致 |
| 7 | 自动降级在帧时间持续 > 20ms 时 5 帧内触发 | 压力测试（大量 shadow caster）+ Profiler 日志 |

---

## Open Questions (Carried from Architecture Doc)

| # | Question | Impact | Resolution Plan |
|---|----------|--------|----------------|
| 5 | WallReceiver shader 是否必须纯 HLSL，还是 ShaderGraph + Custom Function 可行？ | 实现方式选择 | Sprint 0 prototype 验证 |
| 7 | HybridCLR 对 `AsyncGPUReadback` 回调的 AOT 兼容性 | 如不兼容需将回调放入 Default 程序集 | Sprint 0 spike |
| 10 | 自动降级由本模块局部管理 vs 全局 Performance Monitor 统一管理 | 模块边界归属 | ADR-018 讨论时决定 |

---

## Implementation Notes

### Sprint 0 Spikes (必须在实现前完成)

1. **WallReceiver Shader Prototype**：最小 HLSL shader，验证 `MainLightRealtimeShadow()` 在 URP 2022.3 中的采样正确性。
2. **AsyncGPUReadback + HybridCLR Test**：在热更 DLL 中注册 `AsyncGPUReadback.Request` 回调，确认无 AOT 报错。
3. **Quality Tier Runtime Switch Test**：运行时修改 `UniversalRenderPipelineAsset.shadowResolution`，确认立即生效且无闪烁。

### 与 Architecture Principles 的对齐

| Principle | Alignment |
|-----------|-----------|
| P1: 数据驱动 | ✅ 章节风格参数由 Luban 配置表定义，质量档位参数可配置 |
| P2: 事件解耦 | ✅ ShadowRT 通过接口暴露，不直接与 Puzzle System 耦合 |
| P3: 异步优先 | ✅ AsyncGPUReadback 是异步非阻塞操作 |
| P4: 资源闭环 | ✅ ShadowRT 在模块初始化时创建，模块销毁时释放 |
| P5: 层级隔离 | ✅ 位于 Foundation Layer，上层通过 `IShadowRendering` 接口访问 |

---

*End of ADR-002*
