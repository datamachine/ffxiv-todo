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

    public async Task<List<DetailItem>> ScrapeDetailsAsync(List<CategoryItem> categoryItems)
    {
        if (_http == null) throw new InvalidOperationException("HttpClient not configured");

        var results = new List<DetailItem>();

        foreach (var catItem in categoryItems)
        {
            var slug = System.Web.HttpUtility.UrlEncode(catItem.Name.Replace(' ', '_'));
            var url = $"{WikiBase}/wiki/{slug}";

            try
            {
                var html = await _http.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var detail = ParseDetailPage(doc.DocumentNode, url);
                detail.Name = catItem.Name;
                detail.Category = catItem.Category;
                detail.Expansion = catItem.Expansion;

                results.Add(detail);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"WARN: Failed to fetch {url}: {ex.Message}");
                results.Add(new DetailItem
                {
                    Name = catItem.Name,
                    Category = catItem.Category,
                    Expansion = catItem.Expansion,
                    WikiUrl = url
                });
            }

            await Task.Delay(1000);
        }

        return results;
    }

    public DetailItem ParseDetailPage(HtmlNode contentNode, string wikiUrl)
    {
        var item = new DetailItem { WikiUrl = wikiUrl };

        var infobox = contentNode.SelectSingleNode(".//table[contains(@class,'infobox')]");

        if (infobox != null)
        {
            foreach (var row in infobox.SelectNodes(".//tr"))
            {
                var th = row.SelectSingleNode(".//th");
                var td = row.SelectSingleNode(".//td");
                if (th == null || td == null) continue;

                var label = th.InnerText.Trim().ToLowerInvariant();
                var value = td.InnerText.Trim();

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
                        ParsePrerequisites(td, item);
                        break;
                    case "links":
                        ParseLinks(td, item);
                        break;
                }
            }
        }

        return item;
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

    private void ParseLinks(HtmlNode td, DetailItem item)
    {
        var links = td.SelectNodes(".//a[contains(@class,'external')]");
        if (links == null) return;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "");
            if (href.Contains("lodestone") && href.Contains("/quest/"))
                item.EdbUrl = href;
        }
    }
}