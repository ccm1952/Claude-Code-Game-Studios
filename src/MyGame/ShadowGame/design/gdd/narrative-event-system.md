<!-- 该文件由Cursor 自动生成 -->
# Narrative Event System — 叙事事件系统

> **Status**: Draft
> **Author**: Narrative Design Agent
> **Last Updated**: 2026-04-21
> **Last Verified**: 2026-04-21
> **Implements Pillar**: 关系即谜题 — 每一次演出都在揭示一段关系；克制表达 — 用画面和声音而非文字叙事

## Summary

叙事事件系统是《影子回忆》中连接玩法与故事的桥梁。它在谜题 PerfectMatch 时驱动"记忆重现演出"（色温变化、物件动画、环境音切换、纹理视频、Timeline 过场），在章节结束时驱动章节过渡演出。所有演出由可组合的原子效果配置表驱动，不硬编码任何演出流程，确保后续内容迭代时可快速调整。

> **Quick reference** — Layer: `Feature` · Priority: `Vertical Slice` · Key deps: `Shadow Puzzle System, Chapter State System, Audio System`

## Overview

当玩家成功拼出一个关系影子（PerfectMatch），游戏不只是弹出"成功"——而是让整个场景短暂地"活"过来。灯光色温从中性暖黄变为记忆中的橘红，某些物件轻轻移动到它们"本来应该在的位置"，远处传来一段声音片段（雨声、杯碰杯声、椅子拖动声），墙面上可能浮现一段半透明的纹理视频。这些"记忆重现"不是过场动画——它们发生在玩家刚刚操作的那个场景里，让刚刚还是谜题道具的物件突然变成了"有人用过的东西"。章节收束时，系统通过 Timeline 控制的全屏过场动画（黑边电影模式）完成章节过渡。

## Player Fantasy

**"影子成形的瞬间，整个房间都在回忆。"**

玩家把最后一只杯子放到正确的位置。影子在墙面上缓缓合拢——然后灯光微微变暖了。桌上的第二只杯子（那只一直空着的）似乎轻轻移动了一下，像是有人刚放下来。远处隐约传来杯碰杯的声音。墙面上，影子的轮廓里浮现出一小段朦胧的画面——两个人坐在这张桌子旁。三秒钟后一切恢复安静。但房间的感觉不一样了。这不是一个"关卡完成"的反馈，而是一个"我理解了这段记忆"的时刻。

## Detailed Design

### Core Rules

**演出触发条件：**

1. **记忆重现演出（Memory Replay）**：仅由 `PerfectMatchEvent` 触发，NearMatch 不触发演出
2. **章节过渡演出（Chapter Transition）**：由 `ChapterCompleteEvent` 触发（一章所有必需谜题完成后）
3. 触发时 Object Interaction 系统收到 `PuzzleLockAllEvent`，所有物件进入 Locked 状态
4. 演出期间玩家输入被 `InputBlocker` 阻断
5. 每个谜题/章节的演出内容通过配置表指定，同一触发点的演出可随时在配置表中修改

**原子效果类型（Atomic Effect）：**

系统定义以下可组合的原子效果，每个演出由若干原子效果按时间轴编排：

| Effect Type | ID | Description | Parameters |
|-------------|-----|------------|------------|
| **ColorTemperature** | `color_temp` | 场景灯光色温渐变 | targetColor, duration, easing |
| **ObjectAnimate** | `obj_anim` | 指定物件播放预设动画（位移/旋转/缩放） | objectId, targetTransform, duration, easing |
| **AmbientAudioSwitch** | `ambient_switch` | 环境音切换或叠加一层临时音轨 | audioClipId, volume, fadeInDuration, holdDuration, fadeOutDuration |
| **SFXOneShot** | `sfx_oneshot` | 播放一次性音效 | sfxId, delay, volume |
| **AudioDucking** | `audio_duck` | 压低环境音和音乐 | duckRatio, fadeDuration |
| **TextureVideo** | `tex_video` | 在指定 UI 或场景表面播放纹理视频 | videoClipPath, targetRenderer/uiRawImage, fadeInDuration, holdDuration, fadeOutDuration, alpha |
| **TimelinePlayable** | `timeline_play` | 播放 Unity Timeline 过场动画（全屏接管，黑边电影模式） | timelineAssetPath, letterboxRatio |
| **ScreenFade** | `screen_fade` | 全屏淡入淡出（黑屏/白屏） | fadeColor, fadeInDuration, holdDuration, fadeOutDuration |
| **Wait** | `wait` | 等待指定时长后继续下一个效果 | duration |
| **ObjectSnapToTarget** | `obj_snap` | 驱动物件自动吸附到指定目标位置 | objectId, targetPosition, targetRotation, duration, easing |
| **ShadowFade** | `shadow_fade` | 控制指定锚点影子区域的 alpha 渐变（用于缺席型谜题的影子残缺/消散效果） | anchorId, targetAlpha, duration, easing |
| **ObjectFade** | `obj_fade` | 控制指定物件的整体 alpha 渐变（用于缺失物件的淡出效果） | objectId, targetAlpha, duration, easing |

**演出编排（Sequence）：**

1. 每个演出由一个 `NarrativeSequence` 定义，包含一组按时间排序的原子效果
2. 原子效果有 `startTime`（相对于序列开始的偏移），支持并行（多个效果相同 startTime）
3. 配置表结构：`puzzleId/chapterId → NarrativeSequenceId → List<AtomicEffect>`
4. 一个 `NarrativeSequence` 可被多个谜题/章节复用
5. 序列播放完毕后自动触发 `NarrativeSequenceCompleteEvent`，解除 InputBlocker 和物件 Lock

**记忆重现演出流程（典型）：**

1. `PerfectMatchEvent` → 系统查询配置表获取对应的 `NarrativeSequenceId`
2. 发送 `PuzzleLockAllEvent` + push `InputBlocker`
3. 按时间轴依次执行原子效果：
   - t=0.0s: `AudioDucking(0.3, 0.5s)` — 压低环境音
   - t=0.0s: `ObjectSnapToTarget` — PerfectMatch 物件吸附归位
   - t=0.5s: `ColorTemperature(warmOrange, 1.0s, EaseInOut)` — 色温变暖
   - t=0.8s: `SFXOneShot(memory_sfx_01)` — 记忆音效
   - t=1.0s: `TextureVideo(memory_video_01, wallRenderer, 0.5s, 3.0s, 0.5s, 0.6)` — 墙面纹理视频
   - t=5.0s: `ColorTemperature(neutral, 1.0s, EaseInOut)` — 色温恢复
   - t=5.5s: `AudioDucking(1.0, 0.5s)` — 恢复环境音
4. 序列结束 → pop `InputBlocker` + 发送 `PuzzleUnlockEvent`

**缺席型谜题演出流程（Ch.5 专用）：**

1. `AbsenceAcceptedEvent(puzzleId)` → 系统查询配置表获取对应的缺席专用 `NarrativeSequenceId`
2. 发送 `PuzzleLockAllEvent` + push `InputBlocker`
3. 按时间轴执行原子效果（不执行标准物件吸附）：
   - t=0.0s: `AudioDucking(0.3, 0.5s)` — 压低环境音
   - t=0.0s: `ShadowFade(absenceAnchorIds, 0.05, 0.8s, EaseIn)` — 缺口处影子 alpha 渐降
   - t=0.5s: `ColorTemperature(coolBlue, 1.0s, EaseInOut)` — 色温偏冷（与标准 PerfectMatch 的暖色相反）
   - t=1.0s: `SFXOneShot(absence_sfx_01)` — 静谧的空白感音效
   - t=1.5s: `TextureVideo(absence_memory_01, wallRenderer, 0.5s, 2.0s, 1.0s, 0.4)` — 更短、更淡的记忆画面
   - t=4.5s: `ColorTemperature(neutral, 1.0s, EaseInOut)` — 色温恢复
   - t=5.0s: `AudioDucking(1.0, 0.5s)` — 恢复环境音
4. 序列结束 → pop `InputBlocker` + 发送 `PuzzleUnlockEvent`

> **设计意图**：缺席型演出与标准记忆重现的关键差异——色温偏冷而非偏暖，TextureVideo 更短更淡（holdDuration 2s vs 3s，alpha 0.4 vs 0.6），无 ObjectSnapToTarget（物件留在当前位置），增加 ShadowFade 驱动缺口影子渐隐。整体情绪基调从"重逢的温暖"转向"接受的释然"。

**章节过渡演出流程（典型）：**

1. `ChapterCompleteEvent` → 查询配置表获取章节过渡序列
2. 发送 `PuzzleLockAllEvent` + push `InputBlocker`
3. 执行序列：
   - t=0.0s: `ScreenFade(black, 1.0s, 0s, 0s)` — 黑屏淡入
   - t=1.0s: `TimelinePlayable(chapter_transition_01, 0.21)` — 全屏过场（21:9 黑边）
   - t=Timeline结束: `ScreenFade(black, 0s, 0.5s, 1.5s)` — 黑屏保持后淡出
4. 序列结束 → 触发 `LoadNextChapterEvent` → Scene Management 执行场景切换

**章末最后谜题的演出衔接优化：**

章末最后一个谜题的记忆重现演出与章节过渡演出可无缝衔接以减少非交互时间（目标 ~10s，与 Florence 章节转场一致）：
- 记忆重现演出的最后一个 `ColorTemperature(neutral)` 可直接衔接为章节过渡的色温变化，省去先恢复再变化的两次渐变
- 章末谜题的 `PuzzleCompletePanel` 的关系暗示语可整合到章节过渡演出的标题显示阶段，省去独立面板的 ~3s 显示时间
- 配置表中通过 `isChapterFinalPuzzle` 标记区分，使用专用的合并序列

**配置表驱动：**

1. 所有演出内容定义在 Luban 配置表中，格式：

```
NarrativeSequenceConfig:
  sequenceId: string
  effects:
    - effectType: string (enum)
      startTime: float
      params: dict<string, any>
```

2. 谜题→序列映射表：`PuzzleNarrativeMap: puzzleId → sequenceId`
3. 章节→过渡映射表：`ChapterTransitionMap: chapterId → sequenceId`
4. 修改演出只需改配置表，不需要改代码

### States and Transitions

**叙事事件系统状态机：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Idle** | 默认 / 序列播放完毕 | 收到 PerfectMatchEvent 或 ChapterCompleteEvent | 不执行任何演出，监听事件 |
| **Playing** | 从 Idle 收到触发事件 | 序列中所有效果执行完毕 | 按时间轴执行原子效果序列，InputBlocker 激活 |
| **WaitingForTimeline** | Playing 中执行到 TimelinePlayable | Timeline 播放完成回调 | 等待 Unity Timeline 播放完毕 |

**状态转换规则补充：**

- Playing 期间收到新的触发事件 → 排队（enqueue），当前序列完成后播放下一个
- Playing 期间应用切后台 → 暂停序列计时器，切回后恢复
- 序列中某个原子效果的资源加载失败 → 跳过该效果，继续执行后续效果，Log.Warning

### Interactions with Other Systems

**与 Shadow Puzzle System 的交互（上游触发）：**

- 输入：`PerfectMatchEvent(puzzleId)` → 查配置表 → 播放对应记忆重现序列
- 输出：`PuzzleLockAllEvent` / `PuzzleUnlockEvent`（锁定/解锁物件）。**多发送者说明**：`PuzzleLockAllEvent` 有两个合法发送者——Shadow Puzzle System 在 PerfectMatch/AbsenceAccepted 判定时发送，Narrative Event System 在演出开始时发送。Object Interaction 只需监听事件，不关心发送者身份。`PuzzleUnlockEvent` 同理
- 输出：`ObjectSnapToTargetEvent(objectId, targetTransform)`（演出中的物件归位）

**与 Chapter State System 的交互（上游触发）：**

- 输入：`ChapterCompleteEvent(chapterId)` → 查配置表 → 播放章节过渡序列
- 输出：`LoadNextChapterEvent(nextChapterId)`（过渡完成后触发场景切换）

**与 Audio System 的交互（下游控制）：**

- 输出：`AudioDuckingRequest` / `MusicChangeRequest` / `PlayOneShotRequest`
- 演出中的音频效果通过这些指令委托给 Audio System 执行

**与 Input System 的交互：**

- 演出开始时 push `InputBlocker`，演出结束时 pop
- 确保演出期间玩家无法操作物件

**与 UI System 的交互：**

- `TextureVideo` 效果可在 UI RawImage 上播放
- `ScreenFade` 使用全屏 UI 遮罩层
- `TimelinePlayable` 激活黑边 letterbox UI

**与 Scene Management 的交互：**

- 章节过渡演出完成后发送 `LoadNextChapterEvent`，由 Scene Management 执行实际的场景加载

## Formulas

### Sequence Timeline — 序列时间轴计算

```
for each effect in sequence.effects (sorted by startTime):
    if elapsed >= effect.startTime and not effect.started:
        effect.Start()
        effect.started = true
    if effect.started:
        effect.Update(elapsed - effect.startTime)
```

### Color Temperature Lerp — 色温插值

```
currentColor = Color.Lerp(originalColor, targetColor, eased_t)
eased_t = EaseInOutQuad(clamp(elapsed / duration, 0, 1))
EaseInOutQuad(t) = t < 0.5 ? 2t² : 1 - (-2t+2)²/2
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| originalColor | Color | — | runtime | 演出开始时的灯光颜色 |
| targetColor | Color | — | config | 目标色温颜色 |
| duration | float | 0.5-3.0s | config | 色温渐变时长 |

### Texture Video Alpha — 纹理视频透明度

```
phase 1 (fadeIn):  alpha = targetAlpha × clamp(elapsed / fadeInDuration, 0, 1)
phase 2 (hold):    alpha = targetAlpha
phase 3 (fadeOut): alpha = targetAlpha × (1 - clamp((elapsed - fadeIn - hold) / fadeOutDuration, 0, 1))
totalDuration = fadeInDuration + holdDuration + fadeOutDuration
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| targetAlpha | float | 0.3-0.8 | config | 视频最大透明度（半透明叠加在场景上） |
| fadeInDuration | float | 0.3-1.0s | config | 淡入时长 |
| holdDuration | float | 1.0-5.0s | config | 持续显示时长 |
| fadeOutDuration | float | 0.3-1.0s | config | 淡出时长 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 配置表中某谜题无对应序列 | 跳过演出，直接发送 PuzzleUnlockEvent | 开发中可能有尚未配置演出的谜题 |
| 演出中引用的 objectId 不存在 | 跳过该 ObjectAnimate/SnapToTarget 效果 | 场景物件可能在后续版本变化 |
| 演出中视频资源加载失败 | 跳过 TextureVideo 效果，其他效果继续 | 视频是装饰性的，不可阻塞 |
| Timeline 资源加载失败 | 跳过 Timeline，执行 ScreenFade 淡出后直接触发场景切换 | 保证章节过渡不卡死 |
| 演出序列时长为 0（无效果） | 立即触发完成事件 | 空序列等同于无演出 |
| 连续两个 PerfectMatch 快速触发 | 第二个排队等第一个完成 | 避免演出叠加混乱 |
| 玩家使用 Layer 3 提示后完成谜题 | **可选差异化**（非惩罚性）：记忆重现演出的 TextureVideo `holdDuration` 缩短（如 3.0s → 2.0s），保留色温变化和音效。暗示"你得到了答案，但记忆的浮现没有那么深刻"。由 Hint System 提供 `usedLayer3Count` 标记，Narrative 查询后选择标准序列或缩短序列。此功能为可选——若测试显示差异化让玩家感觉被惩罚则不启用 | 使用提示和自己发现答案的体验深度本就不同，但绝不应让玩家觉得"因为用了提示所以被罚" |
| 演出期间应用切后台 | 暂停序列计时器和所有进行中效果，切回后恢复 | 保持演出完整性 |
| 章节过渡演出中 Timeline 含音频 | Timeline 音频独立于 Audio System 的 ducking | Timeline 是全屏接管模式，音频自包含 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Shadow Puzzle System | SP triggers this | PerfectMatchEvent 驱动记忆重现演出 |
| Chapter State System | CS triggers this | ChapterCompleteEvent 驱动章节过渡演出 |
| Audio System | This controls Audio | ducking/音效/音乐切换指令 |
| Input System | This controls Input | InputBlocker push/pop |
| Object Interaction System | This controls OI | PuzzleLockAll/Unlock/SnapToTarget |
| UI System | This uses UI | ScreenFade 遮罩、TextureVideo 显示、Letterbox |
| Scene Management | This triggers SM | LoadNextChapterEvent |
| Luban 配置表 | This reads from | 序列定义、谜题/章节映射 |
| Unity Timeline | This plays | 过场动画资源 |
| Unity VideoPlayer | This plays | 纹理视频资源 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| defaultDuckRatio | 0.3 | 0.1-0.6 | 演出时环境音更响 | 演出时环境音更安静 |
| defaultDuckFade | 0.5s | 0.2-1.5s | ducking 渐变更慢 | ducking 渐变更快 |
| defaultColorTempDuration | 1.0s | 0.5-3.0s | 色温变化更慢更柔和 | 色温变化更快更突然 |
| defaultTextureVideoAlpha | 0.6 | 0.3-0.8 | 纹理视频更清晰 | 纹理视频更朦胧 |
| letterboxRatio | 0.21 (21:9) | 0.15-0.25 | 黑边更厚（更电影感） | 黑边更薄 |
| queueMaxSize | 3 | 1-5 | 允许更多排队演出 | 排队满时丢弃新演出 |

## Acceptance Criteria

- [ ] PerfectMatch 触发后 ≤ 1 帧开始执行演出序列
- [ ] 演出期间所有物件 Locked，玩家输入被阻断
- [ ] 演出结束后物件自动 Unlock，玩家可立即操作
- [ ] 色温变化平滑无突变，从开始到结束无闪烁
- [ ] 纹理视频淡入淡出无爆闪，alpha 过渡平滑
- [ ] Timeline 过场动画以黑边电影模式全屏播放
- [ ] 章节过渡演出结束后自动触发场景切换
- [ ] 所有演出内容从 Luban 配置表读取，修改配置表后无需重新编译
- [ ] 演出中某个效果资源缺失时不崩溃，跳过继续
- [ ] 连续快速触发两个 PerfectMatch，第二个正确排队并在第一个结束后播放

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 纹理视频的分辨率和编码格式对移动端性能的影响 | Tech Lead | VS 制作前 | 需要真机测试 H.264 vs VP8 的解码开销 |
| 是否需要支持跳过演出（长按跳过/二次游玩时自动跳过）？ | Game Design | VS 测试阶段 | 首次游玩强制观看，二次游玩待定 |
| 第五章"缺席型谜题"的演出是否需要特殊的原子效果类型？ | Narrative Design | Alpha 设计阶段 | 可能需要"物件消失"或"影子残缺"效果 |
| Luban 配置表中 dict 类型的参数序列化方案 | Tech Lead | VS 开发前 | 可能用 JSON string 或拆为子表 |
