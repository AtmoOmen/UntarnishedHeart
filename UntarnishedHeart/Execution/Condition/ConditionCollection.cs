using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Execution.Condition;

[JsonConverter(typeof(ConditionCollectionJSONConverter))]
public sealed class ConditionCollection : IEquatable<ConditionCollection>
{
    public List<ConditionBase> Conditions { get; set; } = [];

    public ConditionRelationType RelationType { get; set; } = ConditionRelationType.And;

    public ConditionExecuteType ExecuteType { get; set; } = ConditionExecuteType.Wait;

    public int MinExecuteCount { get; set; } = 1;

    public int MaxExecuteCount { get; set; } = 1;

    public int IntervalMs { get; set; }

    private ConditionBase? conditionToCopy;

    public bool IsEmpty => Conditions.Count == 0;

    public bool Evaluate()
    {
        var context = ConditionContext.Create();
        return Evaluate(context);
    }

    public bool Evaluate(in ConditionContext context)
    {
        if (Conditions.Count == 0)
            return true;

        return RelationType switch
        {
            ConditionRelationType.And => EvaluateAnd(context),
            ConditionRelationType.Or  => EvaluateOr(context),
            _                         => false
        };
    }

    private bool EvaluateAnd(in ConditionContext context)
    {
        foreach (var condition in Conditions)
        {
            if (!condition.Evaluate(context))
                return false;
        }

        return true;
    }

    private bool EvaluateOr(in ConditionContext context)
    {
        foreach (var condition in Conditions)
        {
            if (condition.Evaluate(context))
                return true;
        }

        return false;
    }

    public bool Equals(ConditionCollection? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return RelationType    == other.RelationType    &&
               ExecuteType     == other.ExecuteType     &&
               MinExecuteCount == other.MinExecuteCount &&
               MaxExecuteCount == other.MaxExecuteCount &&
               IntervalMs      == other.IntervalMs      &&
               Conditions.SequenceEqual(other.Conditions);
    }

    public override bool Equals(object? obj) => Equals(obj as ConditionCollection);

    public override int GetHashCode() => HashCode.Combine((int)RelationType, (int)ExecuteType, MinExecuteCount, MaxExecuteCount, IntervalMs, Conditions.Count);

    public void Draw()
    {
        ImGui.SetNextItemWidth(320f * GlobalUIScale);
        ConditionBase.DrawEnumLocalizedSelector
        (
            "处理类型###ExecuteTypeCombo",
            "选择处理类型",
            "暂无可选处理类型",
            ExecuteType,
            value => ExecuteType = value,
            static value => value.GetDescription()
        );

        switch (ExecuteType)
        {
            case ConditionExecuteType.Repeat:
            case ConditionExecuteType.Sustain:
                DrawCountSettings();
                break;
            case ConditionExecuteType.Skip:
            case ConditionExecuteType.Wait:
                MinExecuteCount = 1;
                MaxExecuteCount = 1;
                IntervalMs      = 0;
                break;
        }

        ImGui.SetNextItemWidth(320f * GlobalUIScale);
        ConditionBase.DrawEnumLocalizedSelector
        (
            "关系类型###RelationTypeCombo",
            "选择关系类型",
            "暂无可选关系类型",
            RelationType,
            value => RelationType = value,
            static value => value.GetDescription()
        );

        ImGui.NewLine();

        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新条件", true))
            Conditions.Add(ConditionBase.CreateDefaultCondition(ConditionDetectType.Health));

        ImGui.Spacing();

        for (var i = 0; i < Conditions.Count; i++)
        {
            var condition = Conditions[i];
            var label     = BuildConditionLabel(i, condition);

            using (var node = ImRaii.TreeNode($"{label}###Condition-{i}"))
            {
                if (node)
                {
                    condition     = condition.Draw(i);
                    Conditions[i] = condition;
                }
                else
                    DrawConditionContextMenu(i, condition);
            }
        }
    }

    private void DrawCountSettings()
    {
        var minExecuteCount = MinExecuteCount;
        ImGui.SetNextItemWidth(160f * GlobalUIScale);
        if (ImGui.InputInt("最少执行次数###MinExecuteCountInput", ref minExecuteCount))
            MinExecuteCount = Math.Max(0, minExecuteCount);

        var maxExecuteCount = MaxExecuteCount;
        ImGui.SetNextItemWidth(160f * GlobalUIScale);
        if (ImGui.InputInt("最大执行次数 (<= 0 为无限)###MaxExecuteCountInput", ref maxExecuteCount))
            MaxExecuteCount = maxExecuteCount;

        var intervalMs = IntervalMs;
        ImGui.SetNextItemWidth(160f * GlobalUIScale);
        if (ImGui.InputInt("重复间隔 (ms)###IntervalMsInput", ref intervalMs))
            IntervalMs = Math.Max(0, intervalMs);
    }

    private void DrawConditionContextMenu(int index, ConditionBase condition)
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"ConditionContentMenu_{index}");

        using var context = ImRaii.ContextPopupItem($"ConditionContentMenu_{index}");
        if (!context) return;

        ImGui.Text(BuildConditionLabel(index, condition));
        ImGui.Separator();

        if (ImGui.MenuItem("复制"))
            conditionToCopy = ConditionBase.Copy(condition);

        if (conditionToCopy != null)
        {
            if (ImGui.MenuItem("粘贴至本条"))
                contextOperation = StepOperationType.Paste;

            if (ImGui.MenuItem("向上插入粘贴"))
                contextOperation = StepOperationType.PasteUp;

            if (ImGui.MenuItem("向下插入粘贴"))
                contextOperation = StepOperationType.PasteDown;
        }

        if (ImGui.MenuItem("删除"))
            contextOperation = StepOperationType.Delete;

        if (index > 0 && ImGui.MenuItem("上移"))
            contextOperation = StepOperationType.MoveUp;

        if (index < Conditions.Count - 1 && ImGui.MenuItem("下移"))
            contextOperation = StepOperationType.MoveDown;

        ImGui.Separator();

        if (ImGui.MenuItem("向上插入新条件"))
            contextOperation = StepOperationType.InsertUp;

        if (ImGui.MenuItem("向下插入新条件"))
            contextOperation = StepOperationType.InsertDown;

        ImGui.Separator();

        if (ImGui.MenuItem("复制并插入本条"))
            contextOperation = StepOperationType.PasteCurrent;

        CollectionOperationHelper.Apply
        (
            Conditions,
            index,
            contextOperation,
            createNew: () => ConditionBase.CreateDefaultCondition(ConditionDetectType.Health),
            createClipboardCopy: conditionToCopy == null ? null : () => ConditionBase.Copy(conditionToCopy),
            createCurrentCopy: () => ConditionBase.Copy(condition)
        );
    }

    private static string BuildConditionLabel(int index, ConditionBase condition) => $"{index}. {condition.Name}";

    public static ConditionCollection Copy(ConditionCollection source) =>
        new()
        {
            Conditions      = source.Conditions.Select(ConditionBase.Copy).ToList(),
            RelationType    = source.RelationType,
            ExecuteType     = source.ExecuteType,
            MinExecuteCount = source.MinExecuteCount,
            MaxExecuteCount = source.MaxExecuteCount,
            IntervalMs      = source.IntervalMs
        };
}
