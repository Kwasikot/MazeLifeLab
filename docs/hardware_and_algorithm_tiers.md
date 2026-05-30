# Hardware Profiles and Algorithm Tiers

This document records the expected hardware used for developing and testing MazeLifeLab.

The purpose is to connect algorithmic ambition to realistic compute budgets.

MazeLifeLab should support two levels of experimentation:

1. **Laptop baseline** — reproducible, lightweight experiments that should run on the development laptop.
2. **Desktop extended** — heavier experiments using the stronger desktop GPU and CPU.

---

## 1. Known Hardware Profiles

### Profile A — Laptop Baseline

```text
Machine: HP Pavilion laptop
CPU: AMD Ryzen 5
GPU: NVIDIA RTX 3050 laptop GPU
Role: baseline development and reproducibility testing
```

Expected use:

- Unity Editor development;
- small procedural mazes;
- single-agent experiments;
- baseline agents;
- RRT debugging;
- small ML-Agents tests;
- short training runs;
- visual inspection.

This machine defines the **minimum practical target** for interactive work.

If an experiment cannot run at all on this machine, it should be labelled as an extended experiment.

---

### Profile B — Desktop Extended

```text
Machine: desktop PC
CPU: Intel Core i7-14700KF
RAM: 32 GB
Storage: 2 TB SSD
GPU: NVIDIA RTX 5070 Ti 16 GB
Role: heavier experiments, larger parameter sweeps, training, and multi-agent simulations
```

Expected use:

- larger maze batches;
- longer benchmark runs;
- ML-Agents training;
- larger population sizes;
- parameter sweeps;
- RRT/RRT* variants;
- multi-agent experiments;
- GPU-accelerated learning;
- heavier visualisation and logging.

This machine enables more ambitious experiments, but results should still be documented clearly as **desktop-tier** if they cannot be reproduced on the laptop.

---

## 2. Hardware Tier Labels

Every major experiment should specify a hardware tier.

Recommended labels:

```text
LAPTOP_BASELINE
DESKTOP_EXTENDED
GPU_TRAINING
BATCH_ANALYSIS
```

### LAPTOP_BASELINE

Runs comfortably on the HP Pavilion laptop.

Use for:

- EXP-001;
- deterministic maze generation;
- RandomWalk;
- WallFollower;
- CSV logging;
- small RRT demos;
- visual debugging.

### DESKTOP_EXTENDED

Uses the desktop PC for larger runs.

Use for:

- larger seed batches;
- RRT parameter sweeps;
- multi-agent experiments;
- longer simulations;
- expensive visualisation.

### GPU_TRAINING

Requires GPU training.

Use for:

- ML-Agents PPO/SAC training;
- imitation learning;
- neural policies;
- recurrent agents;
- multi-agent RL.

### BATCH_ANALYSIS

Runs offline analysis, possibly without graphics.

Use for:

- many seeds;
- statistical summaries;
- CSV post-processing;
- parameter sweeps;
- CI-style headless runs.

---

## 3. Algorithm Tiers

### Tier 0 — Deterministic Experimental Infrastructure

Hardware target:

```text
LAPTOP_BASELINE
```

Algorithms / systems:

- deterministic maze seeds;
- episode runner;
- metrics logger;
- CSV output;
- replayable experiment configuration;
- simple visual debugging.

Purpose:

- make experiments reproducible and reviewable.

This tier must remain lightweight.

---

### Tier 1 — Simple Baselines

Hardware target:

```text
LAPTOP_BASELINE
```

Algorithms:

- RandomWalk;
- WallFollowerLeft;
- WallFollowerRight;
- simple local sensor agents;
- simple finite-state navigation agents.

Purpose:

- establish weak and classical baselines;
- test the episode loop;
- test metrics;
- provide comparison points for later algorithms.

---

### Tier 2 — Classical Planning

Hardware target:

```text
LAPTOP_BASELINE for small runs
DESKTOP_EXTENDED for sweeps
```

Algorithms:

- A* with full maze graph;
- Dijkstra;
- BFS shortest path on grid graph;
- classical RRT;
- RRT-Connect;
- RRT*;
- PRM / Probabilistic Roadmaps;
- potential fields;
- local obstacle avoidance.

Purpose:

- establish strong non-learning planning baselines;
- separate planning advantage from learning advantage;
- prepare the ground for Local RRT and Swarm-RRT.

Notes:

- A* and BFS are full-map baselines.
- RRT may use geometry and collision checks.
- Local RRT is more scientifically relevant for partial observability.

---

### Tier 3 — Hybrid Planning + Learning

Hardware target:

```text
DESKTOP_EXTENDED
GPU_TRAINING for neural policies
```

Algorithms:

- RRT-guided reward shaping;
- RRT-generated demonstrations;
- imitation learning from planner paths;
- behavioural cloning from classical planners;
- RL policy with planner hints;
- local planner + learned high-level policy;
- curriculum learning over maze difficulty.

Purpose:

- bridge symbolic/classical planning and learned behaviour;
- reduce sample complexity of RL;
- test whether learning can generalise beyond planner-generated examples.

This tier is likely important because the original project idea involved spreading rewards across RRT and using checkpoint-like navigation.

---

### Tier 4 — Reinforcement Learning

Hardware target:

```text
GPU_TRAINING
DESKTOP_EXTENDED
```

Algorithms:

- PPO via Unity ML-Agents;
- SAC if appropriate;
- imitation learning;
- curriculum learning;
- recurrent policies for partial observability;
- intrinsic motivation / curiosity rewards;
- exploration bonuses;
- memory-augmented policies.

Purpose:

- test whether agents learn reusable navigation strategies;
- compare learned policies against classical planning baselines;
- evaluate generalisation to unseen maze seeds.

Important rule:

RL should not be introduced before EXP-001 metrics and classical baselines are stable.

---

### Tier 5 — Multi-Agent Exploration

Hardware target:

```text
DESKTOP_EXTENDED
GPU_TRAINING for learned policies
```

Algorithms / systems:

- multiple independent RandomWalk agents;
- multiple independent WallFollower agents;
- role-based finite-state agents;
- shared coverage maps;
- decentralised exploration;
- simple flocking-inspired movement;
- collision avoidance between agents.

Purpose:

- test whether adding agents improves exploration;
- measure redundant exploration;
- establish a no-communication multi-agent baseline.

---

### Tier 6 — Communication and Environmental Memory

Hardware target:

```text
DESKTOP_EXTENDED
GPU_TRAINING for learned communication
```

Algorithms / systems:

- light/sound signals;
- pheromone-like fields;
- decaying environmental markers;
- local broadcast messages;
- stigmergic memory;
- learned signal usage;
- communication cost penalties;
- signal ablation studies.

Purpose:

- test whether communication improves collective exploration;
- study primitive external memory;
- prepare for Swarm-RRT.

Scientific caution:

Do not call signals “language” until they are measured as useful, compositional, or convention-like.

---

### Tier 7 — Swarm-RRT / Distributed Search Trees

Hardware target:

```text
DESKTOP_EXTENDED
possibly GPU_TRAINING depending on implementation
```

Core idea:

```text
Classical RRT: one planner builds one search tree.
Swarm-RRT: many agents collectively approximate a search tree through local exploration, signals, and environmental memory.
```

Possible mechanisms:

- each agent acts as a mobile tree expansion frontier;
- agents deposit markers representing explored branches;
- signals encode frontier quality or dead ends;
- environmental memory stores branch utility;
- agents bias movement toward underexplored regions;
- local communication approximates global tree growth.

Purpose:

- create the most original research direction of MazeLifeLab;
- connect RRT, swarm intelligence, artificial life, and collective cognition.

---

## 4. What The Strong Desktop Enables

The RTX 5070 Ti 16 GB and i7-14700KF make the following more realistic:

- longer ML-Agents training runs;
- larger neural policies;
- recurrent policies for partial observability;
- multi-agent RL experiments;
- population-level experiments;
- parameter sweeps for RRT/RRT*;
- comparing 100+ maze seeds;
- curiosity-driven exploration experiments;
- imitation learning from planner-generated trajectories;
- batch evaluation without constant manual inspection.

However, stronger hardware should not remove the need for careful baselines.

The desktop should be used to scale experiments, not to hide unclear experimental design.

---

## 5. Recommended Experiment Scaling

### EXP-001 — Single-Agent Benchmark

Hardware:

```text
LAPTOP_BASELINE
```

Scale:

```text
10 seeds
2 baseline algorithms
small maze sizes
```

Do not use the desktop to make EXP-001 unnecessarily complex.

---

### EXP-002 — RRT vs Baselines

Hardware:

```text
LAPTOP_BASELINE for first implementation
DESKTOP_EXTENDED for parameter sweeps
```

Scale:

```text
10 seeds initially
100 seeds after stabilisation
RRT parameter sweeps on desktop
```

---

### EXP-003 — Local RRT

Hardware:

```text
DESKTOP_EXTENDED recommended
```

Reason:

Local/incremental planning may require repeated replanning and more logging.

---

### EXP-004 — Multi-Agent No Communication

Hardware:

```text
DESKTOP_EXTENDED recommended
```

Scale:

```text
2, 4, 8, 16, 32 agents
```

Start small before testing larger populations.

---

### EXP-005 — Communication

Hardware:

```text
DESKTOP_EXTENDED
GPU_TRAINING if learned policies are used
```

Scale:

```text
communication disabled
a communication random-control condition
hand-designed signal condition
learned signal condition
```

---

### EXP-006 — Swarm-RRT

Hardware:

```text
DESKTOP_EXTENDED
```

Scale:

```text
small proof-of-concept first
large population experiments later
```

---

## 6. Divergent Algorithm Ideas Enabled By The Desktop

The stronger desktop makes it reasonable to explore more ambitious algorithms later.

Candidate directions:

### Curiosity-Driven RL

Agents receive reward for reducing uncertainty or visiting novel states.

Useful for:

- exploration;
- partial observability;
- artificial life framing.

### World-Model Agents

Agents learn a local predictive model of maze dynamics and use it for planning.

Useful for:

- model-based RL;
- internal map formation;
- active inference-inspired experiments.

### Graph Neural Networks

Represent discovered maze structure as a graph and learn policies over that graph.

Useful for:

- generalisation across maze layouts;
- learned planning;
- multi-agent shared maps.

### Differentiable / Neural Planners

Train neural policies to imitate or approximate classical planning.

Useful for:

- comparing symbolic planning with learned heuristics;
- hybrid RRT + neural systems.

### Evolutionary Algorithms

Evolve agent controllers, reward weights, or communication rules.

Useful for:

- artificial life;
- emergent behaviour;
- non-gradient search.

### Quality-Diversity / MAP-Elites

Search for diverse navigation strategies rather than one best policy.

Useful for:

- artificial life;
- behavioural diversity;
- discovering unexpected agent types.

### Neuroevolution

Evolve neural controllers instead of training only by gradient descent.

Useful for:

- simple embodied agents;
- emergent communication;
- comparing evolution vs learning.

### Multi-Agent Curriculum Learning

Gradually increase maze complexity, population size, and communication constraints.

Useful for:

- robust training;
- avoiding early failure in difficult mazes.

### Active Inference-Inspired Agents

Agents select actions to reduce uncertainty or expected surprise.

Useful for:

- biologically inspired cognition;
- exploration under uncertainty;
- bridging AI and cognitive science.

### Swarm-RRT

A population of agents approximates tree expansion collectively.

Useful for:

- the most original MazeLifeLab research direction;
- distributed planning;
- collective intelligence.

---

## 7. Guardrail

The stronger desktop makes more complex algorithms possible, but it does not change the immediate rule:

```text
Finish EXP-001 first.
```

Only after reproducibility, metrics, baselines, and CSV logging work should the project move toward heavier algorithms.

A good research platform scales from simple to complex without losing measurement discipline.
