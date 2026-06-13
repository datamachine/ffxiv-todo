using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataBuilder.Models;

public sealed class CategoryItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = string.Empty;
}

public sealed class CategoryItemsFile
{
    [JsonPropertyName("items")]
    public List<CategoryItem> Items { get; set; } = new();
}

public sealed class DetailItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public uint? Level { get; set; }

    [JsonPropertyName("locationTerritoryName")]
    public string? LocationTerritoryName { get; set; }

    [JsonPropertyName("locationMapX")]
    public float? LocationMapX { get; set; }

    [JsonPropertyName("locationMapY")]
    public float? LocationMapY { get; set; }

    [JsonPropertyName("prerequisiteNames")]
    public List<string> PrerequisiteNames { get; set; } = new();

    [JsonPropertyName("edbUrl")]
    public string? EdbUrl { get; set; }

    [JsonPropertyName("wikiUrl")]
    public string? WikiUrl { get; set; }

    [JsonPropertyName("questId")]
    public uint? QuestId { get; set; }

    [JsonPropertyName("achievementId")]
    public uint? AchievementId { get; set; }

    [JsonPropertyName("locationTerritoryId")]
    public uint? LocationTerritoryId { get; set; }
}

public sealed class DetailItemsFile
{
    [JsonPropertyName("items")]
    public List<DetailItem> Items { get; set; } = new();
}

public sealed class FormattedItem
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public uint Level { get; set; }

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("prerequisiteIds")]
    public List<uint> PrerequisiteIds { get; set; } = new();

    [JsonPropertyName("locationTerritoryId")]
    public uint? LocationTerritoryId { get; set; }

    [JsonPropertyName("locationMapX")]
    public float? LocationMapX { get; set; }

    [JsonPropertyName("locationMapY")]
    public float? LocationMapY { get; set; }

    [JsonPropertyName("questId")]
    public uint? QuestId { get; set; }

    [JsonPropertyName("achievementId")]
    public uint? AchievementId { get; set; }

    [JsonPropertyName("wikiUrl")]
    public string? WikiUrl { get; set; }
}

public sealed class FormattedItemsFile
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("items")]
    public List<FormattedItem> Items { get; set; } = new();
}

public sealed class AchievementOverride
{
    [JsonPropertyName("contentName")]
    public string ContentName { get; set; } = string.Empty;

    [JsonPropertyName("achievementId")]
    public uint AchievementId { get; set; }
}

public sealed class AchievementOverridesFile
{
    [JsonPropertyName("overrides")]
    public List<AchievementOverride> Overrides { get; set; } = new();
}