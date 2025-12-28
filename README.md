# Guard AI - Behavior Tree Implementation

A Unity-based Guard AI system that combines **Behavior Trees** for decision-making with a **Finite State Machine (FSM)** for state execution.

## Architecture Overview

The AI system uses a hybrid approach:
- **Behavior Tree**: Determines WHAT the guard should do (decision-making)
- **FSM**: Handles HOW the guard performs actions (execution)

## Behavior Tree Structure

```
Root (Selector)
├── Chase Branch (Sequence) ─────────── Highest Priority
│   ├── Condition: Can see player?
│   └── Action: Transition to Chase
│
├── Return Branch (Sequence)
│   ├── Condition: Was chasing?
│   ├── Condition: Lost sight of player?
│   └── Action: Transition to Return
│
└── Patrol Branch (Sequence) ─────────── Lowest Priority
    └── Action: Continue patrolling
```

## Node Types

### Composite Nodes
| Node | Behavior |
|------|----------|
| **Selector** | OR logic - Returns Success if ANY child succeeds. Evaluates left-to-right, stops on first Success. |
| **Sequence** | AND logic - Returns Success only if ALL children succeed. Stops on first Failure. |
| **Parallel** | Runs all children simultaneously. Succeeds if required number of children succeed. |

### Decorator Nodes
| Node | Behavior |
|------|----------|
| **Inverter** | Inverts child result (Success ↔ Failure) |
| **Succeeder** | Always returns Success regardless of child result |
| **Failer** | Always returns Failure regardless of child result |
| **Repeater** | Repeats child N times |
| **RepeatUntilFail** | Runs child continuously until it fails |

### Leaf Nodes
| Node | Behavior |
|------|----------|
| **ConditionNode** | Evaluates a boolean condition (true = Success, false = Failure) |
| **ActionNode** | Executes an action that returns its own status |
| **SimpleActionNode** | Executes a void action, always returns Success |
| **WaitNode** | Returns Running for specified duration, then Success |

## Guard States (FSM)

| State | Description |
|-------|-------------|
| **Patrol** | Guard moves between waypoints in sequence |
| **Chase** | Guard pursues the player's current position |
| **ReturnToPatrol** | Guard returns to last patrol position after losing sight |

## How It Works

### Every Frame:
1. **Behavior Tree Evaluation**: The tree is ticked from root to leaves
2. **Decision Making**: Based on conditions (can see player, was chasing, etc.), a state is decided
3. **State Transition**: If the decision differs from current state, transition occurs
4. **State Execution**: The FSM executes the appropriate behavior for the current state

### Detection System:
The guard uses a vision cone to detect the player:
- **View Distance**: Maximum range the guard can see
- **View Angle**: Field of view angle (cone width)
- **Line of Sight**: Raycast check to ensure no obstacles block the view

```
        Player
           *
          /
         /  <- Within angle
        /
    [Guard]────────> Forward
        \
         \
          \
           *
        Not visible (outside angle)
```

## File Structure

```
Assets/Scripts/
├── GuardAI.cs                 # Main guard controller (FSM + BT integration)
├── BehaviorTree.cs            # Guard-specific behavior tree builder
└── BehaviorTree/
    ├── BTNode.cs              # Base node class + NodeStatus enum
    ├── CompositeNodes.cs      # Selector, Sequence, Parallel
    ├── DecoratorNodes.cs      # Inverter, Succeeder, Failer, Repeater
    ├── LeafNodes.cs           # Condition, Action, Wait nodes
    ├── BehaviorTreeBuilder.cs # Fluent API for tree construction
    └── BehaviorTreeRunner.cs  # Tree execution manager
```

## Configuration (Inspector)

| Parameter | Description |
|-----------|-------------|
| View Distance | How far the guard can see |
| View Angle | Field of view in degrees |
| Catch Distance | Distance at which player is caught |
| Waypoint Reach Distance | How close guard needs to be to reach a waypoint |
| Patrol Points | Array of transform waypoints |
| Enable Debug | Toggle debug logging |

## Usage Example

### Building a Custom Behavior Tree:
```csharp
var tree = new BehaviorTreeBuilder()
    .Selector("Root")
        .Sequence("HighPriority")
            .Condition(() => SomeCondition())
            .Action(() => DoSomething())
        .End()
        .Sequence("LowPriority")
            .Action(() => DefaultBehavior())
        .End()
    .End()
    .Build();

var runner = new BehaviorTreeRunner(tree);
runner.Tick(); // Call every frame
```

## Node Status

Each node returns one of three statuses:
- **Success**: Node completed its task successfully
- **Failure**: Node failed to complete its task
- **Running**: Node is still executing (used for multi-frame operations)

## Priority System

The Selector node implements priority-based behavior:
1. Children are evaluated left-to-right
2. First child to return Success "wins"
3. Remaining children are not evaluated

This allows high-priority behaviors (like Chase) to override low-priority ones (like Patrol).

## AI Priority Table

### GuardAI (Hybrid) Priorities

| Priority | Behavior | Condition | Action |
|:--------:|----------|-----------|--------|
| 1 (Highest) | **Chase** | Can see player | Pursue player |
| 2 | **Return** | Was chasing AND lost player | Go to last patrol point |
| 3 (Lowest) | **Patrol** | Default | Move between waypoints |

### PureBTGuardAI (Pure BT) Priorities

| Priority | Behavior | Condition | Action |
|:--------:|----------|-----------|--------|
| 1 (Highest) | **Catch** | Player in catch range | Game Over |
| 2 | **Chase** | Can see player | Move to player + Alert |
| 3 | **Search** | Has last known position AND searching | Investigate last position |
| 4 (Lowest) | **Patrol** | Default | Move between waypoints |

## UML Diagrams

### Behavior Tree Class Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           <<enum>>                                   │
│                          NodeStatus                                  │
├─────────────────────────────────────────────────────────────────────┤
│  Success                                                             │
│  Failure                                                             │
│  Running                                                             │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      <<abstract>> BTNode                             │
├─────────────────────────────────────────────────────────────────────┤
│  + Name : string                                                     │
├─────────────────────────────────────────────────────────────────────┤
│  + Evaluate() : NodeStatus <<abstract>>                              │
│  + Reset() : void                                                    │
└─────────────────────────────────────────────────────────────────────┘
                                    △
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
          ▼                         ▼                         ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│  CompositeNode   │    │  DecoratorNode   │    │    LeafNodes     │
│    <<abstract>>  │    │   <<abstract>>   │    │                  │
├──────────────────┤    ├──────────────────┤    ├──────────────────┤
│ - children: List │    │ - child: BTNode  │    │ ConditionNode    │
├──────────────────┤    ├──────────────────┤    │ ActionNode       │
│ + AddChild()     │    │ + Reset()        │    │ SimpleActionNode │
│ + AddChildren()  │    └──────────────────┘    │ WaitNode         │
└──────────────────┘              △             └──────────────────┘
          △                       │
          │            ┌──────────┴──────────┐
    ┌─────┴─────┐      │          │          │
    │     │     │      ▼          ▼          ▼
    ▼     ▼     ▼   Inverter  Succeeder  Repeater
Selector       Parallel
      Sequence
```

### GuardAI Behavior Tree Flow

```
                              ┌─────────────┐
                              │   Update()  │
                              └──────┬──────┘
                                     │
                                     ▼
                        ┌────────────────────────┐
                        │  BehaviorTree.Tick()   │
                        └────────────┬───────────┘
                                     │
                                     ▼
                    ┌────────────────────────────────┐
                    │      ROOT (Selector)           │
                    │   Evaluates children L→R       │
                    └────────────────┬───────────────┘
                                     │
           ┌─────────────────────────┼─────────────────────────┐
           │                         │                         │
           ▼                         ▼                         ▼
   ┌───────────────┐        ┌───────────────┐        ┌───────────────┐
   │ CHASE Branch  │        │ RETURN Branch │        │ PATROL Branch │
   │  (Sequence)   │        │  (Sequence)   │        │  (Sequence)   │
   │  Priority: 1  │        │  Priority: 2  │        │  Priority: 3  │
   └───────┬───────┘        └───────┬───────┘        └───────┬───────┘
           │                        │                        │
           ▼                        ▼                        ▼
   ┌───────────────┐        ┌───────────────┐        ┌───────────────┐
   │  CanSeePlayer │        │  WasChasing?  │        │  SetPatrol()  │
   │      ?        │        │      ?        │        │               │
   └───────┬───────┘        └───────┬───────┘        └───────────────┘
           │                        │
      Yes  │                   Yes  │
           ▼                        ▼
   ┌───────────────┐        ┌───────────────┐
   │  SetChase()   │        │  LostPlayer?  │
   │   SUCCESS     │        │      ?        │
   └───────────────┘        └───────┬───────┘
                                    │
                               Yes  │
                                    ▼
                            ┌───────────────┐
                            │  SetReturn()  │
                            │   SUCCESS     │
                            └───────────────┘
```

### PureBTGuardAI Behavior Tree Flow

```
                              ┌─────────────┐
                              │   Update()  │
                              └──────┬──────┘
                                     │
                                     ▼
                    ┌────────────────────────────────┐
                    │      ROOT (Selector)           │
                    └────────────────┬───────────────┘
                                     │
     ┌───────────────┬───────────────┼───────────────┬───────────────┐
     │               │               │               │               │
     ▼               ▼               ▼               ▼               │
┌─────────┐   ┌───────────┐   ┌───────────┐   ┌───────────┐         │
│  CATCH  │   │   CHASE   │   │  SEARCH   │   │  PATROL   │         │
│  Seq    │   │   Seq     │   │   Seq     │   │   Seq     │         │
│ Pri: 1  │   │  Pri: 2   │   │  Pri: 3   │   │  Pri: 4   │         │
└────┬────┘   └─────┬─────┘   └─────┬─────┘   └─────┬─────┘         │
     │              │               │               │               │
     ▼              ▼               ▼               ▼               │
┌─────────┐   ┌───────────┐   ┌───────────┐   ┌───────────┐         │
│InRange? │   │CanSee?    │   │HasLastPos?│   │ResetAlert │         │
└────┬────┘   └─────┬─────┘   └─────┬─────┘   └─────┬─────┘         │
     │Yes           │Yes            │Yes            │               │
     ▼              ▼               ▼               ▼               │
┌─────────┐   ┌───────────┐   ┌───────────┐   ┌───────────┐         │
│GameOver │   │UpdatePos  │   │IsSearching│   │ClearLastP │         │
│         │   │ResetTimer │   └─────┬─────┘   └─────┬─────┘         │
└─────────┘   └─────┬─────┘         │Yes            │               │
                    │               ▼               ▼               │
                    ▼         ┌───────────┐   ┌───────────┐         │
              ┌───────────┐   │ PARALLEL  │   │  Patrol() │         │
              │ PARALLEL  │   │  Search   │   │  Running  │         │
              │  Chase    │   │  Actions  │   └───────────┘         │
              └─────┬─────┘   └───────────┘                         │
                    │                                               │
           ┌───────┴───────┐                                        │
           ▼               ▼                                        │
    ┌────────────┐  ┌────────────┐                                  │
    │MoveToPlayer│  │AlertSound  │                                  │
    └────────────┘  └────────────┘                                  │
```

### State Transition Diagram (Hybrid GuardAI)

```
                    ┌──────────────────────────────────────┐
                    │                                      │
                    │         ┌─────────────┐              │
                    │         │             │              │
                    │         │   PATROL    │◄─────────────┘
                    │         │             │    Reached patrol point
                    │         └──────┬──────┘
                    │                │
                    │                │ Can see player
                    │                ▼
                    │         ┌─────────────┐
     Lost player    │         │             │
     ────────────────┤         │    CHASE   │
                    │         │             │
                    │         └──────┬──────┘
                    │                │
                    │                │ Lost sight of player
                    │                ▼
                    │         ┌─────────────┐
                    │         │             │
                    └─────────│   RETURN    │
                              │             │
                              └─────────────┘
```
