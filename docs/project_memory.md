# MazeLifeLab Project Memory

This file is a compact persistent memory for Cursor, reviewers, and future development sessions.

Do not turn this file into a diary.

The purpose is to preserve research context between coding sessions.

---

# Current Active Experiment

Experiment ID:

```text
EXP-001
```

Name:

```text
Single-Agent Navigation Benchmark
```

Status:

```text
PLANNING
```

Primary document:

```text
docs/experiment_001_single_agent.md
```

---

# Current Goal

Create a reproducible benchmark environment for comparing simple navigation strategies.

The immediate objective is NOT machine learning.

The immediate objective is:

- deterministic maze generation;
- reproducible episodes;
- baseline agents;
- metrics collection;
- CSV output.

---

# Current Baselines

Implemented:

```text
None
```

Planned:

```text
RandomWalkAgent
WallFollowerAgent
```

Future:

```text
RRT
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
Deterministic maze seed support
```

Acceptance criteria:

- same seed => same maze
- different seed => different maze
- result can be verified visually and programmatically

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
