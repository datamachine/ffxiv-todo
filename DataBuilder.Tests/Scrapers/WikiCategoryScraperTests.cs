using DataBuilder.Scrapers;
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
}