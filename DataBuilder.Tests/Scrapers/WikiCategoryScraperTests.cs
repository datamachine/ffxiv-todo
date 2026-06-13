using System.Linq;
using DataBuilder.Scrapers;
using HtmlAgilityPack;
using Xunit;

namespace DataBuilder.Tests.Scrapers;

public class WikiCategoryScraperTests
{
    [Fact]
    public void ParseExpansionFromHeading_ReturnsCorrectExpansion()
    {
        Assert.Equal("ARR", WikiCategoryScraper.ParseExpansionFromHeading("A Realm Reborn"));
        Assert.Equal("HW", WikiCategoryScraper.ParseExpansionFromHeading("Heavensward"));
        Assert.Equal("SB", WikiCategoryScraper.ParseExpansionFromHeading("Stormblood"));
        Assert.Equal("ShB", WikiCategoryScraper.ParseExpansionFromHeading("Shadowbringers"));
        Assert.Equal("EW", WikiCategoryScraper.ParseExpansionFromHeading("Endwalker"));
        Assert.Equal("DT", WikiCategoryScraper.ParseExpansionFromHeading("Dawntrail"));
    }

    [Fact]
    public void ParseExpansionFromHeading_UnknownHeading_ReturnsNull()
    {
        Assert.Null(WikiCategoryScraper.ParseExpansionFromHeading("Some Unknown Section"));
    }

    [Fact]
    public void ParseExpansionFromHeading_CaseInsensitive()
    {
        Assert.Equal("ARR", WikiCategoryScraper.ParseExpansionFromHeading("a realm reborn"));
    }

    [Fact]
    public void ParseJobQuestTable_ExtractsPaladinQuests_WithExpansionHW()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/job_quests_paladin.html");
        var scraper = new WikiCategoryScraper(null!);

        var items = scraper.ParseJobQuestTable(doc.DocumentNode, "Heavensward");

        Assert.Equal(3, items.Count);
        Assert.Equal("Paladin's Pledge", items[0].Name);
        Assert.Equal("JobQuest", items[0].Category);
        Assert.Equal("HW", items[0].Expansion);
        Assert.Equal("Honor Lost", items[1].Name);
        Assert.Equal("Power Struggles", items[2].Name);
    }

    [Fact]
    public void ParseRaidsPage_ExtractsNormalAndAllianceRaids()
    {
        var doc = new HtmlDocument();
        doc.Load("TestData/raids_page.html");
        var scraper = new WikiCategoryScraper(null!);

        var items = scraper.ParseRaidsPage(doc.DocumentNode);

        Assert.Equal(3, items.Count);

        var bahamut = items.First(i => i.Name == "The Binding Coil of Bahamut");
        Assert.Equal("RaidSeries", bahamut.Category);
        Assert.Equal("ARR", bahamut.Expansion);

        var alexander = items.First(i => i.Name == "Alexander: Gordias");
        Assert.Equal("RaidSeries", alexander.Category);
        Assert.Equal("HW", alexander.Expansion);

        var labyrinth = items.First(i => i.Name == "The Labyrinth of the Ancients");
        Assert.Equal("AllianceRaid", labyrinth.Category);
        Assert.Equal("ARR", labyrinth.Expansion);
    }
}