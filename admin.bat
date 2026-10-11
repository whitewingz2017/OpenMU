@echo off
setlocal EnableDelayedExpansion

title OpenMU Admin Panel

echo.
echo ========================================
echo        OpenMU Admin Panel
echo ========================================
echo.

cd /d C:\OpenMU

echo Checking OpenMU container...

REM Check if openmu-startup is running
docker ps --format "{{.Names}}" | findstr /x "openmu-startup" >nul

if not errorlevel 1 (
    echo [OK] OpenMU is already running.
    goto GET_PORT
)

echo [INFO] OpenMU is not running.
echo [INFO] Starting OpenMU...
echo.

REM Try to start existing container first
docker start openmu-startup >nul 2>&1

if not errorlevel 1 (
    echo [OK] OpenMU container started.
    goto GET_PORT
)

echo [INFO] Existing container was not found.
echo [INFO] Starting OpenMU through Docker Compose...
echo.

REM Start only the OpenMU service, not nginx
docker compose up -d openmu-startup

if errorlevel 1 (
    echo.
    echo [ERROR] Failed to start OpenMU.
    echo.
    echo Current containers:
    docker ps -a
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] OpenMU started.

:GET_PORT

echo.
echo Waiting for OpenMU Admin Panel...
timeout /t 2 /nobreak >nul

set "DOCKER_PORT="

for /f "delims=" %%A in ('docker port openmu-startup 8080/tcp 2^>nul') do (
    set "DOCKER_PORT=%%A"
)

if not defined DOCKER_PORT (
    echo.
    echo [ERROR] Could not find Admin Panel port.
    echo.
    echo Current Docker port mappings:
    docker port openmu-startup
    echo.
    echo Container status:
    docker ps -a --filter "name=openmu-startup"
    echo.
    pause
    exit /b 1
)

echo Docker mapping:
echo !DOCKER_PORT!

REM Extract the port number from the end of the mapping
for /f "delims=" %%A in ('powershell -NoProfile -Command "$s='!DOCKER_PORT!'; if ($s -match ':(\d+)$') { $Matches[1] }"') do (
    set "PORT=%%A"
)

if not defined PORT (
    echo.
    echo [ERROR] Could not extract Admin Panel port.
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo Admin Panel Port: !PORT!
echo ========================================
echo.
echo Opening:
echo http://localhost:!PORT!
echo.

start "" "http://localhost:!PORT!"

echo Browser opened.
echo.
pause