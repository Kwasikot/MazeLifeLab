# MazeLifeLab Project Memory

This file is a compact persistent memory for Cursor, reviewers, and future development sessions.

Do not turn this file into a diary.

The purpose is to preserve research context between coding sessions.

---

# Current Active Experiment

Experiment ID:

```text
EXP-SWARM-005
```

Name:

```text
Scent / Pheromone Field (Trail Ablation)
```

Status:

```text
IN PROGRESS - first batch ablation completed 2025-06-24; verdict TRAILS NOT WORKING; iterate deposit model before next gate
```

Primary document:

```text
docs/swarm_flight_research_agenda.md
docs/journal/reports/2025-06-24_exp-swarm-005_trail_ablation.md
```

Historical / archived experiments:

```text
EXP-006 — Swarm-RRT / Distributed Search Trees
EXP-005 — Multi-Agent Exploration With Simple Signals
EXP-004 — Multi-Agent Exploration Without Communication
EXP-003 — Local RRT Under Partial Observability
EXP-001 — Single-Agent Navigation Benchmark
```

---

# Current Goal

Build the first bee-hive foraging experiment:

- many small flying agents;
- simple local rules;
- visible emergent swarm behavior;
- measurable baseline metrics.
- visible hive/home zone;
- deterministic food/resource sites;
- simple searching and returning-home agent states;
- food discovery, food return, and foraging-efficiency metrics.

The initial algorithm proposal is `Scented Active Boids`, beginning with separation, alignment, cohesion, random wander, and boundary avoidance.

---

# Implemented For EXP-SWARM-001

```text
SwarmFlightAgent (3D flying body with local Boids steering)
ExperimentSwarm001Runner (episode loop, deterministic spawn, maze-footprint flight arena, arena gizmo, runtime agent creation)
ExperimentSwarm001MetricsLogger (CSV metrics for baseline swarm motion)
ExperimentRunnerExclusivity support for the active swarm-flight runner
Lightweight visible-wall repulsion keeps flyers inside maze corridors before full obstacle-field experiments
```

# Pending Validation For EXP-SWARM-001

```text
Attach or enable ExperimentSwarm001Runner in a Unity scene
Run Play mode on seed 42
Confirm visible swarm motion without collapse or freezing
Inspect CSV output at results/experiment_swarm_001_boids.csv
Tune steering weights and population size after visual/metric review
```

---

# Implemented For EXP-SWARM-002

```text
ExperimentSwarm002Runner (maze-footprint obstacle field, deterministic wall-anchored obstacle slabs spanning multiple wall cells, visible obstacle markers)
ExperimentSwarm002MetricsLogger (CSV metrics for obstacle avoidance)
SwarmFlightAgent obstacle repulsion steering via local obstacle list, no physics raycasts
ExperimentRunnerExclusivity support for EXP-SWARM-002 over EXP-SWARM-001
```

# Pending Validation For EXP-SWARM-002

```text
Attach or enable ExperimentSwarm002Runner in a Unity scene
Run Play mode on seed 42 with default 24 agents and 16 wall-anchored obstacles
Confirm visible obstacle slabs sit on maze wall segments and swarm flow around them
Inspect CSV output at results/experiment_swarm_002_obstacles.csv
Tune obstacle density, radius, and avoidance weight after visual/metric review
```

---

# Implemented For EXP-SWARM-003

```text
ExperimentSwarm003Runner (PlainBoids vs FruitFlySearch exploration modes)
ExperimentSwarm003MetricsLogger (coverage, revisit, and novelty-bias CSV metrics)
SwarmFlightAgent optional exploration-field steering hook
XZ coverage memory for low-visited-space bias
Dense low-flying small-sphere default swarm and flat Scene-view coverage tiles for visible exploration
ExperimentRunnerExclusivity support for EXP-SWARM-003 over EXP-SWARM-002/001
MazeWallVisualizer renders raised 3D wall boxes so maze structure is visible to reviewers
```

# Pending Validation For EXP-SWARM-003

```text
Attach or enable ExperimentSwarm003Runner in a Unity scene
Run Play mode on seed 42 with default 96 small low-flying agents
Compare PlainBoids and FruitFlySearch modes
Confirm coverage grows and revisit ratio is logged
Inspect CSV output at results/experiment_swarm_003_exploration.csv
```

---

# Implemented For EXP-SWARM-004

```text
ExperimentSwarm004Runner (hive zone, food sites, searching / returning-home state loop)
ExperimentSwarm004MetricsLogger (food discovery, food return, and foraging-efficiency CSV metrics)
SwarmFlightAgent optional per-agent foraging-field steering hook
Visible blue hive marker, small red food spheres, and yellow agents (no color change when carrying)
ExperimentRunnerExclusivity support for EXP-SWARM-004 over EXP-SWARM-003/002/001
```

# Pending Validation For EXP-SWARM-004

```text
Play-mode foraging validated (agents find food and return to blue hive)
Optional CSV review at results/experiment_swarm_004_foraging.csv
```

---

# Journal Record For EXP-SWARM-005 (2025-06-24)

```text
First trail ablation batch: seeds 42 and 137, with vs without trails (4 runs)
Verdict: TRAILS NOT WORKING (0/2 primary metrics)
Without trails: mean food 37.0, efficiency 0.000055
With trails: mean food 31.5, efficiency 0.000047
Journal: docs/journal/reports/2025-06-24_exp-swarm-005_trail_ablation.md
HTML index: docs/journal/index.html
```

# Pending Validation For EXP-SWARM-005

```text
Revise trail deposit semantics (food-biased, weaker return-path deposit)
Lower scentWeight and rerun ExperimentSwarmTrailAblationHarness on seeds 42 and 137
Add follow-up journal entry after retry batch
Do not start EXP-SWARM-006 until trail ablation passes or is formally abandoned
```

---

# Implemented For EXP-SWARM-005 (Initial)

```text
SwarmScentField (decaying XZ scent grid)
Optional scent-trail steering on ExperimentSwarm004Runner (enableScentTrails)
ExperimentSwarmTrailAblationHarness (batch with/without trail runs + automated verdict panel)
SwarmTrailAblationEvaluator (concrete pass/fail thresholds on food return, efficiency, first food, revisit)
ExperimentSwarmTrailAblationHarnessEditor (Run All Tests button + statistics panel)
```

---

# Implemented For EXP-001 / Shared Infrastructure

```text
Deterministic maze seed (MazeSeedConfig, MazeGenerator)
Maze fingerprint logging for reproducibility checks
EXP-001 episode loop (start/goal, success/timeout, reset)
ManualAgentController for episode testing (WASD)
RandomWalkAgent baseline with collision and path-length tracking
WallFollowerAgent (right-hand and left-hand rules)
MetricsLogger CSV (single-agent episodes)
Unique-cell coverage tracking in Experiment001Runner
```

# Implemented For EXP-003

```text
MazeLocalDiscoveryMap (incremental_map sensing)
LocalRrtPlanner (grid RRT on discovered topology)
LocalRrtAgent (sense → plan → act loop)
Experiment001Algorithm.LocalRrt
Per-agent RRT visualizer (MazeRrtVisual_A{n})
```

# Implemented For EXP-004

```text
Experiment004Runner (2–32 agents, no communication)
MultiAgentStartLayout (corner/perimeter spawn)
MultiAgentMetricsLogger (team coverage, overlap, CSV)
Experiment004RunnerEditor
```

# Implemented For EXP-005

```text
MazeStigmergyField (decaying per-cell environmental signals)
MazeFrontierClaimField (temporary frontier responsibility claims per agent)
AgentStigmergyController (Trail / FrontierHint / RandomNoise / FrontierClaim deposit modes)
Experiment005Runner (extends multi-agent loop with communication ablation)
Experiment005MetricsLogger (signals_deposited, signal_influenced_steps, frontier claim metrics)
Experiment005RunnerEditor
LocalRrtAgent stigmergy read bias and claim-aware frontier selection (optional, no shared discovery map)
```

# Implemented For EXP-006

```text
SwarmRrtField (shared RRT edge deposits per episode)
SwarmRrtPlanner (graft foreign tree nodes into local RRT)
Experiment006SwarmMode (Independent / DepositOnly / SwarmRrt ablation)
Experiment006Runner (multi-agent loop with swarm overlay)
Experiment006MetricsLogger (swarm_edges_deposited, swarm_graft_nodes)
Experiment006RunnerEditor
LocalRrtAgent.ConfigureSwarmRrt (optional deposit + graft)
```

# Current Baselines

Implemented historical navigation baselines:

```text
RandomWalkAgent
WallFollowerAgent
LocalRrtAgent
ManualAgentController (testing only)
```

Planned:

```text
Independent random flying agents
Boids ablations with one steering force removed
```

Future:

```text
EXP-SWARM-002 — Obstacle Avoidance Field
EXP-SWARM-003 — Fruit-Fly Search / Random Exploration
EXP-SWARM-004 — Bee-Hive Foraging
EXP-SWARM-005 — Scent / Pheromone Field
EXP-SWARM-006 — Threat / Predator Response
EXP-SWARM-007 — Role Differentiation
```

---

# Required Metrics

Minimum historical navigation metrics:

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

Minimum planned swarm-flight metrics:

```text
episode_id
seed
agent_count
mean_speed
mean_neighbor_distance
cohesion_index
separation_violations
boundary_hits
coverage_volume_percent
termination_reason
```

---

# Current Assumptions

1. Reproducibility is more important than complexity.
2. Baselines must exist before ML agents.
3. Metrics must exist before advanced behaviour.
4. Swarm-flight baselines should start with simple local rules before communication, pheromones, predators, roles, or learning.
5. RRT is historical / archived work unless explicitly revived as a comparison baseline.

---

# Out of Scope For EXP-SWARM-001

Do not implement yet:

- ML-Agents training
- emergent communication
- pheromone systems
- hive foraging
- predator / threat systems
- hierarchical agents
- language systems
- role differentiation

These belong to future experiments.

---

# Long-Term Research Direction

MazeLifeLab should evolve toward:

```text
Flying agents
-> Local interaction rules
-> Swarm motion
-> Obstacle avoidance
-> Foraging
-> Scent / pheromone fields
-> Hive-like coordination
-> Role differentiation
-> Artificial life
```

The strongest long-term research theme is:

```text
Swarm-based flying artificial life using simple local rules, environmental cues, and measurable collective behavior.
```

---

# Open Questions

1. What minimal 3D world is enough to show stable swarm motion?
2. Which Boids metrics best distinguish useful swarm behavior from visual noise?
3. How should coverage volume be measured in a bounded flying arena?
4. What population size is stable on the current hardware?
5. Which steering-force ablations should define the first baseline?

---

# Next Recommended Task

Iterate EXP-SWARM-005 trail semantics and rerun ablation:

- food-biased deposits; reduce return-path deposit;
- lower `scentWeight` on `ExperimentSwarm004Runner`;
- rerun `ExperimentSwarmTrailAblationHarness` (seeds 42, 137);
- write follow-up entry in `docs/journal/reports/` and update `docs/journal/index.html`.

See journal report: `docs/journal/reports/2025-06-24_exp-swarm-005_trail_ablation.md`

---

# Update Instructions

Update this file only when:

- experiment status changes;
- major milestone is completed;
- assumptions change;
- metrics schema changes;
- active experiment changes;
- research direction changes.

Do not update it for every small code edit.
