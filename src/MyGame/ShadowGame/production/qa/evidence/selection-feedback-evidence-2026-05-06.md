// 该文件由Cursor 自动生成

# Manual QA Evidence: S4-06 Selection Feedback (Visual/Feel)

> **Date**: 2026-05-06 (placeholder — pending Editor manual verification)
> **Story**: `production/epics/object-interaction/story-005-selection-feedback.md` S4-06
> **Story Type**: Visual/Feel
> **Sprint**: 4 (Sprint 2→3→4 第 2 次 carryover)

---

## Status

[ ] Pending Editor manual verification — Sprint 4 dev-story 阶段已完成 production code (`InteractableObjectFeedback.cs`) + EditMode tests (params 抽参数层 + lifecycle)；本文档记录 Editor 手动验证 evidence 待 user 在 Unity Editor 跑过 chapter scene 后 fill-in。

---

## Manual Verification Checklist (per story-005 §QA Test Cases AC-1..AC-5)

### AC-1: outline 在选中时激活

- [ ] **Setup**: 加载 chapter scene（Sprint 5 VS Build chapter 1 ready 后；当前 fallback：复用 SP011_SceneA / SceneB 或 chapter 测试场景）含 `InteractableObject` + `InteractableObjectFeedback` 配对
- [ ] **Action**: tap object
- [ ] **Verify**:
  - [ ] object 立即显示 outline 高亮（1 帧内出现）
  - [ ] 其他 object 无 outline
  - [ ] outline 在 1m 真实距离可见（outline 厚度 / 颜色 per Art Bible）
- [ ] **Screenshot**: `screenshots/s4-06-ac1-outline-on-selected.png`
- [ ] **Sign-off (designer + tech-artist)**:

### AC-2: scale bounce 在选中时播放

- [ ] **Setup**: 同 AC-1
- [ ] **Action**: tap object
- [ ] **Verify**:
  - [ ] object 短暂 punch 外扩后回弹（DOPunchScale (0.15, 0.15, 0), 0.2s, vibrato=5, elasticity=0.5）
  - [ ] 动画"snappy"且舒服 — 不 sluggish 不 jarring
  - [ ] 0.2s 内完成
  - [ ] scale 精确回到 (1,1,1) — no残留 artefact
- [ ] **Video clip**: `clips/s4-06-ac2-scale-bounce.mp4` (~3s clip)
- [ ] **Sign-off (game-designer)**:

### AC-3: 取消选中时反馈清除

- [ ] **Setup**: 选中 object A
- [ ] **Action**: tap 空白处 (cancel selection)
- [ ] **Verify**:
  - [ ] outline 立即消失
  - [ ] scale == (1,1,1)
  - [ ] 任意其他 object 上无 outline
- [ ] **Screenshot**: `screenshots/s4-06-ac3-deselect-no-outline.png`
- [ ] **Sign-off (game-designer)**:

### AC-4: snap 完成后 settle punch

- [ ] **Setup**: 选中 + drag object 到 grid 附近释放
- [ ] **Verify**:
  - [ ] snap 动画完成后立即播放小幅 settle 脉冲（DOPunchScale (0.05, 0.05, 0), 0.1s, vibrato=3）
  - [ ] settle 比选中 bounce 弱（明显小一倍 magnitude / 短一倍 duration）
  - [ ] object 落在正确 snapped grid 位置
- [ ] **Video clip**: `clips/s4-06-ac4-snap-settle.mp4`
- [ ] **Sign-off (game-designer)**:

### AC-5: Locked 状态无反馈

- [ ] **Setup**: 通过 debug 工具触发 `IInteractionEvent.OnRequestPuzzleLockAll("debug")` (puzzle lock all)
- [ ] **Action**: tap object
- [ ] **Verify**:
  - [ ] 点击 object 无 outline 显示
  - [ ] 无任何 bounce 动画
  - [ ] 视觉层 0 响应（per spec — 被锁不该有视觉响应）
- [ ] **Screenshot**: `screenshots/s4-06-ac5-locked-no-feedback.png`
- [ ] **Sign-off (game-designer)**:

---

## Performance verification (AC: feedback 在中端移动 GPU 上无明显 overdraw)

- [ ] **Setup**: 在 chapter scene 内放 5+ InteractableObject + Feedback；用 Unity Profiler 测试帧时
- [ ] **Verify**: 启用 outline 后帧时 ≤ 0.2ms 增量（vs no-outline baseline）
- [ ] **Profiler screenshot**: `screenshots/s4-06-perf-profile.png`
- [ ] **Sign-off (tech-artist)**:

---

## EditMode Unit Tests Summary (sprint 4 dev-story 完成)

✅ `InteractableObjectFeedbackTests.cs` 8 tests:
- §1 Tween params 抽参数层 (Sprint 1 Visual 教训): 3 tests
  - `SelectPunchParams_AreInDesignSpec`
  - `SnapSettlePunchParams_AreWeakerThanSelectPunch` (验证 settle < select)
  - `SelectPunchParams_AreInSafeBoundsForCollider` (magnitude/duration 安全范围)
- §2 订阅 lifecycle (AC-6): 4 tests
  - `IsSubscribedForTest_ReturnsFalse_BeforeInitialize` (EditMode PlayerLoop 不驱动 OnEnable，必须 explicit Initialize)
  - `Initialize_WithoutTarget_DoesNotSubscribe`
  - `InitializeShutdownCycle_ToggleSubscriptionState` (含 idempotent guard 验证)
  - `Shutdown_WithoutInitialize_IsSafe` (null-check guard)

EditMode tests 自动验证：
- **AC-6** ✅ 订阅生效 + Initialize/Shutdown lifecycle + idempotent guard
- **AC-7** ✅ 协议合规（不订阅 GameEvent；仅 fsm.StateChanged C# event）— 通过 production code 文件无 `GameEvent.AddEventListener<IInteractionEvent>` 显示验证

---

## Deviations / Notes

- 本 story Visual/Feel 部分（AC-1..AC-5）的 Editor manual verification 待 Sprint 5 chapter scene ready 后完成（依赖 Sprint 5 VS Build chapter 1）
- Production code (`InteractableObjectFeedback.cs`) Sprint 4 dev-story 完成；params 抽参数层 + lifecycle 测试已 EditMode 闭环
- Sprint 4 closure 时本 evidence 文件保持 [ ] pending 状态；Sprint 5 chapter ready 后 user 在 Editor 跑过 chapter scene 完成手动验证 fill-in
- **关键设计决策**：本 evidence 文件 Sprint 4 闭环允许 partial completion（EditMode tests ✅ + manual verification pending）；Sprint 5 VS chapter ready 后再做最后一步 manual verification 不阻塞 Sprint 4 闭环
