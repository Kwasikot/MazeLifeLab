# MazeLifeLab / BallPark

```text
  + - - - + - + - -
  + - + - + Kwasikot

                            )            (
                           /(   (\___/)  )\
                          ( #)  \ ('')| ( #
                           ||___c\  > '__||
                           ||**** ),_/ **'|
                     .__   |'* ___| |___*'|
                      \_\  |' (    ~   ,)'|
                       ((  |' /(.  '  .)\ |
                        \\_|_/ <_ _____> \______________
                         /   '-, \   / ,-'      ______  \
                b'ger   /      (//   \\)     __/     /   \
                                            './_____/
```

## 🎯 Project Overview

**MazeLifeLab** is a revival of my old **BallPark** idea.

The long-term goal is to build a small research-oriented Unity laboratory for studying:

- procedural maze generation;
- path planning;
- reinforcement learning;
- local sensing;
- artificial life;
- multi-agent exploration;
- communication and collective intelligence.

The project combines:

- 🌀 Procedural maze generation (`MazeGen.cs`)
- 🌳 Path planning via **Rapidly-Exploring Random Trees** (`Rrt.cs`)
- 🤖 Agents that can scan, navigate, learn, and eventually communicate (`AgentControl.cs`)
- 🧠 Future integration with **Unity ML-Agents**

The project should be developed as a **reproducible research platform**, not only as a Unity prototype.

---

## 🔬 Current Active Research Track

The current active implementation focus is:

```text
EXP-001 — Single-Agent Navigation Benchmark
```

The purpose of EXP-001 is to establish a stable experimental foundation before adding advanced AI features.

Current focus:

- deterministic maze generation with seeds;
- start and goal positions;
- single-agent episode loop;
- RandomWalk baseline;
- WallFollower baseline;
- metrics logging;
- CSV output;
- reproducible experiment runs.

Out of scope for the current stage:

- ML-Agents training;
- neural networks;
- multi-agent coordination;
- sound/light communication;
- pheromone systems;
- emergent language;
- Swarm-RRT.

These are important future directions, but they should not be implemented before the basic benchmark is reproducible and measurable.

Key documents:

- [`docs/research_agenda.md`](docs/research_agenda.md) — long-term research direction.
- [`docs/experiment_001_single_agent.md`](docs/experiment_001_single_agent.md) — current experiment protocol.
- [`docs/schedule_exp_001.md`](docs/schedule_exp_001.md) — practical implementation schedule.
- [`docs/project_memory.md`](docs/project_memory.md) — compact current project context.
- [`docs/hardware_and_algorithm_tiers.md`](docs/hardware_and_algorithm_tiers.md) — hardware profiles and algorithm scaling tiers.
- [`docs/experiment_002_rrt_vs_baselines.md`](docs/experiment_002_rrt_vs_baselines.md) — future RRT comparison protocol.
- [`docs/theory_of_mind_late_stage.md`](docs/theory_of_mind_late_stage.md) — late-stage Theory of Mind / social cognition roadmap.
- [`docs/intelligence_as_dynamic_stability.md`](docs/intelligence_as_dynamic_stability.md) — long-term intelligence-as-dynamic-stability research framing.
- [`docs/decision_log.md`](docs/decision_log.md) — why major decisions were made.
- [`CHANGELOG.md`](CHANGELOG.md) — notable changes and research notes.

---

## 🧭 Research Direction

The long-term research ladder is:

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

The most original long-term idea is **Swarm-RRT**:

> Can a population of locally perceiving agents collectively approximate a search tree through movement, communication, and environmental memory?

This connects the project to artificial life, swarm robotics, embodied cognition, distributed planning, and collective intelligence.

A later research direction may study operational Theory of Mind-like mechanisms, such as other-agent state estimation and knowledge-state estimation, only after multi-agent baselines, communication metrics, and environmental or group memory are established. See [`docs/theory_of_mind_late_stage.md`](docs/theory_of_mind_late_stage.md).

A broader long-term research framing treats intelligence as dynamic stability: agents become more capable by transforming local perception into memory, memory into prediction, prediction into action, and action into collective adaptive structure. See [`docs/intelligence_as_dynamic_stability.md`](docs/intelligence_as_dynamic_stability.md).

---

## 🖥️ Hardware Profiles

MazeLifeLab is expected to be tested on two hardware tiers:

### Laptop baseline

```text
HP Pavilion laptop
CPU: AMD Ryzen 5
GPU: NVIDIA RTX 3050 laptop GPU
```

Used for:

- EXP-001;
- Unity Editor development;
- deterministic seed testing;
- simple baselines;
- small RRT debugging;
- visual inspection.

### Desktop extended

```text
Desktop PC
CPU: Intel Core i7-14700KF
RAM: 32 GB
Storage: 2 TB SSD
GPU: NVIDIA RTX 5070 Ti 16 GB
```

Used for:

- larger seed batches;
- RRT/RRT* parameter sweeps;
- ML-Agents training;
- multi-agent experiments;
- population-level artificial life simulations;
- heavier logging and batch analysis.

See [`docs/hardware_and_algorithm_tiers.md`](docs/hardware_and_algorithm_tiers.md) for the detailed hardware-aware algorithm roadmap.

---

## 🚀 MVP Plan

### Phase 1. Core mechanics

- [x] Simple car/agent movement in Unity
- [x] Basic procedural maze generation
- [ ] Deterministic maze seeds
- [ ] Start and goal positions
- [ ] Episode success / timeout loop
- [ ] Visualise walls and trajectories

### Phase 2. EXP-001 Benchmark

- [ ] RandomWalk baseline
- [ ] WallFollower baseline
- [ ] Metrics logger
- [ ] CSV output
- [ ] Batch runner across multiple maze seeds
- [ ] Reproducibility check

### Phase 3. Path planning

- [ ] Refactor RRT into a reusable planner
- [ ] Visualise search tree in real time
- [ ] Compare RandomWalk vs WallFollower vs RRT
- [ ] Record RRT-specific metrics

### Phase 4. AI Integration

- [ ] Connect Unity ML-Agents
- [ ] Define basic reward functions
- [ ] Train an agent to navigate the maze
- [ ] Compare RL with classical baselines

### Phase 5. Multi-agent artificial life experiments

- [ ] Run multiple agents simultaneously
- [ ] Add simple communication signals
- [ ] Add environmental memory / pheromone-like fields
- [ ] Compare no-communication vs communication conditions
- [ ] Explore Swarm-RRT-like distributed search

---

## 📋 Requirements

The project can draw inspiration from biology, swarm robotics, active inference, distributed cognition, and artificial life.

This project is **not primarily an autopilot project**. A car-like system may be used as a convenient embodiment, but the core goal is broader:

> simulate navigation, exploration, communication, and collective intelligence in procedural environments.

### Functional

- Agent can move in a 3D maze.
- Maze is procedurally generated.
- Maze generation can be reproduced with seeds.
- Experiments can be run as episodes.
- Metrics can be logged to CSV.
- Baseline agents can be compared.
- Future stages may include RRT, RL, multi-agent support, and communication.

### Non-functional

- Runs in real time on the laptop baseline for EXP-001-scale experiments.
- Can scale to the desktop extended profile for heavier experiments.
- Modular code structure: `Maze`, `Agents`, `Planning`, `Experiments`, `Communication`.
- Documented workflow in `docs/`.
- Simple visualisation with Unity Gizmos and `Debug.DrawLine`.
- Reproducibility is preferred over visual complexity.

---

## 🛠 Tech Stack

- **Unity 2022+**
- **C#** for agents, maze generation, planners, and experiment runners
- **Python 3.10+** for future training logic
- **Unity ML-Agents Toolkit** for future reinforcement learning experiments

---

## 📚 References and Inspirations

- Mitchell, Melanie. *Complexity: A Guided Tour*. Oxford University Press, 2009.
- Strogatz, Steven H. *Nonlinear Dynamics and Chaos: With Applications to Physics, Biology, Chemistry, and Engineering*.
- LaValle, Steven M. *Rapidly-Exploring Random Trees: Progress and Prospects*.
- [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents)
- Turgut, A. E., Çelikkanat, H., Gökçe, F., & Şahin, E. (2008). *Self-organized flocking in mobile robot swarms*. Swarm Intelligence, 2, 97–120.
- Trianni, V., & Dorigo, M. (2006). *Self-organisation and communication in groups of simulated and physical robots*. Biological Cybernetics, 95, 213–231.
- Baldassarre, G., Trianni, V., Bonani, M., Mondada, F., Dorigo, M., & Nolfi, S. (2007). *Self-organized coordinated motion in groups of physically connected robots*.
- [Self-Organization and Artificial Life](https://direct.mit.edu/artl/article/26/3/391/93243/Self-Organization-and-Artificial-Life)
- [Continuous Thought Machines](https://arxiv.org/abs/2505.05522)

More detailed references may be moved to a separate `docs/references.md` file later.

---

## 🌌 Vision

The long-term vision is to create a **virtual lab of artificial life**:

- agents that evolve navigation strategies;
- agents that communicate and cooperate in swarms;
- environmental memory and signal fields;
- distributed search and planning;
- a bridge between AI, robotics, artificial life, and computational physics.

The first milestone is intentionally modest:

> make a single-agent benchmark reproducible, measurable, and reviewable.

Only after that should the project move toward RRT, ML-Agents, communication, and Swarm-RRT.

---

## Historical Note

The initial idea of the project was to make robots learn how to go through a maze using reinforcement learning and imitation learning.

In the first phase around 2020, the project explored car-like robots, parking-like tasks, and Rapidly-Exploring Random Trees. The early RRT idea was useful but too rigid from a machine-learning and artificial-life perspective.

The current direction is more ambitious: use maze worlds as a minimal laboratory for studying artificial life, collective intelligence, and distributed exploration.
