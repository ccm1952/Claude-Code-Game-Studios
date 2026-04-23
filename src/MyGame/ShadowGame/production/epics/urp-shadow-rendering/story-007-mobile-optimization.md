// 该文件由Cursor 自动生成

# Story 007: 移动端专项优化（Mobile-Specific Optimizations）

> **Epic**: URP Shadow Rendering
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Config/Data
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-005`, `TR-render-018`, `TR-render-023`, `TR-render-017`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-005: Shadow distance 8m 可配置（⚠️ ADR Coverage 标记为 configurable 未完全覆盖）
- TR-render-018: Shadow 内存 ≤ 15MB（Medium 档总计：主 Shadow Map + Additional + ShadowRT）
- TR-render-023: 所有影子设置通过配置文件调节，无硬编码
- TR-render-017: Draw call 预算 ≤ 150（移动端 Medium 档）/ Shadow pass 额外 < 20

**ADR Governing Implementation**: ADR-002: URP Rendering Pipeline；ADR-003: Mobile-First Platform
**ADR Decision Summary**: 移动端（iPhone 13 Mini 基准）维持 60fps，所有 Shadow 性能参数通过 Luban 配置表按档位配置。Shadow Atlas 大小、depthBias、normalBias、shadowDistance、cascadeBlendDistance 等 tuning knobs 全部数据驱动，不硬编码。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: LOW
**Engine Notes**: URP 2022.3 在移动端（iOS Metal / Android Vulkan/OpenGLES3）的 shadow pipeline 已成熟验证。ASTC 纹理压缩在 iOS 上有效，ShadowRT 的 R8 格式在 Metal 上原生支持。

**Control Manifest Rules (this layer)**:
- Required: Mobile 是主要目标平台——所有性能基准以移动端为准（ADR-003）
- Required: 所有 Shadow 调优参数（depthBias, normalBias, shadowDistance, cascadeBlendDistance, shadowAtlasSize）通过 Luban `TbRenderConfig` 配置，不硬编码（ADR-007）
- Required: 纹理压缩：iOS 使用 ASTC，Android 使用 ASTC / ETC2（ADR-003）
- Required: draw call 预算验证：移动端每帧总 draw calls ≤ 150（硬上限）（ADR-003）
- Required: Shadow memory 验证：Shadow Map + ShadowRT 合计 ≤ 15MB（Medium 档）（GDD）
- Forbidden: 不为 PC 的性能余量设计功能——如果系统需要超过移动端预算，必须实现 LOD/quality scaling（ADR-003）
- Forbidden: 不硬编码 `depthBias = 0.5f` 等数值（ADR-007）
- Guardrail: iPhone 13 Mini Medium 档 60fps（帧时间 < 16.67ms）；低端 Android Low 档 ≥ 45fps（ADR-002）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md`，scoped to this story:*

- [ ] **AC-1**: 所有以下 Tuning Knobs 通过 Luban `TbRenderConfig` 配置（无硬编码），且有合理默认值：

| 参数 | 默认值 | 合理范围 | 说明 |
|------|:------:|:--------:|------|
| `mainShadowResolution` | 1024 (Medium) | 512-2048 | 主光源 Shadow Map 分辨率 |
| `additionalShadowResolution` | 512 (Medium) | 256-1024 | 额外光源 Shadow Map 分辨率 |
| `shadowAtlasSize` | 2048 (Medium) | 1024-4096 | Shadow Atlas 总大小 |
| `shadowDistance` | 8m | 5-15m | 阴影最大投射距离 |
| `depthBias` | 0.5 | 0.1-2.0 | 防止 shadow acne |
| `normalBias` | 0.3 | 0.1-1.0 | 防止 peter-panning |
| `shadowEdgeSoftness` | 1.0px | 0-4px | 边缘柔化 |
| `cascadeBlendDistance` | 0.3 | 0.1-0.5 | Cascade 过渡平滑距离 |
| `asyncReadbackFrequency` | 每帧 | 每帧/隔帧/每3帧 | Readback 频率 |
| `maxShadowCastingLights` | 2 | 1-3 | 最大实时阴影光源数 |

- [ ] **AC-2**: Medium 档 Shadow 内存总量 ≤ 15MB（通过计算公式和 Unity Memory Profiler 双重验证）：
  - Main Shadow Map: 1024×1024×2B = 2MB
  - Shadow Atlas: 2048×2048×2B = 8MB
  - Additional (2灯): 2×(512×512×2B) = 1MB
  - ShadowRT: 1024×1024×1B = 1MB
  - 合计 ≤ 12MB（留余量至 15MB）

- [ ] **AC-3**: iPhone 13 Mini（Medium 档）稳定 60fps（帧时间 < 16.67ms），连续 60 秒无掉帧；低端 Android Low 档稳定 ≥ 45fps（帧时间 < 22ms）

- [ ] **AC-4**: Mobile Medium 档每帧总 draw calls ≤ 131（硬上限 150 的 ~87%，留安全余量）：
  - sceneGeometry ≤ 50
  - shadowPasses ≤ 60（2 lights × 15 casters × 2 cascades）
  - postProcessing ≤ 6
  - UI ≤ 15

- [ ] **AC-5**: Shadow pass 额外 draw calls < 20（Frame Debugger 在 Medium 档验证）

- [ ] **AC-6**: 装饰物件（`Cast Shadows = Off`）和仅投影不渲染物件（`Cast Shadows = Shadows Only`）的 Layer 配置规则文档化并在测试场景中正确设置

- [ ] **AC-7**: 设备从后台返回时（`OnApplicationFocus(true)`），ShadowRT 正确重建——无丢失 / 黑屏 / 白屏（Render Texture 丢失恢复）

- [ ] **AC-8**: `Soft Shadows = Off` 在 Low/Medium 档下无 PCF 软化开销；High 档 `Soft Shadows = On`，Shader GPU 时间仍 < 0.5ms

---

## Implementation Notes

*Derived from ADR-002, ADR-003, control-manifest.md §7:*

**Luban 配置表 `TbRenderConfig` 结构（新增以下字段）：**
```
int id                      // 质量档位 ID（0=Low, 1=Medium, 2=High）
int MainShadowResolution
int AdditionalShadowResolution
int ShadowAtlasSize
float ShadowDistance
float DepthBias
float NormalBias
float ShadowEdgeSoftness
float CascadeBlendDistance
int AsyncReadbackFrequency  // 0=每帧, 1=隔帧, 2=每3帧
int MaxShadowCastingLights
bool SoftShadowsEnabled
int ShadowRTResolution
```

**参数应用（`ApplyQualityTier()` 扩展）：**
```csharp
var config = Tables.Instance.TbRenderConfig.Get((int)tier);
_urpAsset.shadowDistance = config.ShadowDistance;
_urpAsset.mainLightShadowmapResolution = (ShadowResolution)config.MainShadowResolution;
_urpAsset.additionalLightsShadowmapResolution = (ShadowResolution)config.AdditionalShadowResolution;
// depthBias / normalBias 通过 Light 组件设置，非 URP Asset
_mainLight.shadowBias = config.DepthBias;
_mainLight.shadowNormalBias = config.NormalBias;
```

**后台返回 ShadowRT 恢复：**
```csharp
void OnApplicationFocus(bool hasFocus)
{
    if (hasFocus && (_shadowRT == null || !_shadowRT.IsCreated()))
    {
        var config = Tables.Instance.TbRenderConfig.Get((int)_currentTier);
        _shadowRT = new RenderTexture(config.ShadowRTResolution, config.ShadowRTResolution, 0, RenderTextureFormat.R8);
        _shadowRT.Create();
        _shadowCamera.targetTexture = _shadowRT;
    }
}
```

**Shadow 内存验证公式（来自 GDD Formulas）：**
```
shadowMemory = atlasW×atlasH×bytesPerPixel + numAdditionalLights×additionalSize²×bytesPerPixel + shadowRT×1
```
Medium 档实际值：2048²×2 + 2×512²×2 + 1024²×1 = 8MB + 1MB + 1MB = 10MB ≤ 15MB ✓

**Draw Call 预算分配（Medium 档 GDD 目标）：**
```
sceneGeometry  ≤ 50  (SRP Batcher + Static Batching)
shadowPasses   ≤ 60  (2 lights × 15 casters × 2 cascades)
postProcessing ≤ 6
UI             ≤ 15
────────────
total          ≤ 131 (硬上限 150 的 87%，留缓冲)
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 001**: ShadowRT 初次创建（本 story 只补充后台返回时的重建逻辑和参数完善）
- **Story 004**: 三档切换逻辑（本 story 完善配置参数覆盖度和内存/draw call 验收）
- **ADR-003 Platform epic**: APK/IPA 大小优化、纹理压缩流程（不在本 epic 范围内）

---

## QA Test Cases

*Config/Data story — 配置验证 + 性能测试:*

- **AC-1**: 参数全量来自 Luban（无硬编码）
  - Given: `TbRenderConfig` 中 Medium 行的 `ShadowDistance` 改为 `12f`
  - When: 重新运行游戏并应用 Medium 档位
  - Then: `_urpAsset.shadowDistance == 12f`；无任何代码路径 fallback 到硬编码的 `8f`
  - Edge cases: Luban 表中参数值超出合理范围时（如 Resolution = 0），系统 clamp 到最小值并 `Debug.LogWarning`

- **AC-2**: 内存预算
  - Given: Medium 档位应用
  - When: 使用 Unity Memory Profiler 快照
  - Then: Shadow Maps + ShadowRT 合计 ≤ 15MB；核心内存计算公式与实测值误差 < 10%
  - Edge cases: High 档位下内存更高，但不超过 ADR-003 的 1.5GB 总预算

- **AC-3**: 移动端帧率
  - Given: iPhone 13 Mini（Medium 档）+ 测试场景（2 个 shadow-casting 光源 + 10 个可交互物件）
  - When: 连续 60 秒 gameplay（拖拽物件 + 谜题匹配）
  - Then: 帧时间 P95 < 16.67ms；无帧时间 > 33ms 的掉帧
  - Edge cases: 低端 Android（Low 档，Mali-G52 GPU）帧时间 P95 < 22ms（45fps 基准）

- **AC-4 & AC-5**: Draw call 预算
  - Given: Medium 档，2 个光源 + 15 个 shadow caster + SMAA + UI
  - When: 打开 Frame Debugger 录制一帧
  - Then: 总 draw calls ≤ 131；Shadow pass 相关 draw calls < 20
  - Edge cases: `totalDrawCalls > 130` 时，系统优先禁用最低优先级 shadow caster（GDD Formulas edge case）

- **AC-7**: 后台返回 ShadowRT 恢复
  - Given: 应用运行中，手动触发 `OnApplicationFocus(false)` 再 `OnApplicationFocus(true)`
  - When: 恢复后等待 2 帧
  - Then: `_shadowRT.IsCreated() == true`；`_shadowCamera.targetTexture == _shadowRT`；墙面渲染无黑屏
  - Edge cases: 连续 3 次后台返回，ShadowRT 不泄漏（Memory Profiler 确认）

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- 通过 smoke check：`tests/smoke/critical-paths.md` 中的"影子渲染冒烟项"全部通过
- `production/qa/evidence/mobile-optimization-evidence.md`：包含以下内容：
  - iPhone 13 Mini 帧时间截图（Medium 档，60s 录制）
  - Memory Profiler 快照（Medium 档 Shadow 内存）
  - Frame Debugger 截图（draw call 计数）
  - 低端 Android 帧率截图（Low 档）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（ShadowRT 创建），Story 004（质量档位系统），所有 Luban `TbRenderConfig` 配置表字段完整定义
- Unlocks: 本 story 完成后，整个 `urp-shadow-rendering` epic 的 Definition of Done 条件中"性能验收"部分完成
