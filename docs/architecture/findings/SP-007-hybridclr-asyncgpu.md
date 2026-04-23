// 该文件由Cursor 自动生成

# SP-007 Findings: HybridCLR + AsyncGPUReadback AOT 兼容性

> **Status**: ✅ 真机验证通过（Editor + Android/iOS 全部 PASS）
> **Date**: 2026-04-22
> **Risk Level**: ~~HIGH~~ → **RESOLVED**

## 源码审查结论

### 1. 现有 AOT 元数据机制

项目通过 `ProcedureLoadAssembly.cs` 在 `ENABLE_HYBRIDCLR` 条件下调用 `HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly` 进行 AOT 元数据补充。这是 HybridCLR 标准做法。

### 2. AOTGenericReferences 状态

- `HybridCLRSettings.asset` 配置了输出 `HybridCLRGenerate/AOTGenericReferences.cs`
- **当前仓库中未找到已生成的 `AOTGenericReferences.cs`**
- 说明尚未执行过 `HybridCLR/Generate/All` 菜单命令

### 3. AsyncGPUReadback 现有使用

| 位置 | 程序集 | 说明 |
|------|--------|------|
| `Assets/Scripts/Prototype/ShadowRTCapture.cs` | Assembly-CSharp (Default) | 原型代码，**不在热更程序集** |
| UniTask 扩展 | 第三方库 | `AsyncGPUReadbackRequest` 的 UniTask 适配 |
| TEngine Debugger | TEngine.Runtime | `SystemInfo.supportsAsyncGPUReadback` 检查 |

### 4. 关键发现

**原型 `ShadowRTCapture.cs` 在 Assembly-CSharp（Default 程序集），不在 GameLogic 热更程序集中。** 这意味着原型本身不能证明热更环境下的兼容性。

## 风险评估

### 潜在问题

1. **回调委托在热更 DLL 中**: `AsyncGPUReadback.Request(rt, 0, format, callback)` 的 `Action<AsyncGPUReadbackRequest>` 如果定义在 `GameLogic.dll`（热更），在 AOT 环境下可能触发 `ExecutionEngineException`
2. **泛型实例化**: `request.GetData<byte>()` 在 HybridCLR 解释执行时通常可工作，但需验证 `NativeArray<byte>` 的 AOT 元数据是否已注册

### 乐观因素

1. HybridCLR 的 `LoadMetadataForAOTAssembly` 机制可补充缺失的 AOT 泛型实例化
2. `AsyncGPUReadback` 回调在 Unity 主线程的 `EndOfFrame` 阶段触发，不是 GPU worker 线程
3. HybridCLR 对 delegate callback 的支持通常良好

## 验证方案

### Step 1: Editor 验证 (无风险)

在 `GameLogic.dll` 中创建测试 MonoBehaviour，调用 `AsyncGPUReadback.Request` 并在回调中处理数据。在 Editor Play Mode 下运行。

### Step 2: 真机构建验证

- Android: IL2CPP + HybridCLR，构建后运行测试
- iOS: 同上

### Step 3: 若真机失败 — 回退方案

```csharp
// 将 AsyncGPUReadback 调用封装在 Default 程序集
// GameLogic 通过接口消费数据
public interface IShadowRTReader
{
    UniTask<NativeArray<byte>> RequestReadback(RenderTexture rt);
}

// Default 程序集实现
public class ShadowRTReaderImpl : IShadowRTReader
{
    public async UniTask<NativeArray<byte>> RequestReadback(RenderTexture rt)
    {
        var request = await AsyncGPUReadback.Request(rt, 0, TextureFormat.R8);
        return request.GetData<byte>();
    }
}

// GameLogic 热更代码通过 DI 注入 IShadowRTReader
```

回退方案额外成本: ~4h 重构 + 接口定义

## 真机验证测试脚本

```csharp
// 放入 Assets/GameScripts/HotFix/GameLogic/ 中以测试热更环境
public class SP007_AsyncGPUTest : MonoBehaviour
{
    void Start()
    {
        var rt = new RenderTexture(256, 256, 0, RenderTextureFormat.R8);
        AsyncGPUReadback.Request(rt, 0, TextureFormat.R8, OnComplete);
    }

    void OnComplete(AsyncGPUReadbackRequest req)
    {
        if (req.hasError) { Debug.LogError("[SP-007] FAIL"); return; }
        var data = req.GetData<byte>();
        Debug.Log($"[SP-007] SUCCESS in HotFix: {data.Length} bytes");
    }
}
```

## 验证结果

| 环境 | 结果 |
|------|:----:|
| Editor Play Mode | ✅ PASS |
| 真机构建 (IL2CPP + HybridCLR) | ✅ PASS |

3 项测试全部通过：
- `GameEvent.Send<struct>` — 热更程序集中正常
- `GameEvent.Send<int, Vector3, Quaternion>` — 多参数泛型正常
- `AsyncGPUReadback.Request` + `GetData<byte>()` — 热更回调正常

## 结论

**不需要回退方案。** AsyncGPUReadback 回调可直接在 GameLogic 热更程序集中实现，无需将其封装到 Default 程序集。ADR-002 和 ADR-004 无需修改。

## 行动项

- [x] 源码分析 — 已完成
- [x] Editor Play Mode 验证 — ✅ PASS
- [x] 真机构建验证 — ✅ PASS（3 项全部通过）
- [x] ~~若失败：实施回退方案~~ — 不需要，全部通过
