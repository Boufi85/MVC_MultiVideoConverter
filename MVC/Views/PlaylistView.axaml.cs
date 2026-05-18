using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MVC.ViewModels;
using System.Linq;
using System.Threading.Tasks;
namespace MVC.Views
{
    public partial class PlaylistView : UserControl
    {
        public PlaylistView()
        {
            InitializeComponent();
            AddHandler(InputElement.KeyDownEvent, MaskedTextBox_KeyDown, RoutingStrategies.Tunnel);
        }
        private async void MaskedTextBox_GotFocus(object? sender, Avalonia.Input.FocusChangedEventArgs e)
        {
            if (sender is Avalonia.Controls.MaskedTextBox maskedTextBox)
            {

                await Task.Yield();
                maskedTextBox.SelectionStart = 0;
                maskedTextBox.SelectionEnd = 0;
            }
        }
        private async void MaskedTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Source is not MaskedTextBox tb)
            {
                return;
            }
            if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                var clipboard = TopLevel.GetTopLevel(App.TopLevel).Clipboard;
                if (clipboard != null)
                {
                    var text = await clipboard.TryGetTextAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        tb.SelectedText = text;
                        e.Handled = true;
                    }
                }
            }
        }
        private void VideoRow_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Grid row)
            {
                row.AddHandler(InputElement.KeyDownEvent, VideoRowControl_KeyDown, RoutingStrategies.Tunnel);
            }
        }

        private void DownloadRow_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Grid row)
            {
                row.AddHandler(InputElement.KeyDownEvent, DownloadRowControl_KeyDown, RoutingStrategies.Tunnel);
            }
        }

        private void VideoRowControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab || DataContext is not PlaylistViewModel vm)
                return;

            if (e.Source is not Control current)
                return;

            if (current.Name is not ("VideoTitleTextBox" or "AudioToggleButton" or "VideoToggleButton" or "QualityComboBox" or "DeleteVideoButton"))
                return;

            e.Handled = true;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                MoveVideoFocusBackward(vm, current);
            else
                MoveVideoFocusForward(vm, current);
        }

        private void DownloadRowControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab || DataContext is not PlaylistViewModel vm)
                return;

            if (e.Source is not Control current || current.Name != "DeleteDownloadButton")
                return;

            e.Handled = true;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                MoveDownloadFocusBackward(vm, current);
            else
                MoveDownloadFocusForward(vm, current);
        }

        private void MoveVideoFocusForward(PlaylistViewModel vm, Control current)
        {
            var row = FindVideoRow(current);
            if (row?.DataContext is not VideoViewModel item)
                return;

            var index = vm.Videos.IndexOf(item);
            if (index < 0)
                return;

            switch (current.Name)
            {
                case "VideoTitleTextBox":
                    if (TryFocusInRow(row, "AudioToggleButton"))
                        return;
                    if (TryFocusInRow(row, "VideoToggleButton"))
                        return;
                    if (TryFocusInRow(row, "QualityComboBox"))
                        return;
                    if (TryFocusInRow(row, "DeleteVideoButton"))
                        return;
                    break;

                case "AudioToggleButton":
                    if (TryFocusInRow(row, "QualityComboBox"))
                        return;
                    if (TryFocusInRow(row, "DeleteVideoButton"))
                        return;
                    break;

                case "VideoToggleButton":
                    if (TryFocusInRow(row, "QualityComboBox"))
                        return;
                    if (TryFocusInRow(row, "DeleteVideoButton"))
                        return;
                    break;

                case "QualityComboBox":
                    if (TryFocusInRow(row, "DeleteVideoButton"))
                        return;
                    break;

                case "DeleteVideoButton":
                    if (TryFocusVideoRowControl(index + 1, "VideoTitleTextBox"))
                        return;

                    FocusControl(DownloadVideosButton);
                    return;
            }
        }

        private void MoveVideoFocusBackward(PlaylistViewModel vm, Control current)
        {
            var row = FindVideoRow(current);
            if (row?.DataContext is not VideoViewModel item)
                return;

            var index = vm.Videos.IndexOf(item);
            if (index < 0)
                return;

            switch (current.Name)
            {
                case "VideoTitleTextBox":
                    if (TryFocusPreviousVideoRowLastFocusable(index - 1))
                        return;

                    FocusControl(GetPlaylistButton);
                    return;

                case "AudioToggleButton":
                    if (TryFocusInRow(row, "VideoTitleTextBox"))
                        return;
                    break;

                case "VideoToggleButton":
                    if (TryFocusInRow(row, "AudioToggleButton"))
                        return;
                    if (TryFocusInRow(row, "VideoTitleTextBox"))
                        return;
                    break;

                case "QualityComboBox":
                    if (TryFocusInRow(row, "VideoToggleButton"))
                        return;
                    if (TryFocusInRow(row, "AudioToggleButton"))
                        return;
                    if (TryFocusInRow(row, "VideoTitleTextBox"))
                        return;
                    break;

                case "DeleteVideoButton":
                    if (TryFocusInRow(row, "QualityComboBox"))
                        return;
                    if (TryFocusInRow(row, "VideoToggleButton"))
                        return;
                    if (TryFocusInRow(row, "AudioToggleButton"))
                        return;
                    if (TryFocusInRow(row, "VideoTitleTextBox"))
                        return;
                    break;
            }
        }

        private void MoveDownloadFocusForward(PlaylistViewModel vm, Control current)
        {
            var row = FindDownloadRow(current);
            if (row?.DataContext is not DownloadViewModel item)
                return;

            var index = vm.Downloads.IndexOf(item);
            if (index < 0)
                return;

            if (TryFocusDownloadRowControl(index + 1, "DeleteDownloadButton"))
                return;

            FocusControl(ClearDownloadsButton);
        }

        private void MoveDownloadFocusBackward(PlaylistViewModel vm, Control current)
        {
            var row = FindDownloadRow(current);
            if (row?.DataContext is not DownloadViewModel item)
                return;

            var index = vm.Downloads.IndexOf(item);
            if (index < 0)
                return;

            if (TryFocusDownloadRowControl(index - 1, "DeleteDownloadButton"))
                return;

            if (TryFocusLastVideoRowLastFocusable())
                return;

            FocusControl(DownloadVideosButton);
        }

        private Grid? FindVideoRow(Control control)
        {
            return control.GetSelfAndVisualAncestors()
                .OfType<Grid>()
                .FirstOrDefault(g => g.DataContext is VideoViewModel);
        }

        private Grid? FindDownloadRow(Control control)
        {
            return control.GetSelfAndVisualAncestors()
                .OfType<Grid>()
                .FirstOrDefault(g => g.DataContext is DownloadViewModel);
        }

        private bool TryFocusInRow(Grid row, string controlName)
        {
            var control = FindNamedVisualChild<Control>(row, controlName);
            if (!CanFocus(control))
                return false;

            FocusControl(control);
            return true;
        }

        private bool TryFocusVideoRowControl(int index, string controlName)
        {
            if (DataContext is not PlaylistViewModel vm)
                return false;

            if (index < 0 || index >= vm.Videos.Count)
                return false;

            var row = FindRepeaterRowByDataContext(VideosRepeater, vm.Videos[index]);
            if (row == null)
                return false;

            return TryFocusInRow(row, controlName);
        }

        private bool TryFocusPreviousVideoRowLastFocusable(int index)
        {
            if (DataContext is not PlaylistViewModel vm)
                return false;

            if (index < 0 || index >= vm.Videos.Count)
                return false;

            var row = FindRepeaterRowByDataContext(VideosRepeater, vm.Videos[index]);
            if (row == null)
                return false;

            if (TryFocusInRow(row, "DeleteVideoButton"))
                return true;
            if (TryFocusInRow(row, "QualityComboBox"))
                return true;
            if (TryFocusInRow(row, "VideoToggleButton"))
                return true;
            if (TryFocusInRow(row, "AudioToggleButton"))
                return true;

            return TryFocusInRow(row, "VideoTitleTextBox");
        }

        private bool TryFocusLastVideoRowLastFocusable()
        {
            if (DataContext is not PlaylistViewModel vm || vm.Videos.Count == 0)
                return false;

            return TryFocusPreviousVideoRowLastFocusable(vm.Videos.Count - 1);
        }

        private bool TryFocusDownloadRowControl(int index, string controlName)
        {
            if (DataContext is not PlaylistViewModel vm)
                return false;

            if (index < 0 || index >= vm.Downloads.Count)
                return false;

            var row = FindRepeaterRowByDataContext(DownloadsRepeater, vm.Downloads[index]);
            if (row == null)
                return false;

            return TryFocusInRow(row, controlName);
        }

        private Grid? FindRepeaterRowByDataContext(ItemsRepeater repeater, object item)
        {
            return repeater.GetVisualDescendants()
                .OfType<Grid>()
                .FirstOrDefault(g => ReferenceEquals(g.DataContext, item));
        }

        private static T? FindNamedVisualChild<T>(Control root, string name) where T : Control
        {
            return root.GetVisualDescendants()
                .OfType<T>()
                .FirstOrDefault(c => c.Name == name);
        }

        private static bool CanFocus(Control? control)
        {
            return control is { IsEnabled: true, IsVisible: true, Focusable: true };
        }

        private static void FocusControl(Control? control)
        {
            if (!CanFocus(control))
                return;

            control.Focus(NavigationMethod.Tab);
        }
    }
}

