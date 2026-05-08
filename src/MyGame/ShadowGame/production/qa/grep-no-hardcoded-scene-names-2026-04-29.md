# Grep Evidence — No Hardcoded Scene Names in Production Code

> **Story**: S2-07 / `production/epics/scene-management/story-006-luban-scene-mapping.md`
> **AC covered**: AC-2, AC-7（同义重复）
> **Performed**: 2026-04-29
> **Scope**: `Assets/GameScripts/HotFix/` (生产热更代码全量；不含 Tests / docs / design / production / Word / Editor 工具)

---

## 扫描命令

```bash
# 扫"Chapter_0X_xxx" 模式（GDD scene name 命名规则）
rg --type=cs 'Chapter_0[1-5]_' src/MyGame/ShadowGame/Assets/GameScripts/HotFix

# 扫所有 "Chapter_xxx" 字面字符串
rg --type=cs '"Chapter_' src/MyGame/ShadowGame/Assets/GameScripts/HotFix
```

## 扫描结果

| 模式 | 命中数 | 详情 |
|------|:----:|------|
| `Chapter_0[1-5]_` | **0** | 无生产代码硬编码 5 章 scene 名 |
| `"Chapter_` (任何前缀) | **0** | 无生产代码字面 scene 名字符串 |

✅ **零硬编码 scene name 通过**。

## 测试 fixture 内的 scene name

测试代码 `Assets/Tests/EditMode/SceneManagement/LubanSceneMappingTests.cs` 内的
`ResolveFixture` 方法包含 `"Test_Chapter_01"..."Test_Chapter_05"` 5 个字符串字面值。

**这些不在 AC-2/AC-7 扫描范围**：

- 测试 fixture 是手工构造的内存数据（未注入 SceneManager 的 production 路径）
- `Test_` 前缀明确区分于 GDD 推荐的 production scene 名（`Chapter_0X_<Slug>`）
- AC-2 字面要求"代码库 grep 零 *硬编码 scene name 字符串*"——精神是"production 切场景代码不直接引用 scene 字符串"，测试 fixture 不在此范畴

## ChapterData provider 注入路径（生产）

未来 boot pipeline 在 `Tables.Init()` 完成后注入：

```csharp
sceneManager.RegisterChapterDataProvider(
    id => ConfigSystem.Tables.TbChapter.Get(id));
```

scene name 由 Luban `TbChapter.SceneId` 提供，**永不出现在 C# 字面值**。
当前 Luban 接通是后续 story（依赖 TbChapter.xlsx + Schema 编辑 + CodeGen），不在 S2-07 范围。

## 持续监控

下次 grep 触发条件：
- 任何 PR 引入 production C# scene name 字符串字面值
- Luban TbChapter 接入 PR（届时 Tables.cs 自动生成的 `m_TbChapter` 字段允许包含 scene 名常量符号，但仍不属于硬编码）

建议加入 CI grep gate（Sprint 3 polish 阶段评估）：

```bash
rg --type=cs '"Chapter_0[1-5]_' \
   --glob '!**/Tests/**' \
   --glob '!**/GameProto/GameConfig/**' \
   src/MyGame/ShadowGame/Assets/
# 期望：0 命中；非零则 CI red
```
