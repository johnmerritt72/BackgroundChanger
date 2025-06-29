using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BackgroundChanger
{
    class Program
    {
        // Windows API constants
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        // Import Windows API function to set wallpaper
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        // Import Windows API functions to hide/show console window
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        // Import Windows API functions for monitor enumeration
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hmon, ref MonitorInfoEx lpmi);

        // Delegate for monitor enumeration
        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        // Structures for monitor information
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx
        {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        static bool _silentMode = false;

        static void WriteOutput(string message)
        {
            if (!_silentMode)
            {
                Console.WriteLine(message);
            }
        }

        static void HideConsoleWindow()
        {
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }
        }

        static void ShowConsoleWindow()
        {
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_SHOW);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                // Check for silent/hidden mode
                _silentMode = args.Contains("--silent") || args.Contains("--hide") || args.Contains("-s");
                
                if (_silentMode)
                {
                    HideConsoleWindow();
                    // Remove the silent flag from args for further processing
                    args = args.Where(arg => arg != "--silent" && arg != "--hide" && arg != "-s").ToArray();
                }

                if (args.Length < 1)
                {
                    WriteOutput("Usage: BackgroundChanger <image_path> [monitor_index] [--silent]");
                    WriteOutput("  image_path: Path to the .png or .jpg image file");
                    WriteOutput("  monitor_index: Optional monitor index (0-based, default: 0 for primary monitor)");
                    WriteOutput("  --silent, --hide, -s: Hide the console window during execution");
                    WriteOutput("");
                    WriteOutput("Available monitors:");
                    ListMonitors();
                    return;
                }

                string imagePath = args[0];
                int monitorIndex = args.Length > 1 ? int.Parse(args[1]) : 0;

                if (!File.Exists(imagePath))
                {
                    WriteOutput($"Error: Image file '{imagePath}' not found.");
                    return;
                }

                string extension = Path.GetExtension(imagePath).ToLower();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    WriteOutput("Error: Only .png and .jpg/.jpeg files are supported.");
                    return;
                }

                WriteOutput($"Setting background image: {imagePath}");
                WriteOutput($"Target monitor index: {monitorIndex}");

                // Get monitor information
                var monitors = GetMonitors();
                if (monitorIndex >= monitors.Count)
                {
                    WriteOutput($"Error: Monitor index {monitorIndex} not found. Available monitors: 0-{monitors.Count - 1}");
                    return;
                }

                var targetMonitor = monitors[monitorIndex];
                WriteOutput($"Target monitor: {targetMonitor.Width}x{targetMonitor.Height} at ({targetMonitor.X}, {targetMonitor.Y})");

                // Try modern API first
                WriteOutput("Attempting to use modern per-monitor wallpaper API...");
                if (TrySetWallpaperModern(imagePath, monitorIndex))
                {
                    WriteOutput("Successfully set wallpaper using modern API!");
                }
                else
                {
                    WriteOutput("Modern API failed, falling back to combined wallpaper approach...");
                    
                    // Process and set the background for all monitors
                    string processedImagePath = ProcessImageForAllMonitors(imagePath, monitors, monitorIndex);
                    SetWallpaper(processedImagePath);

                    WriteOutput("Background image set successfully using combined wallpaper!");
                    
                    // Clean up temporary file if created
                    if (processedImagePath != imagePath && File.Exists(processedImagePath))
                    {
                        File.Delete(processedImagePath);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void ListMonitors()
        {
            var monitors = GetMonitors();
            WriteOutput($"Total monitors detected: {monitors.Count}");
            for (int i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                string primary = monitor.IsPrimary ? " (Primary)" : "";
                WriteOutput($"  {i}: {monitor.Width}x{monitor.Height} at ({monitor.X}, {monitor.Y}){primary}");
            }
            
            // Show combined desktop bounds
            if (monitors.Count > 0)
            {
                int minX = monitors.Min(m => m.X);
                int minY = monitors.Min(m => m.Y);
                int maxX = monitors.Max(m => m.X + m.Width);
                int maxY = monitors.Max(m => m.Y + m.Height);
                WriteOutput($"Combined desktop: {maxX - minX}x{maxY - minY} (from {minX},{minY} to {maxX},{maxY})");
            }
        }

        static List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();
            
            foreach (Screen screen in Screen.AllScreens)
            {
                monitors.Add(new MonitorInfo
                {
                    X = screen.Bounds.X,
                    Y = screen.Bounds.Y,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    IsPrimary = screen.Primary
                });
            }

            return monitors;
        }

        static string ProcessImageForAllMonitors(string imagePath, List<MonitorInfo> monitors, int targetMonitorIndex)
        {
            using (var originalImage = Image.FromFile(imagePath))
            {
                // Calculate the bounds of all monitors combined
                int minX = monitors.Min(m => m.X);
                int minY = monitors.Min(m => m.Y);
                int maxX = monitors.Max(m => m.X + m.Width);
                int maxY = monitors.Max(m => m.Y + m.Height);
                
                int totalWidth = maxX - minX;
                int totalHeight = maxY - minY;

                WriteOutput($"Creating combined wallpaper: {totalWidth}x{totalHeight}");

                // Create a bitmap that spans all monitors
                using (var combinedBitmap = new Bitmap(totalWidth, totalHeight))
                {
                    using (var graphics = Graphics.FromImage(combinedBitmap))
                    {
                        // Set high quality scaling
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                        // Fill the entire combined area with black
                        graphics.Clear(Color.Black);

                        // Process each monitor
                        for (int i = 0; i < monitors.Count; i++)
                        {
                            var monitor = monitors[i];
                            
                            // Calculate the position of this monitor in the combined bitmap
                            int monitorX = monitor.X - minX;
                            int monitorY = monitor.Y - minY;

                            if (i == targetMonitorIndex)
                            {
                                // This is the target monitor - draw the scaled image
                                double scaleWidth = (double)monitor.Width / originalImage.Width;
                                double scaleHeight = (double)monitor.Height / originalImage.Height;
                                double scale = Math.Min(scaleWidth, scaleHeight);

                                int newWidth = (int)(originalImage.Width * scale);
                                int newHeight = (int)(originalImage.Height * scale);

                                // Calculate position to center the image within this monitor
                                int imageX = monitorX + (monitor.Width - newWidth) / 2;
                                int imageY = monitorY + (monitor.Height - newHeight) / 2;

                                // Draw the scaled image on the target monitor
                                graphics.DrawImage(originalImage, imageX, imageY, newWidth, newHeight);
                                
                                WriteOutput($"Image drawn on monitor {i} at ({imageX}, {imageY}) with size {newWidth}x{newHeight}");
                            }
                            else
                            {
                                // This is not the target monitor - fill with black (already done by Clear, but we could add a pattern here)
                                WriteOutput($"Monitor {i} filled with black background");
                            }
                        }
                    }

                    // Save the combined wallpaper
                    string tempPath = Path.Combine(Path.GetTempPath(), $"combined_wallpaper_{Guid.NewGuid()}.bmp");
                    combinedBitmap.Save(tempPath, ImageFormat.Bmp);
                    
                    // Also save a copy with a predictable name for verification
                    string verificationPath = Path.Combine(Path.GetTempPath(), "BackgroundChanger_LastWallpaper.bmp");
                    if (File.Exists(verificationPath))
                        File.Delete(verificationPath);
                    combinedBitmap.Save(verificationPath, ImageFormat.Bmp);
                    
                    WriteOutput($"Combined wallpaper saved to: {tempPath}");
                    WriteOutput($"Verification copy saved to: {verificationPath}");
                    return tempPath;
                }
            }
        }

        static void SetWallpaper(string imagePath)
        {
            try
            {
                // Clear any existing wallpaper first
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                System.Threading.Thread.Sleep(100); // Brief pause
                
                // Set wallpaper style to ensure it's displayed exactly as we created it
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        // Try "Fill" mode instead of "Tile" - this should stretch to fit exactly
                        key.SetValue("WallpaperStyle", "10", RegistryValueKind.String);
                        key.SetValue("TileWallpaper", "0", RegistryValueKind.String);
                        WriteOutput("Set wallpaper style to Fill mode");
                    }
                }

                // Set the wallpaper using Windows API
                int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                
                if (result == 0)
                {
                    throw new Exception("Failed to set wallpaper. Make sure the image file is accessible and valid.");
                }
            
                WriteOutput("Wallpaper set successfully via Windows API");
                
                // Force a desktop refresh
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
            catch (Exception ex)
            {
                WriteOutput($"Error setting wallpaper: {ex.Message}");
                throw;
            }
        }

        // COM interfaces for modern Windows wallpaper API
        [ComImport]
        [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDesktopWallpaper
        {
            void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
            void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, out System.IntPtr wallpaper);
            void GetMonitorDevicePathAt(uint monitorIndex, out System.IntPtr monitorID);
            void GetMonitorDevicePathCount(out uint count);
            void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID, out Rect displayRect);
            void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
            void GetBackgroundColor(out uint color);
            void SetPosition([MarshalAs(UnmanagedType.I4)] int position);
            void GetPosition(out int position);
            void SetSlideshow(IntPtr items);
            void GetSlideshow(out IntPtr items);
            void SetSlideshowOptions(int options, uint slideshowTick);
            void GetSlideshowOptions(out int options, out uint slideshowTick);
            void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, int direction);
            void GetStatus(out int state);
            void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
        }

        [ComImport]
        [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
        class DesktopWallpaperClass
        {
        }

        static bool TrySetWallpaperModern(string imagePath, int monitorIndex)
        {
            try
            {
                var wallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
                
                // Get monitor count
                uint monitorCount;
                wallpaper.GetMonitorDevicePathCount(out monitorCount);
                WriteOutput($"Modern API detected {monitorCount} monitors");
                
                if (monitorIndex >= monitorCount)
                {
                    WriteOutput($"Monitor index {monitorIndex} exceeds available monitors ({monitorCount})");
                    return false;
                }
                
                // Get the monitor device path
                IntPtr monitorIdPtr;
                wallpaper.GetMonitorDevicePathAt((uint)monitorIndex, out monitorIdPtr);
                string? monitorId = Marshal.PtrToStringUni(monitorIdPtr);
                
                if (monitorId == null)
                {
                    WriteOutput("Failed to get monitor device path");
                    return false;
                }
                
                WriteOutput($"Setting wallpaper on monitor: {monitorId}");
                
                // Set wallpaper for specific monitor
                wallpaper.SetWallpaper(monitorId, imagePath);
                WriteOutput("Wallpaper set using modern API");
                
                Marshal.FreeCoTaskMem(monitorIdPtr);
                return true;
            }
            catch (Exception ex)
            {
                WriteOutput($"Modern API failed: {ex.Message}");
                return false;
            }
        }

        public class MonitorInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsPrimary { get; set; }
        }
    }
}
