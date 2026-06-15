namespace FfxivTodo.Models;

public sealed class ProgressEntry
{
    public ItemStatus Status { get; set; } = ItemStatus.NotStarted;
    public bool IsTracked { get; set; }
    public bool IsIgnored { get; set; }
    public bool IsManual { get; set; }
}
