using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

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
                AllowedUpdates = [] // получать все типы апдейтов
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions
            );

            Console.WriteLine("Bot started.");
            Log.Information("Bot started.");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message;

            switch (message.Text)
            {
                case string text when text.StartsWith("https://open.spotify.com/playlist/"):
                    await HandlePlaylistLinkAsync(message);
                    break;
                case string text when text.StartsWith("https://open.spotify.com/track/"):
                    await HandleTrackLinkAsync(message);
                    break;
                case string text when text.StartsWith("https://open.spotify.com/album/"):
                    await HandleAlbumLinkAsync(message);
                    break;
                case string text when int.TryParse(text, out int trackNumber):
                    await HandleTrackNumberAsync(message, trackNumber);
                    break;
                case "/download":
                    await HandleDownloadCommandAsync(message);
                    break;
                default:
                    await SendWelcomeMessageAsync(message);
                    break;
            }
        }

        private async Task HandlePlaylistLinkAsync(Message message)
        {
            try
            {
                var playlistId = message.Text.Split('/').Last().Split('?').First();
                Log.Information("Received playlist ID: {PlaylistId}", playlistId);
                var playlist = await _spotifyService.GetPlaylistAsync(playlistId);

                await SendPlaylistMessageAsync(message.Chat.Id, playlist);
            }
            catch (ArgumentException ex)
            {
                Log.Error(ex, "Invalid playlist link");
                await SendErrorMessageAsync(
                    message.Chat.Id, 
                    "Sorry, I can't find the playlist on the link, maybe it's invalid," +
                    " try again and make sure it matches the input format.");
            }
        }

        private async Task HandleTrackLinkAsync(Message message)
        {
            try
            {
                var trackId = message.Text.Split('/').Last().Split('?').First();
                Log.Information("Received track ID: {TrackId}", trackId);
                var track = await _spotifyService.GetTrackAsync(trackId);

                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Track: {track.Name} - {track.Artists.First().Name}"
                );
            }
            catch (ArgumentException ex)
            {
                Log.Error(ex, "Invalid track link");
                await SendErrorMessageAsync(message.Chat.Id, 
                    "Sorry, I can't find the track on the link, maybe it's invalid," +
                    " try again and make sure it matches the input format.");
            }
        }

        private async Task HandleAlbumLinkAsync(Message message)
        {
            try
            {
                var albumId = message.Text.Split('/').Last().Split('?').First();
                Log.Information("Received album ID: {AlbumId}", albumId);
                var album = await _spotifyService.GetAlbumAsync(albumId);

                var trackList = album.Tracks.Items.Select((item, index) =>
                {
                    var track = item;
                    return $"{index + 1}. {track.Name} - {track.Artists.First().Name}";
                }).ToList();

                var trackMessage = string.Join("\n", trackList);

                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Album: {album.Name}\n{trackMessage}"
                );
            }
            catch (ArgumentException ex)
            {
                Log.Error(ex, "Invalid album link");
                await SendErrorMessageAsync(message.Chat.Id, "Sorry, I can't find the album on the link, maybe it's invalid, try again and make sure it matches the input format.");
            }
        }

        private async Task HandleTrackNumberAsync(Message message, int trackNumber)
        {
            if (PlaylistContext.UserPlaylists.TryGetValue(message.Chat.Id, out var playlist)
                && playlist.Tracks != null && playlist.Tracks.Items != null)
            {
                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Please wait until the track is loaded and sent to you"
                );

                try
                {
                    var track = (FullTrack)playlist.Tracks.Items[trackNumber - 1].Track;
                    var youtubeUrl = await _youtubeService.SearchYoutubeUrlAsync(track.Name, track.Artists.First().Name);

                    if (youtubeUrl != null)
                    {
                        var outputPath = $"{track.Name}.mp3";
                        await _youtubeService.DownloadTrackAsync(youtubeUrl, outputPath);

                        using var stream = File.OpenRead(outputPath);
                        var inputOnlineFile = new InputFileStream(stream, $"{track.Name}.mp3");

                        await _botClient.SendDocumentAsync(
                            message.Chat.Id,
                            inputOnlineFile
                        );

                        Log.Information("Sent track {TrackName} to user {UserId}", track.Name, message.Chat.Id);
                        stream.Close();
                        File.Delete(outputPath);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            message.Chat.Id,
                            "The track was not found in free sources."
                        );
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Log.Error(ex, "Track number {TrackNumber} not found in playlist", trackNumber);
                    await _botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        $"Sorry, but there is no track number {trackNumber} in the playlist"
                    );
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Please send me the playlist link first."
                );
            }
        }

        private async Task HandleDownloadCommandAsync(Message message)
        {
            if (PlaylistContext.UserPlaylists.TryGetValue(message.Chat.Id, out var playlist)
                && playlist.Tracks != null && playlist.Tracks.Items != null)
            {
                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Please wait while the playlist is downloaded and archived, " +
                    "it may take some time depending on the number of tracks in the playlist.\n" +
                    "The approximate loading time of one track ~ 30 seconds."
                );

                try
                {
                    var trackFiles = new List<string>();

                    foreach (var item in playlist.Tracks.Items)
                    {
                        var track = (FullTrack)item.Track;
                        var youtubeUrl = await _youtubeService.SearchYoutubeUrlAsync(track.Name, track.Artists.First().Name);

                        if (youtubeUrl != null)
                        {
                            var outputPath = $"{track.Name}.mp3";
                            await _youtubeService.DownloadTrackAsync(youtubeUrl, outputPath);
                            trackFiles.Add(outputPath);
                        }
                    }

                    var zipPath = $"{playlist.Name}.zip";
                    using (var zipStream = new FileStream(zipPath, FileMode.Create))
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        foreach (var trackFile in trackFiles)
                        {
                            archive.CreateEntryFromFile(trackFile, Path.GetFileName(trackFile));
                        }
                    }

                    using var stream = File.OpenRead(zipPath);
                    var inputOnlineFile = new InputFileStream(stream, $"{playlist.Name}.zip");
                    await _botClient.SendDocumentAsync(
                        message.Chat.Id,
                        inputOnlineFile
                    );

                    Log.Information("Sent playlist {PlaylistName} to user {UserId}", playlist.Name, message.Chat.Id);
                    stream.Close();
                    File.Delete(zipPath);
                    trackFiles.ForEach(File.Delete);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while downloading the playlist.");
                    await _botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "An error occurred while downloading the playlist."
                    );
                }
            }
        }

        private async Task SendWelcomeMessageAsync(Message message)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Hey, {message.Chat.FirstName} 👋\n" +
                $"\n1) To get started, copy the URL link to the spotify playlist in the format ( https://open.spotify.com/playlist/ ) and paste it into the chat\n" +
                $"\n2) After a while, you will receive a list from the playlist, you can send the /download command to download all tracks in a ZIP-Archive\n" +
                $"\n3) Or send the track number to download a specific track from the list"
            );

            Log.Information("Sent welcome message to user {UserId}", message.Chat.Id);
        }

        private async Task SendPlaylistMessageAsync(long chatId, FullPlaylist playlist)
        {
            if (playlist.Tracks != null && playlist.Tracks.Items != null)
            {
                var trackList = playlist.Tracks.Items.Select((item, index) =>
                {
                    var track = (FullTrack)item.Track;
                    return $"{index + 1}. {track.Name} - {track.Artists.First().Name}";
                }).ToList();

                var trackMessage = string.Join("\n", trackList);

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Playlist: {playlist.Name}\n{trackMessage}"
                );

                PlaylistContext.UserPlaylists[chatId] = playlist;
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Playlist: {playlist.Name}\n{"Sorry, no tracks found in the playlist."}"
                );
            }
        }

        private async Task SendErrorMessageAsync(long chatId, string errorMessage)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                errorMessage
            );
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error(exception, "Error occurred in Telegram Bot");
            return Task.CompletedTask;
        }
    }
}
