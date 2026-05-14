using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.Models
{
    public partial class VideoSnippet : ObservableObject
    {
        
        public string VideoName { get; set; }
        public int VideoSnippetCropStart { get; set; }
        public int VideoSnippetCropEnd { get; set; }

        public VideoSnippet(string tempName, int lastItemCropEnd, int length)
        {
            VideoName = tempName;
            VideoSnippetCropStart = lastItemCropEnd;
            VideoSnippetCropEnd = length;
        }
        public VideoSnippet(string tempName, int length)
        {
            VideoName=tempName;
            VideoSnippetCropStart = 0;
            VideoSnippetCropEnd = length;
        }
    }
}
