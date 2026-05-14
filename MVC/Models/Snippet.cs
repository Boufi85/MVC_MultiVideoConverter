using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.Models
{
    /// <summary>
    /// Represents a snippet of video
    /// </summary>
    partial class Snippet : ObservableObject
    {
        [ObservableProperty]
        private string _name;
        [ObservableProperty]
        private int _start;
        [ObservableProperty]
        private int _end;

        /// <summary>
        /// Default constructor for a video snippet
        /// </summary>
        /// <param name="name">The name of the snippet</param>
        /// <param name="start">The start time of the snippet in seconds</param>
        /// <param name="end">The end time of the snippet in seconds</param>
        public Snippet(string name, int start, int end)
        {
            Name = name;
            Start = start;
            End = end;
        }
    }
}
