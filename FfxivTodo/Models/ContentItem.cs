namespace FfxivTodo.Models;

public sealed class ContentItem
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Level { get; set; }
    public Expansion Expansion { get; set; }
    public ContentCategory Category { get; set; }
    public uint[] PrerequisiteIds { get; set; } = [];
    public uint? LocationTerritoryId { get; set; }
    public string? LocationTerritoryName { get; set; }
    public float? LocationMapX { get; set; }
    public float? LocationMapY { get; set; }
    public uint? QuestId { get; set; }
    public uint? AchievementId { get; set; }
    public uint[] UnlockQuestIds { get; set; } = [];
    public string? WikiUrl { get; set; }
}
