using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TEngine.Editor
{
    public static class LubanTools
    {
        [MenuItem("TEngine/Luban/转表 &X", priority = -100)]
        private static void ZhuanXiaoYi()
        {
            string workDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Configs", "GameConfig"));
            List<string> environmentVars = GetShellEnvironmentPaths();
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            string path = Path.Combine(workDirectory, "gen_code_bin_to_project_lazyload.sh");
#elif UNITY_EDITOR_WIN
            string path = Path.Combine(workDirectory, "gen_code_bin_to_project_lazyload.bat");
#endif
            path = Path.GetFullPath(path);

            if (!File.Exists(path))
            {
                Debug.LogError($"转表脚本不存在：{path}");
                return;
            }

            Debug.Log($"执行转表：{path}");
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            ShellHelper.Run($"bash \"{path}\"", workDirectory, environmentVars);
#elif UNITY_EDITOR_WIN
            ShellHelper.Run($"\"{path}\"", workDirectory, environmentVars);
#endif
        }

        private static List<string> GetShellEnvironmentPaths()
        {
            List<string> paths = new();
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            AddPath(paths, Path.Combine(userHome, ".dotnet"));
            AddPath(paths, Path.Combine(userHome, ".dotnet", "tools"));
            AddPath(paths, Path.Combine(programFiles, "dotnet"));
            AddPath(paths, Path.Combine(programFilesX86, "dotnet"));
            AddPath(paths, "/usr/local/share/dotnet/x64");
            AddPath(paths, "/usr/local/share/dotnet");
            AddPath(paths, "/opt/homebrew/bin");
            AddPath(paths, "/usr/local/bin");

            return paths;
        }

        private static void AddPath(List<string> paths, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !paths.Contains(path))
            {
                paths.Add(path);
            }
        }
    }
}