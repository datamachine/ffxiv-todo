using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataBuilder.Models;

public sealed class QuestChainOverride
{
    [JsonPropertyName("contentName")]
    public string ContentName { get; set; } = string.Empty;

    [JsonPropertyName("questIds")]
    public List<uint> QuestIds { get; set; } = new();

    [JsonPropertyName("explicitChain")]
    public bool ExplicitChain { get; set; }
}

public sealed class QuestChainOverridesFile
{
    [JsonPropertyName("overrides")]
    public List<QuestChainOverride> Overrides { get; set; } = new();
}