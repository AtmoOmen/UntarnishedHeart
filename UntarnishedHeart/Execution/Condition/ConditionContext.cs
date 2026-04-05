using OmenTools.OmenService;

namespace UntarnishedHeart.Execution.Condition;

public readonly record struct ConditionContext
(
    IBattleChara? LocalPlayer,
    IBattleChara? Target
)
{
    public static ConditionContext Create() =>
        new
        (
            DService.Instance().ObjectTable.LocalPlayer,
            TargetManager.Target as IBattleChara
        );
}
