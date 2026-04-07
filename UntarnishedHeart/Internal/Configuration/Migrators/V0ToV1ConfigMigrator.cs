using UntarnishedHeart.Execution.ExecuteAction.Implementations;

namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal sealed class V0ToV1ConfigMigrator : ConfigMigratorBase
{
    public override int FromVersion => 0;

    public override int ToVersion => 1;

    public override void Migrate(PluginConfig config)
    {
        foreach (var dutyOptions in config.Routes.SelectMany(route => route.Steps)
                                          .SelectMany(step => step.BodyActions)
                                          .OfType<ExecutePresetAction>()
                                          .Select(action => action.DutyOptions))
        {
            dutyOptions.LeaderMode        = config.LeaderMode;
            dutyOptions.AutoRecommendGear = config.AutoRecommendGear;
            dutyOptions.RunTimes          = config.RunTimes;
        }
    }
}
