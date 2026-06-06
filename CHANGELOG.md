# Changelog

All notable changes to MazeLifeLab will be documented in this file.

This project follows a research-oriented changelog discipline: changes should be connected to experiments, metrics, baselines, architecture, or scientific assumptions.

---

## [Unreleased]

### Added

- Added initial research documentation structure:
  - `docs/research_agenda.md`
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

- Nothing yet.

### Research Notes

- Active experiment: `EXP-001 — Single-Agent Navigation Benchmark`
- Current status: planning / preparation
- Current focus:
  - deterministic maze generation;
  - single-agent episode loop;
  - baseline agents;
  - metrics logging;
  - CSV output;
  - reproducibility.
- Metrics impacted: none yet; late-stage social-cognition and dynamic-stability metrics are documented but not implemented.
- Scientific reason: establish a reproducible baseline before adding RRT, ML-Agents, multi-agent communication, environmental memory, social state estimation, Theory of Mind-like mechanisms, viability-based navigation, or intelligence-as-dynamic-stability layers.
- Risks / limitations:
  - current implementation may still be prototype-level;
  - deterministic behaviour may be affected by Unity physics;
  - metrics are not yet implemented;
  - baselines are not yet implemented;
  - README still describes the broader long-term roadmap and should later be aligned with the staged experiment documents;
  - Theory of Mind terminology may cause scope creep unless kept as cautious late-stage documentation only;
  - intelligence-as-dynamic-stability terminology may cause scope creep unless kept operational, measurable, and explicitly future-stage.

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
