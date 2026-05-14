using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using MVC.Models;

namespace MVC.ViewModels
{
    partial class SnippetViewModel : ViewModelBase
    {
        private Snippet? _currentSnippet;
        public Snippet? CurrentSnippet
        {
            get => _currentSnippet;
            set
            {
                if (_currentSnippet != value)
                {
                    _currentSnippet = value;
                }
            }
        }

        [ObservableProperty]
        private string _name;
        [ObservableProperty]
        private string _start;
        [ObservableProperty]
        private string _end;

        public SnippetViewModel(string name, int start, int end)
        {
            CurrentSnippet = new Snippet(name, start, end);
            Name = name;
            Start = TimeSpan.FromSeconds(start).ToString();
            End = TimeSpan.FromSeconds(end).ToString();
        }

        partial void OnNameChanged(string value)
        {
            if (CurrentSnippet != null)
            {
                CurrentSnippet.Name = value;
            }
        }
        partial void OnStartChanged(string value)
        {
            if (CurrentSnippet != null && TimeSpan.TryParse(value, out var result))
            {
                CurrentSnippet.Start = (int)result.TotalSeconds;
            }
        }
        partial void OnEndChanged(string value)
        {
            if (CurrentSnippet != null && TimeSpan.TryParse(value, out var result))
            {
                CurrentSnippet.End = (int)result.TotalSeconds;
            }
        }
    }
}
