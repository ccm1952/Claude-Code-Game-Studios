using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Game
{
    /// <summary>
    /// 数据持久化管理
    /// Author:翼小鬼
    /// 2021/10/20
    /// </summary>
    [System.Serializable]
    public class DataBase
    {

        public virtual string DataPath => GetType().Name;
        public string FilePath => Path.Combine(UnityEngine.Application.persistentDataPath, "GameData", DataPath);
        public string DirectoryPath => Path.Combine(UnityEngine.Application.persistentDataPath, "GameData");


        /// <summary>
        /// 加载保持数据
        /// </summary>
        public void LoadData()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }
            if (!File.Exists(FilePath))
            {
                StreamWriter stream = File.CreateText(FilePath);
                stream.Close();
            }
            string text = File.ReadAllText(FilePath);
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    text = this.ToJson();
                }
                else
                {
                    text = Decrypt(text);
                }
                //读取JSON写到自己里面
                JsonConvert.PopulateObject(text, this);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Game_DataBase: Direct parsing failed. Clear file :{e}");
            }
        }
        /// <summary>
        /// 保存数据
        /// </summary>
        public void SaveData()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }
            StreamWriter stream = File.CreateText(FilePath);
            string jsonText = this.ToJson();
            jsonText = Encrypt(jsonText);
            stream.Write(jsonText);
            stream.Close();
        }


        /// <summary>
        /// 加密String
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="k">密码</param>
        /// <returns></returns>
        private static string Encrypt(string content, string k = "1234567890abcdef")
        {
            byte[] keyBytes = System.Text.UTF8Encoding.UTF8.GetBytes(k);
            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged();
            rm.Key = keyBytes;
            rm.Mode = System.Security.Cryptography.CipherMode.ECB;
            rm.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            System.Security.Cryptography.ICryptoTransform ict = rm.CreateEncryptor();
            byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
            byte[] resultBytes = ict.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            return System.Convert.ToBase64String(resultBytes);
        }
        /// <summary>
        /// 解密String
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="k">密码</param>
        /// <returns></returns>
        private static string Decrypt(string content, string k = "1234567890abcdef")
        {
            byte[] keyBytes = System.Text.UTF8Encoding.UTF8.GetBytes(k);
            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged();
            rm.Key = keyBytes;
            rm.Mode = System.Security.Cryptography.CipherMode.ECB;
            rm.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            System.Security.Cryptography.ICryptoTransform ict = rm.CreateDecryptor();
            byte[] contentBytes = System.Convert.FromBase64String(content);
            byte[] resultBytes = ict.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            return System.Text.UTF8Encoding.UTF8.GetString(resultBytes);

        }

    }
}
