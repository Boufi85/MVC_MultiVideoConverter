using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using JsonExtensions;
using JsonExtensions.Reading;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
using static System.Net.WebRequestMethods;

namespace MVC.Models
{
    partial class Video : ObservableObject
    {
        private HttpClient Http = new();

        private string _videoId;
        public string VideoId
        {
            get => _videoId;
            set
            {
                SetProperty(ref _videoId, value);
            }
        }

        private List<Stream> _streams = [];

        public List<Stream> Streams
        {
            get => _streams;
            set
            {
                SetProperty(ref _streams, value);
            }
        }

        private List<Snippet> _snippets = [];

        public List<Snippet> Snippets
        {
            get => _snippets;
            set
            {
                SetProperty(ref _snippets, value);
            }
        }

        private List<Chapter> _chapters = [];

        public List<Chapter> Chapters
        {
            get => _chapters;
            set
            {
                SetProperty(ref _chapters, value);
            }
        }

        private int _videoDownloadCurrentProgress = 0;

        public int VideoDownloadCurrentProgress
        {
            get => _videoDownloadCurrentProgress;
            set
            {
                SetProperty(ref _videoDownloadCurrentProgress, value);
            }
        }

        private bool _downloadEnded = false;

        public bool DownloadEnded
        {
            get => _downloadEnded;
            set
            {
                SetProperty(ref _downloadEnded, value);
            }
        }

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private int _length;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Video()
        {
            VideoId = String.Empty;
            Title = String.Empty;
            Length = 0;
        }

        /// <summary>
        /// Constructor from another Video object
        /// </summary>
        /// <param name="srcItem"></param>
        public Video(Video srcItem)
        {
            VideoId = srcItem.VideoId;
            Title = srcItem.Title;
            Length = srcItem.Length;
            Streams = srcItem.Streams;
            Chapters = srcItem.Chapters;
            Snippets = srcItem.Snippets;
        }

        /// <summary>
        /// Constructor from video id, will populate the rest of the data when GetVideo is called
        /// </summary>
        /// <param name="Id"></param>
        public Video(string Id, string title)
        {
            VideoId = Id;
            Title = title;
            Length = 0;
        }

        /// <summary>
        /// Populate the video data from the video id, including title, length, streams, and chapters
        /// </summary>
        /// <returns></returns>
        public async Task GetVideo()
        {
            var visData = await Task.Run(() => ResolveVisitorDataAsync());
            string watchPage = await Task.Run(() => GetVideoWatchPageAsync());
            JsonElement initialData = await Task.Run(() => GetInitialDataAsync(watchPage));
            JsonElement player = await Task.Run(() => GetVideoPlayerAsync(visData));
            await Task.Run(() => GetName(initialData));
            Length = await Task.Run(() => GetStreams(player));
            await Task.Run(() => GetChapters(initialData));
        }

        public async Task GetStreamsOnly(string visData)
        {
            JsonElement player = await Task.Run(() => GetVideoPlayerAsync(visData));
            Length = await Task.Run(() => GetStreams(player));
        }

        /// <summary>
        /// Gets the visitor data to bypass bot check
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="YoutubeExplodeException"></exception>
        private async Task<string> ResolveVisitorDataAsync()
        {

            using var request = new HttpRequestMessage(
                System.Net.Http.HttpMethod.Get,
                "https://www.youtube.com/sw.js_data"
            );

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.Add(
                "User-Agent",
                "com.google.android.youtube/20.10.38 (Linux; U; ANDROID 11) gzip"
            );

            using var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // TODO: move this to a bridge wrapper
            var jsonString = await response.Content.ReadAsStringAsync();
            if (jsonString.StartsWith(")]}'"))
                jsonString = jsonString[4..];

            var json = JsonNode.Parse(jsonString);

            // This is just an ordered (but unstructured) blob of data
            string value = json[0][2][0][0][13].GetValue<string>();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new YoutubeExplodeException("Failed to resolve visitor data.");
            }

            return value;
        }

        /// <summary>
        /// Get the video WatchPage to extract title and length
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetVideoWatchPageAsync()
        {
            // send request
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://www.youtube.com/watch?v={VideoId}"
            );

            request.Headers.Add(
                "User-Agent",
                "com.google.android.youtube/20.10.38 (Linux; U; ANDROID 11) gzip"
            );

            using var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            return html;
        }

        /// <summary>
        /// Get the initial data from the watch page to extract title, chapters, and other metadata
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private async Task<JsonElement> GetInitialDataAsync(string html)
        {
            //Parse the HTML to extract the ytInitialData script content
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var scriptNode = htmlDoc.DocumentNode
                .Descendants("script")
                .FirstOrDefault(n => n.InnerHtml.Contains("var ytInitialData ="));

            string script = scriptNode.InnerHtml;

            //Trim beginning of the script content to isolate the JSON data
            string value = script.TrimStart("var ytInitialData ='").ToString();

            int end = value.IndexOf(';');

            // Replace the hex characters with their actual characters, and trim the end of the string

            value = Regex.Replace(value,
                @"\\x([0-9A-Fa-f]{2})",
                m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString()
                );
            value = value.TrimEnd(';', '\'');

            value = value.Replace("\\\"", "\"")
                 .Replace("\\\\", "\\")
                 .Replace("\\/", "/");

            using var doc = JsonDocument.Parse(value);
            return doc.RootElement.Clone();
        }

        /// <summary>
        /// Get the video player to extract streams
        /// </summary>
        /// <returns></returns>
        private async Task<JsonElement> GetVideoPlayerAsync(string visitorData)
        {
            var req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post,
            "https://www.youtube.com/youtubei/v1/player"
            );
            req.Content = new StringContent(
                $$"""
            {
              "videoId": {{JsonSerializer.Serialize(VideoId)}},
              "contentCheckOk": true,
              "context": {
                "client": {
                  "clientName": "ANDROID_VR",
                  "clientVersion": "1.60.19",
                  "deviceMake": "Oculus",
                  "deviceModel": "Quest 3",
                  "osName": "Android",
                  "osVersion": "12L",
                  "platform": "MOBILE",
                  "visitorData": {{JsonSerializer.Serialize(visitorData)}},
                  "hl": "en",
                  "gl": "US",
                  "utcOffsetMinutes": 0
                }
              }
            }
            """
            );
            req.Headers.Add(
                "User-Agent",
                "com.google.android.apps.youtube.vr.oculus/1.60.19 (Linux; U; Android 12L; Quest 3 Build/SQ3A.220605.009.A1) gzip"
            );

            var resp = await Http.SendAsync(req);

            var content = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            return root;
        }

        /// <summary>
        /// Get the video title from the initial data
        /// </summary>
        /// <param name="watchPage"></param>
        /// <returns></returns>
        private async Task GetName(JsonElement initialData)
        {
            if (!initialData.TryGetProperty("contents", out var contents) ||
                !contents.TryGetProperty("singleColumnWatchNextResults", out var watchNext) ||
                !watchNext.TryGetProperty("results", out var resultsWrapper) ||
                !resultsWrapper.TryGetProperty("results", out var results) ||
                !results.TryGetProperty("contents", out var contentsArray) ||
                contentsArray.ValueKind != JsonValueKind.Array)
            {
                Title = "Title not found";
                return;
            }

            foreach (var item in contentsArray.EnumerateArray())
            {
                if (!item.TryGetProperty("slimVideoMetadataSectionRenderer", out var metadata))
                    continue;

                if (!metadata.TryGetProperty("contents", out var metadataContents) ||
                    metadataContents.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var contentItem in metadataContents.EnumerateArray())
                {
                    if (!contentItem.TryGetProperty("slimVideoInformationRenderer", out var info))
                        continue;

                    if (!info.TryGetProperty("title", out var titleObj))
                        continue;

                    if (titleObj.TryGetProperty("runs", out var runs) &&
                        runs.ValueKind == JsonValueKind.Array &&
                        runs.GetArrayLength() > 0 &&
                        runs[0].TryGetProperty("text", out var textElem))
                    {
                        Title = Regex.Replace(textElem.GetString(), @"[<>:""/\\|?*]+", " ") ?? "Unknown";
                        return;
                    }

                    if (titleObj.TryGetProperty("simpleText", out var simpleText))
                    {
                        Title = Regex.Replace(simpleText.GetString(), @"[<>:""/\\|?*]+", " ") ?? "Unknown";
                        return;
                    }
                }
            }

            Title = "Title not found";
        }


        /// <summary>
        /// Get the video streams from the player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private async Task<int> GetStreams(JsonElement player)
        {
            if (!player.TryGetProperty("streamingData", out var streamingData) ||
                !streamingData.TryGetProperty("adaptiveFormats", out var streams) ||
                streams.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            foreach (var stream in streams.EnumerateArray())
            {
                string? url = null;
                if (stream.TryGetProperty("url", out var urlProp))
                {
                    url = urlProp.GetString();
                }
                else if (stream.TryGetProperty("signatureCipher", out var cipherProp))
                {
                    var cipher = cipherProp.GetString();
                    url = ParseQueryString(cipher, "url");
                }

                if (string.IsNullOrWhiteSpace(url))
                    continue;

                if (!stream.TryGetProperty("mimeType", out var mimeTypeProp))
                    continue;

                var mimeType = mimeTypeProp.GetString();
                if (string.IsNullOrWhiteSpace(mimeType))
                    continue;

                var mediaType = mimeType.Split('/')[0];
                var formatType = mimeType.Split('/')[1].Split(';')[0];

                string quality = "audio";
                if (stream.TryGetProperty("qualityLabel", out var qualityProp))
                    quality = qualityProp.GetString() ?? "unknown";
                else if (stream.TryGetProperty("audioQuality", out var audioQualityProp))
                    quality = audioQualityProp.GetString() ?? "audio";

                int bitrate = 0;
                if (stream.TryGetProperty("bitrate", out var bitrateProp))
                    bitrate = bitrateProp.GetInt32();

                Streams.Add(new Stream(
                    new Uri(url),
                    mediaType,
                    quality,
                    bitrate,
                    formatType
                ));
            }
            streams[0].TryGetProperty("approxDurationMs", out var dur);
            var duration = dur.GetString() ?? "0";
            return (int.Parse(duration) / 1000);
        }

        private static string? ParseQueryString(string? query, string key)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            foreach (var part in query.Split('&'))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0] == key)
                    return Uri.UnescapeDataString(kv[1]);
            }

            return null;
        }

        /// <summary>
        /// Populate the video chapters from the initial data, if they exist
        /// </summary>
        /// <param name="watchPage"></param>
        /// <returns></returns>
        private async Task GetChapters(JsonElement watchPage)
        {
            if (!watchPage.TryGetProperty("playerOverlays", out var playerOverlays) ||
                !playerOverlays.TryGetProperty("playerOverlayRenderer", out var overlayRenderer) ||
                !overlayRenderer.TryGetProperty("decoratedPlayerBarRenderer", out var decoratedPlayerBarRenderer) ||
                !decoratedPlayerBarRenderer.TryGetProperty("decoratedPlayerBarRenderer", out var decoratedPlayerBar) ||
                !decoratedPlayerBar.TryGetProperty("playerBar", out var playerBar) ||
                !playerBar.TryGetProperty("multiMarkersPlayerBarRenderer", out var multiMarkers) ||
                !multiMarkers.TryGetProperty("markersMap", out var markersMap) ||
                markersMap.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var markerMap in markersMap.EnumerateArray())
            {
                if (!markerMap.TryGetProperty("key", out var keyProp) ||
                    keyProp.GetString() != "DESCRIPTION_CHAPTERS" && keyProp.GetString() != "AUTO_CHAPTERS")
                    continue;

                if (!markerMap.TryGetProperty("value", out var valueProp) ||
                    !valueProp.TryGetProperty("chapters", out var chaptersArray) ||
                    chaptersArray.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var chapterItem in chaptersArray.EnumerateArray())
                {
                    if (!chapterItem.TryGetProperty("chapterRenderer", out var chapterRenderer))
                        continue;

                    if (!chapterRenderer.TryGetProperty("title", out var titleObj) ||
                        !titleObj.TryGetProperty("runs", out var runs) ||
                        runs.ValueKind != JsonValueKind.Array ||
                        runs.GetArrayLength() == 0)
                        continue;

                    if (!runs[0].TryGetProperty("text", out var textProp) ||
                        !chapterRenderer.TryGetProperty("timeRangeStartMillis", out var msProp))
                        continue;

                    Chapters.Add(new Chapter(
                        textProp.GetString() ?? "Unknown",
                        (msProp.GetInt32() / 1000)
                    ));
                }
                return;
            }
        }

        /// <summary>
        /// Populate the snippets of the video, either via video chapters or automatically
        /// </summary>
        /// <param name="ExtractChapters"></param>
        /// <returns></returns>
        private async Task PopulateSnippets(bool ExtractChapters)
        {
            if (!ExtractChapters)
            {
                Snippets.Add(new Snippet(Title, 0, Length));
            }
            else
            {
                for (int i = 0; i < Chapters.Count; i++)
                {
                    Chapter chapter = Chapters[i];
                    if (i + 1 < Chapters.Count)
                    {
                        var end = Chapters[i + 1].Start;
                        Snippets.Add(new Snippet(chapter.Name, chapter.Start, end));
                    }
                    else
                    {
                        Snippets.Add(new Snippet(chapter.Name, chapter.Start, Length));
                    }
                }
            }
        }

        /// <summary>
        ///  Get the audio and/or video stream of the video, download it on temp
        /// </summary>
        /// <param name="quality"></param>
        /// <param name="isVideo"></param>
        /// <returns name ="tempPath">The path to the downloaded stream</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> DownloadStream(string quality, bool isVideo)
        {
            Stream selectedStream;

            if (isVideo)
            {
                selectedStream = Streams
                    .Where(s => s.Type == "video" && s.QualityLabel == quality)
                    .OrderByDescending(s => s.Bitrate)
                    .Last();
            }
            else
            {
                selectedStream = Streams
                    .Where(s => s.Type == "audio")
                    .OrderByDescending(s => s.Bitrate)
                    .Last();
            }
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{selectedStream.Format}");
            var url = selectedStream.Url;
            // Create download parameters
            const int chunkSize = 10 * 1024 * 1024; // 10 MB
            const int maxParallelDownloads = 4;
            const int bufferSize = 128 * 1024;

            // Probe the server to check if it supports range requests and get total content length
            using var probeRequest = new HttpRequestMessage(HttpMethod.Get, url);
            probeRequest.Headers.Range = new RangeHeaderValue(0, 0);

            using var probeResponse = await Http.SendAsync(
                probeRequest,
                HttpCompletionOption.ResponseHeadersRead);

            // check for 206 Partial Content and valid Content-Range header
            if (probeResponse.StatusCode != HttpStatusCode.PartialContent ||
            probeResponse.Content.Headers.ContentRange?.Length is not long totalLength ||
            totalLength <= 0)
            {
                // Server does not support range requests, fallback to single download
                using var fallbackResponse = await Http.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead);

                fallbackResponse.EnsureSuccessStatusCode();

                await using var fallbackHttpStream = await fallbackResponse.Content.ReadAsStreamAsync();
                await using var fallbackFileStream = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await fallbackHttpStream.CopyToAsync(fallbackFileStream, bufferSize);
                return tempPath;
            }

            // Create temp file
            await using var destination = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Write,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess);

            destination.SetLength(totalLength);
            SafeFileHandle handle = destination.SafeFileHandle;

            // Calculate byte ranges for parallel downloading
            var ranges = new List<(long Start, long End)>();
            for (long start = 0; start < totalLength; start += chunkSize)
            {
                long end = Math.Min(start + chunkSize - 1, totalLength - 1);
                ranges.Add((start, end));
            }
            // Create a semaphore to make sure we don't exceed the max parallel downloads
            using var semaphore = new SemaphoreSlim(maxParallelDownloads);

            var tasks = ranges.Select(async range =>
            {
                //For each range download the chunk and write it to the correct position in the file
                await semaphore.WaitAsync();
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Range = new RangeHeaderValue(range.Start, range.End);

                    using var response = await Http.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead);

                    if (response.StatusCode != HttpStatusCode.PartialContent)
                    {
                        throw new InvalidOperationException(
                            $"Expected 206 for range {range.Start}-{range.End}, got {(int)response.StatusCode}.");
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync();

                    long offset = range.Start;
                    byte[] buffer = new byte[bufferSize];

                    while (true)
                    {
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                            break;

                        await RandomAccess.WriteAsync(handle, buffer.AsMemory(0, read), offset);
                        offset += read;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });
            // Wait for all downloads to complete then return the temp file path
            await Task.WhenAll(tasks);

            return tempPath;

        }

        /// <summary>
        /// Convert a snippet to file
        /// </summary>
        /// <param name="audioPath"></param>
        /// <param name="videoPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="snippet"></param>
        /// <returns></returns>
        public async Task ConvertToFile(string audioPath, string videoPath, string outputPath, Snippet snippet)
        {
            try
            {
                if (string.IsNullOrEmpty(videoPath))
                {
                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{audioPath}\" -ss {snippet.Start} -to {snippet.End} -vn -c:a libmp3lame -b:a 192k \"{Path.Combine(outputPath, $"{snippet.Name}.mp3")}\"");
                    conversion.OnProgress += (sender, args) =>
                    {
                        VideoDownloadCurrentProgress = (int)(args.Percent);
                    };
                    await conversion.Start();
                }
                else
                {
                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter(
                            $"-ss {snippet.Start} -to {snippet.End} -i \"{videoPath}\" " +
                            $"-ss {snippet.Start} -to {snippet.End} -i \"{audioPath}\" " +
                            $"-map 0:v:0 -map 1:a:0 " +
                            $"-c:v copy -c:a aac -b:a 192k -shortest " +
                            $"\"{Path.Combine(outputPath, $"{snippet.Name}.mp4")}\"");
                    conversion.OnProgress += (sender, args) =>
                    {
                        VideoDownloadCurrentProgress = (int)(args.Percent);
                    };
                    await conversion.Start();
                }
            }
            finally
            {
                VideoDownloadCurrentProgress = 100;
            }

        }
    }
}
