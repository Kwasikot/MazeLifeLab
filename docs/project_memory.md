# MazeLifeLab Project Memory

This file is a compact persistent memory for Cursor, reviewers, and future development sessions.

Do not turn this file into a diary.

The purpose is to preserve research context between coding sessions.

---

# Current Active Experiment

Experiment ID:

```text
EXP-006
```

Name:

```text
Swarm-RRT / Distributed Search Trees
```

Status:

```text
IN PROGRESS — SwarmRrtField, SwarmRrtPlanner, Experiment006Runner
```

Primary document:

```text
docs/experiment_006_swarm_rrt.md
```

Previous experiments (shared infrastructure):

```text
EXP-005 — Multi-Agent Exploration With Simple Signals
EXP-004 — Multi-Agent Exploration Without Communication
EXP-001 — Single-Agent Navigation Benchmark
EXP-003 — Local RRT Under Partial Observability
```

---

# Current Goal

Test whether agents can collectively approximate a distributed RRT by sharing tree edges through environmental memory:

- `SwarmRrtField` stores deposited RRT branch edges per episode;
- `SwarmRrt` mode grafts foreign tree nodes reachable in each agent's discovered map;
- ablation: `Independent` (EXP-004) vs `DepositOnly` vs `SwarmRrt`;
- team metrics plus `swarm_edges_deposited` and `swarm_graft_nodes`.

---

# Implemented For EXP-001 / Shared Infrastructure

```text
Deterministic maze seed (MazeSeedConfig, MazeGenerator)
Maze fingerprint logging for reproducibility checks
EXP-001 episode loop (start/goal, success/timeout, reset)
ManualAgentController for episode testing (WASD)
RandomWalkAgent baseline with collision and path-length tracking
WallFollowerAgent (right-hand and left-hand rules)
MetricsLogger CSV (single-agent episodes)
Unique-cell coverage tracking in Experiment001Runner
```

# Implemented For EXP-003

```text
MazeLocalDiscoveryMap (incremental_map sensing)
LocalRrtPlanner (grid RRT on discovered topology)
LocalRrtAgent (sense → plan → act loop)
Experiment001Algorithm.LocalRrt
Per-agent RRT visualizer (MazeRrtVisual_A{n})
```

# Implemented For EXP-004

```text
Experiment004Runner (2–32 agents, no communication)
MultiAgentStartLayout (corner/perimeter spawn)
MultiAgentMetricsLogger (team coverage, overlap, CSV)
Experiment004RunnerEditor
```

# Implemented For EXP-005

```text
MazeStigmergyField (decaying per-cell environmental signals)
MazeFrontierClaimField (temporary frontier responsibility claims per agent)
AgentStigmergyController (Trail / FrontierHint / RandomNoise / FrontierClaim deposit modes)
Experiment005Runner (extends multi-agent loop with communication ablation)
Experiment005MetricsLogger (signals_deposited, signal_influenced_steps, frontier claim metrics)
Experiment005RunnerEditor
LocalRrtAgent stigmergy read bias and claim-aware frontier selection (optional, no shared discovery map)
```

# Implemented For EXP-006

```text
SwarmRrtField (shared RRT edge deposits per episode)
SwarmRrtPlanner (graft foreign tree nodes into local RRT)
Experiment006SwarmMode (Independent / DepositOnly / SwarmRrt ablation)
Experiment006Runner (multi-agent loop with swarm overlay)
Experiment006MetricsLogger (swarm_edges_deposited, swarm_graft_nodes)
Experiment006RunnerEditor
LocalRrtAgent.ConfigureSwarmRrt (optional deposit + graft)
```

# Current Baselines

Implemented:

```text
RandomWalkAgent
WallFollowerAgent
LocalRrtAgent
ManualAgentController (testing only)
```

Planned:

```text
RRT_Global (EXP-002)
Batch runner across seeds (EXP-001 Days 10–11)
EXP-005 multi-agent with simple signals
EXP-006 Swarm-RRT / distributed search trees (active)
```

Future:

```text
A*
RL Agent
```

---

# Required Metrics

Minimum metrics:

```text
episode_id
maze_seed
algorithm
success
steps
collisions
path_length
coverage_percent
termination_reason
```

---

# Current Assumptions

1. Reproducibility is more important than complexity.
2. Baselines must exist before ML agents.
3. Metrics must exist before advanced behaviour.
4. Single-agent experiments come before multi-agent experiments.
5. Communication comes after baseline navigation is validated.

---

# Out of Scope For EXP-001

Do not implement yet:

- ML-Agents training
- emergent communication
- pheromone systems
- swarm intelligence
- multi-agent coordination
- hierarchical agents
- language systems

These belong to future experiments.

---

# Long-Term Research Direction

MazeLifeLab should evolve toward:

```text
Navigation
-> Exploration
-> Communication
-> Collective Memory
-> Swarm Intelligence
-> Distributed Planning
-> Artificial Life
```

The strongest long-term research theme is:

```text
Collective exploration under partial observability using simple local communication.
```

---

# Open Questions

1. What is the best deterministic maze generation strategy?
2. How should coverage be measured?
3. How should wall-following be implemented?
4. Should RRT use full map access or local sensing?
5. Which metrics best capture exploration quality?

---

# Next Recommended Task

Verify EXP-005 in Play mode (`communication_mode=FrontierClaim`), then compare CSV against EXP-004 and EXP-005 `Trail` / `FrontierHint` on seed 42.

---

# Update Instructions

Update this file only when:

- experiment status changes;
- major milestone is completed;
- assumptions change;
- metrics schema changes;
- active experiment changes;
- research direction changes.

Do not update it for every small code edit.
