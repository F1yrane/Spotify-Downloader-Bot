using System;
using Microsoft.Extensions.Configuration;
using spotify_dl.Services;

var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

var telegramBotToken = configuration["TelegramBotToken"];
var spotifyClientId = configuration["SpotifyClientId"];
var spotifyClientSecret = configuration["SpotifyClientSecret"];

var spotifyService = new SpotifyService(spotifyClientId, spotifyClientSecret);
var youtubeService = new YoutubeService();
var telegramBotService = new TelegramBotService(telegramBotToken, spotifyService, youtubeService);

telegramBotService.Start();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
