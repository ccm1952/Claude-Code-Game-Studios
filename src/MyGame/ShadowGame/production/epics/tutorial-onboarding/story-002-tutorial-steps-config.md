// 该文件由Cursor 自动生成

# Story 002: Luban TbTutorialStep Config Table Integration

> **Epic**: Tutorial & Onboarding
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Config/Data
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/tutorial-onboarding.md`
**Requirement**: `TR-tutor-001`, `TR-tutor-009`

**ADR Governing Implementation**: ADR-007: Luban Config Access
**ADR Decision Summary**: 所有教学步骤通过 Luban `TbTutorialStep` 配置表定义；代码通过 `Tables.Instance.TbTutorialStep.Get(id)` 读取；配置表在 ProcedureMain 的 `Tables.Init()` 阶段完成加载；任何新教学步骤扩展不改代码，只修改配置表。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation — Luban)**:
- Required: 所有配置读取通过 `Tables.Instance.TbXXX.Get(id)`；配置数据对象加载后只读，禁止运行时修改字段；扩展计算方法写在 GameLogic 程序集的 extension methods，禁止修改 GameProto；`Tables.Init()` 必须在任何系统读取配置前完成
- Forbidden: 禁止在代码中硬编码教学步骤内容、手势类型、提示文本；禁止手动修改 GameProto 中 Luban 生成的文件；禁止在 `UniTask.Run()`（线程池）中访问 Tables
- Guardrail: `Tables.Init()` 执行时间点：ProcedureMain 的第 7 步

---

## Acceptance Criteria

- [ ] 在 Luban 配置源（Excel/JSON）中定义 `TbTutorialStep` 表，包含以下字段：
  - `id` (int): 步骤唯一 ID
  - `stepKey` (string): 可读标识，如 `"tut_drag"`
  - `chapterId` (int): 触发所在章节
  - `order` (int): 同章节内排序
  - `triggerCondition` (enum string): `OnChapterEnter` / `OnPreviousStepDone` / `OnObjectSelected`
  - `requiredGestureType` (enum string): 对应 `GestureType`（Drag/Rotate/LightDrag 等）
  - `allowedGestureTypes` (string list): InputFilter 白名单，逗号分隔 GestureType 枚举名
  - `promptImagePath` (string): 手势图片资源路径（YooAsset 地址）
  - `promptTextKey` (string): 本地化文本 key
  - `promptPosition` (enum string): `Bottom` / `Center` / `NearObject`
  - `completionCount` (int): 完成所需次数，默认 1
- [ ] MVP 的 5 个教学步骤全部在配置表中录入：`tut_drag`(ch1)、`tut_rotate`(ch1)、`tut_snap`(ch1)、`tut_light`(ch2)、`tut_distance`(ch3, TBD)
- [ ] Luban 生成代码在 `GameProto` 程序集内，读取接口通过 `Tables.Instance.TbTutorialStep` 暴露
- [ ] `TbTutorialStep.GetByChapter(int chapterId)` 扩展方法（定义在 GameLogic 程序集）返回指定章节所有步骤，按 `order` 升序排列
- [ ] `allowedGestureTypes` 字符串在读取时能正确解析为 `GestureType[]`（通过 extension method 完成，不修改 GameProto）
- [ ] `Tables.Init()` 完成后可立即调用 `TbTutorialStep.Get(id)` 无异常
- [ ] 配置表中不存在的 `id` 调用 `Get(id)` 返回 `null`，调用方需做空检查

---

## Implementation Notes

**Luban 表定义（conceptual schema）：**
```
TbTutorialStep:
  id              : int              # PK
  stepKey         : string
  chapterId       : int
  order           : int
  triggerCondition: TriggerCondition # enum
  requiredGesture : GestureType      # enum
  allowedGestures : string           # "Tap,Drag" → 运行时解析
  promptImagePath : string
  promptTextKey   : string
  promptPosition  : PromptPosition   # enum
  completionCount : int              # default: 1
```

**GameLogic 扩展方法（不修改 GameProto）：**
```csharp
// 位于 GameLogic 程序集: TutorialStepExtensions.cs
public static class TutorialStepExtensions
{
    public static IReadOnlyList<TbTutorialStepData> GetByChapter(
        this TbTutorialStep table, int chapterId)
    {
        return table.DataList
            .Where(s => s.ChapterId == chapterId)
            .OrderBy(s => s.Order)
            .ToList();
    }

    public static GestureType[] ParseAllowedGestures(this TbTutorialStepData step)
    {
        if (string.IsNullOrEmpty(step.AllowedGestures)) return Array.Empty<GestureType>();
        return step.AllowedGestures
            .Split(',')
            .Select(s => Enum.Parse<GestureType>(s.Trim()))
            .ToArray();
    }
}
```

**MVP 教学步骤数据：**
| id  | stepKey        | chapterId | order | triggerCondition  | requiredGesture | completionCount |
|-----|----------------|-----------|-------|-------------------|-----------------|-----------------|
| 101 | tut_drag       | 1         | 1     | OnChapterEnter    | Drag            | 1               |
| 102 | tut_rotate     | 1         | 2     | OnPreviousStepDone| Rotate          | 1               |
| 103 | tut_snap       | 1         | 3     | OnPreviousStepDone| Drag            | 1               |
| 201 | tut_light      | 2         | 1     | OnChapterEnter    | LightDrag       | 1               |
| 301 | tut_distance   | 3         | 1     | OnChapterEnter    | Drag            | 1               |

**注意**：`allowedGestures` 列的值示例：`tut_drag` → `"Tap,Drag"`（允许 Tap 以便选中物件 + Drag 进行教学）

---

## Out of Scope

- [Story 001]: TutorialController 读取配置并驱动 FSM
- [Story 003]: InputFilter push/pop（白名单来自本 story 的配置字段）
- [Story 004]: 提示图片/文字的加载和显示
- 配置表的 Excel 文件创建（由策划负责，本 story 只定义 schema 和读取代码）

---

## QA Test Cases

### TC-002-01: 正确获取第 1 章步骤列表
**Given** `TbTutorialStep` 包含 `tut_drag(ch1,order=1)`, `tut_rotate(ch1,order=2)`, `tut_snap(ch1,order=3)`, `tut_light(ch2,order=1)`
**When** 调用 `Tables.Instance.TbTutorialStep.GetByChapter(1)`
**Then** 返回 3 个步骤，按 order 升序排列：[tut_drag, tut_rotate, tut_snap]

### TC-002-02: allowedGestures 解析正确
**Given** `tut_drag` 的 `allowedGestures` 字段值为 `"Tap,Drag"`
**When** 调用 `step.ParseAllowedGestures()`
**Then** 返回 `GestureType[] { GestureType.Tap, GestureType.Drag }`；无异常；数组长度 = 2

### TC-002-03: 不存在 ID 返回 null
**Given** `TbTutorialStep` 表中没有 id = 999 的步骤
**When** 调用 `Tables.Instance.TbTutorialStep.Get(999)`
**Then** 返回 `null`；不抛异常

### TC-002-04: Tables.Init 前不可访问
**Given** `Tables.Init()` 尚未被调用
**When** 尝试访问 `Tables.Instance.TbTutorialStep`
**Then** 抛出异常或返回未初始化状态（具体行为由 Luban 生成代码决定）；在日志中有明确错误信息

### TC-002-05: 空 allowedGestures 不崩溃
**Given** 某步骤的 `allowedGestures` 字段为空字符串
**When** 调用 `step.ParseAllowedGestures()`
**Then** 返回空数组 `GestureType[0]`，不抛异常

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: `tests/unit/Tutorial/TutorialStepConfigTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class TutorialStepConfigTests
{
    // Initialize Tables with test fixture data
    // Test GetByChapter ordering, ParseAllowedGestures parsing
    // Test null/empty edge cases
}
```

---

## Dependencies

- Depends on: Luban 工具链已集成到项目构建流程（依赖项目初始化）
- Unlocks: Story 001 (TutorialController 需要读取步骤配置), Story 003 (allowedGestures 数组作为 InputFilter 参数), Story 004 (promptImagePath + promptTextKey)
