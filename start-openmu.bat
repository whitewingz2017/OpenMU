@echo off
setlocal
 title OpenMU Server
cd /d "C:\OpenMU\Server" || (
  echo [ERROR] C:\OpenMU\Server was not found.
  pause
  exit /b 1
)
if not exist "MUnique.OpenMU.Startup.dll" (
  echo [ERROR] OpenMU startup DLL not found.
  pause
  exit /b 1
)
if not defined DB_ADMIN_USER (
  echo [ERROR] DB_ADMIN_USER is not configured.
  echo Set the permanent Windows environment variables, then open a new window.
  pause
  exit /b 1
)
if not defined DB_ADMIN_PW (
  echo [ERROR] DB_ADMIN_PW is not configured.
  echo Set the permanent Windows environment variables, then open a new window.
  pause
  exit /b 1
)
echo ========================================
echo           Starting OpenMU
echo ========================================
echo.
dotnet MUnique.OpenMU.Startup.dll -autostart
echo.
echo [INFO] OpenMU has stopped. Exit code: %errorlevel%
pause
endlocal
