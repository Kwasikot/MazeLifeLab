# MazeLifeLab Research Agenda

## 1. Working Title

**MazeLifeLab: A Minimal Artificial Life Laboratory for Studying Navigation, Communication, and Collective Intelligence in Procedural Maze Worlds**

MazeLifeLab should be developed not merely as a Unity maze-navigation demo, but as a small research platform for studying how simple embodied agents can learn, explore, communicate, and possibly develop collective intelligence in procedurally generated environments.

---

## 2. Central Research Question

**How can locally perceiving agents develop navigation, cooperation, and communication strategies in complex maze environments when they do not have access to a complete global map?**

In simpler terms:

> Can a group of simple agents, using only local perception and simple signals, solve exploration and navigation problems better than a single agent or a centrally planned algorithm?

---

## 3. Core Hypothesis

**Hypothesis:** In partially observable maze environments, simple local communication signals can improve collective exploration efficiency and may lead to primitive division of labour among agents.

A more ambitious version:

**Extended Hypothesis:** Persistent signals left in the environment can become a form of externalised collective memory, allowing a population of agents to build distributed navigation strategies without central control.

---

## 4. Research Identity of the Project

This project sits at the intersection of:

- Artificial life
- Multi-agent reinforcement learning
- Swarm robotics
- Emergent communication
- Path planning
- Procedural environments
- Embodied cognition
- Collective intelligence
- Distributed search
- Computational models of exploration

The maze should be understood as a **minimal problem space**. Agents navigating the maze are a model of agents exploring an unknown world, a scientific hypothesis space, an organisation searching for opportunities, or a swarm of robots exploring terrain.

---

## 5. Conceptual Shift

The project should move from:

> “An agent learns to pass through a maze.”

To:

> “A population of embodied agents develops search, communication, and memory strategies inside a procedurally generated problem space.”

This shift makes the project more scientifically interesting.

Single-agent maze solving is common. The stronger research direction is **collective exploration under partial observability**.

---

## 6. Minimal Research Programme

### Stage 1 — Stable Single-Agent Environment

Goal: create a reproducible maze-navigation task.

Requirements:

- Procedural maze generation with deterministic seed support.
- Start and goal positions.
- Agent movement.
- Collision detection.
- Local ray-based perception.
- Episode reset.
- Basic metrics logging.

Metrics:

- Success / failure.
- Steps to goal.
- Path length.
- Number of collisions.
- Time to goal.
- Explored area percentage.
- Maze seed.

Baselines:

- Random walk agent.
- Wall-following agent.
- A* with full map.
- RRT with full map or physics-based collision checking.

---

### Stage 2 — Planning vs Learning

Goal: compare classical planning and learned behaviour.

Algorithms to compare:

- Random walk.
- Wall-following.
- A*.
- RRT.
- Local RRT.
- Reinforcement learning agent.

Research question:

> Which approach generalises better to unseen mazes: explicit planning, learned policy, or a hybrid approach?

Important comparison dimensions:

- Performance in known mazes.
- Performance in unseen mazes.
- Robustness to noise.
- Dependence on complete map information.
- Computational cost.

---

### Stage 3 — Multi-Agent Exploration Without Communication

Goal: establish a multi-agent baseline.

Agents should explore the same maze, but without explicit communication.

Metrics:

- Collective coverage.
- Time to first goal discovery.
- Redundant exploration.
- Average distance between agents.
- Number of agents reaching the goal.
- Total collisions.

Research question:

> Does simply adding more agents improve exploration, or does lack of coordination create inefficiency?

---

### Stage 4 — Multi-Agent Exploration With Simple Signals

Goal: introduce local communication.

Possible signal types:

- Light pulse.
- Sound pulse.
- Pheromone-like field.
- Cell marker.
- Directional beacon.
- Danger/dead-end marker.

Important design rule:

Signals should not be hard-coded with too much semantic meaning at the beginning. Ideally, the meaning of signals should be learned or discovered through interaction.

Metrics:

- Improvement over no-communication baseline.
- Number of signals emitted.
- Communication cost.
- Signal usefulness.
- Signal redundancy.
- Emergent spatial patterns.
- Whether agents specialise into roles.

Research question:

> Can simple local communication improve collective maze exploration under partial observability?

---

### Stage 5 — Distributed Collective Memory

Goal: treat environmental signals as external memory.

Possible mechanisms:

- Pheromone field that decays over time.
- Persistent markers.
- Heatmap of explored cells.
- Agent-deposited signs.
- Shared but local memory fields.

Research question:

> Can the environment itself become a memory substrate for a population of agents?

This connects the project to stigmergy, ant colonies, swarm intelligence, and artificial life.

---

### Stage 6 — Hierarchical Agent Systems

Goal: test whether hierarchical organisation improves exploration.

Possible roles:

- Scout agents.
- Messenger agents.
- Navigator agents.
- Planner agent.
- Coordinator agent.

Research question:

> Under what conditions does hierarchy improve or harm collective intelligence?

This stage connects the project to organisation theory and distributed cognition.

---

## 7. Candidate Experiments

### Experiment 1 — Single-Agent Navigation Benchmark

**Purpose:** create a clean baseline task.

Setup:

- One agent.
- One procedural maze.
- One goal.
- Local perception only.

Compare:

- Random walk.
- Wall-following.
- RRT.
- RL policy.

Success criteria:

- The experiment can run repeatedly with fixed seeds.
- Results are written to CSV or JSON.
- At least three baseline methods can be compared.

---

### Experiment 2 — Generalisation to Unseen Mazes

**Purpose:** test whether agents learn maze-solving strategies or merely memorise layouts.

Setup:

- Train or tune on one set of maze seeds.
- Evaluate on unseen seeds.

Metrics:

- Success rate on training seeds.
- Success rate on test seeds.
- Performance drop between training and test.

---

### Experiment 3 — Communication Ablation

**Purpose:** test whether communication actually helps.

Conditions:

1. Multi-agent, no communication.
2. Multi-agent, random communication.
3. Multi-agent, learned or meaningful communication.
4. Multi-agent, pheromone-like environmental markers.

Key question:

> Does communication improve exploration beyond what would happen from simply increasing the number of agents?

---

### Experiment 4 — Communication Cost

**Purpose:** prevent trivial overcommunication.

Setup:

- Add a cost for emitting signals.
- Compare low-cost, medium-cost, and high-cost communication.

Research question:

> Does communication become more selective and meaningful when it has a cost?

---

### Experiment 5 — Collective Memory Decay

**Purpose:** test the effect of memory persistence.

Setup:

- Signals decay quickly.
- Signals decay slowly.
- Signals never decay.

Research question:

> What memory lifetime produces the best collective exploration?

---

## 8. Metrics to Implement Early

The project should include a small metrics logger as early as possible.

Recommended fields:

```text
episode_id
maze_seed
agent_count
algorithm
communication_enabled
signal_type
success
steps_to_goal
time_to_goal
path_length
collisions
coverage_percent
redundant_exploration_percent
signals_emitted
communication_cost
goal_discovered_by_agent_id
agents_reached_goal
```

Minimal acceptable first version:

```text
episode_id
maze_seed
algorithm
success
steps_to_goal
collisions
path_length
coverage_percent
```

---

## 9. Baseline Agents

Before implementing advanced learning, implement simple baselines.

### Random Walk Agent

Moves randomly while avoiding immediate collisions if possible.

Purpose:

- Establish the weakest baseline.

### Wall-Following Agent

Follows the left or right wall.

Purpose:

- Establish a classical maze-solving heuristic.

### A* Agent

Uses full maze graph knowledge.

Purpose:

- Establish an upper bound for classical planning with full observability.

### RRT Agent

Uses sampling-based planning.

Purpose:

- Compare sampling-based exploration with grid/graph-based planning.

### Local RRT Agent

Uses only local sensing or partially known map information.

Purpose:

- Bridge classical path planning and embodied partial observability.

### RL Agent

Learns a policy from rewards.

Purpose:

- Compare learned navigation with classical methods.

---

## 10. Possible Agent Observations

For ML-Agents or any custom learning system, observations may include:

- Ray distances to walls.
- Relative direction to goal, if goal information is allowed.
- Current velocity.
- Last action.
- Collision flag.
- Local pheromone/signal values.
- Nearby agent positions, if observable.
- Local explored/unexplored status.

Important distinction:

- **Full observability:** agent has access to the full maze map.
- **Partial observability:** agent only sees local sensor data.

Scientific value is higher under partial observability.

---

## 11. Possible Agent Actions

Basic navigation actions:

- Move forward.
- Move backward.
- Turn left.
- Turn right.
- Stop.

Communication actions:

- Emit no signal.
- Emit signal A.
- Emit signal B.
- Emit signal C.
- Drop marker.
- Increase/decrease local pheromone field.

Advanced actions:

- Follow signal gradient.
- Broadcast local discovery.
- Request help.
- Switch behavioural mode.

---

## 12. Reward Design Ideas

Basic rewards:

- Positive reward for reaching the goal.
- Small negative reward per step.
- Negative reward for collision.
- Positive reward for discovering new cells.

Multi-agent rewards:

- Shared reward when any agent reaches the goal.
- Individual reward for personal progress.
- Reward for reducing unexplored area.
- Reward for useful signalling.
- Penalty for excessive communication.

Important warning:

Reward design can easily create artificial behaviour that looks intelligent but is only exploiting the reward function. Always compare learned behaviour against simple baselines and visual inspection.

---

## 13. Divergent Analogies and Research Inspirations

### Ant Colonies

Agents leave pheromone-like signals. Collective navigation emerges from local rules.

### Slime Mold

The population gradually forms efficient paths through the environment without central planning.

### Neural Growth

RRT-like expansion resembles dendritic or axonal growth through space.

### Scientific Discovery

The maze is a hypothesis space. Agents are researchers. Signals are publications, citations, or shared discoveries.

### Organisations

Agents are employees or teams. The maze is a market or problem space. Signals are reports, dashboards, and institutional memory.

### Brain and Global Workspace

Local agents are cognitive modules. Communication is broadcast. A discovered path becomes a globally available plan.

### Active Inference

Agents do not only chase reward; they reduce uncertainty and prediction error about the environment.

---

## 14. Strongest Research Direction

The strongest near-term direction is:

> **Collective exploration under partial observability using simple local communication.**

This is stronger than ordinary maze solving because it connects to artificial life, swarm intelligence, emergent communication, and distributed cognition.

---

## 15. Suggested Paper-Like Contributions

### Contribution 1 — Platform

A Unity-based environment for studying maze navigation, local sensing, and multi-agent communication.

### Contribution 2 — Baseline Comparison

A systematic comparison of random walk, wall-following, A*, RRT, local RRT, and RL agents.

### Contribution 3 — Communication Study

An ablation study showing whether simple communication improves exploration efficiency.

### Contribution 4 — Distributed Memory

An investigation of pheromone-like environmental memory and its decay dynamics.

### Contribution 5 — Swarm-RRT

A distributed version of RRT where agents collectively approximate a search tree through local exploration and signalling.

---

## 16. Near-Term Engineering Roadmap

Recommended folder structure:

```text
Assets/
  Scripts/
    Maze/
      MazeCell.cs
      MazeWall.cs
      MazeGenerator.cs
      MazeGraph.cs
    Agents/
      AgentSensors.cs
      ManualAgentController.cs
      RandomWalkAgent.cs
      WallFollowerAgent.cs
    Planning/
      AStarPlanner.cs
      RrtPlanner.cs
      LocalRrtPlanner.cs
    Communication/
      SignalField.cs
      PheromoneGrid.cs
    Experiments/
      ExperimentRunner.cs
      MetricsLogger.cs
```

Do not over-engineer at the beginning. The first goal is reproducible experiments, not a perfect architecture.

---

## 17. Cursor Development Rules

When using Cursor or coding agents, use these rules:

1. Do not rewrite the whole project unless explicitly requested.
2. Prefer small, testable changes.
3. Keep experiments reproducible with deterministic seeds.
4. Add metrics before adding advanced AI.
5. Add baselines before training neural agents.
6. Keep single-agent and multi-agent experiments separate.
7. Avoid hard-coding intelligence into communication signals.
8. Preserve the distinction between full observability and partial observability.
9. Each new feature should support a research question.
10. Every experiment should write machine-readable results.

---

## 18. First Cursor Task Recommendation

The first concrete task for Cursor should be:

> Implement Experiment 1: Single-Agent Navigation Benchmark with deterministic maze seeds, a random-walk baseline, a wall-following baseline, and a metrics logger that writes CSV results.

Acceptance criteria:

- A maze can be generated from a seed.
- Agent has a start and goal position.
- Episode ends on success or timeout.
- Metrics are logged to CSV.
- Random-walk and wall-following baselines can be run.
- Results are reproducible across runs with the same seed.

---

## 19. Long-Term Vision

MazeLifeLab can become a small but conceptually rich artificial life laboratory.

The long-term goal is not only to solve mazes, but to study how simple agents can develop:

- exploration strategies;
- communication conventions;
- collective memory;
- role differentiation;
- distributed planning;
- swarm intelligence;
- primitive artificial culture.

In the most ambitious version, MazeLifeLab becomes a bridge between artificial life, reinforcement learning, robotics, organisational intelligence, and theories of collective cognition.
