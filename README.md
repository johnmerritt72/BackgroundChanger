# Background Changer

A simple .NET console application that changes the desktop background image for a specified monitor. The image will be scaled to fit the monitor while maintaining its aspect ratio.

## Prerequisites

- .NET 8.0 SDK or later
- Windows operating system

## Installation

1. Install .NET SDK from https://dotnet.microsoft.com/download
2. Clone or download this project
3. Navigate to the project directory in PowerShell/Command Prompt
4. Build the project:
   ```
   dotnet build
   ```

## Usage

### Basic Usage
```
BackgroundChanger.exe <image_path>
```

### Specify Monitor
```
BackgroundChanger.exe <image_path> <monitor_index>
```

### Examples
```
# Set background on primary monitor
BackgroundChanger.exe "C:\Pictures\wallpaper.jpg"

# Set background on second monitor (index 1)
BackgroundChanger.exe "C:\Pictures\wallpaper.png" 1

# List available monitors
BackgroundChanger.exe
```

## Parameters

- `image_path`: Path to the image file (.png, .jpg, or .jpeg)
- `monitor_index`: Optional monitor index (0-based, default is 0 for primary monitor)

## Features

- **True Per-Monitor Support**: Uses Windows modern API (`IDesktopWallpaper`) to set wallpaper independently on each monitor
- **Automatic Fallback**: Falls back to combined wallpaper approach if modern API is unavailable
- **Aspect Ratio Preservation**: Images are scaled to fit without distortion
- **Format Support**: Handles PNG and JPEG files
- **Centering**: Images are centered on the target monitor
- **High Quality Scaling**: Uses high-quality interpolation for image scaling
- **Multi-Monitor Detection**: Automatically detects all available monitors including virtual displays

## Building for Distribution

To create a self-contained executable:

```
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be located in `bin\Release\net8.0\win-x64\publish\`

## Notes

- **Modern API First**: The application first attempts to use Windows' modern `IDesktopWallpaper` interface for true per-monitor wallpaper support
- **Automatic Fallback**: If the modern API fails, it falls back to creating a combined wallpaper image spanning all monitors
- **True Independence**: Each monitor maintains its own wallpaper independently - changing one doesn't affect others
- Temporary files are created during processing and automatically cleaned up  
- The image is scaled to fit the target monitor without distortion
- Only works on Windows systems (Windows 8 and later recommended for modern API)
- Works with complex monitor arrangements (stacked, side-by-side, mixed orientations)
