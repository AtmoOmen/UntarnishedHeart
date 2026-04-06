using OmenTools.Interop.Game.Lumina;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Enums;
using Item = Lumina.Excel.Sheets.Item;

namespace UntarnishedHeart.Execution.Condition;

public sealed class ItemCountCondition : RouteValueConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.ItemCount;

    public uint ItemID { get; set; }

    protected override int GetCurrentValue(in ConditionContext context) => (int)LocalPlayerState.GetItemCount(ItemID);

    protected override bool EqualsExtraCore(RouteValueConditionBase other) =>
        other is ItemCountCondition condition &&
        ItemID == condition.ItemID;

    protected override int GetExtraHashCode() => (int)ItemID;

    protected override RouteValueConditionBase DeepCopyCore() =>
        new ItemCountCondition
        {
            ComparisonType = ComparisonType,
            ExpectedValue  = ExpectedValue,
            ItemID         = ItemID
        };

    protected override void DrawExtraFields()
    {
        DrawLabel("物品 ID", KnownColor.LightSkyBlue.ToVector4());
        var itemID = ItemID;
        if (ImGui.InputUInt("###ItemIdInput", ref itemID))
            ItemID = itemID;

        if (ItemID == 0 || !LuminaGetter.TryGetRow(ItemID, out Item itemRow))
            return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"({itemRow.Name})");
        ImGuiOm.TooltipHover($"{itemRow.Description}");
    }
}
