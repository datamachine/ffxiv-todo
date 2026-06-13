using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataBuilder.Models;

namespace DataBuilder.Scrapers;

public sealed class XivApiResolver
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, uint> _territoryCache = new();
    private readonly Dictionary<string, uint> _achievementCache = new();

    private static readonly Regex EdbQuestIdRegex = new(
        @"/quest/(\d+)/",
        RegexOptions.Compiled);

    private const string XivApiBase = "https://xivapi.com";

    public XivApiResolver(HttpClient http)
    {
        _http = http;
    }

    public static uint? ExtractQuestIdFromEdbUrl(string? url)
    {
        if (url == null) return null;
        var match = EdbQuestIdRegex.Match(url);
        if (match.Success && uint.TryParse(match.Groups[1].Value, out var id))
            return id;
        return null;
    }

    public async Task ResolveAsync(DetailItem item)
    {
        if (item.QuestId == null && item.EdbUrl != null)
            item.QuestId = ExtractQuestIdFromEdbUrl(item.EdbUrl);

        if (item.QuestId == null)
            item.QuestId = await SearchXivApiAsync("quest", item.Name);

        if (item.AchievementId == null)
        {
            var achName = DeriveAchievementName(item.Name, item.Category);
            item.AchievementId = await SearchXivApiAsync("achievement", achName);
        }

        if (item.LocationTerritoryId == null && item.LocationTerritoryName != null)
        {
            if (!_territoryCache.TryGetValue(item.LocationTerritoryName, out var terrId))
            {
                terrId = (await SearchXivApiAsync("territorytype", item.LocationTerritoryName)) ?? 0;
                if (terrId > 0) _territoryCache[item.LocationTerritoryName] = terrId;
            }
            item.LocationTerritoryId = terrId > 0 ? terrId : null;
        }
    }

    private async Task<uint?> SearchXivApiAsync(string index, string name)
    {
        try
        {
            var url = $"{XivApiBase}/search?string={Uri.EscapeDataString(name)}&indexes={index}&limit=1";
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                if (first.TryGetProperty("ID", out var idEl))
                    return idEl.GetUInt32();
            }
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"WARN: XIVAPI search failed for {index}/{name}: {ex.Message}");
        }

        return null;
    }

    private static string DeriveAchievementName(string contentName, string category)
    {
        return category switch
        {
            "RaidSeries" or "AllianceRaid" or "TrialSeries"
                => $"Mapping the Realm: {contentName}",
            _ => contentName
        };
    }
}
