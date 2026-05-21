@echo off
chcp 65001 >nul
title AAFSS - 声疲劳载荷谱编制系统

cd /d "D:\2026\试点10\AAFSS\src\AAFSS.App"

echo ============================================
echo   AAFSS 声疲劳载荷谱编制系统
echo ============================================
echo.
echo [1/2] 编译中...
dotnet build -c Release --nologo -v q
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 编译失败！请检查是否安装了 .NET 8 SDK
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [2/2] 启动中...
start "" "bin\Release\net8.0-windows\AAFSS.App.exe"
exit
