using CsvHelper.Configuration.Attributes;

namespace DataBuilder.Data;

public sealed record QuestCsvRow
{
    [Index(0)]
    public int Id { get; set; }

    [Index(1)]
    public string Name { get; set; } = string.Empty;

    [Index(1606)]
    public int ClassJobLevel { get; set; }

    [Index(1617)]
    public int Expansion { get; set; }

    [Index(1591)]
    public int PreviousQuest0 { get; set; }

    [Index(1592)]
    public int PreviousQuest1 { get; set; }

    [Index(1593)]
    public int PreviousQuest2 { get; set; }

    [Index(1599)]
    public int IssuerStart { get; set; }

    [Index(1600)]
    public int IssuerLocation { get; set; }

    [Index(1615)]
    public int PlaceName { get; set; }

    [Index(1641)]
    public int LevelMax { get; set; }
}

public sealed record AchievementCsvRow
{
    [Index(0)]
    public int Id { get; set; }

    [Index(2)]
    public string Name { get; set; } = string.Empty;
}

public sealed record EnpcResidentCsvRow
{
    [Index(0)]
    public int Id { get; set; }

    [Index(1)]
    public string Singular { get; set; } = string.Empty;
}

public sealed record TerritoryTypeCsvRow
{
    [Index(0)]
    public int Id { get; set; }

    [Index(1)]
    public string Name { get; set; } = string.Empty;
}

public sealed record PlaceNameCsvRow
{
    [Index(0)]
    public int Id { get; set; }

    [Index(1)]
    public string Name { get; set; } = string.Empty;
}