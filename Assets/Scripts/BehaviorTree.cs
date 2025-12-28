using UnityEngine;

public class BehaviorTree
{
    private GuardAI guard;

    public BehaviorTree(GuardAI guard)
    {
        this.guard = guard;
    }

    public GuardState Evaluate()
    {
        if (guard.CanSeePlayer())
        {
            return GuardState.Chase;
        }

        if (guard.CurrentState == GuardState.Chase && !guard.CanSeePlayer())
        {
            return GuardState.ReturnToPatrol;
        }

        return GuardState.Patrol;
    }
}
