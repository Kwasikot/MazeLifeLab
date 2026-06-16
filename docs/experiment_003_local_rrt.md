# Experiment 003 — Local RRT Under Partial Observability

## 1. Experiment Title

**Incremental RRT Navigation With Local Sensing in Procedural Mazes**

EXP-003 begins after EXP-001 baselines exist. It does not require EXP-002 (global RRT) to be complete, but reuses the same maze, episode loop, and metrics infrastructure.

---

## 2. Research Question

**Can an agent build a useful path by incrementally discovering maze structure and running RRT only over known free space?**

Practical version:

> Does Local RRT (`observability_mode = incremental_map`) navigate seeded mazes more efficiently than RandomWalk while using less global information than a full-map planner?

---

## 3. Observability Mode

```text
incremental_map
```

The agent:

- does **not** read the full `MazeGenerator` wall map for planning;
- discovers passages through a local sensor model;
- plans only through **known-open** passages;
- treats unknown passages as blocked (conservative planning).

Goal world position may be known (GPS-style), but maze topology is not.

---

## 4. Algorithm — RRT_Local

Each episode step:

1. **Sense** — update `MazeLocalDiscoveryMap` from current position (BFS within sensor radius through line-of-sight corridors).
2. **Plan** — run grid RRT over discovered cells toward goal with goal bias.
3. **Act** — follow one cell along the plan, or frontier-explore through a known-open passage into undiscovered space if no plan exists.

Configurable parameters:

```text
sensor_radius_cells
rrt_iterations_per_step
rrt_goal_bias
replan_interval_steps
```

---

## 5. Metrics

Reuse EXP-001 episode metrics plus:

```text
observability_mode = incremental_map
coverage_percent
rrt_nodes_created
rrt_iterations
rrt_path_found
```

---

## 6. Comparison Baselines

Compare against (same seeds, same start/goal):

- `RandomWalk_Local`
- `WallFollowerRight_Local`
- `RRT_Local` (this experiment)

Future: `RRT_Global` from EXP-002 as upper-bound reference.

---

## 7. Acceptance Criteria

EXP-003 milestone is reached when:

- [x] `LocalRrtAgent` runs inside the existing episode loop.
- [x] Agent plans only on discovered map data (no full-map cheat during planning).
- [ ] Coverage increases as the agent explores (verify in Play mode).
- [ ] Local RRT reaches the goal on seed 42 in a reasonable step budget (verify in Play mode).
- [x] Episode logs include coverage and RRT stats.

---

## 8. Implementation Files

```text
Assets/Scripts/Planning/MazeLocalDiscoveryMap.cs
Assets/Scripts/Planning/LocalRrtPlanner.cs
Assets/Scripts/Agents/LocalRrtAgent.cs
```

Integrated via `Experiment001Algorithm.LocalRrt` on `Experiment001Runner`.

---

## 9. Relationship To Future Work

EXP-003 prepares:

- EXP-004 — multi-agent exploration without communication;
- EXP-006 — Swarm-RRT with distributed partial maps.

Do not add multi-agent behaviour in this experiment.
