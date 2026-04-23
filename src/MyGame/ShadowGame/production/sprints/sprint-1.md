// 该文件由Cursor 自动生成

# Sprint 1 — Foundation Layer Complete

> **Period**: 2026-04-22 ~ 2026-05-06 (2 weeks)
> **Phase**: Pre-Production
> **Goal**: 完成 Foundation 层全部核心 stories + Core 层无依赖 epic 起步，使 Feature 层具备开发条件

## Sprint Goal

建立可运行的 Input System 和 URP Shadow Rendering 基础设施，同时启动无上游依赖的 Save System，为 Sprint 2 的 Core 层全面推进打通关键路径。

## Capacity

- Total days: 10 (2 周 × 5 天)
- Buffer (20%): 2 天（不可预见问题、真机调试）
- Available: 8 天有效产能

## Story Velocity Baseline

- Sprint 0 实际产出：Story 001 实现 + code review + test = ~0.5 天
- 预估本 Sprint：Logic story ~0.5d，Integration story ~1d，Visual story ~1.5d

---

## Tasks

### Must Have — Critical Path (Foundation 层)

| ID | Story | Epic | Type | Est. | Deps | Status |
|----|-------|------|------|:----:|------|--------|
| S1-01 | ~~story-001: SingleFinger FSM~~ | input-system | Logic | 0.5d | — | ✅ Complete |
| S1-02 | ~~story-002: DualFinger FSM~~ | input-system | Logic | 0.5d | S1-01 | ✅ Complete |
| S1-03 | ~~story-003: Gesture Event Dispatch~~ | input-system | Integration | 0.5d | S1-01, S1-02 | ✅ Complete |
| S1-04 | ~~story-004: InputBlocker Stack~~ | input-system | Logic | 0.5d | S1-03 | ✅ Complete |
| S1-05 | ~~story-005: InputFilter Whitelist~~ | input-system | Logic | 0.5d | S1-03 | ✅ Complete |
| S1-06 | ~~story-006: DPI Normalization + Luban Config~~ | input-system | Config/Data | 0.5d | S1-01 | ✅ Complete |
| S1-07 | ~~story-001: ShadowRT Setup~~ | urp-shadow-rendering | Visual | 1.0d | — | ✅ Complete |
| S1-08 | ~~story-002: WallReceiver HLSL Shader~~ | urp-shadow-rendering | Visual | 1.5d | S1-07 | ✅ Complete |

**Must Have 小计**: 5.5d（含已完成 0.5d）→ 剩余 5.0d

### Should Have — Core 层起步（无 Foundation 依赖）

| ID | Story | Epic | Type | Est. | Deps | Status |
|----|-------|------|------|:----:|------|--------|
| S1-09 | ~~story-001: Save Data Schema~~ | save-system | Logic | 0.5d | — | ✅ Complete |
| S1-10 | ~~story-002: Atomic Write + CRC32~~ | save-system | Logic | 1.0d | S1-09 | ✅ Complete |
| S1-11 | ~~story-001: Chapter Data Model~~ | chapter-state | Logic | 0.5d | — | ✅ Complete |

**Should Have 小计**: 2.0d

### Nice to Have — 如有余力

| ID | Story | Epic | Type | Est. | Deps | Status |
|----|-------|------|------|:----:|------|--------|
| S1-12 | ~~story-003: ShadowRT ReadBack~~ | urp-shadow-rendering | Integration | 1.0d | S1-08 | ✅ Complete |
| S1-13 | ~~story-003: Load Fallback Chain~~ | save-system | Logic | 0.5d | S1-10 | ✅ Complete |

**Nice to Have 小计**: 1.5d

---

## 排期总览

| 优先级 | Stories | 估时 | 累计 |
|--------|:-------:|:----:|:----:|
| Must Have | 8 (1 done) | 5.5d | 5.5d |
| Should Have | 3 | 2.0d | 7.5d |
| Buffer | — | 2.0d | 9.5d |
| Nice to Have | 2 | 1.5d | 11.0d |

**计划承诺**: Must Have (8) + Should Have (3) = **11 stories**
**冲刺目标**: Must Have 全部完成为 Sprint PASS

---

## Critical Path

```
S1-01 ✅ → S1-02 → S1-03 → S1-04
                         → S1-05
         → S1-06 (parallel)

S1-07 → S1-08 (→ S1-12 if time)

S1-09 → S1-10 (→ S1-13 if time)
S1-11 (parallel, no deps)
```

**最长路径**: S1-01 → S1-02 → S1-03 → S1-04/S1-05 = 2.0d
**并行轨道**: Input (5.0d) ‖ URP Shadow (2.5d) ‖ Save+Chapter (2.0d)

---

## Excluded from Sprint 1 (Deferred to Sprint 2+)

| Story | Reason |
|-------|--------|
| input-system/story-007 (Haptic) | P2 nice-to-have, 非 MVP 关键路径 |
| input-system/story-008 (PC Input) | 平台为 Mobile-First，PC 延后 |
| urp-shadow/story-004~007 | 依赖 story-003 完成后的质量调优 |
| scene-management 全部 | 依赖 save-system + chapter-state 基础 |
| object-interaction 全部 | 依赖 input-system 完成 |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|:----------:|:------:|------------|
| WallReceiver shader 真机表现不达标 | MEDIUM | HIGH | SP-005 已决策纯 HLSL；预留 0.5d 调试 |
| ShadowRT readback 性能问题 | LOW | MEDIUM | SP-007 已验证 AsyncGPUReadback 兼容 |
| Luban 配置表结构未确定 | LOW | LOW | Story-006 可先用硬编码 fallback，后接入 |

---

## Definition of Done (Sprint Level)

- [ ] Must Have stories 全部 Status = Complete
- [ ] 所有 Logic story 有对应 EditMode 测试且全绿
- [ ] Code review 通过（零 ADR 违规）
- [ ] Unity Editor 编译零错误
- [ ] active.md 更新反映 Sprint 1 完成状态

---

## Recommended Execution Order

1. **Day 1**: S1-02 (DualFinger FSM) + S1-06 (DPI Config) — 并行
2. **Day 2**: S1-03 (Gesture Dispatch) + S1-07 (ShadowRT Setup) — 并行
3. **Day 3**: S1-04 (Blocker) + S1-05 (Filter) + S1-09 (Save Schema) — 三轨并行
4. **Day 4-5**: S1-08 (WallReceiver Shader) + S1-10 (Atomic Write) + S1-11 (Chapter Model)
5. **Day 6-8**: Buffer + Should Have + Nice to Have

> **下一步**: `/story-readiness input-system/story-002-dual-finger-gestures` → `/dev-story`
