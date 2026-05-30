# Experiment 002 — RRT vs Baselines

## 1. Experiment Title

**Comparing RRT With Simple Navigation Baselines in Procedural Maze Environments**

This document defines the second major experiment for MazeLifeLab.

EXP-002 should begin only after EXP-001 is complete or at least stable enough to provide:

- deterministic maze seeds;
- start and goal positions;
- episode loop;
- RandomWalk baseline;
- WallFollower baseline;
- metrics logger;
- CSV output.

---

## 2. Research Context

MazeLifeLab began with interest in RRT-style exploration and later expanded toward artificial life, multi-agent systems, and collective intelligence.

Before moving toward Swarm-RRT or emergent communication, we need to understand how a classical sampling-based planner performs against simple navigation heuristics in the same procedural maze environment.

EXP-002 is the bridge between:

```text
simple embodied navigation
```

and

```text
sampling-based planning / distributed search
```

---

## 3. Central Research Question

**How does Rapidly-Exploring Random Tree planning compare with simple baseline agents in procedurally generated maze environments?**

Practical version:

> Does RRT provide a meaningful advantage over RandomWalk and WallFollower agents when solving the same seeded mazes under the same metrics?

---

## 4. Main Hypotheses

### Hypothesis 1 — RRT should outperform random walk

RRT should find paths more efficiently than random movement in most maze configurations.

### Hypothesis 2 — RRT should often outperform wall-following in path efficiency

Wall-following may eventually solve many mazes, but its paths can be long and inefficient. RRT should often produce shorter or more direct paths if it has enough samples and collision checking is reliable.

### Hypothesis 3 — RRT performance depends strongly on observability and implementation assumptions

A full-map or global-sampling RRT is not equivalent to a local embodied agent. Therefore, RRT results must be clearly labelled according to what information the planner has access to.

### Hypothesis 4 — Local RRT may be more scientifically interesting than full-map RRT

A Local RRT variant using partial sensing or incrementally discovered space may be a stronger bridge toward artificial life and swarm intelligence than a classical full-observability planner.

---

## 5. Purpose of the Experiment

The purpose is not merely to implement RRT.

The purpose is to establish a fair comparison between:

- weak embodied baseline;
- simple heuristic baseline;
- sampling-based planner;
- possibly full-observability oracle planner.

This experiment should help answer:

1. Is RRT useful in this maze environment?
2. How much better is it than simple baselines?
3. What assumptions does RRT require?
4. Can RRT become the foundation for a future Swarm-RRT experiment?

---

## 6. Required Prior Work

Before starting EXP-002, confirm that EXP-001 provides:

```text
[ ] deterministic maze seeds
[ ] start and goal cells
[ ] episode success condition
[ ] episode timeout condition
[ ] RandomWalk baseline
[ ] WallFollower baseline
[ ] metrics CSV output
[ ] at least 10 seeds runnable in batch mode
```

Do not start heavy RRT refactoring before EXP-001 is stable.

---

## 7. Algorithms Compared

### 7.1 RandomWalk

Weak baseline.

Purpose:

- Show minimum expected performance.
- Detect whether RRT is actually better than random exploration.

---

### 7.2 WallFollower

Classical heuristic baseline.

Purpose:

- Provide a simple non-learning, local strategy.
- Establish whether RRT improves path efficiency over a known maze heuristic.

---

### 7.3 RRT — Full Observability Variant

Classical RRT planner with access to global sampling space and collision checking.

Possible assumptions:

- knows global bounds of maze;
- can sample anywhere in maze space;
- uses physics raycast or geometry collision checks;
- can plan from start to goal before agent moves;
- produces path that the agent follows.

This should be labelled as:

```text
RRT_Global
```

or

```text
RRT_FullObservability
```

Important warning:

This is not directly comparable to a locally perceiving embodied agent because it has more information and planning power.

---

### 7.4 RRT — Local / Incremental Variant

Optional but scientifically important.

The agent does not know the full maze in advance. It expands a local search tree based on discovered free space.

Possible label:

```text
RRT_Local
```

or

```text
RRT_Incremental
```

Purpose:

- Bridge between classical planning and embodied partial observability.
- Prepare future Swarm-RRT experiments.

---

### 7.5 Optional A* Oracle

A* can be added as a full-map oracle baseline.

Possible label:

```text
AStar_FullMap
```

Purpose:

- Estimate near-optimal path length.
- Provide a stronger classical planning reference.

Warning:

A* with full maze graph access is an oracle-like planner and should be interpreted separately from local agents.

---

## 8. Observability Conditions

The experiment must clearly distinguish observability levels.

### Full Observability

Planner has access to complete maze structure or can sample globally.

Examples:

- A* with full maze graph.
- RRT with global bounds and collision checking.

### Partial Observability

Agent only has local sensors or an incrementally discovered map.

Examples:

- RandomWalk with raycasts.
- WallFollower with local sensors.
- Local RRT based on discovered space.

### Why This Matters

A full-observability planner may perform better not because it is more intelligent, but because it is given more information.

All results must record observability condition.

Recommended CSV field:

```text
observability_mode
```

Possible values:

```text
local
full_map
incremental_map
oracle
```

---

## 9. Experimental Conditions

Minimum conditions:

```text
RandomWalk_Local
WallFollower_Local
RRT_Global
```

Recommended conditions:

```text
RandomWalk_Local
WallFollowerRight_Local
WallFollowerLeft_Local
RRT_Global
RRT_Local
AStar_FullMap
```

Do not implement all at once. Start with the minimum set.

---

## 10. Independent Variables

Minimum:

```text
algorithm
maze_seed
```

Recommended:

```text
algorithm
maze_seed
observability_mode
rrt_sample_count
rrt_step_size
rrt_goal_bias
rrt_max_iterations
```

Optional:

```text
maze_width_cells
maze_height_cells
sensor_range
collision_check_mode
path_smoothing_enabled
```

---

## 11. Dependent Variables / Metrics

Reuse EXP-001 metrics:

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

Add RRT-specific metrics:

```text
observability_mode
rrt_nodes_created
rrt_edges_created
rrt_iterations
rrt_samples_rejected
rrt_planning_time_ms
rrt_path_found
rrt_path_length
rrt_path_waypoint_count
rrt_collision_checks
rrt_collision_check_failures
```

Optional metrics:

```text
path_following_error
path_smoothing_enabled
smoothed_path_length
final_distance_to_goal
```

---

## 12. RRT Planner Requirements

A minimal RRT planner should support:

- start position;
- goal position;
- bounded sampling space;
- nearest-neighbour search;
- step-size limited extension;
- collision checking;
- goal threshold;
- path reconstruction;
- visualisation of tree;
- metrics export.

Recommended configurable parameters:

```text
sample_count
max_iterations
step_size
goal_threshold
goal_bias
collision_layer_mask
random_seed
```

---

## 13. RRT Implementation Notes

The existing project contains an early RRT prototype.

For EXP-002, prefer turning RRT into a reusable planner module instead of keeping it as a scene-only MonoBehaviour.

Recommended direction:

```text
Assets/Scripts/Planning/RrtPlanner.cs
Assets/Scripts/Planning/RrtNode.cs
Assets/Scripts/Planning/RrtPath.cs
Assets/Scripts/Planning/RrtPlannerConfig.cs
```

The planner should be testable separately from the visualisation layer.

Visualisation can be handled by a separate script:

```text
Assets/Scripts/Planning/RrtDebugVisualizer.cs
```

Avoid mixing:

- planning logic;
- Unity scene setup;
- debug drawing;
- agent movement;
- metrics logging.

---

## 14. Path Following

If RRT produces a path, the agent must follow it.

Minimal path-following behaviour:

1. Move toward current waypoint.
2. When waypoint is reached, switch to next waypoint.
3. End episode when goal is reached.
4. Count collisions during path following.

Important distinction:

```text
planning success != episode success
```

The RRT planner may find a path, but the agent may fail to follow it due to movement or physics issues.

Record both:

```text
rrt_path_found
success
```

---

## 15. Collision Checking

Collision checking must be reliable.

Possible methods:

- Physics.Raycast between nodes.
- SphereCast or CapsuleCast for agent radius.
- Grid/graph wall checks.
- Maze cell adjacency checks.

For a car-like or physical agent, raycast-only checks may be too optimistic because they ignore body radius.

Recommended first version:

```text
Physics.Raycast or SphereCast
```

Record collision check mode:

```text
collision_check_mode
```

Possible values:

```text
raycast
spherecast
grid_adjacency
```

---

## 16. Fairness Rules

### Rule 1 — Same Seeds

All algorithms must run on the same maze seeds.

### Rule 2 — Same Start and Goal

All algorithms must use the same start and goal for each seed.

### Rule 3 — Label Information Access

Do not compare full-map RRT to local WallFollower without noting observability difference.

### Rule 4 — Separate Planning Time and Execution Time

For planners, record:

```text
planning_time_ms
execution_steps
```

### Rule 5 — No Hidden Tuning Per Maze

Do not tune RRT parameters separately for each maze unless explicitly running a parameter sweep.

---

## 17. Experimental Protocol

### Step 1 — Confirm EXP-001 works

Do not proceed if baseline episode loop is unstable.

### Step 2 — Implement or refactor RRT planner

Keep it modular.

### Step 3 — Add RRT metrics

Add planning-specific metrics to CSV.

### Step 4 — Run small test

Run one seed with visualisation enabled.

### Step 5 — Run batch test

Run multiple seeds with:

```text
RandomWalk
WallFollowerRight
RRT_Global
```

### Step 6 — Analyse results

Compare:

- success rate;
- planning time;
- execution steps;
- path length;
- collisions;
- failure modes.

---

## 18. Recommended Initial Seeds

Reuse EXP-001 seeds:

```text
1, 2, 3, 4, 5, 10, 20, 42, 100, 123
```

Later expand to:

```text
100 seeds
```

Only expand after the pipeline is stable.

---

## 19. Recommended Initial RRT Parameters

First-pass values:

```text
max_iterations = 5000
step_size = 2.0
goal_threshold = 3.0
goal_bias = 0.05
```

These are starting points, not final scientific claims.

Later run parameter sweeps:

```text
max_iterations: 500, 1000, 5000, 10000
step_size: 1.0, 2.0, 5.0
goal_bias: 0.0, 0.05, 0.1, 0.2
```

---

## 20. CSV Schema

Recommended extended CSV columns:

```text
episode_id,
maze_seed,
algorithm,
observability_mode,
maze_width,
maze_height,
start_cell_x,
start_cell_y,
goal_cell_x,
goal_cell_y,
success,
steps,
time_seconds,
collisions,
path_length,
coverage_percent,
termination_reason,
planning_time_ms,
rrt_path_found,
rrt_nodes_created,
rrt_edges_created,
rrt_iterations,
rrt_samples_rejected,
rrt_collision_checks,
rrt_collision_check_failures,
rrt_path_length,
rrt_path_waypoint_count,
rrt_step_size,
rrt_goal_bias,
rrt_max_iterations,
collision_check_mode
```

If a metric is not applicable to an algorithm, leave it empty or use `NA`.

---

## 21. Acceptance Criteria

EXP-002 is complete when:

- EXP-001 baselines still work.
- RRT can run on the same maze seeds.
- RRT can produce a path or report failure.
- RRT tree can be visualised for debugging.
- RRT path can be followed by an agent or evaluated as a planned path.
- CSV includes RRT-specific metrics.
- RandomWalk, WallFollower, and RRT can be compared on at least 10 seeds.
- Observability mode is recorded.
- Planning time is recorded separately from execution steps.
- Known limitations are documented.
- `CHANGELOG.md` is updated.
- `docs/project_memory.md` is updated when EXP-002 becomes active.

---

## 22. Failure Modes to Watch

Possible issues:

- RRT samples unreachable areas.
- Raycast collision checking misses wall thickness or agent radius.
- RRT finds a geometrical path that the agent cannot physically follow.
- RRT appears better only because it has full-map/global sampling access.
- WallFollower and RRT use unfairly different goal information.
- RRT planning time is ignored.
- Results vary because random seeds are not controlled.
- Tree visualisation works but path reconstruction is broken.
- Metrics mix planning path length with executed path length.

Document these explicitly if discovered.

---

## 23. Scientific Interpretation

If RRT outperforms RandomWalk and WallFollower, this does not automatically mean it is more biologically realistic or more intelligent.

It may simply mean:

- it has better access to global space;
- it uses more computation;
- it performs explicit planning;
- baselines are weak.

The scientific value comes from carefully distinguishing:

```text
performance advantage
```

from

```text
information advantage
```

and

```text
computational advantage
```

---

## 24. Why This Matters For Swarm-RRT

The long-term originality of MazeLifeLab may come from transforming RRT from a central planner into a distributed collective process.

Classical RRT:

```text
one planner builds one search tree
```

Swarm-RRT idea:

```text
many agents collectively approximate a search tree through local exploration and communication
```

EXP-002 is necessary because we first need to understand how classical RRT behaves in the maze environment.

Only after that can we ask:

> Can a population of agents reproduce or improve RRT-like exploration through local signals?

---

## 25. Cursor Implementation Rules For EXP-002

When EXP-002 becomes active, Cursor should follow these rules:

1. Read `docs/project_memory.md` first.
2. Confirm EXP-002 is active before changing RRT code heavily.
3. Do not remove EXP-001 baselines.
4. Preserve CSV compatibility where possible.
5. Add RRT-specific metrics without breaking existing metrics.
6. Keep planner logic separate from visualisation.
7. Clearly label observability mode.
8. Do not add multi-agent communication yet.
9. Do not add ML-Agents yet unless explicitly requested.
10. Update `CHANGELOG.md` after meaningful changes.
11. Update `docs/project_memory.md` when EXP-002 becomes active or changes status.

---

## 26. Recommended First Cursor Prompt For EXP-002

Use this only after EXP-001 is stable:

```text
Read docs/research_agenda.md, docs/experiment_001_single_agent.md, docs/experiment_002_rrt_vs_baselines.md, docs/project_memory.md, and CHANGELOG.md.

Do not implement multi-agent behaviour, communication, pheromones, ML-Agents, or Swarm-RRT yet.

Prepare a small implementation plan for EXP-002 that:
- preserves EXP-001 baselines;
- adds or refactors RRT as a modular planner;
- records observability mode;
- records RRT-specific metrics;
- compares RandomWalk, WallFollower, and RRT_Global on the same seeds.

Do not modify code until the plan is reviewed.
```

---

## 27. Relationship To Future Experiments

EXP-002 leads naturally to:

```text
EXP-003 — Local RRT Under Partial Observability
EXP-004 — Multi-Agent Exploration Without Communication
EXP-005 — Multi-Agent Exploration With Simple Signals
EXP-006 — Swarm-RRT / Distributed Search Trees
```

Do not jump directly to EXP-006.

The scientific discipline is to build the ladder step by step.
