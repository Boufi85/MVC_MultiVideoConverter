using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace MVC.Views
{
    public partial class VideoPageView : UserControl
    {
        public VideoPageView()
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
            if(e.Source is not MaskedTextBox tb)
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

    }
}