# EXP-001 Implementation Schedule

## Purpose

This file is a practical schedule for implementing **EXP-001 — Single-Agent Navigation Benchmark**.

Use it as a daily reference while working with Cursor.

The goal is to avoid scope creep and keep the project focused on one narrow scientific milestone:

> Build a reproducible single-agent maze-navigation benchmark with deterministic seeds, baseline agents, and metrics logging.

---

# Core Rule

During this schedule, do not implement:

- ML-Agents training;
- multi-agent behaviour;
- agent communication;
- pheromone systems;
- emergent language;
- hierarchical agents;
- advanced swarm logic.

These belong to later experiments.

For now, focus only on:

```text
Maze seed -> Episode loop -> Baselines -> Metrics -> CSV -> Reproducibility
```

---

# Expected Duration

Recommended duration:

```text
10–14 focused working days
```

This is not a deadline. It is a pacing guide.

If something takes longer, do not panic. Prefer correctness and reproducibility over speed.

---

# Current Active Experiment

```text
EXP-001 — Single-Agent Navigation Benchmark
```

Reference documents:

```text
docs/research_agenda.md
docs/experiment_001_single_agent.md
docs/project_memory.md
```

---

# Phase 0 — Preparation

## Goal

Make sure Cursor understands the project context.

## Checklist

- [ ] Read `docs/research_agenda.md`.
- [ ] Read `docs/experiment_001_single_agent.md`.
- [ ] Read `docs/project_memory.md`.
- [ ] Read `.cursor/rules/project_memory_and_changelog.mdc`.
- [ ] Confirm that the active experiment is EXP-001.
- [ ] Confirm that ML-Agents and multi-agent systems are out of scope for now.

## Cursor Prompt

```text
Read docs/research_agenda.md, docs/experiment_001_single_agent.md, docs/project_memory.md, and .cursor/rules/project_memory_and_changelog.mdc.

Do not modify code yet.

Summarise the current active experiment, implementation constraints, and the next smallest implementation step.
```

## Done When

Cursor can correctly explain that the current task is a reproducible single-agent benchmark, not ML training or multi-agent communication.

---

# Day 1 — Deterministic Maze Seed

## Goal

Make maze generation reproducible.

## Why This Matters

Without deterministic seeds, experiments cannot be compared scientifically.

## Tasks

- [ ] Identify current maze generation code.
- [ ] Add configurable maze seed.
- [ ] Ensure same seed produces same maze.
- [ ] Ensure different seeds produce different mazes.
- [ ] Add a simple way to inspect or log the seed.
- [ ] Update `docs/project_memory.md` if seed support is completed.
- [ ] Update `CHANGELOG.md`.

## Cursor Prompt

```text
Implement deterministic maze seed support for EXP-001.

Requirements:
- Same seed should generate the same maze.
- Different seeds should generate different mazes.
- Keep the change small.
- Do not add ML-Agents, multi-agent logic, or communication.
- Update CHANGELOG.md.
- Update docs/project_memory.md only if the milestone is completed.

After implementation, explain how to verify determinism in Unity.
```

## Done When

- [ ] Same seed can be run twice with the same maze result.
- [ ] Different seed changes the maze.
- [ ] No unrelated systems were added.

---

# Days 2–3 — Start, Goal, and Episode Loop

## Goal

Create the basic structure of an experiment episode.

## Tasks

- [ ] Define start cell.
- [ ] Define goal cell.
- [ ] Place agent at start.
- [ ] Place visible goal object.
- [ ] Detect goal reached.
- [ ] Add max step timeout.
- [ ] Reset episode cleanly.
- [ ] Track step count.
- [ ] Track termination reason.
- [ ] Update `CHANGELOG.md`.

## Cursor Prompt

```text
Implement the EXP-001 episode loop.

Requirements:
- The agent starts from a defined start cell.
- A goal is placed in a defined goal cell.
- Episode ends when the agent reaches the goal.
- Episode ends with timeout after max_steps.
- Track steps and termination_reason.
- Keep this independent from ML-Agents.
- Do not add multi-agent or communication features.
- Update CHANGELOG.md.

Prefer adding code under Assets/Scripts/Experiments and Assets/Scripts/Agents where appropriate.
```

## Done When

- [ ] Agent starts correctly.
- [ ] Goal is visible.
- [ ] Success termination works.
- [ ] Timeout termination works.
- [ ] Episode can reset.

---

# Days 4–5 — Random Walk Baseline

## Goal

Implement the weakest baseline agent.

## Why This Matters

Random walk establishes a minimum performance baseline and tests the episode loop.

## Tasks

- [ ] Create `RandomWalkAgent` or equivalent.
- [ ] Define action set.
- [ ] Make the agent move autonomously.
- [ ] Avoid obvious stuck behaviour if simple to do.
- [ ] Run random walk until success or timeout.
- [ ] Track collisions if possible.
- [ ] Update `CHANGELOG.md`.

## Cursor Prompt

```text
Implement the RandomWalk baseline for EXP-001.

Requirements:
- Agent selects simple random actions.
- Agent can run inside the existing episode loop.
- Episode terminates on success or timeout.
- Do not implement learning.
- Do not implement communication.
- Keep behaviour simple and measurable.
- Update CHANGELOG.md.

After implementation, describe how to run RandomWalk on one maze seed.
```

## Done When

- [ ] Random walk agent moves without manual input.
- [ ] Random walk can complete or timeout.
- [ ] It can be selected as an algorithm.
- [ ] No advanced AI was added.

---

# Days 6–7 — Wall-Following Baseline

## Goal

Implement a simple classical maze-solving heuristic.

## Why This Matters

Wall-following provides a stronger baseline than random walk.

## Tasks

- [ ] Create `WallFollowerAgent` or equivalent.
- [ ] Implement right-hand wall-following rule.
- [ ] Optionally support left-hand rule.
- [ ] Use local sensing or collision probes.
- [ ] Run until success or timeout.
- [ ] Track steps and collisions.
- [ ] Update `CHANGELOG.md`.

## Cursor Prompt

```text
Implement the WallFollower baseline for EXP-001.

Requirements:
- Implement a simple right-hand wall-following rule.
- Optionally support left-hand rule if it is simple.
- Use local sensing, raycasts, or collision probes.
- Run inside the same episode loop as RandomWalk.
- Do not add global path planning yet.
- Do not add ML-Agents.
- Do not add multi-agent logic.
- Update CHANGELOG.md.

After implementation, compare expected behaviour with RandomWalk qualitatively.
```

## Done When

- [ ] WallFollower moves autonomously.
- [ ] It can run on the same maze seeds as RandomWalk.
- [ ] It terminates on success or timeout.
- [ ] It is selectable as an algorithm.

---

# Days 8–9 — Metrics Logger and CSV Output

## Goal

Record experiment results in machine-readable form.

## Required CSV Columns

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

## Tasks

- [ ] Create `MetricsLogger` or equivalent.
- [ ] Track episode id.
- [ ] Track algorithm name.
- [ ] Track maze seed.
- [ ] Track success.
- [ ] Track steps.
- [ ] Track collisions.
- [ ] Track approximate path length.
- [ ] Track approximate coverage percent.
- [ ] Track termination reason.
- [ ] Write CSV file.
- [ ] Update `CHANGELOG.md`.
- [ ] Update `docs/project_memory.md` if metrics are completed.

## Cursor Prompt

```text
Implement MetricsLogger and CSV output for EXP-001.

Required columns:
- episode_id
- maze_seed
- algorithm
- success
- steps
- collisions
- path_length
- coverage_percent
- termination_reason

Requirements:
- One row per episode.
- Output should be machine-readable CSV.
- Keep metrics definitions consistent with docs/experiment_001_single_agent.md.
- Update CHANGELOG.md.
- Update docs/project_memory.md if the metrics milestone is complete.

Do not add ML-Agents or communication.
```

## Done When

- [ ] CSV file is created.
- [ ] Each episode writes one row.
- [ ] Required columns exist.
- [ ] Values are plausible when checked visually.

---

# Days 10–11 — Batch Runner Across Seeds

## Goal

Run multiple seeds and algorithms automatically.

## Recommended Seeds

```text
1, 2, 3, 4, 5, 10, 20, 42, 100, 123
```

## Tasks

- [ ] Add batch runner.
- [ ] Run RandomWalk across seed list.
- [ ] Run WallFollower across seed list.
- [ ] Append all results to CSV.
- [ ] Ensure algorithm and seed are recorded correctly.
- [ ] Update `CHANGELOG.md`.

## Cursor Prompt

```text
Implement a simple batch runner for EXP-001.

Requirements:
- Run RandomWalk and WallFollower across a list of maze seeds.
- Recommended seeds: 1, 2, 3, 4, 5, 10, 20, 42, 100, 123.
- Write one CSV row per episode.
- Keep implementation simple and Editor-friendly.
- Do not add ML-Agents.
- Do not add multi-agent features.
- Update CHANGELOG.md.

After implementation, describe how to run the batch benchmark from Unity.
```

## Done When

- [ ] At least 10 seeds can be run.
- [ ] At least 2 algorithms can be compared.
- [ ] CSV contains multiple rows.
- [ ] Results are reproducible enough to compare.

---

# Days 12–14 — Validation, Cleanup, and Review

## Goal

Stabilise the experiment and make it reviewable.

## Tasks

- [ ] Run visual inspection for several seeds.
- [ ] Check whether same seed gives same maze.
- [ ] Check whether CSV values are plausible.
- [ ] Check if RandomWalk and WallFollower are both logged correctly.
- [ ] Fix obvious bugs.
- [ ] Do not add new features.
- [ ] Update `docs/project_memory.md` status.
- [ ] Update `CHANGELOG.md`.
- [ ] Prepare summary for review.

## Cursor Prompt

```text
Perform validation and cleanup for EXP-001.

Do not add new features.

Check:
- deterministic maze seed behaviour;
- episode success and timeout;
- RandomWalk baseline;
- WallFollower baseline;
- CSV logging;
- metric plausibility;
- CHANGELOG.md;
- docs/project_memory.md.

Produce a concise review summary with remaining risks and known issues.
```

## Done When

- [ ] EXP-001 can be run end-to-end.
- [ ] Results are logged.
- [ ] Known limitations are documented.
- [ ] Project memory reflects current status.
- [ ] Changelog reflects major changes.

---

# End-of-Experiment Checklist

EXP-001 is complete when:

- [ ] Deterministic maze seed exists.
- [ ] Start cell exists.
- [ ] Goal cell exists.
- [ ] Episode loop exists.
- [ ] Success condition exists.
- [ ] Timeout condition exists.
- [ ] RandomWalk baseline exists.
- [ ] WallFollower baseline exists.
- [ ] MetricsLogger exists.
- [ ] CSV output exists.
- [ ] Batch runner across seeds exists.
- [ ] At least 10 seeds can be tested.
- [ ] Results are reproducible enough for comparison.
- [ ] `CHANGELOG.md` is updated.
- [ ] `docs/project_memory.md` is updated.

---

# Review Prompt For ChatGPT / Advisor

Use this prompt when you want an external review:

```text
Please review MazeLifeLab EXP-001.

Check whether the implementation matches:
- docs/research_agenda.md
- docs/experiment_001_single_agent.md
- docs/project_memory.md
- docs/schedule_exp_001.md

Focus on:
- reproducibility;
- metrics correctness;
- baseline validity;
- scope creep;
- scientific usefulness;
- obvious architecture risks.

Do not suggest advanced features unless they are necessary to complete EXP-001.
```

---

# If You Feel Lost

Return to this question:

> What is the smallest next change that makes EXP-001 more reproducible, measurable, or reviewable?

If the change does not help with reproducibility, measurement, or reviewability, postpone it.

---

# Next Experiment After EXP-001

Only after EXP-001 is complete, move to:

```text
EXP-002 — RRT vs Baselines
```

Possible document:

```text
docs/experiment_002_rrt_vs_baselines.md
```

Do not start EXP-002 until EXP-001 has stable metrics and CSV output.
