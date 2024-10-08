# Twitch Clip Shoutout for Streamer.bot

This repository contains a C# script for Streamer.bot that enables automatic shoutouts with Twitch clips for streamers. 

There is also a version available for direct import. Simply copy the `shoutout.sakurascripts.sb` file into the import page of Streamer.bot.

If you would like to use this for your own channel, please consider following on twitch at https://www.twitch.tv/sakuragore and perhaps even making a test shoutout to `!so SakuraGore`!

You can also follow this at the [Streamer Bot Extensions site](https://extensions.streamer.bot/t/shoutout-clip-playing/1930).

## Features

- Automatically selects a random clip when shouting out a user
- Supports manual clip selection by providing a Twitch clip URL
- Integrates with OBS to display the clip and streamer name
- Uses Twitch's GraphQL API to retrieve clip information

## Requirements

- [Streamer.bot](https://streamer.bot/)
- [OBS](https://obsproject.com/)

## Setup

1. Copy the contents of `shoutout.cs` into a new C# Code action in Streamer.bot.
2. Set up the following global variables in Streamer.bot:
   - `ShoutOutScene`: The name of your OBS scene for shoutouts
   - `ShoutOutSource`: The name of your media source in OBS for playing clips
   - `ShoutOutText`: The name of your text source in OBS for displaying the streamer's name
   - `twitchClipUrl`: (Optional) Set this if you want to play a specific clip URL (works best when automatically parsed from Twitch Chat)

3. Create a command in Streamer.bot (e.g., !so or !shoutout) that triggers this action.
4. Add the relevant OBS scene and sources to your OBS. The scene that is defined in the actions is ShoutOuts. It is important to make sure that you add this scene and the sources defined below. You can then add this ShoutOuts scene to your normal scenes. That way you can have your shout outs in any scene you would normally be using. Of course, you can name it something else, you will just need to update the actions.

## Usage (if you used the import)

- Use `!so <username>` to shout out a user with a random clip
- If a chatter posts a Twitch clip URL, you can use that specific clip for the shoutout
- Use `!watch` to watch the last clip that was posted in chat
- Use `!stop` to stop the clip from playing

## Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](link-to-your-issues-page).

## License

[WTFPL](http://www.wtfpl.net/)

## Acknowledgements

- Thanks to the Streamer.bot community for their support and inspiration
- Twitch for providing the API that makes this possible
- https://vrflad.com/ for the original idea and some code snippets - specifically their ongoing work on https://vrflad.com/shoutout.html
