using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class CPHInline
{
    public bool Execute()
    {
        // Retrieve global variables
        var scene = CPH.GetGlobalVar<string>("ShoutOutScene", true);
        var source = CPH.GetGlobalVar<string>("ShoutOutSource", true);
        var text = CPH.GetGlobalVar<string>("ShoutOutText", true);
        var watchUrl = CPH.GetGlobalVar<string>("twitchClipUrl", true);

        string slug;
        string userName;
        float duration;

        // Check if a specific clip URL is provided (will be set if a chatter posts a clip URL)
        if (!string.IsNullOrEmpty(watchUrl) && !string.IsNullOrWhiteSpace(watchUrl))
        {
            (slug, userName, duration) = ParseWatchUrl(watchUrl);
        }
        else
        {
            // If no URL is provided, select a random clip for the user provided in the shoutout command
            (slug, userName, duration) = SelectRandomClip();
        }

        // If no valid clip is found, exit
        if (string.IsNullOrEmpty(slug))
        {
            CPH.SendMessage("I couldn't find that clip! Sadge");
            return false;
        }

        // Retrieve clip information using GraphQL
        var (sourceUrl, signature, token) = GetClipInfo(slug).Result;

        if (string.IsNullOrEmpty(sourceUrl))
        {
            CPH.LogError("SAKURA - SO - Failed to retrieve clip info from GraphQL");
            CPH.SendMessage("I couldn't find that clip! Sadge");
            return false;
        }

        // Log clip information
        LogClipInfo(sourceUrl, signature, token, userName, duration);

        // Play the clip in OBS
        PlayClip(scene, source, text, sourceUrl, signature, token, userName, duration);

        return true;
    }

    // Parse the provided Twitch clip URL
    private (string slug, string userName, float duration) ParseWatchUrl(string watchUrl)
    {
        // Regular expression to extract slug and username from Twitch clip URL
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:clips\.twitch\.tv\/|twitch\.tv\/(?<userName>[^\/]+)\/clip\/)(?<slug>[^?\s]+)");
        var match = regex.Match(watchUrl);

        if (match.Success)
        {
            string slug = match.Groups["slug"].Value;
            string userName = match.Groups["userName"].Success ? match.Groups["userName"].Value : "";
            CPH.LogInfo($"SAKURA - SO - Parsed URL - UserName: {userName}, Slug: {slug}");

            float duration = FindClipDuration(userName, slug);

            if (duration == 0)
            {
                CPH.SendMessage("I couldn't find that clip! Sadge");
                CPH.LogError($"SAKURA - SO - Failed to find duration for clip: {slug}");
                return (null, null, 0);
            }

            return (slug, userName, duration);
        }
        else
        {
            CPH.SendMessage("I couldn't find that clip! Sadge");
            CPH.LogError($"SAKURA - SO - Failed to parse watch URL: {watchUrl}");
            return (null, null, 0);
        }
    }

    // Find the duration of a specific clip
    private float FindClipDuration(string userName, string slug)
    {
        var allClips = CPH.GetClipsForUser(userName);
        foreach (var clip in allClips)
        {
            if (clip.Id == slug)
            {
                return clip.Duration;
            }
        }
        return 0;
    }

    // Select a random clip for the shouted out user
    private (string slug, string userName, float duration) SelectRandomClip()
    {
        string userName = args["targetUser"].ToString();
        CPH.LogInfo($"SAKURA - SO - For: {userName}");

        int maxDays = Int16.Parse(args["clipsWithinDays"].ToString());
        DateTime now = DateTime.Now;
        DateTime startdate = now.AddDays(-maxDays);

        // Try to get clips within the specified time range
        var allClips = CPH.GetClipsForUser(userName, startdate, now);
        if (allClips.Count == 0)
        {
            // If no clips found, get all clips for the user
            allClips = CPH.GetClipsForUser(userName);
        }
        CPH.LogInfo($"SAKURA - SO - Clip count {allClips.Count}");

        if (allClips.Count == 0)
        {
            CPH.SendMessage("This streamer doesn't have any clips! Sadge");
            return (null, null, 0);
        }

        // Select a random clip
        Random randomNumber = new Random();
        int clipId = randomNumber.Next(0, allClips.Count);
        CPH.LogInfo($"SAKURA - SO - Clip ID: {clipId}");
        var clip = allClips[clipId];

        // Log clip details
        CPH.LogInfo($"SAKURA - SO - Matched URL: {clip.Url}");
        CPH.LogInfo($"SAKURA - SO - Video ID: {clip.VideoId}");
        CPH.LogInfo($"SAKURA - SO - Clip ID: {clip.Id}");
        CPH.LogInfo($"SAKURA - SO - Thumbnail URL: {clip.ThumbnailUrl}");

        return (clip.Id, userName, clip.Duration);
    }

    // Retrieve clip information using GraphQL
    private async Task<(string sourceUrl, string signature, string token)> GetClipInfo(string clipId)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");

            var query = BuildGraphQLQuery(clipId);
            CPH.LogInfo($"SAKURA - SO - GraphQL Query: {query}");

            var content = new StringContent(query, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://gql.twitch.tv/gql", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            CPH.LogInfo($"SAKURA - SO - GraphQL Response: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                return ParseGraphQLResponse(responseBody);
            }
            else
            {
                CPH.LogError($"SAKURA - SO - GraphQL request failed with status code: {response.StatusCode}");
                CPH.LogError($"SAKURA - SO - Response content: {responseBody}");
                return (null, null, null);
            }
        }
    }

    // Build the GraphQL query string
    private string BuildGraphQLQuery(string clipId)
    {
        return @"{
            ""operationName"": ""VideoAccessToken_Clip"",
            ""variables"": {
                ""slug"": """ + clipId + @"""
            },
            ""extensions"": {
                ""persistedQuery"": {
                    ""version"": 1,
                    ""sha256Hash"": ""36b89d2507fce29e5ca551df756d27c1cfe079e2609642b4390aa4c35796eb11""
                }
            }
        }";
    }

    // Parse the GraphQL response to extract necessary information
    private (string sourceUrl, string signature, string token) ParseGraphQLResponse(string responseBody)
    {
        var jsonResponse = JObject.Parse(responseBody);

        var videoQualities = jsonResponse["data"]["clip"]["videoQualities"] as JArray;
        var sourceUrlIndex = videoQualities != null && videoQualities.Count > 2 ? 2 : 0;
        var sourceUrl = videoQualities[sourceUrlIndex]["sourceURL"].ToString();
        CPH.LogInfo($"SAKURA - SO - Selected video source URL: {sourceUrl}");

        var playbackAccessToken = jsonResponse["data"]["clip"]["playbackAccessToken"];
        var signature = playbackAccessToken["signature"].ToString();
        var token = playbackAccessToken["value"].ToString();

        return (sourceUrl, signature, token);
    }

    // Log clip information
    private void LogClipInfo(string sourceUrl, string signature, string token, string userName, float duration)
    {
        CPH.LogInfo($"SAKURA - SO - Source URL: {sourceUrl}");
        CPH.LogInfo($"SAKURA - SO - Signature: {signature}");
        CPH.LogInfo($"SAKURA - SO - Token: {token}");
        CPH.LogInfo($"SAKURA - SO - UserName: {userName}");
        CPH.LogInfo($"SAKURA - SO - Duration: {duration}");
    }

    // Play the clip in OBS
    private void PlayClip(string scene, string source, string text, string sourceUrl, string signature, string token, string userName, float duration)
    {
        int delay = (int)(duration * 1000);
        var url = sourceUrl + "?token=" + Uri.EscapeDataString(token) + "&sig=" + signature;

        CPH.LogInfo($"SAKURA - SO - Final built SO URL: {url}");

        // Set up OBS scene
        CPH.ObsSetSourceVisibility(scene, source, false);
        CPH.ObsSetSourceVisibility(scene, text, false);
        CPH.ObsSetMediaSourceFile(scene, source, url);
        CPH.Wait(500);
        CPH.ObsSetGdiText(scene, text, userName);
        CPH.ObsSetSourceVisibility(scene, source, true);
        CPH.ObsSetSourceVisibility(scene, text, true);
        
        // Wait for clip duration
        CPH.Wait(delay);
        
        // Clean up OBS scene
        CPH.ObsSetSourceVisibility(scene, source, false);
        CPH.ObsSetSourceVisibility(scene, text, false);
        CPH.ObsSetMediaSourceFile(scene, source, "");
    }
}
