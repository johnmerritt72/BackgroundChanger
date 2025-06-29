@echo off
if "%~1"=="" (
    echo Usage: run.bat ^<image_path^> [monitor_index]
    echo.
    echo Examples:
    echo   run.bat "C:\Pictures\wallpaper.jpg"
    echo   run.bat "C:\Pictures\wallpaper.png" 1
    echo.
    echo Note: Uses Windows modern API for true per-monitor wallpaper support
    echo.
    dotnet run
) else (
    dotnet run "%~1" %2
)
