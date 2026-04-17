using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVC.Models;
using System;
using System.Collections.ObjectModel;
using VideoLibrary;

namespace MVC.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<VideoItemViewModel> VideoItems { get; } = new ObservableCollection<VideoItemViewModel>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddVideoItemCommand))]
        private string _newVideoURL;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddVideoItemCommand))]
        private string _newVideoPath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddVideoItemCommand))]
        private string _newVideoName;

        private bool CanAddVideoItem() => IsValidURL(NewVideoURL,NewVideoPath);

        private bool IsValidURL(string url, string path)
        {
            if (!string.IsNullOrWhiteSpace(url) && (url.Contains("www.youtube.com") || url.Contains("youtu.be")) && !string.IsNullOrWhiteSpace(path))
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddVideoItem))]
        private void AddVideoItem()
        {
            var model = new VideoItem(NewVideoURL, NewVideoPath, NewVideoName)
            {
                VideoURL = NewVideoURL,
                VideoPath = NewVideoPath
            };


            VideoItems.Add(new VideoItemViewModel(model));

            NewVideoURL = null;
            NewVideoPath = null;
            NewVideoName = null;
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
