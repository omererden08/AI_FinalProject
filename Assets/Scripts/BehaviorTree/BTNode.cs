namespace BehaviorTreeSystem
{
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    public abstract class BTNode
    {
        public string Name { get; }

        protected BTNode(string name = "Node")
        {
            Name = name;
        }

        public abstract NodeStatus Evaluate();
        public virtual void Reset() { }
    }
}
