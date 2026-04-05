using UntarnishedHeart.Execution.Route.Enums;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal sealed class V0ToV1ConfigMigrator : ConfigMigratorBase
{
    public override int FromVersion => 0;

    public override int ToVersion => 1;

    public override void Migrate(PluginConfig config)
    {
        foreach (var dutyOptions in config.Routes.SelectMany(route => route.Steps)
                                                 .Where(step => step.StepType == RouteStepType.SwitchPreset)
                                                 .Select(step => step.DutyOptions))
        {
            dutyOptions.LeaderMode        = config.LeaderMode;
            dutyOptions.AutoRecommendGear = config.AutoRecommendGear;
            dutyOptions.RunTimes          = config.RunTimes;
        }
    }
}
