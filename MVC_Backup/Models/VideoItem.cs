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

namespace MVC.Models
{
    [INotifyPropertyChanged]
    public partial class VideoItem : YouTube
    {
        public string VideoPath { get; set; }
        public string VideoURL { get; set; }
        public string VideoName { get; set; }

        public int VideoCropStart { get; set; }

        public int VideoCropEnd { get; set; }

        public int VideoLength { get; set; }

        [ObservableProperty]
        private int _videoDownloadCurrentProgress;


        public YouTubeVideo Video { get; set; }

        public VideoItem(string url)
        {
            VideoURL = url;
        }


        public async Task InitializeAsync()
        {
            try
            {
                Video = await YouTube.Default.GetVideoAsync(VideoURL);
                VideoLength = Video.Info.LengthSeconds;
                var safeVideoName = Regex.Replace(Video.Title, @"[\\\/:*?""<>|]", "_");
                VideoName = safeVideoName;
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        public async Task ConvertFromYoutubeToMp3(string youtubeUrl, string outputDirectory, string outputFileName)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl)) throw new ArgumentException("youtubeUrl");
            if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("outputDirectory");
            if (string.IsNullOrWhiteSpace(outputFileName)) throw new ArgumentException("outputFileName");

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Use VideoLibrary to get the video stream (as binary) and save to a temp file
            var tempVideoPath = Path.Combine(outputDirectory, Guid.NewGuid().ToString() + Path.GetExtension(this.Video.FullName));
            await File.WriteAllBytesAsync(tempVideoPath, this.Video.GetBytes());

            var outputMp3Path = Path.Combine(outputDirectory, outputFileName + ".mp3");


            try
            {
                var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(tempVideoPath, outputMp3Path);
                if (VideoCropStart>0 || VideoCropEnd > 0)
                {
                    conversion.AddParameter($"-ss {VideoCropStart} -t {VideoCropEnd - VideoCropStart}");
                }

                conversion.OnProgress += (sender, args) =>
                {
                    try
                    {
                        var expectedTotal = (VideoCropStart > 0 || VideoCropEnd > 0)
                            ? TimeSpan.FromSeconds(VideoCropEnd - VideoCropStart)
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
                    }
                    catch
                    {
                    }
                };

                await conversion.Start();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                this.VideoDownloadCurrentProgress = 100;
                try { File.Delete(tempVideoPath); } catch { }
            }
        }
    }
}
