using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DataBuilder.Models;
using HtmlAgilityPack;

namespace DataBuilder.Scrapers;

public sealed class WikiCategoryScraper
{
    private readonly HttpClient _http;

    private static readonly Dictionary<string, string> KnownCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Job_Quests"] = "JobQuest",
        ["Raids"] = null!, // Handled specially — contains multiple subcategories
        ["Allied_Society_Quests"] = "BeastTribe",
        ["Side_Quests"] = "SideQuest",
        ["Feature_Quests"] = null!, // Handled specially — contains BlueUnlock+
        ["Custom_Deliveries"] = "CustomDelivery",
    };

    private static readonly Dictionary<string, string> ExpansionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a realm reborn"] = "ARR",
        ["heavensward"] = "HW",
        ["stormblood"] = "SB",
        ["shadowbringers"] = "ShB",
        ["endwalker"] = "EW",
        ["dawntrail"] = "DT",
    };

    public WikiCategoryScraper(HttpClient http)
    {
        _http = http;
    }

    public List<CategoryItem> ParseJobQuestTable(HtmlNode contentNode, string expansion)
    {
        var items = new List<CategoryItem>();
        var expShort = ParseExpansionFromHeading(expansion) ?? expansion;

        var h3Tags = contentNode.SelectNodes(".//h3");
        if (h3Tags == null) return items;

        foreach (var h3 in h3Tags)
        {
            var span = h3.SelectSingleNode(".//span[@id]");
            if (span == null) continue;

            var current = h3;
            HtmlNode? table = null;
            while ((current = current.NextSibling) != null)
            {
                if (current.Name == "table" && current.GetAttributeValue("class", "").Contains("questlist"))
                {
                    table = current;
                    break;
                }
                if (current.Name == "h3" || current.Name == "h2")
                    break;
            }

            if (table == null) continue;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 1) continue;

                var link = cells[0].SelectSingleNode(".//a");
                if (link == null) continue;

                var name = System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim());
                items.Add(new CategoryItem
                {
                    Name = name,
                    Category = "JobQuest",
                    Expansion = expShort
                });
            }
        }

        return items;
    }

    public static string? ParseExpansionFromHeading(string heading)
    {
        return ExpansionMap.TryGetValue(heading, out var value) ? value : null;
    }

    public List<CategoryItem> ParseRaidsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentSection = string.Empty;
        var currentExpansion = string.Empty;

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var span = heading.SelectSingleNode(".//span[@id]");
            if (span == null) continue;
            var sectionId = span.GetAttributeValue("id", "");

            if (sectionId == "Normal_Raids") { currentSection = "RaidSeries"; continue; }
            if (sectionId == "Alliance_Raids") { currentSection = "AllianceRaid"; continue; }
            if (sectionId.StartsWith("Savage_Raids") || sectionId.StartsWith("Ultimate_Raids"))
            { currentSection = string.Empty; continue; }
            if (sectionId == "Chaotic_Alliance_Raids" || sectionId.StartsWith("Field_Operations"))
            { currentSection = string.Empty; continue; }

            var exp = ParseExpansionFromHeading(System.Text.RegularExpressions.Regex.Replace(sectionId.Replace("_", " "), @" \d+$", ""));
            if (exp != null)
            {
                currentExpansion = exp;
            }
            else if (string.IsNullOrEmpty(currentSection))
            {
                continue;
            }

            var current = heading;
            HtmlNode? table = null;
            while ((current = current.NextSibling) != null)
            {
                if (current.Name == "table") { table = current; break; }
                if (current.Name == "h2" || current.Name == "h3") break;
            }

            if (table == null) continue;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 1) continue;

                var firstCol = cells[0];
                var link = firstCol.SelectSingleNode(".//a");
                var dutyName = link != null
                    ? System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim())
                    : System.Web.HttpUtility.HtmlDecode(firstCol.InnerText.Trim());

                if (string.IsNullOrWhiteSpace(dutyName)) continue;

                items.Add(new CategoryItem
                {
                    Name = dutyName,
                    Category = currentSection,
                    Expansion = currentExpansion
                });
            }
        }

        return items;
    }

    public List<CategoryItem> ParseAlliedSocietyPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentExpansion = string.Empty;

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var span = heading.SelectSingleNode(".//span[@id]");
            if (span == null) continue;
            var sectionId = span.GetAttributeValue("id", "");

            var cleaned = System.Text.RegularExpressions.Regex.Replace(sectionId.Replace("_", " "), @" (Allied Societies|Daily Quests)$", "");
            var exp = ParseExpansionFromHeading(cleaned);
            if (exp != null)
            {
                currentExpansion = exp;
                continue;
            }

            var para = heading.NextSibling;
            while (para != null && para.Name != "p" && para.Name != "h2" && para.Name != "h3")
                para = para.NextSibling;

            if (para == null || para.Name != "p") continue;

            var links = para.SelectNodes(".//a[contains(@href,'/wiki/')]");
            if (links == null) continue;

            foreach (var link in links)
            {
                var questName = System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim());
                if (string.IsNullOrWhiteSpace(questName)) continue;
                items.Add(new CategoryItem
                {
                    Name = questName,
                    Category = "BeastTribe",
                    Expansion = currentExpansion
                });
                break;
            }
        }

        return items;
    }

    public List<CategoryItem> ParseFeatureQuestsPage(HtmlNode contentNode, string defaultExpansion)
    {
        var items = new List<CategoryItem>();
        var currentExpansion = ParseExpansionFromHeading(defaultExpansion) ?? defaultExpansion;
        var currentCategory = "BlueUnlock";

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var span = heading.SelectSingleNode(".//span[@id]");
            if (span == null) continue;
            var sectionId = span.GetAttributeValue("id", "");

            var exp = ParseExpansionFromHeading(System.Text.RegularExpressions.Regex.Replace(sectionId.Replace("_", " "), @" (Instances|Dungeons)$", ""));
            if (exp != null) { currentExpansion = exp; continue; }

            if (sectionId.Contains("Class") || sectionId.Contains("Job") || sectionId.Contains("Role"))
            { currentCategory = string.Empty; continue; }
            if (sectionId.Contains("Chronicles") || sectionId.Contains("Trials")
                || sectionId.Contains("Normal_Raids") || sectionId.Contains("Alliance_Raids"))
            { currentCategory = string.Empty; continue; }

            currentCategory = "BlueUnlock";

            var current = heading;
            HtmlNode? table = null;
            while ((current = current.NextSibling) != null)
            {
                if (current.Name == "table") { table = current; break; }
                if (current.Name == "h2" || current.Name == "h3") break;
            }

            if (table == null || string.IsNullOrEmpty(currentCategory)) continue;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 1) continue;

                var link = cells[0].SelectSingleNode(".//a");
                if (link == null) continue;

                var name = System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim());
                items.Add(new CategoryItem
                {
                    Name = name,
                    Category = currentCategory,
                    Expansion = currentExpansion
                });
            }
        }

        return items;
    }

    private static readonly string WikiBase = "https://ffxiv.consolegameswiki.com";

    public async Task<List<CategoryItem>> ScrapeAllAsync()
    {
        var allItems = new List<CategoryItem>();

        // Job Quests — each h2 section is an expansion
        var jobItems = await FetchAndParseAsync("/wiki/Job_Quests", doc =>
        {
            var expHeads = doc.DocumentNode.SelectNodes(".//h2");
            var result = new List<CategoryItem>();
            if (expHeads != null)
            {
                foreach (var h2 in expHeads)
                {
                    var span = h2.SelectSingleNode(".//span[@id]");
                    if (span == null) continue;
                    var exp = ParseExpansionFromHeading(
                        System.Text.RegularExpressions.Regex.Replace(
                            span.GetAttributeValue("id", "").Replace("_", " "),
                            @"_\d+$", ""));
                    if (exp != null)
                        result.AddRange(ParseJobQuestTable(doc.DocumentNode, exp));
                }
            }
            if (result.Count == 0)
                result.AddRange(ParseJobQuestTable(doc.DocumentNode, "ARR"));
            return result;
        });
        allItems.AddRange(jobItems);

        // Raids (Normal + Alliance)
        var raidItems = await FetchAndParseAsync("/wiki/Raids", doc =>
            ParseRaidsPage(doc.DocumentNode));
        allItems.AddRange(raidItems);

        // Beast Tribes
        var beastItems = await FetchAndParseAsync("/wiki/Allied_Society_Quests", doc =>
            ParseAlliedSocietyPage(doc.DocumentNode));
        allItems.AddRange(beastItems);

        // Feature Quests
        var blueItems = await FetchAndParseAsync("/wiki/Feature_Quests", doc =>
        {
            var items = new List<CategoryItem>();
            foreach (var exp in new[] { "ARR", "HW", "SB", "ShB", "EW", "DT" })
                items.AddRange(ParseFeatureQuestsPage(doc.DocumentNode, exp));
            return items;
        });
        allItems.AddRange(blueItems);

        // Custom Deliveries
        var deliveryItems = await FetchAndParseAsync("/wiki/Custom_Deliveries", doc =>
        {
            var items = new List<CategoryItem>();
            foreach (var exp in new[] { "HW", "SB", "ShB", "EW", "DT" })
                items.AddRange(ParseFeatureQuestsPage(doc.DocumentNode, exp));
            return items;
        });
        allItems.AddRange(deliveryItems);

        // Side Quests
        var sideItems = await FetchAndParseAsync("/wiki/Side_Quests", doc =>
            ParseFeatureQuestsPage(doc.DocumentNode, "ARR"));
        allItems.AddRange(sideItems);

        return allItems.DistinctBy(i => i.Name).ToList();
    }

    private async Task<List<CategoryItem>> FetchAndParseAsync(
        string path, Func<HtmlDocument, List<CategoryItem>> parser)
    {
        var html = await _http.GetStringAsync($"{WikiBase}{path}");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return parser(doc);
    }
}