using Dalamud.Interface.Utility;
using UntarnishedHeart.Execution.CommandCondition.Enums;
using UntarnishedHeart.Execution.CommandCondition.Implementations;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Execution.CommandCondition;

public class CommandCondition
{
    public List<CommandSingleCondition> Conditions   { get; set; } = [];
    public CommandRelationType          RelationType { get; set; } = CommandRelationType.And;
    public CommandExecuteType           ExecuteType  { get; set; } = CommandExecuteType.Wait;
    public float                        TimeValue    { get; set; }

    private CommandSingleCondition? conditionToCopy;

    public void Draw()
    {
        ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);

        using (var combo = ImRaii.Combo("处理类型###ExecuteTypeCombo", ExecuteType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var executeType in Enum.GetValues<CommandExecuteType>())
                {
                    if (ImGui.Selectable($"{executeType.GetDescription()}", ExecuteType == executeType))
                        ExecuteType = executeType;
                    ImGuiOm.TooltipHover($"{executeType.GetDescription()}");
                }
            }
        }

        if (ExecuteType == CommandExecuteType.Repeat)
        {
            var timeValue = TimeValue;
            ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputFloat("重复间隔 (ms)###TimeValueInput", ref timeValue))
                TimeValue = timeValue;
            ImGuiOm.TooltipHover("每执行一轮间的时间间隔");
        }

        ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);

        using (var combo = ImRaii.Combo("关系类型###RelationTypeCombo", RelationType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var relationType in Enum.GetValues<CommandRelationType>())
                {
                    if (ImGui.Selectable($"{relationType.GetDescription()}", RelationType == relationType))
                        RelationType = relationType;
                    ImGuiOm.TooltipHover($"{relationType.GetDescription()}");
                }
            }
        }

        ImGui.NewLine();

        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新条件", true))
            Conditions.Add(new HealthCommandCondition());

        ImGui.Spacing();

        for (var i = 0; i < Conditions.Count; i++)
        {
            var step = Conditions[i];

            using var node = ImRaii.TreeNode($"第 {i + 1} 条###Step-{i}");

            if (!node)
            {
                DrawStepContextMenu(i, step);
                continue;
            }

            step          = step.Draw(i);
            Conditions[i] = step;
            DrawStepContextMenu(i, step);
        }

        return;

        void DrawStepContextMenu(int i, CommandSingleCondition step)
        {
            var contextOperation = StepOperationType.Pass;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup($"ConditionContentMenu_{i}");

            using var context = ImRaii.ContextPopupItem($"ConditionContentMenu_{i}");
            if (!context) return;

            if (ImGui.MenuItem("复制"))
                conditionToCopy = CommandSingleCondition.Copy(step);

            if (conditionToCopy != null)
            {
                using (ImRaii.Group())
                {
                    if (ImGui.MenuItem("粘贴至本步"))
                        contextOperation = StepOperationType.Paste;

                    if (ImGui.MenuItem("向上插入粘贴"))
                        contextOperation = StepOperationType.PasteUp;

                    if (ImGui.MenuItem("向下插入粘贴"))
                        contextOperation = StepOperationType.PasteDown;
                }
            }

            if (ImGui.MenuItem("删除"))
                contextOperation = StepOperationType.Delete;

            if (i > 0)
            {
                if (ImGui.MenuItem("上移"))
                    contextOperation = StepOperationType.MoveUp;
            }

            if (i < Conditions.Count - 1)
            {
                if (ImGui.MenuItem("下移"))
                    contextOperation = StepOperationType.MoveDown;
            }

            ImGui.Separator();

            if (ImGui.MenuItem("向上插入新步骤"))
                contextOperation = StepOperationType.InsertUp;

            if (ImGui.MenuItem("向下插入新步骤"))
                contextOperation = StepOperationType.InsertDown;

            ImGui.Separator();

            if (ImGui.MenuItem("复制并插入本步骤"))
                contextOperation = StepOperationType.PasteCurrent;

            CollectionOperationHelper.Apply
            (
                Conditions,
                i,
                contextOperation,
                createNew: () => new HealthCommandCondition(),
                createClipboardCopy: conditionToCopy == null ? null : () => CommandSingleCondition.Copy(conditionToCopy),
                createCurrentCopy: () => CommandSingleCondition.Copy(step)
            );
        }
    }

    public bool IsConditionsTrue()
    {
        var context = CommandConditionContext.Create();

        return RelationType switch
        {
            CommandRelationType.And => Conditions.All(x => x.Evaluate(context)),
            CommandRelationType.Or  => Conditions.Any(x => x.Evaluate(context)),
            _                       => false
        };
    }

    public static CommandCondition Copy(CommandCondition source)
    {
        var conditions = new List<CommandSingleCondition>();
        source.Conditions.ForEach(x => conditions.Add(CommandSingleCondition.Copy(x)));

        return new CommandCondition
        {
            Conditions   = conditions.ToList(),
            RelationType = source.RelationType,
            ExecuteType  = source.ExecuteType,
            TimeValue    = source.TimeValue
        };
    }
}
