using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DataBuilder.Data;
using DataBuilder.Models;
using DataBuilder.Scrapers;

namespace DataBuilder.Tests.Scrapers;

public sealed class CsvEnricherTests
{
    [Fact]
    public async Task Enrich_FoundQuest_PopulatesDetailItem()
    {
        var provider = await CreateProviderAsync();
        var enricher = new CsvEnricher(provider, "Cache");

        var categoryItems = new List<CategoryItem>
        {
            new() { Name = "Hallo Halatali", Category = "JobQuest", Expansion = "ARR" }
        };

        var results = enricher.Enrich(categoryItems);

        Assert.Single(results);
        var item = results[0];
        Assert.Equal("Hallo Halatali", item.Name);
        Assert.Equal("JobQuest", item.Category);
        Assert.NotNull(item.QuestId);
        Assert.True(item.QuestId > 0);
    }

    [Fact]
    public async Task Enrich_NotFound_Skipped()
    {
        var provider = await CreateProviderAsync();
        var enricher = new CsvEnricher(provider, "Cache");

        var categoryItems = new List<CategoryItem>
        {
            new() { Name = "Nonexistent Quest", Category = "JobQuest", Expansion = "ARR" }
        };

        var results = enricher.Enrich(categoryItems);

        var item = Assert.Single(results);
        Assert.Equal("Nonexistent Quest", item.Name);
        Assert.Equal("JobQuest", item.Category);
        Assert.Equal("ARR", item.Expansion);
        Assert.Null(item.Level);
        Assert.Null(item.QuestId);
        Assert.NotNull(item.WikiUrl);
    }

    private static async Task<CsvDataProvider> CreateProviderAsync()
    {
        var http = new HttpClient();
        var fixtureDir = Path.GetFullPath(Path.Combine("TestData", "csv", "en"));
        var provider = new CsvDataProvider(http, fixtureDir);
        await provider.InitializeAsync();
        return provider;
    }
}
