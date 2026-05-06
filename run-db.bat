@echo off
setlocal

set ACTION=%1
if "%ACTION%"=="" set ACTION=up

if "%ACTION%"=="up" (
    if not exist ".env" (
        echo ERROR: .env file not found. Copy .env.example to .env and fill in the secrets.
        echo   copy .env.example .env
        exit /b 1
    )
    docker compose -f docker-compose.yml up -d db
    goto end
)

if "%ACTION%"=="down" (
    docker compose -f docker-compose.yml stop db
    goto end
)

if "%ACTION%"=="logs" (
    docker compose -f docker-compose.yml logs -f db
    goto end
)

if "%ACTION%"=="clean" (
    docker compose -f docker-compose.yml rm -f -s db
    goto end
)

echo Usage: run-db.bat [up^|down^|logs^|clean]
echo   up      - Create/start only SQL Server container (default)
echo   down    - Stop only SQL Server container
echo   logs    - Tail SQL Server container logs
echo   clean   - Remove stopped SQL Server container

:end
endlocal
