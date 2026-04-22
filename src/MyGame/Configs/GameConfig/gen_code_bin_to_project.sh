#!/bin/bash

set -e

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../../)"
export LUBAN_DLL="${WORKSPACE}/Tools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export DATA_OUTPATH="${WORKSPACE}/UnityProject/Assets/AssetRaw/Configs/bytes/"
export CODE_OUTPATH="${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "未找到 dotnet，请先安装 .NET SDK 并确保 dotnet 在 PATH 中。"
    exit 1
fi

if [ ! -f "${LUBAN_DLL}" ]; then
    echo "未找到 Luban.dll：${LUBAN_DLL}"
    echo "请先在 Tools 目录执行 build-luban.sh 生成 Luban 工具。"
    exit 1
fi

cp -R "${CONF_ROOT}/CustomTemplate/ConfigSystem.cs" \
   "${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs"
cp -R "${CONF_ROOT}/CustomTemplate/ExternalTypeUtil.cs" \
    "${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/ExternalTypeUtil.cs"

dotnet "${LUBAN_DLL}" \
    -t client \
    -c cs-bin \
    -d bin \
    --conf "${CONF_ROOT}/luban.conf" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}"
