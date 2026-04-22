using System;
using UnityEngine;

/// <summary>
/// 本地数据持久化
/// 非常轻便简洁，使用过快捷
/// Author:翼小鬼
/// 2021/10/20
/// </summary>
public static class PPData
{

    /// <summary>
    /// 保存Int类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">Int数据</param>
    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    /// <summary>
    /// 获取Int类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一/param>
    /// <returns>Int数据</returns>
    public static int GetInt(string key, int defaultValue = default)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }
    
    
    /// <summary>
    /// 保存long类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">long数据</param>
    public static void SetLong(string key, long value)
    {
        PlayerPrefs.SetString(key, value.ToString());
    }

    /// <summary>
    /// 获取long类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一/param>
    /// <returns>long数据</returns>
    public static long GetLong(string key, long defaultValue = default)
    {
        if (long.TryParse(PlayerPrefs.GetString(key), out long result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 保存Float类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">Float数据</param>
    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    /// <summary>
    /// 获取Float类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <returns>Float数据</returns>
    public static float GetFloat(string key, float defaultValue = default)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    /// <summary>
    /// 保存String类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">String数据</param>
    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    /// <summary>
    /// 获取String类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <returns>String数据</returns>
    public static string GetString(string key, string defaultValue = default)
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    /// <summary>
    /// 保存Bool类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">Bool数据</param>
    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    /// <summary>
    /// 获取Bool类型的数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <returns>Bool数据</returns>
    public static bool GetBool(string key, bool defaultValue = default)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    /// <summary>
    /// 保存Enum类型的数据
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">Enum数据</param>
    public static void SetEnum<T>(string key, T value) where T : Enum
    {
        PlayerPrefs.SetString(key, value.ToString());
    }

    /// <summary>
    /// 获取Enum类型的数据
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="key">Key值：唯一</param>
    /// <returns>Enum数据</returns>
    public static T GetEnum<T>(string key, T defaultValue = default) where T : Enum
    {
        return (T)Enum.Parse(typeof(T), PlayerPrefs.GetString(key, defaultValue.ToString()));
    }

    /// <summary>
    /// 保存Object类型的数据(Object可序列化)
    /// </summary>
    /// <typeparam name="T">Object类型</typeparam>
    /// <param name="key">Key值：唯一</param>
    /// <param name="value">Object数据</param>
    public static void SetObject<T>(string key, T value)
    {
        PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
    }

    /// <summary>
    /// 获取Object类型的数据(Object可序列化)
    /// </summary>
    /// <typeparam name="T">Object类型</typeparam>
    /// <param name="key">Key值：唯一</param>
    /// <returns>Object数据</returns>
    public static T GetObject<T>(string key, T defaultValue = default)
    {
        return JsonUtility.FromJson<T>(PlayerPrefs.GetString(key, JsonUtility.ToJson(defaultValue)));
    }

    /// <summary>
    /// 保存一下
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 删除某个数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    /// <summary>
    /// 清理所有的数据
    /// </summary>
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }

    /// <summary>
    /// 是否存在某个数据
    /// </summary>
    /// <param name="key">Key值：唯一</param>
    /// <returns>存在与否</returns>
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }
}
