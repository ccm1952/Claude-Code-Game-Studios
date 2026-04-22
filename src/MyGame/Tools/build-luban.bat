@echo off
setlocal

cd /d "%~dp0"

for %%I in ("%CD%\..") do set "PROJECT_ROOT=%%~fI"
for %%I in ("%CD%\..\..") do set "PARENT_ROOT=%%~fI"
set "LUBAN_CSPROJ=%PARENT_ROOT%\luban\src\Luban\Luban.csproj"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo 未找到 dotnet，请先安装 .NET SDK 并确保 dotnet 在 PATH 中。
    exit /b 1
)

if not exist "%LUBAN_CSPROJ%" (
    echo 未找到 Luban 工程：%LUBAN_CSPROJ%
    echo 请先在 %PROJECT_ROOT% 的同级目录克隆 luban-next 仓库。
    exit /b 1
)

if exist Luban rd /s /q Luban

dotnet build "%LUBAN_CSPROJ%" -c Release -o Luban

if /I "%~1"=="--pause" pause