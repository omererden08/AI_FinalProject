using System;

namespace BehaviorTreeSystem
{
    public class ConditionNode : BTNode
    {
        private readonly Func<bool> condition;

        public ConditionNode(Func<bool> condition, string name = "Condition") : base(name)
        {
            this.condition = condition;
        }

        public override NodeStatus Evaluate() => 
            condition() ? NodeStatus.Success : NodeStatus.Failure;
    }

    public class ActionNode : BTNode
    {
        private readonly Func<NodeStatus> action;

        public ActionNode(Func<NodeStatus> action, string name = "Action") : base(name)
        {
            this.action = action;
        }

        public override NodeStatus Evaluate() => action();
    }

    public class SimpleActionNode : BTNode
    {
        private readonly Action action;

        public SimpleActionNode(Action action, string name = "SimpleAction") : base(name)
        {
            this.action = action;
        }

        public override NodeStatus Evaluate()
        {
            action();
            return NodeStatus.Success;
        }
    }

    public class WaitNode : BTNode
    {
        private readonly float duration;
        private readonly Func<float> getTime;
        private float startTime;
        private bool isWaiting;

        public WaitNode(float seconds, Func<float> timeProvider, string name = "Wait") : base(name)
        {
            duration = seconds;
            getTime = timeProvider;
        }

        public override NodeStatus Evaluate()
        {
            if (!isWaiting)
            {
                startTime = getTime();
                isWaiting = true;
            }

            if (getTime() - startTime < duration)
                return NodeStatus.Running;

            isWaiting = false;
            return NodeStatus.Success;
        }

        public override void Reset() => isWaiting = false;
    }
}
