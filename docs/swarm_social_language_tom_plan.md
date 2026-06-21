# Late-Stage Swarm Social Language and Theory-of-Mind Plan

This document describes a **future research direction** for MazeLifeLab: extending swarm-flight agents with social memory, language-mediated interaction, subjective-status reports, and operational Theory-of-Mind-like mechanisms.

It is inspired by the generative-agents architecture of memory, retrieval, reflection, planning, and dialogue, but adapted to flying artificial-life agents rather than human-like townspeople.

This is **not a current implementation task**.

Current active work should remain focused on the staged swarm-flight experiments, especially simple Boids, obstacle avoidance, exploration, foraging, scent, threat response, and role differentiation.

---

## 1. Research Motivation

MazeLifeLab currently studies many simple flying agents using local rules, swarm motion, foraging, environmental cues, and collective search.

A later stage may ask a more ambitious question:

```text
Can swarm agents develop believable social behaviour when they can remember events,
communicate in language, reflect on past interactions, and estimate what other agents know?
```

This should be framed cautiously as **operational social cognition**, not as real consciousness or real human-like Theory of Mind.

---

## 2. Core Idea

Imagine a future upgrade where birds, flies, or bee-like agents can express their internal state in simple English and can understand simple natural-language messages.

Example agent reports:

```text
I am searching near the north wall.
I found a fruit source but I am low on energy.
I saw agent B near the red obstacle.
I think agent C has not seen the predator yet.
I am returning to the hive because my carrying state is full.
```

These reports should be treated as **simulation-level introspective status messages**, not as evidence that the agent has conscious experience.

The scientific purpose is to test whether language-like summaries and social memory improve collective search, coordination, and robustness.

---

## 3. Relationship to Generative Agents

The Generative Agents paper suggests a useful architecture:

```text
perceive -> memory stream -> retrieve -> reflect -> plan -> act
```

MazeLifeLab can adapt this architecture in a smaller and more operational way:

```text
sense local world
-> store event memory
-> retrieve relevant events
-> optionally generate reflection
-> choose social or movement action
-> update swarm state
```

Key adapted components:

```text
Memory Stream       = natural-language or structured event log per agent
Retrieval           = recency + relevance + importance
Reflection          = periodic summary of social and environmental experience
Planning            = short-horizon intention or role plan
Dialogue            = short natural-language exchange between nearby agents
Interview Protocol  = evaluation questions to probe memory, plans, reactions, and social beliefs
```

---

## 4. Scientific Guardrail

Use operational language.

Preferred terms:

```text
social memory
subjective-status report
agent-local narrative state
language-mediated coordination
other-agent state estimation
knowledge-state estimation
belief-like variable
social prediction
```

Avoid premature terms:

```text
real consciousness
true subjective experience
real Theory of Mind
sentience
inner life
human-like selfhood
```

Allowed phrasing:

```text
The agent reports its simulated internal state in English.
The agent estimates what another agent has probably observed.
The agent uses memory and language to coordinate search.
```

Avoid phrasing:

```text
The bird is conscious.
The fly understands itself.
The agent truly knows what another agent believes.
```

---

## 5. Proposed Late-Stage Experiment Ladder

These experiments should come after the existing swarm-flight roadmap.

### EXP-SOCIAL-001 — Social Memory Stream

Purpose:

- give each agent a persistent event memory;
- record observations of resources, threats, other agents, messages, and own actions;
- keep memory small enough for real-time simulation.

Implementation sketch:

```text
SocialMemoryEntry
- id
- timestamp
- agent_id
- event_type
- subject
- object
- location
- importance_score
- natural_language_summary
```

Acceptance criteria:

- agent records meaningful events;
- memory can be inspected in Unity or exported to CSV/JSON;
- memory does not affect behaviour yet.

---

### EXP-SOCIAL-002 — Retrieval for Action Context

Purpose:

- retrieve memories relevant to the current situation;
- use recency, relevance, and importance scores;
- compare behaviour with and without retrieved memory.

Implementation sketch:

```text
SocialMemoryRetriever
score = recency + relevance + importance
```

Initial relevance can be keyword/tag based. Embeddings can be added later.

Acceptance criteria:

- relevant memories are retrieved for resource, predator, hive, and agent queries;
- irrelevant memories are usually ignored;
- retrieval can be tested without calling an LLM.

---

### EXP-SOCIAL-003 — Language Status Reports

Purpose:

- allow agents to produce short English summaries of their current state;
- expose these summaries to the user and optionally to nearby agents.

Example reports:

```text
I am scouting the eastern region.
I found food near the blue marker.
I am avoiding the predator.
I am following agent 12 because it recently found food.
```

Implementation sketch:

```text
IAgentNarrator
GenerateStatusReport(agent_state, local_observations, recent_memory)
```

This can start with template-based generation before any LLM is connected.

Acceptance criteria:

- reports match actual simulation state;
- no fabricated events;
- reports are short and inspectable.

---

### EXP-SOCIAL-004 — LLM Provider Abstraction

Purpose:

- add a provider interface so the project can use either a remote OpenRouter model or a local HTTP LLM server.

Implementation sketch:

```text
ILanguageModelProvider
- Generate(prompt, options)
- IsAvailable()
- EstimateCost(prompt)

OpenRouterLanguageModelProvider
LocalHttpLanguageModelProvider
MockLanguageModelProvider
TemplateLanguageModelProvider
```

The default for tests should be:

```text
MockLanguageModelProvider
TemplateLanguageModelProvider
```

Use OpenRouter or a local LLM only when explicitly enabled.

Local hardware note:

```text
The desktop RTX 5070 Ti 16 GB tier may be used for small or medium quantized local models.
Large models should be treated as optional and external to core simulation correctness.
```

Acceptance criteria:

- simulation runs without any paid API;
- LLM calls are logged;
- provider can be disabled;
- tests use deterministic mocks.

---

### EXP-SOCIAL-005 — Reflection and Social Summaries

Purpose:

- periodically summarize repeated observations into higher-level social memories.

Examples:

```text
Agent 7 often finds food near the west side.
Agent 4 tends to flee early when predators appear.
Agent 12 has not visited the hive recently.
The northern region is dangerous after predator sightings.
```

Implementation sketch:

```text
SocialReflectionEngine
- Trigger by importance threshold or time interval
- Retrieve recent important memories
- Generate summary/reflection
- Store reflection as memory
```

Acceptance criteria:

- reflections cite source memory IDs;
- reflections are not allowed without evidence;
- reflections can be inspected and deleted.

---

### EXP-SOCIAL-006 — Other-Agent State Estimation

Purpose:

- estimate what another agent is probably doing or knows based on observation history.

Possible variables:

```text
last_seen_location
estimated_goal
estimated_resource_knowledge
estimated_threat_knowledge
estimated_energy_state
estimated_role
```

Implementation sketch:

```text
OtherAgentModel
- target_agent_id
- last_seen_time
- last_seen_location
- estimated_role
- known_resource_sites_estimate
- known_threats_estimate
- confidence
```

Acceptance criteria:

- estimates are compared against ground truth;
- error is logged;
- no claim of real mind-reading.

---

### EXP-SOCIAL-007 — Minimal Operational Theory of Mind

Purpose:

- test whether estimating another agent's knowledge improves coordination.

Example task:

```text
Agent A sees food.
Agent B does not see food.
Agent A can either continue collecting or signal B.
A ToM-like agent should signal B when B probably lacks the information.
```

Metrics:

```text
knowledge_estimation_accuracy
useful_signal_ratio
redundant_signal_rate
coordination_success_rate
resource_return_rate
response_to_unseen_threat
```

Acceptance criteria:

- ToM-like agent outperforms memory-only and signal-only baselines;
- results are measured across seeds;
- language is treated as a coordination mechanism, not proof of consciousness.

---

## 6. Unity / Cursor Implementation Guidance

Cursor should implement this direction only when explicitly asked and only one layer at a time.

Recommended module structure:

```text
Assets/Scripts/SwarmSocial/
  SocialMemoryEntry.cs
  SocialMemoryStream.cs
  SocialMemoryRetriever.cs
  AgentNarrativeState.cs
  AgentStatusReporter.cs
  LanguageModel/
    ILanguageModelProvider.cs
    MockLanguageModelProvider.cs
    TemplateLanguageModelProvider.cs
    OpenRouterLanguageModelProvider.cs
    LocalHttpLanguageModelProvider.cs
  Reflection/
    SocialReflectionEngine.cs
  TheoryOfMind/
    OtherAgentModel.cs
    OtherAgentModelStore.cs
    KnowledgeStateEstimator.cs
```

Do not create files named:

```text
ConsciousAgent.cs
SentientBird.cs
TrueTheoryOfMind.cs
ArtificialSoul.cs
```

Prefer operational names:

```text
AgentNarrativeState
SocialMemoryStream
OtherAgentModel
KnowledgeStateEstimator
SocialReflectionEngine
```

---

## 7. Evaluation Protocol

Evaluation should include both behavioural metrics and interview-style probes.

Behavioural metrics:

```text
food_discovered
food_returned
time_to_first_food
foraging_efficiency
useful_signal_ratio
knowledge_estimation_accuracy
social_memory_retrieval_precision
coordination_success_rate
survival_rate_under_threat
```

Interview probes:

```text
Who did you see near the hive?
What food source do you remember?
Who probably knows about the predator?
What are you planning to do next?
Why did you signal agent 5?
What did agent 3 tell you?
```

The interview answers must be checked against memory and ground truth to detect hallucination.

---

## 8. Minimal First Step

The safest first implementation, when this direction becomes active, is not LLM integration.

The safest first step is:

```text
EXP-SOCIAL-001 — Social Memory Stream
```

Why:

- no API cost;
- deterministic;
- inspectable;
- useful even without language models;
- prepares the ground for retrieval, reflection, and later Theory-of-Mind-like experiments.

---

## 9. Summary

The long-term idea is not to make conscious birds or flies.

The scientific idea is:

> add memory, language-like reporting, reflection, and other-agent state estimation to swarm-flight agents, then test whether these mechanisms measurably improve collective search, foraging, threat response, and coordination.

This direction should remain late-stage until the simpler swarm experiments are reproducible and measured.
