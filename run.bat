@echo off
if "%~1"=="" (
    echo Usage: run.bat ^<image_path^> [monitor_index] [--silent]
    echo.
    echo Examples:
    echo   run.bat "C:\Pictures\wallpaper.jpg"
    echo   run.bat "C:\Pictures\wallpaper.png" 1
    echo   run.bat "C:\Pictures\wallpaper.jpg" 0 --silent
    echo.
    echo Options:
    echo   --silent, --hide, -s: Hide the console window during execution
    echo.
    echo Note: Uses Windows modern API for true per-monitor wallpaper support
    echo.
    dotnet run
) else (
    dotnet run %*
)
