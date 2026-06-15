using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FfxivTodo.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum Expansion
{
    ARR,
    HW,
    SB,
    ShB,
    EW,
    DT
}

[JsonConverter(typeof(StringEnumConverter))]
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
    CustomDelivery,
    SavageRaid,
    UltimateRaid,
    FieldOperation,
    VariantDungeon,
    ChaoticRaid,
    DeepDungeon,
    RelicWeapon,
    IslandSanctuary,
    IshgardianRestoration,
    FauxHollows,
    MaskedCarnivale
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ItemStatus
{
    NotStarted,
    InProgress,
    Completed
}
