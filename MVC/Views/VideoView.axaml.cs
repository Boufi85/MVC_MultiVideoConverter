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
    public partial class VideoView : UserControl
    {
        public VideoView()
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
        private void SnippetRow_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Grid row)
            {
                row.AddHandler(InputElement.KeyDownEvent, SnippetRowControl_KeyDown, RoutingStrategies.Tunnel);
            }
        }

        private void DownloadRow_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Grid row)
            {
                row.AddHandler(InputElement.KeyDownEvent, DownloadRowControl_KeyDown, RoutingStrategies.Tunnel);
            }
        }

        private void SnippetRowControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab || DataContext is not VideoViewModel vm)
                return;

            if (e.Source is not Control current)
                return;

            if (current.Name is not ("SnippetNameTextBox" or "SnippetStartMaskedTextBox" or "SnippetEndMaskedTextBox" or "DeleteSnippetButton"))
                return;

            e.Handled = true;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                MoveSnippetFocusBackward(vm, current);
            else
                MoveSnippetFocusForward(vm, current);
        }

        private void DownloadRowControl_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab || DataContext is not VideoViewModel vm)
                return;

            if (e.Source is not Control current || current.Name != "DeleteDownloadButton")
                return;

            e.Handled = true;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                MoveDownloadFocusBackward(vm, current);
            else
                MoveDownloadFocusForward(vm, current);
        }

        private void MoveSnippetFocusForward(VideoViewModel vm, Control current)
        {
            var row = FindSnippetRow(current);
            if (row?.DataContext is not SnippetViewModel item)
                return;

            var index = vm.Snippets.IndexOf(item);
            if (index < 0)
                return;

            switch (current.Name)
            {
                case "SnippetNameTextBox":
                    if (TryFocusInRow(row, "SnippetStartMaskedTextBox"))
                        return;
                    if (TryFocusInRow(row, "SnippetEndMaskedTextBox"))
                        return;
                    if (TryFocusInRow(row, "DeleteSnippetButton"))
                        return;
                    break;

                case "SnippetStartMaskedTextBox":
                    if (TryFocusInRow(row, "SnippetEndMaskedTextBox"))
                        return;
                    if (TryFocusInRow(row, "DeleteSnippetButton"))
                        return;
                    break;

                case "SnippetEndMaskedTextBox":
                    if (TryFocusInRow(row, "DeleteSnippetButton"))
                        return;
                    break;

                case "DeleteSnippetButton":
                    if (TryFocusSnippetRowControl(index + 1, "SnippetNameTextBox"))
                        return;

                    FocusControl(DownloadVideoButton);
                    return;
            }
        }

        private void MoveSnippetFocusBackward(VideoViewModel vm, Control current)
        {
            var row = FindSnippetRow(current);
            if (row?.DataContext is not SnippetViewModel item)
                return;

            var index = vm.Snippets.IndexOf(item);
            if (index < 0)
                return;

            switch (current.Name)
            {
                case "SnippetNameTextBox":
                    if (TryFocusPreviousSnippetRowLastFocusable(index - 1))
                        return;

                    FocusControl(AddSnippetButton);
                    return;

                case "SnippetStartMaskedTextBox":
                    if (TryFocusInRow(row, "SnippetNameTextBox"))
                        return;
                    break;

                case "SnippetEndMaskedTextBox":
                    if (TryFocusInRow(row, "SnippetStartMaskedTextBox"))
                        return;
                    if (TryFocusInRow(row, "SnippetNameTextBox"))
                        return;
                    break;

                case "DeleteSnippetButton":
                    if (TryFocusInRow(row, "SnippetEndMaskedTextBox"))
                        return;
                    if (TryFocusInRow(row, "SnippetStartMaskedTextBox"))
                        return;
                    if (TryFocusInRow(row, "SnippetNameTextBox"))
                        return;
                    break;
            }
        }

        private void MoveDownloadFocusForward(VideoViewModel vm, Control current)
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

        private void MoveDownloadFocusBackward(VideoViewModel vm, Control current)
        {
            var row = FindDownloadRow(current);
            if (row?.DataContext is not DownloadViewModel item)
                return;

            var index = vm.Downloads.IndexOf(item);
            if (index < 0)
                return;

            if (TryFocusDownloadRowControl(index - 1, "DeleteDownloadButton"))
                return;

            if (TryFocusLastSnippetRowLastFocusable())
                return;

            FocusControl(DownloadVideoButton);
        }

        private Grid? FindSnippetRow(Control control)
        {
            return control.GetSelfAndVisualAncestors()
                .OfType<Grid>()
                .FirstOrDefault(g => g.DataContext is SnippetViewModel);
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

        private bool TryFocusSnippetRowControl(int index, string controlName)
        {
            if (DataContext is not VideoViewModel vm)
                return false;

            if (index < 0 || index >= vm.Snippets.Count)
                return false;

            var row = FindRepeaterRowByDataContext(SnippetsRepeater, vm.Snippets[index]);
            if (row == null)
                return false;

            return TryFocusInRow(row, controlName);
        }

        private bool TryFocusPreviousSnippetRowLastFocusable(int index)
        {
            if (DataContext is not VideoViewModel vm)
                return false;

            if (index < 0 || index >= vm.Snippets.Count)
                return false;

            var row = FindRepeaterRowByDataContext(SnippetsRepeater, vm.Snippets[index]);
            if (row == null)
                return false;

            if (TryFocusInRow(row, "DeleteSnippetButton"))
                return true;
            if (TryFocusInRow(row, "SnippetEndMaskedTextBox"))
                return true;
            if (TryFocusInRow(row, "SnippetStartMaskedTextBox"))
                return true;

            return TryFocusInRow(row, "SnippetNameTextBox");
        }

        private bool TryFocusLastSnippetRowLastFocusable()
        {
            if (DataContext is not VideoViewModel vm || vm.Snippets.Count == 0)
                return false;

            return TryFocusPreviousSnippetRowLastFocusable(vm.Snippets.Count - 1);
        }

        private bool TryFocusDownloadRowControl(int index, string controlName)
        {
            if (DataContext is not VideoViewModel vm)
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

