using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using FfxivTodo.Services;
using FfxivTodo.Windows;

namespace FfxivTodo;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "FFXIV Todo";

    [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IQuestManager QuestManager { get; private set; } = null!;
    [PluginService] internal static IAchievementManager AchievementManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FfxivTodo");

    private readonly ContentManager _contentManager;
    private readonly ProgressStore _progressStore;
    private readonly ProgressScanner _progressScanner;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _contentManager = new ContentManager();
        _progressStore = new ProgressStore(PluginInterface.ConfigDirectory.FullName);
        _progressScanner = new ProgressScanner(_progressStore);

        WindowSystem.AddWindow(new MainWindow(_contentManager, _progressStore, _progressScanner));
        WindowSystem.AddWindow(new OverlayWindow(_contentManager, _progressStore));

        CommandManager.AddHandler("/todo", new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle FFXIV Todo main window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        ClientState.Login += OnLogin;
        ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler("/todo");
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        ClientState.Login -= OnLogin;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        _progressScanner.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var mainWindow = WindowSystem.GetWindow<MainWindow>()!;
        var overlayWindow = WindowSystem.GetWindow<OverlayWindow>()!;

        if (args == "overlay")
            overlayWindow.IsOpen = !overlayWindow.IsOpen;
        else
            mainWindow.IsOpen = !mainWindow.IsOpen;
    }

    private void DrawUI() => WindowSystem.Draw();
    private void DrawConfigUI() => WindowSystem.GetWindow<MainWindow>()!.IsOpen = true;

    private void OnLogin() => _progressScanner.ScanAll(_contentManager.Items);
    private void OnTerritoryChanged(ushort territoryId) => _progressScanner.ScanZone(_contentManager.Items);
}