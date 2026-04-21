using CommunityToolkit.Mvvm.ComponentModel;
using MVC.Models;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.ViewModels
{
    public partial class VideoItemViewModel : ViewModelBase
    {
        private readonly VideoItem _item;

        public VideoItemViewModel(VideoItem item)
        {
            _item = item;
            VideoPath = item.VideoPath;
            VideoName = item.VideoName;
            VideoURL = item.VideoURL;
            VideoLength = item.VideoLength;
            _item.PropertyChanged += VideoItemPropertyChanged;
        }


        [ObservableProperty]
        private string _videoPath;
        [ObservableProperty]
        private string _videoURL;
        [ObservableProperty]
        private string? _videoName;
        [ObservableProperty]
        private int _videoLength;
        [ObservableProperty]
        private float _videoCurrentProgress;


        private void VideoItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is VideoItem item)
            {
                // PropertyChanged may be raised from a background thread (MediaToolkit engine callback).
                // Ensure we update view-model properties on the UI thread so bindings refresh correctly.
                Dispatcher.UIThread.Post(() =>
                {
                    VideoCurrentProgress = item.VideoDownloadCurrentProgress;
                });
            }
        }
    }
}
