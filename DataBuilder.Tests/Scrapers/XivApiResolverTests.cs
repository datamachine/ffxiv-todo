using System;
using System.Net.Http;
using System.Threading.Tasks;
using DataBuilder.Scrapers;
using Xunit;

namespace DataBuilder.Tests.Scrapers;

public class XivApiResolverTests
{
    [Fact]
    public void ExtractQuestIdFromEdbUrl_ValidUrl_ReturnsId()
    {
        var url = "https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/";
        var id = XivApiResolver.ExtractQuestIdFromEdbUrl(url);
        Assert.Equal((uint)65586, id);
    }

    [Fact]
    public void ExtractQuestIdFromEdbUrl_InvalidUrl_ReturnsNull()
    {
        var url = "https://example.com/something";
        var id = XivApiResolver.ExtractQuestIdFromEdbUrl(url);
        Assert.Null(id);
    }

    [Fact]
    public void ExtractQuestIdFromEdbUrl_NullUrl_ReturnsNull()
    {
        var id = XivApiResolver.ExtractQuestIdFromEdbUrl(null);
        Assert.Null(id);
    }

    [Fact]
    public async Task ResolveAsync_SetsQuestIdFromEdbUrl()
    {
        var handler = new MockHttpHandler(req =>
            """{"Results":[]}""");

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://xivapi.com") };
        var resolver = new XivApiResolver(http);

        var item = new Models.DetailItem
        {
            Name = "Test Duty",
            EdbUrl = "https://na.finalfantasyxiv.com/lodestone/playguide/db/quest/65586/"
        };

        await resolver.ResolveAsync(item);

        Assert.Equal((uint)65586, item.QuestId);
    }
}