using System.Collections.Generic;

namespace BehaviorTreeSystem
{
    public abstract class CompositeNode : BTNode
    {
        protected List<BTNode> children = new List<BTNode>();

        public CompositeNode(string name = "Composite") : base(name) { }

        public void AddChild(BTNode child)
        {
            children.Add(child);
        }

        public void AddChildren(params BTNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override void Reset()
        {
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }

    public class Selector : CompositeNode
    {
        public Selector(string name = "Selector") : base(name) { }

        public override NodeStatus Evaluate()
        {
            foreach (var child in children)
            {
                NodeStatus status = child.Evaluate();

                if (status == NodeStatus.Success)
                    return NodeStatus.Success;

                if (status == NodeStatus.Running)
                    return NodeStatus.Running;
            }

            return NodeStatus.Failure;
        }
    }

    public class Sequence : CompositeNode
    {
        public Sequence(string name = "Sequence") : base(name) { }

        public override NodeStatus Evaluate()
        {
            foreach (var child in children)
            {
                NodeStatus status = child.Evaluate();

                if (status == NodeStatus.Failure)
                    return NodeStatus.Failure;

                if (status == NodeStatus.Running)
                    return NodeStatus.Running;
            }

            return NodeStatus.Success;
        }
    }

    public class Parallel : CompositeNode
    {
        private int successThreshold;

        public Parallel(int requiredSuccesses = 1, string name = "Parallel") : base(name)
        {
            successThreshold = requiredSuccesses;
        }

        public override NodeStatus Evaluate()
        {
            int successCount = 0;
            int failureCount = 0;

            foreach (var child in children)
            {
                NodeStatus status = child.Evaluate();

                if (status == NodeStatus.Success)
                    successCount++;
                else if (status == NodeStatus.Failure)
                    failureCount++;
            }

            if (successCount >= successThreshold)
                return NodeStatus.Success;

            if (failureCount > children.Count - successThreshold)
                return NodeStatus.Failure;

            return NodeStatus.Running;
        }
    }
}
