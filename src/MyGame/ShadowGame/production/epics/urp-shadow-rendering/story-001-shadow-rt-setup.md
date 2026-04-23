// 该文件由Cursor 自动生成

# Story 001: ShadowRT RenderTexture & Camera Setup

> **Epic**: URP Shadow Rendering
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-009`, `TR-render-003`, `TR-render-004`, `TR-render-015`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-009: ShadowSampleCamera + ShadowRT 独立渲染管线
- TR-render-003: Shadow map 分辨率按质量档分级（512/1024/2048）
- TR-render-004: Shadow Cascades 按质量档配置（1/2/4 级）
- TR-render-015: Shadow Only 渲染模式（物件不可见但影子可见）

**ADR Governing Implementation**: ADR-002: URP Rendering Pipeline
**ADR Decision Summary**: 采用 URP 原生方向光级联阴影贴图 + ShadowSampleCamera 独立渲染到 R8 格式 RenderTexture（512×512），通过 `CameraType.Game` + 独立 render target 与主 Camera Stack 隔离。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: MEDIUM
**Engine Notes**: URP 2022.3 支持运行时修改 `UniversalRenderPipelineAsset.shadowResolution`、`shadowCascadeCount`、`softShadows`。ShadowSampleCamera 不加入 Camera Stack，使用独立 render target。

**Control Manifest Rules (this layer)**:
- Required: ShadowSampleCamera 使用 `CameraType.Game` + 独立 render target，不加入 Camera Stack（ADR-002）
- Required: ShadowRT 格式 R8 (8-bit grayscale)，512×512，~256KB 内存（ADR-002）
- Required: 所有 Shadow Map 分辨率参数从 Luban config 读取，不硬编码（ADR-007）
- Required: ShadowRT 在模块初始化时创建，模块销毁时释放（ADR-002 资源闭环原则）
- Required: 所有模块访问通过 `GameModule.XXX` 静态访问器（ADR-001）
- Forbidden: 不得使用 `Camera.main` per-frame — 缓存 camera 引用（ADR-012）
- Forbidden: 不得硬编码分辨率数值如 `512`、`1024`、`2048`（ADR-007）
- Guardrail: ShadowRT CPU readback ≤ 1.5ms；ShadowRT 内存 ~256KB（ADR-002）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md`，scoped to this story:*

- [ ] **AC-1**: `ShadowSampleCamera` 以正交投影对准投影墙面，Culling Mask 仅包含 ShadowCaster Layer + Wall Surface Layer，不加入主 Camera Stack
- [ ] **AC-2**: `ShadowRT` 以 R8 格式、512×512 分辨率创建，内存占用 ≤ 256KB
- [ ] **AC-3**: ShadowRT 的 Depth Texture 和 Opaque Texture 按需启用（Shadow RT 采样需要 Depth）
- [ ] **AC-4**: Shadow Cascade 数量在 High 档 = 4，Medium 档 = 2，Low 档 = 1，通过 `UniversalRenderPipelineAsset` 运行时配置
- [ ] **AC-5**: Shadow Only 渲染物件（"只有影子无本体"谜题物件）所在 Layer 被主 Camera Culling Mask 排除，但在 ShadowSampleCamera 的 Culling Mask 中保留
- [ ] **AC-6**: 系统 Dispose 时 `ShadowRT.Release()` 被调用，无 RenderTexture 内存泄漏
- [ ] **AC-7**: 渲染系统初始化（含 ShadowRT 创建）完成时间 ≤ 500ms

---

## Implementation Notes

*Derived from ADR-002 & control-manifest.md:*

**ShadowSampleCamera 配置：**
- `cameraType = CameraType.Game`
- `targetTexture = _shadowRT`（独立 render target，不与主 Camera 共享）
- `orthographic = true`，projection 对准投影墙面 AABB
- `cullingMask` = `ShadowCasterLayer | WallSurfaceLayer`（包含 Shadow Only Layer）
- 不调用 `camera.AddCameraToStack()`，不加入主 Camera Stack

**ShadowRT 创建：**
```csharp
_shadowRT = new RenderTexture(512, 512, 0, RenderTextureFormat.R8);
_shadowRT.name = "ShadowRT";
_shadowRT.filterMode = FilterMode.Bilinear;
_shadowRT.Create();
```
分辨率 512 来自 Luban `TbRenderConfig` 而非硬编码。

**Shadow Map 参数：**
- 通过运行时修改 `UniversalRenderPipelineAsset` 实例的 `shadowResolution`、`shadowCascadeCount` 切换档位
- `depthBias = 0.5`，`normalBias = 0.3`（防止 shadow acne / peter-panning），参数来自 Luban config
- `shadowDistance = 8m`（来自 Luban，TR-render-005 标记为 ⚠️ configurable 要求）

**初始化顺序：**
```
Tables.Init() 完成 → 读取 TbRenderConfig → 创建 ShadowRT → 配置 ShadowSampleCamera → 进入 Active 状态
```

**资源闭环：**
- `OnModuleInit()`: 创建 ShadowRT 并缓存 camera 引用
- `OnModuleDestroy()`: `_shadowRT?.Release(); _shadowRT = null;`

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: WallReceiver HLSL shader 实现（本 story 只建立渲染管线框架）
- **Story 003**: AsyncGPUReadback 回读管线（本 story 只创建 RT，不实现 CPU 读取）
- **Story 004**: 运行时质量档位切换逻辑（本 story 仅按初始档位创建 RT）
- **Story 007**: 移动端分辨率动态缩放（本 story 以固定参数初始化）

---

## QA Test Cases

*Logic story — 自动化测试规格:*

- **AC-1**: ShadowSampleCamera 独立于 Camera Stack
  - Given: 场景加载完毕，`ShadowRenderingModule` 初始化完成
  - When: 查询主 Camera 的 Camera Stack
  - Then: Stack 中不包含 ShadowSampleCamera；`shadowCamera.cameraType == CameraType.Game`；`shadowCamera.targetTexture == _shadowRT`
  - Edge cases: 场景热重载后 Camera Stack 仍不包含 ShadowSampleCamera

- **AC-2**: ShadowRT 内存规格
  - Given: `ShadowRenderingModule.Init()` 完成
  - When: 读取 `_shadowRT` 的属性
  - Then: `width == 512 && height == 512 && format == RenderTextureFormat.R8`；`_shadowRT.IsCreated() == true`
  - Edge cases: Init 调用两次时第二次调用不创建第二个 RT（幂等）

- **AC-4**: Shadow Cascade 参数按档位正确配置
  - Given: `QualityTier.High / Medium / Low` 分别设置
  - When: 读取 `UniversalRenderPipelineAsset.shadowCascadeCount`
  - Then: High=4, Medium=2, Low=1
  - Edge cases: 档位切换时无帧闪烁（下一帧才生效）

- **AC-6**: 资源释放
  - Given: `ShadowRenderingModule` 初始化后持有有效 ShadowRT
  - When: 调用 `OnModuleDestroy()`
  - Then: `_shadowRT` 引用为 null；Unity Memory Profiler 确认 RenderTexture 内存释放
  - Edge cases: 未初始化就 Destroy 时无 NullReferenceException

- **AC-7**: 初始化时间
  - Given: 场景从 Load 开始计时
  - When: `ShadowRenderingModule` 的 `OnModuleInit()` 返回
  - Then: 经过时间 ≤ 500ms（`Profiler.BeginSample / EndSample` 计时）
  - Edge cases: Low 档位（512 RT）比 High 档位（1024 RT）更快

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/urp-shadow-rendering/shadow_rt_setup_test.cs` — 必须存在且通过

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: 无（Foundation Layer 第一个 story，无上游依赖）
- Unlocks: Story 002（需要 ShadowRT + Camera 就绪后才能验证 WallReceiver shader 的渲染输出），Story 003（需要 ShadowRT 句柄）
