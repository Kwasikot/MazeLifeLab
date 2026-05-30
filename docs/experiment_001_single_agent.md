# Experiment 001 — Single-Agent Navigation Benchmark

## 1. Experiment Title

**Single-Agent Navigation Benchmark in Procedural Maze Environments**

This document defines the first reproducible experiment for MazeLifeLab. Its purpose is to turn the project from a Unity prototype into a small scientific testbed with clear hypotheses, baselines, metrics, and acceptance criteria.

---

## 2. Research Context

MazeLifeLab aims to study navigation, learning, communication, and collective intelligence in procedurally generated maze worlds.

Before multi-agent communication or artificial life experiments can be meaningful, the project needs a stable single-agent benchmark.

This experiment establishes that foundation.

---

## 3. Central Research Question

**How well can simple single-agent navigation strategies solve procedurally generated mazes under controlled and reproducible conditions?**

A more practical version:

> Can we create a deterministic benchmark where different navigation methods can be compared fairly using the same maze seeds, start positions, goal positions, and metrics?

---

## 4. Hypothesis

**Hypothesis 1:** Classical heuristic baselines such as wall-following should outperform random walk in many maze configurations.

**Hypothesis 2:** A benchmark with deterministic maze seeds will make it possible to compare future RRT, A*, RL, and multi-agent methods in a scientifically meaningful way.

**Hypothesis 3:** Local perception alone will be sufficient for simple baselines, but may produce inefficient paths compared to methods with global map access.

---

## 5. Purpose of the Experiment

The main goal is not to create the smartest agent yet.

The main goal is to create a **reproducible experimental loop**:

1. Generate maze from seed.
2. Place agent and goal.
3. Run selected baseline algorithm.
4. End episode on success or timeout.
5. Record metrics.
6. Repeat across multiple seeds.
7. Compare results.

This experiment should become the foundation for all later experiments.

---

## 6. Scope

### In Scope

- Deterministic maze generation using a seed.
- Single-agent navigation.
- Start and goal placement.
- Episode reset.
- Random-walk baseline.
- Wall-following baseline.
- Basic collision tracking.
- Basic coverage tracking.
- Metrics logging to CSV.
- Reproducible experiment runner.

### Out of Scope

- ML-Agents training.
- Multi-agent experiments.
- Agent communication.
- Pheromone fields.
- Emergent language.
- Hierarchical agents.
- Advanced statistical analysis.
- Perfect code architecture.

These should be added later only after this benchmark works.

---

## 7. Experimental Environment

### Maze

The maze is a procedurally generated grid-like environment.

Recommended initial settings:

```text
maze_width_cells = 20
maze_height_cells = 20
cell_size = 5
```

Maze generation must support deterministic seeds.

Example:

```text
maze_seed = 42
```

Running the same seed should produce the same maze.

### Start Position

The agent starts in a defined cell, for example:

```text
start_cell = (0, 0)
```

or another valid open cell.

### Goal Position

The goal is placed in a different cell, for example:

```text
goal_cell = (maze_width_cells - 1, maze_height_cells - 1)
```

The start and goal should be reachable by construction.

---

## 8. Agent Model

The first version may use a simple embodied agent represented by a Unity GameObject.

The agent should support:

- Forward movement.
- Rotation.
- Collision detection.
- Ray-based local sensing.
- Reset to start position.

The agent does not need to use ML-Agents at this stage.

---

## 9. Baseline Algorithms

### 9.1 Random Walk Agent

The random walk agent selects actions randomly.

Allowed actions:

```text
move_forward
turn_left
turn_right
wait
```

Optional improvement:

- If a wall is directly ahead, increase probability of turning.

Purpose:

- Establish the weakest baseline.
- Confirm that metrics and episode logic work.

Expected behaviour:

- Low success rate in larger mazes.
- Many inefficient movements.
- Many repeated visits.

---

### 9.2 Wall-Following Agent

The wall-following agent follows either the left-hand or right-hand wall.

Suggested variant:

```text
right_hand_rule = true
```

Basic rule:

1. If right side is open, turn right and move.
2. Else if forward is open, move forward.
3. Else if left side is open, turn left.
4. Else turn around.

Purpose:

- Establish a simple classical maze-solving heuristic.

Expected behaviour:

- Better than random walk in many perfect mazes.
- May be inefficient.
- May fail or loop in some maze types depending on maze topology.

---

### 9.3 Optional: Direct Oracle / A* Baseline

This is optional for Experiment 001.

If implemented, A* may use full maze graph access.

Purpose:

- Establish an approximate upper bound for path efficiency under full observability.

Important note:

- A* is not directly comparable to local perception agents because it has more information.
- It should be labelled as a full-observability baseline.

---

## 10. Independent Variables

The independent variables are the factors deliberately changed by the experiment.

Minimum set:

```text
algorithm
maze_seed
```

Recommended later additions:

```text
maze_width_cells
maze_height_cells
start_cell
goal_cell
max_steps
sensor_range
```

---

## 11. Dependent Variables / Metrics

The dependent variables are the measured outcomes.

Minimum required metrics:

```text
episode_id
maze_seed
algorithm
success
steps_to_goal
collisions
path_length
coverage_percent
```

Recommended additional metrics:

```text
time_to_goal_seconds
unique_cells_visited
repeated_cell_visits
dead_end_visits
final_distance_to_goal
termination_reason
```

---

## 12. Episode Termination Conditions

An episode ends when one of the following occurs:

### Success

The agent reaches the goal.

Example condition:

```text
distance(agent_position, goal_position) < goal_radius
```

### Timeout

The agent exceeds maximum allowed steps.

Recommended initial value:

```text
max_steps = 5000
```

### Invalid State

Optional:

- Agent leaves maze bounds.
- Agent gets stuck for too long.
- Agent object becomes invalid.

---

## 13. Metrics Definitions

### success

Boolean value.

```text
true  = agent reached goal
false = agent did not reach goal before timeout
```

### steps_to_goal

Number of simulation steps before success.

If the agent fails, record either:

```text
steps_to_goal = max_steps
```

or leave empty and use `termination_reason`.

### collisions

Number of detected collisions with walls or obstacles.

### path_length

Approximate total distance travelled by the agent.

Can be computed by accumulating frame-to-frame displacement.

### coverage_percent

Percentage of maze cells visited at least once.

```text
coverage_percent = unique_cells_visited / total_cells * 100
```

### repeated_cell_visits

Number of visits to cells that have already been visited before.

Useful for measuring inefficient exploration.

---

## 14. Experimental Protocol

### Step 1 — Select Algorithms

Initial algorithms:

```text
RandomWalk
WallFollowerRight
WallFollowerLeft
```

### Step 2 — Select Maze Seeds

Recommended initial seeds:

```text
1, 2, 3, 4, 5, 10, 20, 42, 100, 123
```

### Step 3 — Run Episodes

For each algorithm and each seed:

1. Generate maze using seed.
2. Reset agent to start cell.
3. Place goal.
4. Run until success or timeout.
5. Record metrics.
6. Write row to CSV.

### Step 4 — Save Results

Recommended output path:

```text
results/experiment_001_single_agent.csv
```

### Step 5 — Inspect Behaviour Visually

The Unity scene should allow visual inspection of:

- Maze.
- Agent trajectory.
- Goal.
- Collisions.
- Visited cells.

Do not trust metrics alone. Visual inspection is important in early simulation work.

---

## 15. CSV Schema

Required CSV columns:

```text
episode_id,maze_seed,algorithm,success,steps,collisions,path_length,coverage_percent,termination_reason
```

Recommended extended schema:

```text
episode_id,
maze_seed,
algorithm,
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
unique_cells_visited,
coverage_percent,
repeated_cell_visits,
final_distance_to_goal,
termination_reason
```

---

## 16. Acceptance Criteria

Experiment 001 is complete when:

- Maze generation is deterministic by seed.
- A start and goal are defined.
- At least two baseline agents exist:
  - Random walk.
  - Wall-following.
- Episodes reset cleanly.
- Episodes terminate on success or timeout.
- Metrics are written to CSV.
- Running the same algorithm on the same seed gives reproducible results.
- At least 10 maze seeds can be tested automatically.
- The Unity scene still allows visual inspection.
- The implementation does not require ML-Agents.

---

## 17. Cursor Implementation Instructions

When implementing this experiment, Cursor should follow these rules:

1. Do not rewrite the whole project.
2. Make the smallest changes necessary.
3. Prefer new files under `Assets/Scripts/Experiments`, `Assets/Scripts/Agents`, and `Assets/Scripts/Maze`.
4. Keep old prototype scripts unless they must be modified.
5. Add deterministic seed support before adding advanced agents.
6. Add metrics logging before adding learning.
7. Avoid ML-Agents in this experiment.
8. Do not add multi-agent logic yet.
9. Do not add communication yet.
10. Keep the experiment runnable from Unity Editor.

Recommended new files:

```text
Assets/Scripts/Experiments/Experiment001Runner.cs
Assets/Scripts/Experiments/MetricsLogger.cs
Assets/Scripts/Agents/RandomWalkAgent.cs
Assets/Scripts/Agents/WallFollowerAgent.cs
Assets/Scripts/Maze/MazeSeedConfig.cs
```

Optional helper files:

```text
Assets/Scripts/Maze/MazeCellIndex.cs
Assets/Scripts/Maze/MazeGraph.cs
Assets/Scripts/Agents/AgentSensors.cs
```

---

## 18. Suggested Implementation Order

### Task 1 — Deterministic Maze Seed

Add seed support to maze generation.

Acceptance:

- Same seed produces same maze.
- Different seeds produce different mazes.

### Task 2 — Start and Goal

Add start and goal cells.

Acceptance:

- Agent starts at start cell.
- Goal is visible in scene.
- Episode detects when goal is reached.

### Task 3 — Metrics Logger

Implement CSV logger.

Acceptance:

- CSV file is created.
- Each episode writes one row.
- Required columns are present.

### Task 4 — Random Walk Agent

Implement random walk baseline.

Acceptance:

- Agent moves without manual input.
- Agent can run until success or timeout.

### Task 5 — Wall-Following Agent

Implement wall-following baseline.

Acceptance:

- Agent follows left or right wall.
- Agent can run until success or timeout.

### Task 6 — Batch Runner

Run algorithms across multiple seeds.

Acceptance:

- At least 10 seeds can be run automatically.
- Results are appended to CSV.

---

## 19. Expected Results

Expected qualitative outcomes:

- Random walk should be inefficient.
- Wall-following should often outperform random walk.
- Some mazes may expose weaknesses of wall-following.
- Coverage and repeated visits should reveal exploration inefficiency.

Do not worry if results are imperfect. The main achievement is reproducibility.

---

## 20. Failure Modes to Watch

Possible problems:

- Maze seed does not actually reproduce the same maze.
- Agent starts inside a wall.
- Goal is unreachable.
- Collision counting is unreliable.
- Coverage tracking does not match visual behaviour.
- Wall-following loops forever.
- CSV logging works in Editor but fails in build.
- Physics step timing changes results.

Each failure mode should be documented if discovered.

---

## 21. Scientific Interpretation

This experiment is not meant to prove a major AI result.

Its scientific role is to create a baseline layer.

Once Experiment 001 works, the project can move to stronger questions:

- Does RRT outperform simple heuristics?
- Does RL generalise across unseen maze seeds?
- Does multi-agent exploration outperform single-agent exploration?
- Does communication improve collective search?
- Can agents create external memory through signals?

---

## 22. Definition of Done

Experiment 001 is done when a developer can run the benchmark and obtain a CSV table like this:

```text
episode_id,maze_seed,algorithm,success,steps,collisions,path_length,coverage_percent,termination_reason
1,1,RandomWalk,false,5000,73,812.5,41.2,timeout
2,1,WallFollowerRight,true,642,12,128.4,57.8,success
3,2,RandomWalk,false,5000,91,790.1,38.0,timeout
4,2,WallFollowerRight,true,811,17,162.9,61.5,success
```

The exact numbers do not matter. The structure and reproducibility matter.

---

## 23. Next Experiment After Completion

After this experiment is complete, the recommended next document is:

```text
docs/experiment_002_rrt_vs_baselines.md
```

That experiment should compare:

- Random walk.
- Wall-following.
- RRT.
- A* if available.

Only after that should ML-Agents be introduced.
