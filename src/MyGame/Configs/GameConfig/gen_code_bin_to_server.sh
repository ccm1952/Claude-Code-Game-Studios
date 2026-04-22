#!/bin/bash

set -e

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../../)"
export LUBAN_DLL="${WORKSPACE}/Tools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export DATA_OUTPATH="${WORKSPACE}/Server/GameConfig"
export CODE_OUTPATH="${WORKSPACE}/Server/Hotfix/Config/GameConfig"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "未找到 dotnet，请先安装 .NET SDK 并确保 dotnet 在 PATH 中。"
    exit 1
fi

if [ ! -f "${LUBAN_DLL}" ]; then
    echo "未找到 Luban.dll：${LUBAN_DLL}"
    echo "请先在 Tools 目录执行 build-luban.sh 生成 Luban 工具。"
    exit 1
fi

dotnet "${LUBAN_DLL}" \
    -t server \
    -c cs-bin \
    -d bin \
    --conf "${CONF_ROOT}/luban.conf" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}"
