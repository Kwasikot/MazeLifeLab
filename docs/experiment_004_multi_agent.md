# EXP-004 — Multi-Agent Exploration Without Communication

## 1. Purpose

Establish a reproducible multi-agent baseline before communication experiments (EXP-005).

Multiple autonomous agents explore the **same maze** with **no shared maps, signals, or coordination**. Each agent uses only local sensing and its own internal state (same algorithms as EXP-001 / EXP-003).

## 2. Research Question

> Does simply adding more agents improve exploration, or does lack of coordination create redundant work?

Compare against single-agent EXP-001 runs on the same seeds and algorithms.

## 3. Scope

### In scope

- 2–32 agents in one maze (start with 2 in scene defaults).
- Shared goal; episode succeeds when **any** agent reaches the goal.
- Algorithms: `RandomWalk`, `WallFollowerRight`, `WallFollowerLeft`, `LocalRrt`.
- Team metrics: collective coverage, overlap, collisions, path length.
- CSV logging to `results/experiment_004_multi_agent.csv`.

### Out of scope

- Inter-agent communication (EXP-005).
- Shared discovery maps or pheromones.
- ML-Agents / learned policies.
- Physical agent–agent collision blocking (agents may overlap visually).

## 4. Episode Protocol

1. Regenerate maze from seed.
2. Spawn `N` agents at distinct start cells (corners first, then perimeter).
3. Run fixed simulation steps; all agents act each step.
4. Terminate on:
   - **Success** — any agent within `goalRadius` of goal (record `steps_to_first_goal`), simulation continues until all agents reach the goal or `maxSteps`;
   - **Timeout** — `maxSteps` reached with no agent at goal.
5. Log one CSV row per episode.

## 5. Metrics

| Column | Definition |
|--------|------------|
| `episode_id` | Monotonic run id |
| `maze_seed` | Maze seed |
| `algorithm` | Shared algorithm name |
| `agent_count` | Number of agents |
| `success` | Any agent reached goal |
| `steps` | Simulation steps until termination |
| `steps_to_first_goal` | Step when first agent reached goal (−1 if none) |
| `agents_at_goal` | Agents at goal at episode end |
| `total_collisions` | Sum of per-agent collision counts |
| `total_path_length` | Sum of per-agent path lengths |
| `team_coverage_percent` | Union of visited cells / total cells × 100 |
| `overlap_percent` | Cells visited by ≥2 agents / total cells × 100 |
| `termination_reason` | `Success`, `Timeout`, or `InvalidConfiguration` |

## 6. Implementation

```text
Assets/Scripts/Experiments/Experiment004Runner.cs
Assets/Scripts/Experiments/MultiAgentMetricsLogger.cs
Assets/Scripts/Experiments/MultiAgentStartLayout.cs
Assets/Editor/Experiment004RunnerEditor.cs
```

Scene: `Experiment004Runner` on `MazeSystem` (disable `Experiment001Runner` when running EXP-004).

## 7. Acceptance Criteria

- [ ] Two or more agents spawn and move independently.
- [ ] No code path shares maps or state between agents.
- [ ] CSV row written per episode with team metrics.
- [ ] Visual inspection shows overlapping exploration paths.
- [ ] Team coverage ≥ best single-agent coverage on same seed (not required automatically, but expected trend for LocalRrt).

## 8. Next Step

EXP-005 — Multi-Agent Exploration With Simple Signals.
