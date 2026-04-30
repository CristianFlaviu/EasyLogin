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
    docker compose -f docker-compose.yml -f docker-compose.override.yml up --build -d
    goto end
)

if "%ACTION%"=="down" (
    docker compose down
    goto end
)

if "%ACTION%"=="logs" (
    docker compose logs -f
    goto end
)

if "%ACTION%"=="restart" (
    docker compose down
    docker compose -f docker-compose.yml -f docker-compose.override.yml up --build -d
    goto end
)

if "%ACTION%"=="clean" (
    docker compose down -v
    goto end
)

echo Usage: run.bat [up^|down^|logs^|restart^|clean]
echo   up       - Build and start all services (default)
echo   down     - Stop all services
echo   logs     - Tail logs for all services
echo   restart  - Stop, rebuild, and start all services
echo   clean    - Stop all services and remove volumes

:end
endlocal
