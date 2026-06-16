# MazeLifeLab Project Memory

This file is a compact persistent memory for Cursor, reviewers, and future development sessions.

Do not turn this file into a diary.

The purpose is to preserve research context between coding sessions.

---

# Current Active Experiment

Experiment ID:

```text
EXP-003
```

Name:

```text
Local RRT Under Partial Observability
```

Status:

```text
IN PROGRESS — initial LocalRrtAgent integrated with episode loop
```

Primary document:

```text
docs/experiment_003_local_rrt.md
```

Previous experiment (still used as shared infrastructure):

```text
EXP-001 — Single-Agent Navigation Benchmark
docs/experiment_001_single_agent.md
```

---

# Current Goal

Implement incremental-map RRT navigation:

- local sensing builds `MazeLocalDiscoveryMap`;
- `LocalRrtPlanner` plans only through known-open passages;
- agent explores frontiers when no plan exists;
- metrics include coverage and RRT stats.

---

# Implemented For EXP-001 / Shared Infrastructure

```text
Deterministic maze seed (MazeSeedConfig, MazeGenerator)
Maze fingerprint logging for reproducibility checks
EXP-001 episode loop (start/goal, success/timeout, reset)
ManualAgentController for episode testing (WASD)
RandomWalkAgent baseline with collision and path-length tracking
WallFollowerAgent (right-hand and left-hand rules)
```

# Implemented For EXP-003

```text
MazeLocalDiscoveryMap (incremental_map sensing)
LocalRrtPlanner (grid RRT on discovered topology)
LocalRrtAgent (sense → plan → act loop)
Experiment001Algorithm.LocalRrt
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
MetricsLogger CSV (EXP-001 Days 8–9)
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

Implement:

```text
WallFollowerAgent baseline (Days 6–7)
```

Or next:

```text
MetricsLogger (CSV) for EXP-001 episodes (Days 8–9)
```

Acceptance criteria:

- CSV file is created with required columns;
- each episode writes one row;
- steps and termination_reason are recorded.

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
