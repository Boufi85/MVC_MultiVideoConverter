using Avalonia.Controls.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVC.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoLibrary;
using System.Configuration;
using System.Globalization;
using Avalonia.Input;
using Avalonia.Controls;
using Video = MVC.Models.Video;

namespace MVC.ViewModels
{
    partial class DownloadViewModel : ViewModelBase
    {
        private Video _video;

        [ObservableProperty]
        private string _videoName;

        [ObservableProperty]
        private int _downloadProgress;

        [ObservableProperty]
        private bool _isDownloadFinished;


        // Constructor that initializes the ViewModel with a Video
        public DownloadViewModel(Video video, string videoSnippetName)
        {
            _video = video;
            VideoName = videoSnippetName;
            DownloadProgress = video.VideoDownloadCurrentProgress;
            IsDownloadFinished = video.DownloadEnded;
            _video.PropertyChanged += OnVideoItemPropertyChanged;
        }

        
        private void OnVideoItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update the ViewModel properties when the VideoItem properties change
            if (e.PropertyName == nameof(Video.VideoDownloadCurrentProgress))
            {
                DownloadProgress = _video.VideoDownloadCurrentProgress;
                if (DownloadProgress == 100)
                {
                    this.IsDownloadFinished = true;
                }
            }
        }
    }
}

