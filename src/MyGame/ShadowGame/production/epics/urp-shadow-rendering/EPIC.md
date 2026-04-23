// 该文件由Cursor 自动生成

# Epic: URP Shadow Rendering

> **Layer**: Foundation
> **GDD**: `design/gdd/urp-shadow-rendering.md`
> **Architecture Module**: ShadowRenderingSystem (WallReceiver shader, ShadowRT, quality tiers)
> **Governing ADRs**: ADR-002 (URP Rendering Pipeline), ADR-018 (Performance Monitoring)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 7 stories created

## Overview

URP Shadow Rendering 是影子回忆核心玩法的视觉基础，负责通过 URP Forward Renderer 和自定义 WallReceiver shader 实现墙面影子投影效果。系统管理 shadow map 分辨率分级（PC=2048/High=1024/Medium=512）、ShadowSampleCamera + ShadowRT（R8 灰度 RenderTexture）的 GPU→CPU 读回管线，以及 5 个章节各自的 shadow style 预设。

该系统是 Shadow Puzzle 匹配评分算法的数据源——通过 `AsyncGPUReadback` 将 ShadowRT 像素数据传递给 CPU 端的 match scoring 计算。同时提供 quality tier 自动降级机制（连续 5 帧 > 20ms 时降档），确保在移动端低端设备上维持 60fps。WallReceiver shader 采用纯 HLSL 实现（SP-005 决策），支持 shadow glow、freeze snapshot、chapter style 过渡等特效接口。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-002: URP Rendering Pipeline | URP Forward + SRP Batcher；WallReceiver 纯 HLSL shader；ShadowRT R8 + AsyncGPUReadback；3 质量档 + 自动降级 | MEDIUM |
| ADR-018: Performance Monitoring | 全局 PerformanceMonitor 广播 QualityTierChanged 事件；帧时间 5 帧滑窗检测 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-render-001 | URP Forward + SRP Batcher | ADR-002 ✅ |
| TR-render-002 | HDR off, MSAA off, SMAA | ADR-002 ✅ |
| TR-render-003 | Shadow map resolution tiers | ADR-002 ✅ |
| TR-render-004 | Shadow cascades per tier | ADR-002 ✅ |
| TR-render-005 | Shadow distance 8m configurable | ADR-002 ⚠️ |
| TR-render-006 | 3 quality tiers + auto-detect | ADR-002 ✅ |
| TR-render-007 | WallReceiver custom shader | ADR-002 ✅ |
| TR-render-008 | Shadow contrast ≥ 3:1 | ADR-002 ⚠️ |
| TR-render-009 | ShadowSampleCamera + ShadowRT | ADR-002 ✅ |
| TR-render-010 | AsyncGPUReadback for ShadowRT | ADR-002, ADR-012 ✅ |
| TR-render-011 | Shadow effect interfaces | ADR-002 ✅ |
| TR-render-012 | 5 chapter shadow style presets | ADR-002 ✅ |
| TR-render-013 | Max 2 shadow-casting lights | ADR-002 ⚠️ |
| TR-render-014 | Shadow caster priority ordering | ADR-002 ⚠️ |
| TR-render-015 | Shadow Only rendering mode | ADR-002 ✅ |
| TR-render-016 | Auto quality degradation | ADR-002, ADR-018 ✅ |
| TR-render-017 | Draw call budget ≤ 150/40 | ADR-002, ADR-003 ✅ |
| TR-render-018 | Shadow memory ≤ 15MB | ADR-002 ⚠️ |
| TR-render-019 | ShadowRT CPU processing ≤ 1.5ms | ADR-002, ADR-012 ✅ |
| TR-render-020 | NearMatch glow not affect ShadowRT | ADR-002 ⚠️ |
| TR-render-021 | High-contrast accessibility mode | ❌ Deferred to ADR-020 (P2) |
| TR-render-022 | Shadow outline accessibility mode | ❌ Deferred to ADR-020 (P2) |
| TR-render-023 | All shadow settings configurable | ADR-002 ✅ |

## Sprint 0 Findings Impact

- **SP-005 (WallReceiver Shader)**: 决策采用纯 HLSL 实现（非 ShaderGraph）。需在 Editor 中验证 GPU 时间 ≤ 0.5ms。
- **SP-007 (HybridCLR + AsyncGPUReadback)**: `AsyncGPUReadback` 回调在 HybridCLR 热更代码中的兼容性需真机验证。若失败，需将 ShadowRT reader 移至 Default 程序集并通过 `IShadowRTReader` 接口暴露。**最高技术风险项。**
- **SP-010 (Performance Monitor)**: 决策采用全局 PerformanceMonitor 模块统一管理降级，Shadow Rendering 监听 `Evt_QualityTierChanged` 事件调整质量档。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

None — URP Shadow Rendering is a Foundation layer epic with no upstream epic dependencies. However, SP-007 must be resolved before implementing the AsyncGPUReadback pipeline.

## Stories

| # | Story | Type | Status | ADR | Notes |
|---|-------|------|--------|-----|-------|
| 001 | ShadowRT RenderTexture & Camera Setup | Logic | Ready | ADR-002 | Foundation — no upstream deps |
| 002 | WallReceiver HLSL Shader | Visual/Feel | Ready | ADR-002, SP-005 | Depends on 001 |
| 003 | ShadowRT AsyncGPUReadback 管线 | Logic | Ready⚠️ | ADR-002, SP-007 | **BLOCKED pending SP-007 real-device verification** |
| 004 | 质量档位系统（Quality Tier System） | Logic | Ready | ADR-002, SP-010 | Depends on 001 |
| 005 | 章节影子风格过渡 | Visual/Feel | Ready | ADR-002 | Depends on 002 |
| 006 | 性能自动降级集成 | Integration | Ready | ADR-018, SP-010 | Depends on 003, 004 + PerformanceMonitor epic |
| 007 | 移动端专项优化 | Config/Data | Ready | ADR-002, ADR-003 | Depends on 001, 004 |

**Story Summary**: 7 total — 2 Logic, 1 Logic(blocked), 2 Visual/Feel, 1 Integration, 1 Config/Data

## Implementation Order

```
001 (ShadowRT Setup) ──┬──> 002 (WallReceiver Shader) ──> 005 (Chapter Style)
                       │
                       ├──> 004 (Quality Tiers) ──┬──> 006 (Auto-Degrade) [needs PerformanceMonitor]
                       │                          │
                       └──> 003 (Readback) ───────┘
                       │
                       └──> 007 (Mobile Optimization)
```

> ⚠️ Story 003 blocked pending SP-007 real-device AsyncGPUReadback verification in HybridCLR.
> Resolve SP-007 before starting Story 003; other stories can proceed in parallel.

## Next Step

Run `/story-readiness production/epics/urp-shadow-rendering/story-001-shadow-rt-setup.md` to begin implementation.
