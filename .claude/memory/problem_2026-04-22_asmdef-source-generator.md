# 问题记录：asmdef 引用 GameLogic 导致 Source Generator 编译失败

> **日期**: 2026-04-22
> **发生次数**: 2 次（SP-007 测试脚本、Story-001 EditMode 测试）
> **严重性**: 阻断编译

## 问题现象

在新建或修改 `.asmdef` 文件时，如果该 asmdef 引用了 `GameLogic` 但未引用 `TEngine.Runtime`，Unity 编译时报错：

```
SourceGenerator/EventInterfaceGenerator/GameEventHelper.g.cs(11,7): error CS0246: 
The type or namespace name 'TEngine' could not be found 
(are you missing a using directive or an assembly reference?)
```

## 原因分析

TEngine 框架使用 Roslyn Source Generator (`EventInterfaceGenerator`)，它会在**所有**引用了 `GameLogic` 的程序集中生成 `GameEventHelper.g.cs`。这个生成文件包含 `using TEngine;`，因此编译它的程序集必须能解析 `TEngine` 命名空间。

**根因**：asmdef 的传递依赖不会自动传递 Source Generator 的编译上下文。即使 `GameLogic.asmdef` 已经引用了 `TEngine.Runtime`，消费者 asmdef 仍需显式引用。

## 解决方案

所有引用 `GameLogic` 的 asmdef **必须同时引用 `TEngine.Runtime`**：

```json
{
    "references": [
        "GameLogic",
        "TEngine.Runtime"
    ]
}
```

## 受影响的场景

| 场景 | asmdef | 需要添加 |
|------|--------|---------|
| EditMode 测试 | `EditModeTests.asmdef` | `TEngine.Runtime` |
| PlayMode 测试 | `PlayModeTests.asmdef` | `TEngine.Runtime` |
| Editor 工具扩展 | 任何引用 GameLogic 的 Editor asmdef | `TEngine.Runtime` |
| 新功能模块 | 任何新 asmdef 引用 GameLogic | `TEngine.Runtime` |

## 已更新的文档

- [x] `.claude/docs/technical-preferences.md` — 新增 "Assembly Definition (asmdef) Rules" 节
- [x] `.claude/skills/dev-story/SKILL.md` — Phase 5 添加 asmdef 警告
- [x] `docs/architecture/control-manifest.md` — Foundation 层 2.1 添加 asmdef guardrail
