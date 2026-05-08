// 该文件由Cursor 自动生成

# 问题记录：MonoBehaviour 类名与 .cs 文件名不一致导致脚本无法挂载

> **日期**: 2026-04-23
> **发生次数**: 1 次（SP-011 YooAsset Additive Spike 执行期间）
> **严重性**: 高（直接阻塞 Spike 自动化 + 误判为 HybridCLR 问题 + 误触发跨项目"回滚"连带事故）

## 问题现象

SP-011 Spike 过程中，Agent 创建的测试脚本同时定义了两个 public 类：

```csharp
// 文件：Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveTest.cs
public class SP011_YooAssetAdditiveTest { ... }             // 纯 C# 测试逻辑
public class SP011_YooAssetAdditiveLauncher : MonoBehaviour { ... }  // MonoBehaviour
```

**症状链**：

1. `gameobject-component-add` MCP 调用返回成功，Launcher 组件挂上了
2. `scene-save` 保存成功
3. Inspector 显示 `SP011_Launcher` 上有一个 `(Script)` 组件，标黄色警告：
   **"The associated script can not be loaded. Please fix any compile errors and assign a valid script."**
4. Project 搜索 `SP011` 只返回 3 个资产：`SP011_SceneA.unity` / `SP011_SceneB.unity` / `SP011_YooAssetAdditiveTest.cs` —— **没有 `SP011_YooAssetAdditiveLauncher.cs`**（因为类和文件名不一致，Unity 不把它作为可独立识别的 MonoBehaviour 注册）
5. Add Component 搜 `SP011` 搜不到 Launcher 类
6. PlayMode 启动后 `Awake` 从未执行 → `SP011_Result.json` 从未生成 → Console 无任何 `[SP-011]` 日志
7. 在真正查明根因前，Agent 误判为"HotFix Assembly 在 Editor 模式下 MonoBehaviour 注册有问题 / HybridCLR 干扰 / MCP 绑定错项目"等一连串错误假设

## 根本原因

**Unity 的硬性规则**：继承自 `MonoBehaviour`（或 `ScriptableObject`）的类**必须**放在**与类名完全同名**的 `.cs` 文件中，否则：

- Unity 不会把该类注册为可附加到 GameObject 的 MonoBehaviour
- 无法在 Add Component 搜索结果里出现
- 场景/Prefab 里已挂载的 Component 引用会显示 "The associated script can not be loaded"（即使编译成功、类型存在）
- 实例化 MonoBehaviour 的所有生命周期方法（`Awake` / `OnEnable` / `Start` / `Update`）**全部不会被调用**

### 为什么容易踩坑

| 原因 | 说明 |
|------|------|
| 语法上合法 | C# 允许一个 `.cs` 文件定义多个 public 类；非 MonoBehaviour 类没有这个限制，容易以为 MonoBehaviour 也一样 |
| 编译通过 | Unity 编译不会报错、不会警告，只在 Add Component / 场景反序列化时才体现 |
| 错误信息误导 | "can not be loaded, fix compile errors" 暗示编译错误，但实际编译无错 — 真实原因是文件名不匹配 |
| Agent 自动化视角看不见 | `gameobject-component-add` 通过 FullName 能成功"添加"，但 Unity 运行时反序列化时找不到 MonoScript guid，"失败"发生在后续的场景加载环节，跟添加操作解耦 |

## 连锁影响（本次事故链）

1. Launcher 从未执行 → 无任何日志/文件产出
2. Agent 误判为 Console 被 Clear on Play 清空 → 关了 Clear on Play（无效修复，但无害）
3. Agent 误判为 MCP 绑定错项目（Girls/928）→ 在**当时以为是 Girls/928**的 main.unity 里执行了 `gameobject-destroy` + `scene-save` "回滚"，实际是操作在**正确的 ShadowGame** 项目上，导致 ShadowGame 原本已挂好的 Launcher GameObject 被真实误删
4. 花费 ~45 分钟在错误假设上排查（HybridCLR、MCP 绑定、PlayMode 状态），最终通过 Project 搜索截图才发现只有 1 个 .cs 文件

## 解决方案

### 本次立即修复

将 Launcher 类拆到独立文件：

- `Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveTest.cs` — 仅含 `SP011_YooAssetAdditiveTest`
- `Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveLauncher.cs` — 仅含 `SP011_YooAssetAdditiveLauncher : MonoBehaviour`

修复后 Inspector Add Component 能搜到 `SP011 Yoo Asset Additive Launcher`，脚本关联建立，`Awake/Start` 正常执行。

### 工程规则层面（防复发）

在 `src/MyGame/ShadowGame/CLAUDE.md` 的「🎯 TEngine 任务前置协议」增加一条**编码硬约束**（适用于所有 Unity C# 文件，不只 TEngine）：

> **MonoBehaviour / ScriptableObject 文件命名规则（MUST）**
>
> 所有继承自 `UnityEngine.MonoBehaviour` 或 `UnityEngine.ScriptableObject` 的 public 类，**必须**单独放在一个与类名**完全同名**的 `.cs` 文件中（大小写敏感）。
>
> - ❌ 禁止：一个 `.cs` 文件定义多个 MonoBehaviour 类
> - ❌ 禁止：MonoBehaviour 类与其他（包括非 MonoBehaviour）public 类共存在同一文件
> - ✅ 允许：同一文件中 MonoBehaviour 类 + 同命名空间下的 `private` / `internal` 辅助类（如枚举、只被该 MB 使用的嵌套类）
> - ✅ 允许：非 MonoBehaviour 的纯 C# 类可以多个共存一个文件
>
> **自检方法**：创建/修改涉及 MonoBehaviour 的文件后，调 `Glob` 搜 `**/类名.cs` 确认文件名匹配；若不匹配立即重构。

### Agent 行为规则（防误判）

当 Unity 出现 "The associated script can not be loaded" 警告时，**优先按这个顺序排查**（不要再一开始就猜 HybridCLR / Assembly 加载）：

1. **文件名检查**（1 分钟内能完成）：Project 搜索类名，确认存在同名 `.cs` 文件
2. **编译错误检查**（30 秒）：`console-get-logs` 过滤 Error
3. **命名空间冲突检查**（2 分钟）：是否有两个同名类
4. 以上都排除了，才考虑 Assembly / HybridCLR / ScriptingBackend 层面问题

## 受影响 / 已更新的文档

- [x] `/.claude/memory/problem_2026-04-23_monobehaviour-filename-mismatch.md` — 本文件
- [x] `src/MyGame/ShadowGame/CLAUDE.md` — 已追加编码红线第 6 条 + MonoBehaviour 挂载前置自检
- [x] `.cursor/rules/shadowgame-tengine.mdc` — 已同步第 6 条红线 + 快速检查清单
- [x] `src/MyGame/ShadowGame/AGENTS.md` — 已从"五条"升级为"六条编码红线"并补入该规则
- [ ] `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` — 可补充该规则（次优先级，延后处理）

## 预防复发的机制

1. **强制文件名匹配自检**：任何创建或编辑 `class X : MonoBehaviour` 的 .cs 文件后，Agent 必须 `Glob **/X.cs` 确认文件名与类名一致
2. **MCP 挂载前置检查**：调 `gameobject-component-add` 前，先 `Glob` 确认目标组件类有同名 .cs 文件，否则预警用户
3. **Add Component 搜不到的 2 分钟规则**：用户反馈 Inspector 搜不到 MonoBehaviour 时，Agent 的第一动作必须是 `Glob` 检查文件名，**不得**直接跳到 HybridCLR / Assembly 问题
4. **Spike/一次性脚本模板**：以后创建此类 MonoBehaviour 时，默认每个 MonoBehaviour 独立一个文件，即使只有几十行代码也不合并

## 复盘：本次流程的教训

| 教训 | 原因 |
|------|------|
| "脚本编译过 ≠ MonoBehaviour 能挂载" | Unity 的 MonoBehaviour 识别依赖文件名，不只看类型系统 |
| 错误假设成本很高 | 从"MonoBehaviour 不生效"跳到"MCP 绑定错项目"是 4 层推理 + 1 次破坏性操作，真正的根因只差 1 层（Glob 搜 .cs）|
| 看截图比看日志先 | 截图直接显示 "can not be loaded"，Console 日志只显示"没日志"，前者信号更强 |
| 项目搜索截图是廉价的黄金证据 | Project 搜 `SP011` 只有 3 个结果，一目了然缺 Launcher.cs；如果第一时间让用户搜，整个事故可避免 |
