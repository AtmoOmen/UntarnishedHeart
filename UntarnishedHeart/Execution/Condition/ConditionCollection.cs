using Dalamud.Interface.Utility;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Execution.Condition;

public class ConditionCollection
{
    public List<Condition> Conditions   { get; set; } = [];
    public ConditionRelationType          RelationType { get; set; } = ConditionRelationType.And;
    public ConditionExecuteType           ExecuteType  { get; set; } = ConditionExecuteType.Wait;
    public float                        TimeValue    { get; set; }

    private Condition? conditionToCopy;

    public void Draw()
    {
        ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);

        using (var combo = ImRaii.Combo("处理类型###ExecuteTypeCombo", ExecuteType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var executeType in Enum.GetValues<ConditionExecuteType>())
                {
                    if (ImGui.Selectable($"{executeType.GetDescription()}", ExecuteType == executeType))
                        ExecuteType = executeType;
                    ImGuiOm.TooltipHover($"{executeType.GetDescription()}");
                }
            }
        }

        if (ExecuteType == ConditionExecuteType.Repeat)
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
                foreach (var relationType in Enum.GetValues<ConditionRelationType>())
                {
                    if (ImGui.Selectable($"{relationType.GetDescription()}", RelationType == relationType))
                        RelationType = relationType;
                    ImGuiOm.TooltipHover($"{relationType.GetDescription()}");
                }
            }
        }

        ImGui.NewLine();

        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新条件", true))
            Conditions.Add(new HealthCondition());

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

        void DrawStepContextMenu(int i, Condition step)
        {
            var contextOperation = StepOperationType.Pass;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup($"ConditionContentMenu_{i}");

            using var context = ImRaii.ContextPopupItem($"ConditionContentMenu_{i}");
            if (!context) return;

            if (ImGui.MenuItem("复制"))
                conditionToCopy = Condition.Copy(step);

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
                createNew: () => new HealthCondition(),
                createClipboardCopy: conditionToCopy == null ? null : () => Condition.Copy(conditionToCopy),
                createCurrentCopy: () => Condition.Copy(step)
            );
        }
    }

    public bool IsConditionsTrue()
    {
        var context = ConditionContext.Create();

        return RelationType switch
        {
            ConditionRelationType.And => Conditions.All(x => x.Evaluate(context)),
            ConditionRelationType.Or  => Conditions.Any(x => x.Evaluate(context)),
            _                       => false
        };
    }

    public static ConditionCollection Copy(ConditionCollection source)
    {
        var conditions = new List<Condition>();
        source.Conditions.ForEach(x => conditions.Add(Condition.Copy(x)));

        return new ConditionCollection
        {
            Conditions   = conditions.ToList(),
            RelationType = source.RelationType,
            ExecuteType  = source.ExecuteType,
            TimeValue    = source.TimeValue
        };
    }
}
