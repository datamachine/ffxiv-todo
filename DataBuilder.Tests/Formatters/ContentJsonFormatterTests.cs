using System.Collections.Generic;
using System.Linq;
using DataBuilder.Formatters;
using DataBuilder.Models;
using Xunit;

namespace DataBuilder.Tests.Formatters;

public class ContentJsonFormatterTests
{
    [Fact]
    public void Format_AssignsSequentialIds()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Quest A", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "Quest B", Category = "JobQuest", Expansion = "ARR", Level = 35 },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Equal(1u, result.Items[0].Id);
        Assert.Equal(2u, result.Items[1].Id);
    }

    [Fact]
    public void Format_ResolvesPrerequisiteNamesToIds()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Quest A", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new()
            {
                Name = "Quest B", Category = "JobQuest", Expansion = "ARR", Level = 35,
                PrerequisiteNames = new List<string> { "Quest A" }
            },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Single(result.Items[1].PrerequisiteIds);
        Assert.Equal(1u, result.Items[1].PrerequisiteIds[0]);
    }

    [Fact]
    public void Format_SkipsItemsMissingRequiredFields()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Valid", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "No Category", Category = "", Expansion = "ARR", Level = 30 },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Single(result.Items);
        Assert.Equal("Valid", result.Items[0].Name);
    }

    [Fact]
    public void Format_SortsByExpansionCategoryLevelName()
    {
        var items = new List<DetailItem>
        {
            new() { Name = "Z Quest", Category = "RaidSeries", Expansion = "ARR", Level = 50 },
            new() { Name = "A Quest", Category = "JobQuest", Expansion = "ARR", Level = 30 },
            new() { Name = "B Quest", Category = "JobQuest", Expansion = "HW", Level = 60 },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Equal("A Quest", result.Items[0].Name);
        Assert.Equal("Z Quest", result.Items[1].Name);
        Assert.Equal("B Quest", result.Items[2].Name);
    }

    [Fact]
    public void Format_UnresolvablePrereq_OmittedWithWarning()
    {
        var items = new List<DetailItem>
        {
            new()
            {
                Name = "Quest", Category = "JobQuest", Expansion = "ARR", Level = 30,
                PrerequisiteNames = new List<string> { "Nonexistent Quest" }
            },
        };

        var result = ContentJsonFormatter.Format(items);

        Assert.Empty(result.Items[0].PrerequisiteIds);
    }

    [Fact]
    public void Format_UnlockQuestIds_MappedToFormattedItem()
    {
        var items = new List<DetailItem>
        {
            new()
            {
                Name = "Eden's Gate", Category = "RaidSeries", Expansion = "ShB",
                Level = 80, UnlockQuestIds = [69163]
            }
        };

        var result = ContentJsonFormatter.Format(items);

        var item = Assert.Single(result.Items);
        Assert.Equal([69163u], item.UnlockQuestIds);
    }
}