using CommunityToolkit.Mvvm.ComponentModel;
using MVC.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode.Exceptions;
using static System.Net.WebRequestMethods;

namespace MVC.Models
{
    partial class Playlist : ObservableObject
    {
        private HttpClient Http = new();

        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _visitorData = string.Empty;

        public string VisitorData
        {
            get => _visitorData;
            set => SetProperty(ref _visitorData, value);
        }
        public List<Video> Videos { get; set; } = new List<Video>();
        public Playlist()
        {
            
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
        /// Get the playlist data
        /// </summary>
        /// <returns></returns>
        public async Task GetPlaylist()
        {
            VisitorData = await Task.Run(() => ResolveVisitorDataAsync());
            var playlistData = await Task.Run(() => GetPlaylistDataAsync(VisitorData));
            await Task.Run(() => GetPlaylistElementsAsync(playlistData));

        }

        private async Task<JsonElement> GetPlaylistDataAsync(string visData)
        {
            var req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post,
                "https://www.youtube.com/youtubei/v1/next"
                );
            req.Content = new StringContent(
                $$"""
            {
              "playlistId": {{JsonSerializer.Serialize(Id)}},
              "videoId": {{JsonSerializer.Serialize(string.Empty)}},
              "playlistIndex": 0,
              "context": {
                "client": {
                  "clientName": "WEB",
                  "clientVersion": "2.20210408.08.00",
                  "hl": "en",
                  "gl": "US",
                  "utcOffsetMinutes": 0,
                  "visitorData": {{JsonSerializer.Serialize(visData)}}
                }
              }
            }
            """
            );

            var resp = await Http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            return root;
        }
        
        private async Task GetPlaylistElementsAsync(JsonElement playlistData)
        {
            string videoId = null;
            string videoTitle = null;
            if (playlistData.TryGetProperty("contents", out var contents) &&
                contents.TryGetProperty("twoColumnWatchNextResults", out var twoColumn) &&
                 twoColumn.TryGetProperty("playlist", out var playlist) &&
                    playlist.TryGetProperty("playlist", out var subPlaylist) &&
                        subPlaylist.TryGetProperty("contents", out var playlistContents))
            {
                foreach (var element in playlistContents.EnumerateArray())
                {
                    if (element.TryGetProperty("playlistPanelVideoRenderer", out var videoRenderer))
                    {
                        if(videoRenderer.TryGetProperty("title", out var title))
                        {
                            videoTitle = Regex.Replace(title.GetProperty("simpleText").GetString(), @"[<>:""/\\|?*]+", " ") ?? string.Empty;
                        }
                        if (videoRenderer.TryGetProperty("navigationEndpoint", out var navEndpoint))
                        {
                            if (navEndpoint.TryGetProperty("watchEndpoint", out var endpoint))
                            {
                                videoId = endpoint.GetProperty("videoId").GetString() ?? string.Empty;
                                
                                Videos.Add(new Video(videoId, videoTitle));
                            }
                        }
                    }
                }
            }
        }

    }
}
