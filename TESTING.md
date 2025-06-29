# Sample Test Images

You can test the BackgroundChanger application with these commands:

## Create a test image (using PowerShell)
```powershell
# Create a simple test image using .NET drawing
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap(800, 600)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([System.Drawing.Color]::Blue)
$font = New-Object System.Drawing.Font("Arial", 24)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.DrawString("Test Background", $font, $brush, 250, 280)
$bitmap.Save("$env:TEMP\test-background.png", [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()
Write-Host "Test image created at: $env:TEMP\test-background.png"
```

## Test the application
```powershell
# Test with the created image
dotnet run "$env:TEMP\test-background.png"

# Test with specific monitor
dotnet run "$env:TEMP\test-background.png" 1
```

## Sample usage with real images
```powershell
# Use an existing image
dotnet run "C:\Windows\Web\Wallpaper\Windows\img0.jpg"

# Set background on second monitor
dotnet run "C:\Users\YourUsername\Pictures\wallpaper.png" 1
```
