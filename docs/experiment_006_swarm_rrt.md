# EXP-006 — Swarm-RRT / Distributed Search Trees

## 1. Purpose

Test whether a population of locally perceiving agents can **collectively approximate a search tree** by depositing RRT branch edges into shared environmental memory and grafting onto foreign branches during replanning.

This is the first distributed-planning layer after EXP-005 stigmergy — agents still keep independent discovery maps; only **tree edge geometry** is shared, not full internal maps.

## 2. Research Question

> Does sharing RRT tree edges across agents improve collective navigation compared to independent Local RRT (EXP-004)?

Compare against EXP-004 / EXP-006 `Independent` on identical maze seeds and agent counts.

## 3. Scope

### In scope

- 2–32 agents, shared goal, same spawn layout as EXP-004.
- **SwarmRrtField** — per-episode shared store of deposited RRT edges (from, to, depositor agent).
- Swarm modes (ablation ladder):
  - `Independent` — EXP-004 equivalent (no deposit, no graft).
  - `DepositOnly` — control: deposit local tree edges, planner ignores them.
  - `SwarmRrt` — deposit + graft foreign tree nodes reachable in the agent's discovered map.
- Algorithm: `LocalRrt` with optional swarm overlay.
- CSV logging to `results/experiment_006_swarm_rrt.csv`.

### Out of scope

- Shared discovery maps or direct agent-to-agent messages.
- Learned signal semantics or neural policies.
- Edge decay within episode (edges persist until episode reset).
- Classical global RRT oracle comparison (EXP-002 scope).

## 4. Episode Protocol

Same as EXP-004, plus:

1. Reset `SwarmRrtField` at episode start.
2. Each replan: optionally graft foreign seeds → run local RRT → optionally deposit new tree edges.
3. Log `swarm_mode`, `swarm_edges_deposited`, `swarm_graft_nodes`.

Termination unchanged: all agents at goal or `maxSteps`.

## 5. Metrics

All EXP-004 team metrics, plus:

| Column | Definition |
|--------|------------|
| `swarm_mode` | `Independent`, `DepositOnly`, `SwarmRrt` |
| `swarm_edges_deposited` | Total edge deposit operations this episode |
| `swarm_graft_nodes` | Sum of foreign seed nodes grafted during replans |

## 6. Implementation

```text
Assets/Scripts/Planning/SwarmRrtField.cs
Assets/Scripts/Planning/SwarmRrtPlanner.cs
Assets/Scripts/Planning/Experiment006SwarmMode.cs
Assets/Scripts/Experiments/Experiment006Runner.cs
Assets/Scripts/Experiments/Experiment006MetricsLogger.cs
Assets/Editor/Experiment006RunnerEditor.cs
LocalRrtAgent.ConfigureSwarmRrt (optional graft + deposit)
```

Scene: `Experiment006Runner` on `MazeSystem` (disable EXP-004/005 runners when running EXP-006).

## 7. Acceptance Criteria

- [ ] `Independent` mode reproduces EXP-004 Local RRT behaviour.
- [ ] `DepositOnly` shows `swarm_edges_deposited` > 0 with no graft benefit.
- [ ] `SwarmRrt` shows `swarm_graft_nodes` > 0 when agents overlap in discovered space.
- [ ] Agents still use independent local maps (no shared `MazeLocalDiscoveryMap`).
- [ ] CSV row per episode with swarm columns.
- [ ] Side-by-side comparison: EXP-004 vs EXP-006 `Independent` vs `SwarmRrt` on seed 42.

## 8. Next Step

Optional follow-ups: edge decay, useful-graft ratio, comparison to classical global RRT tree size, population scaling (4–32 agents).
