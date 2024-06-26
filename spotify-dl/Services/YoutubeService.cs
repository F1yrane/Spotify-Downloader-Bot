using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;

namespace spotify_dl.Services
{
    public class YoutubeService
    {
        private readonly YoutubeClient _youtubeClient;

        public YoutubeService()
        {
            _youtubeClient = new YoutubeClient();
        }

        public async Task<string?> SearchYoutubeUrlAsync(string trackName, string artistName)
        {
            var searchQuery = $"{trackName} {artistName}";
            var searchResults = await _youtubeClient.Search.GetVideosAsync(searchQuery);

            return searchResults?.FirstOrDefault()?.Url;
        }

        public async Task DownloadTrackAsync(string videoUrl, string outputPath)
        {
            await _youtubeClient.Videos.DownloadAsync(videoUrl, outputPath, builder => builder.SetContainer("mp3"));
        }
    }
}
