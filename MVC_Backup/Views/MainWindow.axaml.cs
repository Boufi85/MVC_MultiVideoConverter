using Avalonia.Controls;
using System.Threading.Tasks;

namespace MVC.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MaskedTextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
        {
            if (sender is Avalonia.Controls.MaskedTextBox maskedTextBox)
            {
                await Task.Yield();
                maskedTextBox.SelectionStart = 0;
                maskedTextBox.SelectionEnd = 0;
            }
        }
    }
}