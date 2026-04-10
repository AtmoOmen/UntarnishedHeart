using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Models;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.ExecuteAction.Helpers;

internal static class ExecuteActionDrawHelper
{
    public static void DrawActionReference(ActionReference reference, string idSuffix = "")
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var actionTypeCandidates = Enum.GetValues<ActionType>();
        using (var combo = ImRaii.Combo($"技能类型###ActionTypeCombo{idSuffix}", reference.ActionType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            var request = new CollectionSelectorRequest
            (
                "选择技能类型",
                "暂无可选技能类型",
                Array.IndexOf(actionTypeCandidates, reference.ActionType),
                actionTypeCandidates.Select(candidate => new CollectionSelectorItem(candidate.GetDescription())).ToArray()
            );

            CollectionSelectorWindow.Open
            (
                request,
                index =>
                {
                    if ((uint)index >= (uint)actionTypeCandidates.Length)
                        return;

                    reference.ActionType = actionTypeCandidates[index];
                }
            );
        }

        var actionID = reference.ActionID;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputUInt($"技能 ID###{idSuffix}", ref actionID))
            reference.ActionID = actionID;
    }

    public static void DrawTargetSelector(TargetSelector selector, string idSuffix)
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var selectorKindCandidates = Enum.GetValues<TargetSelectorKind>();
        using (var combo = ImRaii.Combo($"目标选择方式###TargetSelectorKind{idSuffix}", selector.Kind.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            var request = new CollectionSelectorRequest
            (
                "选择目标选择方式",
                "暂无可选目标选择方式",
                Array.IndexOf(selectorKindCandidates, selector.Kind),
                selectorKindCandidates.Select(candidate => new CollectionSelectorItem(candidate.GetDescription())).ToArray()
            );

            CollectionSelectorWindow.Open
            (
                request,
                index =>
                {
                    if ((uint)index >= (uint)selectorKindCandidates.Length)
                        return;

                    selector.Kind = selectorKindCandidates[index];
                }
            );
        }

        switch (selector.Kind)
        {
            case TargetSelectorKind.ByObjectKindAndDataID:

                using (ImRaii.Group())
                {
                    ImGui.SetNextItemWidth(240f * GlobalUIScale);
                    var objectKindCandidates = Enum.GetValues<ObjectKind>();
                    using (var combo = ImRaii.Combo($"对象类型###TargetObjectKind{idSuffix}", selector.ObjectKind.GetDescription(), ImGuiComboFlags.HeightLargest))
                    {
                        if (combo)
                            ImGui.CloseCurrentPopup();
                    }

                    if (ImGui.IsItemClicked())
                    {
                        var request = new CollectionSelectorRequest
                        (
                            "选择对象类型",
                            "暂无可选对象类型",
                            Array.IndexOf(objectKindCandidates, selector.ObjectKind),
                            objectKindCandidates.Select(candidate => new CollectionSelectorItem(candidate.GetDescription())).ToArray()
                        );

                        CollectionSelectorWindow.Open
                        (
                            request,
                            index =>
                            {
                                if ((uint)index >= (uint)objectKindCandidates.Length)
                                    return;

                                selector.ObjectKind = objectKindCandidates[index];
                            }
                        );
                    }

                    var dataID = selector.DataID;
                    ImGui.SetNextItemWidth(240f * GlobalUIScale);
                    if (ImGui.InputUInt($"Data ID###{idSuffix}", ref dataID))
                        selector.DataID = dataID;
                }

                ImGui.SameLine();

                if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Bullseye, "选择当前目标"))
                {
                    if (TargetManager.Target is { } target)
                    {
                        selector.ObjectKind = target.ObjectKind;
                        selector.DataID     = target.DataID;
                    }
                }

                var requireTargetable = selector.RequireTargetable;
                if (ImGui.Checkbox($"要求对象为可选中状态###{idSuffix}", ref requireTargetable))
                    selector.RequireTargetable = requireTargetable;
                break;

            case TargetSelectorKind.ByEntityID:
                var entityID = selector.EntityID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputUInt($"Entity ID###{idSuffix}", ref entityID))
                    selector.EntityID = entityID;

                ImGui.SameLine();

                if (ImGuiOm.ButtonIcon("SelectTarget", FontAwesomeIcon.Bullseye, "选择当前目标"))
                {
                    if (TargetManager.Target is { } target)
                        selector.EntityID = target.EntityID;
                }

                break;
        }
    }

    public static unsafe void DrawPositionSelector(string buttonID, Action<Vector3> setPosition, Func<Vector3>? getPosition = null)
    {
        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon($"{buttonID}_GetCurrent", FontAwesomeIcon.Bullseye, "取当前位置", true) &&
            DService.Instance().ObjectTable.LocalPlayer is { } localPlayer0)
            setPosition(localPlayer0.Position);

        if (getPosition == null)
            return;

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon($"{buttonID}_ToCurrent", FontAwesomeIcon.WheelchairMove, "瞬移至设置坐标", true) &&
            DService.Instance().ObjectTable.LocalPlayer is { } localPlayer1)
        {
            var target = getPosition();
            localPlayer1.ToStruct()->SetPosition(target.X, target.Y, target.Z);
        }
    }

    public static void DrawNoExtraParametersHint() =>
        ImGui.TextDisabled("此动作无需额外参数");

    public static void DrawAtkValueParameters(List<AtkValueParameter> parameters, string idSuffix)
    {
        if (ImGui.Button($"新增参数###{idSuffix}"))
            parameters.Add(new AtkValueParameter());

        if (parameters.Count == 0)
        {
            ImGui.TextDisabled("当前没有参数");
            return;
        }

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            using var id    = ImRaii.PushId($"{idSuffix}-{i}");
            using var group = ImRaii.Group();

            ImGui.AlignTextToFramePadding();
            ImGui.Text($"参数 {i}");
            ImGui.SameLine();

            if (i > 0 && ImGui.ArrowButton("###MoveUp", ImGuiDir.Up))
                (parameters[i - 1], parameters[i]) = (parameters[i], parameters[i - 1]);

            if (i > 0)
                ImGui.SameLine();

            if (i < parameters.Count - 1 && ImGui.ArrowButton("###MoveDown", ImGuiDir.Down))
                (parameters[i + 1], parameters[i]) = (parameters[i], parameters[i + 1]);

            if (i < parameters.Count - 1 || i > 0)
                ImGui.SameLine();

            if (ImGui.Button("删除"))
            {
                parameters.RemoveAt(i);
                i--;
                continue;
            }

            ImGui.SetNextItemWidth(200f * GlobalUIScale);
            var parameterTypeCandidates = Enum.GetValues<AtkValueParameterType>();
            using (var combo = ImRaii.Combo("参数类型###Type", parameter.Type.GetDescription(), ImGuiComboFlags.HeightLargest))
            {
                if (combo)
                    ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsItemClicked())
            {
                var request = new CollectionSelectorRequest
                (
                    "选择参数类型",
                    "暂无可选参数类型",
                    Array.IndexOf(parameterTypeCandidates, parameter.Type),
                    parameterTypeCandidates.Select(candidate => new CollectionSelectorItem(candidate.GetDescription())).ToArray()
                );

                CollectionSelectorWindow.Open
                (
                    request,
                    index =>
                    {
                        if ((uint)index >= (uint)parameterTypeCandidates.Length)
                            return;

                        parameter.Type = parameterTypeCandidates[index];
                    }
                );
            }

            DrawAtkValueParameterValue(parameter);
            ImGui.Separator();
        }
    }

    private static void DrawAtkValueParameterValue(AtkValueParameter parameter)
    {
        switch (parameter.Type)
        {
            case AtkValueParameterType.Int:
                var intValue = parameter.IntValue;
                ImGui.SetNextItemWidth(200f * GlobalUIScale);
                if (ImGui.InputInt("参数值###IntValue", ref intValue))
                    parameter.IntValue = intValue;
                break;

            case AtkValueParameterType.UInt:
                var uintValue = parameter.UIntValue;
                ImGui.SetNextItemWidth(200f * GlobalUIScale);
                if (ImGui.InputUInt("参数值###UIntValue", ref uintValue))
                    parameter.UIntValue = uintValue;
                break;

            case AtkValueParameterType.Float:
                var floatValue = parameter.FloatValue;
                ImGui.SetNextItemWidth(200f * GlobalUIScale);
                if (ImGui.InputFloat("参数值###FloatValue", ref floatValue))
                    parameter.FloatValue = floatValue;
                break;

            case AtkValueParameterType.Bool:
                var boolValue = parameter.BoolValue;
                if (ImGui.Checkbox("参数值###BoolValue", ref boolValue))
                    parameter.BoolValue = boolValue;
                break;

            case AtkValueParameterType.String:
                var stringValue = parameter.StringValue;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputText("参数值###StringValue", ref stringValue, 1024))
                    parameter.StringValue = stringValue;
                break;
        }
    }
}
