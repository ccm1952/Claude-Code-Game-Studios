// 该文件由Cursor 自动生成

# Story 003: ShadowRT AsyncGPUReadback 管线

> **Epic**: URP Shadow Rendering
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

---

> ⚠️ **BLOCKED pending SP-007 real-device verification of AsyncGPUReadback in HybridCLR context.**
>
> SP-007 源码分析已完成（`docs/architecture/findings/SP-007-hybridclr-asyncgpu.md`），但尚未完成真机 Android/iOS 验证。
>
> - 如果真机验证 **通过**：直接在 `GameLogic` 热更程序集中实现 `AsyncGPUReadback.Request` 回调，本 story 正常进行。
> - 如果真机验证 **失败**：必须实施 SP-007 回退方案——将 `AsyncGPUReadback` 调用封装在 `Default` 程序集中，通过 `IShadowRTReader` 接口向 `GameLogic` 暴露，额外工作量约 4h 重构。
>
> **在 SP-007 Editor Play Mode + 真机验证完成之前，本 story 不应进入实现阶段。**

---

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-010`, `TR-render-019`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-010: AsyncGPUReadback 管线将 ShadowRT 数据传递给 Shadow Puzzle System
- TR-render-019: ShadowRT CPU 读回处理时间 ≤ 1.5ms

**ADR Governing Implementation**: ADR-002: URP Rendering Pipeline；SP-007 Findings
**ADR Decision Summary**: 采用 `AsyncGPUReadback.Request(_shadowRT, 0, TextureFormat.R8, callback)` 非阻塞 GPU→CPU 读取，引入 1 帧延迟（可接受），回调中 `NativeArray<byte>` 仅在回调内有效须立即消费或拷贝。HybridCLR AOT 兼容性高风险，回退方案为 `IShadowRTReader` 接口。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: HIGH
**Engine Notes**: SP-007 确认 `AsyncGPUReadback` 回调的 `Action<AsyncGPUReadbackRequest>` 如果定义在 `GameLogic.dll`（热更），在 AOT 环境下可能触发 `ExecutionEngineException`。`request.GetData<byte>()` 泛型实例化需验证 `NativeArray<byte>` AOT 元数据已注册。**这是整个 epic 的最高技术风险项。**

**Control Manifest Rules (this layer)**:
- Required: AsyncGPUReadback 回调中 `NativeArray<byte>` 数据仅在回调内有效——立即消费或拷贝（ADR-002）
- Required: 回调失败（`request.hasError == true`）时使用上一帧缓存的 ShadowRT 数据（GDD Edge Case）
- Required: 所有 async 操作通过 UniTask（tech-prefs）；若使用 UniTask AsyncGPUReadback 扩展，需验证扩展在 HybridCLR 环境下兼容
- Required: ShadowRT CPU readback 用 `Profiler.BeginSample / EndSample` 计时，超过 1.5ms 时 `Debug.LogWarning`
- Required: 若 SP-007 失败，回退方案必须将 `AsyncGPUReadback` 调用封装在 Default 程序集中，通过 `IShadowRTReader` 接口向 GameLogic 暴露（SP-007）
- Forbidden: 不使用同步 GPU Readback（`Texture2D.ReadPixels`）——会造成 CPU-GPU 同步阻塞（ADR-002）
- Forbidden: 不在回调外持有 `NativeArray<byte>` 引用（ADR-002）
- Guardrail: ShadowRT CPU readback ≤ 1.5ms；允许 1-3 帧 AsyncGPUReadback 延迟（ADR-002）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md`，scoped to this story:*

- [ ] **AC-1**: `AsyncGPUReadback.Request` 在 `GameLogic.dll` 热更程序集中调用，回调正常触发（SP-007 验证通过后）；或在 SP-007 失败时通过 `IShadowRTReader` 接口在 Default 程序集实现
- [ ] **AC-2**: 每帧（或隔帧，由 Low 档控制）发起一次 readback 请求；上一帧请求未完成时不重复请求（防止 readback queue 堆积）
- [ ] **AC-3**: readback 回调中，`NativeArray<byte>` 数据立即传递给 Shadow Puzzle System 的消费接口，不持有跨帧引用
- [ ] **AC-4**: readback 失败（`hasError == true`）时，使用上一帧缓存的像素 buffer，Shadow Puzzle System 不感知中断
- [ ] **AC-5**: CPU 端 readback 处理时间（从回调开始到数据传递完成）≤ 1.5ms（`Profiler` marker 验证）
- [ ] **AC-6**: `AsyncGPUReadback` 引入的延迟 ≤ 3 帧（约 50ms @60fps），不影响匹配评分体感
- [ ] **AC-7**: 低档位（Low）下 readback 频率降为隔帧（每 2 帧 1 次），减少 GPU→CPU 总线压力

---

## Implementation Notes

*Derived from ADR-002, SP-007, control-manifest.md §5.1 & §7:*

**正常路径（SP-007 验证通过）：**
```csharp
// 在 GameLogic.dll 中，ShadowRenderingModule.Update()
private bool _readbackPending = false;
private NativeArray<byte> _lastFrameBuffer; // 上一帧缓存

void Update()
{
    if (!_readbackPending)
    {
        _readbackPending = true;
        AsyncGPUReadback.Request(_shadowRT, 0, TextureFormat.R8, OnReadbackComplete);
    }
}

void OnReadbackComplete(AsyncGPUReadbackRequest request)
{
    _readbackPending = false;
    if (request.hasError)
    {
        // 使用上一帧缓存
        GameEvent.Send(EventId.Evt_ShadowRTUpdated, _lastFrameBuffer);
        return;
    }
    var data = request.GetData<byte>();
    // 立即消费——NativeArray 在此回调后失效
    _lastFrameBuffer = new NativeArray<byte>(data, Allocator.Persistent); // 拷贝保留
    GameEvent.Send(EventId.Evt_ShadowRTUpdated, data);
}
```

**回退路径（SP-007 失败，ADR-004 HybridCLR 边界规则）：**
```csharp
// Default 程序集（不热更）
public class ShadowRTReaderImpl : IShadowRTReader
{
    public async UniTask<NativeArray<byte>> RequestReadback(RenderTexture rt)
    {
        var request = await AsyncGPUReadback.RequestAsync(rt, 0, TextureFormat.R8);
        return request.GetData<byte>();
    }
}

// GameLogic.dll 通过 DI 注入 IShadowRTReader
// ShadowRenderingModule 通过接口调用，不直接引用 Default 程序集实现
```

**关键约束（来自 ADR-002）：**
- readback 引入 1 帧延迟：GPU 帧 N 的数据在帧 N+1 的回调中可用
- 回调在主线程 `Update()` 之后执行，Shadow Puzzle System 在下一帧消费数据
- Low 档下 `_skipFrameCount = 1`（隔帧 readback），Medium/High 档 `_skipFrameCount = 0`

**性能计时：**
```csharp
void OnReadbackComplete(AsyncGPUReadbackRequest request)
{
    Profiler.BeginSample("ShadowRT.Readback.CPU");
    // ... 处理逻辑
    Profiler.EndSample();
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 001**: ShadowRT 的创建（本 story 假设 RT 已存在并可读）
- **Story 004**: Low 档隔帧 readback 的频率配置来自质量档位系统（本 story 读取档位，不决定档位）
- **Shadow Puzzle System**: `Evt_ShadowRTUpdated` 的消费端实现（本 story 只负责发送事件）

---

## QA Test Cases

*Logic story — 自动化测试规格:*

- **AC-2**: 不重复请求
  - Given: 上一帧的 readback 请求尚未完成（`_readbackPending == true`）
  - When: `Update()` 再次调用
  - Then: 不会发起第二次 `AsyncGPUReadback.Request`；Queue 中只有 1 个 pending 请求
  - Edge cases: 连续 10 帧 readback 未完成时，系统稳定不崩溃

- **AC-3**: NativeArray 不跨帧持有
  - Given: 测试中 mock `AsyncGPUReadback` 使其立即完成
  - When: 回调执行完毕
  - Then: 除 `_lastFrameBuffer`（已拷贝）外，不存在对原始 `NativeArray<byte>` 的持久引用
  - Edge cases: Shadow Puzzle System 消费接口在回调后访问 data 时应访问拷贝而非原始 NativeArray

- **AC-4**: 失败降级
  - Given: 模拟 `request.hasError = true`
  - When: `OnReadbackComplete` 被调用
  - Then: 使用 `_lastFrameBuffer` 发送 `Evt_ShadowRTUpdated`；Shadow Puzzle System 正常接收数据；无 NullReferenceException
  - Edge cases: 第一帧 readback 就失败时（`_lastFrameBuffer` 未初始化），不崩溃，发送空 buffer 或跳过

- **AC-5**: CPU 处理时间
  - Given: Medium 档，`_shadowRT` 为 512×512 R8 格式
  - When: `OnReadbackComplete` 执行完整处理流程
  - Then: `Profiler.BeginSample("ShadowRT.Readback.CPU")` 到 `EndSample` 的时间 ≤ 1.5ms
  - Edge cases: 连续 100 帧的 P95 时间 ≤ 1.5ms（非最大值）

- **AC-7**: Low 档隔帧读取
  - Given: 质量档位切换到 Low，`_skipFrameCount = 1`
  - When: 连续 10 帧运行
  - Then: `AsyncGPUReadback.Request` 被调用 5 次（隔帧）；偶数帧使用上一次 readback 的缓存数据
  - Edge cases: 档位从 Low → Medium 时立即恢复每帧 readback

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/urp-shadow-rendering/shadow_rt_readback_test.cs` — 必须存在且通过
- 若使用回退路径（IShadowRTReader）：`tests/integration/urp-shadow-rendering/shadow_rt_reader_interface_test.cs`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（需要 ShadowRT 句柄就绪）；**SP-007 真机验证结果**（阻塞条件）
- Unlocks: Shadow Puzzle System（匹配评分算法依赖本 story 输出的 `Evt_ShadowRTUpdated` 事件）
