// 该文件由Cursor 自动生成

# Story 003 — Shadow RT Sampling

> **Epic**: shadow-puzzle
> **Type**: Integration
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — Shadow Match Score (visibilityScore) |
| **TR-IDs** | TR-puzzle-001, TR-puzzle-010 |
| **ADR** | ADR-012 (AsyncGPUReadback pipeline), ADR-002 (URP Shadow Rendering) |
| **Finding** | SP-007 — HybridCLR + AsyncGPUReadback 高风险：若 HybridCLR AOT 不支持回调，需将回调移至 Default 程序集的 `IShadowRTReader` 接口后面 |
| **Engine** | Unity 2022.3.62f2 LTS / AsyncGPUReadback / NativeArray |
| **Assembly** | `GameLogic` (接口) + 可能需要 `Default` (回调实现) — 视 SP-007 结论 |

### Control Manifest Rules

- **CM-3.1**: AsyncGPUReadback 使用双缓冲 `NativeArray<byte>`；回调在 render thread，计算在 main thread 读取 front buffer
- **CM-3.1**: readback 失败/过时时复用上一帧缓冲区（graceful degradation）
- **CM-7 (SP-007)**: AsyncGPUReadback 回调在 hot-fix DLL 中为 HIGH 风险；若真机失败，回调移至 Default 程序集，通过 `IShadowRTReader` 接口暴露数据
- **CM-5.1**: ShadowRT 为 R8 格式（8-bit 灰度），512×512，来自 URP Shadow Rendering
- **CM-5.1**: `NativeArray<byte>` 数据仅在回调中有效，必须立即消费或复制

---

## Acceptance Criteria

1. **AC-001**: `AsyncGPUReadback.Request(shadowRT)` 每帧（或低端设备每隔一帧）发起一次请求
2. **AC-002**: 回调中将 `NativeArray<byte>` 复制到双缓冲 back buffer；主线程读取 front buffer
3. **AC-003**: readback 失败或超时（> 3 帧）时，主线程继续使用上一帧 front buffer，不产生异常
4. **AC-004**: `IShadowRTReader.GetLatestBuffer()` 返回最新可用的像素缓冲区（R8 byte array，512×512 = 262144 bytes）
5. **AC-005**: 在 iPhone 13 Mini 上，readback + buffer 处理总耗时 ≤ 1.5ms（TR-render-019）
6. **AC-006**: 低端 Android（Mali-G52）验证：若 AsyncGPUReadback 不稳定，`IShadowRTReader` 可替换为 CPU readback fallback（`RenderTexture.ReadPixels`），不改变消费方 API
7. **AC-007**: SP-007 风险缓解：在真机构建（HybridCLR IL2CPP）上验证回调执行路径；若 GameLogic 回调失败，实现移至 Default 程序集

---

## Implementation Notes

### 接口设计（SP-007 隔离）

```csharp
// 在 GameLogic 程序集中定义接口
public interface IShadowRTReader
{
    // 返回最新可用的 ShadowRT 像素数据（只读）
    // 返回 null 表示数据未就绪（消费方应使用上一帧数据）
    NativeArray<byte>? GetLatestBuffer();
    bool IsDataFresh { get; }  // 当前帧数据是否已更新
}

// 在 Default 或 GameLogic 程序集中实现（视 SP-007 结论）
public class AsyncGPUShadowRTReader : MonoBehaviour, IShadowRTReader
{
    private NativeArray<byte> _frontBuffer;
    private NativeArray<byte> _backBuffer;
    private bool _backBufferReady;
    // AsyncGPUReadback.Request() 每帧发起
    // 回调中 swap buffers
}
```

### 双缓冲策略

1. `_backBuffer` 在 callback 中填写（render thread）
2. 每帧开始（主线程）：若 `_backBufferReady`，swap front/back
3. `ShadowMatchCalculator` 调用 `GetLatestBuffer()` 读取 front buffer
4. 无 GC：所有 `NativeArray` 预分配，不在回调中 `new`

### SP-007 风险处理流程

```
Sprint 0 验证步骤：
1. 在 HybridCLR IL2CPP 构建上运行，测试 callback 是否执行
2. 若 GameLogic 中的 callback 正常 → 保持当前设计
3. 若 callback 失败（AOT 限制）→ 将 AsyncGPUShadowRTReader 移至 Default 程序集，
   通过 IShadowRTReader 接口桥接到 GameLogic
```

---

## Out of Scope

- ShadowRT 的渲染（属于 URP Shadow Rendering foundation layer）
- matchScore 计算（story-002）
- Shadow RT 分辨率配置（ADR-002 coverage）

---

## QA Test Cases

### TC-001: 正常 readback 流程

**Given**: ShadowRT 已渲染，`AsyncGPUShadowRTReader` 初始化完成  
**When**: 等待 3 帧（readback 延迟窗口）  
**Then**: `GetLatestBuffer()` 返回非 null，`IsDataFresh = true`，buffer length = 262144（512×512）

### TC-002: Readback 失败降级

**Given**: 模拟 `AsyncGPUReadback` 请求失败（返回 `hasError=true`）  
**When**: 调用 `GetLatestBuffer()`  
**Then**: 返回上一帧的有效 buffer（不返回 null），`IsDataFresh = false`

### TC-003: 双缓冲无竞争

**Given**: 主线程正在读取 front buffer  
**When**: 同时 callback 完成写入 back buffer  
**Then**: 读取过程中数据不被修改（主线程读 front，callback 写 back，互不干扰）

### TC-004: 性能预算（集成）

**Setup**: 在 PlayMode 中运行真实的 AsyncGPUReadback + `GetLatestBuffer()` 调用  
**Verify**: 在 Unity Profiler 中测量，readback 处理（非 GPU 渲染时间）≤ 1.5ms/帧  
**Pass**: 连续 300 帧（5s）均满足预算，无异常峰值

### TC-005: SP-007 真机验证

**Setup**: HybridCLR IL2CPP 构建，部署到 Android 真机  
**Verify**: 添加 `Debug.Log` 确认 AsyncGPUReadback callback 在 GameLogic 程序集中正常触发  
**Pass**: 回调触发且 buffer 数据非零；若失败，记录并切换到 Default 程序集实现

---

## Test Evidence

- **Integration Test**: `tests/integration/ShadowRT_Readback_Integration_Test.cs`
- **Evidence Doc**: `production/qa/evidence/story-003-shadow-rt-sampling.md`（真机验证截图 + Profiler 数据）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| ADR-002 (URP Shadow Rendering) | Foundation | 提供 ShadowRT 纹理引用 `IShadowRenderingService.GetShadowRT()` |
| story-002 (MatchScore Calculation) | Consumer | 消费 `IShadowRTReader.GetLatestBuffer()` |
| SP-007 验证结论 | Decision Gate | 决定回调实现放在哪个 assembly |
| Default Assembly (可能) | Code | 若 SP-007 回调需移至 Default |
