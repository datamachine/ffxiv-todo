using FfxivTodo.Models;
using FfxivTodo.Services;

namespace FfxivTodo.Tests.Services;

public sealed class ContentManagerTests
{
    [Fact]
    public void GetUnlockQuests_WithChain_ReturnsQuestItems()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Eden's Gate", Category = ContentCategory.RaidSeries, UnlockQuestIds = [10, 20] },
            new() { Id = 10, Name = "Quest A", Category = ContentCategory.BlueUnlock, QuestId = 100 },
            new() { Id = 20, Name = "Quest B", Category = ContentCategory.BlueUnlock, QuestId = 200 },
        };

        cm.LoadFromList(items);

        var quests = cm.GetUnlockQuests(1);
        Assert.Equal(2, quests.Count);
        Assert.Equal("Quest A", quests[0].Name);
        Assert.Equal("Quest B", quests[1].Name);
    }

    [Fact]
    public void GetUnlockQuests_NoChain_ReturnsEmpty()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Some Item", Category = ContentCategory.SideQuest }
        };

        cm.LoadFromList(items);

        var quests = cm.GetUnlockQuests(1);
        Assert.Empty(quests);
    }

    [Fact]
    public void FindParentContent_QuestInChain_ReturnsParent()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Eden's Gate", Category = ContentCategory.RaidSeries, UnlockQuestIds = [10] },
            new() { Id = 10, Name = "Quest A", Category = ContentCategory.BlueUnlock, QuestId = 100 },
        };

        cm.LoadFromList(items);

        var parent = cm.FindParentContent(10);
        Assert.NotNull(parent);
        Assert.Equal("Eden's Gate", parent.Name);
    }

    [Fact]
    public void FindParentContent_NotInChain_ReturnsNull()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Some Item", Category = ContentCategory.SideQuest }
        };

        cm.LoadFromList(items);

        var parent = cm.FindParentContent(1);
        Assert.Null(parent);
    }

    [Fact]
    public void FindParentContent_ByQuestId_MatchesUnlockQuestIds()
    {
        var cm = new ContentManager();
        var items = new List<ContentItem>
        {
            new() { Id = 1, Name = "Eden's Gate", UnlockQuestIds = [69163] },
            new() { Id = 10, Name = "In the Middle of Nowhere", QuestId = 69163 },
        };

        cm.LoadFromList(items);

        var parent = cm.FindParentContent(10);
        Assert.NotNull(parent);
        Assert.Equal(1u, parent.Id);
    }
}
