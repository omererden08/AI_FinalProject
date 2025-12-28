using BehaviorTreeSystem;

public class GuardBehaviorTree
{
    private readonly GuardAI guard;
    private readonly BehaviorTreeRunner treeRunner;
    private GuardState currentDecision;

    public GuardBehaviorTree(GuardAI guard)
    {
        this.guard = guard;
        this.currentDecision = GuardState.Patrol;
        this.treeRunner = new BehaviorTreeRunner(BuildTree());
    }

    private BTNode BuildTree()
    {
        return new BehaviorTreeBuilder()
            .Selector("GuardAI_Root")
                .Sequence("ChaseBranch")
                    .Condition(CanSeePlayer, "CanSeePlayer")
                    .Action(SetChaseState, "SetChase")
                .End()
                .Sequence("ReturnBranch")
                    .Condition(WasChasing, "WasChasing")
                    .Condition(LostPlayer, "LostPlayer")
                    .Action(SetReturnState, "SetReturn")
                .End()
                .Sequence("PatrolBranch")
                    .Action(SetPatrolState, "SetPatrol")
                .End()
            .End()
            .Build();
    }

    private bool CanSeePlayer() => guard.CanSeePlayer();
    private bool WasChasing() => guard.CurrentState == GuardState.Chase;
    private bool LostPlayer() => !guard.CanSeePlayer();

    private NodeStatus SetChaseState() => SetState(GuardState.Chase);
    private NodeStatus SetReturnState() => SetState(GuardState.ReturnToPatrol);
    private NodeStatus SetPatrolState() => SetState(GuardState.Patrol);

    private NodeStatus SetState(GuardState state)
    {
        currentDecision = state;
        return NodeStatus.Success;
    }

    public GuardState Evaluate()
    {
        treeRunner.Tick();
        return currentDecision;
    }

    public void SetDebugEnabled(bool enabled)
    {
        treeRunner.SetDebug(enabled);
    }
}