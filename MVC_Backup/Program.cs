using Avalonia;
using System;
using System.IO;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MVC
{
    internal sealed class Program
    {

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                var ffFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                var ffExe = Path.Combine(ffFolder, "ffmpeg.exe");
                if (!File.Exists(ffExe))
                {
                    FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffFolder).GetAwaiter().GetResult();
                }

                FFmpeg.SetExecutablesPath(ffFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to setup FFmpeg executables: {ex.Message}");
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
