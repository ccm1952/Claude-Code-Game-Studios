<!-- 该文件由Cursor 自动生成 -->

# URP Shadow Rendering — URP 影子渲染系统

> **Status**: Draft
> **Author**: Technical Art / Rendering Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 关系即谜题（为影子谜题提供视觉基础）

## Summary

URP 影子渲染系统是《影子回忆》的 Foundation Layer 基础设施，负责在 URP 管线下实现高可读性的实时影子投影。它为 Shadow Puzzle System 提供视觉表现——所有谜题的核心体验"在墙面上拼出关系影子"依赖本系统输出清晰、稳定、性能可控的实时阴影。本系统的最高设计约束是：影子轮廓的可读性优先于一切视觉效果。

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None`

## Overview

玩家在温暖的室内场景中摆放物件、调整光源，墙面和地面上的影子随之实时变化。本系统负责：(1) 配置 URP 渲染管线使影子满足美术可读性标准；(2) 管理 Shadow Map 的分辨率和质量分级以适配移动端性能预算；(3) 提供影子特效接口（NearMatch 发光、PerfectMatch 定格、章节间影子风格变化）供 Shadow Puzzle System 调用；(4) 将投影墙面区域渲染到 Render Texture 供匹配判定算法采样。本系统不处理谜题逻辑，只保证"墙上的影子"好看、清晰、性能达标。

## Player Fantasy

玩家不会意识到渲染系统的存在——他们只会感觉到："这个影子好真实、好清晰，我能一边拖物件一边盯着墙面看它变化。"当物件被放对位置时，影子轮廓锐化并发出柔和暖光的那一刻，应该产生"影子活过来了"的感觉。渲染系统服务的情感是**信任感**：玩家信任屏幕上的影子是光与物件关系的真实表达，从而沉浸在拼出记忆的过程中。

## Detailed Design

### Core Rules

#### 1. 渲染管线配置（URP Pipeline Asset）

1. 管线基础选择 Universal Render Pipeline (URP)，使用项目专用的 `ShadowGameURPAsset`
2. 渲染路径使用 Forward Rendering（移动端最优路径）
3. 启用 SRP Batcher，所有材质必须 SRP Batcher compatible
4. HDR 关闭（移动端，节省带宽），PC 可选开启
5. MSAA 关闭，通过 Post-Processing SMAA 实现抗锯齿（更可控的性能消耗）
6. 渲染精度（Render Scale）：移动端 1.0（不做降采样，影子边缘质量优先）
7. Depth Texture 和 Opaque Texture 按需启用（Shadow RT 采样需要 Depth）

#### 2. Shadow Map 配置

1. **主光源（Directional Light）Shadow Map**：
   - 分辨率：PC 2048×2048 / 移动端 1024×1024
   - Shadow Cascade：PC 4 级 / 移动端 2 级
   - Shadow Distance：8m（场景最大深度 ~5m，预留缓冲）
   - Depth Bias：0.5（防止 shadow acne，同时保持影子贴合物件底部）
   - Normal Bias：0.3（防止 peter-panning，即影子离物件过远）
   - Soft Shadows：移动端关闭（硬阴影更清晰、更省性能），PC 可选开启
2. **Additional Lights（Point Light / Spot Light）Shadow Map**：
   - 分辨率：PC 1024×1024 / 移动端 512×512
   - 每帧最多 2 个 Additional Light 投射阴影（第 3 个光源标记为非阴影光）
   - Shadow 更新策略：主操作光源每帧更新；非操作光源可降频至隔帧更新
3. **Shadow Atlas**：
   - 总 Atlas 大小：PC 4096×4096 / 移动端 2048×2048
   - 单灯最大占用 Atlas 面积：50%（防止单灯独占整个 Atlas）

#### 3. 影子质量分级（Quality Tiers）

系统提供三个质量档位，玩家可在设置中切换，启动时根据设备自动选择默认档位：

| 参数 | Low（低端手机） | Medium（主流手机） | High（高端手机/PC） |
|------|-----------------|-------------------|-------------------|
| Main Shadow Map | 512×512 | 1024×1024 | 2048×2048 |
| Additional Light Shadow | 256×256 | 512×512 | 1024×1024 |
| Shadow Atlas | 1024×1024 | 2048×2048 | 4096×4096 |
| Shadow Cascade | 1 级 | 2 级 | 4 级 |
| Soft Shadows | Off | Off | On (PCF) |
| Shadow Update | 主光每帧 / 辅光隔帧 | 全部每帧 | 全部每帧 |
| 投影墙面 RT | 512×512 | 1024×1024 | 1024×1024 |
| Max Shadow-Casting Lights | 2 | 2 | 3 |

**自动档位选择规则**：
- GPU 型号位于预设的低端列表（Mali-G52 以下、Adreno 610 以下）→ Low
- iPhone 13 Mini 及以上 / GPU benchmark ≥ 中位数 → Medium
- iPhone 15 Pro 及以上 / GPU benchmark ≥ 高位数 / 任意 PC → High

#### 4. 投影接收面（Shadow Receiver）专用材质

1. 投影墙面使用专用 Shader：`ShadowGame/WallReceiver`
2. 该 Shader 功能：
   - 接收 URP 实时阴影（Main Light + Additional Light Shadows）
   - 自身 Albedo 使用低频纹理（纯色/微噪点），不使用高频细节
   - 可配置的 Shadow Contrast 参数：控制影子区域与非影子区域的明暗比
   - 可配置的 Shadow Color Tint：影子颜色不是纯黑，带有章节对应的冷暖倾向
   - 可配置的 Shadow Edge Softness：控制影子边缘柔化程度（不依赖 Shadow Map 的 PCF）
3. 影子与墙面的明暗比强制 ≥ 3:1，推荐 5:1
4. 墙面材质 Specular/Smoothness 设为 0（不产生任何高光，避免干扰影子可读性）

#### 5. Shadow Render Texture（影子 RT 采样）

1. 配置一个额外的 Camera（`ShadowSampleCamera`）正交投影到投影墙面
2. ShadowSampleCamera 输出到一个 Render Texture（`ShadowRT`）
3. ShadowRT 分辨率：Medium 档 1024×1024，可在质量分级表中调节
4. ShadowRT 格式：R8 单通道灰度（只需要明暗信息）
5. Shadow Puzzle System 通过读取 ShadowRT 的像素数据来计算影子匹配度
6. ShadowRT 更新频率与影子更新频率一致：最高每帧一次，最低隔帧一次
7. ShadowRT 采样使用 AsyncGPUReadback，避免 CPU-GPU 同步阻塞

#### 6. 影子特效接口

本系统向 Shadow Puzzle System 暴露以下可调用接口：

| 接口 | 功能 | 触发时机 |
|------|------|---------|
| `SetShadowGlow(float intensity, Color color)` | 在投影墙面上为影子轮廓添加外发光 | NearMatch 进入时 |
| `ClearShadowGlow()` | 移除影子发光效果 | NearMatch 退出时 |
| `FreezeShadow(float duration)` | 锁定当前帧的影子 RT，不再更新 | PerfectMatch 定格时 |
| `UnfreezeShadow()` | 恢复影子 RT 实时更新 | 定格演出结束时 |
| `SetShadowStyle(ShadowStylePreset preset)` | 切换影子风格（边缘锐度、颜色、Penumbra） | 章节切换时 |
| `SetShadowContrast(float ratio)` | 调整影子与墙面的明暗对比度 | 章节切换/色温变化时 |
| `GetShadowRT()` | 返回当前帧的 Shadow Render Texture | 匹配度计算时 |
| `SetShadowPenumbra(float width)` | 设定影子边缘半影宽度 | 章节风格化需求 |

#### 7. 章节影子风格预设

| 章节 | Penumbra (px) | Shadow Color Tint | Contrast Ratio | Edge Sharpness | 描述 |
|------|---------------|-------------------|----------------|---------------|------|
| Ch.1 靠近 | 0-1 | `#2C2C2E`（深影灰，中性） | 5:1 | 1.0（最锐利） | 清晰硬朗的陌生人影子 |
| Ch.2 共同空间 | 0-1 | `#322C28`（微暖深灰） | 5:1 | 0.95 | 依然清晰，微暖 |
| Ch.3 共同生活 | 1-2 | `#352A22`（暖深棕灰） | 4:1 | 0.85 | 温暖柔和，叠影区域自然融合 |
| Ch.4 松动 | 2-4 | `#2C2E33`（冷深灰） | 4:1 | 0.7 | 边缘开始毛刺/虚化 |
| Ch.5 缺席 | 4-8 | `#2E3035`→`#2F2D2A` | 3:1→4:1 | 0.5→0.8 | 先模糊虚幻，后释然回清 |

#### 8. 光源阴影投射规则

1. 场景中所有可交互光源（台灯/蜡烛/手电）默认启用阴影投射
2. 环境补光（Ambient Light）不投射阴影
3. 同一时刻最多 2 个光源投射实时阴影（性能红线），超出的光源只提供照明
4. 投射阴影的优先级排序：玩家当前操作的光源 > 上次操作的光源 > 场景默认主光
5. 光源移动时阴影必须在同帧或下一帧更新（最大延迟 1 帧 = 16.7ms@60fps）
6. 非关键光源（纯氛围光）可标记为 `ShadowCasterPriority.Ambient`，仅在性能余量时投射阴影

#### 9. Shadow Caster 配置

1. 所有可交互物件的 Mesh Renderer 启用 `Cast Shadows = On`
2. 不可交互装饰物件设为 `Cast Shadows = Shadows Only`（只投影不渲染几何，节省 draw calls）或 `Cast Shadows = Off`
3. 投影墙面设为 `Receive Shadows = On, Cast Shadows = Off`
4. 背景层（z > 3m）所有物件 `Cast Shadows = Off`
5. "看不见本体只看到影子"的特殊谜题物件使用 `Shadow Only` 渲染模式：Mesh Renderer 所在 Layer 在主 Camera 中 Culling Mask 排除，但在 Shadow Map 渲染中保留

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Initializing** | 场景加载开始 | URP Asset 配置完毕 + ShadowRT 创建完毕 | 根据设备档位加载对应 URP Pipeline Asset，创建 ShadowSampleCamera 和 ShadowRT |
| **Active** | 初始化完毕 | 场景卸载或进入 Frozen | 每帧渲染影子，更新 ShadowRT，响应光源/物件位置变化 |
| **Frozen** | PerfectMatch 调用 `FreezeShadow()` | `UnfreezeShadow()` 被调用或超时（安全阈值 10s） | 停止 ShadowRT 更新，影子画面定格，Shadow Map 仍更新但不写入 RT |
| **TransitionStyle** | 章节切换调用 `SetShadowStyle()` | 风格参数插值完成 | 在 0.5-1.0s 内平滑过渡影子风格参数（Penumbra/Color/Contrast） |
| **Degraded** | 帧时间超过性能阈值 (>20ms) 连续 5 帧 | 帧时间恢复 (<16ms) 连续 10 帧 | 自动降低 Shadow Map 分辨率一档，减少 Shadow-casting light 数量 |
| **Disposed** | 场景卸载 | — | 释放 ShadowRT 及相关资源，销毁 ShadowSampleCamera |

### Interactions with Other Systems

**与 Shadow Puzzle System 的交互：**
- 输出：`ShadowRT`（灰度 Render Texture），供匹配度计算采样
- 输出：影子特效接口（Glow / Freeze / Style）
- 输入：当前谜题的光源配置（哪些光投射阴影、优先级）
- 输入：NearMatch / PerfectMatch 状态变化事件，触发对应视觉效果
- 权责边界：本系统只负责"让影子正确渲染出来"；匹配度计算、谜题逻辑完全由 Shadow Puzzle System 负责

**与 Chapter State System 的交互：**
- 输入：当前章节 ID，用于选择影子风格预设
- 本系统在章节切换时通过 `SetShadowStyle(preset)` 平滑过渡影子风格

**与 Art Bible / Post-Processing 的关系：**
- 投影墙面 Shader 参数须与 Art Bible 中定义的章节色温保持一致
- Post-Processing Volume 中的 Bloom / Vignette 不应影响 ShadowRT 采样结果（ShadowSampleCamera 不应用 Post-Processing）

**与 Settings / Accessibility 的交互：**
- 接收质量档位切换请求，热切换 URP Asset 参数
- 接收"高对比度影子"辅助功能开关，开启后 Shadow Color 改为纯黑 `#000000`，Contrast 提升至 8:1

## Formulas

### Draw Call 预算分配

```
totalDrawCalls = sceneGeometry + shadowPasses + postProcessing + UI

sceneGeometry   = staticBatched + dynamicObjects + transparents
shadowPasses    = numShadowCastingLights * shadowCasterCount * cascadeCount
postProcessing  = ppEffectCount
UI              = uiDrawCalls
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| totalDrawCalls | int | 0-150 | runtime | 每帧总 draw calls，移动端硬上限 150 |
| sceneGeometry | int | 30-60 | runtime | 场景几何渲染（SRP Batcher 合批后） |
| numShadowCastingLights | int | 1-2 | config | 投射阴影的光源数量 |
| shadowCasterCount | int | 5-15 | runtime | 投射阴影的物件数量 |
| cascadeCount | int | 1-4 | quality tier | Shadow Cascade 级数 |
| ppEffectCount | int | 3-6 | config | Post-Processing 效果数量 |
| uiDrawCalls | int | 5-15 | runtime | UI 渲染批次 |

**目标预算分配（Medium 档 / 移动端）：**

```
sceneGeometry  ≤ 50  (SRP Batcher + Static Batching)
shadowPasses   ≤ 60  (2 lights × 15 casters × 2 cascades)
postProcessing ≤ 6
UI             ≤ 15
────────────────────
total          ≤ 131 (buffer to 150)
```

**Edge case**: 当 `totalDrawCalls > 130` 时，进入 Degraded 状态前先尝试：
1. 禁用优先级最低的阴影光源
2. 将装饰物件的 `Cast Shadows` 设为 Off
3. 如仍超标，降低 Shadow Map 分辨率一档

### Shadow Map 内存消耗

```
shadowMemory = atlasWidth * atlasHeight * bytesPerPixel
             + numAdditionalLights * additionalShadowSize^2 * bytesPerPixel
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| atlasWidth/Height | int | 1024-4096 | quality tier | Shadow Atlas 尺寸 |
| bytesPerPixel | float | 2-4 bytes | format | 16-bit depth = 2B, 32-bit = 4B |
| numAdditionalLights | int | 0-3 | runtime | 额外光源数量 |
| additionalShadowSize | int | 256-1024 | quality tier | 每个额外光源的 Shadow Map 尺寸 |

**内存预算示例（Medium 档）：**

```
mainShadow  = 2048 × 2048 × 2B = 8 MB
additional  = 2 × (512 × 512 × 2B) = 1 MB
shadowRT    = 1024 × 1024 × 1B = 1 MB
────────────────────
total       = 10 MB (占 1.5GB 总预算的 0.67%)
```

### Shadow RT 采样时间预算

```
shadowSampleTime = readbackLatency + cpuProcessingTime

readbackLatency    = asyncGPUReadbackFrames * frameTime
cpuProcessingTime  = rtPixelCount * costPerPixel
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| asyncGPUReadbackFrames | int | 1-3 | runtime | AsyncGPUReadback 延迟帧数 |
| frameTime | float | 16.7ms | 60fps | 单帧时间 |
| rtPixelCount | int | 262K-1M | quality tier | ShadowRT 总像素数 |
| costPerPixel | float | ~0.002μs | measured | CPU 端单像素处理耗时 |

**目标**：`cpuProcessingTime ≤ 1.5ms`，为 Shadow Puzzle System 的匹配度计算留出 0.5ms，总计 ≤ 2ms。

### 影子对比度公式

```
finalShadowColor = lerp(wallBaseColor, shadowTintColor, shadowIntensity * contrastMultiplier)

contrastRatio = luminance(wallBaseColor) / luminance(finalShadowColor)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| wallBaseColor | Color | — | wall material | 墙面基础颜色（柔象牙 #F5F0E8 为典型值） |
| shadowTintColor | Color | — | chapter preset | 章节影子色彩倾向 |
| shadowIntensity | float | 0.0-1.0 | URP shadow | URP 阴影强度采样值 |
| contrastMultiplier | float | 1.0-3.0 | wall shader | 对比度增强因子 |

**约束**：`contrastRatio` 必须 ≥ 3:1（低端档），推荐 ≥ 5:1。

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 物件贴近墙面导致影子极小 | 保持影子最小可见尺寸（≥10px @1080p），通过 Shader 补偿 | 防止影子消失导致玩家困惑 |
| 光源被物件完全遮挡 | 该光源产生的影子全部消失，不影响其他光源的影子 | 物理正确行为，也是潜在谜题机制 |
| 两个光源产生的影子重叠 | 重叠区域影子加深（更暗），但不超过纯黑 | 多光源场景的自然物理行为 |
| Shadow Map 分辨率不足导致影子边缘锯齿 | 自动启用投影墙面 Shader 的边缘柔化（2px gaussian）补偿 | 低端设备仍保证可读性 |
| 玩家快速移动物件导致影子撕裂 | 启用 1 帧延迟的影子运动模糊（极轻微），或直接接受 1 帧延迟 | 性能优先，允许 1 帧延迟 |
| 设备切换到后台再返回 | 重建 ShadowRT，从当前 Shadow Map 状态恢复 | 防止 Render Texture 丢失 |
| 章节过渡期间影子风格正在插值 | 如果此时触发 PerfectMatch 定格，以当前插值中间值定格 | 避免在过渡态时出现视觉跳变 |
| 物件处于两个 Shadow Cascade 边界 | 使用 Cascade Blending（URP 内置），混合两级 Cascade 避免硬接缝 | Shadow Cascade 接缝会严重影响影子可读性 |
| "Shadow Only"物件在 ShadowRT 中不可见 | ShadowSampleCamera 的 Culling Mask 包含 Shadow Only Layer | 确保"只有影子"的谜题物件在 ShadowRT 中正确采样 |
| GPU Readback 失败或超时 | 使用上一帧缓存的 ShadowRT 数据继续匹配判定 | AsyncGPUReadback 在某些低端设备上不稳定 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Shadow Puzzle System | Puzzle depends on this | 消费 ShadowRT 做匹配判定；调用影子特效接口 |
| Chapter State System | This reads from Chapter | 读取章节 ID 切换影子风格预设 |
| Settings / Accessibility | This reads from Settings | 读取质量档位和辅助功能开关 |
| URP Pipeline (Unity) | This depends on URP | 全部阴影渲染依赖 URP 管线 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| mainShadowResolution | 1024 | 512-2048 | 影子边缘更锐利，GPU 负担加重 | 影子边缘锯齿，GPU 负担减轻 |
| additionalShadowResolution | 512 | 256-1024 | Point/Spot 光影子更清晰 | Point/Spot 光影子模糊 |
| shadowDistance | 8m | 5-15m | 覆盖更大场景，Shadow Map 密度降低 | 覆盖更小范围，近景影子更精细 |
| depthBias | 0.5 | 0.1-2.0 | 减少 shadow acne，但影子缩小 | 更贴合物件但可能出现 acne |
| normalBias | 0.3 | 0.1-1.0 | 减少 peter-panning 伪影 | 影子更紧贴但可能穿帮 |
| wallContrastMultiplier | 2.0 | 1.0-3.0 | 影子更黑更醒目 | 影子更淡更柔和 |
| shadowEdgeSoftness | 1.0px | 0-4px | 影子边缘更柔和 | 影子边缘更锐利 |
| shadowRTResolution | 1024 | 512-2048 | 匹配判定精度更高 | 匹配判定精度降低，GPU/内存更省 |
| maxShadowCastingLights | 2 | 1-3 | 更多光源产生影子，性能压力大 | 影子效果简化，性能充裕 |
| asyncReadbackFrequency | 每帧 | 每帧/隔帧/每3帧 | 匹配判定更实时 | 匹配延迟加大但性能更好 |
| degradedThresholdMs | 20ms | 16-33ms | 更晚触发降级，但可能持续掉帧 | 更早降级，帧率更稳定 |
| shadowGlowIntensity | 0.4 | 0.1-0.8 | NearMatch 发光更醒目 | NearMatch 发光更微妙 |
| shadowFreezeTransition | 0.15s | 0.05-0.3s | 定格过渡更柔和 | 定格更干脆 |
| cascadeBlendDistance | 0.3 | 0.1-0.5 | Cascade 过渡更平滑但开销略增 | 过渡区域可能出现硬接缝 |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| 影子实时跟随物件移动 | 影子位置/大小/方向逐帧变化 | 无 | MVP |
| NearMatch 影子发光 | 影子轮廓外缘出现 2-4px 暖色辉光（使用墙面 Shader 的 Emission 通道） | 无（由 Shadow Puzzle System 控制音效） | MVP |
| PerfectMatch 影子定格 | 影子画面冻结 + 轮廓锐化至最大清晰度 + 辉光从暖色变为白色再收敛 | 无（由 Shadow Puzzle System 控制） | MVP |
| 章节切换影子风格过渡 | Penumbra / Color / Contrast 在 0.5-1.0s 内平滑插值 | 无 | Vertical Slice |
| 高对比度辅助模式 | 影子变为纯黑 + 墙面变为纯白，contrastRatio ≥ 8:1 | 无 | Vertical Slice |
| 影子描边辅助模式 | 影子边缘叠加 1-2px 亮色（释然青 #7BA5A0）描边 | 无 | Vertical Slice |
| Ch.4 影子毛刺效果 | 影子边缘添加低频噪点位移（Shader 层实现） | 无 | Alpha |
| Ch.5 影子虚化/消散效果 | 影子 alpha 降低至 60-80%，边缘大幅柔化，消散粒子 | 无 | Alpha |
| Ch.5 缺席型"残缺影子"渲染 | 完整部分保持正常 alpha；缺失部分 alpha 从边缘向内渐降至 0.1（"影子正在消散"）。不使用粒子化（避免"特效感"违反 P3 克制表达）。通过 Shader `_AbsenceMask` 纹理控制 alpha 梯度区域 | 无 | Alpha |
| Ch.5 AbsenceAccepted 演出影子定格 | 影子在当前状态微微发光（alpha +0.05, 200ms）→ 缺口部分轻闪两次（alpha 0.1↔0.2, 各 300ms）→ 影子整体亮度降低至 70%（500ms）→ 缺口处浮现极淡虚影轮廓（alpha 0.05, 800ms 渐入）→ 3s 后转入 Complete | 无（由 Narrative Event System 控制音效） | Alpha |

## Game Feel

### Feel Reference

影子的实时变化应该感觉像**操纵一台小型皮影戏**——你移动手中的物件，墙上的影子即时且忠实地回应。光影关系的物理正确性让人产生"我在控制真实的光"的沉浸感。**不应该**感觉像是在调试参数面板或拖拽图层——影子是光的副产品，而不是一个独立的 UI 元素。

定格瞬间应该感觉像**按下快门捕捉到了一个瞬间**——突然间世界安静了，影子凝固在墙面上成为一张永恒的照片。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 影子跟随物件更新 | 16.7ms（最优）/ 33.4ms（可接受） | 1-2 frames | 允许 1 帧延迟换取性能 |
| 影子跟随光源更新 | 16.7ms（最优）/ 33.4ms（可接受） | 1-2 frames | 同上 |
| NearMatch 发光出现 | 100ms | 6 frames | 有意的渐入效果，不需即时 |
| PerfectMatch 定格 | 50ms | 3 frames | 从判定到视觉冻结需快速响应 |
| 质量档位切换 | 500ms | 30 frames | 允许短暂卡顿，通知"切换中" |
| 章节风格过渡 | 渐变 500-1000ms | 持续过渡 | 刻意的慢过渡，不需即时 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| NearMatch 辉光渐入 | 6 (100ms) | 持续 | 12 (200ms) 退出时 | 柔和呼吸感 | Linear Ease |
| PerfectMatch 定格闪白 | 0 | 3 (50ms) | 12 (200ms) | 快门闪光感 | 瞬闪后缓收 |
| PerfectMatch 轮廓锐化 | 0 | 18 (300ms) | 0 | 影子从当前清晰度过渡到最大锐度 | EaseOutQuad |
| 章节风格过渡 | 0 | 30-60 (500-1000ms) | 0 | 不知不觉的变化 | Linear |
| 影子消散 (Ch.5) | 12 (200ms) | 36 (600ms) | 0 | 缓慢消失，带有不舍感 | EaseInQuad |
| Degraded 模式切换 | 0 | 1 | 0 | 玩家不可感知的品质降低 | 无动画，即时切换 |

### Impact Moments

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| PerfectMatch 定格 | 300ms | Shadow Map 停止更新，ShadowRT 冻结，影子轮廓轻微白闪后稳定 | Yes |
| 章节末影子最终定格 | 800ms | 长时间冻结 + 影子对比度缓慢提升至最大值 | Yes |
| Ch.5 影子消散 | 1200ms | 影子从清晰到模糊到消散，边缘粒子化 | Yes |

### Weight and Responsiveness Profile

- **Weight**: 影子是光的物理产物，感觉"真实且有因果关系"——物件移一点，影子就变一点，不多不少。
- **Player control**: 间接但可预测。玩家不直接操作影子，而是通过操作物件和光源来间接控制。但影子的变化必须可预测——同样的物件位置 + 光源位置必须产生同样的影子。
- **Snap quality**: 影子本身不存在 snap（它是物理计算的结果）。NearMatch 发光和 PerfectMatch 定格是外加的反馈层，它们的 snap 感来自物件吸附，不来自影子本身。
- **Acceleration model**: 无加速度。物件移动一个像素，影子就跟着变化一个对应量。线性、即时。
- **Failure texture**: 无失败。影子永远如实反映物件和光源的关系。"错误"只是影子看起来不像目标——没有惩罚，只有"还没到位"的自然状态。

### Feel Acceptance Criteria

- [ ] 测试者自然地"一边拖物件一边看墙面影子"，而非拖完才看
- [ ] 影子更新延迟不可感知（测试者不报告"影子跟不上"）
- [ ] NearMatch 发光被理解为"快对了"而非"出 bug 了"
- [ ] PerfectMatch 定格让测试者感觉"影子活了"或"定格了一个瞬间"
- [ ] 低端设备（iPhone 13 Mini）上影子质量仍被认为"清晰可读"
- [ ] 无测试者报告影子边缘"锯齿严重"或"模糊看不清"

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 画质档位选项 | 设置界面 | 用户操作时 | 提供 Low/Medium/High 三档选择 |
| 高对比度影子开关 | 设置界面 > 辅助功能 | 用户操作时 | 辅助功能 |
| 影子描边辅助开关 | 设置界面 > 辅助功能 | 用户操作时 | 辅助功能 |
| 画质自动降级提示 | 画面角落，出现 3 秒后消失 | Degraded 状态触发时 | 提示"已优化画质以保证流畅" |

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| ShadowRT 供匹配度计算采样 | `design/gdd/shadow-puzzle-system.md` | Shadow Match Score 公式中的屏幕空间采样 | Data dependency |
| NearMatch / PerfectMatch 触发影子特效 | `design/gdd/shadow-puzzle-system.md` | 谜题状态机 NearMatch / PerfectMatch 状态 | State trigger |
| 章节 ID 决定影子风格预设 | `design/gdd/chapter-state-and-save.md` | 当前章节 ID | Data dependency |
| 投影墙面材质约束来自 Art Bible | `design/art/art-bible.md` | 影子清晰度技术标准 + 墙面材质原则 | Rule dependency |
| 质量档位和辅助功能开关 | `design/gdd/ui-system.md` (SettingsPanel) | 画质设置和辅助功能选项 | Data dependency |

## Acceptance Criteria

- [ ] **影子可读性**：投影墙面上的影子在 Low/Medium/High 三个档位下均被测试者评价为"轮廓清晰可辨认"
- [ ] **明暗比**：影子与墙面的明暗对比度在 Medium 档达到 ≥ 5:1（通过 Shader 采样验证）
- [ ] **帧率达标**：iPhone 13 Mini（Medium 档）上稳定 60fps，低端 Android（Low 档）上稳定 ≥ 45fps
- [ ] **Draw calls**：移动端（Medium 档）每帧 draw calls ≤ 131（给 150 硬上限留余量）
- [ ] **影子延迟**：物件/光源移动后，影子更新延迟 ≤ 1 帧（33ms @60fps）
- [ ] **ShadowRT 采样**：`AsyncGPUReadback` 完成时间 ≤ 2 帧，CPU 处理耗时 ≤ 1.5ms
- [ ] **匹配预算**：影子采样 + 匹配计算合计 ≤ 2ms/帧
- [ ] **Shadow Memory**：Shadow Map + ShadowRT 合计内存 ≤ 15MB（Medium 档）
- [ ] **NearMatch 发光**：发光效果不影响 ShadowRT 的灰度值（发光在 ShadowSampleCamera 之外渲染）
- [ ] **PerfectMatch 定格**：`FreezeShadow()` 调用后 ≤ 50ms 内影子可视停止更新
- [ ] **章节风格切换**：Ch.1-Ch.5 的 Penumbra/Color/Contrast 切换无跳变，过渡时间 0.5-1.0s
- [ ] **辅助功能**：高对比度模式下影子明暗比 ≥ 8:1，描边辅助模式下轮廓可见性进一步提升
- [ ] **自动降级**：连续 5 帧超 20ms 时自动降低质量档位，玩家不感知明显跳变
- [ ] **无硬编码**：所有 Shadow Map 分辨率、Bias 值、对比度参数通过配置文件/ScriptableObject 调节
- [ ] Performance: 渲染系统初始化（含 ShadowRT 创建）≤ 500ms

## Open Questions

| Question | Owner | Deadline | Priority | Resolution |
|----------|-------|----------|----------|-----------|
| **实时阴影 vs 投影纹理（Projector）vs 混合方案的最终选择** | TA / Graphics Lead | MVP 原型完成前 | **P0 — 最高** | 需要在 iPhone 13 Mini + 低端 Android（Mali-G52）上同时验证三种方案的帧率和视觉质量。当前 GDD 以 URP 实时阴影为基准方案，但需在原型阶段用 2 天时间做 A/B 测试：(A) 纯 URP Shadow Map (B) Projector/Decal 模拟 (C) 主光实时 + 辅光 Projector。评估指标：帧率、影子清晰度、多物件叠影质量、实现复杂度 |
| Shadow Map 512（Low 档）是否满足最低可读性要求？ | TA | 原型阶段 | P1 | 需要在低端 Android 上用实际谜题物件测试，可能需要 Shader 补偿方案 |
| AsyncGPUReadback 在低端 Android 设备上是否稳定？ | Engine Programmer | 原型阶段 | P1 | 若不稳定需准备 CPU Readback 降级方案（会引入更高延迟） |
| "Shadow Only"渲染模式（物件不可见但影子可见）的性能消耗是否可接受？ | TA | Vertical Slice | P2 | 仅第五章部分谜题需要此特性，可延后验证 |
| 章节间影子风格差异是否会影响谜题难度一致性？ | Game Design + TA | Vertical Slice | P2 | Ch.4-5 的边缘柔化可能让匹配判定更难——需协调 Shadow Puzzle System 调整阈值 |
| 是否需要为 ShadowRT 采样实现降采样方案以进一步优化 CPU 处理时间？ | Engine Programmer | Alpha | P3 | 如果 1024×1024 的 RT 处理超出 1.5ms 预算，可尝试降采样到 512×512 后再做匹配 |
| URP 版本升级（Unity 2022→2023）是否会影响 Shadow API？ | Engine Programmer | 远期 | P3 | 记录当前使用的 URP Shadow API，升级时对照变更日志 |
