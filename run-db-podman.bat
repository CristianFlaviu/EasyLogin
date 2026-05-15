@echo off
setlocal

set ACTION=%1
if "%ACTION%"=="" set ACTION=up

set CONTAINER_NAME=easylogin-db
set VOLUME_NAME=easylogin_sqldata
set DB_IMAGE=mcr.microsoft.com/mssql/server:2022-latest

if "%ACTION%"=="up" (
    if not exist ".env" (
        echo ERROR: .env file not found. Copy .env.example to .env and fill in the secrets.
        echo   copy .env.example .env
        exit /b 1
    )

    podman container exists %CONTAINER_NAME% >nul 2>&1
    if errorlevel 1 (
        podman run -d --name %CONTAINER_NAME% --restart=always --env-file .env -e ACCEPT_EULA=Y -p 1433:1433 -v %VOLUME_NAME%:/var/opt/mssql %DB_IMAGE%
    ) else (
        podman start %CONTAINER_NAME%
    )
    goto end
)

if "%ACTION%"=="down" (
    podman stop %CONTAINER_NAME%
    goto end
)

if "%ACTION%"=="logs" (
    podman logs -f %CONTAINER_NAME%
    goto end
)

if "%ACTION%"=="clean" (
    podman rm -f %CONTAINER_NAME%
    goto end
)

echo Usage: run-db.bat [up^|down^|logs^|clean]
echo   up      - Create/start only SQL Server container with Podman (default)
echo   down    - Stop only SQL Server container
echo   logs    - Tail SQL Server container logs
echo   clean   - Remove SQL Server container
echo.
echo Docker Compose version: run-db-docker.bat [up^|down^|logs^|clean]

:end
endlocal
