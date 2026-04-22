using System.IO;
using Launcher;
using TEngine;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    /// <summary>
    /// 流程 => 闪屏。
    /// </summary>
    public class ProcedureGetVersion : ProcedureBase
    {
        public override bool UseNativeDialog => true;

        private ProcedureOwner _procedureOwner;
        
        private UnityWebRequest _webRequest;

        private const string versionPath = "version.txt";
        private bool downloadTag = false;
        public static string VersionContent = "";

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            _procedureOwner = procedureOwner;

            DownloadVersion(procedureOwner);
        }

        private void DownloadVersion(ProcedureOwner procedureOwner)
        {
            downloadTag = false;
            LauncherMgr.ShowUI<LoadUpdateUI>("开始下载配置文件");
            EPlayMode playMode = _resourceModule.PlayMode;

            // 编辑器模式。
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                Log.Info("Editor resource mode detected.");
                ChangeState<ProcedureInitPackage>(procedureOwner);
            }
            // 单机模式。
            else if (playMode == EPlayMode.OfflinePlayMode)
            {
                Log.Info("Package resource mode detected.");
                ChangeState<ProcedureInitPackage>(procedureOwner);
            }
            // 可更新模式。
            else if (playMode == EPlayMode.HostPlayMode ||
                     playMode == EPlayMode.WebPlayMode)
            {
                
                Log.Info("Host resource mode detected.");
                var path = Path.Combine(_resourceModule.HostServerURL, versionPath);
                _webRequest = UnityWebRequest.Get(path);
                _webRequest.SendWebRequest();

            }
            else
            {
                Log.Error("UnKnow resource mode detected Please check???");
            }
        }
        
        private void OnInitPackageFailed(ProcedureOwner procedureOwner, string message)
        {
            Log.Error($"{message}");

            // 打开启动UI。
            LauncherMgr.ShowUI<LoadUpdateUI>("开始下载配置文件");

            LauncherMgr.ShowMessageBox("下载配置版本文件失败，请检查网络连接", () => { Retry(procedureOwner); },
                Application.Quit);
        }
        
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (downloadTag || _webRequest == null || _webRequest.isDone == false) return;
            
            downloadTag = true;
            if (_webRequest.error != null)
            {
                OnInitPackageFailed(procedureOwner, _webRequest.error);
                Log.Error($"下载 version.txt失败， path = {_webRequest.url}, err = {_webRequest.error}");
                return;
            }

            var text = _webRequest.downloadHandler.text;
            if (string.IsNullOrEmpty(text))
            {
                Log.Error("version.txt 是空！");
                OnInitPackageFailed(procedureOwner, _webRequest.error);
                return;
            }
            text = text.Trim();
            VersionContent = text;
            var hostUrl = Path.Combine(_resourceModule.HostServerURL, text);
            Log.Info($"Host URL = {hostUrl}");
            _resourceModule.SetRemoteServicesUrl(hostUrl, hostUrl);
            ChangeState<ProcedureInitPackage>(procedureOwner);
        }
        
        private void Retry(ProcedureOwner procedureOwner)
        {
            DownloadVersion(procedureOwner);
        }
    }
}
