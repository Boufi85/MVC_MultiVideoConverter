using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoLibrary;
using Xabe.FFmpeg;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Linq;

namespace MVC.Models
{
    [INotifyPropertyChanged]
    public partial class VideoItem : YouTube
    {
        public string? VideoPath { get; set; }
        public string? VideoURL { get; set; }

        [ObservableProperty]
        public string? _videoName;

        public int? VideoCropStart { get; set; }

        public int? VideoCropEnd { get; set; }

        public int? VideoLength { get; set; }

        [ObservableProperty]
        private int _videoDownloadCurrentProgress;

        [ObservableProperty]
        private bool _downloadEnded = false;

        public YouTubeVideo Video { get; set; }

        public VideoItem(string url)
        {
            VideoURL = url;
        }

        public VideoItem(VideoItem srcItem)
        {
            Video = srcItem.Video;
            VideoURL = srcItem.VideoURL;
            VideoName = srcItem.VideoName;
            VideoPath = srcItem.VideoPath;
            VideoCropStart = srcItem.VideoCropStart;
            VideoCropEnd = srcItem.VideoCropEnd;
            VideoLength = srcItem.VideoLength;
            DownloadEnded = srcItem.DownloadEnded;

        }

        

        public async Task<string> InitializeAsync()
        {
            try
            {
                VideoName= "Chargement...";
                Video = await YouTube.Default.GetVideoAsync(VideoURL);
                VideoLength = Video.Info.LengthSeconds;
                var safeVideoName = Regex.Replace(Video.Title, @"[\\\/:*?""<>|#]", "_");
                VideoName = safeVideoName;
                return VideoName;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("URL non valide");
            }
        }

       

        public async Task ConvertFromYoutubeToMp3()
        {
            Console.WriteLine("ConvertFromYoutubeToMp3 : starting function");
            this.DownloadEnded = false;
            // Ensure output directory exists
            Directory.CreateDirectory(VideoPath);

            // Use VideoLibrary to get the video stream (as binary) and save to a temp file
            var tempVideoPath = Path.Combine(VideoPath, Guid.NewGuid().ToString() + Path.GetExtension(this.Video.FullName));
            Console.WriteLine("ConvertFromYoutubeToMp3 :writing temporary vidéo to file");
            await File.WriteAllBytesAsync(tempVideoPath, this.Video.GetBytes());

            var outputMp3Path = Path.Combine(VideoPath, VideoName + ".mp3");

            Console.WriteLine("ConvertFromYoutubeToMp3 : temp write complete");
            try
            {
                var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(tempVideoPath, outputMp3Path);
                                
                conversion.AddParameter($"-ss {VideoCropStart} -t {VideoCropEnd - VideoCropStart}");
                conversion.OnProgress += (sender, args) =>
                {
                    try
                    {
                        var expectedTotal = (VideoCropStart > 0 || VideoCropEnd > 0)
                            ? TimeSpan.FromSeconds((double?)(VideoCropEnd - VideoCropStart)?? 0)
                            : args.TotalLength;

                        if (expectedTotal.TotalSeconds <= 0)
                            return;
                        var percent = (int)Math.Min(
                            100,
                            Math.Max(
                                0,
                                Math.Round(args.Duration.TotalSeconds / expectedTotal.TotalSeconds * 100)
                            )
                        );

                        this.VideoDownloadCurrentProgress = percent;
                        Console.WriteLine("ConvertFromYoutubeToMp3 : percent updated");
                    }
                    catch
                    {
                        Console.WriteLine("ConvertFromYoutubeToMp3: error while converting");
                        throw;
                    }
                };

                await conversion.Start();
                Console.WriteLine("ConvertFromYoutubeToMp3 :Conversion complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConvertFromYoutubeToMp3: error while converting 2");
                throw;
            }
                
            this.VideoDownloadCurrentProgress = 100;
            Console.WriteLine("ConvertFromYoutubeToMp3: es finito");
            try 
            {
                Console.WriteLine("ConvertFromYoutubeToMp3: attempt to delete temp file");
                await DeleteTempVideoFile(tempVideoPath); 
            } 
            catch (Exception) 
            { 
                Console.WriteLine("ConvertFromYoutubeToMp3: failed to delete temp video file, giving up");
                throw; 
            }
        }
        private static async Task DeleteTempVideoFile(string path, int attempts = 10, int delayMs = 300)
        {
            if (string.IsNullOrWhiteSpace(path)|| !File.Exists(path))
            {

                Console.WriteLine("DeleteTempVideoFile: nothing to delete");
                return;
            }
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    Console.WriteLine($"DeleteTempVideoFile: Attempting to delete {path}");
                    File.Delete(path);

                    if (!File.Exists(path))
                    {
                        Console.WriteLine("DeleteTempVideoFile: file deleted");
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("DeleteTempVideoFile: error while trying to delete");
                }
                await Task.Delay(delayMs);
            }
        }
    }
}
