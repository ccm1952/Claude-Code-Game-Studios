// 该文件由Cursor 自动生成

# SP-001 Findings: GameEvent Payload 类型支持

> **Status**: ✅ 已验证（源码审查）
> **Date**: 2026-04-22
> **Source**: `Assets/TEngine/Runtime/Core/GameEvent/GameEvent.cs` (~601 行)

## 结论

**GameEvent.Send 完全支持泛型 struct/class payload。** ADR-006 的 payload 约定有效，无需切换到 static event bus。

## 发现详情

### Send 方法签名

```csharp
// int eventType 版本
public static void Send(int eventType)
public static void Send<TArg1>(int eventType, TArg1 arg1)
public static void Send<TArg1, TArg2>(int eventType, TArg1 arg1, TArg2 arg2)
public static void Send<TArg1, TArg2, TArg3>(int eventType, TArg1 arg1, TArg2 arg2, TArg3 arg3)
public static void Send<TArg1, TArg2, TArg3, TArg4>(int eventType, TArg1 arg1, ..., TArg4 arg4)
public static void Send<TArg1, TArg2, TArg3, TArg4, TArg5>(int eventType, ..., TArg5 arg5)
// TArg6 仅 int eventType 版本
```

- **泛型无 struct 约束**: 引用类型和值类型均可
- **最大参数数**: int eventType 最多 6 参数, string eventType 最多 5 参数
- **AddEventListener**: 支持到 TArg6

### 对架构的影响

| 架构文档中的 payload 场景 | 可行性 |
|--------------------------|:------:|
| `Send<GestureData>(1000, data)` — 单参数 struct | ✅ |
| `Send<int, Vector3, Quaternion>(1100, id, pos, rot)` — 3 参数 | ✅ |
| `Send<MatchScoreChangedPayload>(1200, payload)` — 复合 struct | ✅ |
| `Send<string>(1205, lockToken)` — PuzzleLockAll token | ✅ |

### 剩余风险

**HybridCLR AOT 泛型实例化**: `GameEvent.Send<struct>()` 在 AOT 环境下可能需要 AOT 泛型元数据补充注册。项目已有 `RuntimeApi.LoadMetadataForAOTAssembly` 机制 (见 `ProcedureLoadAssembly.cs`)。

**建议**: 在 SP-007 真机验证中同步测试 GameEvent 泛型调用。

## 行动项

- [x] 确认 Send<T> 支持 struct — 已确认
- [x] 确认最大参数数 — int 版 6 参数, string 版 5 参数
- [ ] 真机验证 HybridCLR AOT 泛型（合并到 SP-007）
