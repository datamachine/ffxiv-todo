using System;
using System.Collections.Generic;
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
}