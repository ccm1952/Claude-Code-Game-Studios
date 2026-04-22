#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
LUBAN_CSPROJ="$(cd "${SCRIPT_DIR}/../.." && pwd)/luban/src/Luban/Luban.csproj"

cd "${SCRIPT_DIR}"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "未找到 dotnet，请先安装 .NET SDK 并确保 dotnet 在 PATH 中。"
    exit 1
fi

if [ ! -f "${LUBAN_CSPROJ}" ]; then
    echo "未找到 Luban 工程：${LUBAN_CSPROJ}"
    echo "请先在 ${PROJECT_ROOT} 的同级目录克隆 luban-next 仓库。"
    exit 1
fi

[ -d Luban ] && rm -rf Luban

dotnet build "${LUBAN_CSPROJ}" -c Release -o Luban