using CommunityToolkit.Mvvm.ComponentModel;
using MVC.Models;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace MVC.ViewModels
{
    public partial class VideoItemViewModel : ViewModelBase
    {

        public ObservableCollection<VideoItem> VideoItems { get; } = new ObservableCollection<VideoItem>();

        private VideoItem _item;
        [NotifyCanExecuteChangedFor(nameof(LoadVideoItemCommand))]
        [ObservableProperty]
        private string? _videoURL;
        [ObservableProperty]
        private string? _videoName;
        [NotifyCanExecuteChangedFor(nameof(ConvertVideoItemCommand))]
        [ObservableProperty]
        private string? _videoPath;
        [ObservableProperty]
        private TimeSpan _videoCropStart;
        [ObservableProperty]
        private TimeSpan _videoCropEnd;
        [ObservableProperty]
        private int _videoDownloadCurrentProgress;
        [ObservableProperty]
        private string _videoCropMask = "00:00";
        [ObservableProperty]
        private bool _videoItemsPopulated = false;

        partial void OnVideoURLChanged(string? value)
        {
            _item.VideoURL = value;
        }
        partial void OnVideoNameChanged(string? value)
        {
            _item.VideoName = value;
        }
        partial void OnVideoPathChanged(string? value)
        {
            _item.VideoPath = value;
        }
        partial void OnVideoCropStartChanged(TimeSpan value)
        {
            _item.VideoCropStart = (int)value.TotalSeconds;
        }
        partial void OnVideoCropEndChanged(TimeSpan value)
        {
            _item.VideoCropEnd = (int)value.TotalSeconds;
        }
        partial void OnVideoDownloadCurrentProgressChanged(int value)
        {
            _item.VideoDownloadCurrentProgress = value;
        }

        [RelayCommand(CanExecute = nameof(IsValidURL))]
        private async Task LoadVideoItem()
        {
            await _item.InitializeAsync();
            _item.PropertyChanged += OnVideoItemPropertyChanged;
            VideoName = _item.VideoName;
            if (_item.VideoLength < 3600)
            {
                VideoCropMask = "00:00";
            }
            else
            {
                VideoCropMask = "00:00:00";
            }

        }

        private bool IsValidURL() => !string.IsNullOrWhiteSpace(VideoURL) && (VideoURL.Contains("www.youtube.com") || VideoURL.Contains("youtu.be"));
        public VideoItemViewModel()
        {
            _item = new VideoItem(string.Empty);
        }

        private bool CanConvertVideoItem()
        {
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanConvertVideoItem))]
        private async Task ConvertVideoItem()
        {
            var newItem = new VideoItem(_item);
            VideoItems.Add(newItem);
            VideoItemsPopulated = true;
            Task.Run(() => newItem.ConvertFromYoutubeToMp3());
            
            ClearCurrentVideoItem();
        }

        private void OnVideoItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is VideoItem item)
            {
                if (item.VideoDownloadCurrentProgress!= VideoDownloadCurrentProgress)
                {
                    VideoDownloadCurrentProgress = item.VideoDownloadCurrentProgress;
                }
                if (item.VideoName != VideoName)
                {
                    VideoName = item.VideoName;
                }
                    
            }
        }

        [RelayCommand]
        private void RemoveItem(VideoItem item)
        {
            if (item != null)
            {
                VideoItems.Remove(item);
                if (VideoItems.Count == 0)
                {
                    VideoItemsPopulated = false;
                }
            }
        }

        private void ClearCurrentVideoItem()
        {
            _item = new VideoItem(string.Empty);
            VideoURL = null;
            VideoName = null;
            VideoPath = null;
            VideoCropStart = TimeSpan.Zero;
            VideoCropEnd = TimeSpan.Zero;
            VideoDownloadCurrentProgress = 0;
            VideoCropMask = "00:00";

        }
        private bool IsValidTimeSpan(string timeSpanStr)
        {
            return TimeSpan.TryParse(timeSpanStr, out _);
        }
    }
}
