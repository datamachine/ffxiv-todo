using System.Text.Json.Serialization;

namespace FfxivTodo.Models;

public sealed class ProgressEntry
{
    [JsonPropertyName("status")]
    public ItemStatus Status { get; set; } = ItemStatus.NotStarted;

    [JsonPropertyName("isTracked")]
    public bool IsTracked { get; set; }

    [JsonPropertyName("isIgnored")]
    public bool IsIgnored { get; set; }

    [JsonPropertyName("isManual")]
    public bool IsManual { get; set; }
}