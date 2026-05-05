using Avalonia.Controls;
using MVC.ViewModels;
using System;
using System.Threading.Tasks;

namespace MVC.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closed += OnClosed;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if(DataContext is MainWindowViewModel vm)
            {
                vm.CurrentContext.OnPageClose(sender, e);
            }
        }
    }
}