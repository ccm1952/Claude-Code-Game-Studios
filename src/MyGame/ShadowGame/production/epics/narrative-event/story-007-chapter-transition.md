// 该文件由Cursor 自动生成

# Story 007 — Chapter Transition (Chapter End → Transition Sequence → Load Next Chapter)

> **Epic**: narrative-event
> **Type**: Integration
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 章节过渡演出流程（典型）|
| **TR-IDs** | TR-narr-006, TR-narr-007 |
| **ADR** | ADR-016 (ChapterTransition sequence type), ADR-009 (Scene Lifecycle) |
| **Finding** | SP-008 — 章末谜题通过 chain 无缝衔接章节过渡序列 |
| **Engine** | Unity 2022.3.62f2 LTS / Unity Timeline (PlayableDirector) / TEngine Scene Module |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-2.5 (ADR-009)**: 章节切换通过 `GameEvent.Send(Evt_RequestSceneChange, payload)` 触发，不直接调用 SceneManager
- **CM-2.5**: `LoadSceneMode.Additive`（禁止使用 `LoadSceneMode.Single`）
- **CM-2.5**: 场景名通过 Luban `TbChapter.sceneId` 获取，不硬编码
- **CM-4.1**: `TimelinePlayable` effect 在 `ChapterTransition` 序列中使用，支持全屏黑边电影模式
- **CM-4.1**: 若 Timeline 资源加载失败 → 跳过 Timeline，执行 ScreenFade 后直接触发场景切换

---

## Acceptance Criteria

1. **AC-001**: `NarrativeSequenceEngine` 监听 `Evt_ChapterComplete`；收到后查询 `TbChapterTransitionMap.Get(chapterId)` 获取过渡序列 ID
2. **AC-002**: 章节过渡序列（`sequenceType=ChapterTransition`）执行完毕后，发送 `Evt_LoadNextChapter{nextChapterId}`，由 Scene Management 执行场景切换
3. **AC-003**: `TimelinePlayable` 效果正确播放（黑边 letterbox 比例 0.21）；Timeline 完成回调触发后，序列继续执行后续效果
4. **AC-004**: Timeline 资源加载失败 → 跳过 Timeline effect，执行 ScreenFade 后发送 `Evt_LoadNextChapter`（不卡死）
5. **AC-005**: 章末最后谜题通过 SP-008 chain 无缝衔接：`ch3_puzzle5_complete → ch3_chapter_transition`（nextSequenceDelay=0，无黑屏间隙）
6. **AC-006**: 章节过渡期间 InputBlocker 持续激活（玩家无法操作），切换完成后解除
7. **AC-007**: 序列完成后 `nextChapterId` 从 Luban `TbChapterTransitionMap` 读取，不硬编码

---

## Implementation Notes

### ChapterComplete 监听

```csharp
// Init() 中注册
GameEvent.AddEventListener<ChapterCompletePayload>(EventId.Evt_ChapterComplete, OnChapterComplete);

private void OnChapterComplete(ChapterCompletePayload payload)
{
    var map = Tables.Instance.TbChapterTransitionMap.Get(payload.ChapterId);
    if (map == null)
    {
        Log.Warning($"[Narrative] No transition for chapter {payload.ChapterId}");
        return;
    }
    TryEnqueue(map.SequenceId);
}
```

### 章节切换触发（序列完成后）

```csharp
// 在序列的 OnSequenceComplete 中（当最后一个 effect 执行完）：
// 序列类型为 ChapterTransition 时，触发场景切换
GameEvent.Send(EventId.Evt_RequestSceneChange, new SceneChangePayload
{
    NextChapterId = _chapterTransitionConfig.NextChapterId
});
```

### TimelinePlayable Effect 实现

```csharp
public class TimelinePlayableEffect : IAtomicEffect
{
    private PlayableDirector _director;
    private bool _isComplete;

    public bool IsComplete => _isComplete;

    public async void Start()
    {
        // 异步加载 Timeline 资源
        var handle = await GameModule.Resource.LoadAssetAsync<PlayableAsset>(_timelinePath);
        if (handle == null) { _isComplete = true; return; }  // 加载失败，跳过

        _director = _letterboxCamera.GetComponent<PlayableDirector>();
        _director.playableAsset = handle.Result;
        // 激活 letterbox UI（21:9 黑边）
        _letterboxUI.SetActive(true);
        _director.stopped += _ => { _isComplete = true; _letterboxUI.SetActive(false); };
        _director.Play();
    }
}
```

---

## Out of Scope

- Scene Management 的具体实现（属于 foundation layer，ADR-009）
- Timeline 资源的内容制作（属于 narrative/art team）
- 最终章（Ch.5 → Credits）特殊处理（配置表中指定 sequenceId 即可，引擎无需特殊代码）

---

## QA Test Cases

### TC-001: ChapterComplete → 过渡序列启动

**Given**: ChapterId=1 在 TbChapterTransitionMap 中有映射  
**When**: `Evt_ChapterComplete{chapterId=1}` 发送  
**Then**: 对应的过渡序列启动，ScreenFade 黑屏效果开始

### TC-002: 章节切换在序列完成后触发

**Given**: ChapterTransition 序列播放完毕  
**When**: 最后一个 effect 结束  
**Then**: `Evt_RequestSceneChange{nextChapterId=2}` 发送，场景切换流程启动

### TC-003: Timeline 资源失败降级

**Setup**: 将 Timeline 资源路径设置为无效  
**Verify**: 序列播放时 Timeline effect 跳过，其他效果（ScreenFade）正常执行  
**Pass**: 最终 `Evt_RequestSceneChange` 仍被发送，场景正常切换；`Log.Warning` 记录 Timeline 失败

### TC-004: SP-008 Chain 无间隙衔接

**Given**: ch3_puzzle5_complete.nextSequenceId="ch3_chapter_transition", nextSequenceDelay=0  
**When**: ch3_puzzle5_complete 序列结束  
**Then**: ch3_chapter_transition 立即开始（同一帧），无黑屏间隙（用录屏验证帧间隔）

### TC-005: 场景名不硬编码

**Given**: NarrativeSequenceEngine 和 ChapterTransition 相关代码  
**When**: 搜索硬编码场景名（如 "Chapter2Scene"）  
**Then**: 不存在任何硬编码场景名；场景名通过 TbChapter.sceneId 动态获取

---

## Test Evidence

- **Integration Test**: `tests/integration/NarrativeEngine_ChapterTransition_Test.cs`
- **Evidence Doc**: `production/qa/evidence/story-007-chapter-transition.md`（Timeline letterbox 截图，chain 无缝录屏）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Sequence Engine) | Base | `TryEnqueue()` + 序列播放基础 |
| story-003 (Sequence Chain) | Integration | SP-008 chain 衔接章末谜题与过渡序列 |
| story-004 (Luban Config) | Blocking | TbChapterTransitionMap |
| ADR-009 (Scene Lifecycle) | Architecture | `Evt_RequestSceneChange` 触发场景切换 |
| chapter-state epic | Integration | `Evt_ChapterComplete` 发送方 |
| Unity Timeline / PlayableDirector | Engine API | TimelinePlayable effect |
