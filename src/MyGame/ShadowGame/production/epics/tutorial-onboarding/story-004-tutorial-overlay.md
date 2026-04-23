// 该文件由Cursor 自动生成

# Story 004: TutorialOverlay UIWindow (Gesture Images + Text Prompts)

> **Epic**: Tutorial & Onboarding
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/tutorial-onboarding.md`
**Requirement**: `TR-tutor-008`

**ADR Governing Implementation**: ADR-011: UIWindow Management; ADR-005: YooAsset Resource Lifecycle
**ADR Decision Summary**: `TutorialOverlay` 继承 `UIWindow`，置于 Overlay 层（SortOrder base=300），使用 `GameModule.UI.ShowWindow<TutorialOverlay>()` / `CloseWindow<TutorialOverlay>()` 管理；手势图片通过 `GameModule.Resource.LoadAssetAsync<Sprite>()` 异步加载；UIWindow 不直接引用其他系统——通过 GameEvent 接收显示指令。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Presentation — UIWindow)**:
- Required: 继承 `UIWindow`；Overlay 层（SortOrder base=300）；`OnCreate` 初始化（仅第一次）；`OnRefresh` 每次显示时更新内容；所有 UI 根 Canvas 设置 `SetUISafeFitHelper`；CanvasScaler = Scale With Screen Size，1920×1080，match 0.5；最小触摸目标 44×44dp；所有 UI 预制体通过 `GameModule.Resource.LoadAssetAsync` 加载；图片资源加载后通过 `Release()` 在 `OnClose` 时释放
- Forbidden: 禁止用 `Resources.Load`；禁止跨系统直接引用；禁止用 UI Toolkit
- Guardrail: UI 系统每帧 < 0.5ms；TutorialPromptPanel 使用 InputFilter（不是 InputBlocker）

---

## Acceptance Criteria

- [ ] `TutorialOverlay` 继承 `UIWindow`，挂载在 Overlay UI 层（Sort Order base = 300）
- [ ] 提示面板由两个元素组成：手势循环动画图片（Image 组件）+ 单行提示文字（TextMeshPro）
- [ ] 监听 `Evt_TutorialStepStarted(stepId)`：从 `TbTutorialStep.Get(stepId)` 读取配置，异步加载 `promptImagePath` 对应图片，设置 `promptTextKey` 对应的本地化文字，在 `promptPosition` 指定区域显示
- [ ] 监听 `Evt_TutorialPaused`：执行淡出动画（0.3s）隐藏面板，不关闭 Window（保留状态）
- [ ] 监听 `Evt_TutorialResumed`：执行淡入动画（0.3s）重新显示面板（演出结束后恢复）
- [ ] 监听 `Evt_TutorialStepCompleted`：执行淡出动画（0.3s）后隐藏提示
- [ ] 提示位置映射：`Bottom` → 屏幕底部安全区上方 20dp；`Center` → 屏幕中央；`NearObject` → 跟随目标物件屏幕位置（Story 005 传递坐标）
- [ ] 手势图片循环播放（帧动画或 Sprite Animation），循环间隔从 `TbTutorialStep.gestureAnimLoopInterval`（Tuning Knobs）获取，默认 2.0s
- [ ] 图片加载失败时只显示文字（不崩溃，`Log.Warning`）
- [ ] 所有文字通过 `GameModule.Localization` 的本地化 key 显示，支持运行时语言切换
- [ ] `OnClose` 时释放图片资源句柄（`Release()`），避免内存泄漏
- [ ] 提示文字字号 ≥ 16sp，触摸（如有按钮）目标 ≥ 44dp

---

## Implementation Notes

**UIWindow 生命周期钩子：**
```csharp
public class TutorialOverlay : UIWindow
{
    private AssetHandle _gestureImageHandle;
    private Image _gestureImage;
    private TMP_Text _promptText;

    protected override void OnCreate()
    {
        // 初始化 UI 组件引用（一次性）
        _gestureImage = GetComponentInChildren<Image>();
        _promptText = GetComponentInChildren<TMP_Text>();
        
        // 注册 GameEvent 监听
        GameEvent.AddEventListener<int>(EventId.Evt_TutorialStepStarted, OnStepStarted);
        GameEvent.AddEventListener<int>(EventId.Evt_TutorialStepCompleted, OnStepCompleted);
        GameEvent.AddEventListener(EventId.Evt_TutorialPaused, OnPaused);
        GameEvent.AddEventListener(EventId.Evt_TutorialResumed, OnResumed);
    }

    protected override void OnClose()
    {
        // 释放图片资源
        _gestureImageHandle?.Release();
        _gestureImageHandle = null;
        
        // 移除监听
        GameEvent.RemoveEventListener<int>(EventId.Evt_TutorialStepStarted, OnStepStarted);
        // ...
    }

    private async UniTaskVoid OnStepStarted(int stepId)
    {
        var step = Tables.Instance.TbTutorialStep.Get(stepId);
        if (step == null) return;

        // 加载图片
        _gestureImageHandle?.Release();
        _gestureImageHandle = GameModule.Resource.LoadAssetAsync<Sprite>(step.PromptImagePath);
        await _gestureImageHandle.ToUniTask();
        
        if (_gestureImageHandle.AssetObject != null)
            _gestureImage.sprite = _gestureImageHandle.AssetObject as Sprite;
        else
            Log.Warning($"[Tutorial] Gesture image not found: {step.PromptImagePath}");

        // 设置文字（本地化）
        _promptText.text = GameModule.Localization.GetString(step.PromptTextKey);

        // 设置位置
        SetPromptPosition(step.PromptPosition);

        // 淡入
        await FadeIn(0.3f);
    }
}
```

**提示位置实现：**
```csharp
private void SetPromptPosition(PromptPosition position)
{
    var rect = GetComponent<RectTransform>();
    switch (position)
    {
        case PromptPosition.Bottom:
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 80f); // 安全区上方 20dp ≈ 80px
            break;
        case PromptPosition.Center:
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            break;
        case PromptPosition.NearObject:
            // 位置由 Evt_TutorialObjectPosition(worldPos) 动态更新（Story 005 传递）
            break;
    }
}
```

**动画（DOTween）：**
```csharp
private UniTask FadeIn(float duration)
{
    var cg = GetComponent<CanvasGroup>();
    cg.alpha = 0f;
    gameObject.SetActive(true);
    return cg.DOFade(1f, duration).ToUniTask();
}

private async UniTask FadeOut(float duration)
{
    var cg = GetComponent<CanvasGroup>();
    await cg.DOFade(0f, duration).ToUniTask();
    gameObject.SetActive(false);
}
```

---

## Out of Scope

- [Story 001]: TutorialController 决定何时发送 Evt_TutorialStepStarted
- [Story 002]: 配置表字段定义
- [Story 003]: InputFilter（Overlay 不负责输入控制）
- [Story 005]: NearObject 位置坐标的来源（跟随物件逻辑）
- 手势图片的美术资产创建（美术负责）

---

## QA Test Cases

### TC-004-01: 提示面板正确出现
**Setup** 打开测试关卡，触发 `Evt_TutorialStepStarted(101)`（tut_drag）
**Verify** TutorialOverlay 在屏幕底部出现；显示拖拽手势图片（或占位图）；显示"拖动物件到新位置"文字；有淡入动画（0.3s 内 alpha 0→1）
**Pass** 面板可见；文字正确；图片加载或显示文字降级（无崩溃）

### TC-004-02: 提示面板正确消失
**Setup** TutorialOverlay 正在显示步骤 `tut_drag`
**Verify** 发送 `Evt_TutorialStepCompleted(101)`
**Pass** 面板执行淡出动画（0.3s）后隐藏；不影响游戏操作

### TC-004-03: 演出暂停/恢复提示
**Setup** TutorialOverlay 正在显示，发送 `Evt_TutorialPaused`
**Verify** 面板淡出；30s 后发送 `Evt_TutorialResumed`
**Pass** 面板重新淡入显示，内容与暂停前相同

### TC-004-04: 图片加载失败降级
**Setup** 配置表 `promptImagePath` 指向不存在的资源路径
**Verify** 触发 `Evt_TutorialStepStarted`
**Pass** 面板显示文字提示但无图片；控制台有 `Log.Warning`；不崩溃；用户可正常完成教学

### TC-004-05: 安全区适配
**Setup** 在刘海屏设备（iPhone 15）上测试
**Verify** TutorialOverlay 的 Bottom 位置提示不被刘海/Home 指示器遮挡
**Pass** 提示完全可见；无 UI 被系统 UI 遮挡

### TC-004-06: 语言切换实时更新
**Setup** TutorialOverlay 正在显示（中文）
**Verify** 通过设置切换到英文（`Evt_SettingChanged`）
**Pass** 提示文字立即更新为英文版本；图片不变

### TC-004-07: 资源不泄漏
**Setup** 触发教学步骤 → 完成 → 关闭 TutorialOverlay
**Verify** 在 Unity Memory Profiler 检查 Sprite 资产引用数量
**Pass** 图片资源引用计数减少（句柄已释放）；无残留

---

## Test Evidence

**Story Type**: UI
**Required evidence**: `production/qa/evidence/tutorial/overlay-ui-evidence.md`
**Status**: [ ] Not yet created

**Evidence checklist**:
- [ ] 截图：各 promptPosition 下的提示显示效果（Bottom / Center / NearObject）
- [ ] 截图：图片加载失败时的降级显示
- [ ] 截图：iPhone SE / iPhone 15 / iPad 三个分辨率下的布局
- [ ] 录屏：淡入/淡出动画效果（0.3s）
- [ ] 录屏：语言切换实时更新

---

## Dependencies

- Depends on: ui-system epic (UIWindow 基础设施), Story 001 (Evt_TutorialStepStarted 事件发送), Story 002 (TbTutorialStep 配置表字段), YooAsset 图片加载
- Unlocks: Story 005 (NearObject 位置坐标需要 Overlay 接收)
