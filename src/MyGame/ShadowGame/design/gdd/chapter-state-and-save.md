<!-- 该文件由Cursor 自动生成 -->

# Chapter State System & Save System — 章节状态系统 & 存档系统

> **Status**: Draft
> **Author**: System Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 关系即谜题（章节弧线驱动谜题解锁）/ 克制表达（进度本身讲述关系变化）

## Summary

本文档定义《影子回忆》中两个紧密耦合的核心系统。**Chapter State System** 是全局进度的唯一权威来源，管理 5 章关系弧线中的章节解锁、谜题线性推进和回放功能。**Save System** 负责将运行时进度持久化到本地存储，支持断点续玩。两者通过 `IChapterProgress` 数据接口解耦：Chapter State 拥有运行时状态，Save System 仅读写该接口的快照，避免循环编译依赖。

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `相互依赖（通过 IChapterProgress 解耦）`

## Overview

玩家在《影子回忆》中经历 5 个章节，每章代表一段关系的不同阶段（靠近→共同空间→共同生活→松动→缺席与重新理解）。Chapter State System 控制哪些章节和谜题对玩家可见、可操作，并在谜题完成时推进全局进度。Save System 确保玩家关闭游戏后不丢失任何进度——下次打开时从上次离开的地方继续。这两个系统合在一起构成了游戏进度管线的完整闭环。

## Player Fantasy

玩家不应感知到"系统"的存在。他们只是"翻开下一页记忆"，或者"回到那个曾经的房间再看一次"。章节推进应该感觉像翻相册——自然、线性、带着期待。存档是隐形的——玩家从不需要"保存游戏"，一切都已被妥善记住，正如那些记忆本身。

---

# Part 1: Chapter State System

## Detailed Design

### Core Rules

**章节结构规则：**

1. 游戏包含 5 个章节，固定顺序，不可乱序解锁：
   - Chapter 1: 靠近（Approach）— 初始章节，默认解锁
   - Chapter 2: 共同空间（Shared Space）
   - Chapter 3: 共同生活（Living Together）
   - Chapter 4: 松动（Loosening）
   - Chapter 5: 缺席与重新理解（Absence & Understanding）
2. 每章包含 5-8 个谜题，由 Luban 配置表定义具体数量和内容
3. 章内谜题严格线性推进：必须按顺序完成，不可跳过
4. Chapter 1 在新游戏开始时自动解锁为 `ChapterActive`
5. 后续章节必须前一章完成后才解锁

**谜题推进规则：**

1. 每章第一个谜题在章节激活时自动解锁为 `Idle`
2. 每个谜题完成后，下一个谜题自动从 `Locked` 转为 `Idle`
3. 谜题完成状态不可逆——一旦 `Complete` 永远保持 `Complete`
4. 当一章所有谜题都达到 `Complete` 时，该章自动转为 `ChapterComplete`
5. 章节完成触发章末演出，演出结束后下一章解锁

**回放规则：**

1. 已完成章节（`ChapterComplete`）中的所有谜题可自由回放
2. 回放不影响主线进度——回放中的谜题状态独立于主进度
3. 回放从"章节选择"界面进入，选择具体谜题开始
4. 回放中的谜题初始化为 `Idle` 状态（而非 `Complete`），允许重新体验
5. 回放完成不触发叙事演出的再次播放（可配置）
6. 当前活跃章节（`ChapterActive`）不可回放——必须先完成

**进度权威规则：**

1. Chapter State System 是全局进度的唯一写入者
2. 其他系统（Shadow Puzzle、Narrative Event 等）只能通过 GameEvent 请求状态变更
3. Chapter State System 验证请求合法性后执行变更并广播结果
4. 任何绕过 Chapter State 直接修改进度的行为视为 bug

### States and Transitions

**章节状态机：**

```
ChapterLocked ──[前一章 Complete]──→ ChapterActive ──[所有谜题 Complete]──→ ChapterComplete
                                                                              │
                                                                              └──→ Replayable（可回放子状态）
```

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **ChapterLocked** | 初始状态（Chapter 2-5）| 前一章 `ChapterComplete` + 章末演出结束 | 章节选择 UI 中显示为锁定，不可进入 |
| **ChapterActive** | Chapter 1: 新游戏；Chapter 2-5: 前一章完成 | 该章所有谜题 `Complete` | 玩家当前所在章节，逐个推进谜题 |
| **ChapterComplete** | 该章最后一个谜题 `Complete` + 章末演出结束 | —（终态）| 章节选择 UI 中显示完成标记，所有谜题可回放 |

**谜题状态机（归 Chapter State 管理，Shadow Puzzle 驱动状态变更）：**

```
Locked ──[前序谜题 Complete]──→ Idle ──[玩家首次操作]──→ Active
                                                          │
                                          ┌───────────────┤
                                          │               ▼
                                          │          NearMatch
                                          │               │
                                          │    ┌──────────┤
                                          │    │          ▼
                                          │    │    PerfectMatch ──→ Complete
                                          │    │
                                          │    └── [匹配度降回 < 35%] ──→ Active
                                          │
                                          └── [匹配度直达 > 85%] ──→ PerfectMatch
```

| State | Entry Condition | Exit Condition | Behavior | 归属 |
|-------|----------------|----------------|----------|------|
| **Locked** | 初始状态 | 前序谜题 `Complete` | 不可见/灰显 | Chapter State 管理 |
| **Idle** | 前序完成或章节激活（首个谜题）| 玩家首次操作物件 | 场景可见，物件初始位置 | Chapter State 管理 |
| **Active** | 玩家首次操作 | NearMatch / PerfectMatch | 物件可操作，匹配度计算中 | Shadow Puzzle 驱动 |
| **NearMatch** | 匹配度 ≥ 40% | 匹配度 < 35%（回退）或 ≥ 85%（进入 PerfectMatch）| 视觉反馈启动 | Shadow Puzzle 驱动 |
| **PerfectMatch** | 匹配度 ≥ 85% | 吸附动画完成 → Complete | 吸附动画播放中 | Shadow Puzzle 驱动 |
| **Complete** | 吸附动画完成 | —（终态）| 触发叙事演出，通知 Chapter State 推进 | Chapter State 确认 |

> **状态归属说明**：`Locked`/`Idle`/`Complete` 由 Chapter State System 直接管理；`Active`/`NearMatch`/`PerfectMatch` 由 Shadow Puzzle System 驱动，但状态值存储在 Chapter State 中，Shadow Puzzle 通过 GameEvent 请求变更。

### Interactions with Other Systems

**与 Shadow Puzzle System 的交互（最密切）：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Shadow Puzzle | 谜题配置（物件列表、锚点、目标影子ID）、当前谜题状态 | Luban 配置表 + 直接查询 |
| Shadow Puzzle → Chapter State | 谜题状态变更请求（Active/NearMatch/PerfectMatch/Complete）| GameEvent: `PuzzleStateChangeRequest` |
| Chapter State → Shadow Puzzle | 状态变更确认 | GameEvent: `PuzzleStateChanged` |

**与 Save System 的交互（通过 IChapterProgress 接口）：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Save System | `IChapterProgress` 快照（序列化数据）| 直接调用 `Save()` |
| Save System → Chapter State | 加载的进度数据 | 启动时注入 `RestoreProgress()` |

**与 Narrative Event System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Narrative | 谜题完成事件、章节完成事件 | GameEvent: `PuzzleCompleted` / `ChapterCompleted` |
| Narrative → Chapter State | 章末演出完成确认（解锁下一章的前提）| GameEvent: `ChapterOutroFinished` |

**与 UI System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → UI | 章节列表及状态、谜题进度 (N/M)、可回放标记 | 查询接口 + GameEvent 广播变更 |

**与 Scene Management 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Scene | 当前应加载的场景ID | 查询接口 |
| Scene → Chapter State | 场景加载完成确认 | GameEvent: `SceneLoaded` |

**与 Collectible System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Collectible | 当前章节/谜题上下文（决定哪些收集物可见）| 查询接口 |

**与 Analytics 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Analytics | 章节开始/完成时间戳、谜题耗时 | GameEvent 广播 |

---

# Part 2: Save System

## Detailed Design

### Core Rules

**自动存档规则：**

1. 存档为纯自动保存，玩家不需要（也无法）手动存档
2. 触发自动保存的时机：
   - 谜题状态发生变更时（任何状态转换）
   - 章节状态发生变更时
   - 收集物获取时
   - 设置变更时
   - 玩家退到后台时（`OnApplicationPause(true)`）
   - 玩家退出游戏时（`OnApplicationQuit`）
3. 自动保存间隔不低于 1 秒（防抖），即 1 秒内多次变更合并为一次写入
4. 保存操作异步执行（UniTask），不阻塞主线程

**单存档槽规则：**

1. 本游戏仅有 1 个存档槽（叙事驱动，不需要多存档）
2. 存档文件路径: `Application.persistentDataPath/save/game_save.json`
3. 备份文件路径: `Application.persistentDataPath/save/game_save.backup.json`
4. 每次成功写入后，将上一版存档复制为 `.backup`

**数据完整性规则：**

1. 写入采用"写临时文件 → 验证 → 原子替换"策略，防止写入中断导致存档损坏
2. 加载时如果主存档损坏（JSON 解析失败或校验不通过），自动尝试加载 `.backup`
3. 如果主存档和备份均损坏，启动新游戏并记录错误日志
4. 存档包含版本号，用于未来数据迁移

**启动加载流程：**

1. 游戏启动时，Save System 在所有其他系统之前初始化
2. Save System 从本地加载存档文件（或创建空存档）
3. Save System 将加载的 `IChapterProgress` 数据注入 Chapter State System
4. Chapter State System 基于注入的数据恢复运行时状态
5. 其他系统（Collectible、Settings 等）依次从 Save System 获取各自的数据切片

### IChapterProgress 接口设计

> **这是 Chapter State System 和 Save System 之间的解耦契约。**

```csharp
/// Chapter State System 定义此接口，Save System 仅引用接口，不引用实现。
/// 位于共享程序集（如 GameScripts.Shared）中，两个系统都可引用。
public interface IChapterProgress
{
    int CurrentChapterId { get; }
    int CurrentPuzzleId { get; }
    ChapterState[] ChapterStates { get; }
    PuzzleState[] GetPuzzleStates(int chapterId);
    long LastPlayTimestamp { get; }
    int SaveVersion { get; }
}

public enum ChapterStateEnum
{
    Locked = 0,
    Active = 1,
    Complete = 2
}

public enum PuzzleStateEnum
{
    Locked = 0,
    Idle = 1,
    Active = 2,
    NearMatch = 3,
    PerfectMatch = 4,
    Complete = 5
}

[System.Serializable]
public struct ChapterState
{
    public int ChapterId;
    public ChapterStateEnum State;
    public long StartTimestamp;      // 首次进入时间（Unix ms）
    public long CompleteTimestamp;   // 完成时间，0 = 未完成
}

[System.Serializable]
public struct PuzzleState
{
    public int ChapterId;
    public int PuzzleId;
    public PuzzleStateEnum State;
    public int AttemptCount;         // 尝试次数（进入 Active 的次数）
    public float BestMatchScore;     // 历史最高匹配度
    public long CompleteTimestamp;   // 完成时间，0 = 未完成
    public float CompleteDuration;   // 从 Idle→Complete 的总耗时（秒）
}
```

**数据流时序图：**

```
┌──────────┐       ┌──────────────────┐       ┌──────────────┐
│  Save    │       │  Chapter State   │       │  Shadow      │
│  System  │       │  System          │       │  Puzzle      │
└────┬─────┘       └───────┬──────────┘       └──────┬───────┘
     │                     │                         │
     │ ── 启动阶段 ─────────────────────────────────────────────
     │                     │                         │
     │  LoadFromDisk()     │                         │
     │────────────────────>│                         │
     │  IChapterProgress   │                         │
     │  data               │                         │
     │<────────────────────│                         │
     │                     │                         │
     │  RestoreProgress()  │                         │
     │────────────────────>│                         │
     │                     │ (重建运行时状态)          │
     │                     │                         │
     │ ── 运行阶段 ─────────────────────────────────────────────
     │                     │                         │
     │                     │    查询谜题配置           │
     │                     │<────────────────────────│
     │                     │    返回配置数据           │
     │                     │────────────────────────>│
     │                     │                         │
     │                     │    PuzzleStateChange     │
     │                     │    Request (GameEvent)   │
     │                     │<────────────────────────│
     │                     │                         │
     │                     │ (验证+执行状态变更)       │
     │                     │                         │
     │                     │    PuzzleStateChanged    │
     │                     │    (GameEvent 广播)      │
     │                     │────────────────────────>│
     │                     │                         │
     │  SaveProgress()     │                         │
     │<────────────────────│                         │
     │  (异步写入磁盘)      │                         │
     │                     │                         │
```

### Save Data Schema (JSON)

完整的存档文件结构：

```json
{
  "version": 1,
  "createdAt": 1713264000000,
  "lastModifiedAt": 1713350400000,
  "totalPlayTime": 3600.5,
  "checksum": "a1b2c3d4",

  "chapterProgress": {
    "currentChapterId": 2,
    "currentPuzzleId": 3,
    "chapters": [
      {
        "chapterId": 1,
        "state": "Complete",
        "startTimestamp": 1713264000000,
        "completeTimestamp": 1713300000000,
        "puzzles": [
          {
            "puzzleId": 1,
            "state": "Complete",
            "attemptCount": 2,
            "bestMatchScore": 0.92,
            "completeTimestamp": 1713268000000,
            "completeDuration": 85.3
          },
          {
            "puzzleId": 2,
            "state": "Complete",
            "attemptCount": 1,
            "bestMatchScore": 0.88,
            "completeTimestamp": 1713272000000,
            "completeDuration": 142.7
          }
        ]
      },
      {
        "chapterId": 2,
        "state": "Active",
        "startTimestamp": 1713300000000,
        "completeTimestamp": 0,
        "puzzles": [
          {
            "puzzleId": 1,
            "state": "Complete",
            "attemptCount": 3,
            "bestMatchScore": 0.91,
            "completeTimestamp": 1713320000000,
            "completeDuration": 210.1
          },
          {
            "puzzleId": 2,
            "state": "Complete",
            "attemptCount": 1,
            "bestMatchScore": 0.95,
            "completeTimestamp": 1713340000000,
            "completeDuration": 68.0
          },
          {
            "puzzleId": 3,
            "state": "Active",
            "attemptCount": 1,
            "bestMatchScore": 0.32,
            "completeTimestamp": 0,
            "completeDuration": 0
          },
          {
            "puzzleId": 4,
            "state": "Locked",
            "attemptCount": 0,
            "bestMatchScore": 0,
            "completeTimestamp": 0,
            "completeDuration": 0
          }
        ]
      },
      {
        "chapterId": 3,
        "state": "Locked",
        "startTimestamp": 0,
        "completeTimestamp": 0,
        "puzzles": []
      },
      {
        "chapterId": 4,
        "state": "Locked",
        "startTimestamp": 0,
        "completeTimestamp": 0,
        "puzzles": []
      },
      {
        "chapterId": 5,
        "state": "Locked",
        "startTimestamp": 0,
        "completeTimestamp": 0,
        "puzzles": []
      }
    ]
  },

  "collectibles": {
    "unlockedIds": [101, 102, 105],
    "viewedIds": [101, 102]
  },

  "tutorialCompleted": ["tut_drag", "tut_rotate", "tut_snap"]
}

```

**JSON Schema 字段规范：**

| 字段路径 | 类型 | 约束 | 说明 |
|---------|------|------|------|
| `version` | int | ≥ 1 | 存档格式版本号，用于数据迁移 |
| `createdAt` | long | Unix ms | 存档创建时间 |
| `lastModifiedAt` | long | Unix ms | 最后修改时间 |
| `totalPlayTime` | float | ≥ 0 | 累计游玩时间（秒） |
| `checksum` | string | CRC32 hex | 数据完整性校验（排除 checksum 字段本身） |
| `chapterProgress.currentChapterId` | int | 1-5 | 当前活跃章节 |
| `chapterProgress.currentPuzzleId` | int | ≥ 1 | 当前活跃谜题在当前章节内的 ID |
| `chapterProgress.chapters[].chapterId` | int | 1-5 | 章节编号 |
| `chapterProgress.chapters[].state` | string | enum: Locked/Active/Complete | 章节状态 |
| `chapterProgress.chapters[].startTimestamp` | long | Unix ms, 0 = 未开始 | 首次进入时间 |
| `chapterProgress.chapters[].completeTimestamp` | long | Unix ms, 0 = 未完成 | 完成时间 |
| `chapterProgress.chapters[].puzzles[].puzzleId` | int | ≥ 1 | 谜题在章节内的顺序 ID |
| `chapterProgress.chapters[].puzzles[].state` | string | enum: Locked/Idle/Active/NearMatch/PerfectMatch/Complete | 谜题状态 |
| `chapterProgress.chapters[].puzzles[].attemptCount` | int | ≥ 0 | 累计尝试次数 |
| `chapterProgress.chapters[].puzzles[].bestMatchScore` | float | 0.0-1.0 | 历史最佳匹配度 |
| `chapterProgress.chapters[].puzzles[].completeTimestamp` | long | Unix ms, 0 = 未完成 | 完成时间 |
| `chapterProgress.chapters[].puzzles[].completeDuration` | float | ≥ 0 | Idle→Complete 总耗时（秒） |
| `collectibles.unlockedIds` | int[] | 收集物配置表 ID | 已解锁的收集物 |
| `collectibles.viewedIds` | int[] | ⊆ unlockedIds | 已查看过的收集物 |
| `tutorialCompleted` | string[] | TutorialStep.stepId 列表 | 已完成的教学步骤 ID 列表（由 Tutorial / Onboarding 系统写入） |

> **设置数据存储说明**：玩家偏好设置（音量、振动、灵敏度、语言、帧率等）通过 PlayerPrefs 独立存储，不包含在游戏存档 JSON 中。参见 `settings-accessibility.md`。

### States and Transitions

**Save System 自身的状态机（内部运作）：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Uninitialized** | 游戏启动 | `Initialize()` 调用 | 等待初始化 |
| **Loading** | `Initialize()` 调用 | 文件读取+反序列化完成 | 异步读取存档文件，解析 JSON |
| **Ready** | 加载完成 | 游戏退出 | 正常运行，响应保存请求 |
| **Saving** | 保存请求触发 | 写入磁盘完成 | 序列化+写入（与 Ready 并行，队列化处理）|
| **Error** | 加载/保存失败 | 恢复操作完成 | 尝试从 backup 恢复，或创建新存档 |

> `Saving` 不阻塞 `Ready`，通过队列确保写入顺序。同一时刻只有一个写入任务执行。

### Interactions with Other Systems

**与 Chapter State System 的交互（核心，通过 IChapterProgress）：**

| 数据方向 | 接口 | 时机 |
|---------|------|------|
| Save → Chapter State | `ChapterStateSystem.RestoreProgress(IChapterProgress)` | 游戏启动时，加载完成后调用 |
| Chapter State → Save | `SaveSystem.SaveChapterProgress(IChapterProgress)` | 谜题/章节状态变更时 |

**与 Collectible System 的交互：**

| 数据方向 | 接口 | 时机 |
|---------|------|------|
| Save → Collectible | 返回 `collectibles` 数据切片 | 启动加载时 |
| Collectible → Save | 写入新解锁/已查看的收集物 ID | 收集物获取/查看时 |

**与 Settings 的交互：**

| 数据方向 | 接口 | 时机 |
|---------|------|------|
| Save → Tutorial | 返回 `tutorialCompleted` 列表 | 启动加载时 |
| Tutorial → Save | 写入新完成的教学步骤 ID | 教学步骤完成时 |

> **注意**：Settings 偏好数据已迁移至 PlayerPrefs 独立存储，不再通过 Save System 接口读写。

---

# Part 3: 共享设计（两系统共用）

## Formulas

### Save Data Checksum（存档校验和）

```
checksum = CRC32(json_without_checksum_field)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| json_without_checksum_field | string | — | runtime | 移除 `"checksum": "..."` 字段后的完整 JSON 字符串 |

**Expected output**: 8 字符十六进制字符串（如 `"a1b2c3d4"`）

### Chapter Completion Percentage（章节完成度）

```
chapterPercent = completedPuzzleCount / totalPuzzleCount
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| completedPuzzleCount | int | 0 - totalPuzzleCount | runtime | 该章已完成的谜题数 |
| totalPuzzleCount | int | 5-8 | Luban 配置表 | 该章总谜题数 |

**Expected output range**: 0.0 到 1.0

### Overall Game Completion（总体完成度）

```
overallPercent = Σ(chapterWeight_i * chapterPercent_i) / Σ(chapterWeight_i)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| chapterWeight_i | float | 0.5-1.5 | Luban 配置表 | 每章的权重（谜题多的章节权重更高） |
| chapterPercent_i | float | 0.0-1.0 | calculated | 单章完成度 |

**Expected output range**: 0.0 到 1.0

### Save Debounce（存档防抖）

```
shouldSave = (currentTime - lastSaveTime) >= SAVE_DEBOUNCE_INTERVAL
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| SAVE_DEBOUNCE_INTERVAL | float | 1.0s | config | 最小保存间隔 |
| lastSaveTime | float | ≥ 0 | runtime | 上次保存完成时间 |

**Edge case**: `OnApplicationPause` 和 `OnApplicationQuit` 无视防抖，立即保存。

## Luban 配置表设计

### TbChapter（章节配置表）

| 字段名 | 类型 | 约束 | 说明 |
|-------|------|------|------|
| `id` | int | PK, 1-5 | 章节编号 |
| `nameKey` | string | L10N key | 章节名称本地化键 |
| `themeKey` | string | L10N key | 章节主题名（靠近/共同空间/…） |
| `puzzleCount` | int | 5-8 | 该章谜题总数 |
| `completionWeight` | float | 0.5-1.5 | 在总进度中的权重 |
| `sceneId` | string | 场景资源路径 | 该章对应的场景 |
| `bgmAsset` | string | 音频资源路径 | 章节背景音乐 |
| `colorTempStart` | float | 2700-6500K | 章节起始色温 |
| `colorTempEnd` | float | 2700-6500K | 章节结束色温 |
| `unlockCondition` | string | "chapter_complete:{id}" | 解锁条件表达式 |
| `outroEventId` | int | FK → TbNarrativeEvent | 章末演出事件 ID |

**示例数据：**

| id | nameKey | themeKey | puzzleCount | completionWeight | sceneId |
|----|---------|----------|-------------|-----------------|---------|
| 1 | chapter_1_name | theme_approach | 5 | 0.8 | Scene_Ch1_Room |
| 2 | chapter_2_name | theme_shared_space | 6 | 1.0 | Scene_Ch2_Room |
| 3 | chapter_3_name | theme_living_together | 7 | 1.0 | Scene_Ch3_Room |
| 4 | chapter_4_name | theme_loosening | 6 | 1.0 | Scene_Ch4_Room |
| 5 | chapter_5_name | theme_absence | 8 | 1.2 | Scene_Ch5_Room |

### TbPuzzle（谜题配置表）

| 字段名 | 类型 | 约束 | 说明 |
|-------|------|------|------|
| `id` | int | PK | 全局唯一谜题 ID |
| `chapterId` | int | FK → TbChapter.id | 所属章节 |
| `orderInChapter` | int | 1-8 | 章节内排序（线性推进顺序） |
| `nameKey` | string | L10N key | 谜题名称本地化键 |
| `descKey` | string | L10N key | 谜题描述（回放界面显示） |
| `sceneId` | string | 场景/预制体路径 | 谜题场景资源 |
| `targetShadowId` | string | 目标影子资源 ID | 预设目标影子配置 |
| `objectIds` | int[] | FK → TbPuzzleObject | 可操作物件 ID 列表 |
| `lightSourceCount` | int | 1-3 | 光源数量 |
| `hasLightControl` | bool | — | 玩家是否可控制光源 |
| `nearMatchThreshold` | float | 0.30-0.55 | NearMatch 进入阈值（覆盖默认值） |
| `perfectMatchThreshold` | float | 0.75-0.95 | PerfectMatch 阈值（覆盖默认值） |
| `isAbsencePuzzle` | bool | — | 是否为第五章"缺席型谜题" |
| `maxCompletionScore` | float | 0.0-1.0 | 缺席型谜题的最大可达匹配度 |
| `hintDelayOverride` | float | -1 = 使用默认 | 该谜题的提示延迟覆盖值 |
| `narrativeEventId` | int | FK → TbNarrativeEvent | 完成后触发的叙事事件 |

### TbPuzzleObject（谜题物件配置表）

| 字段名 | 类型 | 约束 | 说明 |
|-------|------|------|------|
| `id` | int | PK | 物件 ID |
| `prefabPath` | string | 预制体路径 | 物件预制体资源 |
| `nameKey` | string | L10N key | 物件名称 |
| `canMove` | bool | — | 是否可拖拽移动 |
| `canRotateY` | bool | — | 是否可绕 Y 轴旋转 |
| `canRotateX` | bool | — | 是否可绕 X 轴倾斜 |
| `canAdjustDistance` | bool | — | 是否可调整与光源距离 |
| `anchorCount` | int | 1-3 | 该物件的锚点数量 |
| `anchorWeights` | float[] | 0.1-1.0 | 各锚点权重 |
| `snapGridSize` | float | 0.1-0.5 | 格点吸附尺寸（覆盖默认值） |
| `initialPosition` | Vector3 | — | 初始位置 |
| `initialRotation` | Vector3 | — | 初始旋转 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 存档文件不存在（首次启动） | 创建默认存档：Chapter 1 = Active，其余 Locked | 新游戏的正常流程 |
| 主存档 JSON 解析失败 | 尝试加载 `.backup` 文件；成功则使用，失败则新游戏 | 防止存档损坏导致无法游玩 |
| 主存档 checksum 不匹配 | 记录警告日志，仍然使用该存档（不强制拒绝） | 移动端环境不可控，容错优先于安全 |
| 存档版本号高于当前游戏版本 | 拒绝加载，提示"请更新游戏" | 防止降级导致数据损坏 |
| 存档版本号低于当前游戏版本 | 执行版本迁移链（v1→v2→...→vN） | 支持增量升级 |
| 写入中途 App 被系统终止 | 临时文件残留不影响下次加载（原子替换策略保护主文件） | 移动端常见场景 |
| 玩家在谜题 Active 状态退出 | 保存当前状态为 Active（不回退为 Idle） | 回来后继续当前谜题 |
| 回放中途退出 | 回放进度不保存（回放是临时的） | 回放不影响主进度 |
| Chapter State 数据与配置表不一致（如配置表增加了谜题） | Chapter State 以配置表为准，补充缺失的谜题状态为 Locked | 支持热更内容扩展 |
| 同一帧内多个谜题状态变更 | 防抖合并为一次存档写入 | 减少 IO 操作 |
| 磁盘空间不足无法写入 | 捕获异常，记录错误日志，下次尝试重新保存 | 不中断游戏体验 |
| 两个系统初始化顺序错误 | Save System 必须先初始化，强制通过 Module 依赖链保证 | 架构约束 |
| `OnApplicationPause` 紧接 `OnApplicationQuit` | 两次保存请求通过队列去重，只执行一次 | 避免重复写入 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| **Chapter State ↔ Save System** | Bidirectional (via IChapterProgress) | Chapter State 定义数据接口；Save System 序列化/反序列化 |
| Shadow Puzzle System | Depends on Chapter State | 需要谜题配置和状态；通过 GameEvent 驱动状态变更 |
| Narrative Event System | Depends on Chapter State | 接收谜题/章节完成事件触发演出 |
| UI System | Depends on Chapter State | 显示章节进度和谜题状态 |
| Scene Management | Depends on Chapter State | 获取当前章节的场景 ID |
| Collectible System | Depends on both | Chapter State 提供上下文，Save System 持久化收集物 |
| Settings & Accessibility | Independent (PlayerPrefs) | 偏好设置独立存储于 PlayerPrefs，不依赖 Save System |
| Analytics | Depends on Chapter State | 采集进度和时间数据 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| SAVE_DEBOUNCE_INTERVAL | 1.0s | 0.5-3.0s | 减少 IO 频率，可能丢失更多最近数据 | 更频繁写入，增加 IO 负担 |
| BACKUP_ENABLED | true | true/false | 占用双倍存储空间（约 KB 级别，可忽略） | 无备份恢复手段 |
| MAX_SAVE_RETRY | 3 | 1-5 | 更多重试机会 | 更快放弃（但不中断游戏） |
| PUZZLE_COUNT_PER_CHAPTER | 5-8 | 3-10 | 章节更长，单章体验更丰富 | 章节更短，节奏更快 |
| CHAPTER_COMPLETION_WEIGHT | 0.8-1.2 | 0.5-1.5 | 该章在总进度中占比更高 | 该章在总进度中占比更低 |
| REPLAY_TRIGGERS_NARRATIVE | false | true/false | 回放时重新播放叙事演出 | 回放时跳过叙事演出（更纯粹的谜题体验） |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| 章节解锁 | 章节选择界面：锁图标消散 + 光线照亮新章节缩略图 | 轻柔的解锁音效（如门锁开启 + 光线扩散音） | Vertical Slice |
| 章节完成 | 章节缩略图上显示完成标记（柔和的金色光环） | 章节完成和弦 | Vertical Slice |
| 谜题解锁（下一个） | 场景内下一个谜题区域微弱亮起 | 极轻微的提示音 | MVP |
| 自动保存指示 | 屏幕角落短暂出现保存图标（日记本翻页动画），0.5 秒后淡出 | 无（静默保存） | MVP |
| 保存失败 | 无玩家可见反馈（仅内部日志） | 无 | — |
| 进度恢复成功 | 加载画面显示"继续你的记忆…" | 温暖的环境音渐入 | MVP |
| 新游戏开始 | 加载画面显示"一段新的旅程…" | 主题音乐第一个音符 | MVP |

## Game Feel

### Feel Reference

章节推进应该感觉像 **Florence 的章节转场** — 画面自然流动到下一个生活片段，没有"关卡选择"的游戏感。**不应该**感觉像 Candy Crush 的关卡地图那样有压力和目标感。

存档应该感觉像 **Journey 的自动保存** — 完全隐形，玩家从不思考存档的存在，下次打开就是上次离开的地方。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 章节选择点击 | 100ms | 6 frames | 允许加载延迟，但需即时视觉反馈 |
| 回放谜题选择 | 100ms | 6 frames | 同上 |
| 自动保存完成 | 不可感知 | — | 异步后台操作，无用户等待 |
| 进度恢复（启动加载） | < 500ms | — | 存档文件小（< 10KB），加载极快 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| 章节解锁动画 | 6 | 30 | 12 | 期待感，像门缓缓打开 | 与章末演出衔接 |
| 自动保存图标出现 | 3 | 15 | 12 | 低调，不打断体验 | 淡入-短停-淡出 |
| 章节选择界面切换 | 0 | 20 | 0 | 翻相册的流畅感 | 横向滑动过渡 |

### Impact Moments

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| 章节完成（最后谜题 PerfectMatch 后） | 2000-3000ms | 光线渐暖 → 短暂静止 → 章末演出开始 | Yes |
| 新章节首次进入 | 1500ms | 场景从暗到亮渐显 + 章节标题浮现 | Yes |
| 全游戏完成 | 5000ms+ | 特殊结局演出（五章影子回溯蒙太奇） | Yes |

### Weight and Responsiveness Profile

- **Weight**: 轻盈、自然。章节推进不应有"通关"的沉重感，更像翻到相册的下一页。
- **Player control**: 被动接受。章节推进是自动的，玩家不需要做"推进"的操作——完成最后一个谜题就自然进入下一章。
- **Snap quality**: 平滑过渡。没有硬切、没有弹窗、没有"恭喜通关"。
- **Acceleration model**: 渐进式。场景变化缓慢展开，给玩家时间消化情绪。
- **Failure texture**: 不存在失败。没有"保存失败"的玩家可见状态——系统在后台默默处理一切。

### Feel Acceptance Criteria

- [ ] 章节切换时，测试者不会说"在等加载"
- [ ] 没有测试者询问"游戏存档了吗"——存档完全隐形
- [ ] 退出并重新打开后，测试者能立即辨识出"我上次玩到这里"
- [ ] 章节完成的那一刻有"翻篇"的仪式感但不过度

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 当前章节名 | 屏幕顶部/角落 | 章节切换时 | 仅在非谜题操作时可见 |
| 谜题进度（N/M）| 屏幕角落 | 谜题完成时 | 当前章节的完成进度 |
| 章节选择界面 | 全屏覆盖 | 进入/退出时 | 暂停菜单或主菜单 |
| 章节缩略图 + 状态 | 章节选择界面 | 实时 | 锁定/当前/完成三态 |
| 回放谜题列表 | 章节选择的子界面 | 进入时 | 仅已完成章节可展开 |
| 自动保存图标 | 屏幕右下角 | 保存时闪现 | 0.5 秒淡入淡出 |
| 总体进度百分比 | 章节选择界面底部 | 实时 | 可选显示，默认关闭 |
| "继续游戏" | 主菜单 | 存在存档时 | 直接进入上次进度 |
| "新游戏" | 主菜单 | 常驻 | 有存档时二次确认 |

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 谜题状态变更由 Shadow Puzzle 驱动 | `design/gdd/shadow-puzzle-system.md` | 谜题状态机 (Active/NearMatch/PerfectMatch/Complete) | State trigger |
| NearMatch/PerfectMatch 阈值 | `design/gdd/shadow-puzzle-system.md` | `nearMatchThreshold` / `perfectMatchThreshold` | Data dependency |
| 谜题完成触发叙事演出 | `design/gdd/narrative-event-system.md` | 记忆重现事件链 | State trigger |
| 章节完成触发章末演出 | `design/gdd/narrative-event-system.md` | 章末演出事件 | State trigger |
| 章末演出结束确认解锁下一章 | `design/gdd/narrative-event-system.md` | `ChapterOutroFinished` 事件 | State trigger |
| 收集物状态持久化 | `design/gdd/collectible-system.md` (planned) | 收集物解锁/查看状态 | Ownership handoff |
| 设置偏好持久化 | `design/gdd/settings-accessibility.md` | 音量/触感/语言设置 | Ownership handoff |
| 提示延迟配置 | `design/gdd/hint-system.md` | 提示层级触发条件 | Data dependency |
| 章节场景映射 | `design/gdd/scene-management.md` | 场景加载/卸载 | Rule dependency |

## Acceptance Criteria

### Chapter State System

- [ ] 新游戏启动：Chapter 1 = Active，Chapter 2-5 = Locked
- [ ] 完成 Chapter 1 所有谜题后：Chapter 1 = Complete，Chapter 2 = Active
- [ ] 谜题严格线性推进：未完成 Puzzle N 时，Puzzle N+1 保持 Locked
- [ ] 已完成章节的所有谜题可从章节选择界面自由回放
- [ ] 回放不影响主线进度（回放中的完成不重复计入）
- [ ] 当前活跃章节不可回放
- [ ] 谜题状态变更仅接受合法转换（如 Locked 不能直接跳到 Complete）
- [ ] 所有状态变更通过 GameEvent 广播，相关系统能正确响应
- [ ] 章节/谜题数量由 Luban 配置表驱动，代码中无硬编码的章节数

### Save System

- [ ] 首次启动创建有效的默认存档
- [ ] 谜题/章节/收集物状态变更后 ≤ 1 秒自动保存（防抖）
- [ ] `OnApplicationPause` 和 `OnApplicationQuit` 立即触发保存
- [ ] 主存档损坏时自动恢复 `.backup`
- [ ] 存档文件大小 < 10KB（全游戏完成状态下）
- [ ] 保存操作不阻塞主线程（异步 UniTask）
- [ ] 存档版本升级迁移正确执行（v1 → v2 → ... → vN）
- [ ] 写入采用原子替换策略，中途中断不损坏主文件
- [ ] Performance: 保存操作 < 50ms（含序列化+写入）
- [ ] Performance: 加载操作 < 100ms（含读取+反序列化+校验）

### 集成测试

- [ ] 完整流程：新游戏 → 完成 Chapter 1 → 退出 → 重启 → Chapter 2 Active
- [ ] 边界：在谜题 Active 状态退出 → 重启 → 恢复到同一谜题 Active 状态
- [ ] 损坏恢复：删除主存档 → 重启 → 从 backup 恢复
- [ ] 配置变更：热更增加新谜题 → 存档兼容性正确处理

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 是否需要"重置章节"功能（重玩某章并清除该章进度）？ | Game Design | Vertical Slice | 待定——可能与回放功能冲突，需要明确区分"回放体验"和"重新挑战" |
| 存档是否需要云同步（iCloud/Google Play）？ | Tech Lead | Alpha | MVP 仅本地存储；Alpha 评估云同步需求和实现成本 |
| 存档加密是否必要？ | Tech Lead | MVP | 建议 MVP 不加密（CRC32 校验即可）；如果有付费内容则 Alpha 加入 |
| 第五章"缺席型谜题" `maxCompletionScore < 1.0` 的存档如何表示？ | Game Design | Vertical Slice | 当前设计：`bestMatchScore` 允许 < 1.0 的 Complete 状态，需确认 UI 如何展示 |
| 是否支持"新游戏+"（通关后带特殊标记重新开始）？ | Game Design | Full Vision | 不在 MVP/VS 范围内，但存档结构预留 `gameCompletedOnce` 字段 |
| Luban 配置表的 `unlockCondition` 是否需要支持复合条件？ | Tech Lead | MVP | MVP 仅支持 `chapter_complete:{id}`；后续可扩展为表达式解析 |
