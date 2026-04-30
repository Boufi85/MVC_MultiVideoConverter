using CommunityToolkit.Mvvm.ComponentModel;
using MVC.Models;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MVC.Views;
using System.IO;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace MVC.ViewModels
{
    public partial class VideoPageViewModel : ViewModelBase
    {

        public ObservableCollection<VideoItemDownloadViewModel> VideoItems { get; } = new ObservableCollection<VideoItemDownloadViewModel>();

        private VideoItem _item;

        [ObservableProperty]
        public ObservableCollection<VideoSnippetViewModel> _videoSnippets = new ObservableCollection<VideoSnippetViewModel>();

        [NotifyCanExecuteChangedFor(nameof(LoadVideoItemCommand))]
        [ObservableProperty]
        private string? _videoURL;

        [NotifyCanExecuteChangedFor(nameof(ConvertVideoItemCommand))]
        [ObservableProperty]
        private string? _videoPath;

        [ObservableProperty]
        private int _videoDownloadCurrentProgress;

        [ObservableProperty]
        private bool _videoItemsCollectionPopulated = false;

        [ObservableProperty]
        private bool _downloadAudioButtonEnabled = false;

        [ObservableProperty]
        private bool _isVideoItemLoaded = false;

        [NotifyCanExecuteChangedFor(nameof(ClearVideoItemsCommand))]
        [ObservableProperty]
        private bool _clearAllButtonEnabled = false;

        [ObservableProperty]
        private bool _showSnippetSection = false;
        [NotifyCanExecuteChangedFor(nameof(ConvertVideoItemCommand))]
        [ObservableProperty]
        private bool _canDownloadSnippets = false;

        private static readonly Regex InvalidCharacters = new(@"[\\\/:\*\?""<>\|]", RegexOptions.Compiled);
        // these partial methods are automatically called when the corresponding properties change, allowing us to update the underlying VideoItem properties accordingly
        partial void OnVideoURLChanged(string? value)
        {
            _item.VideoURL = value;
        }
        partial void OnVideoPathChanged(string? value)
        {
            _item.VideoPath = value;
        }
        partial void OnVideoDownloadCurrentProgressChanged(int value)
        {
            _item.VideoDownloadCurrentProgress = value;
        }

        
        [RelayCommand]
        private void AddVideoSnippet()
        {
            if (VideoSnippets.Count == 0)
            {
                var newVideoSnippet = new VideoSnippetViewModel(_item.Video.Title, _item.VideoLength ?? 0);
                VideoSnippets.Add(newVideoSnippet);
            }
            else
            {
                var lastSnippet = VideoSnippets[VideoSnippets.Count - 1];
                var newVideoSnippet = new VideoSnippetViewModel(_item.Video.Title, lastSnippet.Item.VideoSnippetCropEnd, _item.VideoLength ?? 0);
                VideoSnippets.Add(newVideoSnippet);
            }
        }



        // This command loads the video item based on the provided URL, initializes it, and updates the ViewModel properties accordingly. It also checks if the URL is valid before allowing execution.
        [RelayCommand(CanExecute = nameof(CanLoadVideoItem))]
        private async Task LoadVideoItem()
        {
            try
            {
                var data = await _item.InitializeAsync();
                var newVideoSnippet = new VideoSnippetViewModel(_item.Video.Title, _item.VideoLength ?? 0);
                VideoSnippets.Clear();
                VideoSnippets.Add(newVideoSnippet);
                ShowSnippetSection = true;
            }
            catch(Exception ex)
            {
                await RaiseError(ex.Message, "Veuillez vérifier que l'URL est valide et que la vidéo est accessible.");
                
            }
        }

        // This method checks if the provided video URL is valid and belongs to YouTube, enabling the LoadVideoItem command accordingly
        private bool CanLoadVideoItem() => !string.IsNullOrWhiteSpace(VideoURL) && (VideoURL.Contains("www.youtube.com") || VideoURL.Contains("youtu.be"));

        // The constructor initializes the ViewModel with a new VideoItem instance, setting up the initial state for the application
        public VideoPageViewModel()
        {
            _item = new VideoItem(string.Empty);

            VideoSnippets.CollectionChanged += (sender, e) =>
            {
                if (VideoSnippets.Count > 0)
                {
                    CanDownloadSnippets = true;
                }
                else
                {
                    CanDownloadSnippets = false;
                }

            };
            
        }

        public async Task RaiseError(string errorMessage, string errorDescription)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine("Error");

                var Err = new ErrorWindowView(errorMessage, errorDescription);
                Err.Show();
            });
        }

        // This overloaded constructor allows initializing the ViewModel with an existing VideoItem, which can be useful for editing or reloading a previously loaded video item
        public VideoPageViewModel(VideoItem item)
        {
            _item = item;
            VideoURL = item.VideoURL;
            VideoPath = item.VideoPath;
            VideoDownloadCurrentProgress = item.VideoDownloadCurrentProgress;
        }

        // This method checks if the current video item has valid properties to allow conversion, such as a valid URL, name, and path
        private bool CanConvertVideoItem() => 
            !string.IsNullOrWhiteSpace(VideoURL) && 
            !string.IsNullOrWhiteSpace(VideoPath) && CanDownloadSnippets
            ;
        

        // This command creates a new VideoItem based on the current item, adds it to the collection, and starts the conversion process in a background task
        [RelayCommand(CanExecute = nameof(CanConvertVideoItem))]
        private async Task ConvertVideoItem()
        {
            if (!Path.Exists(VideoPath))
            {
                await RaiseError("Chemin invalide", "Veuillez sélectionner un chemin de sauvegarde valide pour la vidéo.");
                return;
            }
            foreach(var snippet in VideoSnippets)
            {
                if(File.Exists(Path.Combine(VideoPath, snippet.VideoSnippetName + ".mp3")))
                {
                    await RaiseError("Fichier existant", $"Un fichier avec le nom '{snippet.VideoSnippetName}.mp3' existe déjà dans le chemin de sauvegarde. Veuillez choisir un nom différent ou supprimer le fichier existant.");
                    return;
                }
                
                if (snippet.Item.VideoSnippetCropStart >= snippet.Item.VideoSnippetCropEnd)
                {
                    await RaiseError("Segments invalides", $"Le début du segment '{snippet.VideoSnippetName}' doit être inférieur à la fin.");
                    return;
                }
                if (snippet.Item.VideoSnippetCropEnd > _item.VideoLength)
                {
                    await RaiseError("Segments invalides", $"La fin du segment '{snippet.VideoSnippetName}' ne peut pas être supérieure à la durée totale de la vidéo.");
                    return;
                }
                if (!snippet.IsValidTimeSpan(snippet.VideoSnippetCropStart) || !snippet.IsValidTimeSpan(snippet.VideoSnippetCropEnd))
                {
                    await RaiseError("Segments invalides", $"Les valeurs du segment '{snippet.VideoSnippetName}' ne sont pas valides.");
                    return;
                }
                if (InvalidCharacters.IsMatch(snippet.VideoSnippetName))
                {
                    await RaiseError("Segments invalides", $"Le nom du segment '{snippet.VideoSnippetName}' contient des caractères invalides.\n Les caractères invalides sont : \\ / : * ? \" < > |");
                    return;
                }
            }
            /*if(File.Exists(Path.Combine(VideoPath, VideoName + ".mp3")))
            {
                await RaiseError("Fichier existant", "Un fichier avec le même nom existe déjà dans le chemin de sauvegarde. Veuillez choisir un nom différent ou supprimer le fichier existant.");
                return;
            }*/

                        
            foreach (var snippet in VideoSnippets)
            {
                var newItem = new VideoItem(_item);
                var newItemViewModel = new VideoItemDownloadViewModel(newItem, snippet.VideoSnippetName);
                newItemViewModel.PropertyChanged += OnVideoItemDownloadProgressChanged;
                VideoItems.Add(newItemViewModel);
                Task.Run(() => newItem.ConvertFromYoutubeToMp3(snippet.Item));
            }
            VideoItemsCollectionPopulated = true;
            VideoSnippets.Clear();
        }

        // This event handler listens for changes in the download progress of each video item, and updates the Clear All button state accordingly when any item's download progress changes
        private void OnVideoItemDownloadProgressChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is VideoItemDownloadViewModel item)
            {
                if(VideoItems.Count>0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ClearAllButtonEnabled = AssertVideoItemsDownloadEnded();
                    });
                }
            }
        }

        // This method checks if all video items in the collection have finished downloading, which is used to enable or disable the Clear All button
        private bool AssertVideoItemsDownloadEnded()
        {
            foreach (var item in VideoItems)
            {
                if (!item.IsDownloadFinished)
                {
                    return false;
                }
            }
            return true; 
        }

        // This command allows the user to remove a video item from the list
        [RelayCommand]
        private void RemoveItem(VideoItemDownloadViewModel item)
        {
            if (item != null && item.IsDownloadFinished)
            {
                VideoItems.Remove(item);
                if (VideoItems.Count == 0)
                {
                    ClearAllButtonEnabled = false;
                    VideoItemsCollectionPopulated = false;
                }
            }
        }

        [RelayCommand]
        private void RemoveSnippet(VideoSnippetViewModel item)
        {
            if (item != null)
            {
                VideoSnippets.Remove(item);
                
            }
        }


        

        // This command clears all video items from the list, resetting the application state
        [RelayCommand]
        private void ClearVideoItems()
        {
            if (ClearAllButtonEnabled)
            {
                VideoItems.Clear();
                VideoItemsCollectionPopulated = false;
                ClearAllButtonEnabled = false;
            }
            
        }

        // This command opens a folder picker dialog for the user to select a save location for the video, and updates the VideoPath property accordingly
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
                VideoPath = dialog[0].TryGetLocalPath();
            }
        }
    }
}
