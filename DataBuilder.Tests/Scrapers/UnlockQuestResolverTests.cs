using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DataBuilder.Models;
using DataBuilder.Scrapers;

namespace DataBuilder.Tests.Scrapers;

public sealed class UnlockQuestResolverTests
{
    [Fact]
    public void Resolve_WithOverride_PopulatesUnlockQuestIds()
    {
        var overrides = new QuestChainOverridesFile
        {
            Overrides =
            [
                new() { ContentName = "Eden's Gate", QuestIds = [69163] }
            ]
        };

        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

            var items = new List<DetailItem>
            {
                new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            resolver.Resolve(items);

            Assert.Equal([69163u], items[0].UnlockQuestIds);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void Resolve_WithoutOverride_DoesNotModifyItem()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(new QuestChainOverridesFile()));

            var items = new List<DetailItem>
            {
                new() { Name = "Some Item", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            resolver.Resolve(items);

            Assert.Empty(items[0].UnlockQuestIds);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void Resolve_WithOverride_TracksOverriddenNames()
    {
        var overrides = new QuestChainOverridesFile
        {
            Overrides =
            [
                new() { ContentName = "Eden's Gate", QuestIds = [69163] }
            ]
        };

        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

            var items = new List<DetailItem>
            {
                new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            resolver.Resolve(items);

            Assert.True(resolver.IsOverridden("Eden's Gate"));
            Assert.False(resolver.IsOverridden("Unknown Raid"));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void ExtractUnlockQuestNames_FindsQuestNameInHtml()
    {
        var html = File.ReadAllText(Path.Combine("TestData", "edens_gate.html"));
        var questNames = UnlockQuestResolver.ExtractUnlockQuestNames(html);
        Assert.Contains("In the Middle of Nowhere", questNames);
    }

    [Fact]
    public void ExtractUnlockQuestNames_EmptyHtml_ReturnsEmpty()
    {
        var names = UnlockQuestResolver.ExtractUnlockQuestNames("<html></html>");
        Assert.Empty(names);
    }

    [Fact]
    public void ResolveWithChainCreation_WithOverride_CreatesMissingQuestEntries()
    {
        var overrides = new QuestChainOverridesFile
        {
            Overrides =
            [
                new() { ContentName = "Eden's Gate", QuestIds = [69163] }
            ]
        };

        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

            var items = new List<DetailItem>
            {
                new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            var newItems = resolver.ResolveWithChainCreation(items, csv: null);

            Assert.Equal(2, items.Count);
            Assert.Equal("Eden's Gate", items[0].Name);
            Assert.Equal([69163u], items[0].UnlockQuestIds);
            Assert.Equal("BlueUnlock", items[1].Category);
            Assert.Equal(69163u, items[1].QuestId);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public void ResolveWithChainCreation_WithoutCsv_DoesNotWalkChain()
    {
        var overrides = new QuestChainOverridesFile
        {
            Overrides =
            [
                new() { ContentName = "Eden's Gate", QuestIds = [69163] }
            ]
        };

        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, JsonSerializer.Serialize(overrides));

            var items = new List<DetailItem>
            {
                new() { Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB" }
            };

            var resolver = new UnlockQuestResolver(tmpFile);
            var newItems = resolver.ResolveWithChainCreation(items, csv: null);

            Assert.Single(newItems);
            Assert.Equal(69163u, newItems[0].QuestId);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}