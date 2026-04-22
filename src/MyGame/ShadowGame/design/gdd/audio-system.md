<!-- 该文件由Cursor 自动生成 -->
# Audio System — 音频系统

> **Status**: Draft
> **Author**: Audio Design Agent
> **Last Updated**: 2026-04-21
> **Last Verified**: 2026-04-21
> **Implements Pillar**: 克制表达 — 声音是氛围的一部分，不是信息的载体

## Summary

音频系统是《影子回忆》情绪包裹的核心基础设施。它管理三类声音：全局统一的环境音（持续营造安静的室内氛围）、与物件交互绑定的音效（传达"日常即重量"的触感）、每章独立的氛围音乐（跟随关系弧线的情绪基调）。系统基于 TEngine AudioModule 构建，通过配置表驱动音频事件的播放/淡入淡出/混合，不硬编码任何音频逻辑。

> **Quick reference** — Layer: `Feature` · Priority: `Vertical Slice` · Key deps: `None (independent infrastructure)`

## Overview

玩家在安静的室内场景中操作物件和光源。环境音始终存在——远处的雨声、窗外模糊的街景、室内的空调低鸣——它们构成"安静"本身。物件交互产生的音效（陶瓷碰桌面、布料轻轻拖过木头）是游戏中最接近"对话"的声音。每章有自己的氛围音乐，不是旋律化的配乐，而是情绪性的声景（drone、和声垫、偶发的钢琴单音），跟随关系弧线从温暖走向冷寂。系统的设计目标是让玩家在摘下耳机后仍能感受到房间里的安静——因为声音从未打破过它。

## Player Fantasy

**"这个房间好安静，但不是空的。"**

耳机里传来轻微的雨声和某种说不清的嗡鸣。手指拖动陶瓷杯时听到了桌面的摩擦声——很轻，像是真的在移动一只杯子。杯子放下的那一声"嗒"让人觉得这只杯子是真实的。当影子终于拼合成功，钢琴的单音从远处飘来，像是从记忆深处浮起的一个音符。没有管弦乐高潮，没有胜利号角，只是一个单音——但恰好在那个瞬间，它就是整个世界。

## Detailed Design

### Core Rules

**音频分类与混合层级：**

1. 系统管理三个独立的混合层（Mix Layer），每层有独立的音量控制：
   - **Ambient Layer** — 环境音：全局统一，跨章节持续播放，循环无缝衔接
   - **SFX Layer** — 音效：物件交互、谜题反馈、UI 操作等一次性音频事件
   - **Music Layer** — 氛围音乐：每章一首循环曲，章节切换时交叉淡入淡出
2. 三个层的最终输出音量 = 层基础音量 × 全局音量（Master Volume）
3. 每个层可独立静音，不影响其他层

**环境音（Ambient）：**

1. 环境音在游戏启动后自动播放，退出游戏时停止
2. 全局统一一套环境音，不随章节变化
3. 环境音由 2-3 个子轨叠加组成（如：雨声 + 室内低频 + 偶发的远处声响）
4. 子轨通过配置表定义：音频资源、基础音量、是否循环、随机偶发间隔
5. 偶发环境音（如远处的汽车声、鸟叫）按配置的时间间隔随机触发，每次随机选择变体
6. 环境音在演出（Narrative Event）期间可被降低音量（ducking），不停止

**音效（SFX）：**

1. 音效通过事件触发，每个事件对应一个音效 ID
2. 同一事件可配置多个变体（variant），播放时随机选择，避免重复感
3. 音效参数从配置表读取：音效 ID、资源路径列表（变体）、音量、音调随机范围、空间化类型
4. 支持两种空间化类型：
   - **2D**：UI 音效、全局反馈音，不受物件位置影响
   - **3D**：物件交互音效，音量和声像跟随物件世界坐标
5. 同一音效的并发数限制（maxConcurrent），超出时最旧的实例被淡出
6. 音效不跨帧排队——触发即播放，延迟 ≤ 1 帧

**音效事件列表（MVP + VS）：**

| 事件 ID | 触发时机 | 空间化 | 变体数 | 优先级 |
|---------|---------|--------|-------|--------|
| `sfx_object_select` | 物件选中 | 3D | 3（陶瓷/木质/金属） | MVP |
| `sfx_object_drag` | 物件拖拽中（循环，跟随移动） | 3D | 2（桌面/布面） | MVP |
| `sfx_object_snap` | 物件格点吸附 | 3D | 2 | MVP |
| `sfx_object_putdown` | 物件放下回弹 | 3D | 3（瓷碰桌/金属碰木/布落地） | MVP |
| `sfx_object_rotate` | 物件旋转中（循环） | 3D | 1 | MVP |
| `sfx_boundary_bump` | 物件到达边界 | 3D | 1 | MVP |
| `sfx_puzzle_nearmatch` | NearMatch 触发 | 2D | 1 | MVP |
| `sfx_puzzle_perfectmatch` | PerfectMatch 触发 | 2D | 1 | MVP |
| `sfx_hint_ambient` | Hint Layer 1 环境暗示 | 3D | 2 | VS |
| `sfx_hint_direction` | Hint Layer 2 方向引导 | 2D | 1 | VS |
| `sfx_chapter_transition` | 章节过渡 | 2D | 1 | VS |
| `sfx_ui_click` | UI 按钮点击 | 2D | 1 | MVP |
| `sfx_ui_back` | UI 返回/关闭 | 2D | 1 | MVP |

**氛围音乐（Music）：**

1. 每章对应一首氛围音乐，通过配置表映射：`chapterId → musicAssetPath`
2. 音乐在进入章节时自动播放，循环到章节结束
3. 章节切换时执行交叉淡入淡出（crossfade）：旧音乐淡出 + 新音乐淡入，时长可配
4. 演出期间音乐不停止，但可被 ducking（降低音量到指定百分比）
5. 暂停菜单打开时音乐不暂停，保持低音量播放维持氛围
6. 不支持音乐层级混合——每次只播放一首完整的音乐轨

### States and Transitions

**音频系统全局状态：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Normal** | 默认状态 | 进入 Ducking/Paused | 三层按配置音量正常播放 |
| **Ducking** | Narrative Event 请求 ducking | Narrative Event 结束 ducking | Ambient 和 Music 降低到配置的 ducking 比例，SFX 正常 |
| **Paused** | 应用切后台 | 应用切回前台 | 所有音频暂停，切回后从暂停点恢复 |

### Interactions with Other Systems

**与 Object Interaction System 的交互（上游）：**

- 监听物件状态变更事件（Selected/Dragging/Rotating/Snapping/Idle）
- 根据物件材质标签（ceramic/wood/metal/cloth）选择对应的音效变体
- 3D 音效位置跟随物件 Transform

**与 Shadow Puzzle System 的交互（上游）：**

- 监听 `NearMatchEvent` → 播放 `sfx_puzzle_nearmatch`
- 监听 `PerfectMatchEvent` → 播放 `sfx_puzzle_perfectmatch`

**与 Narrative Event System 的交互：**

- 接收 ducking 指令：`AudioDuckingRequest(duckRatio, fadeDuration)`
- 接收音乐切换指令：`MusicChangeRequest(musicId, crossfadeDuration)`
- 接收一次性演出音效指令：`PlayOneShotRequest(sfxId)`

**与 Chapter State System 的交互（上游）：**

- 监听章节切换事件 → 自动执行音乐交叉淡入淡出

**与 Settings 的交互：**

- 读取玩家设置：Master Volume、Music Volume、SFX Volume、SFX 开关
- `sfx_enabled` 开关仅控制 SFX 层的播放启停，Ambient 层不受此开关影响
- `ambientVolume = 0.6` 为内部设计基线值，不暴露给玩家设置界面。运行时 Ambient 层最终音量 = `clipBaseVolume × ambientBaseVolume(0.6) × masterVolume × duckingMultiplier`（不乘 sfxVolume）
- 设置变更时实时生效，不需要重启

**与 UI System 的交互：**

- 监听 UI 操作事件 → 播放对应 UI 音效
- 暂停菜单打开时不暂停音乐

## Formulas

### Final Volume — 最终播放音量

```
finalVolume = clipBaseVolume × layerVolume × masterVolume × duckingMultiplier
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| clipBaseVolume | float | 0-1 | AudioConfig (Luban) | 单个音频片段的基础音量 |
| layerVolume | float | 0-1 | Settings | 对应层（Ambient/SFX/Music）的玩家设置音量 |
| masterVolume | float | 0-1 | Settings | 全局主音量 |
| duckingMultiplier | float | 0-1 | runtime | Normal 状态=1.0，Ducking 状态=配置值（如 0.3） |

### Crossfade — 交叉淡入淡出

```
outVolume = currentMusicVolume × (1 - t)
inVolume = nextMusicVolume × t
t = clamp(elapsed / crossfadeDuration, 0, 1)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| crossfadeDuration | float | 1.0-5.0s | config | 交叉淡入淡出总时长 |
| elapsed | float | 0-crossfadeDuration | runtime | 已过时间 |

### Pitch Randomization — 音调随机化

```
playPitch = basePitch + Random.Range(-pitchVariance, pitchVariance)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| basePitch | float | 0.8-1.2 | AudioConfig | 基础音调 |
| pitchVariance | float | 0-0.15 | AudioConfig | 音调随机偏移范围 |

### Ambient Occasional Interval — 偶发环境音间隔

```
nextTriggerTime = lastTriggerTime + baseInterval + Random.Range(0, intervalVariance)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| baseInterval | float | 10-60s | AudioConfig | 偶发音基础间隔 |
| intervalVariance | float | 0-30s | AudioConfig | 间隔随机偏移 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 快速连续选中/放下同一物件 | 第二次音效正常播放，不等第一次播完 | maxConcurrent 控制，旧实例淡出 |
| 演出 ducking 期间玩家调低音量 | 取 ducking 后音量和玩家设置音量的较小值 | 玩家设置永远优先 |
| 章节切换瞬间触发 PerfectMatch 音效 | 音效正常播放，不被 crossfade 影响 | SFX 层独立于 Music 层 |
| 音效资源尚未加载完成 | 跳过本次播放，不阻塞游戏逻辑 | 音频是装饰性的，不可因资源问题卡死游戏 |
| 应用切后台再切回 | 所有音频从暂停点恢复，环境音无缝续播 | 避免回来后突然一声巨响 |
| 音效变体列表为空 | 静默处理，Log.Warning 但不抛异常 | 开发中可能有占位音效尚未填充 |
| 音量设为 0 | 该层所有音频 source 静音但不停止，避免恢复音量时重新播放 | 保持播放状态一致 |
| 多个 3D 音效同时播放 | 按距离排序，超出 maxConcurrent 的最远实例淡出 | 优先保留离玩家近的音效 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| TEngine AudioModule | This wraps | 底层音频播放、音源池管理 |
| Object Interaction System | OI triggers this | 物件交互事件驱动 SFX 播放 |
| Shadow Puzzle System | SP triggers this | 匹配事件驱动反馈音效 |
| Narrative Event System | NE controls this | ducking/音乐切换/演出音效 |
| Chapter State System | CS triggers this | 章节切换驱动音乐交叉淡入淡出 |
| Settings | Settings configures this | 音量/开关设置 |
| Luban 配置表 | This reads from | 音效事件定义、音乐映射、参数配置 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| masterVolume | 1.0 | 0-1 | 全局更响 | 全局更安静 |
| ambientVolume | 0.6 | 0-1 | 环境音更突出 | 环境音更隐蔽 |
| sfxVolume | 0.8 | 0-1 | 交互音效更清晰 | 交互音效更柔和 |
| musicVolume | 0.5 | 0-1 | 音乐更明显 | 音乐更背景化 |
| duckingRatio | 0.3 | 0.1-0.6 | ducking 时仍较响 | ducking 时几乎无声 |
| duckingFadeDuration | 0.5s | 0.2-1.5s | 淡入淡出更慢更柔和 | 淡入淡出更快更突然 |
| crossfadeDuration | 3.0s | 1.0-5.0s | 章节音乐过渡更长更平滑 | 过渡更快更明确 |
| sfxMaxConcurrent | 4 | 2-8 | 允许更多同时音效 | 减少音频压力 |
| pitchVariance | 0.05 | 0-0.15 | 音效更有随机感 | 音效更一致 |
| ambientOccasionalInterval | 20s | 10-60s | 偶发音更少更安静 | 偶发音更频繁 |

## Visual/Audio Requirements

> 本系统是音频系统自身，此处列出需要准备的音频资产。

| Asset Category | Count (Est.) | Format | Priority |
|---------------|-------------|--------|----------|
| 环境音子轨（循环） | 2-3 | .ogg, 44.1kHz, mono | VS |
| 偶发环境音变体 | 4-6 | .ogg, 44.1kHz, mono | VS |
| 物件交互音效（含变体） | ~20 | .ogg, 44.1kHz, mono | MVP |
| 谜题反馈音效 | 3-4 | .ogg, 44.1kHz, stereo | MVP |
| UI 音效 | 3-5 | .ogg, 44.1kHz, stereo | MVP |
| 章节氛围音乐 | 5 | .ogg, 44.1kHz, stereo | VS |
| 演出专用音效 | 按需 | .ogg | VS |

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 物件交互音效触发点 | `design/gdd/object-interaction.md` | Visual/Audio Requirements 表的 Audio Feedback 列 | Event mapping |
| 物件材质变体标签 | `design/art/art-bible.md` | 物件材质原则 | Data dependency |
| NearMatch/PerfectMatch 音效触发 | `design/gdd/shadow-puzzle-system.md` | 状态转换事件 | Event mapping |
| Hint 层音效触发 | `design/gdd/hint-system.md` | 提示激活事件 | Event mapping |
| 音量/开关设置 | `design/gdd/settings-accessibility.md` | 音频设置项 | Configuration |

## Acceptance Criteria

- [ ] 环境音在游戏启动后 2 秒内开始播放，无明显延迟
- [ ] 物件交互音效延迟 ≤ 1 帧（16ms at 60fps）
- [ ] 同一音效连续播放 5 次，不出现完全相同的两次（变体 + 音调随机化）
- [ ] 章节切换时音乐交叉淡入淡出平滑，无可感知的间断或爆音
- [ ] Ducking 生效时环境音和音乐平滑降低，结束时平滑恢复
- [ ] 应用切后台再切回，音频无爆音、无重复播放
- [ ] 音量设为 0 时完全无声，恢复后立即有声
- [ ] 所有音效事件 ID、变体、参数从 Luban 配置表读取，无硬编码
- [ ] 同时 10 个物件在场时，音频系统 CPU 开销 < 1ms/帧
- [ ] 音频资源总内存占用 < 30MB（全部加载时）

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 环境音的具体内容（雨声/街景/室内低频）是否需要根据游戏氛围重新定义？ | Audio Director | VS 制作前 | 待音频资产制作时确认 |
| 3D 音效的衰减模型参数（最小/最大距离、衰减曲线）需要真机调试 | Audio / QA | VS 真机测试 | 当前用 Unity 默认线性衰减，后续调优 |
| PerfectMatch 音效是否需要根据章节/谜题主题有不同变体？ | Game Design | VS 设计阶段 | 当前 MVP 统一一个，VS 可能按章节变体 |
