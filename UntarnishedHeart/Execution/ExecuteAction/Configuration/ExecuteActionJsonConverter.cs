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

public sealed class ExecuteActionJsonConverter : JsonConverter<ExecuteActionBase>
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
        var kind      = PresetStepJsonConverter.ReadEnum(obj["Kind"], ExecuteActionKind.WaitMilliseconds);
        var condition = PresetStepJsonConverter.ReadObject(obj["Condition"], serializer, new ConditionCollection());

        return kind switch
        {
            ExecuteActionKind.WaitMilliseconds => new WaitMillisecondsAction
            {
                Milliseconds = PresetStepJsonConverter.ReadInt(obj["Milliseconds"]),
                Condition    = condition
            },
            ExecuteActionKind.JumpToStep => new JumpToStepAction
            {
                StepIndex = PresetStepJsonConverter.ReadInt(obj["StepIndex"], -1),
                Condition = condition
            },
            ExecuteActionKind.RestartCurrentStep => new RestartCurrentStepAction
            {
                Condition = condition
            },
            ExecuteActionKind.JumpToAction => new JumpToActionAction
            {
                ActionIndex = PresetStepJsonConverter.ReadInt(obj["ActionIndex"], -1),
                Condition   = condition
            },
            ExecuteActionKind.RestartCurrentAction => new RestartCurrentActionAction
            {
                Condition = condition
            },
            ExecuteActionKind.LeaveDutyAndEndPreset => new LeaveDutyAndEndAction
            {
                Condition = condition
            },
            ExecuteActionKind.LeaveDutyAndRestartPreset => new LeaveDutyAndRestartAction
            {
                Condition = condition
            },
            ExecuteActionKind.TextCommand => new TextCommandAction
            {
                Commands  = PresetStepJsonConverter.ReadString(obj["Commands"]),
                Condition = condition
            },
            ExecuteActionKind.SelectTarget => new SelectTargetAction
            {
                Selector  = PresetStepJsonConverter.ReadObject(obj["Selector"], serializer, new TargetSelector()),
                Condition = condition
            },
            ExecuteActionKind.InteractTarget => new InteractTargetAction
            {
                Selector              = PresetStepJsonConverter.ReadObject(obj["Selector"], serializer, new TargetSelector()),
                OpenObjectInteraction = PresetStepJsonConverter.ReadBool(obj["OpenObjectInteraction"]),
                Condition             = condition
            },
            ExecuteActionKind.InteractNearestObject => new InteractNearestObjectAction
            {
                Condition = condition
            },
            ExecuteActionKind.UseAction => new UseActionExecuteAction
            {
                Action         = PresetStepJsonConverter.ReadObject(obj["Action"],         serializer, new ActionReference()),
                TargetSelector = PresetStepJsonConverter.ReadObject(obj["TargetSelector"], serializer, new TargetSelector()),
                Location       = PresetStepJsonConverter.ReadObject(obj["Location"],       serializer, default(Vector3)),
                UseLocation    = PresetStepJsonConverter.ReadBool(obj["UseLocation"]),
                Condition      = condition
            },
            ExecuteActionKind.MoveToPosition => new MoveToPositionAction
            {
                Position  = PresetStepJsonConverter.ReadObject(obj["Position"], serializer, default(Vector3)),
                MoveType  = PresetStepJsonConverter.ReadEnum(obj["MoveType"], MoveType.传送),
                Condition = condition
            },
            _ => new WaitMillisecondsAction { Condition = condition }
        };
    }
}
