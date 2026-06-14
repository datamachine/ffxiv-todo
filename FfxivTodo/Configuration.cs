using Dalamud.Configuration;
using System.Numerics;

namespace FfxivTodo;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public Vector2 OverlayPosition { get; set; } = new(100, 100);
    public float OverlayWidth { get; set; } = 300;
    public float OverlayOpacity { get; set; } = 80;
    public int OverlayMaxItems { get; set; } = 10;
    public bool OverlayLocked { get; set; } = false;
    public float OverlayFontScale { get; set; } = 1.0f;
    public int ScanDebounceMs { get; set; } = 2000;
}