namespace BehaviorTreeSystem
{
    public abstract class DecoratorNode : BTNode
    {
        protected readonly BTNode child;

        protected DecoratorNode(BTNode child, string name = "Decorator") : base(name)
        {
            this.child = child;
        }

        public override void Reset() => child?.Reset();
    }

    public class Inverter : DecoratorNode
    {
        public Inverter(BTNode child, string name = "Inverter") : base(child, name) { }

        public override NodeStatus Evaluate()
        {
            return child.Evaluate() switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                var status => status
            };
        }
    }

    public class Succeeder : DecoratorNode
    {
        public Succeeder(BTNode child, string name = "Succeeder") : base(child, name) { }

        public override NodeStatus Evaluate()
        {
            child.Evaluate();
            return NodeStatus.Success;
        }
    }

    public class Failer : DecoratorNode
    {
        public Failer(BTNode child, string name = "Failer") : base(child, name) { }

        public override NodeStatus Evaluate()
        {
            child.Evaluate();
            return NodeStatus.Failure;
        }
    }

    public class Repeater : DecoratorNode
    {
        private readonly int repeatCount;
        private int currentCount;

        public Repeater(BTNode child, int count, string name = "Repeater") : base(child, name)
        {
            repeatCount = count;
        }

        public override NodeStatus Evaluate()
        {
            if (currentCount >= repeatCount)
                return NodeStatus.Success;

            child.Evaluate();
            currentCount++;
            return NodeStatus.Running;
        }

        public override void Reset()
        {
            base.Reset();
            currentCount = 0;
        }
    }

    public class RepeatUntilFail : DecoratorNode
    {
        public RepeatUntilFail(BTNode child, string name = "RepeatUntilFail") : base(child, name) { }

        public override NodeStatus Evaluate()
        {
            NodeStatus status = child.Evaluate();

            if (status == NodeStatus.Failure)
                return NodeStatus.Success;

            return NodeStatus.Running;
        }
    }
}
