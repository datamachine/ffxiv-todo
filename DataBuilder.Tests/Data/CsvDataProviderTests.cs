using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DataBuilder.Data;

namespace DataBuilder.Tests.Data;

public sealed class CsvDataProviderTests
{
    private static string FixtureDir =>
        Path.GetFullPath(Path.Combine("TestData", "csv", "en"));

    [Fact]
    public async Task LookupQuest_ExactMatch_ReturnsRow()
    {
        var provider = await CreateProviderAsync();

        var row = provider.LookupQuest("Hallo Halatali");

        Assert.NotNull(row);
        Assert.Equal(66695, row.Id);
        Assert.Equal("Hallo Halatali", row.Name);
    }

    [Fact]
    public async Task LookupQuest_CaseInsensitive_ReturnsRow()
    {
        var provider = await CreateProviderAsync();

        var row = provider.LookupQuest("hallo halatali");

        Assert.NotNull(row);
        Assert.Equal(66695, row.Id);
    }

    [Fact]
    public async Task LookupQuest_WhitespaceNormalized_ReturnsRow()
    {
        var provider = await CreateProviderAsync();

        var row = provider.LookupQuest("  Hallo Halatali  ");

        Assert.NotNull(row);
        Assert.Equal(66695, row.Id);
    }

    [Fact]
    public async Task LookupQuest_NotFound_ReturnsNull()
    {
        var provider = await CreateProviderAsync();

        var row = provider.LookupQuest("Nonexistent Quest");

        Assert.Null(row);
    }

    [Fact]
    public async Task LookupQuest_ParentheticalsRemoved_ReturnsRow()
    {
        var provider = await CreateProviderAsync();

        var row = provider.LookupQuest("Family Crest (Gladiator)");

        Assert.NotNull(row);
        Assert.Equal(288, row.Id);
    }

    [Fact]
    public async Task ResolveNpcName_Found_ReturnsName()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolveNpcName(1002345);

        Assert.Equal("Swynbroes", name);
    }

    [Fact]
    public async Task ResolveNpcName_NotFound_ReturnsNull()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolveNpcName(9999999);

        Assert.Null(name);
    }

    [Fact]
    public async Task ResolveTerritoryName_Found_ReturnsName()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolveTerritoryName(129);

        Assert.Equal("Lower La Noscea", name);
    }

    [Fact]
    public async Task ResolveTerritoryName_NotFound_ReturnsNull()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolveTerritoryName(99999);

        Assert.Null(name);
    }

    [Fact]
    public async Task ResolvePlaceName_Found_ReturnsName()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolvePlaceName(28);

        Assert.Equal("Lower La Noscea", name);
    }

    [Fact]
    public async Task ResolvePlaceName_NotFound_ReturnsNull()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolvePlaceName(99999);

        Assert.Null(name);
    }

    [Fact]
    public async Task ResolveQuestName_Found_ReturnsName()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolveQuestName(66695);

        Assert.Equal("Hallo Halatali", name);
    }

    [Fact]
    public async Task ResolveQuestName_NotFound_ReturnsNull()
    {
        var provider = await CreateProviderAsync();

        var name = provider.ResolveQuestName(9999999);

        Assert.Null(name);
    }

    [Fact]
    public async Task LookupQuest_SmartQuoteNormalized_ReturnsRow()
    {
        var provider = await CreateProviderAsync();

        var row = provider.LookupQuest("\u201cHallo Halatali\u201d");

        Assert.NotNull(row);
        Assert.Equal(66695, row.Id);
    }

    private static async Task<CsvDataProvider> CreateProviderAsync()
    {
        var http = new HttpClient();
        var provider = new CsvDataProvider(http, FixtureDir);
        await provider.InitializeAsync();
        return provider;
    }
}