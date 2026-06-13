using System.Text.Json.Serialization;

namespace FfxivTodo.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Expansion
{
    ARR,
    HW,
    SB,
    ShB,
    EW,
    DT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentCategory
{
    SideQuest,
    BlueUnlock,
    JobQuest,
    RoleQuest,
    TrialSeries,
    RaidSeries,
    AllianceRaid,
    BeastTribe,
    CustomDelivery
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ItemStatus
{
    NotStarted,
    InProgress,
    Completed
}