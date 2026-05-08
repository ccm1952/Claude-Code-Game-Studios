// 该文件由Cursor 自动生成

# Object Interaction 协议合规 grep 证据

> **Story**: S2-08 / story-001-interaction-state-machine.md AC-9
> **Date**: 2026-04-29
> **Scope**: `src/MyGame/ShadowGame/Assets/GameScripts/HotFix/`（含本 story 新增 `GameLogic/ObjectInteraction/` 目录）
> **Verdict**: ✅ **PASS** — 0 命中所有禁用模式

---

## 背景

X1 Object Interaction epic 全量架构迁移（2026-04-29）将所有 `Evt_*` 常量 + Payload struct 协议替换为单一 `IInteractionEvent` 接口（ADR-027）。本 story（S2-08）落地 ADR-013 的纯 C# `InteractableObjectFsm` + `InteractableObject` MonoBehaviour 绑定层，**禁止**引入任何 ADR-006 残留。本 evidence 文档为 AC-9 静态扫描证据，与 `InteractableObjectFsmTests.AC9_*` reflection sanity check 互补。

---

## 扫描命令 + 结果

### 1. 9 个禁用 EventId 常量

```bash
rg "EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected|TapGesture|DragGesture|RotateGesture|LightPositionChanged)" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
```

**Result**: `0 命中`

### 2. Payload struct 残留

```bash
rg "ObjectTransformChangedPayload|PuzzleLockAllPayload|ObjectSelectedPayload" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
```

**Result**: `0 命中`

### 3. ObjectInteraction 子目录单独验证（本 story 新增代码）

```bash
rg "EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected|TapGesture|DragGesture|RotateGesture|LightPositionChanged)" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
```

**Result**: `0 命中`

### 4. 正向验证：sender 调用使用 ADR-027 形态

```bash
rg "GameEvent\.Get<IInteractionEvent>\(\)\.On" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
```

**Expected hits（本 story 实装）**:
- `InteractableObjectFsm.cs` 中 3 处：`OnObjectSelected(ObjectId)`（OnTapHit Idle/Snapping 分支）、`OnObjectDeselected(ObjectId)`（OnDeselect / OnLockChanged from=Selected 分支）

### 5. 正向验证：listener 注册使用 per-method ADR-027 形态

```bash
rg "GameEvent\.AddEventListener<.+>\(IInteractionEvent_Event\." \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
```

**Expected hits**:
- `InteractableObject.cs` 中 1 处：`AddEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged)`

---

## 允许例外（不算 grep 命中）

- **测试 fixture / mock**：本 story 测试位于 `Assets/Tests/EditMode/ObjectInteraction/InteractableObjectFsmTests.cs`，仅使用 `IInteractionEvent_Event.OnObjectSelected` / `OnObjectDeselected` 的事件 hash ID 注册测试 listener，符合 ADR-027 协议。
- **注释 / TODO 引用**：实装代码注释中提及历史 `Evt_*` 名称属解释性描述，非真实调用（grep 模式精确匹配 `EventId\.Evt_*` 形式，注释中的纯字面 "Evt_*" 不命中）。

---

## CI 集成建议

将以下 grep 模式加入 GitHub Actions / Unity Cloud Build 步骤，命中即 fail（与 S2-07 `grep-no-hardcoded-scene-names` 同模式）：

```yaml
- name: ADR-027 protocol compliance grep gate
  run: |
    set -e
    OUT=$(rg -l \
      'EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected|TapGesture|DragGesture|RotateGesture|LightPositionChanged)' \
      src/MyGame/ShadowGame/Assets/GameScripts/HotFix/ || true)
    if [ -n "$OUT" ]; then
      echo "❌ ADR-027 violation: ADR-006 Evt_* constants found in:"; echo "$OUT"; exit 1
    fi
    OUT=$(rg -l \
      'ObjectTransformChangedPayload|PuzzleLockAllPayload|ObjectSelectedPayload' \
      src/MyGame/ShadowGame/Assets/GameScripts/HotFix/ || true)
    if [ -n "$OUT" ]; then
      echo "❌ ADR-027 violation: legacy Payload struct found in:"; echo "$OUT"; exit 1
    fi
    echo "✅ ADR-027 protocol compliance: PASS"
```

---

## 关联

- **Story**: `production/epics/object-interaction/story-001-interaction-state-machine.md` AC-9
- **ADR**: ADR-013（**Accepted** 2026-04-29）+ ADR-027 GameEvent Interface Protocol
- **Test counterpart**: `Assets/Tests/EditMode/ObjectInteraction/InteractableObjectFsmTests.cs::AC9_IInteractionEvent_HasEventInterfaceAttribute_GroupLogic`
- **同模式先例**: `production/qa/grep-no-hardcoded-scene-names-2026-04-29.md`（S2-07）
