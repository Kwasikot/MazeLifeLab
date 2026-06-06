# Theory of Mind as a Late-Stage Research Direction

This document defines how MazeLifeLab should treat future work related to Theory of Mind, sense of self, social cognition, and multi-agent intelligence.

The main purpose is to preserve the long-term research idea while preventing premature implementation during the current benchmark stages.

---

## 1. Status

Theory of Mind is a **late-stage research layer**.

It is **not** part of:

```text
EXP-001 — Single-Agent Navigation Benchmark
EXP-002 — RRT vs Baselines
```

It should not be implemented while the project is still establishing:

- deterministic single-agent benchmarks;
- RandomWalk and WallFollower baselines;
- metrics logging;
- CSV output;
- RRT comparison protocols;
- multi-agent baselines;
- communication metrics;
- environmental memory mechanisms.

The current active implementation focus remains:

```text
EXP-001 — Single-Agent Navigation Benchmark
```

---

## 2. Scientific Framing

In MazeLifeLab, Theory of Mind should be interpreted cautiously and operationally.

The project should not claim that artificial agents possess consciousness, self-awareness, or human-like mental states.

Preferred language:

```text
self-state
agent-local memory
other-agent state estimation
knowledge estimate
social belief state
group memory
distributed knowledge estimation
```

Avoid premature language:

```text
consciousness
self-awareness
real Theory of Mind
phenomenal self
human-like psychology
```

The scientifically safer interpretation is:

> Agents may maintain and update limited estimates about themselves, other agents, and the distribution of knowledge inside a collective search process.

---

## 3. Development Ladder

The following ladder describes possible conceptual stages. These are not current implementation tasks.

### Level 0 — Reactive Agent

The agent reacts directly to local sensor input.

Possible properties:

- no persistent internal state;
- no memory of previous locations;
- no model of other agents;
- behaviour is driven by current observations only.

Example:

```text
RandomWalkAgent
WallFollowerAgent
```

### Level 1 — Minimal Self-State

The agent tracks a small amount of internal state relevant to its own behaviour.

Possible properties:

- current mode;
- last action;
- recent collision flag;
- local progress estimate;
- fatigue, cost, or resource counter if introduced later.

Preferred term:

```text
AgentSelfState
```

### Level 2 — Egocentric Memory

The agent stores an agent-local memory of its own exploration history.

Possible properties:

- recently visited cells;
- local trajectory memory;
- dead-end memory;
- local explored/unexplored estimate.

Preferred terms:

```text
AgentMemory
agent-local memory
egocentric map
```

### Level 3 — Other-Agent Detection

The agent can detect nearby agents as external entities.

Possible properties:

- nearby agent position;
- nearby agent direction;
- collision or proximity events;
- basic identity token if needed for metrics.

This is not yet social cognition. It is only perception of other agents.

Preferred terms:

```text
other-agent detection
nearby agent observation
```

### Level 4 — Social State Estimation

The agent estimates simple state variables of other agents.

Possible properties:

- other agent location estimate;
- other agent movement direction;
- whether another agent appears stuck, exploring, returning, or signalling;
- local estimate of which areas another agent has likely visited.

Preferred terms:

```text
OtherAgentTracker
SocialStateEstimator
social belief state
```

### Level 5 — Knowledge-State Estimation

The agent estimates what another agent or group probably knows about the maze.

Possible properties:

- which regions another agent has likely observed;
- whether another agent probably knows a path, dead end, or goal location;
- whether information is already present in a shared map or group memory.

Preferred terms:

```text
knowledge estimate
group memory
SharedMap
GroupMemory
```

### Level 6 — Goal / Intention Estimation

The agent estimates what another agent is likely trying to do.

Possible properties:

- exploration vs return-to-goal;
- help-seeking vs independent search;
- signalling vs moving silently;
- scouting vs exploiting a known path.

Preferred terms:

```text
intention estimate
goal estimate
behavioural mode estimate
```

This should be measured behaviourally, not described as mind reading.

### Level 7 — Minimal Theory of Mind

The agent uses estimates of other agents' knowledge states, goals, and information asymmetries to choose actions or communication.

Possible properties:

- selective signalling based on what another agent likely does not know;
- avoiding redundant exploration because another agent likely already covered a region;
- helping another agent reach useful information;
- deciding whether communication is worth its cost.

This is still a **minimal operational model**, not human-like Theory of Mind.

Preferred description:

> Minimal Theory of Mind in MazeLifeLab means measurable use of other-agent state, knowledge-state, and goal estimates under partial observability.

---

## 4. Possible Future Experiments

These experiments should be considered only after multi-agent baselines, communication systems, and communication metrics already exist.

### EXP-007 — Social State Estimation

Question:

> Can an agent improve collective exploration by estimating the local state of other agents?

Possible comparison:

```text
MultiAgent_NoCommunication
MultiAgent_WithOtherAgentDetection
MultiAgent_WithSocialStateEstimator
```

Possible metrics:

- redundant_exploration_percent;
- other_agent_prediction_accuracy;
- division_of_labour_index;
- coverage_percent;
- time_to_goal.

### EXP-008 — Knowledge Asymmetry and Selective Communication

Question:

> Can agents communicate selectively when they estimate that another agent lacks useful information?

Possible comparison:

```text
Broadcast_AllSignals
Random_Signals
Selective_Signals_ByKnowledgeEstimate
NoCommunication
```

Possible metrics:

- knowledge_estimation_accuracy;
- useful_signal_ratio;
- communication_cost;
- communication_efficiency;
- redundant_exploration_percent.

### EXP-009 — Minimal Theory of Mind Under Partial Observability

Question:

> Can agents use estimates of other agents' knowledge states and goals to improve collective search under partial observability?

Possible comparison:

```text
SharedMap_Only
OtherAgentTracker
SocialStateEstimator
KnowledgeStateEstimator
MinimalToM_Operational
```

Possible metrics:

- knowledge_estimation_accuracy;
- intention_estimation_accuracy;
- other_agent_prediction_accuracy;
- false_signal_recovery_time;
- communication_efficiency;
- division_of_labour_index.

---

## 5. Example Tasks

### Hidden Region Task

Some maze regions are discovered by one agent before others.

Research question:

> Can agents avoid redundant exploration by estimating which regions are already known to others or to group memory?

### False Trail Task

A misleading signal or obsolete marker points toward a dead end or no-longer-useful route.

Research question:

> Can agents recover from false, stale, or misleading information?

### Rescue / Help Task

One agent reaches a useful region or goal-related clue, while another agent is stuck or inefficiently exploring.

Research question:

> Can one agent emit a useful signal or move in a way that helps another agent reduce search cost?

### Division of Labour Task

Agents must distribute exploration across different maze regions.

Research question:

> Can local state estimation and communication reduce duplicated work and increase collective coverage?

### Communication Cost Task

Signals have a measurable cost.

Research question:

> Does communication become more selective when signalling is costly?

---

## 6. Candidate Metrics

Future social-cognition experiments should use measurable behavioural metrics.

Candidate metrics:

```text
redundant_exploration_percent
other_agent_prediction_accuracy
knowledge_estimation_accuracy
intention_estimation_accuracy
useful_signal_ratio
communication_cost
communication_efficiency
division_of_labour_index
false_signal_recovery_time
```

Possible interpretations:

- `redundant_exploration_percent` — how much work is duplicated across agents.
- `other_agent_prediction_accuracy` — how well an agent predicts another agent's future position, state, or action class.
- `knowledge_estimation_accuracy` — how well an agent estimates which regions or facts another agent likely knows.
- `intention_estimation_accuracy` — how well an agent estimates another agent's behavioural mode or goal-directed pattern.
- `useful_signal_ratio` — fraction of emitted signals that measurably improve another agent's behaviour.
- `communication_cost` — cost paid for signalling.
- `communication_efficiency` — benefit per unit of communication cost.
- `division_of_labour_index` — degree to which agents cover complementary rather than redundant regions.
- `false_signal_recovery_time` — time needed to recover from misleading or stale information.

All definitions should be formalised before implementation.

---

## 7. Relation to Swarm-RRT

Swarm-RRT is the idea that many agents may collectively approximate a search tree through local exploration, communication, and environmental memory.

In that context, Theory of Mind should not mean human-like psychology.

It should mean:

> distributed knowledge estimation inside collective search.

A Swarm-RRT system may need agents to estimate:

- which branches of the distributed search have already been explored;
- which agents probably know about useful corridors or dead ends;
- which local signals are stale, useful, or redundant;
- when communication improves search-tree expansion;
- when agents should split, follow, return, or signal.

This connects Theory of Mind-like mechanisms to search efficiency, not to claims about consciousness.

---

## 8. Guardrails

Do not implement Theory of Mind before:

- EXP-001 is reproducible;
- EXP-002 has clarified RRT vs baseline assumptions;
- multi-agent baselines exist;
- communication mechanisms exist;
- communication cost and usefulness metrics exist;
- environmental memory or shared-map mechanisms are measurable.

Do not create early classes such as:

```text
TheoryOfMindAgent
ConsciousAgent
SelfAwareAgent
```

Acceptable future names:

```text
AgentSelfState
AgentMemory
OtherAgentTracker
SocialStateEstimator
SharedMap
GroupMemory
```

Implementation should start from measurable state estimation, not from grand cognitive labels.

---

## 9. Practical Rule For Cursor

When Cursor or another coding agent proposes anything related to Theory of Mind, selfhood, or social cognition, it must answer:

```text
Which future experiment does this support?
Which baseline will it be compared against?
Which metric will measure improvement?
Which existing prerequisite is already implemented?
Why is this not scope creep for EXP-001 or EXP-002?
```

If these questions cannot be answered, postpone the feature and keep it as documentation only.

---

## 10. Summary

Theory of Mind is a valuable late-stage research direction for MazeLifeLab, but only if treated as an operational, measurable layer of multi-agent state estimation.

The correct order is:

```text
single-agent benchmark
-> RRT and planning baselines
-> local partial-observability planning
-> multi-agent baselines
-> communication metrics
-> environmental / group memory
-> social state estimation
-> knowledge-state estimation
-> minimal operational Theory of Mind
```

This preserves the ambitious vision while keeping the current implementation disciplined and scientifically reviewable.
