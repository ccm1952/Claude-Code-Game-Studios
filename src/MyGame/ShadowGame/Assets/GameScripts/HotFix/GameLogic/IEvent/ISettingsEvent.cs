// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Settings 变更事件接口（SettingsSystem → 任意消费者）。
    /// <para>Sender: SettingsSystem / SettingsPanel</para>
    /// <para>Listener: InputConfigFromLuban（触摸灵敏度）、AudioService、ShadowRendering 等</para>
    /// <para>Cascade depth: 0（配置变更为终点事件，监听者不应再 Send 其他事件）</para>
    /// <para>协议来源：ADR-027 取代 ADR-006 §1 `Evt_Settings_TouchSensitivityChanged`</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ISettingsEvent
    {
        /// <summary>
        /// 通用设置变更（字符串键值对，适用于绝大多数 PlayerPrefs 项）。
        /// </summary>
        /// <param name="key">设置项 key（与 PlayerPrefs key 一致）。</param>
        /// <param name="value">字符串形式的新值（bool/int/float 由消费者解析）。</param>
        void OnSettingChanged(string key, string value);

        /// <summary>
        /// 触摸灵敏度变更（高频 / 热路径，单独接口方法避免字符串解析）。
        /// <c>multiplier</c> 会被 InputConfigFromLuban Clamp 到 [0.5, 2.0]。
        /// </summary>
        void OnTouchSensitivityChanged(float multiplier);
    }
}
