using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataBuilder.Models;
using HtmlAgilityPack;

namespace DataBuilder.Scrapers;

public sealed class WikiDetailScraper
{
    private const string WikiBase = "https://ffxiv.consolegameswiki.com";
    private static readonly Regex CoordRegex = new(
        @"\((?:X|x):\s*([\d.]+),\s*(?:Y|y):\s*([\d.]+)\)",
        RegexOptions.Compiled);

    private readonly HttpClient? _http;

    public WikiDetailScraper() { }

    public WikiDetailScraper(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<DetailItem>> ScrapeDetailsAsync(List<CategoryItem> categoryItems, int concurrency = 10)
    {
        if (_http == null) throw new InvalidOperationException("HttpClient not configured");
        var results = new List<DetailItem>();
        var semaphore = new SemaphoreSlim(concurrency);
        var tasks = categoryItems.Select(catItem => ScrapeOneAsync(_http!, catItem, results, semaphore));
        await Task.WhenAll(tasks);
        return results;
    }

    private async Task ScrapeOneAsync(HttpClient http, CategoryItem catItem, List<DetailItem> results, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var slug = catItem.Name.Replace(' ', '_');
            var url = $"{WikiBase}/wiki/{slug}";

            try
            {
                var html = await http.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var detail = ParseDetailPage(doc.DocumentNode, url);
                detail.Name = catItem.Name;
                detail.Category = catItem.Category;
                detail.Expansion = catItem.Expansion;

                lock (results) { results.Add(detail); }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"WARN: Failed to fetch {url}: {ex.Message}");
                lock (results)
                {
                    results.Add(new DetailItem
                    {
                        Name = catItem.Name,
                        Category = catItem.Category,
                        Expansion = catItem.Expansion,
                        WikiUrl = url
                    });
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public DetailItem ParseDetailPage(HtmlNode contentNode, string wikiUrl)
    {
        var item = new DetailItem { WikiUrl = wikiUrl };

        // Modern infobox: <div class="infobox-n quest"> <div class="wrapper"> <dl> <dt>...</dt><dd>...</dd> </dl> </div>
        var wrapper = contentNode.SelectSingleNode(".//div[contains(@class,'infobox-n')]//div[contains(@class,'wrapper')]//dl");

        if (wrapper == null)
        {
            // Fallback: old table-style infobox
            var oldInfobox = contentNode.SelectSingleNode(".//table[contains(@class,'infobox')]");
            if (oldInfobox != null)
                ParseTableInfobox(oldInfobox, item);
        }
        else
        {
            ParseDlInfobox(wrapper, item);
        }

        return item;
    }

    private void ParseTableInfobox(HtmlNode table, DetailItem item)
    {
        foreach (var row in table.SelectNodes(".//tr"))
        {
            var th = row.SelectSingleNode(".//th");
            var td = row.SelectSingleNode(".//td");
            if (th == null || td == null) continue;

            var label = th.InnerText.Trim().ToLowerInvariant();
            ProcessInfoboxField(label, td, item);
        }
    }

    private void ParseDlInfobox(HtmlNode dl, DetailItem item)
    {
        var currentDt = dl.SelectSingleNode(".//dt");
        while (currentDt != null)
        {
            var dd = currentDt.SelectSingleNode("following-sibling::dd[1]");
            if (dd == null) break;

            var label = currentDt.InnerText.Trim().ToLowerInvariant();
            ProcessInfoboxField(label, dd, item);

            currentDt = dd.SelectSingleNode("following-sibling::dt[1]");
        }
    }

    private void ProcessInfoboxField(string label, HtmlNode valueNode, DetailItem item)
    {
        var value = valueNode.InnerText.Trim();

        switch (label)
        {
            case "level":
                if (uint.TryParse(value, out var level))
                    item.Level = level;
                break;
            case "location":
                ParseLocation(value, item);
                break;
            case "requirements":
                ParsePrerequisites(valueNode, item);
                break;
            case "links":
                ParseLinks(valueNode, item);
                break;
        }
    }

    private void ParseLocation(string raw, DetailItem item)
    {
        var match = CoordRegex.Match(raw);
        if (match.Success)
        {
            var ampIdx = raw.IndexOf('&');
            var territoryName = ampIdx > 0
                ? raw[..ampIdx].Trim()
                : raw[..raw.IndexOf('(')].Trim();

            item.LocationTerritoryName = System.Web.HttpUtility.HtmlDecode(territoryName);
            if (float.TryParse(match.Groups[1].Value, out var x))
                item.LocationMapX = x;
            if (float.TryParse(match.Groups[2].Value, out var y))
                item.LocationMapY = y;
        }
        else
        {
            item.LocationTerritoryName = System.Web.HttpUtility.HtmlDecode(raw);
        }
    }

    private void ParsePrerequisites(HtmlNode td, DetailItem item)
    {
        var links = td.SelectNodes(".//a");
        if (links != null)
        {
            foreach (var link in links)
            {
                var name = System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(name))
                    item.PrerequisiteNames.Add(name);
            }
        }

        if (item.PrerequisiteNames.Count == 0)
        {
            var text = td.InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var cleaned = text.Replace(" cleared", "").Replace(" completed", "").Trim();
                item.PrerequisiteNames.Add(cleaned);
            }
        }
    }

    private static readonly Regex GtQuestIdRegex = new(
        @"/#quest/(\d+)",
        RegexOptions.Compiled);

    private void ParseLinks(HtmlNode td, DetailItem item)
    {
        var links = td.SelectNodes(".//a[contains(@class,'external')]");
        if (links == null) return;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "");
            if (href.Contains("lodestone") && href.Contains("/quest/"))
            {
                item.EdbUrl = href;
            }

            var gtMatch = GtQuestIdRegex.Match(href);
            if (gtMatch.Success && uint.TryParse(gtMatch.Groups[1].Value, out var gtId))
            {
                // Use GT ID as fallback QuestId (XIVAPI can refine it later)
                if (item.QuestId == null)
                    item.QuestId = gtId;
            }
        }
    }
}