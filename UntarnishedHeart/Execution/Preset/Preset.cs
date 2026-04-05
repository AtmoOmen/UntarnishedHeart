using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using OmenTools.Interop.Game.Lumina;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset.Configuration;
using UntarnishedHeart.Execution.Preset.Enums;

namespace UntarnishedHeart.Execution.Preset;

[JsonConverter(typeof(PresetJsonConverter))]
public class Preset : IEquatable<Preset>
{
    public int Version { get; set; } = PresetJSONMigrator.CurrentJSONVersion;

    public string Name { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public ushort Zone { get; set; }

    public List<PresetStep> Steps { get; set; } = [];

    public bool AutoOpenTreasures { get; set; }

    public int DutyDelay { get; set; } = 500;

    public bool IsValid => Zone != 0 && Steps.Count > 0 && LuminaGetter.TryGetRow<TerritoryType>(Zone, out _);

    public bool Equals(Preset? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name              == other.Name              &&
               Remark            == other.Remark            &&
               Zone              == other.Zone              &&
               AutoOpenTreasures == other.AutoOpenTreasures &&
               DutyDelay         == other.DutyDelay         &&
               Steps.SequenceEqual(other.Steps);
    }

    public override bool Equals(object? obj) => Equals(obj as Preset);

    public override int GetHashCode() => HashCode.Combine(Name, Remark, Zone, AutoOpenTreasures, DutyDelay, Steps.Count);

    public override string ToString() => $"ExecutorPreset_{Name}_{Zone}_{Steps.Count}Steps";

    public void ExportToClipboard()
    {
        try
        {
            var json   = JsonConvert.SerializeObject(this);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            Clipboard.SetText(base64);
            NotifyHelper.Instance().Chat("已成功导出预设至剪贴板");
        }
        catch (Exception)
        {
            NotifyHelper.Instance().ChatError("尝试导出预设至剪贴板时发生错误");
        }
    }

    public static Preset? ImportFromClipboard()
    {
        try
        {
            var base64 = Clipboard.GetText();
            if (string.IsNullOrEmpty(base64))
                return null;

            var json   = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var preset = JsonConvert.DeserializeObject<Preset>(json);
            if (preset != null)
                NotifyHelper.Instance().Chat("已成功从剪贴板导入预设");

            return preset;
        }
        catch (Exception)
        {
            NotifyHelper.Instance().ChatError("尝试从剪贴板导入预设时发生错误");
            return null;
        }
    }

    public static readonly Preset ExamplePreset0 = new()
    {
        Name = "O5 魔列车",
        Zone = 748,
        Steps =
        [
            new()
            {
                Name = "选中魔列车",
                BodyActions =
                [
                    new SelectTargetAction
                    {
                        Selector =
                        {
                            Kind       = TargetSelectorKind.ByObjectKindAndDataID,
                            ObjectKind = ObjectKind.BattleNpc,
                            DataID     = 8510
                        }
                    },
                    new MoveToPositionAction
                    {
                        Position = new(0, 0, -15)
                    }
                ]
            }
        ]
    };

    public static readonly Preset ExamplePreset1 = new()
    {
        Name = "假火 (测试用)",
        Zone = 1045,
        Steps =
        [
            new()
            {
                Name = "选中伊弗利特",
                BodyActions =
                [
                    new SelectTargetAction
                    {
                        Selector =
                        {
                            Kind       = TargetSelectorKind.ByObjectKindAndDataID,
                            ObjectKind = ObjectKind.BattleNpc,
                            DataID     = 207
                        }
                    },
                    new MoveToPositionAction
                    {
                        Position = new(11, 0, 0)
                    }
                ]
            }
        ]
    };

    public static readonly Preset ExamplePreset2 = new()
    {
        Name = "极风 (测试用)",
        Zone = 297,
        Steps =
        [
            new()
            {
                Name = "移动到起点",
                BodyActions =
                [
                    new MoveToPositionAction
                    {
                        Position       = new(-0.24348414f, -1.9395045f, -14.213441f),
                        WaitForArrival = false
                    },
                    new WaitMillisecondsAction
                    {
                        Milliseconds = 8000
                    }
                ]
            },
            new()
            {
                Name = "移动到第二点",
                BodyActions =
                [
                    new MoveToPositionAction
                    {
                        Position       = new(-0.63603175f, -1.8021163f, 0.6449276f),
                        WaitForArrival = false
                    },
                    new WaitMillisecondsAction
                    {
                        Milliseconds = 5000
                    }
                ]
            }
        ]
    };
}
