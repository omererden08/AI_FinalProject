using System;
using System.Collections.Generic;

namespace BehaviorTreeSystem
{
    public class BehaviorTreeBuilder
    {
        private readonly Stack<CompositeNode> compositeStack = new Stack<CompositeNode>();
        private BTNode rootNode;

        public BehaviorTreeBuilder Selector(string name = "Selector")
        {
            var node = new Selector(name);
            AddNode(node);
            compositeStack.Push(node);
            return this;
        }

        public BehaviorTreeBuilder Sequence(string name = "Sequence")
        {
            var node = new Sequence(name);
            AddNode(node);
            compositeStack.Push(node);
            return this;
        }

        public BehaviorTreeBuilder Parallel(int requiredSuccesses = 1, string name = "Parallel")
        {
            var node = new Parallel(requiredSuccesses, name);
            AddNode(node);
            compositeStack.Push(node);
            return this;
        }

        public BehaviorTreeBuilder End()
        {
            if (compositeStack.Count > 0)
                compositeStack.Pop();
            return this;
        }

        public BehaviorTreeBuilder Condition(Func<bool> condition, string name = "Condition")
        {
            AddNode(new ConditionNode(condition, name));
            return this;
        }

        public BehaviorTreeBuilder Action(Func<NodeStatus> action, string name = "Action")
        {
            AddNode(new ActionNode(action, name));
            return this;
        }

        public BehaviorTreeBuilder Do(Action action, string name = "Do")
        {
            AddNode(new SimpleActionNode(action, name));
            return this;
        }

        public BehaviorTreeBuilder Wait(float seconds, Func<float> timeProvider, string name = "Wait")
        {
            AddNode(new WaitNode(seconds, timeProvider, name));
            return this;
        }

        public BehaviorTreeBuilder Inverter(BTNode child, string name = "Inverter")
        {
            AddNode(new Inverter(child, name));
            return this;
        }

        public BTNode Build() => rootNode;

        private void AddNode(BTNode node)
        {
            if (compositeStack.Count > 0)
                compositeStack.Peek().AddChild(node);
            else
                rootNode = node;
        }
    }
}
