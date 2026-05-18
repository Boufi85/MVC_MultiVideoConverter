using AngleSharp.Io;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVC.Models;
using MVC.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace MVC.ViewModels
{
    partial class PlaylistViewModel : ViewModelBase
    {
        private Playlist _currentPlaylist;
        public Playlist CurrentPlaylist
        {
            get => _currentPlaylist;
            set => _currentPlaylist = value;
        }

        [ObservableProperty]
        private ObservableCollection<VideoViewModel> _videos = [];

        [ObservableProperty]
        private ObservableCollection<DownloadViewModel> _downloads = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadPlaylistCommand))]
        private string _playlistPath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadPlaylistCommand))]
        private string _playlistUrl;
        [ObservableProperty]
        private bool _loadPlaylistButtonEnabled;
        [ObservableProperty]
        private bool _deleteVideoButtonEnabled = false;
        [ObservableProperty]
        private bool _clearAllButtonEnabled = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PlaylistViewModel()
        {
            CurrentPlaylist = new Playlist();
            PlaylistPath = string.Empty;
            PlaylistUrl = string.Empty;
        }

        //Events
        partial void OnPlaylistUrlChanged(string value)
        {
            _ = HandlePlaylistUrlChanged(value);
        }

        partial void OnPlaylistPathChanged(string value)
        {
            HandlePlaylistPathChanged(value);
        }

        private async Task HandlePlaylistUrlChanged(string value)
        {
            if(Regex.Match(PlaylistUrl, @"^https?:\/\/(?:www\.)?youtube\.com\/(?:watch\?[^#\s]*|playlist\?[^#\s]*)\blist=([^&\s]+)[^#\s]*$").Success)
            {
                CurrentPlaylist.Id = Regex.Match(PlaylistUrl, @"^https?:\/\/(?:www\.)?youtube\.com\/(?:watch\?[^#\s]*|playlist\?[^#\s]*)\blist=([^&\s]+)[^#\s]*$").Groups[1].Value;
            }
        }

        private void HandlePlaylistPathChanged(string value)
        {
            foreach(var video in Videos)
            {
                video.VideoPath = value;
            }
        }

        private void OnVideoTitleChanged(Object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is VideoViewModel video && e.PropertyName == nameof(VideoViewModel.VideoTitle))
            {
                video.Snippets[0].Name = video.VideoTitle;
            }
        }

        private async Task PopulateVideosAsync()
        {
            
            Videos.Clear();
            foreach(var video in CurrentPlaylist.Videos)
            {
                var newItem = new VideoViewModel(video, PlaylistPath, video.Title);
                Videos.Add(newItem);
                newItem.PropertyChanged += OnVideoTitleChanged;
            }
            await GetVideoStreamsAsync();
            DeleteVideoButtonEnabled = true;
        }

        private bool CanLoadPlaylist()
        {
            if (!string.IsNullOrEmpty(PlaylistUrl) && Path.Exists(PlaylistPath))
            {
                LoadPlaylistButtonEnabled = true;
                return true;
            }

            return false;
        }

        private async Task GetVideoStreamsAsync()
        {
            foreach(var video in Videos)
            {
                await Task.Run(() => video.CurrentVideo.GetStreamsOnly(CurrentPlaylist.VisitorData));
                video.LoadQualities();
                video.PopulateSnippets(false);
                await Task.Delay(100);
            }
        }

        private async Task ProcessVideoAsync(VideoViewModel video, SemaphoreSlim semaphore)
        {
            DeleteVideoButtonEnabled = false;
            await semaphore.WaitAsync();

            string tempAudioPath = string.Empty;
            string tempVideoPath = string.Empty;

            try
            {
                tempAudioPath = await video.CurrentVideo.DownloadStream(video.SelectedQuality, false);

                if (video.VideoMode)
                {
                    tempVideoPath = await video.CurrentVideo.DownloadStream(video.SelectedQuality, true);
                }

                var snippetTasks = video.Snippets.Select(async snippet =>
                {
                    var newItemViewModel = new DownloadViewModel(video.CurrentVideo, snippet.Name);
                    newItemViewModel.PropertyChanged += OnVideoDownloadProgressChanged;

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Downloads.Add(newItemViewModel);
                    });

                    await video.CurrentVideo.ConvertToFile(
                        tempAudioPath,
                        tempVideoPath,
                        video.VideoPath,
                        snippet.CurrentSnippet);
                });

                await Task.WhenAll(snippetTasks);
            }
            finally
            {
                semaphore.Release();

                await DeleteFileIfExistsAsync(tempAudioPath);
                await DeleteFileIfExistsAsync(tempVideoPath);
                ResetViewModel();
            }
        }

        private static Task DeleteFileIfExistsAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return Task.CompletedTask;
            }

            return Task.Run(() => System.IO.File.Delete(path));
        }

        private void ResetViewModel()
        {
            PlaylistUrl = String.Empty;
            LoadPlaylistButtonEnabled = false;
            DeleteVideoButtonEnabled = false;

        }

        // RelayCommands 

        [RelayCommand]
        private async Task DownloadVideos()
        {
            foreach(var video in Videos)
            {
                if (Regex.Match(video.VideoTitle, @"[<>:""/\\|?*]+").Success)
                {
                    await RaiseError("Erreur de téléchargement", $"Le nom de la vidéo {video.VideoTitle} contient des caractères interdits (<>:\"/\\|?*). Veuillez renommer cette vidéo avant de lancer le téléchargement.");
                    return;
                }
                if (video.VideoMode)
                {
                    if (File.Exists(Path.Combine(PlaylistPath, $"{video.VideoTitle}.mp4")))
                    {
                        await RaiseError("Erreur de téléchargement", $"Le fichier de la vidéo {video.VideoTitle}.mp4 existe déjà dans le dossier de destination. Veuillez supprimer le fichier existant ou renommer la vidéo.");
                        return;
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(PlaylistPath, $"{video.VideoTitle}.mp3")))
                    {
                        await RaiseError("Erreur de téléchargement", $"Le fichier de la vidéo {video.VideoTitle}.mp3 existe déjà dans le dossier de destination. Veuillez supprimer le fichier existant ou renommer la vidéo.");
                        return;
                    }
                }
            }
            using var semaphore = new SemaphoreSlim(2, 2);

            var tasks = Videos.Select(video => ProcessVideoAsync(video, semaphore)).ToList();

            await Task.WhenAll(tasks);

            Videos.Clear();
            ResetViewModel();
        }

        [RelayCommand (CanExecute = nameof(CanLoadPlaylist))]
        private async Task LoadPlaylist()
        {
            try
            {
                CurrentPlaylist.Videos.Clear();
                await CurrentPlaylist.GetPlaylist();
                await PopulateVideosAsync();
            }
            catch
            {
                await RaiseError("Erreur de chargement de la playlist", "La playlist n'a pas pu être chargée. Elle n'existe pas ou est indisponible.");
            }
        }
        [RelayCommand]
        private async Task BrowsePlaylistPath()
        {
            var dialog = await App.TopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choisissez l'emplacement d'enregistrement de la playlist",
                AllowMultiple = false
            });

            if (dialog.Count > 0)
            {
                PlaylistPath = dialog[0].TryGetLocalPath() ?? String.Empty;
            }
        }

        [RelayCommand]
        private void RemoveVideo(VideoViewModel item)
        {
            Videos.Remove(item);
        }

        [RelayCommand]
        private void RemoveDownload(DownloadViewModel item)
        {
            Downloads.Remove(item);
        }
        [RelayCommand]
        private void ClearDownloads()
        {
            Downloads.Clear();
            ClearAllButtonEnabled = false;
        }
        public void OnVideoDownloadProgressChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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

        //Error
        public async Task RaiseError(string errorMessage, string errorDescription)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine("Error");

                var Err = new ErrorWindowView(errorMessage, errorDescription);
                Err.Show();
            });
        }
    }
}
