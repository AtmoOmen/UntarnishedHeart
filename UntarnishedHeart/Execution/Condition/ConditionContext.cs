using OmenTools.OmenService;

namespace UntarnishedHeart.Execution.Condition;

public readonly record struct ConditionContext
(
    IBattleChara? LocalPlayer,
    IBattleChara? Target,
    int           CompletedDutyCount = 0
)
{
    public static ConditionContext Create(int completedDutyCount = 0) =>
        new
        (
            DService.Instance().ObjectTable.LocalPlayer,
            TargetManager.Target as IBattleChara,
            completedDutyCount
        );
}
