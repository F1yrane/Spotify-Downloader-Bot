using System.Collections.Concurrent;
using SpotifyAPI.Web;

namespace spotify_dl
{
    public static class PlaylistContext
    {
        public static ConcurrentDictionary<long, FullPlaylist> UserPlaylists = new ConcurrentDictionary<long, FullPlaylist>();
    }
}
