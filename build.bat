@echo off
setlocal
cd /d "%~dp0"
set "SRC=%~dp0src\Cyberloafing"
set "OUT=%~dp0bin"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo 找不到 C# 编译器：%CSC%
  exit /b 1
)
if not exist "%OUT%" mkdir "%OUT%"

set "RSP=%TEMP%\cyberloafing-sources.rsp"
if exist "%RSP%" del "%RSP%"
for %%F in ("%SRC%\*.cs") do echo "%%~fF">>"%RSP%"

"%CSC%" /nologo /target:winexe /optimize /codepage:65001 /platform:anycpu /win32manifest:"%SRC%\app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"%OUT%\Cyberloafing.exe" @"%RSP%"
if errorlevel 1 (
  echo 编译失败
  exit /b 1
)

copy /y "%SRC%\App.config" "%OUT%\Cyberloafing.exe.config" >nul
for %%F in ("%~dp0content\*.*") do (
  if not exist "%OUT%\%%~nxF" copy /y "%%~fF" "%OUT%\%%~nxF" >nul
)
if not exist "%OUT%\novel" mkdir "%OUT%\novel"
for %%F in ("%~dp0content\novel\*.txt") do (
  if not exist "%OUT%\novel\%%~nxF" copy /y "%%~fF" "%OUT%\novel\" >nul
)
echo 已生成 bin\Cyberloafing.exe
