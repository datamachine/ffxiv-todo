using System.Text.Json.Serialization;

namespace FfxivTodo.Models;

public sealed class ContentItem
{
    [JsonPropertyName("id")]
    public uint Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("level")]
    public uint Level { get; init; }

    [JsonPropertyName("expansion")]
    public Expansion Expansion { get; init; }

    [JsonPropertyName("category")]
    public ContentCategory Category { get; init; }

    [JsonPropertyName("prerequisiteIds")]
    public uint[] PrerequisiteIds { get; init; } = [];

    [JsonPropertyName("locationTerritoryId")]
    public uint? LocationTerritoryId { get; init; }

    [JsonPropertyName("locationMapX")]
    public float? LocationMapX { get; init; }

    [JsonPropertyName("locationMapY")]
    public float? LocationMapY { get; init; }

    [JsonPropertyName("questId")]
    public uint? QuestId { get; init; }

    [JsonPropertyName("achievementId")]
    public uint? AchievementId { get; init; }

    [JsonPropertyName("wikiUrl")]
    public string? WikiUrl { get; init; }
}