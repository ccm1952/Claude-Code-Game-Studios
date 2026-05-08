// 该文件由Cursor 自动生成

# Consistency Failures Reflexion Log

> **Purpose**: append-only log of cross-document consistency failures detected by `/architecture-review` and other governance skills. Use this file to surface recurring patterns and anti-patterns across the architecture.
>
> **Discipline**:
> - **Append only** — never modify or delete past entries
> - Only **CONFLICT** entries are logged here (not GAP — missing ADRs are expected during architecture growth)
> - Each entry has `Domain`, `Documents involved`, `What happened`, `Resolution`, `Pattern` (generalised lesson)
>
> **Created**: 2026-05-06 (post Sprint 3 governance closure / ADR-029 V2.0)

---

## Index

| Date | Conflict ID | Domain | Severity | Resolution |
|------|-----------|--------|:--------:|-----------|
| 2026-05-06 | CONFLICT-008 | Event Protocol (intra-ADR) | MINOR | Fixed in same session — ADR-011 §7 sample patched to ADR-027 form + null-out + null-check guard |

---

### 2026-05-06 — /architecture-review — 🔴 CONFLICT-008

**Domain**: Event Protocol / Intra-ADR Sample Drift

**Documents involved**: `docs/architecture/adr-011-uiwindow-management.md` §7 vs §8 (same ADR, different sections)

**What happened**: ADR-011 §7 ("通信约束" Implementation Guidelines code block, lines 222-224) still showed legacy ADR-006 const-int sample:

```csharp
GameEvent.AddEventListener(EventId.ChapterCompleted, OnChapterCompleted);
GameEvent.RemoveEventListener(EventId.ChapterCompleted, OnChapterCompleted);
```

This contradicted the same ADR's §"8. UI GameEvent 接口（遵循 ADR-027）" which explicitly mandates the `[EventInterface]` interface protocol and states "本 ADR 不再定义独立的 `UIEventId` 常量类". The §7 sample was leftover from the 2026-04-22 ADR draft, missed during the 2026-04-23 ADR-027 supersession revision.

Additionally missed: ADR-027 §5 ⚠️ Framework knowledge fact (2026-04-30 lite propagation v2 result) — the sample also did not show the required null-out + null-check guard pattern (TEngine `RemoveEventListener` is non-idempotent; raw double-remove throws `Delete handle failed, not exist`).

**Impact**: Developers reading ADR-011 §7 first would copy the legacy const-int pattern (auto-blocked at ADR-029 R1 readiness gate, ~5-10 min drift per story). Also propagates ignorance of the ADR-027 §5 framework knowledge fact through any UI-system reference flow.

**Resolution**: 2026-05-06 same session — replaced ADR-011 §7 sample (lines 222-224) with the ADR-027-compliant pattern including ADR-027 §5 null-out + null-check guard:

```csharp
// 跨系统事件（通过 GameEvent — 遵循 ADR-027 §5 framework knowledge fact）
private Action<int> _onChapterCompleted;

public override void RegisterEvent()
{
    _onChapterCompleted = OnChapterCompleted;
    GameEvent.AddEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, _onChapterCompleted);
}

private void OnChapterCompleted(int chapterId) { /* ... */ }

// Cleanup 路径（OnDisable / Dispose 等）— null-out + null-check guard
public void Cleanup()
{
    if (_onChapterCompleted != null)
    {
        GameEvent.RemoveEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, _onChapterCompleted);
        _onChapterCompleted = null;
    }
}
```

**Pattern (generalised lesson)**:

> **ADR supersession revisions must scrub all code samples in superseded sections, not just header text.**
>
> When ADR-006 was superseded by ADR-027 (2026-04-23), the supersession note was added to ADR-011 §8 but ADR-011 §7's code samples were not updated. The intra-ADR drift is invisible to standard cross-ADR conflict detection (which compares ADR pairs, not within a single ADR).
>
> **For future supersession revisions**: include explicit code-sample scrub step in propagation playbook (ADR-029 V2.0 §V2-4 lite propagation v2). Grep within the affected ADR for the deprecated symbols (e.g., `EventId\.[A-Z]\w+` const-int pattern) before considering supersession complete.
>
> **Detection signal**: `/architecture-review` Phase 4 should grep each ADR for legacy patterns from any superseded ADR (intra-ADR drift detection, in addition to cross-ADR conflict detection). Add this to V3 升级 触发条件 if drift recurs.

---

*Future entries appended below*
