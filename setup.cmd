@echo off
rem =====================================================================
rem  Store Platform Capstone - one-time local setup (Windows)
rem
rem  Run this once after cloning, from any folder:
rem      setup.cmd
rem  Then start the platform:
rem      docker compose up --build
rem
rem  What it does:
rem    1. Checks that Docker Desktop and Docker Compose v2 are available
rem    2. Creates .env from .env.example (never overwrites an existing .env)
rem    3. Reports the installed .NET SDK (only needed outside Docker)
rem    4. Validates docker-compose.yml against your .env when it is present
rem =====================================================================
setlocal EnableExtensions
cd /d "%~dp0"

echo.
echo ==========================================================
echo   Store Platform Capstone - local environment setup
echo ==========================================================
echo.

rem ---------------------------------------------------------------------
echo [1/4] Checking Docker...
where docker >nul 2>nul
if errorlevel 1 goto :err_no_docker
docker compose version >nul 2>nul
if errorlevel 1 goto :err_no_compose
docker info >nul 2>nul
if errorlevel 1 goto :err_docker_stopped
echo       OK - Docker and Docker Compose are available.
echo.

rem ---------------------------------------------------------------------
echo [2/4] Preparing .env...
if exist ".env" goto :env_exists
if not exist ".env.example" goto :err_no_env_example
copy /Y ".env.example" ".env" >nul
if errorlevel 1 goto :err_env_copy
echo       OK - created .env from .env.example.
goto :env_done
:env_exists
echo       OK - .env already exists, left unchanged.
:env_done
echo.

rem ---------------------------------------------------------------------
echo [3/4] Checking the .NET SDK...
set "DOTNET_VERSION="
where dotnet >nul 2>nul
if errorlevel 1 goto :dotnet_missing
for /f "delims=" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VERSION=%%v"
if not defined DOTNET_VERSION goto :dotnet_missing
echo       OK - .NET SDK %DOTNET_VERSION% found.
goto :dotnet_done
:dotnet_missing
echo       NOTE - .NET SDK not found. Docker builds everything, so this is
echo              fine. Install the .NET 10 SDK only to build outside Docker.
:dotnet_done
echo.

rem ---------------------------------------------------------------------
echo [4/4] Validating the Compose configuration...
if not exist "docker-compose.yml" goto :compose_missing
docker compose config --quiet
if errorlevel 1 goto :err_compose_invalid
echo       OK - docker-compose.yml is valid with your .env.
goto :done
:compose_missing
echo       NOTE - docker-compose.yml is not in this checkout, skipped.

:done
echo.
echo ==========================================================
echo   Setup complete.
echo ==========================================================
echo.
echo   Start everything:   docker compose up --build
echo   Stop everything:    docker compose down
echo.
echo   Default addresses, change the ports in .env if they are taken:
echo     Storefront        http://localhost:5100
echo     Gateway           http://localhost:5000
echo     RabbitMQ console  http://localhost:15672   guest / guest
echo.
endlocal
exit /b 0

rem =====================================================================
rem  Error exits
rem =====================================================================
:err_no_docker
echo.
echo [ERROR] Docker was not found on PATH.
echo         Install Docker Desktop from https://www.docker.com/products/docker-desktop/
echo         then open a new terminal and run setup.cmd again.
goto :fail

:err_no_compose
echo.
echo [ERROR] "docker compose" is not available.
echo         Update Docker Desktop so that Docker Compose v2 is included.
goto :fail

:err_docker_stopped
echo.
echo [ERROR] Docker is installed but the engine is not running.
echo         Start Docker Desktop, wait until it reports "Engine running",
echo         then run setup.cmd again.
goto :fail

:err_no_env_example
echo.
echo [ERROR] .env.example is missing from "%CD%".
echo         Restore it with:  git checkout -- .env.example
goto :fail

:err_env_copy
echo.
echo [ERROR] Could not create .env from .env.example.
echo         Check that this folder is writable and try again.
goto :fail

:err_compose_invalid
echo.
echo [ERROR] docker-compose.yml did not validate with the values in .env.
echo         Compare your .env with .env.example for missing or misspelled keys.
goto :fail

:fail
echo.
endlocal
exit /b 1
