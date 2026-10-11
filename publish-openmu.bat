@echo off
setlocal
title Publish OpenMU Server

cd /d "C:\OpenMU" || goto :missing_source
if not exist "src\Startup\MUnique.OpenMU.Startup.csproj" goto :missing_source

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] dotnet was not found. Install the .NET 10 SDK first.
  pause
  exit /b 1
)

echo ========================================
echo         Publish OpenMU Server
echo ========================================
echo.
echo Stop the running OpenMU server before continuing.
echo Output folder: C:\OpenMU\Server
echo Back up customized configuration files before publishing.
echo Press Ctrl+C to cancel, or any key to publish.
pause >nul

dotnet publish "src\Startup\MUnique.OpenMU.Startup.csproj" -c Release -p:ci=true -o "C:\OpenMU\Server"
if errorlevel 1 goto :publish_failed

echo.
echo [SUCCESS] Publishing completed.
echo Start OpenMU using C:\OpenMU\start-openmu.bat.
echo Open Admin Panel - Updates and apply Enable starting Castle Siege skills.
echo Restart OpenMU after applying the update.
pause
exit /b 0

:missing_source
echo [ERROR] OpenMU source project was not found under C:\OpenMU\src.
pause
exit /b 1

:publish_failed
echo.
echo [ERROR] Publishing failed. See the build errors above.
echo Do not start the server from a partially updated output folder.
pause
exit /b 1
