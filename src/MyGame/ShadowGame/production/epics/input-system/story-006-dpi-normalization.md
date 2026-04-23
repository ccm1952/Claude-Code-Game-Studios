// 该文件由Cursor 自动生成

# Story 006: DPI Normalization & Luban Config

> **Epic**: Input System
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Config/Data
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-007`, `TR-input-017`, `TR-input-018`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: 所有手势识别阈值从 Luban 配置表 `TbInputConfig` 读取（禁止硬编码）；`dragThreshold` 通过 DPI 归一化公式 `baseDragThreshold_mm * Screen.dpi / 25.4` 在运行时计算，`Screen.dpi == 0` 时使用 fallback = 160；Settings 系统可在运行时修改触摸灵敏度（`TR-input-018`），通过重新计算 derived 值生效。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: 所有配置通过 `Tables.Instance.TbInputConfig.Get(id)` 读取（ADR-007）；`Tables.Init()` 必须在 `ProcedureMain` 步骤 7 完成后才读取，InputService 在步骤 9-10 初始化；配置对象读取后只读（禁止修改 Luban 生成的字段）；派生值（extension method）写在 GameLogic assembly
- Forbidden: 禁止在任何 C# 文件中硬编码阈值数值（`float threshold = 30f` 等）；禁止手动编辑 GameProto 下的 Luban 生成文件；禁止从 `UniTask.Run()` 线程池访问 `Tables.Instance`（main thread only）
- Guardrail: `Screen.dpi == 0` 兜底 fallbackDPI = 160；`baseDragThreshold_mm` 安全范围 2.0–5.0mm；运行时灵敏度修改通过 Settings 事件触发重算，不需要重启

---

## Acceptance Criteria

- [ ] Luban 配置表 `TbInputConfig` 包含全部 9 个参数：`baseDragThreshold_mm`（3.0）、`tapTimeout`（0.25）、`rotateThreshold`（8.0°）、`pinchThreshold`（0.08）、`minFingerDistance`（20.0）、`maxDeltaPerFrame`（100.0）、`fallbackDPI`（160.0）、`pcRotateSensitivity`（0.005）、`pcScrollSensitivity`（0.1）
- [ ] `InputService.Init()` 中从 `Tables.Instance.TbInputConfig` 加载全部阈值并缓存为 `_config` 字段（per-method-scope 读取规则的例外：热路径缓存）
- [ ] `dragThreshold` 运行时计算：`baseDragThreshold_mm * Screen.dpi / 25.4`；`Screen.dpi == 0` → 使用 `fallbackDPI`（来自 config）
- [ ] 计算结果验证（不硬断言精确值，允许浮点误差）：iPhone 13 Mini（476 DPI）→ ~37px；iPad（264 DPI）→ ~21px；PC（96 DPI）→ ~8px
- [ ] Settings 系统可通过 `GameEvent.Send(Evt_Settings_TouchSensitivityChanged, newMultiplier)` 触发 InputService 更新触摸灵敏度（`baseDragThreshold_mm` 的倍率）
- [ ] 灵敏度修改立即生效（同帧生效，不需要重启）；有效范围 multiplier ∈ [0.5, 2.0]（Luban 配置安全范围）
- [ ] C# 代码中无任何硬编码手势阈值数字（代码审查可通过 grep 验证）
- [ ] 配置加载时序正确：`Tables.Init()` 完成 → InputService 方可读取 `TbInputConfig`

---

## Implementation Notes

来自 ADR-010 + ADR-007 Implementation Guidelines：

**Luban 表结构设计（配置表 schema）**
```
table: TbInputConfig
  id: int (主键)
  baseDragThreshold_mm: float = 3.0
  tapTimeout: float = 0.25
  rotateThreshold: float = 8.0     // degrees
  pinchThreshold: float = 0.08
  minFingerDistance: float = 20.0  // px
  maxDeltaPerFrame: float = 100.0  // px
  fallbackDPI: float = 160.0
  pcRotateSensitivity: float = 0.005  // rad/px
  pcScrollSensitivity: float = 0.1
```

**配置加载（InputService.Init()）**
```csharp
private InputConfig _config; // cached, read from Luban at Init
private float _dragThreshold; // derived, recalculated on DPI or sensitivity change

public void Init()
{
    var row = Tables.Instance.TbInputConfig.Get(1); // single-row config table
    _config = new InputConfig
    {
        BaseDragThreshold_mm = row.BaseDragThreshold_mm,
        TapTimeout           = row.TapTimeout,
        RotateThreshold      = row.RotateThreshold * Mathf.Deg2Rad, // store in radians
        PinchThreshold       = row.PinchThreshold,
        MinFingerDistance    = row.MinFingerDistance,
        MaxDeltaPerFrame     = row.MaxDeltaPerFrame,
        FallbackDPI          = row.FallbackDPI,
        PcRotateSensitivity  = row.PcRotateSensitivity,
        PcScrollSensitivity  = row.PcScrollSensitivity,
    };
    RecalculateDerivedValues();
}
```

**DPI 归一化计算**
```csharp
private void RecalculateDerivedValues()
{
    float dpi = Screen.dpi > 0f ? Screen.dpi : _config.FallbackDPI;
    _dragThreshold = _config.BaseDragThreshold_mm * dpi / 25.4f * _sensitivityMultiplier;
}
```

**Settings 集成（运行时灵敏度修改）**
```csharp
private float _sensitivityMultiplier = 1.0f;

// 在 Init() 中注册：
GameEvent.AddEventListener<float>(EventId.Evt_Settings_TouchSensitivityChanged, OnSensitivityChanged);

private void OnSensitivityChanged(float multiplier)
{
    _sensitivityMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);
    RecalculateDerivedValues(); // 立即重算 dragThreshold
}
```

**Extension Method 示例（GameLogic assembly）**
```csharp
// InputConfigExtensions.cs (GameLogic, NOT GameProto)
public static class InputConfigExtensions
{
    public static float GetRotateThresholdRad(this TbInputConfigRow row)
        => row.RotateThreshold * Mathf.Deg2Rad;
}
```

**注意**：`Tables.Instance` 为常规类，不是单例，通过 `ConfigSystem` 属性访问。确保 InputService.Init() 在 `ProcedureMain` 步骤 9 执行（`Tables.Init()` 在步骤 7 已完成）。

---

## Out of Scope

- [Story 001]: dragThreshold 在 FSM 中的使用（消费本 Story 提供的 `_dragThreshold`）
- [Story 002]: rotateThreshold / pinchThreshold 在 DualFSM 中的使用
- [Story 008]: pcRotateSensitivity / pcScrollSensitivity 在 PC mapping 中的使用

---

## QA Test Cases

### TC-006-01: 所有阈值从 Luban 加载（非硬编码）
**Given** InputService 初始化完成  
**When** 使用 grep 扫描 `InputService.cs` 及相关文件  
**Then** 不存在 `3.0f`、`0.25f`、`8.0f`、`0.08f`、`20.0f`、`100.0f`、`160.0f` 等具体阈值数字；所有来自 `_config.XXX` 字段引用

### TC-006-02: DPI 归一化计算正确性（iPhone 13 Mini）
**Given** `baseDragThreshold_mm = 3.0`，`Screen.dpi = 476`，`sensitivityMultiplier = 1.0`  
**When** `RecalculateDerivedValues()` 执行  
**Then** `_dragThreshold ≈ 56.2px`（3.0 × 476 / 25.4）；误差 < 0.5px

### TC-006-03: DPI = 0 时 fallback 生效
**Given** `Screen.dpi = 0`，`fallbackDPI = 160`，`baseDragThreshold_mm = 3.0`  
**When** 计算 dragThreshold  
**Then** `_dragThreshold ≈ 18.9px`（3.0 × 160 / 25.4）；不抛 DivideByZero；不返回 NaN

### TC-006-04: 运行时灵敏度修改立即生效
**Given** `_dragThreshold = 37px`（初始值）  
**When** `GameEvent.Send(Evt_Settings_TouchSensitivityChanged, 0.5f)`  
**Then** 同帧内 `_dragThreshold = 18.5px`（减半）；后续手势以新阈值判定

### TC-006-05: 灵敏度 clamp 有效范围
**Given** multiplier 输入超出范围  
**When** `Send(Evt_Settings_TouchSensitivityChanged, 5.0f)` → clamped 到 2.0  
**When** `Send(Evt_Settings_TouchSensitivityChanged, 0.1f)` → clamped 到 0.5  
**Then** `_sensitivityMultiplier` 不超出 [0.5, 2.0]；`_dragThreshold` 在合理范围内

### TC-006-06: 配置加载时序保障
**Given** `Tables.Init()` 尚未完成（模拟 Tables.Instance 未准备好）  
**When** 过早调用 InputService.Init()  
**Then** 系统正确处理（抛出明确异常或延迟初始化）；不使用未初始化的配置数据导致静默错误

### TC-006-07: rotateThreshold 单位转换
**Given** `TbInputConfig.rotateThreshold = 8.0`（度）  
**When** InputService 加载配置  
**Then** 内部存储值 = `8.0 * Mathf.Deg2Rad ≈ 0.1396 rad`；FSM 使用弧度累积角度无需二次转换

---

## Test Evidence

**Story Type**: Config/Data  
**Required evidence**:
- `tests/unit/InputSystem/DpiNormalizationTests.cs`（DPI 计算公式单元测试）
- `tests/evidence/input-system/story-006-config-audit.md`（代码审查证明：无硬编码阈值）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None（Luban 配置工具链和 `Tables.Init()` 是先决条件，不是 story 依赖）
- Unlocks: Story 001（`_dragThreshold` 计算）、Story 002（旋转/缩放阈值）、Story 008（PC 灵敏度参数）
