@echo off
if not exist "%~dp0Builds\Windows\build-success.json" (
  echo Windows build is not available yet.
  echo Activate a Unity license in Unity Hub, then run Build-Demo.ps1.
  pause
  exit /b 1
)
start "" "%~dp0Builds\Windows\CheckInPlease.exe"
