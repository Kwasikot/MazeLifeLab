# MazeLifeLab Decision Log

This file records important research and engineering decisions.

It is not a diary and not a changelog.

Use it to preserve **why** a decision was made, not merely what changed.

---

## Format

Use this format for future entries:

```markdown
## YYYY-MM-DD — Decision Title

### Context
What problem or uncertainty existed?

### Decision
What was decided?

### Reasoning
Why was this option chosen?

### Alternatives Considered
What other options were considered?

### Risks / Limitations
What could go wrong?

### Related Files
- `docs/...`
- `Assets/...`
```

---

## 2026-06-06 — Frame intelligence as dynamic stability for future stages

### Context

MazeLifeLab is inspired by artificial life, cybernetics, procedural environments, reinforcement learning, swarm intelligence, and distributed search. The project needs a careful long-term concept of intelligence that is useful for experiments but does not create premature claims about consciousness, human-like minds, or real Theory of Mind.

### Decision

Document a long-term operational framing of intelligence as dynamic stability: an agent becomes more capable when it can maintain viable internal and behavioural states through perception, prediction, action, memory, and adaptation.

This framing is documentation only. It does not change the current active implementation focus, which remains `EXP-001 — Single-Agent Navigation Benchmark`.

### Reasoning

The dynamic-stability framing connects several future research directions in a disciplined way:

- agent self-state can be treated as measurable internal or behavioural variables;
- partial observability can be described as an agent-relative Umwelt;
- intelligence can be measured through prediction-action loops rather than vague cognitive labels;
- navigation can later be studied through viability maintenance, not only explicit reward;
- randomness can be treated as an adaptive exploration primitive;
- advanced agents can be built through tested lower-level capabilities rather than opaque monolithic cognitive classes.

This supports the project's artificial-life direction while preserving scientific caution.

### Alternatives Considered

- Keep intelligence undefined until much later.
- Treat intelligence primarily as reinforcement-learning reward maximisation.
- Introduce broad biological or consciousness terminology immediately.
- Document the framing now while explicitly preventing implementation scope creep.

The selected option is to document the framing now as a future research note and keep all implementation gated by experiments, metrics, and baselines.

### Risks / Limitations

- The framing may sound more ambitious than the current codebase supports.
- Terms such as viability, self-state, Umwelt, and dynamic stability require operational metrics before implementation.
- Cursor or future agents may try to implement advanced features too early unless the staged roadmap is followed.
- The project must avoid claiming consciousness, real self-awareness, or human-like understanding.

### Related Files

- `docs/intelligence_as_dynamic_stability.md`
- `docs/theory_of_mind_late_stage.md`
- `docs/research_agenda.md`
- `docs/project_memory.md`
- `.cursor/rules/project_memory_and_changelog.mdc`
- `.cursor/rules/research_decision_gates.mdc`

---

## 2026-06-06 — Postpone Theory of Mind to late-stage experiments

### Context

MazeLifeLab has a long-term research interest in multi-agent intelligence, social cognition, sense of self, other-agent modelling, communication, environmental memory, and collective search. These ideas are scientifically interesting, but they can easily create scope creep if introduced before the project has reproducible single-agent benchmarks, RRT comparisons, multi-agent baselines, communication metrics, and environmental memory mechanisms.

### Decision

Treat Theory of Mind as a late-stage research direction only. It should not be implemented during `EXP-001 — Single-Agent Navigation Benchmark` or `EXP-002 — RRT vs Baselines`.

The project may document a future ladder from reactive agents to self-state, agent-local memory, other-agent state estimation, knowledge-state estimation, intention estimation, and minimal operational Theory of Mind, but this ladder remains documentation until the prerequisites exist.

### Reasoning

Theory of Mind-like mechanisms are only scientifically meaningful in MazeLifeLab if they are operationalised through measurable behaviour: prediction of other agents, estimation of knowledge asymmetry, selective communication, reduced redundant exploration, improved division of labour, and better collective search under partial observability.

Introducing grand cognitive labels too early would weaken the research discipline of the project. The safer approach is to use cautious terms such as `AgentSelfState`, `AgentMemory`, `OtherAgentTracker`, `SocialStateEstimator`, `SharedMap`, and `GroupMemory`.

### Alternatives Considered

- Start implementing a `TheoryOfMindAgent` immediately.
- Add self-awareness or consciousness terminology to the agent architecture.
- Ignore Theory of Mind entirely until much later.
- Document the direction now but explicitly postpone implementation.

The selected option is to document the direction now, while adding guardrails against premature implementation.

### Risks / Limitations

- The terminology may still attract scope creep.
- Future agents may appear more cognitively sophisticated than they really are if metrics are weak.
- Knowledge-state and intention-estimation metrics will require careful definitions.
- The project must avoid claiming human-like psychology, consciousness, or real Theory of Mind.

### Related Files

- `docs/theory_of_mind_late_stage.md`
- `docs/research_agenda.md`
- `docs/project_memory.md`
- `.cursor/rules/project_memory_and_changelog.mdc`
- `.cursor/rules/research_decision_gates.mdc`

---

## 2026-05-30 — Treat MazeLifeLab as a research platform

### Context

MazeLifeLab began as a Unity prototype connected to maze navigation, RRT, and agent behaviour. The project risks becoming a collection of interesting but disconnected features: RRT, ML-Agents, artificial life, multi-agent communication, and swarm intelligence.

### Decision

Treat MazeLifeLab as a reproducible research platform rather than a loose Unity prototype.

### Reasoning

This makes the project easier to develop, review, and extend. Each major feature should support a research question, experiment, metric, or baseline.

### Alternatives Considered

- Continue adding features opportunistically.
- Focus immediately on ML-Agents training.
- Focus immediately on multi-agent communication.

### Risks / Limitations

- More documentation overhead.
- Slower initial coding speed.
- Requires discipline to keep documents updated.

### Related Files

- `docs/research_agenda.md`
- `docs/project_memory.md`
- `.cursor/rules/project_memory_and_changelog.mdc`

---

## 2026-05-30 — Start with EXP-001 before RRT and ML-Agents

### Context

The project has long-term ambitions involving RRT, reinforcement learning, multi-agent communication, artificial life, and Swarm-RRT. However, those directions require a stable experimental base.

### Decision

Begin with `EXP-001 — Single-Agent Navigation Benchmark`.

### Reasoning

A reproducible single-agent benchmark provides the foundation for all later experiments. It establishes deterministic seeds, episode lifecycle, baseline agents, metrics, and CSV output.

### Alternatives Considered

- Start directly with RRT.
- Start directly with ML-Agents.
- Start directly with multi-agent communication.

### Risks / Limitations

- The first experiment may feel too simple.
- It may delay work on the more exciting artificial life direction.

### Related Files

- `docs/experiment_001_single_agent.md`
- `docs/schedule_exp_001.md`
- `docs/project_memory.md`

---

## 2026-05-30 — Use a compact project memory file

### Context

Cursor and future reviewers need persistent context, but a large memory file can become noisy and hard to trust.

### Decision

Create `docs/project_memory.md` as a compact project-status file.

### Reasoning

The project memory should answer: what experiment is active, what is in scope, what is out of scope, what metrics matter, and what the next task is.

### Alternatives Considered

- Rely only on Cursor conversation context.
- Put all details into README.
- Use only CHANGELOG.md.

### Risks / Limitations

- If updated too often, it becomes a diary.
- If not updated after major changes, it becomes stale.

### Related Files

- `docs/project_memory.md`
- `.cursor/rules/project_memory_and_changelog.mdc`

---

## 2026-05-30 — Keep EXP-002 as a future protocol, not current implementation

### Context

RRT is central to the project’s originality, but implementing or refactoring it before EXP-001 is stable may create scope creep.

### Decision

Create `docs/experiment_002_rrt_vs_baselines.md` as a future experiment protocol, but keep EXP-001 as the active implementation focus.

### Reasoning

This preserves the long-term direction while preventing premature implementation complexity.

### Alternatives Considered

- Start RRT refactoring immediately.
- Leave EXP-002 undocumented until later.

### Risks / Limitations

- Cursor may still try to implement EXP-002 early unless project memory and rules are followed.

### Related Files

- `docs/experiment_002_rrt_vs_baselines.md`
- `docs/project_memory.md`
- `.cursor/rules/project_memory_and_changelog.mdc`
