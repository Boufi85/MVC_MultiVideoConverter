using Avalonia.Controls.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    partial class MainWindowViewModel : ViewModelBase
    {
        public VideoViewModel Video{ get; } = new();
        public PlaylistViewModel Playlist { get; } = new();

        public MainWindowViewModel()
        {
        }
    }
}
