using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.Models
{
    /// <summary>
    /// Describes all the streams of a video, such as the quality, format, etc.
    /// </summary>
    public struct Stream
    {
        private Uri _url;

        public Uri Url
        {
            get { return _url; }
            set { _url = value; }
        }
        private string _type;

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }
        private string _qualityLabel;

        public string QualityLabel
        {
            get { return _qualityLabel; }
            set { _qualityLabel = value; }
        }

        public int Bitrate
        {
            get
            {
                return _bitrate;
            }
            set
            {
                _bitrate = value;
            }
        }
        private int _bitrate;

        public string Format
        {
            get
            {
                return _format;
            }
            set
            {
                _format = value;
            }
        }

        private string _format;
        public Stream (Uri url, string type, string qualityLabel, int bitrate, string format)
        {
            Url = url;
            Type = type;
            QualityLabel = qualityLabel;
            Bitrate = bitrate;
            Format = format;

        }

    }
}
