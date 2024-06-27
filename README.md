# Spotify Downloader Bot
Is a bot that allows users to download Spotify playlists from YouTube in MP3 format. 

This is a tool for those who want to save their favorite music for offline listening.


## Features
* **Track Downloading**: Allows downloading any track from Spotify via link.
* **High-Quality Audio**: Downloads tracks in highest possible quality (128 kbps).
* **Simple Interface**: Intuitive and user-friendly interface.
* **Playlist Support**: Capability to download entire playlists & albums.


## Installation 
To get started with the bot, follow these steps:
1. **Clone the repository**:
```bash
git clone https://github.com/F1yrane/Spotify-Downloader-Bot.git
```
`cd Spotify-Downloader-Bot`
2. **Install the required libraries**:
```bash
dotnet add package SpotifyApi.Web
dotnet add package System.IO.Compression
dotnet add package Telegram.Bot
dotnet add package YouTubeExplode
dotnet add package YoutubeExplodeConverter
```
3. **Installing FFmpeg**: FFmpeg is required for bot. If using FFmpeg only for bot,
you can install FFmpeg essentials as it is shown here [Windows Tutorial](https://windowsloop.com/install-ffmpeg-windows-10/) , 
you can just add ffmpeg.exe into the build path or set to system environment variables.
4. **Configure appsettings.json**: Add your Spotify Web API keys and Telegram Bot Token
   [Spotify Developer](https://developer.spotify.com/), [BotFather](https://t.me/BotFather)

## Usage
Build & run the application and go to your telegram bot, there you will receive instructions on how to use the functionality

 
