using System;
using System.Collections.Generic;
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

    public static string? ParseExpansionFromHeading(string heading)
    {
        return ExpansionMap.TryGetValue(heading, out var value) ? value : null;
    }
}