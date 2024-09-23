using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static readonly CompSig ExecuteCommandSig = 
        new("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 ?? 5F C3 CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC ?? 80 A1");
    private delegate nint ExecuteCommandDelegate(int command, int param1 = 0, int param2 = 0, int param3 = 0, int param4 = 0);
    private static ExecuteCommandDelegate ExecuteCommand;

    private static readonly CompSig PlayerControllerSig =
        new("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C ?? 75 ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? EB ?? 49 3B FE");

    static GameFunctions()
    {
        ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(ExecuteCommandSig.ScanText());
    }

    public static unsafe void RegisterToEnterDuty()
        => SendEvent(AgentId.ContentsFinder, 0, 12, 0);

    public static void Teleport(Vector3 pos)
    {
        if (DService.ClientState.LocalPlayer is not { } localPlayer) return;

        var address = localPlayer.Address + 176;
        MemoryHelper.Write(address, pos.X);
        MemoryHelper.Write(address + 4, pos.Y);
        MemoryHelper.Write(address + 8, pos.Z);
    }

    public static void Move(GameObjectId gameObjectID)
    {
        var baseAddress = PlayerControllerSig.GetStatic();

        SafeMemory.Write(baseAddress + 1080, 4);
        SafeMemory.Write(baseAddress + 1072, gameObjectID);
    }

    public static void LeaveDuty() => ExecuteCommand(819, 1);
}
