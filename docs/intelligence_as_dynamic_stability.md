# Intelligence as Dynamic Stability

This document records a long-term research framing for MazeLifeLab inspired by ideas from artificial life, cybernetics, computational biology, and theories of intelligence as adaptive modelling.

It is a **research note**, not an implementation task list.

The current active implementation focus remains:

```text
EXP-001 — Single-Agent Navigation Benchmark
```

The ideas below should not be implemented before the project has reproducible baselines, metrics, RRT comparisons, multi-agent baselines, communication metrics, and environmental or group memory mechanisms.

---

## 1. Core Framing

MazeLifeLab can treat intelligence operationally as:

> the ability of an agent to maintain dynamically viable states through perception, prediction, action, memory, and adaptation.

In this framing, an intelligent agent is not defined by human-like thought. It is defined by measurable abilities:

- sensing relevant features of the environment;
- estimating its own internal or behavioural state;
- predicting consequences of possible actions;
- avoiding states that reduce viability;
- using memory to improve future behaviour;
- adapting behaviour under partial observability;
- optionally coordinating with other agents when social information becomes useful.

This fits MazeLifeLab because the project studies agents in procedural maze environments where local perception, uncertainty, action, memory, and collective search can be measured.

---

## 2. Dynamic Stability

A dynamically stable pattern persists not because it is static, but because it continuously regenerates, repairs, or maintains itself through activity.

For MazeLifeLab, this suggests that future agents may be evaluated not only by whether they reach a goal, but also by whether they maintain useful operating conditions while exploring.

Possible future viability variables:

```text
energy
stuckness
local_progress
confidence
memory_load
signal_budget
risk_exposure
prediction_error
```

Possible future metrics:

```text
survival_time
energy_efficiency
stuck_recovery_rate
risk_avoidance_score
self_state_prediction_error
loop_escape_time
coverage_efficiency
```

These are not part of EXP-001 unless explicitly added later through a decision gate.

---

## 3. Agent Self-State

A future agent may maintain a limited self-state.

Preferred term:

```text
AgentSelfState
```

This should mean an operational internal state used for behaviour and metrics, not consciousness or phenomenal selfhood.

Possible future self-state fields:

```text
energy
recent_collision_count
recent_loop_score
stuckness
local_exploration_rate
confidence_in_local_map
last_known_progress_direction
communication_budget
```

A self-state layer could support questions such as:

> Does an agent that estimates its own stuckness recover from loops more effectively than a purely reactive baseline?

Possible future experiment:

```text
EXP-010 — Self-State Guided Navigation
```

Possible comparison:

```text
ReactiveAgent
MemoryAgent
SelfStateGuidedAgent
```

Possible metrics:

```text
stuck_recovery_rate
loop_escape_time
energy_per_success
coverage_efficiency
success_rate
```

---

## 4. Maze as Agent-Relative Umwelt

A maze is not only a geometric object. For each agent, it is also an agent-relative world of meaningful cues.

Possible interpretation:

```text
Maze geometry = objective environment
Agent sensors = accessible environment
Agent memory = remembered environment
Agent goals = relevant environment
Agent communication = socially extended environment
```

Different agents may therefore inhabit different operational Umwelten in the same maze.

Examples:

```text
ReactiveAgent Umwelt:
wall / empty / collision

MemoryAgent Umwelt:
wall / empty / visited / unvisited / dead end

SignalAgent Umwelt:
wall / empty / signal / signal age / signal reliability

SocialAgent Umwelt:
wall / empty / other agent / shared information / estimated knowledge asymmetry

SwarmRRT Agent Umwelt:
local free space / frontier / branch / explored region / stale signal
```

This framing is useful for partial observability experiments because it makes clear that the agent does not act on the full maze. It acts on a compressed, action-relevant model of the maze.

---

## 5. Prediction-Action Loop

A future agent can be described as modelling the relationship between:

```text
X = external observations
H = hidden or internal state
O = output actions
```

In MazeLifeLab:

```text
X = walls, corridors, visible goal cues, signals, local map, visible agents
H = energy, stuckness, confidence, memory state, behavioural mode
O = move, turn, wait, signal, mark, follow, explore, return
```

The agent becomes more capable when it improves its model of how observations, internal state, and actions jointly affect future success.

Possible future metrics:

```text
next_state_prediction_accuracy
collision_prediction_accuracy
goal_progress_prediction_accuracy
stuck_state_prediction_accuracy
signal_outcome_prediction_accuracy
```

This does not require claiming that the agent has human-like understanding. It only requires measurable prediction and action selection.

---

## 6. Viability-Based Navigation

A conventional reinforcement-learning setup often begins with an explicit reward function.

A viability-based framing instead asks:

> What states must the agent avoid or maintain in order to remain effective?

Possible negative states:

```text
being stuck in a loop
revisiting dead ends repeatedly
exhausting an energy budget
following stale signals
duplicating exploration already performed by others
losing useful local map information
```

Possible future experiment:

```text
EXP-011 — Viability-Based Navigation
```

Possible comparison:

```text
GoalRewardAgent
ViabilityAgent
HybridGoalViabilityAgent
```

Possible metrics:

```text
success_rate
energy_per_success
dead_end_revisit_rate
loop_escape_time
coverage_efficiency
risk_exposure
```

This could become relevant after EXP-001 and EXP-002 establish reproducible navigation baselines.

---

## 7. Randomness as an Exploration Primitive

Randomness should not be treated only as noise.

In MazeLifeLab, RandomWalk is a weak baseline, but randomness itself can be useful when applied adaptively.

Possible future idea:

```text
AdaptiveRandomnessAgent
```

The agent could increase randomness when:

```text
stuckness is high
local memory says the region is overexplored
signals are unreliable
prediction confidence is low
```

The agent could decrease randomness when:

```text
a reliable path exists
a strong goal cue is present
a shared map is trustworthy
a useful signal is recent and reliable
```

Possible metrics:

```text
adaptive_randomness_score
exploration_entropy
stuck_escape_rate
noise_vs_efficiency_tradeoff
```

This should be studied only after simple deterministic and stochastic baselines exist.

---

## 8. Symbiotic Composition of Agent Capabilities

Advanced agents should not be created from scratch as large, opaque systems.

Instead, new cognitive layers should be composed from lower-level capabilities that have already been tested.

Possible capability ladder:

```text
Reactive movement
+ Collision handling
+ Agent-local memory
+ Local map
+ Stuckness estimate
+ RRT planning
+ Signal emission
+ Signal interpretation
+ Shared map
+ Other-agent tracking
+ Social state estimation
+ Knowledge-state estimation
```

This resembles a symbiotic composition principle:

> higher-level intelligence should be assembled from stable lower-level modules whose individual contributions are measurable.

Guardrail:

```text
Do not create a complex social agent before its component capabilities have independent baselines and metrics.
```

---

## 9. Persistent Behavioural Patterns

In complex systems, some higher-level patterns are useful because they allow prediction.

MazeLifeLab can look for persistent patterns in trajectories, communication, and group behaviour.

Possible patterns:

```text
loop pattern
frontier expansion pattern
wall-following pattern
signal cascade
agent clustering pattern
group split pattern
rescue / help pattern
pheromone trail pattern
```

Possible future metrics:

```text
trajectory_pattern_frequency
loop_pattern_duration
frontier_expansion_rate
signal_cascade_length
persistent_group_structure_score
```

A behavioural pattern should be considered meaningful only if it improves prediction, explanation, or experimental comparison.

---

## 10. Relation to Theory of Mind

This framing supports the existing late-stage Theory of Mind roadmap.

Theory of Mind-like behaviour should not be added as a label. It should become relevant only when agents must model other agents as predictors under partial observability.

Prerequisites:

```text
multi-agent baselines
communication mechanisms
communication cost metrics
environmental or shared memory
knowledge asymmetry tasks
other-agent prediction metrics
```

Possible future components:

```text
OtherAgentTracker
SocialStateEstimator
KnowledgeStateEstimator
IntentionEstimator
```

These should remain late-stage concepts and must not enter EXP-001 or EXP-002 implementation.

---

## 11. Candidate Future Experiments

The following experiment ideas are speculative and should be placed after the current roadmap unless explicitly promoted by a decision gate.

```text
EXP-010 — Self-State Guided Navigation
EXP-011 — Viability-Based Navigation
EXP-012 — Evolution of Local Navigation Strategies
EXP-013 — Adaptive Randomness in Partial Observability
EXP-014 — Agent-Relative Umwelt and Sensor Design
EXP-015 — Persistent Behavioural Patterns in Collective Exploration
```

These are not current implementation tasks.

---

## 12. Scientific Guardrails

Use cautious operational language.

Preferred terms:

```text
self-state
viability
homeostatic variable
prediction-action loop
agent-relative Umwelt
partial observability
adaptive exploration
dynamic stability
behavioural pattern
state estimation
```

Avoid premature terms:

```text
consciousness
true self-awareness
real life emerged
artificial organisms are alive
agents understand themselves
human-like mind
```

Core rule:

```text
No cognitive label should be introduced without a task, baseline, and metric.
```

---

## 13. Summary

MazeLifeLab can treat intelligence as a staged operational phenomenon:

```text
local perception
-> compressed agent-relative world model
-> memory
-> prediction
-> viability maintenance
-> adaptive action
-> multi-agent coordination
-> distributed collective intelligence
```

The strongest long-term formulation is:

> MazeLifeLab studies how simple agents become more capable by transforming local perception into memory, memory into prediction, prediction into action, and action into collective adaptive structure.

This framing is ambitious, but it must remain subordinate to the current staged experimental discipline.

Current active implementation remains:

```text
EXP-001 — Single-Agent Navigation Benchmark
```
