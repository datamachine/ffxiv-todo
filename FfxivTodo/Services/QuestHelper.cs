using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace FfxivTodo.Services;

internal static class QuestHelper
{
    private static bool _initialized;
    private static IsQuestCompleteDelegate? _isQuestComplete;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private delegate bool IsQuestCompleteDelegate(ushort questId);

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var address = QuestManager.Addresses.IsQuestComplete.Value;
        if (address != nint.Zero)
            _isQuestComplete = Marshal.GetDelegateForFunctionPointer<IsQuestCompleteDelegate>(address);
    }

    public static bool IsQuestComplete(uint questId)
    {
        return _isQuestComplete?.Invoke((ushort)questId) ?? false;
    }

    public static unsafe bool IsQuestInProgress(uint questId)
    {
        try
        {
            var manager = QuestManager.Instance();
            if (manager == null) return false;
            return manager->IsQuestAccepted((ushort)questId);
        }
        catch
        {
            return false;
        }
    }
}
