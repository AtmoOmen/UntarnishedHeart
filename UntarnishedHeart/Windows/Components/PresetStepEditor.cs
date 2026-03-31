using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Utility;
using OmenTools.Dalamud;
using OmenTools.Interop.Game;
using OmenTools.Interop.Windows.Helpers;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Windows.Components;

internal static class PresetStepEditor
{
    public static void Draw(PresetStep step, ref int i, List<PresetStep> steps)
    {
        using var id    = ImRaii.PushId($"Step-{i}");
        using var group = ImRaii.Group();

        var stepName = step.Note;
        ImGuiOm.CompLabelLeft("名称:", -1f, () => ImGui.InputText("###StepNoteInput", ref stepName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            step.Note = stepName;

        ImGui.Spacing();

        using var child = ImRaii.Child("StepContentChild", ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemSpacing);
        if (!child) return;

        using var color  = ImRaii.PushColor(ImGuiCol.ChildBg, Vector4.Zero);
        using var tabBar = ImRaii.TabBar("###StepContentTabBar");
        if (!tabBar) return;

        using (var preWait = ImRaii.TabItem("前置等待"))
        {
            if (preWait)
                DrawPreWaitTab(step);
            else
                ImGuiOm.TooltipHover("各类状态检查, 每一步骤开始时顺序执行此处每一项状态检查");
        }

        using (var positionCheck = ImRaii.TabItem("坐标"))
        {
            if (positionCheck)
                DrawPositionTab(step);
            else
                ImGuiOm.TooltipHover("在执行完左侧的前置等待后, 会按照此处的配置移动到指定的坐标");
        }

        using (var targetCheck = ImRaii.TabItem("目标"))
        {
            if (targetCheck)
                DrawTargetTab(step);
            else
                ImGuiOm.TooltipHover("在开始向左侧配置的坐标移动后, 会按照此处的配置选中指定的目标");
        }

        using (var textCommand = ImRaii.TabItem("文本指令"))
        {
            if (textCommand)
                DrawTextCommandTab(step);
            else
                ImGuiOm.TooltipHover("在执行完左侧的目标选择后, 会按照此处的配置选中自定义文本指令\n支持多条文本指令, 一行一条");
        }

        using (var postWait = ImRaii.TabItem("后置等待"))
        {
            if (postWait)
                DrawPostWaitTab(step);
        }

        using (var flowControl = ImRaii.TabItem("流程控制"))
        {
            if (flowControl)
                DrawFlowControlTab(step, steps);
            else
                ImGuiOm.TooltipHover("为流程添加跳转, 在本步骤完成后跳转到指定步骤继续执行");
        }

        DrawReorderButtons(ref i, steps);
    }

    private static void DrawPreWaitTab(PresetStep step)
    {
        var stepStopWhenAnyAlive = step.StopWhenAnyAlive;
        if (ImGui.Checkbox("若任一队友不在无法战斗状态, 则等待###StepStopWhenAnyAliveInput", ref stepStopWhenAnyAlive))
            step.StopWhenAnyAlive = stepStopWhenAnyAlive;
        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否存在任一队友不在无法战斗状态, 若存在, 则阻塞步骤执行");

        var stepStopInCombat = step.StopInCombat;
        if (ImGui.Checkbox("若已进入战斗, 则等待###StepStopInCombatInput", ref stepStopInCombat))
            step.StopInCombat = stepStopInCombat;
        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否进入战斗状态, 若已进入, 则阻塞步骤执行");

        var stepStopWhenBusy = step.StopWhenBusy;
        if (ImGui.Checkbox("若为忙碌状态, 则等待###StepStopWhenBusyInput", ref stepStopWhenBusy))
            step.StopWhenBusy = stepStopWhenBusy;
        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否正处于忙碌状态 (如: 过图加载, 交互等), 若已进入, 则阻塞步骤执行");
    }

    private static void DrawPositionTab(PresetStep step)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "方式:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);

        using (var combo = ImRaii.Combo("###StepMoveTypeCombo", step.MoveType.ToString()))
        {
            if (combo)
            {
                foreach (var moveType in Enum.GetValues<MoveType>())
                {
                    if (moveType == MoveType.无)
                        continue;

                    if (ImGui.Selectable(moveType.ToString(), step.MoveType == moveType))
                        step.MoveType = moveType;
                }
            }
        }

        if (step.MoveType == MoveType.无)
            ImGuiOm.TooltipHover("若设置为 无, 则代表使用 传送\n[废弃功能, 仅兼容用]");

        using (ImRaii.Group())
        {
            var stepPosition = step.Position;
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "位置:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputFloat3("###StepPositionInput", ref stepPosition))
                step.Position = stepPosition;
            ImGuiOm.TooltipHover("若不想执行移动, 请将坐标设置为 <0, 0, 0>");

            ImGui.SameLine();

            if (ImGuiOm.ButtonIcon("GetPosition", FontAwesomeIcon.Bullseye, "取当前位置", true) &&
                DService.Instance().ObjectTable.LocalPlayer is { } localPlayer)
                step.Position = localPlayer.Position;

            ImGui.SameLine();

            if (ImGuiOm.ButtonIcon("PastePosition", FontAwesomeIcon.Clipboard, "粘贴坐标", true))
                TryPastePosition(step);

            ImGui.SameLine();

            if (ImGuiOm.ButtonIcon("MoveToThis", FontAwesomeIcon.MapMarkerAlt, "根据当前移动方式移动到此位置", true))
            {
                var finalMoveType = step.MoveType == MoveType.无 ? MoveType.传送 : step.MoveType;
                _ = PreviewMoveAsync(step.Position, finalMoveType);
            }
        }

        ImGui.Spacing();

        using (ImRaii.Group())
        {
            var waitForGetClose = step.WaitForGetClose;
            if (ImGui.Checkbox("等待接近坐标后再继续###WaitForGetCloseInput", ref waitForGetClose))
                step.WaitForGetClose = waitForGetClose;
            ImGuiOm.TooltipHover("若勾选, 则会等待完全接近坐标后再继续执行下面的操作");
        }
    }

    private static void DrawTargetTab(PresetStep step)
    {
        using (var table = ImRaii.Table("TargetInfoTable", 2))
        {
            if (table)
            {
                ImGui.TableSetupColumn("标签", ImGuiTableColumnFlags.WidthFixed,   ImGui.CalcTextSize("Data ID:").X);
                ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthStretch, 50);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "类型:");

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);

                using (var combo = ImRaii.Combo("###TargetObjectKindCombo", step.ObjectKind.ToString()))
                {
                    if (combo)
                    {
                        foreach (var objectKind in Enum.GetValues<ObjectKind>())
                        {
                            if (ImGui.Selectable(objectKind.ToString(), step.ObjectKind == objectKind))
                                step.ObjectKind = objectKind;
                        }
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "Data ID:");
                ImGuiOm.TooltipHover("设置为 0 则为不自动执行任何选中操作");

                ImGui.TableNextColumn();
                var stepDataID = step.DataID;
                ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputUInt("###StepDatIDInput", ref stepDataID))
                    step.DataID = stepDataID;

                ImGui.SameLine();

                if (ImGuiOm.ButtonIcon("GetTarget", FontAwesomeIcon.Crosshairs, "取当前目标", true) &&
                    TargetManager.Target is { } target)
                {
                    step.DataID     = target.DataID;
                    step.ObjectKind = target.ObjectKind;
                }
            }
        }

        ImGui.Spacing();

        var interactWithNearestObject = step.InteractWithNearestObject;
        if (ImGui.Checkbox("交互最近可交互物体", ref interactWithNearestObject))
            step.InteractWithNearestObject = interactWithNearestObject;
        ImGuiOm.TooltipHover("启用后将忽略本步骤内的指定目标查找与交互配置，转而自动寻找最近的可交互目标");

        var waitForTargetSpawn = step.WaitForTargetSpawn;
        if (ImGui.Checkbox("等待目标生成", ref waitForTargetSpawn))
            step.WaitForTargetSpawn = waitForTargetSpawn;
        ImGuiOm.TooltipHover("勾选后, 则会阻塞进程持续查找对应目标, 直到符合条件的目标进入游戏客户端内存中");

        var waitForTarget = step.WaitForTarget;
        if (ImGui.Checkbox("等待目标被选中", ref waitForTarget))
            step.WaitForTarget = waitForTarget;
        ImGuiOm.TooltipHover("勾选后, 则会阻塞进程持续查找对应目标并尝试选中, 直至任一目标被选中");

        var interactWithTarget = step.InteractWithTarget;
        if (ImGui.Checkbox("交互此目标", ref interactWithTarget))
            step.InteractWithTarget = interactWithTarget;
        ImGuiOm.TooltipHover("勾选后, 则会尝试与当前目标进行交互\n\n注: 请自行确保位于一个可交互到目标的坐标");

        if (step.InteractWithTarget)
        {
            var interactNeedTargetAnything = step.InteractNeedTargetAnything;
            if (ImGui.Checkbox("交互需要选中目标", ref interactNeedTargetAnything))
                step.InteractNeedTargetAnything = interactNeedTargetAnything;
            ImGuiOm.TooltipHover("勾选后, 若当前未选中任何目标, 则会跳过交互");
        }

        ImGui.NewLine();

        var targetNeedTargetable = step.TargetNeedTargetable;
        if (ImGui.Checkbox("目标需要为\"可选中\"状态", ref targetNeedTargetable))
            step.TargetNeedTargetable = targetNeedTargetable;
        ImGuiOm.TooltipHover("勾选后, 在游戏内部被标记为不可选中的目标将不会被纳入检测范围");
    }

    private static void DrawTextCommandTab(PresetStep step)
    {
        var stepCommands = step.Commands;
        var commandInputHeight = Math.Max
        (
            ImGui.GetTextLineHeightWithSpacing() * 5f,
            ImGui.CalcTextSize(stepCommands).Y + 2 * ImGui.GetStyle().ItemSpacing.Y
        );

        if (ImGui.InputTextMultiline
            (
                "###CommandsInput",
                ref stepCommands,
                1024,
                new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X, commandInputHeight)
            ))
            step.Commands = stepCommands;
        ImGuiOm.TooltipHover("支持以下特殊指令:\n/wait <时间(ms)> - 等待指定毫秒的时间 (如: /wait 2000 - 等待 2 秒)");

        step.CommandCondition.Draw();
    }

    private static void DrawPostWaitTab(PresetStep step)
    {
        using var group = ImRaii.Group();

        var stepDelay = step.Delay;
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputUInt("等待时间 (ms)###StepDelayInput", ref stepDelay))
            step.Delay = stepDelay;
        ImGuiOm.TooltipHover("在开始下一步骤前, 需要等待的时间");
    }

    private static void DrawFlowControlTab(PresetStep step, List<PresetStep> steps)
    {
        var jumpToIndex = step.JumpToIndex;
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);

        if (ImGui.InputInt("目标步骤 (设置为 -1 以禁用)###StepJumpToIndex", ref jumpToIndex))
        {
            jumpToIndex      = Math.Clamp(jumpToIndex, -1, steps.Count - 1);
            step.JumpToIndex = jumpToIndex;
        }

        if (step.JumpToIndex >= 0 && step.JumpToIndex < steps.Count)
        {
            var targetStep = steps[step.JumpToIndex];
            ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), $"第 {step.JumpToIndex} 步 - {targetStep.Note}");
        }
    }

    private static void DrawReorderButtons(ref int i, List<PresetStep> steps)
    {
        if (i > 0)
        {
            if (ImGui.TabItemButton("↑"))
            {
                var index = i - 1;
                steps.Swap(i, index);
                i = index;
            }

            ImGuiOm.TooltipHover("上移");
        }

        if (i < steps.Count - 1)
        {
            if (ImGui.TabItemButton("↓"))
            {
                var index = i + 1;
                steps.Swap(i, index);
                i = index;
            }

            ImGuiOm.TooltipHover("下移");
        }
    }

    private static void TryPastePosition(PresetStep step)
    {
        try
        {
            var clipboardText = ImGui.GetClipboardText();

            if (TryParsePosition(clipboardText, out var parsedPosition))
            {
                step.Position = parsedPosition;
                NotifyHelper.Instance().Chat($"已从剪贴板读取坐标 <{parsedPosition.X:F2}, {parsedPosition.Y:F2}, {parsedPosition.Z:F2}>");
                return;
            }

            NotifyHelper.Instance().Chat("剪贴板内容格式不正确, 期望格式: X: -0.41, Y: 0.00, Z: 5.46");
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"读取剪贴板失败: {ex.Message}");
        }
    }

    private static async Task PreviewMoveAsync(Vector3 position, MoveType moveType)
    {
        try
        {
            switch (moveType)
            {
                case MoveType.寻路:
                    await PreviewPathfindMoveAsync(position);
                    break;
                case MoveType.vnavmesh:
                    await PreviewVnavmeshMoveAsync(position);
                    break;
                case MoveType.无:
                case MoveType.传送:
                default:
                    Teleport(position);
                    break;
            }
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"预览移动失败: {ex.Message}");
        }
    }

    private static unsafe void Teleport(Vector3 position)
    {
        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return;

        localPlayer.ToStruct()->SetPosition(position.X, position.Y, position.Z);
        KeyEmulationHelper.SendKeypress(Keys.W);
    }

    private static async Task PreviewPathfindMoveAsync(Vector3 position)
    {
        using var movementController = new MovementInputController();
        movementController.DesiredPosition = position;
        movementController.Enabled         = true;

        try
        {
            while (true)
            {
                if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
                {
                    await Task.Delay(100);
                    continue;
                }

                if (Vector3.DistanceSquared(localPlayer.Position, position) <= 2f)
                    break;

                await Task.Delay(500);
            }
        }
        finally
        {
            movementController.Enabled         = false;
            movementController.DesiredPosition = default;
        }
    }

    private static async Task PreviewVnavmeshMoveAsync(Vector3 position, bool fly = false)
    {
        try
        {
            var timeout = DateTime.Now.AddSeconds(10);
            while (!vnavmeshIPC.GetIsNavReady() && DateTime.Now < timeout)
                await Task.Delay(100);

            if (!vnavmeshIPC.GetIsNavReady())
            {
                NotifyHelper.Instance().ChatError("vnavmesh 未准备就绪");
                return;
            }

            if (!vnavmeshIPC.PathfindAndMoveTo(position, fly))
            {
                NotifyHelper.Instance().ChatError("vnavmesh 寻路启动失败");
                return;
            }

            await Task.Delay(500);

            while (true)
            {
                if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
                {
                    await Task.Delay(100);
                    continue;
                }

                var distance = Vector3.Distance(localPlayer.Position, position);
                if (distance <= 2f)
                    break;

                if (!vnavmeshIPC.GetIsPathfindRunning() && !vnavmeshIPC.GetIsNavPathfindInProgress())
                {
                    await Task.Delay(500);
                    distance = Vector3.Distance(localPlayer.Position, position);
                    if (distance > 2f)
                        NotifyHelper.Instance().Chat($"vnavmesh 寻路结束但未到达目标, 距离 {distance:F2} 米");
                    break;
                }

                await Task.Delay(100);
            }
        }
        finally
        {
            vnavmeshIPC.StopPathfind();
        }
    }

    private static bool TryParsePosition(string text, out Vector3 position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        try
        {
            text = text.Replace(" ", "");

            var xIndex = text.IndexOf("X:", StringComparison.OrdinalIgnoreCase);
            var yIndex = text.IndexOf("Y:", StringComparison.OrdinalIgnoreCase);
            var zIndex = text.IndexOf("Z:", StringComparison.OrdinalIgnoreCase);

            if (xIndex == -1 || yIndex == -1 || zIndex == -1) return false;

            var xStart = xIndex + 2;
            var xEnd   = text.IndexOf(',', xStart);
            if (xEnd == -1) return false;
            var xStr = text.Substring(xStart, xEnd - xStart);

            var yStart = yIndex + 2;
            var yEnd   = text.IndexOf(',', yStart);
            if (yEnd == -1) return false;
            var yStr = text.Substring(yStart, yEnd - yStart);

            var zStart = zIndex + 2;
            var zEnd   = text.Length;

            var commaAfterZ = text.IndexOf(',', zStart);
            if (commaAfterZ != -1)
                zEnd = commaAfterZ;

            var zStr = text.Substring(zStart, zEnd - zStart);

            if (!float.TryParse(xStr, out var x)) return false;
            if (!float.TryParse(yStr, out var y)) return false;
            if (!float.TryParse(zStr, out var z)) return false;

            position = new Vector3(x, y, z);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
