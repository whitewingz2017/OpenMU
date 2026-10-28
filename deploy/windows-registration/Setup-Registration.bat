@echo off
setlocal EnableExtensions DisableDelayedExpansion
title TwinMU VPS Registration Setup
if not exist "%~dp0Setup-Registration.ps1" (
  echo Extract the complete RegistrationSetup ZIP before running this file.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-Registration.ps1" %*
set "setup_result=%errorlevel%"
echo.
if not "%setup_result%"=="0" echo Setup did not finish. Read the message above before trying again.
pause
exit /b %setup_result%
