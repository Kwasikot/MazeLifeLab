# Changelog

All notable changes to MazeLifeLab will be documented in this file.

This project follows a research-oriented changelog discipline: changes should be connected to experiments, metrics, baselines, architecture, or scientific assumptions.

---

## [Unreleased]

### Added

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

### Research Notes

- Active experiment: `EXP-001 — Single-Agent Navigation Benchmark`
- Current status: in progress — Days 6–7 complete (RandomWalk + WallFollower baselines)
- Current focus:
  - MetricsLogger (CSV) — Days 8–9;
  - batch runner — Days 10–11;
  - validation pass — Days 12–14.
- Metrics impacted: episode logs include `steps`, `collisions`, `pathLength`; CSV export pending.
- Scientific reason: deterministic seeds and comparable baselines before batch evaluation.
- Risks / limitations:
  - wall-following may loop on non-simply-connected mazes (documented in EXP-001 spec);
  - RandomWalk still uses geometric raycast sensing (WallFollower uses grid topology).

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
