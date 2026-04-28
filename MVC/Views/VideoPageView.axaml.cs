using Avalonia;
using Avalonia.Controls;
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
        }
        private async void MaskedTextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
        {
            if (sender is Avalonia.Controls.MaskedTextBox maskedTextBox)
            {

                await Task.Yield();
                maskedTextBox.Text = "";
                maskedTextBox.SelectionStart = 0;
                maskedTextBox.SelectionEnd = 0;
            }
        }
    }
}