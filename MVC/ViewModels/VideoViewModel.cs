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
        public Video CurrentVideo { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoCommand))]
        private string _videoURL;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadVideoCommand))]
        private string _videoPath;

        [ObservableProperty]
        private string _videoTitle;

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

        /// <summary>
        /// Default constructor
        /// </summary>
        public VideoViewModel()
        {
            CurrentVideo = new Video();
            VideoURL = string.Empty;
            VideoPath = string.Empty;
            VideoTitle = string.Empty;
            SelectedQuality = string.Empty;
            Snippets.CollectionChanged += OnSnippetsCollectionChanged;
        }

        /// <summary>
        /// Constructor that initializes the ViewModel with a Video, video path and title
        /// </summary>
        /// <param name="vid"></param>
        /// <param name="path"></param>
        /// <param name="Title"></param>
        public VideoViewModel(Video vid, string path, string Title)
        {
            CurrentVideo = vid;
            VideoURL = string.Empty;
            VideoPath = path;
            VideoTitle = Title;
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
                try
                {
                    CurrentVideo = new();
                    CurrentVideo.VideoId = match.Groups[2].Value;
                    await CurrentVideo.GetVideo();
                    ChaptersSelectionButtonEnabled = CurrentVideo.Chapters.Count > 0;
                    Qualities.Clear();
                    Snippets.Clear();
                    LoadQualities();
                }
                finally
                {
                    if (CurrentVideo.Title == "Title not found")
                    {
                        await RaiseError("Erreur de chargement de la vidéo", "La vidéo n'a pas pu être chargée. Elle n'existe pas ou est indisponible.");
                        ResetViewModel();
                    }
                }
            }
        }

        public void LoadQualities()
        {
            for (int i = 0; i < CurrentVideo.Streams.Count; i++)
            {

                if (!Qualities.Contains(CurrentVideo.Streams[i].QualityLabel) && CurrentVideo.Streams[i].Type == "video")
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

        private void OnSnippetsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SaveButtonEnabled = Snippets.Count > 0;
        }

        public void OnPageClose(Object? sender, EventArgs e)
        {

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
                Title = "Choisissez l'emplacement d'enregistrement de la vidéo",
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
            foreach(var snippet in Snippets)
            {
                if (snippet.CurrentSnippet.Start == snippet.CurrentSnippet.End)
                {
                    await RaiseError("Erreur de téléchargement", "Un ou plusieurs extraits ont une durée de 0 seconde. Veuillez vérifier les extraits avant de lancer le téléchargement.");
                    return;
                }
                if (snippet.CurrentSnippet.Start > snippet.CurrentSnippet.End)
                {
                    await RaiseError("Erreur de téléchargement", "Un ou plusieurs extraits ont un début supérieure à leur fin. Veuillez vérifier les extraits avant de lancer le téléchargement.");
                    return;
                }
                if (snippet.CurrentSnippet.End > CurrentVideo.Length)
                {
                    await RaiseError("Erreur de téléchargement", "Un ou plusieurs extraits ont une fin supérieure à la durée de la vidéo. Veuillez vérifier les extraits avant de lancer le téléchargement.");
                    return;
                }
                if (Regex.Match(snippet.CurrentSnippet.Name, @"[<>:""/\\|?*]+").Success)
                {
                    await RaiseError("Erreur de téléchargement", $"Le nom de l'extrait {snippet.Name} contient des caractères interdits (<>:\"/\\|?*). Veuillez renommer cet extrait avant de lancer le téléchargement.");
                    return;
                }
                if (VideoMode)
                {
                    if (File.Exists(Path.Combine(VideoPath, $"{snippet.Name}.mp4")))
                    {
                        await RaiseError("Erreur de téléchargement", $"Le fichier {snippet.Name}.mp4 existe déjà dans le dossier de destination. Veuillez supprimer ou renommer ce fichier avant de lancer le téléchargement.");
                        return;
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(VideoPath, $"{snippet.Name}.mp3")))
                    {
                        await RaiseError("Erreur de téléchargement", $"Le fichier {snippet.Name}.mp3 existe déjà dans le dossier de destination. Veuillez supprimer ou renommer ce fichier avant de lancer le téléchargement.");
                        return;
                    }
                }
            }
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

        public void PopulateSnippets(bool hasChapters)
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
