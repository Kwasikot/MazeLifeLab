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

### Research Notes

- Active experiment: `EXP-001 — Single-Agent Navigation Benchmark`
- Current status: in progress — episode loop implemented (Days 2–3 milestone)
- Current focus:
  - metrics logger (CSV);
  - RandomWalk baseline;
  - WallFollower baseline;
  - batch runner.
- Metrics impacted: `maze_seed` will feed future CSV rows; no runtime metrics yet.
- Scientific reason: deterministic seeds are required before baseline comparison across runs.
- Risks / limitations:
  - maze still rendered with `Debug.DrawLine` only (no 3D wall colliders yet);
  - legacy neighbor-cell edge cases preserved from prototype;
  - carving uses `System.Random`, isolated from `UnityEngine.Random` used elsewhere.

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
