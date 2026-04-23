// 该文件由Cursor 自动生成

# ADR-008: Save System Architecture

| Field | Value |
|-------|-------|
| **Status** | Proposed |
| **Date** | 2026-04-22 |
| **Author** | Technical Director |
| **Project** | 影子回忆 (Shadow Memory) |
| **Engine** | Unity 2022.3.62f2 LTS |
| **Depends On** | None |
| **Enables** | ADR-014 (Puzzle State Machine uses save data) |
| **Blocks** | Chapter State system implementation (needs save data on init) |

---

## Context

影子回忆是一款单人解谜手游，需要持久化以下玩家数据：

- **章节/谜题进度**：当前章节、各谜题完成状态
- **教程完成标记**：已完成的教程步骤 ID 列表
- **收集品解锁状态**：每个收集品是否已发现

存档系统处于**Foundation Layer**（参见 Master Architecture §3.1），是最底层的持久化服务。它不含任何 gameplay 逻辑——仅负责文件 I/O 和数据格式完整性。

### 核心边界定义

Save System 与 Chapter State System 之间通过 `IChapterProgress` 接口解耦：

- **Chapter State System** 拥有运行时游戏进度数据，实现 `IChapterProgress`
- **Save System** 拥有文件 I/O 和格式完整性，序列化/反序列化 `IChapterProgress`

**Settings（玩家设置）不在存档文件中**——8 项设置通过 `PlayerPrefs` 独立存储：
`master_volume`, `music_volume`, `sfx_volume`, `sfx_enabled`, `haptic_enabled`, `touch_sensitivity`, `language`, `target_framerate`

### 技术约束

- 存档文件大小 < 10KB（5 章 × 若干谜题 + 教程 + 收集品）
- 移动端加载目标 < 200ms（含文件读取 + JSON 解析 + CRC32 校验）
- 自动存档不能造成可感知的卡顿（主线程不阻塞）
- 必须处理 iOS/Android 后台切换时的紧急存档（`OnApplicationPause` 时间窗有限）
- 必须支持跨版本存档迁移（向前兼容）

---

## Decision

采用 **Custom JSON + CRC32 校验 + 备份文件 + 原子写入** 方案。

### 存档格式

```json
{
  "version": 1,
  "currentChapterId": 2,
  "chapters": [
    {
      "id": 1,
      "status": "complete",
      "puzzles": [
        { "id": 1, "state": "Complete" },
        { "id": 2, "state": "Complete" }
      ]
    },
    {
      "id": 2,
      "status": "active",
      "puzzles": [
        { "id": 3, "state": "Active" },
        { "id": 4, "state": "Locked" }
      ]
    }
  ],
  "tutorialCompleted": ["tut_drag", "tut_rotate"],
  "collectibles": { "1": true, "2": false }
}
```

### 序列化方案

- **首选**：Unity `JsonUtility`（零依赖、无 GC 分配优势）
- **备选**：Newtonsoft.Json（当需要 `Dictionary` 序列化或复杂多态类型时迁移）
- `JsonUtility` 不支持 `Dictionary` 直接序列化——`collectibles` 字段需要一个 wrapper struct（`SerializableDict` 或扁平化为 `CollectibleEntry[]` 数组）

### 数据完整性

| 机制 | 说明 |
|------|------|
| **CRC32 校验** | 存档 JSON 内容的 CRC32 值存储在同目录下的 `save.crc` 文件中（或 JSON 内嵌 `_checksum` 字段，加载时先剥离再校验） |
| **备份文件** | 每次成功写入后，将当前有效存档复制为 `save.backup.json`（及对应 `save.backup.crc`） |
| **原子写入** | 写入流程：`save.tmp` → CRC32 校验 tmp 文件 → rename `save.tmp` → `save.json`，避免写入中断导致数据丢失 |

### 文件位置

```
Application.persistentDataPath/
├── save.json           // 主存档
├── save.crc            // CRC32 校验值
├── save.backup.json    // 备份存档
└── save.backup.crc     // 备份校验值
```

### 加载容错链

```
LoadAsync():
  1. 读取 save.json + save.crc
     ├── CRC32 校验通过 → 反序列化 → 返回数据
     └── 校验失败 或 文件不存在 →
  2. 读取 save.backup.json + save.backup.crc
     ├── CRC32 校验通过 → 反序列化 → 返回数据 (log WARNING)
     └── 校验失败 或 文件不存在 →
  3. 返回全新 SaveData (fresh start)
     → 通知用户 "存档数据丢失，已重新开始"
```

### 版本迁移

采用**顺序迁移链**（Sequential Migration Chain）：

```
v1 → v2 → v3 → ... → vCurrent
```

- 每个版本升级对应一个 `IMigration` 实现
- 加载时检测 `version` 字段，逐步执行迁移
- 迁移函数是纯数据变换，可单元测试

```csharp
public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    SaveData Migrate(SaveData data);
}
```

### 自动存档触发器

| 触发事件 | 行为 | 延迟 |
|----------|------|------|
| `Evt_PuzzleStateChanged` | 防抖存档 | 1s debounce |
| `Evt_ChapterComplete` | 立即存档 | 无延迟 |
| Collectible pickup | 防抖存档 | 1s debounce |
| `OnApplicationPause(true)` | 立即存档，绕过防抖 | 无延迟 |
| `OnApplicationQuit()` | 立即存档，绕过防抖 | 无延迟 |

防抖逻辑：连续多次触发防抖存档时，只在最后一次触发后 1s 执行一次写入，防止频繁 I/O。

### 公开接口

```csharp
public interface ISaveService
{
    UniTask<IChapterProgress> LoadAsync();
    UniTask SaveAsync(IChapterProgress progress);
    UniTask DeleteSave();
    bool HasSave { get; }

    void TriggerAutoSave();          // debounced (1s min interval)
    void TriggerImmediateSave();     // bypasses debounce (app pause/quit)

    // Events (via GameEvent):
    //   Evt_SaveComplete { }
}
```

- 所有异步操作使用 `UniTask`，遵守 P3 异步优先原则
- 禁止 Coroutine，禁止同步文件 I/O

### Settings 独立存储

Settings 系统使用 `PlayerPrefs` 存储 8 项设置值，**完全独立于存档文件**：

- Settings 的读写不经过 Save System
- 删除存档（`DeleteSave()`）不会清除 Settings
- 这确保了玩家重新开始游戏时，音量/语言等偏好不受影响

---

## Alternatives Considered

### Alternative 1: Binary Serialization (BinaryFormatter / MessagePack)

| 维度 | 评估 |
|------|------|
| 文件大小 | 更小（但我们的数据 < 10KB，无意义） |
| 解析速度 | 更快（但 JSON 解析 10KB 已 < 50ms） |
| 可读性 | 不可读，调试困难 |
| 安全性 | `BinaryFormatter` 存在远程代码执行漏洞（[Microsoft 已弃用](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide)） |
| 额外依赖 | MessagePack 需要添加 NuGet 包 |

**结论**：收益不足以弥补调试成本和安全风险。对 < 10KB 数据，JSON 性能已足够。

### Alternative 2: SQLite

| 维度 | 评估 |
|------|------|
| 查询能力 | 强大，适合复杂结构化数据 |
| 数据量匹配 | 严重过度——我们只有 1 行存档数据 |
| 移动端部署 | 需要原生插件（`.so`/`.dylib`），增加包体和兼容性风险 |
| 原子性 | 内建 ACID，但对我们的场景过于重量 |

**结论**：工程复杂度远超需求。SQLite 适合需要多表关联查询的场景（如 MMO 背包），不适合 < 10KB 的线性进度数据。

### Alternative 3: Cloud Save (PlayFab / GameSparks)

| 维度 | 评估 |
|------|------|
| 跨设备同步 | 核心优势 |
| 网络依赖 | 需要网络连接，离线无法存档——对移动解谜游戏不可接受 |
| MVP 复杂度 | 注册/登录流程、冲突解决、后端成本 |
| 未来兼容 | 可以作为 **同步层** 叠加在本地存档之上，不影响本 ADR 决策 |

**结论**：MVP 阶段不需要。未来如需云存档，可在 `ISaveService` 之上添加 `ICloudSyncService`，本地存档作为 ground truth，云端作为同步副本。

---

## Consequences

### Positive

- **简单可靠**：JSON + CRC32 + 备份是经过大量游戏验证的方案
- **调试友好**：人类可读的 JSON 文件，开发/测试阶段可直接编辑
- **无外部依赖**：仅使用 Unity 内置 API + `System.IO`
- **干净的关注点分离**：Save System 只做 I/O，不懂 gameplay；Settings 独立于存档
- **面向未来**：版本迁移链保证跨版本兼容；接口设计允许未来叠加云同步

### Negative

- **无加密**：存档文件明文可读，玩家可手动修改。对单人解谜游戏可接受——不存在竞争性或经济系统
- **MVP 无云同步**：换设备需重新开始。可在后续版本通过 `ICloudSyncService` 补充
- **JSON 解析比二进制稍慢**：对 < 10KB 数据差异在 1ms 量级，可忽略

### Risks

| 风险 | 严重度 | 缓解措施 |
|------|:------:|----------|
| CRC32 非密码学安全，可被伪造 | LOW | 单人解谜游戏无作弊动机；如需防篡改可升级为 HMAC-SHA256 |
| `OnApplicationPause` 时间窗不足以完成写入 | MEDIUM | 立即存档绕过防抖；存档数据 < 10KB 写入 < 10ms；保留 backup 作为最坏情况回退 |
| `JsonUtility` 不支持 `Dictionary` | LOW | 使用 `CollectibleEntry[]` 数组替代，或在需要时迁移到 Newtonsoft |
| 文件系统权限问题（特定 Android 设备） | LOW | `Application.persistentDataPath` 是 Unity 保证的可写路径 |

---

## Performance Implications

| 指标 | 预算 | 预期实际值 | 说明 |
|------|:----:|:---------:|------|
| 存档文件大小 | < 50KB | < 10KB | 5 章完整数据 |
| 加载时间（读取 + 解析 + CRC32） | < 200ms | < 50ms | 基于 10KB JSON 在低端移动设备的实测预估 |
| 保存时间（序列化 + 写入 + 备份） | < 200ms | < 100ms | 原子写入包含一次额外 rename 操作 |
| 自动存档防抖 | 最小间隔 1s | — | 防止快速连续操作导致的 I/O 风暴 |
| 主线程阻塞 | 0ms | 0ms | 所有文件 I/O 通过 UniTask 在后台线程执行 |

---

## GDD Requirements Addressed

| TR ID | Requirement | 本 ADR 覆盖方式 |
|-------|------------|-----------------|
| TR-save-001 | 存档格式定义 | JSON schema + version 字段 |
| TR-save-002 | 数据完整性校验 | CRC32 + 备份文件 |
| TR-save-003 | 原子写入防损坏 | .tmp → verify → rename 流程 |
| TR-save-004 | 加载容错链 | Primary → Backup → Fresh start |
| TR-save-005 | IChapterProgress 解耦 | ISaveService 接口参数类型 |
| TR-save-006 | 自动存档触发点 | 5 个触发器 + 防抖逻辑 |
| TR-save-007 | 版本迁移 | Sequential migration chain |
| TR-save-008 | 异步 I/O（UniTask） | LoadAsync/SaveAsync 接口 |
| TR-save-009 | Settings 独立于存档 | PlayerPrefs 分离 |
| TR-save-010 | 删除存档功能 | DeleteSave() API |
| TR-save-011 | HasSave 查询 | HasSave property |
| TR-save-012 | 存档路径 | Application.persistentDataPath |
| TR-save-013 | 备份机制 | .backup.json 自动创建 |
| TR-save-014 | OnApplicationPause 紧急存档 | TriggerImmediateSave() |
| TR-save-015 | OnApplicationQuit 存档 | TriggerImmediateSave() |
| TR-save-016 | 移动端加载 < 200ms | 预算定义 + async 实现 |
| TR-save-017 | 存档完成事件 | Evt_SaveComplete |
| TR-save-018 | Save 必须在 ChapterState 之前初始化 | Init order §5.6 steps 10-12 |

Source GDDs:
- `chapter-state-and-save.md`: TR-save-001 through TR-save-018
- `settings-accessibility.md`: Settings 独立于存档文件

---

## Validation Criteria

| # | 验证项 | 测试方式 |
|---|--------|---------|
| 1 | Load/Save round-trip 保持所有数据字段不变 | 单元测试：序列化 → 反序列化 → 断言每个字段相等 |
| 2 | 主文件损坏时成功回退到 backup | 单元测试：写入无效数据到 save.json → LoadAsync() 返回 backup 数据 |
| 3 | 主文件和 backup 均损坏时返回 fresh start | 单元测试：两个文件均无效 → LoadAsync() 返回默认 SaveData |
| 4 | 版本迁移链正确执行 | 单元测试：v1 数据 → 经 v1→v2→v3 迁移 → 断言 v3 格式正确 |
| 5 | 自动存档在所有指定的 gameplay 时刻触发 | 集成测试：模拟 PuzzleStateChanged / ChapterComplete 等事件 → 验证文件更新 |
| 6 | 防抖逻辑：1s 内多次触发只写入一次 | 单元测试：连续 5 次 TriggerAutoSave() → 验证只有 1 次文件写入 |
| 7 | OnApplicationPause 存档在 iOS/Android 后台时间限制内完成 | 设备测试：在真机上触发 home 键 → 验证存档文件已更新 |
| 8 | DeleteSave 清除所有存档文件但不影响 PlayerPrefs | 单元测试：DeleteSave() → save.json 不存在 → PlayerPrefs 值不变 |
| 9 | 原子写入：写入中断不损坏现有存档 | 单元测试：模拟 .tmp 写入后 rename 失败 → 原 save.json 完好 |

---

## Implementation Notes

### 写入流程伪代码

```csharp
async UniTask SaveAsync(IChapterProgress progress)
{
    var json = JsonUtility.ToJson(ToSerializable(progress));
    var crc = ComputeCRC32(json);
    var tmpPath = Path.Combine(persistentPath, "save.tmp");
    var savePath = Path.Combine(persistentPath, "save.json");
    var crcPath = Path.Combine(persistentPath, "save.crc");
    var backupPath = Path.Combine(persistentPath, "save.backup.json");
    var backupCrcPath = Path.Combine(persistentPath, "save.backup.crc");

    await File.WriteAllTextAsync(tmpPath, json);

    var verifyJson = await File.ReadAllTextAsync(tmpPath);
    if (ComputeCRC32(verifyJson) != crc)
        throw new SaveCorruptionException("tmp file CRC mismatch");

    if (File.Exists(savePath))
    {
        File.Copy(savePath, backupPath, overwrite: true);
        File.Copy(crcPath, backupCrcPath, overwrite: true);
    }

    File.Move(tmpPath, savePath); // atomic on most file systems
    await File.WriteAllTextAsync(crcPath, crc.ToString());

    GameEvent.Send(EventId.Evt_SaveComplete);
}
```

### 初始化顺序

参见 Master Architecture §5.6：

```
Step 10: SaveSystem.Init() → determine save file path
Step 11: SaveSystem.LoadAsync() → load save data (or create fresh)
Step 12: ChapterState.Init(saveData) → populate runtime progress
```

ChapterState 的初始化**必须**在 SaveSystem.LoadAsync() 完成之后。

---

*End of ADR-008*
