using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataBuilder.Models;
using DataBuilder.Scrapers;
using HtmlAgilityPack;
using Xunit;

namespace DataBuilder.Tests.Scrapers;

public class WikiDetailScraperTests
{
    [Fact]
    public void ParseInfobox_ExtractsLevel()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal((uint)50, result.Level);
    }

    [Fact]
    public void ParseInfobox_ExtractsLocationCoords()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal("The Waking Sands", result.LocationTerritoryName);
        Assert.Equal(6.0f, result.LocationMapX);
        Assert.Equal(4.9f, result.LocationMapY);
    }

    [Fact]
    public void ParseInfobox_ExtractsPrerequisites()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Single(result.PrerequisiteNames);
        Assert.Equal("The Navel (Hard)", result.PrerequisiteNames[0]);
    }

    [Fact]
    public void ParseInfobox_ExtractsEdbUrl()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal("https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/", result.EdbUrl);
    }

    [Fact]
    public async Task ScrapeDetailsAsync_EnrichesCategoryItems()
    {
        var handler = new MockHttpHandler(req =>
            File.ReadAllText("TestData/primal_awakening.html"));

        var http = new HttpClient(handler)
            { BaseAddress = new Uri("https://ffxiv.consolegameswiki.com") };
        var scraper = new WikiDetailScraper(http);

        var categoryItems = new List<CategoryItem>
        {
            new() { Name = "Primal Awakening", Category = "RaidSeries", Expansion = "ARR" }
        };

        var results = await scraper.ScrapeDetailsAsync(categoryItems);

        Assert.Single(results);
        Assert.Equal((uint)50, results[0].Level);
        Assert.Equal("ARR", results[0].Expansion);
        Assert.Equal("RaidSeries", results[0].Category);
        Assert.Equal("https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening", results[0].WikiUrl);
    }
}
