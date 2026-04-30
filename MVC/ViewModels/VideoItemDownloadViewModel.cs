using Avalonia.Controls.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaToolkit;
using MediaToolkit.Model;
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

namespace MVC.ViewModels
{
    public partial class VideoItemDownloadViewModel : ViewModelBase
    {
        private VideoItem _videoItem;

        [ObservableProperty]
        private string _videoName;

        [ObservableProperty]
        private int _downloadProgress;

        [ObservableProperty]
        private bool _isDownloadFinished;

        // Constructor that initializes the ViewModel with a VideoItem
        public VideoItemDownloadViewModel(VideoItem videoItem, string videoSnippetName)
        {
            _videoItem = videoItem;
            VideoName = videoSnippetName;
            DownloadProgress = videoItem.VideoDownloadCurrentProgress;
            IsDownloadFinished = videoItem.DownloadEnded;
            _videoItem.PropertyChanged += OnVideoItemPropertyChanged;
        }

        
        private void OnVideoItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update the ViewModel properties when the VideoItem properties change
            if (e.PropertyName == nameof(VideoItem.VideoDownloadCurrentProgress))
            {
                DownloadProgress = _videoItem.VideoDownloadCurrentProgress;
                if (DownloadProgress == 100)
                {
                    this.IsDownloadFinished = true;
                }
            }
        }
    }
}

