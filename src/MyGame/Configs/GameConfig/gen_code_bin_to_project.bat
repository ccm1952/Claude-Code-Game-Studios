@echo off
setlocal

Cd /d %~dp0
echo %CD%

for %%I in ("%CD%\..\..") do set "WORKSPACE=%%~fI"
set "LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll"
set "CONF_ROOT=%CD%"
set "DATA_OUTPATH=%WORKSPACE%\UnityProject\Assets\AssetRaw\Configs\bytes\"
set "CODE_OUTPATH=%WORKSPACE%\UnityProject\Assets\GameScripts\HotFix\GameProto\GameConfig\"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo 未找到 dotnet，请先安装 .NET SDK 并确保 dotnet 在 PATH 中。
    exit /b 1
)

if not exist "%LUBAN_DLL%" (
    echo 未找到 Luban.dll：%LUBAN_DLL%
    echo 请先在 Tools 目录执行 build-luban.bat 生成 Luban 工具。
    exit /b 1
)

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\ConfigSystem.cs" "%WORKSPACE%\UnityProject\Assets\GameScripts\HotFix\GameProto\ConfigSystem.cs"
xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\ExternalTypeUtil.cs" "%WORKSPACE%\UnityProject\Assets\GameScripts\HotFix\GameProto\ExternalTypeUtil.cs"

dotnet "%LUBAN_DLL%" ^
    -t client ^
    -c cs-bin ^
    -d bin^
    --conf "%CONF_ROOT%\luban.conf" ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir="%CODE_OUTPATH%" ^
    -x outputDataDir="%DATA_OUTPATH%" 

if /I "%~1"=="--pause" pause

