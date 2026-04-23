// 该文件由Cursor 自动生成

# Story: UI Text Localization via ILocalizationModule (SP-009)

> **Epic**: ui-system
> **Story ID**: ui-system-010
> **Story Type**: Integration
> **GDD Requirement**: TR-ui-021 (All text via localization keys) — MVP 范围内实施
> **ADR References**: ADR-011 (UIWindow Management), SP-009 (I2 Localization)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001 至 ui-system-007)

## Context

TEngine 内嵌了 I2 Localization（已由 SP-009 验证），通过 `GameModule.Localization`（`ILocalizationModule`）接口访问，**禁止 `using I2.Loc`**。UI 文本组件使用 TEngine 提供的 `Localize` 组件（MonoBehaviour）绑定本地化 key，运行时语言切换无需重启场景。

本 story 将所有已实现的 UIWindow 面板中的静态文本替换为本地化 key 绑定，并验证中文简体 ↔ 英文切换后文本正确更新。

## Acceptance Criteria

- [ ] 所有 UIWindow 面板中的静态文本（按钮标签、标题、提示文字）均通过 TEngine `Localize` 组件绑定本地化 key，不使用硬编码字符串
- [ ] 本地化字符串表包含以下 key：`ui_btn_new_game`, `ui_btn_continue`, `ui_btn_settings`, `ui_btn_resume`, `ui_btn_quit`, `ui_title_pause_menu`, `ui_title_settings`, `ui_title_chapter_select`, `ui_title_puzzle_complete`, `ui_btn_hint`, `ui_label_master_volume`, `ui_label_music_volume`, `ui_label_sfx_volume`, `ui_label_language`（共 14+ keys，按面板需求扩展）
- [ ] 每个 key 提供至少两个语言版本：`Chinese (Simplified)` 和 `English`
- [ ] 运行时语言切换（Settings Dropdown → `GameModule.Localization.SetLanguage("English")`）后，所有可见面板文本在下一帧内更新为新语言，无需重启场景
- [ ] 动态文本（如章节名称、分数数字）使用代码拼接时，仍使用 `GameModule.Localization` 获取模板字符串（如 `"score_label"` → "得分: {0}"），不硬编码中文或英文文本
- [ ] 禁止 `using I2.Loc` 出现在任何 UIWindow 相关代码文件中
- [ ] 禁止直接调用 `LocalizationManager.CurrentLanguage` 或 `LocalizationManager.GetTranslation()`
- [ ] 语言选择持久化：语言设置通过 PlayerPrefs（key: `"language"`）保存，游戏重启后恢复为上次选择的语言

## Implementation Notes

- `Localize` 组件挂载到 TextMeshPro 文本组件所在 GameObject，在 Inspector 中设置 Term（本地化 key）
- 本地化字符串表文件位置：`Assets/Resources/I2Languages.asset`（TEngine 内嵌 I2 的默认位置，以实际项目为准）
- 运行时语言切换 API：`GameModule.Localization.SetLanguage("Chinese (Simplified)")`（SP-009 确认的正确 API）
- 语言名称字符串与 I2 内部语言名称一致（从项目现有 I2Languages.asset 中确认支持的语言列表）
- `OnCreate` 中不需要手动更新文本，`Localize` 组件自动在 Start 时和语言切换时刷新

## Out of Scope

- 完整 I2 字符串表的内容填充（由翻译人员负责）
- 音频本地化（VO）
- 字体替换（不同语言使用不同字体，P2 功能）

## QA Test Cases (Integration)

### TC-001: 中文简体默认显示
- **Given**: 默认语言为中文简体（PlayerPrefs 无 language 记录）
- **When**: 打开 MainMenuPanel
- **Then**: "新游戏" / "继续" / "设置" 按钮文本以中文显示

### TC-002: 运行时英文切换
- **Given**: 游戏运行中，显示中文
- **When**: Settings Dropdown 选择 "English" → `GameModule.Localization.SetLanguage("English")` 调用
- **Then**: 所有可见面板文本在下一帧切换为英文（"New Game" / "Continue" / "Settings"）；无控制台 I2 命名空间错误

### TC-003: 语言持久化跨会话
- **Given**: 语言设置为 English，PlayerPrefs["language"] = "English"
- **When**: 模拟游戏重启（重新 Init）
- **Then**: 游戏以 English 语言启动

### TC-004: 无 I2.Loc using 声明
- **Setup**: 扫描所有 UIWindow 相关 .cs 文件
- **Verify**: 无文件包含 `using I2.Loc` 声明
- **Pass**: grep 结果为空

### TC-005: 禁止直接 LocalizationManager 调用
- **Setup**: 扫描所有 UIWindow 相关 .cs 文件
- **Verify**: 无 `LocalizationManager.CurrentLanguage` 或 `LocalizationManager.GetTranslation` 调用
- **Pass**: grep 结果为空

## Test Evidence Path

- `production/qa/evidence/ui-system/localization-evidence.md`（含截图：中英切换对比）
- `tests/integration/UILocalization_Switch_Test.cs`

## Dependencies

- ui-system-001 至 ui-system-007：所有面板已实现
- SP-009: I2 Localization（`GameModule.Localization` API 确认）
- Control Manifest §2.9: Localization 规范（禁止 I2.Loc，使用 ILocalizationModule）
- ADR-008: PlayerPrefs 存储语言设置
