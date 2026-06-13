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
    private readonly MapFlagHelper _mapFlagHelper;
    private readonly MainWindow _mainWindow;
    private readonly OverlayWindow _overlayWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _mapFlagHelper = new MapFlagHelper();
        _contentManager = new ContentManager();
        _progressStore = new ProgressStore(PluginInterface.ConfigDirectory.FullName);
        _progressScanner = new ProgressScanner(_progressStore);
        _progressScanner.SetDebounce(Configuration.ScanDebounceMs);

        _contentManager.LoadContent();
        _progressStore.Load();
        _contentManager.SetProgress(_progressStore.GetAll());

        _mainWindow = new MainWindow(_contentManager, _progressStore, _progressScanner, _mapFlagHelper);
        _overlayWindow = new OverlayWindow(_contentManager, _progressStore, _mapFlagHelper);
        _overlayWindow.IsOpen = false;

        WindowSystem.AddWindow(_mainWindow);
        WindowSystem.AddWindow(_overlayWindow);

        CommandManager.AddHandler("/todo", new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle FFXIV Todo windows"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        PluginInterface.UiBuilder.SaveConfig += OnSaveConfig;
        ClientState.Login += OnLogin;
        ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler("/todo");
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        PluginInterface.UiBuilder.SaveConfig -= OnSaveConfig;
        ClientState.Login -= OnLogin;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        _progressScanner.Dispose();
        _mainWindow.Dispose();
        _overlayWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (args == "overlay")
            _overlayWindow.IsOpen = !_overlayWindow.IsOpen;
        else if (args == "refresh")
            OnRefresh();
        else
            _mainWindow.IsOpen = !_mainWindow.IsOpen;
    }

    private void DrawUI() => WindowSystem.Draw();

    private void DrawConfigUI() => _mainWindow.IsOpen = true;

    private void OnSaveConfig()
    {
        PluginInterface.SavePluginConfig(Configuration);
        _progressScanner.SetDebounce(Configuration.ScanDebounceMs);
    }

    private void OnLogin() => OnRefresh();

    private void OnTerritoryChanged(ushort territoryId) => _progressScanner.ScanZone(_contentManager.Items);

    private void OnRefresh()
    {
        _progressStore.Load();
        _progressScanner.ScanAll(_contentManager.Items);
        _contentManager.SetProgress(_progressStore.GetAll());
    }
}