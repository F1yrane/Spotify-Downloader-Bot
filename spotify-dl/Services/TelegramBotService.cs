using System;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace spotify_dl.Services
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly SpotifyService _spotifyService;
        private readonly YoutubeService _youtubeService;

        public TelegramBotService(string token, SpotifyService spotifyService, YoutubeService youtubeService)
        {
            _botClient = new TelegramBotClient(token);
            _spotifyService = spotifyService;
            _youtubeService = youtubeService;
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions
            );

            Console.WriteLine("Bot started.");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message;

            if (message.Text.StartsWith("https://open.spotify.com/playlist/"))
            {
                var playlistId = message.Text.Split('/').Last().Split('?').First();
                var playlist = await _spotifyService.GetPlaylistAsync(playlistId);

                if (playlist.Tracks != null && playlist.Tracks.Items != null)
                {
                    var trackList = playlist.Tracks.Items.Select((item, index) =>
                    {
                        var track = (FullTrack)item.Track;
                        return $"{index + 1}. {track.Name} - {track.Artists.First().Name}";
                    }).ToList();
                    var trackMessage = string.Join("\n", trackList);

                    await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Playlist: {playlist.Name}\n{trackMessage}"
                    );

                    // Сохраняем контекст плейлиста для последующего выбора треков
                    PlaylistContext.UserPlaylists[message.Chat.Id] = playlist;
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Playlist: {playlist.Name}\n{"Sorry, no tracks found in the playlist"}"
                    );
                }
            }
            else if (int.TryParse(message.Text, out int trackNumber))
            {
                if (PlaylistContext.UserPlaylists.TryGetValue(message.Chat.Id, out var playlist) 
                    && playlist.Tracks != null && playlist.Tracks.Items != null)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Please wait until the track is loaded and sent to you");

                    try
                    {
                        var track = (FullTrack)playlist.Tracks.Items[trackNumber - 1].Track;
                        var youtubeUrl = await _youtubeService.SearchYoutubeUrlAsync(track.Name, track.Artists.First().Name);

                        if (youtubeUrl != null)
                        {
                            var outputPath = $"{track.Name}.mp3";
                            await _youtubeService.DownloadTrackAsync(youtubeUrl, outputPath);

                            using var stream = System.IO.File.OpenRead(outputPath);
                            var inputOnlineFile = new InputFileStream(stream, $"{track.Name}.mp3");
                            await _botClient.SendDocumentAsync(message.Chat.Id, inputOnlineFile);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "The track was not found in free sources.");
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        await _botClient.SendTextMessageAsync(message.Chat.Id, $"Sorry, but there is no track number {trackNumber} in the playlist");
                    }

                }
                else 
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "The track was not found in the sent playlist.");
                }
            }
	        else
	        {
	           await _botClient.SendTextMessageAsync(message.Chat.Id, $"Hey, {message.Chat.FirstName} 👋\n" +
                   $"\n1) To get started, copy the URL link to the spotify playlist in the format ( https://open.spotify.com/playlist/ ) and paste it into the chat\n" +
                   $"\n2) After a while, you will recieve a list from the playlist, you can send the /download command to download all tracks in a ZIP-Archive\n" +
            	   $"\n3) Or send the track number to download a specific track from the list");
	        }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            return Task.CompletedTask;
        }
    }
}