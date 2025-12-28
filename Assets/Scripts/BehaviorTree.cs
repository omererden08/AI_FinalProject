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
                    .Condition(ShouldChase, "ShouldChase")
                    .Action(SetChaseState, "SetChase")
                .End()
                .Sequence("SearchBranch")
                    .Condition(ShouldSearch, "ShouldSearch")
                    .Action(SetSearchState, "SetSearch")
                .End()
                .Sequence("SearchCompleteBranch")
                    .Condition(IsSearching, "IsSearching")
                    .Condition(SearchComplete, "SearchComplete")
                    .Action(SetReturnState, "SetReturn")
                .End()
                .Sequence("ContinueSearchBranch")
                    .Condition(IsSearching, "IsSearching")
                    .Action(KeepSearching, "KeepSearching")
                .End()
                .Sequence("PatrolBranch")
                    .Action(SetPatrolState, "SetPatrol")
                .End()
            .End()
            .Build();
    }

    private bool CanSeePlayer() => guard.CanSeePlayer();
    private bool ShouldChase() => guard.CanSeePlayer() || (guard.CurrentState == GuardState.Chase && guard.ChaseTimer > 0);
    private bool ShouldSearch() => guard.CurrentState == GuardState.Chase && !guard.CanSeePlayer() && guard.ChaseTimer <= 0 && guard.HasLastKnownPosition;
    private bool IsSearching() => guard.CurrentState == GuardState.Search;
    private bool SearchComplete() => guard.IsSearchComplete();

    private NodeStatus SetChaseState() => SetState(GuardState.Chase);
    private NodeStatus SetSearchState()
    {
        guard.StartSearch();
        return SetState(GuardState.Search);
    }
    private NodeStatus KeepSearching() => SetState(GuardState.Search);
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