using System.Linq;
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
    public void ParseInfobox_SetsWikiUrl()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/primal_awakening.html");
        var scraper = new WikiDetailScraper();

        var result = scraper.ParseDetailPage(doc.DocumentNode, "https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening");

        Assert.Equal("https://ffxiv.consolegameswiki.com/wiki/Primal_Awakening", result.WikiUrl);
    }
}
