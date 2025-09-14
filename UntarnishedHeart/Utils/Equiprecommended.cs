using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Collections.Generic;

namespace UntarnishedHeart.Utils;

public unsafe class Equiprecommended
{
    private static RecommendEquipModule* Module => Framework.Instance()->GetUIModule()->GetRecommendEquipModule();

    public static bool? TryEquipRecommendGear()
    {

        if (Module == null) return false;

        if (BetweenAreas || OccupiedInEvent || !IsScreenReady()) return false;

        if (DService.ObjectTable.LocalPlayer is not { ClassJob.RowId: var job, IsTargetable: true } || job == 0)
            return false;

        Module->SetupForClassJob((byte)(DService.ClientState.LocalPlayer?.ClassJob.RowId ?? 0));
        DService.Framework.Update += DoEquip;

        return true;
    }

    private static void DoEquip(IFramework framework)
    {
        if (Module == null || Module->EquippedMainHand == null)
        {
            DService.Framework.Update -= DoEquip;
            return;
        }
        framework.DelayTicks(30);
        Module->EquipRecommendedGear();
    }
}
