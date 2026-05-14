using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.Models
{
    public struct Chapter
    {
        public string Name { get; set; }

        public int Start { get; set; }


        public Chapter(string name, int start)
        {
            Name = name;
            Start = start;
        }
    }
}
