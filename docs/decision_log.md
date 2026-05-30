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
