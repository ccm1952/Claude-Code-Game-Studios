// 该文件由Cursor 自动生成

# 问题记录：C# 字符串字面量内嵌套未转义 ASCII 双引号导致 CS1003 编译错误

> **日期**: 2026-04-29
> **发生次数**: ≥ 2 次（S2-09 InteractableObject.cs 编译错误链中疑似 1 次 + 本次 S2-10 GridSnapTests.cs 显式 1 次）
> **严重性**: 中（编译阻塞，定位简单但反复发生即时间浪费）

## 问题现象

C# 文件中出现形如下面的字符串字面量：

```csharp
Assert.AreEqual(InteractableObjectState.Idle, _io.Fsm.CurrentState,
    "浮点小偏差 < epsilon → 仍走"已在格点"路径，无 DOMove");
//                            ^^^^^^^^^^         ^
//                       第二个 ASCII " 闭合首串    第三个 " 开新串
```

Unity / Roslyn 报：

```
Assets/Tests/EditMode/ObjectInteraction/GridSnapTests.cs(214,39): error CS1003: Syntax error, ',' expected
Assets/Tests/EditMode/ObjectInteraction/GridSnapTests.cs(214,43): error CS1003: Syntax error, ',' expected
```

报错列号通常落在嵌套引号处或其后；错误信息 "Syntax error, ',' expected" / "Syntax error, ')' expected" / "; expected" 都是同一类问题的不同表征。

## 根本原因

C# 普通字符串字面量用 ASCII `"` 作为定界符。Agent 写中文/英文文档语境的引号引用时，**直接把外部说理风格的"…"原样搬进字符串字面量**，没意识到 `"` 是 C# 的语法字符。

**编译器视角**：

```
"浮点小偏差 < epsilon → 仍走"  ← 完整字符串字面量
已在格点                       ← 裸标识符（CS0103 / CS1003）
"路径，无 DOMove"              ← 第二个完整字符串字面量
");                            ← 多余的逗号 / 括号
```

### 为什么容易踩坑

| 原因 | 说明 |
|------|------|
| 中文写作惯性 | 写说明性文字时本能用"嵌套引号"来引用术语，没区分这是文档（.md）还是代码（.cs）|
| 视觉相似 | 编辑器里 ASCII `"` 与 ASCII `"` 字形几乎一致，但语义完全不同（前者闭合字符串，后者只是字符）|
| 跨语言惯性 | Markdown / YAML / JSON 字符串嵌套规则各不相同（YAML 单引号包双引号无需转义；Markdown 不需要；C# 需要 `\"` 或 `""` 或 verbatim `@""`），切换语言时易混淆 |
| 错误信息误导 | CS1003 报 "Syntax error, ',' expected"，引导人去看分隔符而非引号问题 |

## 解决方案（任选）

按推荐度从高到低：

### 1. 中文文案优先用中文标点（推荐 default）

代码内的描述性消息（log / Assert message / exception message）大多是给开发者看的中文文档式表达，应当用中文标点系统：

| 替代符 | 用途 | 示例 |
|---|---|---|
| `『…』` | 书名号（推荐 — 美观、明确） | `"走『已在格点』路径"` |
| `「…」` | 角引号 | `"按「Skill-first 原则」处理"` |
| `《…》` | 书名号（用于真书/章节名） | `"参《ADR-013》§Grid Snap"` |
| `'…'` | 单引号（中英混用时） | `"参 'ADR-013'"` |

中文标点既不与 C# 语法冲突，又比转义字符可读性高得多。

### 2. 转义 `\"`

最朴素的兼容方式：

```csharp
"浮点小偏差 < epsilon → 仍走\"已在格点\"路径，无 DOMove"
```

适合**短**的、必须保留 ASCII 引号的场景（如序列化 JSON 模板、SQL）。

### 3. Verbatim 字符串 `@"..."` 内用 `""` 转义

适合多行模板：

```csharp
@"浮点小偏差 < epsilon → 仍走""已在格点""路径，无 DOMove"
```

### 4. C# 11+ 原始字符串字面量 `"""..."""`

如果 Unity 项目语言版本支持（C# 11 / .NET 7+；Unity 2022.3 LTS 默认 C# 9，需要 `<langVersion>` 提升）：

```csharp
"""浮点小偏差 < epsilon → 仍走"已在格点"路径，无 DOMove"""
```

⚠ Unity 项目慎用——多数 LTS 默认 C# 9，写出来 IDE 不报错但 mono 编译报错。

## 类似问题的语言矩阵（防类比误用）

| 语言 | 普通字符串内嵌 ASCII `"` 的处理 |
|------|-------------------------------|
| C# | `\"` 或 verbatim `@""` 或 raw `"""` |
| C / C++ / Java / Kotlin / JS / TS | `\"` |
| Python | `\"` 或 `'...'` 包 `"...":` 或 `"""..."""` |
| Rust | `\"` 或 `r#"..."#` |
| Go | `\"` 或 backtick raw string |
| Lua | `\"` 或 `'...'` 或 `[[...]]` |
| GDScript / Godot | `\"` 或 `'...'` |
| YAML | 单引号包字符串内可写 `"`，双引号包则需 `\"` |
| JSON | 必须 `\"`（无 verbatim 模式） |
| Shell（Bash/Zsh） | `\"` 或单引号包但单引号内不可再嵌单引号 |

## Agent 自检清单（写每段 C# 代码前问自己）

写**任何** Assert message / Log.* / Exception message / `throw new XxxException(...)` 前：

1. 这段消息里有没有 `"`？
2. 如果有：
   - **是中文术语引用**？→ 用 `『』` / `「」` 替换（最优解）
   - **是 ASCII 必须保留**？→ 用 `\"` 转义；或改用 `@"...""...""..."` verbatim
3. 写完后**视觉扫一遍**：每行字符串字面量内的 `"` 计数，必须是偶数（成对）。

## 受影响 / 已更新的文档

- [x] `.claude/memory/problem_2026-04-29_csharp-string-literal-nested-quotes.md` — 本文件
- [x] `src/MyGame/ShadowGame/CLAUDE.md` — 编码红线表追加第 8 条（字符串字面量嵌套引号约束）
- [x] `~/.cursor/rules/string-literal-nested-quotes.mdc` — 跨工程通用 cursor rule（适用所有 .cs/.ts/.js/.py/.go/.rs/.java/.kt/.cpp 等）

## 预防复发的机制

1. **写 C# 前的引号自检**（agent 强制）：写任何 C# 字符串字面量前，对包含中文术语的引用一律先用 `『』` / `「』」` 替代 ASCII `"`，仅在序列化场景（JSON 模板等）才用 `\"`。
2. **编译错误第一排查项**：CS1003 / CS1525 / CS1026 / CS1010 类 "Syntax error, X expected" 在中文文件里出现时，**第一动作是 `Grep` 该行查 `"` 计数**（应该成对）；不要先怀疑代码逻辑。
3. **测试编辑后预扫**：`StrReplace` / `Write` 完成后若涉及 C# 字符串字面量，调一次 `Grep -n '"'` 看看每行引号计数是否反常（极快的 sanity check）。

## 复盘：本次流程的教训

| 教训 | 原因 |
|------|------|
| 中文写作切到代码时容易"惯性嵌套引号" | 文档与代码使用的是同一种 ASCII 字符，但语义不同 |
| 中文标点（『』「」）是免费的"解" | 既无须转义、又比 `\"` 可读，应当作 default |
| CS1003 信号弱 | 只看错误列号会被引导到分隔符上；要看错误所在那一行整体引号配对 |
| 反复犯同一错误 = 应当沉淀规则 | 当一个错误出现 ≥ 2 次（S2-09 + S2-10），说明 agent 没有内化此规则，必须写到 rules 而非靠"下次注意" |
