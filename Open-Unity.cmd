@echo off
set "EDITOR=F:\CheckInPleaseTools\Unity\6000.3.28f1\Editor\Unity.exe"
if not exist "%EDITOR%" (
  echo Unity Editor is not installed yet. Check F:\CheckInPleaseTools.
  pause
  exit /b 1
)
start "" "%EDITOR%" -projectPath "%~dp0UnityDemo"
