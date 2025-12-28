using UnityEngine;

namespace BehaviorTreeSystem
{
    public class BehaviorTreeRunner
    {
        private BTNode rootNode;
        private bool debugEnabled;

        public BehaviorTreeRunner(BTNode root, bool enableDebug = false)
        {
            rootNode = root;
            debugEnabled = enableDebug;
        }

        public NodeStatus Tick()
        {
            if (rootNode == null)
            {
                Debug.LogWarning("[BehaviorTree] No root node set!");
                return NodeStatus.Failure;
            }

            var status = rootNode.Evaluate();

            if (debugEnabled)
                Debug.Log($"[BehaviorTree] Tick: {status}");

            return status;
        }

        public void Reset() => rootNode?.Reset();
        public void SetRoot(BTNode root) => rootNode = root;
        public void SetDebug(bool enabled) => debugEnabled = enabled;
    }
}
