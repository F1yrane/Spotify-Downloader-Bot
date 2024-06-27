using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using spotify_dl.Services;

// Настройка конфигурации
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var telegramBotToken = configuration["TelegramBotToken"];
var spotifyClientId = configuration["SpotifyClientId"];
var spotifyClientSecret = configuration["SpotifyClientSecret"];

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    // Логируем запуск приложения
    Log.Information("Starting application");

    // Создание и настройка сервисов с логгированием через Serilog
    var spotifyService = new SpotifyService(spotifyClientId, spotifyClientSecret);
    var youtubeService = new YoutubeService();
    var telegramBotService = new TelegramBotService(telegramBotToken, spotifyService, youtubeService);

    // Запуск Telegram бота
    telegramBotService.Start();

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
catch (Exception ex)
{
    // Логируем исключения
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // Обеспечиваем очистку и остановку внутренних таймеров/потоков перед завершением приложения
    Log.CloseAndFlush();
}
