using CommunityToolkit.Mvvm.ComponentModel;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.ViewModels
{
    public partial class VideoItemViewModel : ViewModelBase
    {
        public VideoItemViewModel() { }

        public VideoItemViewModel(VideoItem item)
        {
            VideoPath = item.VideoPath;
            VideoName = item.VideoName;
            VideoURL = item.VideoURL;
        }


        [ObservableProperty]
        private string _videoPath;
        [ObservableProperty]
        private string _videoURL;
        [ObservableProperty]
        private string? _videoName;

        
    }
}
