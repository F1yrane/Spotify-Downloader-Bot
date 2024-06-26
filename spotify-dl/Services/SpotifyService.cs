using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace spotify_dl.Services
{
    public class SpotifyService
    {
        private readonly SpotifyClient _spotifyClient;

        public SpotifyService(string clientId, string clientSecret)
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, clientSecret));
            _spotifyClient = new SpotifyClient(config);
        }

        public async Task<FullPlaylist> GetPlaylistAsync(string playlistId)
        {
            return await _spotifyClient.Playlists.Get(playlistId);
        }
    }
}
