using Dalamud.Game.ClientState.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetStepV2ToV3Migrator : JsonObjectMigratorBase
{
    private static readonly ConditionFlag[] BusyFlags =
    [
        ConditionFlag.Occupied,
        ConditionFlag.Occupied30,
        ConditionFlag.Occupied33,
        ConditionFlag.Occupied38,
        ConditionFlag.Occupied39,
        ConditionFlag.OccupiedInCutSceneEvent,
        ConditionFlag.OccupiedInEvent,
        ConditionFlag.OccupiedInQuestEvent,
        ConditionFlag.OccupiedSummoningBell,
        ConditionFlag.WatchingCutscene,
        ConditionFlag.WatchingCutscene78,
        ConditionFlag.BetweenAreas,
        ConditionFlag.BetweenAreas51,
        ConditionFlag.InThatPosition,
        ConditionFlag.TradeOpen,
        ConditionFlag.Crafting,
        ConditionFlag.Unconscious,
        ConditionFlag.MeldingMateria,
        ConditionFlag.Gathering,
        ConditionFlag.OperatingSiegeMachine,
        ConditionFlag.CarryingItem,
        ConditionFlag.CarryingObject,
        ConditionFlag.BeingMoved,
        ConditionFlag.Emoting,
        ConditionFlag.RidingPillion,
        ConditionFlag.Mounting,
        ConditionFlag.Mounting71,
        ConditionFlag.ParticipatingInCustomMatch,
        ConditionFlag.PlayingLordOfVerminion,
        ConditionFlag.ChocoboRacing,
        ConditionFlag.PlayingMiniGame,
        ConditionFlag.Performing,
        ConditionFlag.PreparingToCraft,
        ConditionFlag.Fishing,
        ConditionFlag.Transformed,
        ConditionFlag.UsingHousingFunctions
    ];

    public override int FromVersion => 2;

    public override int ToVersion => 3;

    public override JObject Migrate(JObject jsonObject)
    {
        var name         = PresetStepJsonConverter.ReadString(jsonObject["Note"]);
        var remark       = string.Empty;
        var enterActions = new JArray();
        var bodyActions  = new JArray();
        var exitActions  = new JArray();

        if (PresetStepJsonConverter.ReadBool(jsonObject["StopInCombat"], true))
        {
            enterActions.Add
            (
                CreateWaitGateAction
                (
                    CreateSingleCondition
                    (
                        "GameCondition",
                        new JObject
                        {
                            ["Flag"]           = nameof(ConditionFlag.InCombat),
                            ["ComparisonType"] = nameof(PresenceComparisonType.NotHas)
                        }
                    )
                )
            );
        }

        if (PresetStepJsonConverter.ReadBool(jsonObject["StopWhenBusy"]))
            enterActions.Add(CreateWaitGateAction(CreateBusyConditionCollection()));

        if (PresetStepJsonConverter.ReadBool(jsonObject["StopWhenAnyAlive"]))
        {
            enterActions.Add
            (
                CreateWaitGateAction
                (
                    CreateSingleCondition
                    (
                        nameof(ConditionDetectType.PartyAllDead),
                        new JObject
                        {
                            ["ComparisonType"] = nameof(PresenceComparisonType.Has)
                        }
                    )
                )
            );
        }

        var position = jsonObject["Position"];

        if (position is { Type: JTokenType.Object, HasValues: true } && position.ToString(Formatting.None) != "{\"X\":0.0,\"Y\":0.0,\"Z\":0.0}")
        {
            bodyActions.Add
            (
                CreateAction
                (
                    ExecuteActionKind.MoveToPosition,
                    new JObject
                    {
                        ["Position"]       = position.DeepClone(),
                        ["MoveType"]       = (jsonObject["MoveType"] ?? JValue.CreateString("传送")).DeepClone(),
                        ["WaitForArrival"] = PresetStepJsonConverter.ReadBool(jsonObject["WaitForGetClose"])
                    }
                )
            );
        }

        var selector = CreateSelector(jsonObject);

        if (selector != null)
        {
            if (PresetStepJsonConverter.ReadBool(jsonObject["WaitForTargetSpawn"]))
            {
                bodyActions.Add
                (
                    CreateWaitGateAction
                    (
                        CreateSingleCondition
                        (
                            "NearbyTarget",
                            new JObject
                            {
                                ["ComparisonType"] = PresenceComparisonType.Has.ToString(),
                                ["Selector"]       = selector.DeepClone()
                            }
                        )
                    )
                );
            }

            if (PresetStepJsonConverter.ReadBool(jsonObject["WaitForTarget"], true))
            {
                bodyActions.Add
                (
                    CreateAction
                    (
                        ExecuteActionKind.SelectTarget,
                        new JObject
                        {
                            ["Selector"]  = selector.DeepClone(),
                            ["Condition"] = CreateRepeatSelectCondition(selector)
                        }
                    )
                );
            }
            else bodyActions.Add(CreateAction(ExecuteActionKind.SelectTarget, new JObject { ["Selector"] = selector.DeepClone() }));

            if (PresetStepJsonConverter.ReadBool(jsonObject["InteractWithTarget"]))
            {
                var interactSelector = PresetStepJsonConverter.ReadBool(jsonObject["InteractNeedTargetAnything"], true)
                                           ? new JObject { ["Kind"] = TargetSelectorKind.CurrentTarget.ToString() }
                                           : (JObject)selector.DeepClone();

                bodyActions.Add
                (
                    CreateAction
                    (
                        ExecuteActionKind.InteractTarget,
                        new JObject
                        {
                            ["Selector"]              = interactSelector,
                            ["OpenObjectInteraction"] = true
                        }
                    )
                );
            }
        }

        if (PresetStepJsonConverter.ReadBool(jsonObject["InteractWithNearestObject"]))
            bodyActions.Add(CreateAction(ExecuteActionKind.InteractNearestObject, new JObject()));

        if (!string.IsNullOrWhiteSpace(PresetStepJsonConverter.ReadString(jsonObject["Commands"])))
        {
            bodyActions.Add
            (
                CreateAction
                (
                    ExecuteActionKind.TextCommand,
                    new JObject
                    {
                        ["Commands"]  = PresetStepJsonConverter.ReadString(jsonObject["Commands"]),
                        ["Condition"] = (jsonObject["Condition"] ?? CreateEmptyConditionCollection()).DeepClone()
                    }
                )
            );
        }

        var delay = PresetStepJsonConverter.ReadInt(jsonObject["Delay"], 5000);
        if (delay > 0)
            exitActions.Add(CreateAction(ExecuteActionKind.WaitMilliseconds, new JObject { ["Milliseconds"] = delay }));

        var jumpToIndex = PresetStepJsonConverter.ReadInt(jsonObject["JumpToIndex"], -1);
        if (jumpToIndex >= 0)
            exitActions.Add(CreateAction(ExecuteActionKind.JumpToStep, new JObject { ["StepIndex"] = jumpToIndex }));

        return new JObject
        {
            ["Version"]      = 3,
            ["Name"]         = name,
            ["Remark"]       = remark,
            ["EnterActions"] = enterActions,
            ["BodyActions"]  = bodyActions,
            ["ExitActions"]  = exitActions
        };
    }

    private static JObject? CreateSelector(JObject jsonObject)
    {
        var dataID = PresetStepJsonConverter.ReadUInt(jsonObject["DataID"]);
        if (dataID == 0)
            return null;

        return new JObject
        {
            ["Kind"]              = nameof(TargetSelectorKind.ByObjectKindAndDataID),
            ["ObjectKind"]        = (jsonObject["ObjectKind"] ?? JValue.CreateString("BattleNpc")).DeepClone(),
            ["DataId"]            = dataID,
            ["RequireTargetable"] = PresetStepJsonConverter.ReadBool(jsonObject["TargetNeedTargetable"], true)
        };
    }

    private static JObject CreateBusyConditionCollection()
    {
        var conditions = new JArray();

        foreach (var busyFlag in BusyFlags)
        {
            conditions.Add
            (
                CreateCondition
                (
                    "GameCondition",
                    new JObject
                    {
                        ["Flag"]           = busyFlag.ToString(),
                        ["ComparisonType"] = PresenceComparisonType.NotHas.ToString()
                    }
                )
            );
        }

        return new JObject
        {
            ["Version"]         = 2,
            ["Conditions"]      = conditions,
            ["RelationType"]    = ConditionRelationType.And.ToString(),
            ["ExecuteType"]     = ConditionExecuteType.Wait.ToString(),
            ["MinExecuteCount"] = 1,
            ["MaxExecuteCount"] = 1,
            ["IntervalMs"]      = 0
        };
    }

    private static JObject CreateRepeatSelectCondition(JObject selector) =>
        new()
        {
            ["Version"] = 2,
            ["Conditions"] = new JArray
            (
                CreateCondition
                (
                    "HasSpecificTarget",
                    new JObject
                    {
                        ["ComparisonType"] = PresenceComparisonType.Has.ToString(),
                        ["Selector"]       = selector.DeepClone()
                    }
                )
            ),
            ["RelationType"]    = ConditionRelationType.And.ToString(),
            ["ExecuteType"]     = ConditionExecuteType.Repeat.ToString(),
            ["MinExecuteCount"] = 1,
            ["MaxExecuteCount"] = 0,
            ["IntervalMs"]      = 100
        };

    private static JObject CreateWaitGateAction(JObject conditionCollection) =>
        CreateAction(ExecuteActionKind.WaitMilliseconds, new JObject { ["Milliseconds"] = 0, ["Condition"] = conditionCollection });

    private static JObject CreateSingleCondition(string kind, JObject payload) =>
        new()
        {
            ["Version"]         = 2,
            ["Conditions"]      = new JArray(CreateCondition(kind, payload)),
            ["RelationType"]    = ConditionRelationType.And.ToString(),
            ["ExecuteType"]     = ConditionExecuteType.Wait.ToString(),
            ["MinExecuteCount"] = 1,
            ["MaxExecuteCount"] = 1,
            ["IntervalMs"]      = 0
        };

    private static JObject CreateCondition(string kind, JObject payload)
    {
        var result = new JObject
        {
            ["Version"] = 3,
            ["Kind"]    = kind
        };

        foreach (var property in payload.Properties())
            result[property.Name] = property.Value;

        return result;
    }

    private static JObject CreateAction(ExecuteActionKind kind, JObject payload)
    {
        var result = new JObject
        {
            ["Version"]   = 1,
            ["Kind"]      = kind.ToString(),
            ["Condition"] = CreateEmptyConditionCollection()
        };

        foreach (var property in payload.Properties())
            result[property.Name] = property.Value;

        return result;
    }

    private static JObject CreateEmptyConditionCollection() =>
        new()
        {
            ["Version"]         = 2,
            ["Conditions"]      = new JArray(),
            ["RelationType"]    = ConditionRelationType.And.ToString(),
            ["ExecuteType"]     = ConditionExecuteType.Wait.ToString(),
            ["MinExecuteCount"] = 1,
            ["MaxExecuteCount"] = 1,
            ["IntervalMs"]      = 0
        };

    private static string AppendMigrationRemark(string remark, string line) =>
        string.IsNullOrWhiteSpace(remark)
            ? $"[迁移提示] {line}"
            : $"{remark}\n[迁移提示] {line}";
}
