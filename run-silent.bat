@echo off
if "%~1"=="" (
    echo Usage: run-silent.bat ^<image_path^> [monitor_index]
    echo.
    echo This batch file runs BackgroundChanger with the console window hidden.
    echo.
    echo Examples:
    echo   run-silent.bat "C:\Pictures\wallpaper.jpg"
    echo   run-silent.bat "C:\Pictures\wallpaper.png" 1
    echo.
    exit /b
)

dotnet run "%~1" %2 --silent
