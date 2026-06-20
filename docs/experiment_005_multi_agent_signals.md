# EXP-005 — Multi-Agent Exploration With Simple Signals

## 1. Purpose

Extend the EXP-004 no-communication baseline with **environmental stigmergy**: agents deposit and read scalar signals on maze cells without sharing internal maps.

This is the first communication layer in the research ladder — simple enough to ablate, measurable, and comparable to EXP-004 on the same seeds.

## 2. Research Question

> Does simple environmental signalling improve collective exploration compared to EXP-004, or does it add noise without benefit?

Compare against EXP-004 (`communication_mode=None`) on identical maze seeds, agent counts, and algorithms.

## 3. Scope

### In scope

- 2–32 agents, shared goal, same spawn layout as EXP-004.
- **Stigmergy field** on the maze grid (per-cell strength, exponential decay each step).
- Communication modes (ablation ladder):
  - `None` — EXP-004 equivalent (no deposit, no read bias).
  - `RandomNoise` — control: random deposits near the agent.
  - `Trail` — hand-designed: deposit on each visited cell (path marking).
  - `FrontierHint` — hand-designed: deposit only on cells adjacent to undiscovered space.
- Algorithms: same as EXP-004 (`LocalRrt` default).
- Local RRT may **bias movement** toward neighbouring cells with higher **foreign** signal (open passage required) when goal-directed steps fail.
- `Trail` reads other agents' deposits only; `RandomNoise` deposits only (no read); `None` matches EXP-004.
- CSV logging to `results/experiment_005_multi_agent_signals.csv`.

### Out of scope (later EXP-005+ / EXP-006)

- Learned signal semantics.
- Direct agent-to-agent message packets.
- Shared discovery maps.
- Signal cost / budget (add in ablation follow-up).
- ML-Agents policies.

## 4. Episode Protocol

Same as EXP-004, plus:

1. Reset stigmergy field at episode start.
2. Each fixed step: decay field → agent actions → deposits on move.
3. Log `communication_mode`, `signals_deposited`, `signal_influenced_steps`.

Termination unchanged: all agents at goal or `maxSteps`; success when any agent reaches goal (`steps_to_first_goal` recorded).

## 5. Metrics

All EXP-004 team metrics, plus:

| Column | Definition |
|--------|------------|
| `communication_mode` | `None`, `RandomNoise`, `Trail`, `FrontierHint` |
| `signals_deposited` | Total deposit operations this episode |
| `signal_influenced_steps` | Agent steps where stigmergy bias changed movement choice |

## 6. Implementation

```text
Assets/Scripts/Communication/MazeStigmergyField.cs
Assets/Scripts/Communication/AgentStigmergyController.cs
Assets/Scripts/Communication/Experiment005CommunicationMode.cs
Assets/Scripts/Experiments/Experiment005Runner.cs
Assets/Scripts/Experiments/Experiment005MetricsLogger.cs
Assets/Editor/Experiment005RunnerEditor.cs
```

Scene: `Experiment005Runner` on `MazeSystem` (disable `Experiment004Runner` when running EXP-005).

## 7. Acceptance Criteria

- [ ] `None` mode reproduces EXP-004 behaviour (no deposits, no bias).
- [ ] `Trail` / `FrontierHint` deposit visible in metrics (`signals_deposited` > 0).
- [ ] Agents still use independent local maps (no shared `MazeLocalDiscoveryMap`).
- [ ] CSV row per episode with communication columns.
- [ ] Side-by-side comparison possible: EXP-004 vs EXP-005 `None` vs EXP-005 `Trail` on seed 42.

## 8. Next Step

EXP-006 — Swarm-RRT / distributed search trees (see `docs/experiment_006_swarm_rrt.md`).
