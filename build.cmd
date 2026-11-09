@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo csc.exe nicht gefunden: %CSC%
  pause
  exit /b 1
)
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:FolderList.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll FolderList.cs
if errorlevel 1 (
  echo.
  echo BUILD FEHLGESCHLAGEN
) else (
  echo.
  echo BUILD OK: %~dp0FolderList.exe
  echo Die Datei FolderList.exe.config muss neben der EXE liegen bleiben.
)
pause
