using FfxivTodo.Models;
using FfxivTodo.Services;

namespace FfxivTodo.Tests.Services;

public sealed class ProgressStoreTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void SetStatus_LastQuestInChain_AutoCompletesParent()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Eden's Gate", UnlockQuestIds = [100] },
                new() { Id = 10, Name = "Quest A", QuestId = 100 },
            };

            store.SetStatus(10, ItemStatus.Completed, false, contentItems);

            var parentEntry = store.GetOrCreate(1);
            Assert.Equal(ItemStatus.Completed, parentEntry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetStatus_MidChain_DoesNotAutoCompleteParent()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Eden's Gate", UnlockQuestIds = [100, 200] },
                new() { Id = 10, Name = "Quest A", QuestId = 100 },
                new() { Id = 20, Name = "Quest B", QuestId = 200 },
            };

            store.SetStatus(10, ItemStatus.Completed, false, contentItems);

            var parentEntry = store.GetOrCreate(1);
            Assert.NotEqual(ItemStatus.Completed, parentEntry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetStatus_NotAQuest_NoAutoCompletion()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Some Item" },
            };

            store.SetStatus(1, ItemStatus.Completed, false, contentItems);

            var entry = store.GetOrCreate(2);
            Assert.Equal(ItemStatus.NotStarted, entry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetStatus_ManualParent_NotOverwritten()
    {
        var dir = TempDir();
        try
        {
            var store = new ProgressStore(dir);
            store.Load();

            var contentItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Eden's Gate", UnlockQuestIds = [100] },
                new() { Id = 10, Name = "Quest A", QuestId = 100 },
            };

            store.SetStatus(1, ItemStatus.NotStarted, true);
            store.SetStatus(10, ItemStatus.Completed, false, contentItems);

            var parentEntry = store.GetOrCreate(1);
            Assert.Equal(ItemStatus.NotStarted, parentEntry.Status);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}