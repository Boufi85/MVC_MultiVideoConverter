using Avalonia.Controls.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaToolkit;
using MediaToolkit.Model;
using MVC.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoLibrary;
using System.Configuration;
using System.Globalization;
using Avalonia.Input;
using Avalonia.Controls;

namespace MVC.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private VideoPageViewModel _currentContext;

        public MainWindowViewModel()
        {
            _currentContext = new VideoPageViewModel();
        }
    }
}
