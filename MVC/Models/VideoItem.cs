using System;
using System.Collections.Generic;
using System.Text;
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace MVC.Models
{
    public class VideoItem : YouTube
    {
        public required string VideoPath { get; set; }
        public required string VideoURL { get; set; }
        public string VideoName { get; set; }

        public YouTubeVideo vid { get; set; }

        public VideoItem(string url, string path, string? name)
        {
            VideoURL = url;
            VideoPath = path;
            vid = YouTube.Default.GetVideo(url);
            if (string.IsNullOrWhiteSpace(name))
            {
                VideoName = vid.Title;
            }
            else
            {
                VideoName = name;
            }

        }
    }
}
