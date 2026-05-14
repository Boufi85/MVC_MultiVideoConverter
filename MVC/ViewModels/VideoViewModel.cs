using AngleSharp.Dom;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVC.Models;
using MVC.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MVC.ViewModels
{
    partial class VideoViewModel : ViewModelBase
    {
        private Video CurrentVideo { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoCommand))]
        private string _videoURL;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoCommand))]
        private string _videoPath;
        
        [ObservableProperty]
        private ObservableCollection<SnippetViewModel> _snippets = [];

        [ObservableProperty]
        private ObservableCollection<DownloadViewModel> _downloads = [];

        [ObservableProperty]
        private ObservableCollection<string> _qualities = [];

        [ObservableProperty]
        private string _selectedQuality;

        [ObservableProperty]
        private bool _videoMode = false;

        [ObservableProperty]
        private bool _chapterSplit = false;

        [ObservableProperty]
        private bool _loadVideoButtonEnabled = false;

        [ObservableProperty]
        private bool _chaptersSelectionButtonEnabled = false;

        [ObservableProperty]
        private bool _addButtonEnabled = false;

        [ObservableProperty]
        private bool _clearAllButtonEnabled = false;

        [ObservableProperty]
        private bool _saveButtonEnabled = false;

        public VideoViewModel()
        {
            CurrentVideo = new Video();
            VideoURL = string.Empty;
            VideoPath = string.Empty;
            SelectedQuality = string.Empty;
            Snippets.CollectionChanged += OnSnippetsCollectionChanged;
        }


        //Events

        partial void OnVideoURLChanged(string value)
        {
            _= HandleVideoURLChangedAsync(value);
        }

        private async Task HandleVideoURLChangedAsync(string value)
        {
            var match = Regex.Match(value, @"^https?:\/\/(www\.)?youtube\.com\/watch\?v=([\w-]{11})");
            if (match.Success)
            {
                CurrentVideo = new();
                CurrentVideo.VideoId = match.Groups[2].Value;
                await CurrentVideo.GetVideo();
                ChaptersSelectionButtonEnabled = CurrentVideo.Chapters.Count > 0;
                Qualities.Clear();
                Snippets.Clear();
                for(int i = 0;i < CurrentVideo.Streams.Count;i++)
                { 
                    
                    if (!Qualities.Contains(CurrentVideo.Streams[i].QualityLabel) && CurrentVideo.Streams[i].Type =="video")
                    {
                        Qualities.Add(CurrentVideo.Streams[i].QualityLabel);
                        int index = CurrentVideo.Streams[i].QualityLabel.IndexOf("p");
                        var qualityInt = SelectedQuality != null ? int.Parse(CurrentVideo.Streams[i].QualityLabel.Substring(0, index)) : 0;
                        if (string.IsNullOrEmpty(SelectedQuality) || qualityInt > int.Parse(SelectedQuality.Substring(0, SelectedQuality.IndexOf("p"))))
                        {
                            SelectedQuality = CurrentVideo.Streams[i].QualityLabel;
                        }
                    }
                }
            }
        }

        private void OnSnippetsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SaveButtonEnabled = Snippets.Count > 0;
        }
        public void OnPageClose(Object? sender, EventArgs e)
        {

        }


        private void OnVideoDownloadProgressChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is DownloadViewModel item)
            {
                if (Downloads.Count > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ClearAllButtonEnabled = AssertVideoItemsDownloadEnded();
                    });
                }
            }
        }


        //RelayCommands

        [RelayCommand(CanExecute = nameof(CanLoadVideo))]
        private async Task LoadVideo()
        {
            PopulateSnippets(ChapterSplit);
        }

        [RelayCommand]
        private async Task BrowseVideoPath()
        {
            var dialog = await App.TopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Video Save Folder",
                AllowMultiple = false
            });

            if (dialog.Count > 0)
            {
                VideoPath = dialog[0].TryGetLocalPath()??String.Empty;
            }
        }

        [RelayCommand]
        private void AddSnippet()
        {
            var lastEndTime = Snippets.Count > 0 ? Snippets.Last().CurrentSnippet?.End ?? 0 : 0;
            Snippets.Add(new SnippetViewModel(CurrentVideo.Title, lastEndTime, CurrentVideo.Length));
        }
        [RelayCommand]
        private async Task DownloadVideo()
        {
            string tempAudioPath = string.Empty;
            string tempVideoPath = string.Empty;

            try
            {
                tempAudioPath = await CurrentVideo.DownloadStream(SelectedQuality, false);

                if (VideoMode)
                {
                    tempVideoPath = await CurrentVideo.DownloadStream(SelectedQuality, true);
                }

                var tasks = new List<Task>();

                foreach (var snippet in Snippets)
                {
                    var newItem = new Video(CurrentVideo);
                    var newItemViewModel = new DownloadViewModel(newItem, snippet.Name);
                    newItemViewModel.PropertyChanged += OnVideoDownloadProgressChanged;
                    Downloads.Add(newItemViewModel);

                    tasks.Add(newItem.ConvertToFile(
                        tempAudioPath,
                        tempVideoPath,
                        VideoPath,
                        snippet.CurrentSnippet));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempAudioPath) && System.IO.File.Exists(tempAudioPath))
                {
                    System.IO.File.Delete(tempAudioPath);
                }

                if (!string.IsNullOrWhiteSpace(tempVideoPath) && System.IO.File.Exists(tempVideoPath))
                {
                    System.IO.File.Delete(tempVideoPath);
                }
            }
            Snippets.Clear();
            CurrentVideo = new();
            ResetViewModel();
        }

        [RelayCommand]
        private void ClearDownloads()
        {
            Downloads.Clear();
            ClearAllButtonEnabled = false;
        }
        [RelayCommand]
        private void RemoveSnippet(SnippetViewModel item)
        {
            Snippets.Remove(item);
        }

        [RelayCommand]
        private void RemoveDownload(DownloadViewModel item)
        {
            Downloads.Remove(item);
        }

        // Helpers

        private void PopulateSnippets(bool hasChapters)
        {
            if (!hasChapters)
            {
                Snippets.Add(new SnippetViewModel(CurrentVideo.Title, 0, CurrentVideo.Length));
            }
            else
            {
                for (int i = 0; i < CurrentVideo.Chapters.Count; i++)
                {
                    var chapter = CurrentVideo.Chapters[i];
                    var endTime = (i < CurrentVideo.Chapters.Count - 1) ? CurrentVideo.Chapters[i + 1].Start : CurrentVideo.Length;
                    Snippets.Add(new SnippetViewModel(chapter.Name, chapter.Start, endTime));
                }              
            }
            AddButtonEnabled = true;
        }

        private bool AssertVideoItemsDownloadEnded()
        {
            foreach (var item in Downloads)
            {
                if (item.DownloadProgress != 100)
                {
                    return false;
                }
            }
            return true;
        }

        private void ResetViewModel()
        {
            VideoURL = String.Empty;
            SelectedQuality = String.Empty;
            Qualities = [];
            AddButtonEnabled = false;
            ChaptersSelectionButtonEnabled = false;

        }

        // Error handling

        public async Task RaiseError(string errorMessage, string errorDescription)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine("Error");

                var Err = new ErrorWindowView(errorMessage, errorDescription);
                Err.Show();
            });
        }

        // Checks

        private bool CanLoadVideo()
        {
            if(!string.IsNullOrEmpty(VideoURL) && Path.Exists(VideoPath))
            {
                LoadVideoButtonEnabled = true;
                return true;
            }

            return false;
        }

    }
}
