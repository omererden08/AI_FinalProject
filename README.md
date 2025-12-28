# Guard AI - Behavior Tree Implementation

A Unity-based Guard AI system that combines **Behavior Trees** for decision-making with a **Finite State Machine (FSM)** for state execution. Includes both a **Hybrid (BT+FSM)** and **Pure Behavior Tree** implementation.

## Architecture Overview

The AI system provides two approaches:
- **GuardAI (Hybrid)**: Behavior Tree determines WHAT to do, FSM handles HOW to execute
- **PureBTGuardAI (Pure BT)**: Everything handled within the Behavior Tree using action nodes

## Features

- **Vision Cone Detection**: Distance + Angle + Line-of-sight raycast
- **Chase Persistence**: Guards continue chasing briefly after losing sight
- **Search Behavior**: Guards investigate last known position and look around
- **Speed Modulation**: Guards move faster during chase
- **In-Game Visuals**: Runtime vision cone mesh rendering
- **Editor Gizmos**: Debug visualization in Scene view

## Behavior Tree Structure

### GuardAI (Hybrid) Tree

```
Root (Selector)
├── Chase Branch (Sequence) ─────────── Priority 1
│   ├── Condition: Should chase? (can see OR chase timer > 0)
│   └── Action: Set Chase State
│
├── Search Branch (Sequence) ─────────── Priority 2
│   ├── Condition: Should search? (was chasing, lost sight, has last position)
│   └── Action: Start Search State
│
├── Search Complete Branch (Sequence) ── Priority 3
│   ├── Condition: Is searching?
│   ├── Condition: Search complete?
│   └── Action: Set Return State
│
├── Continue Search Branch (Sequence) ── Priority 4
│   ├── Condition: Is searching?
│   └── Action: Keep Searching
│
└── Patrol Branch (Sequence) ─────────── Priority 5 (Lowest)
    └── Action: Set Patrol State
```

### PureBTGuardAI Tree

```
Root (Selector)
├── Catch Branch (Sequence) ─────────── Priority 1 (Highest)
│   ├── Condition: Player in catch range?
│   └── Action: Catch Player (Game Over)
│
├── Chase Branch (Sequence) ─────────── Priority 2
│   ├── Condition: Should chase?
│   ├── Do: Update last known position
│   ├── Do: Reset search timer
│   └── Parallel:
│       ├── Action: Chase Player
│       └── Action: Play Alert Sound
│
├── Search Branch (Sequence) ─────────── Priority 3
│   ├── Condition: Has last known position?
│   ├── Condition: Is searching?
│   └── Parallel:
│       ├── Action: Move to last position + Look around
│       └── Action: Update search timer
│
└── Patrol Branch (Sequence) ─────────── Priority 4 (Lowest)
    ├── Do: Reset alert sound
    ├── Do: Clear last known position
    └── Action: Patrol
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
| **Chase** | Guard pursues player (faster speed), tracks last known position |
| **Search** | Guard goes to last known position and looks around (rotates left/right) |
| **ReturnToPatrol** | Guard returns to last patrol position after search complete |

## How It Works

### Every Frame (GuardAI - Hybrid):
1. **Behavior Tree Evaluation**: Tree is ticked from root to leaves
2. **Decision Making**: Conditions checked (can see player, chase timer, search timer)
3. **State Transition**: If decision differs from current state, transition occurs
4. **State Execution**: FSM executes appropriate behavior for current state

### Every Frame (PureBTGuardAI - Pure):
1. **Behavior Tree Tick**: Tree handles both decisions AND execution
2. **Action Nodes**: Directly control NavMeshAgent movement
3. **Parallel Nodes**: Allow simultaneous actions (move + alert)

### Detection System:
The guard uses a vision cone to detect the player:
- **View Distance**: Maximum range the guard can see
- **View Angle**: Field of view angle (cone width)
- **Line of Sight**: Raycast check to ensure no obstacles block the view

### Chase Persistence:
When the guard loses sight of the player:
1. **Chase Timer** starts counting down (default 2-3 seconds)
2. Guard continues moving to **last known position**
3. When timer expires, guard enters **Search** state

### Search Behavior:
1. Guard moves to **last known player position**
2. Once arrived, guard **looks around** (rotates ±120°)
3. After search duration expires, guard **returns to patrol**

## File Structure

```
Assets/Scripts/
├── GuardAI.cs                 # Hybrid guard (FSM + BT integration)
├── PureBTGuardAI.cs           # Pure behavior tree guard
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

### Vision Settings
| Parameter | Description | Default |
|-----------|-------------|---------|
| View Distance | How far the guard can see | 10 |
| View Angle | Field of view in degrees | 60 |

### Chase Settings
| Parameter | Description | Default |
|-----------|-------------|---------|
| Catch Distance | Distance to catch player | 1.5 |
| Waypoint Reach Distance | Distance to reach waypoint | 0.5 |
| Chase Persistence Time | Time to chase after losing sight | 2-3s |
| Chase Speed Multiplier | Speed increase during chase | 1.5x |
| Search Duration | How long to search at last position | 3-5s |
| Look Around Speed | Rotation speed when searching | 90°/s |

### In-Game Visuals
| Parameter | Description |
|-----------|-------------|
| Show Vision Cone | Toggle runtime vision mesh |
| Vision Cone Material | Optional custom material |
| Normal Color | Vision cone color (patrolling) |
| Alert Color | Vision cone color (chasing) |
| Search Color | Vision cone color (searching) - PureBT only |

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
| 1 (Highest) | **Chase** | Can see player OR chase timer > 0 | Pursue player (faster speed) |
| 2 | **Search** | Was chasing, lost player, chase timer expired | Go to last known position, look around |
| 3 | **Search Complete** | Is searching AND search timer expired | Transition to return |
| 4 | **Continue Search** | Is searching | Keep searching |
| 5 (Lowest) | **Patrol** | Default | Move between waypoints |

### PureBTGuardAI (Pure BT) Priorities

| Priority | Behavior | Condition | Action |
|:--------:|----------|-----------|--------|
| 1 (Highest) | **Catch** | Player in catch range | Game Over |
| 2 | **Chase** | Can see player OR chase timer > 0 | Move to player (faster) + Alert |
| 3 | **Search** | Has last known position AND search timer > 0 | Go to position + Look around |
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
     ┌──────────────┬────────────────┼────────────────┬──────────────┐
     │              │                │                │              │
     ▼              ▼                ▼                ▼              ▼
┌─────────┐  ┌───────────┐  ┌──────────────┐  ┌───────────┐  ┌─────────┐
│  CHASE  │  │  SEARCH   │  │SEARCH COMPLETE│ │ CONTINUE  │  │ PATROL  │
│  Seq    │  │   Seq     │  │    Seq       │  │  SEARCH   │  │  Seq    │
│ Pri: 1  │  │  Pri: 2   │  │   Pri: 3     │  │  Pri: 4   │  │ Pri: 5  │
└────┬────┘  └─────┬─────┘  └──────┬───────┘  └─────┬─────┘  └────┬────┘
     │             │               │                │             │
     ▼             ▼               ▼                ▼             ▼
┌──────────┐ ┌──────────┐  ┌────────────┐    ┌──────────┐  ┌──────────┐
│ShouldChas│ │ShouldSear│  │IsSearching?│    │IsSearchin│  │SetPatrol │
│    ?     │ │ch?       │  │SearchDone? │    │g?        │  │          │
└────┬─────┘ └────┬─────┘  └─────┬──────┘    └────┬─────┘  └──────────┘
     │Yes         │Yes           │Yes             │Yes
     ▼            ▼              ▼                ▼
┌──────────┐ ┌──────────┐  ┌────────────┐    ┌──────────┐
│SetChase()│ │StartSearc│  │ SetReturn()│    │KeepSearch│
│          │ │h()       │  │            │    │ing()     │
└──────────┘ └──────────┘  └────────────┘    └──────────┘
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
│InRange? │   │ShouldChas │   │HasLastPos?│   │ResetAlert │         │
└────┬────┘   │e?         │   └─────┬─────┘   └─────┬─────┘         │
     │Yes     └─────┬─────┘         │Yes            │               │
     ▼              │Yes            ▼               ▼               │
┌─────────┐         ▼         ┌───────────┐   ┌───────────┐         │
│GameOver │   ┌───────────┐   │IsSearching│   │ClearLastP │         │
│         │   │UpdatePos  │   └─────┬─────┘   └─────┬─────┘         │
└─────────┘   │ResetTimer │         │Yes            │               │
              └─────┬─────┘         ▼               ▼               │
                    │         ┌───────────┐   ┌───────────┐         │
                    ▼         │ PARALLEL  │   │  Patrol() │         │
              ┌───────────┐   │MoveToLast │   │  Running  │         │
              │ PARALLEL  │   │+LookAround│   └───────────┘         │
              │  Chase    │   │UpdateTimer│                         │
              └─────┬─────┘   └───────────┘                         │
                    │                                               │
           ┌───────┴───────┐                                        │
           ▼               ▼                                        │
    ┌────────────┐  ┌────────────┐                                  │
    │ChasePlayer │  │AlertSound  │                                  │
    │(fast speed)│  │            │                                  │
    └────────────┘  └────────────┘                                  │
```

### State Transition Diagram (Hybrid GuardAI)

```
                              ┌─────────────┐
                              │             │
         ┌────────────────────│   PATROL    │◄────────────────────┐
         │                    │             │                     │
         │                    └──────┬──────┘                     │
         │                           │                            │
         │                           │ Can see player             │
         │                           ▼                            │
         │                    ┌─────────────┐                     │
         │  Lost sight        │             │                     │
         │  (timer > 0)       │    CHASE    │──┐                  │
         │  ┌─────────────────│  (faster)   │  │                  │
         │  │                 └──────┬──────┘  │ Can see player   │
         │  │                        │         │ (reset timer)    │
         │  │                        │ Lost sight + timer expired │
         │  │                        ▼         │                  │
         │  │                 ┌─────────────┐  │                  │
         │  │                 │             │◄─┘                  │
         │  │                 │   SEARCH    │                     │
         │  │                 │ (look around)│                    │
         │  │                 └──────┬──────┘                     │
         │  │                        │                            │
         │  │                        │ Search timer expired       │
         │  │                        ▼                            │
         │  │                 ┌─────────────┐                     │
         │  │                 │             │                     │
         │  └────────────────►│   RETURN    │─────────────────────┘
         │                    │             │  Reached patrol point
         │                    └─────────────┘
         │                           ▲
         └───────────────────────────┘
              Can see player during return
```
