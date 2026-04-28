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

namespace MVC.ViewModels
{
    public partial class VideoPageViewModel : ViewModelBase
    {

        public ObservableCollection<VideoItemDownloadViewModel> VideoItems { get; } = new ObservableCollection<VideoItemDownloadViewModel>();

        private VideoItem _item;

        [NotifyCanExecuteChangedFor(nameof(LoadVideoItemCommand))]
        [ObservableProperty]
        private string? _videoURL;

        [NotifyCanExecuteChangedFor(nameof(ConvertVideoItemCommand))]
        [ObservableProperty]
        private string? _videoName;

        [NotifyCanExecuteChangedFor(nameof(ConvertVideoItemCommand))]
        [ObservableProperty]
        private string? _videoPath;

        [ObservableProperty]
        private string _videoCropStart;

        [ObservableProperty]
        private string _videoCropEnd;

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



        // these partial methods are automatically called when the corresponding properties change, allowing us to update the underlying VideoItem properties accordingly
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
        partial void OnVideoCropStartChanged(string value)
        {
            if (!IsValidTimeSpan(value))
                return;
            Console.WriteLine("timestamp updated");
            _item.VideoCropStart = (int)TimeSpan.Parse(value).TotalSeconds;
        }
        partial void OnVideoCropEndChanged(string value)
        {
            if (!IsValidTimeSpan(value))
                return;
            _item.VideoCropEnd = (int)TimeSpan.Parse(value).TotalSeconds;
        }
        partial void OnVideoDownloadCurrentProgressChanged(int value)
        {
            _item.VideoDownloadCurrentProgress = value;
        }




        // This command loads the video item based on the provided URL, initializes it, and updates the ViewModel properties accordingly. It also checks if the URL is valid before allowing execution.
        [RelayCommand(CanExecute = nameof(IsValidURL))]
        private async Task LoadVideoItem()
        {
            try
            {
                var data = await _item.InitializeAsync();
                VideoName = _item.VideoName;

                VideoCropStart = "00:00:00";
                _item.VideoCropStart = 0;
                var EndFormat = TimeSpan.FromSeconds((double)_item.VideoLength).ToString("mm\\:ss");
                if (_item.VideoLength > 3600)
                {
                    VideoCropEnd = EndFormat;
                }
                else
                {
                    VideoCropEnd = "00:" + EndFormat;
                }
                
                
            }
            catch(Exception ex)
            {
                await RaiseError(ex.Message, "Veuillez vérifier que l'URL est valide et que la vidéo est accessible.");
                
            }

            

        }

        // This method checks if the provided video URL is valid and belongs to YouTube, enabling the LoadVideoItem command accordingly
        private bool IsValidURL() => !string.IsNullOrWhiteSpace(VideoURL) && (VideoURL.Contains("www.youtube.com") || VideoURL.Contains("youtu.be"));

        // The constructor initializes the ViewModel with a new VideoItem instance, setting up the initial state for the application
        public VideoPageViewModel()
        {
            _item = new VideoItem(string.Empty);
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
            VideoName = item.VideoName;
            VideoPath = item.VideoPath;
            VideoCropStart = TimeSpan.FromSeconds(value: (double)item.VideoCropStart).ToString();
            VideoCropEnd = TimeSpan.FromSeconds(value: (double)item.VideoCropEnd).ToString();
            VideoDownloadCurrentProgress = item.VideoDownloadCurrentProgress;
        }

        // This method checks if the current video item has valid properties to allow conversion, such as a valid URL, name, and path
        private bool CanConvertVideoItem() => 
            !string.IsNullOrWhiteSpace(VideoURL) && 
            !string.IsNullOrWhiteSpace(VideoName) && 
            !string.IsNullOrWhiteSpace(VideoPath)
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
            if (_item.VideoCropStart == null || _item.VideoCropEnd == null)
            {
                await RaiseError("Timestamps invalides", "Veuillez vérifier que les timestamps de début et de fin sont au format HH:MM:SS et qu'ils sont valides.");
                return;
            }
            if (_item.VideoCropStart >= _item.VideoCropEnd)
            {
                await RaiseError("Timestamps invalides", "Le timestamp de début doit être inférieur au timestamp de fin.");
                return;
            }
            if(_item.VideoCropEnd > _item.VideoLength)
            {
                await RaiseError("Timestamps invalides", "Le timestamp de fin ne peut pas être supérieur à la durée totale de la vidéo.");
                return;
            }
            if(File.Exists(Path.Combine(VideoPath, VideoName + ".mp3")))
            {
                await RaiseError("Fichier existant", "Un fichier avec le même nom existe déjà dans le chemin de sauvegarde. Veuillez choisir un nom différent ou supprimer le fichier existant.");
                return;
            }

            var newItem = new VideoItem(_item);
            var newItemViewModel = new VideoItemDownloadViewModel(newItem);
            VideoItems.Add(newItemViewModel);
            VideoItemsCollectionPopulated = true;
            newItemViewModel.PropertyChanged += OnVideoItemDownloadProgressChanged;
            Task.Run(() => newItem.ConvertFromYoutubeToMp3());
            
            ClearCurrentVideoItem();
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
            if (item != null)
            {
                VideoItems.Remove(item);
                if (VideoItems.Count == 0)
                {
                    VideoItemsCollectionPopulated = false;
                }
            }
        }

        // This method clears the current video item properties, resetting the form for a new entry
        private void ClearCurrentVideoItem()
        {
            _item = new VideoItem(string.Empty);
            VideoURL = null;
            VideoName = null;
            VideoPath = null;
            VideoCropStart = TimeSpan.Zero.ToString();
            VideoCropEnd = TimeSpan.Zero.ToString();
            VideoDownloadCurrentProgress = 0;

        }

        // This method validates if the provided time span string is in a correct format
        private bool IsValidTimeSpan(string timeSpanStr)
        {
            return TimeSpan.TryParse(timeSpanStr, out _);
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
