// 该文件由Cursor 自动生成

# SP-004 Findings: Luban Tables 线程安全性

> **Status**: ✅ 已验证（源码审查）
> **Date**: 2026-04-22
> **Source**: `Assets/GameScripts/HotFix/GameProto/GameConfig/Tables.cs`, `ConfigSystem.cs`

## 结论

**Luban Tables 非线程安全，但在本项目中不需要额外同步锁。** 原因是所有访问路径均在 Unity 主线程上。

## 发现详情

### Tables 类结构

- **不是单例**: `Tables` 是普通类，构造函数为 `Tables(Func<string, ByteBuf> loader)`
- **不是 IReadOnlyDictionary**: 每个表（如 `TbItem`）内部使用 `Dictionary<int, T>` + `List<T>`
- **Init() 为空**: Luban 生成的 `Init()` 方法无实际逻辑
- **由 ConfigSystem 持有**: `ConfigSystem` 单例在 `Load()` 中 `new Tables(LoadByteBuf)`，通过属性暴露

### 访问模式分析

```
初始化时序（architecture.md §5.6）：
  Step 7: ConfigSystem.Load() → new Tables(loader) → 主线程
  Step 8+: 各系统从 Tables 读取配置 → 主线程

运行时访问：
  Tables.TbPuzzle.Get(id) → 主线程
  Tables.TbChapter.Get(id) → 主线程
```

### 安全保障

| 条件 | 满足？ | 说明 |
|------|:------:|------|
| Tables 在主线程初始化 | ✅ | ConfigSystem.Load() 在 Procedure 链主线程中 |
| 初始化完成后只读 | ✅ | Luban 表数据加载后不修改 |
| 所有 UniTask await 在主线程续接 | ✅ | UniTask 默认 PlayerLoop context |
| 无 UniTask.Run 中读 Tables | ✅ | 需作为编码规范执行 |

### 必须遵守的规则

```
❌ 禁止: await UniTask.Run(() => Tables.TbPuzzle.Get(id));
✅ 允许: var data = Tables.TbPuzzle.Get(id); // 主线程直接读
✅ 允许: await SomeAsync(); var data = Tables.TbPuzzle.Get(id); // await 后仍在主线程
```

## ADR-007 影响

新增编码规范：**禁止在 `UniTask.Run`（线程池）中访问 Luban Tables**。此规则应写入 Control Manifest。

## 行动项

- [x] 确认 Tables 容器类型 — Dictionary<int, T>
- [x] 确认访问安全性 — 主线程只读安全
- [x] 产出编码规范 — 禁止 UniTask.Run 中读 Tables
