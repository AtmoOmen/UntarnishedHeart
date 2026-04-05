using OmenTools.OmenService;

namespace UntarnishedHeart.Execution.CommandCondition;

public readonly record struct CommandConditionContext
(
    IBattleChara? LocalPlayer,
    IBattleChara? Target
)
{
    public static CommandConditionContext Create() =>
        new
        (
            DService.Instance().ObjectTable.LocalPlayer,
            TargetManager.Target as IBattleChara
        );
}
