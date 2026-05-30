# Research Decision Gates and Fallback Routes

This document defines how MazeLifeLab should decide whether to continue, pivot, pause, or abandon a research direction.

The purpose is to avoid two failure modes:

1. **Rigid tunnel vision** — forcing the project toward Swarm-RRT even if evidence does not support it.
2. **Chaotic exploration** — jumping between RRT, RL, ML-Agents, communication, and artificial life without measurable progress.

MazeLifeLab should behave like a research programme with flexible but disciplined branching.

---

## 1. Core Principle

A research direction should continue only if it remains:

- reproducible;
- measurable;
- comparable to baselines;
- connected to a documented research question;
- technically maintainable;
- scientifically interpretable.

If a direction fails these conditions, the project should not force it. Instead, it should switch to a documented fallback path.

---

## 2. Experiment Ladder

The current planned ladder is:

```text
EXP-001 — Single-Agent Navigation Benchmark
    ↓
EXP-002 — RRT vs Baselines
    ↓
EXP-003 — Local RRT Under Partial Observability
    ↓
EXP-004 — Multi-Agent Exploration Without Communication
    ↓
EXP-005 — Multi-Agent Exploration With Simple Signals
    ↓
EXP-006 — Swarm-RRT / Distributed Search Trees
```

This ladder is a guide, not a prison.

Each experiment should have a decision gate before the project moves forward.

---

## 3. Decision Gate Template

After each experiment, answer these questions:

```text
1. Is the experiment reproducible?
2. Are the metrics trustworthy?
3. Does the method beat relevant baselines?
4. Is the result scientifically interpretable?
5. Are failure modes understood?
6. Is the next experiment justified by evidence?
7. Should the project continue, pivot, pause, or simplify?
```

Record the decision in:

```text
docs/decision_log.md
CHANGELOG.md
docs/project_memory.md if active experiment or assumptions change
```

---

## 4. Decision Outcomes

Each decision gate should produce one of these outcomes:

### CONTINUE

The experiment worked well enough to proceed to the next planned experiment.

Use when:

- metrics are stable;
- results are interpretable;
- baselines are valid;
- next step is clearly motivated.

### ITERATE

The direction is promising, but implementation or metrics need improvement.

Use when:

- bugs remain;
- metrics are incomplete;
- results are noisy;
- visual behaviour and metrics disagree;
- baseline comparison is not yet fair.

### SIMPLIFY

The experiment is too complex and should be reduced to a smaller version.

Use when:

- too many features were added at once;
- debugging becomes unclear;
- the project has scope creep;
- the experiment cannot be interpreted.

### PIVOT

The current direction is not producing value, but a related direction is promising.

Use when:

- a hypothesis fails;
- another algorithm looks more promising;
- implementation cost is too high;
- the result suggests a different research question.

### PAUSE

The direction is interesting but should be postponed.

Use when:

- prerequisites are missing;
- hardware or tooling is not ready;
- other experiments are more foundational.

### ABANDON

The direction should be stopped for now.

Use when:

- it repeatedly fails;
- it cannot beat simple baselines;
- it is too hard to measure;
- it does not support the project’s research identity.

---

## 5. Fallback Tree

### If EXP-001 Fails

Problem examples:

- deterministic seeds do not work;
- episode loop is unstable;
- metrics are unreliable;
- CSV output is inconsistent;
- Unity physics makes runs too noisy.

Fallback options:

1. Reduce maze size.
2. Use simpler grid-based logic before full Unity physics.
3. Separate pure maze graph logic from visual Unity representation.
4. Implement headless logic tests before visual simulation.
5. Defer physical movement and test cell-to-cell movement first.

Do not proceed to RRT or ML-Agents if EXP-001 is unstable.

---

### If EXP-002 RRT Does Not Beat Baselines

Problem examples:

- RRT is slower than WallFollower;
- RRT fails in narrow maze corridors;
- path following fails despite path planning success;
- RRT only works with unrealistic full-map assumptions.

Fallback options:

1. Keep RRT as a visual/planning baseline only.
2. Try A*, BFS, or Dijkstra as stronger classical baselines.
3. Try RRT-Connect or RRT* only if metrics justify it.
4. Shift toward Local RRT under partial observability.
5. Use RRT to generate demonstrations for imitation learning instead of treating it as final behaviour.

Interpretation:

Failure of RRT is still useful if it clarifies when sampling-based planning is inappropriate for maze worlds.

---

### If Local RRT Is Too Hard

Problem examples:

- partial map representation becomes too complex;
- local replanning is unstable;
- the agent cannot use local trees effectively;
- debugging planner state becomes difficult.

Fallback options:

1. Use occupancy-grid mapping first.
2. Use frontier-based exploration.
3. Use A* on the discovered partial graph.
4. Use simple exploration heuristics with memory.
5. Defer Local RRT and continue with multi-agent baselines.

---

### If Multi-Agent Exploration Does Not Help

Problem examples:

- multiple agents duplicate work;
- collisions increase;
- collective coverage does not improve;
- coordination overhead dominates.

Fallback options:

1. Add simple spatial separation rules.
2. Add shared visited-cell memory.
3. Test fewer agents.
4. Introduce role-based finite-state agents.
5. Compare against one stronger single-agent planner.

Important interpretation:

More agents do not automatically imply collective intelligence.

---

### If Communication Does Not Help

Problem examples:

- signals are ignored;
- signals become noise;
- communication cost outweighs benefit;
- agents overcommunicate;
- no measurable improvement over no-communication baseline.

Fallback options:

1. Add communication cost.
2. Test hand-designed signal semantics before learned communication.
3. Use environmental markers instead of direct messages.
4. Use ablation studies: no signal, random signal, meaningful signal.
5. Shift to stigmergy / pheromone fields rather than language-like communication.

Important interpretation:

Communication is only scientifically meaningful if it improves measured behaviour or reveals interpretable coordination.

---

### If Swarm-RRT Does Not Work

Problem examples:

- agents do not approximate a useful search tree;
- environmental memory becomes noisy;
- distributed search is worse than classical RRT;
- metrics cannot distinguish tree-like search from random exploration;
- implementation becomes too complex.

Fallback options:

1. Reframe Swarm-RRT as an exploratory visual model, not the main algorithm.
2. Return to Local RRT and planner-learning hybrids.
3. Study stigmergic exploration instead of explicit tree formation.
4. Use RRT only as an analysis tool: compare swarm trajectories to RRT trees.
5. Switch to quality-diversity search for diverse exploration strategies.
6. Focus on multi-agent coverage and communication metrics instead of RRT analogy.

Important interpretation:

Swarm-RRT is a hypothesis, not a commitment. If it fails, the project can still produce valuable results in artificial life, multi-agent exploration, or hybrid planning.

---

## 6. Alternative Research Branches

If the main Swarm-RRT path becomes weak, MazeLifeLab can pivot to one of these branches.

### Branch A — Classical Planning Benchmark

Focus:

- A*;
- Dijkstra;
- BFS;
- RRT;
- RRT*;
- PRM;
- Local RRT.

Scientific value:

- clear comparisons;
- strong reproducibility;
- useful baseline platform.

---

### Branch B — Learning From Planners

Focus:

- planner-generated demonstrations;
- imitation learning;
- behavioural cloning;
- RL with planner-shaped rewards;
- curriculum learning.

Scientific value:

- bridges classical planning and learned policies;
- uses RRT/A* as teachers rather than competitors.

---

### Branch C — Artificial Life and Stigmergy

Focus:

- pheromone fields;
- environmental memory;
- decaying markers;
- collective coverage;
- emergent trail systems.

Scientific value:

- closer to ant colonies, slime mold, and self-organisation;
- may be more natural than forcing an RRT analogy.

---

### Branch D — Multi-Agent Coordination

Focus:

- multiple agents;
- no-communication baseline;
- shared memory;
- role assignment;
- local communication;
- coordination cost.

Scientific value:

- studies when adding agents helps or harms exploration.

---

### Branch E — Complexity and Adaptive Systems

Focus:

- maze worlds as adaptive systems;
- agent populations;
- robustness;
- diversity of strategies;
- emergence and self-organisation.

Scientific value:

- aligns with the complexity-science inspiration of the project.

---

## 7. Interface Discipline

Fallbacks are only possible if the code remains modular.

Keep these systems separable:

```text
Maze generation
Agent control
Planner interface
Experiment runner
Metrics logger
Debug visualisation
Communication system
Training system
```

Do not hard-wire one algorithm into the experiment loop.

Prefer interfaces such as:

```text
INavigationPolicy
IPlanner
IExperimentAgent
IMetricsSink
ICommunicationModel
```

Names may change, but the architectural principle should remain.

---

## 8. Success Criteria Should Be Relative To Baselines

Do not ask only:

```text
Did the algorithm work?
```

Ask:

```text
Did it outperform the relevant baseline under the same conditions?
```

Examples:

- RandomWalk is a weak baseline.
- WallFollower is a simple heuristic baseline.
- A* is a full-map oracle-like baseline.
- RRT_Global is a full-observability sampling-based baseline.
- Local RRT is a partial-observability planning baseline.
- No-communication multi-agent exploration is a baseline for communication experiments.

A complex algorithm that does not beat simple baselines should be treated sceptically.

---

## 9. When To Change The Active Experiment

The active experiment should change only after a decision gate.

Before switching, update:

```text
docs/project_memory.md
CHANGELOG.md
docs/decision_log.md
```

Do not let Cursor switch the active experiment implicitly.

The user or project maintainer should explicitly approve the switch.

---

## 10. Practical Cursor Rule

When Cursor proposes a new feature, it should answer:

```text
Which experiment does this support?
Which baseline will it be compared against?
Which metric will detect improvement?
What is the fallback if it fails?
```

If these questions cannot be answered, the feature should be postponed.

---

## 11. Current Recommendation

Current state:

```text
Active experiment: EXP-001
Recommended action: finish reproducible single-agent benchmark first
```

Do not start Swarm-RRT implementation yet.

Use Swarm-RRT as a long-term hypothesis, not as the current MVP.
