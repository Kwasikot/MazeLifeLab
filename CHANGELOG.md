# Changelog

All notable changes to MazeLifeLab will be documented in this file.

This project follows a research-oriented changelog discipline: changes should be connected to experiments, metrics, baselines, architecture, or scientific assumptions.

---

## [Unreleased]

### Added

- Added swarm-flight research agenda:
  - `docs/swarm_flight_research_agenda.md`
  - Experiment sequence: `EXP-SWARM-001` through `EXP-SWARM-007`
  - Algorithm proposal: `Scented Active Boids`
- Added EXP-006 Swarm-RRT / distributed search trees:
  - `Assets/Scripts/Planning/SwarmRrtField.cs`
  - `Assets/Scripts/Planning/SwarmRrtPlanner.cs`
  - `Assets/Scripts/Planning/Experiment006SwarmMode.cs`
  - `Assets/Scripts/Experiments/Experiment006Runner.cs`
  - `Assets/Scripts/Experiments/Experiment006MetricsLogger.cs`
  - `Assets/Editor/Experiment006RunnerEditor.cs`
  - `docs/experiment_006_swarm_rrt.md`
  - CSV output: `results/experiment_006_swarm_rrt.csv`
  - Swarm modes: `Independent`, `DepositOnly`, `SwarmRrt`
  - `LocalRrtAgent.ConfigureSwarmRrt` (optional edge deposit + foreign-node graft)
- Added EXP-005 multi-agent exploration with environmental stigmergy signals:
  - `Assets/Scripts/Communication/MazeStigmergyField.cs`
  - `Assets/Scripts/Communication/MazeFrontierClaimField.cs`
  - `Assets/Scripts/Communication/AgentStigmergyController.cs`
  - `Assets/Scripts/Communication/Experiment005CommunicationMode.cs`
  - `Assets/Scripts/Experiments/Experiment005Runner.cs`
  - `Assets/Scripts/Experiments/Experiment005MetricsLogger.cs`
  - `Assets/Editor/Experiment005RunnerEditor.cs`
  - `docs/experiment_005_multi_agent_signals.md`
  - CSV output: `results/experiment_005_multi_agent_signals.csv`
  - Communication modes: `None`, `RandomNoise`, `Trail`, `FrontierHint`, `FrontierClaim`
- Added EXP-004 multi-agent exploration without communication:
  - `Assets/Scripts/Experiments/Experiment004Runner.cs`
  - `Assets/Scripts/Experiments/MultiAgentMetricsLogger.cs`
  - `Assets/Scripts/Experiments/MultiAgentStartLayout.cs`
  - `Assets/Editor/Experiment004RunnerEditor.cs`
  - `docs/experiment_004_multi_agent.md`
  - CSV output: `results/experiment_004_multi_agent.csv`
- Added `MetricsLogger` CSV output for EXP-001 (Days 8–9):
  - `Assets/Scripts/Experiments/MetricsLogger.cs`
  - One row per episode in `results/experiment_001_single_agent.csv`
  - Unified `coverage_percent` from unique visited cells in `Experiment001Runner`
- Added deterministic maze seed support for EXP-001:
  - `Assets/Scripts/Maze/MazeSeedConfig.cs`
  - `Assets/Scripts/Maze/MazeGenerator.cs`
- Refactored `Assets/Scripts/MazeGen.cs` to use seeded generation and log a reproducibility fingerprint.
- Added EXP-001 episode loop (Days 2–3):
  - `Assets/Scripts/Experiments/Experiment001Runner.cs`
  - `Assets/Scripts/Experiments/EpisodeTerminationReason.cs`
  - `Assets/Scripts/Agents/ManualAgentController.cs`
  - `Assets/Scripts/Maze/MazeCellIndex.cs`
- Added custom Inspector buttons for episode control (`Assets/Editor/Experiment001RunnerEditor.cs`).
- Added initial research documentation structure:
  - `docs/experiment_001_single_agent.md`
  - `docs/project_memory.md`
  - `docs/schedule_exp_001.md`
  - `docs/experiment_002_rrt_vs_baselines.md`
- Added Cursor rule for project memory and changelog discipline:
  - `.cursor/rules/project_memory_and_changelog.mdc`
- Added Cursor rule for Unity experiment debugging, reproducibility, metrics sanity checks, invariant checks, and visual debugging:
  - `.cursor/rules/unity_experiment_debugging.mdc`
- Added decision log for preserving why major research and engineering decisions were made:
  - `docs/decision_log.md`
- Added late-stage Theory of Mind research documentation:
  - `docs/theory_of_mind_late_stage.md`
- Added long-term intelligence-as-dynamic-stability research framing:
  - `docs/intelligence_as_dynamic_stability.md`

### Changed

- Pivoted the active research direction from RRT-centered maze path planning to swarm-based flying artificial life agents.
- Changed active experiment in project memory to `EXP-SWARM-001 — 3D Boids Baseline`.
- Marked RRT, Local RRT, and Swarm-RRT as historical / archived research work unless explicitly revived as comparison baselines.
- Updated README to point to `docs/swarm_flight_research_agenda.md` as the active direction.
- Changed EXP-005 coordination with `FrontierClaim` mode: agents publish temporary frontier responsibility claims, bias local RRT planning toward their own claimed frontier, and avoid duplicating peers' claimed regions.
- Changed `FrontierClaim` scoring to reward outward reachable frontiers and soft per-agent responsibility anchors across the maze, reducing dense local-region coverage.
- Tuned `FrontierClaim` to refresh claims sooner and reward directional progress from each agent's start toward its responsibility anchor, helping coverage expand beyond the initial local patch.
- Changed EXP-005 `FrontierClaim` starts to deterministic stratified-random cells across the full maze instead of a corner cluster.
- Changed EXP-005 smell deposits to diffuse through open passages over a configurable radius and tuned defaults for longer-lived signals.
- Changed `FrontierClaim` so agents no longer use generic smell-gradient following; smell remains visible/deposited while movement is driven by claims and frontier selection.
- Changed `LocalRrtAgent` to detect short A-B-A-B movement oscillations, clear the current plan/claim, and prefer a least-visited non-backtracking escape move.
- Changed `FrontierClaim` movement to one claim-guided step per tick instead of following multi-step RRT paths, with immediate backtrack suppression and local confinement detection (≤3 unique cells in recent history) to break small looping territories.
- Tuned `FrontierClaim` outward propagation: prefer farthest-tier reachable frontiers, refresh claims when reached or stalled, reward distance from spawn, and deposit smell once per cell (trails instead of dense local blobs).
- Changed `LocalRrtAgent` exploration fallback so agents that move without discovering new cells clear local plans, seek a random reachable frontier, and then prefer least-visited open passages before continuing local movement.
- Reframed the project direction toward a reproducible artificial life and maze-navigation research platform.
- Clarified that `EXP-001 — Single-Agent Navigation Benchmark` is the current active implementation focus before RRT, ML-Agents, multi-agent communication, or Swarm-RRT.
- Clarified that Theory of Mind, self-state modelling, social cognition, and knowledge-state estimation are late-stage research directions, not implementation tasks for EXP-001 or EXP-002.
- Clarified a future operational framing of intelligence as dynamic stability, viability maintenance, agent-relative Umwelt, prediction-action loops, adaptive randomness, and symbiotic composition of agent capabilities.

### Fixed

- Removed invalid `Unity.VisualScripting` import from legacy maze generation code (compile error CS0234).
- Replaced tank-style agent controls with top-down WASD movement and added a follow camera for EXP-001 manual testing.
- Maze walls now render in Game view via `MazeWallVisualizer` mesh (replacing Scene-only `Debug.DrawLine`).
- Fixed duplicated/scaled maze visuals by moving generation to `MazeSystem` (scale 1) and aligning the floor plane to 100x100 world units.
- Expanded EXP-001 camera zoom (orthographic top-down, up to full maze view; press **F** to toggle full-maze framing).
- Added RandomWalk baseline for EXP-001 (Days 4–5):
  - `Assets/Scripts/Agents/RandomWalkAgent.cs`
  - `Assets/Scripts/Experiments/Experiment001Algorithm.cs`
  - Algorithm selector on `Experiment001Runner` (`Manual` / `RandomWalk`)
  - Geometric maze raycast sensing (no physics colliders on walls)
  - Episode logs now include `collisions` and `pathLength`
- Added WallFollower baseline for EXP-001 (Days 6–7):
  - `Assets/Scripts/Agents/WallFollowerAgent.cs`
  - `WallFollowerRight` and `WallFollowerLeft` algorithms on `Experiment001Runner`
  - Grid-based passage sensing via `MazeGenerator.HasVisibleWallBetween`
  - Fixed maze carving neighbor lookup for row-0/column-0 adjacency (`j > 0`, `i > 0`)
  - Removed unstable `MeshCollider` wall sensing; orthographic camera snap to stop scene jitter
- Started EXP-003 — Local RRT under partial observability:
  - `docs/experiment_003_local_rrt.md`
  - `MazeLocalDiscoveryMap`, `LocalRrtPlanner`, `LocalRrtAgent`
  - `Experiment001Algorithm.LocalRrt` on episode runner
  - Logs include `coverage`, `rrtNodes`, `rrtIterations`

### Research Notes

- Experiment: `EXP-SWARM-001 — 3D Boids Baseline`
- Metrics impacted: planned swarm-flight metrics include `agent_count`, `mean_speed`, `mean_neighbor_distance`, `cohesion_index`, `separation_violations`, `boundary_hits`, and `coverage_volume_percent`.
- Scientific reason: flying swarm agents better support the project’s artificial-life goal than RRT-centered path planning because they foreground local rules, emergent swarm motion, foraging, environmental memory, hive-like coordination, and social behavior.
- Risks / limitations: no implementation was added in this phase; swarm visuals must be validated with metrics before claiming collective intelligence.
- Metrics impacted: EXP-005 CSV includes `frontier_claims_created`, `claim_conflicts`, and `claimed_frontier_steps` in addition to signal metrics.
- Scientific reason: test whether dynamic responsibility claims reduce duplicate exploration compared with no-communication, random signals, trails, and frontier hints.
- Risks / limitations:
  - wall-following may loop on non-simply-connected mazes (documented in EXP-001 spec);
  - `FrontierClaim` is hand-designed communication semantics and must be ablated against simpler signal modes before treating it as evidence of emergent coordination.

---

## Changelog Format For Future Updates

Use this structure under `[Unreleased]`:

```markdown
### Added
- ...

### Changed
- ...

### Fixed
- ...

### Research Notes
- Experiment: EXP-001 — Single-Agent Navigation Benchmark
- Metrics impacted: ...
- Scientific reason: ...
- Risks / limitations: ...
```

Guidelines:

- Record user-visible, research-visible, or architecture-visible changes.
- Do not record every tiny edit.
- Keep each entry connected to an experiment or research goal.
- Update `docs/project_memory.md` when experiment status, assumptions, metrics, or next steps change.
