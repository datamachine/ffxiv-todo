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
}