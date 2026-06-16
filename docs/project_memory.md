# MazeLifeLab Project Memory

This file is a compact persistent memory for Cursor, reviewers, and future development sessions.

Do not turn this file into a diary.

The purpose is to preserve research context between coding sessions.

---

# Current Active Experiment

Experiment ID:

```text
EXP-004
```

Name:

```text
Multi-Agent Exploration Without Communication
```

Status:

```text
IN PROGRESS — Experiment004Runner with 2+ independent agents, team CSV metrics
```

Primary document:

```text
docs/experiment_004_multi_agent.md
```

Previous experiments (shared infrastructure):

```text
EXP-001 — Single-Agent Navigation Benchmark
EXP-003 — Local RRT Under Partial Observability
```

---

# Current Goal

Run multi-agent baseline without communication:

- N agents share one maze and one goal;
- each agent has independent sensing and planning;
- team metrics: coverage, overlap, collisions, path length;
- compare against single-agent EXP-001 on same seeds.

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

Verify EXP-004 in Play mode, then:

```text
Batch comparison: EXP-001 vs EXP-004 on same seeds (2, 4 agents)
```

Or:

```text
EXP-001 Days 10–11 — batch runner across seeds and algorithms
```

Acceptance criteria for EXP-004:

- Two or more agents move independently with no shared state;
- CSV row per episode with team coverage and overlap;
- visual inspection shows exploration overlap.

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
