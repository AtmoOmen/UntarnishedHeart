using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route.Legacy;
using UntarnishedHeart.Execution.Route.Legacy.Enums;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Route.Configuration.Migrators;

internal sealed class RouteV1ToV2Migrator : JsonObjectMigratorBase
{
    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();

    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = new JObject
        {
            ["Version"] = 2,
            ["Name"]    = PresetStepJsonConverter.ReadString(jsonObject["Name"]),
            ["Remark"]  = PresetStepJsonConverter.ReadString(jsonObject["Remark"], PresetStepJsonConverter.ReadString(jsonObject["Note"]))
        };

        var migratedSteps = new JArray();
        if (jsonObject["Steps"] is JArray steps)
        {
            for (var index = 0; index < steps.Count; index++)
            {
                if (steps[index] is not JObject stepObject)
                    continue;

                migratedSteps.Add(JObject.FromObject(MigrateStep(stepObject, index), Serializer));
            }
        }

        migrated["Steps"] = migratedSteps;
        return migrated;
    }

    private static PresetStep MigrateStep(JObject stepObject, int stepIndex)
    {
        var legacyStep = stepObject.ToObject<RouteStep>(Serializer) ?? new RouteStep();
        var step       = new PresetStep
        {
            Name   = legacyStep.Name,
            Remark = legacyStep.Remark
        };

        switch (legacyStep.StepType)
        {
            case RouteStepType.SwitchPreset:
                step.BodyActions.Add(CreateExecutePresetAction(legacyStep));

                var afterPresetAction = MigrateBranchAction(legacyStep.AfterPresetAction, legacyStep.AfterPresetJumpIndex, stepIndex);
                if (afterPresetAction != null)
                    step.BodyActions.Add(afterPresetAction);
                break;

            case RouteStepType.ConditionCheck:
            {
                var positiveCondition = CreateConditionCollection(CreateCondition(legacyStep), false);
                var trueAction        = MigrateBranchAction(legacyStep.TrueAction, legacyStep.TrueJumpIndex, stepIndex);
                if (trueAction != null)
                {
                    trueAction.Condition = positiveCondition;
                    step.BodyActions.Add(trueAction);
                }

                var falseAction = MigrateBranchAction(legacyStep.FalseAction, legacyStep.FalseJumpIndex, stepIndex);
                if (falseAction != null)
                {
                    falseAction.Condition = CreateConditionCollection(CreateCondition(legacyStep), true);
                    step.BodyActions.Add(falseAction);
                }

                break;
            }
        }

        return step;
    }

    private static ExecutePresetAction CreateExecutePresetAction(RouteStep legacyStep)
    {
        var action = new ExecutePresetAction
        {
            PresetName  = legacyStep.PresetName,
            DutyOptions = legacyStep.DutyOptions == null
                ? new DutyOptions()
                : new DutyOptions
                {
                    LeaderMode           = legacyStep.DutyOptions.LeaderMode,
                    AutoRecommendGear    = legacyStep.DutyOptions.AutoRecommendGear,
                    RunTimes             = legacyStep.DutyOptions.RunTimes,
                    ContentEntryType     = legacyStep.DutyOptions.ContentEntryType,
                    ContentsFinderOption = legacyStep.DutyOptions.ContentsFinderOption.Clone()
                }
        };

        action.ResetMetadata();
        return action;
    }

    private static ExecuteActionBase? MigrateBranchAction(RouteStepActionType actionType, int jumpIndex, int stepIndex) =>
        actionType switch
        {
            RouteStepActionType.RepeatCurrentStep => InitializeAction(new RestartCurrentStepAction()),
            RouteStepActionType.JumpToStep        => InitializeAction(new JumpToStepAction { StepIndex = jumpIndex }),
            RouteStepActionType.EndRoute          => InitializeAction(new LeaveDutyAndEndAction()),
            RouteStepActionType.GoToPreviousStep => stepIndex > 0
                ? InitializeAction(new JumpToStepAction { StepIndex = stepIndex - 1 })
                : InitializeAction(new RestartCurrentStepAction()),
            RouteStepActionType.GoToNextStep => null,
            _                               => null
        };

    private static ConditionCollection CreateConditionCollection(ConditionBase condition, bool negate) =>
        new()
        {
            ExecuteType = ConditionExecuteType.Skip,
            Conditions  = [negate ? NegateCondition(condition) : condition]
        };

    private static ConditionBase CreateCondition(RouteStep legacyStep)
    {
        ConditionBase condition = legacyStep.ConditionType switch
        {
            RouteConditionType.PlayerLevel => new PlayerLevelCondition(),
            RouteConditionType.OptimalPartyRecommendation => new OptimalPartyRecommendationCondition(),
            RouteConditionType.CompletedDutyCount => new CompletedDutyCountCondition(),
            RouteConditionType.AchievementCount => new AchievementCountCondition
            {
                AchievementID = (uint)Math.Max(0, legacyStep.ExtraID)
            },
            RouteConditionType.ItemCount => new ItemCountCondition
            {
                ItemID = (uint)Math.Max(0, legacyStep.ExtraID)
            },
            _ => new PlayerLevelCondition()
        };

        if (condition is RouteValueConditionBase routeValueCondition)
        {
            routeValueCondition.ComparisonType = MapComparisonType(legacyStep.ComparisonType);
            routeValueCondition.ExpectedValue  = legacyStep.ConditionValue;
        }

        condition.ResetMetadata();
        return condition;
    }

    private static ConditionBase NegateCondition(ConditionBase condition)
    {
        var negated = ConditionBase.Copy(condition);

        if (negated is RouteValueConditionBase routeValueCondition)
        {
            routeValueCondition.ComparisonType = routeValueCondition.ComparisonType switch
            {
                NumericComparisonType.GreaterThan        => NumericComparisonType.LessThanOrEqual,
                NumericComparisonType.GreaterThanOrEqual => NumericComparisonType.LessThan,
                NumericComparisonType.LessThan           => NumericComparisonType.GreaterThanOrEqual,
                NumericComparisonType.LessThanOrEqual    => NumericComparisonType.GreaterThan,
                NumericComparisonType.EqualTo            => NumericComparisonType.NotEqualTo,
                NumericComparisonType.NotEqualTo         => NumericComparisonType.EqualTo,
                _                                        => routeValueCondition.ComparisonType
            };
        }

        return negated;
    }

    private static NumericComparisonType MapComparisonType(ComparisonType comparisonType) =>
        comparisonType switch
        {
            ComparisonType.GreaterThan        => NumericComparisonType.GreaterThan,
            ComparisonType.LessThan           => NumericComparisonType.LessThan,
            ComparisonType.Equal              => NumericComparisonType.EqualTo,
            ComparisonType.GreaterThanOrEqual => NumericComparisonType.GreaterThanOrEqual,
            ComparisonType.LessThanOrEqual    => NumericComparisonType.LessThanOrEqual,
            ComparisonType.NotEqual           => NumericComparisonType.NotEqualTo,
            _                                 => NumericComparisonType.EqualTo
        };

    private static T InitializeAction<T>(T action)
        where T : ExecuteActionBase
    {
        action.ResetMetadata();
        return action;
    }
}
