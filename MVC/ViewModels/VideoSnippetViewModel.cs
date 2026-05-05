using CommunityToolkit.Mvvm.ComponentModel;
using MVC.Models;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MVC.Views;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace MVC.ViewModels
{
    public partial class VideoSnippetViewModel : ViewModelBase
    {


        public VideoSnippet Item { get; private set; }

        [ObservableProperty]
        private string _videoSnippetName;
        [ObservableProperty]
        private string _videoSnippetCropStart;
        [ObservableProperty]
        private string _videoSnippetCropEnd;


        partial void OnVideoSnippetNameChanged(string value)
        {
            Item.VideoName = value;
        }

        public VideoSnippetViewModel(string tempName, int length)
        {
            Item = new VideoSnippet(tempName, length);
            VideoSnippetName = tempName;
            VideoSnippetCropStart = TimeSpan.FromSeconds(0).ToString("hh\\:mm\\:ss");
            VideoSnippetCropEnd = TimeSpan.FromSeconds(length).ToString("hh\\:mm\\:ss");
            

        }

        public VideoSnippetViewModel(string tempName, int lastItemCropEnd, int length)
        {
            Item = new VideoSnippet(tempName, lastItemCropEnd, length);
            VideoSnippetName = tempName;
            VideoSnippetCropStart = TimeSpan.FromSeconds(lastItemCropEnd).ToString("hh\\:mm\\:ss");
            VideoSnippetCropEnd = TimeSpan.FromSeconds(length).ToString("hh\\:mm\\:ss");
        }
        
        partial void OnVideoSnippetCropStartChanged(string value)
        {
            if (!IsValidTimeSpan(value))
                return;
            Console.WriteLine("timestamp updated");
            Item.VideoSnippetCropStart = (int)TimeSpan.Parse(value).TotalSeconds;
        }
        partial void OnVideoSnippetCropEndChanged(string value)
        {
            if (!IsValidTimeSpan(value))
                return;
            Item.VideoSnippetCropEnd = (int)TimeSpan.Parse(value).TotalSeconds;
        }


        // This method validates if the provided time span string is in a correct format
        public bool IsValidTimeSpan(string timeSpanStr)
        {
            return TimeSpan.TryParse(timeSpanStr, out _);
        }
    }
}
