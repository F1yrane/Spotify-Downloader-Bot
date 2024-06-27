using System;
using System.Threading.Tasks;
using Serilog;
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
            try
            {
                return await _spotifyClient.Playlists.Get(playlistId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching playlist {PlaylistId}", playlistId);
                throw;
            }
        }

        public async Task<FullTrack> GetTrackAsync(string trackId)
        {
            try
            {
                return await _spotifyClient.Tracks.Get(trackId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching track {TrackId}", trackId);
                throw;
            }
        }

        public async Task<FullAlbum> GetAlbumAsync(string albumId)
        {
            try
            {
                return await _spotifyClient.Albums.Get(albumId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching album {AlbumId}", albumId);
                throw;
            }
        }
    }
}
