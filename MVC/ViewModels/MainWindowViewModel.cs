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
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<VideoItemViewModel> VideoItems { get; } = new ObservableCollection<VideoItemViewModel>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoItemCommand))]
        private string _newVideoURL;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadVideoItemCommand))]
        private string _newVideoPath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadVideoItemCommand))]
        private string _newVideoName;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadVideoItemCommand))]
        private int _newVideoLength;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadVideoItemCommand))]
        private string _newVideoBeginning;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadVideoItemCommand))]
        private string _newVideoEnd;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoItemCommand))]
        private string _newVideoCropMask = "00:00";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoItemCommand))]
        private VideoItem _currentVideoItem;

        private bool CanLoadVideoItem() => IsValidURL(NewVideoURL);

        private bool CanDownloadVideoItem() => CurrentVideoItem != null && !string.IsNullOrWhiteSpace(NewVideoPath) && !string.IsNullOrWhiteSpace(NewVideoName);

        private bool IsValidURL(string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && (url.Contains("www.youtube.com") || url.Contains("youtu.be")))
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        private bool IsValidTimeSpan(string timeSpanStr)
        {
            return TimeSpan.TryParse(timeSpanStr, out _);
        }

        [RelayCommand(CanExecute = nameof(CanDownloadVideoItem))]
        private async Task DownloadVideoItem()
        {
            CurrentVideoItem.VideoPath = NewVideoPath;
            CurrentVideoItem.VideoName = NewVideoName;
            if(IsValidTimeSpan(NewVideoBeginning) && IsValidTimeSpan(NewVideoEnd))
                if(CurrentVideoItem.VideoLength>3600)
                {
                    CurrentVideoItem.VideoCropStart = (int)TimeSpan.ParseExact(NewVideoBeginning, "hh':'mm':'ss", null).TotalSeconds;
                    CurrentVideoItem.VideoCropEnd = (int)TimeSpan.ParseExact(NewVideoEnd, "hh':'mm':'ss", null).TotalSeconds;
                }
                else
                {
                    CurrentVideoItem.VideoCropStart = (int)TimeSpan.ParseExact(NewVideoBeginning, "mm':'ss", null).TotalSeconds;
                    CurrentVideoItem.VideoCropEnd = (int)TimeSpan.ParseExact(NewVideoEnd, "mm':'ss", null).TotalSeconds;
                }

            VideoItems.Add(new VideoItemViewModel(CurrentVideoItem));

            Task.Run(()=>CurrentVideoItem.ConvertFromYoutubeToMp3(CurrentVideoItem.VideoURL, CurrentVideoItem.VideoPath, CurrentVideoItem.VideoName));

            NewVideoURL = null;
            NewVideoPath = null;
            NewVideoName = null;
            NewVideoBeginning = "";
            NewVideoEnd = "";
            NewVideoCropMask = "00:00";
            CurrentVideoItem = null;
        }

        [RelayCommand(CanExecute = nameof(CanLoadVideoItem))]
        private async Task LoadVideoItem()
        {
            try
            {
                var youTube = YouTube.Default;
                NewVideoName = "Chargement...";
                CurrentVideoItem = new VideoItem(NewVideoURL);
                await Task.Run(()=>CurrentVideoItem.InitializeAsync());
                NewVideoName = CurrentVideoItem.VideoName;
                NewVideoLength = CurrentVideoItem.Video.Info.LengthSeconds;
                if (NewVideoLength < 3600)
                { 
                    NewVideoCropMask = "00:00";
                }
                else
                {
                    NewVideoCropMask = "00:00:00";
                }
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        private void RemoveItem(VideoItemViewModel item)
        {
            if (item != null)
            {
                VideoItems.Remove(item);
            }
        }
    }
}
