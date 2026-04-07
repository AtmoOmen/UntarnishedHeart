using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration;

public sealed class ExecuteActionJSONConverter : JsonConverter<ExecuteActionBase>
{
    public override void WriteJson(JsonWriter writer, ExecuteActionBase? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override ExecuteActionBase? ReadJson
    (
        JsonReader         reader,
        Type               objectType,
        ExecuteActionBase? existingValue,
        bool               hasExistingValue,
        JsonSerializer     serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = ExecuteActionJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(ExecuteActionBase value, JsonSerializer serializer)
    {
        var obj = new JObject
        {
            ["Version"]   = ExecuteActionJSONMigrator.CurrentJSONVersion,
            ["Kind"]      = value.Kind.ToString(),
            ["Name"]      = value.Name,
            ["Remark"]    = value.Remark,
            ["Condition"] = JToken.FromObject(value.Condition, serializer)
        };

        switch (value)
        {
            case WaitMillisecondsAction waitMilliseconds:
                obj["Milliseconds"] = waitMilliseconds.Milliseconds;
                break;
            case JumpToStepAction jumpToStep:
                obj["StepIndex"] = jumpToStep.StepIndex;
                break;
            case JumpToActionAction jumpToAction:
                obj["ActionIndex"] = jumpToAction.ActionIndex;
                break;
            case TextCommandAction textCommand:
                obj["Commands"] = textCommand.Commands;
                break;
            case SelectTargetAction selectTarget:
                obj["Selector"] = JToken.FromObject(selectTarget.Selector, serializer);
                break;
            case InteractTargetAction interactTarget:
                obj["Selector"]              = JToken.FromObject(interactTarget.Selector, serializer);
                obj["OpenObjectInteraction"] = interactTarget.OpenObjectInteraction;
                break;
            case UseActionExecuteAction useAction:
                obj["Action"]         = JToken.FromObject(useAction.Action,         serializer);
                obj["TargetSelector"] = JToken.FromObject(useAction.TargetSelector, serializer);
                obj["Location"]       = JToken.FromObject(useAction.Location,       serializer);
                obj["UseLocation"]    = useAction.UseLocation;
                break;
            case MoveToPositionAction moveToPosition:
                obj["Position"] = JToken.FromObject(moveToPosition.Position, serializer);
                obj["MoveType"] = JToken.FromObject(moveToPosition.MoveType, serializer);
                break;
        }

        return obj;
    }

    internal static ExecuteActionBase DeserializeCurrent(JObject obj, JsonSerializer serializer)
    {
        var kind      = PresetStepJsonConverter.ReadEnum(obj["Kind"], ExecuteActionKind.Wait);
        var condition = PresetStepJsonConverter.ReadObject(obj["Condition"], serializer, new ConditionCollection());
        var name      = PresetStepJsonConverter.ReadString(obj["Name"], kind.GetDescription());
        var remark    = PresetStepJsonConverter.ReadString(obj["Remark"]);

        return kind switch
        {
            ExecuteActionKind.Wait => PopulateCommonFields
            (
                new WaitMillisecondsAction
                {
                    Milliseconds = PresetStepJsonConverter.ReadInt(obj["Milliseconds"])
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.JumpToStep => PopulateCommonFields
            (
                new JumpToStepAction
                {
                    StepIndex = PresetStepJsonConverter.ReadInt(obj["StepIndex"], -1)
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.RestartCurrentStep => PopulateCommonFields(new RestartCurrentStepAction(), name, remark, condition),
            ExecuteActionKind.JumpToAction => PopulateCommonFields
            (
                new JumpToActionAction
                {
                    ActionIndex = PresetStepJsonConverter.ReadInt(obj["ActionIndex"], -1)
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.RestartCurrentAction => PopulateCommonFields(new RestartCurrentActionAction(), name, remark, condition),
            ExecuteActionKind.LeaveDutyAndEndPreset => PopulateCommonFields(new LeaveDutyAndEndAction(), name, remark, condition),
            ExecuteActionKind.LeaveDutyAndRestartPreset => PopulateCommonFields(new LeaveDutyAndRestartAction(), name, remark, condition),
            ExecuteActionKind.TextCommand => PopulateCommonFields
            (
                new TextCommandAction
                {
                    Commands = PresetStepJsonConverter.ReadString(obj["Commands"])
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.SelectTarget => PopulateCommonFields
            (
                new SelectTargetAction
                {
                    Selector = PresetStepJsonConverter.ReadObject(obj["Selector"], serializer, new TargetSelector())
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.InteractTarget => PopulateCommonFields
            (
                new InteractTargetAction
                {
                    Selector              = PresetStepJsonConverter.ReadObject(obj["Selector"], serializer, new TargetSelector()),
                    OpenObjectInteraction = PresetStepJsonConverter.ReadBool(obj["OpenObjectInteraction"])
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.InteractNearestObject => PopulateCommonFields(new InteractNearestObjectAction(), name, remark, condition),
            ExecuteActionKind.UseAction => PopulateCommonFields
            (
                new UseActionExecuteAction
                {
                    Action         = PresetStepJsonConverter.ReadObject(obj["Action"],         serializer, new ActionReference()),
                    TargetSelector = PresetStepJsonConverter.ReadObject(obj["TargetSelector"], serializer, new TargetSelector()),
                    Location       = PresetStepJsonConverter.ReadObject(obj["Location"],       serializer, default(Vector3)),
                    UseLocation    = PresetStepJsonConverter.ReadBool(obj["UseLocation"])
                },
                name,
                remark,
                condition
            ),
            ExecuteActionKind.MoveToPosition => PopulateCommonFields
            (
                new MoveToPositionAction
                {
                    Position = PresetStepJsonConverter.ReadObject(obj["Position"], serializer, default(Vector3)),
                    MoveType = PresetStepJsonConverter.ReadEnum(obj["MoveType"], MoveType.传送)
                },
                name,
                remark,
                condition
            ),
            _ => PopulateCommonFields(new WaitMillisecondsAction(), name, remark, condition)
        };
    }

    private static T PopulateCommonFields<T>(T action, string name, string remark, ConditionCollection condition)
        where T : ExecuteActionBase
    {
        action.Name      = name;
        action.Remark    = remark;
        action.Condition = condition;
        return action;
    }
}
