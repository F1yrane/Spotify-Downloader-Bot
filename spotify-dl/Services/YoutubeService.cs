using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;

namespace spotify_dl.Services
{
    public class YoutubeService
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly ConcurrentDictionary<string, string> _cache;

        public YoutubeService()
        {
            _youtubeClient = new YoutubeClient();
            _cache = new ConcurrentDictionary<string, string>();
        }

        public async Task<string?> SearchYoutubeUrlAsync(string trackName, string artistName)
        {
            var searchQuery = $"{trackName} {artistName}";
            if (_cache.TryGetValue(searchQuery, out var cachedUrl))
            {
                Log.Information("Found cached URL for {SearchQuery}: {CachedUrl}", searchQuery, cachedUrl);
                return cachedUrl;
            }

            try
            {
                var searchResults = await _youtubeClient.Search.GetVideosAsync(searchQuery);
                var videoUrl = searchResults?.FirstOrDefault()?.Url;
                if (videoUrl != null)
                {
                    _cache[searchQuery] = videoUrl;
                    Log.Information("Added URL to cache for {SearchQuery}: {VideoUrl}", searchQuery, videoUrl);
                }
                return videoUrl;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching YouTube for {SearchQuery}: {ErrorMessage}", searchQuery, ex.Message);
                return null;
            }
        }

        public async Task DownloadTrackAsync(string videoUrl, string outputPath)
        {
            try
            {
                Log.Information("Downloading video from {VideoUrl} to {OutputPath}", videoUrl, outputPath);
                await _youtubeClient.Videos.DownloadAsync(videoUrl, outputPath, builder => builder.SetContainer("mp3"));
                Log.Information("Download completed for {VideoUrl}", videoUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading video {VideoUrl}: {ErrorMessage}", videoUrl, ex.Message);
            }
        }
    }
}
