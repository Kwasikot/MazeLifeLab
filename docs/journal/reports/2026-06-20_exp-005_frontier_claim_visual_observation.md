# EXP-005 — Frontier Claim Visual Observation

- **Date:** 2026-06-20
- **Experiment ID:** EXP-005
- **Status:** completed — qualitative visual observation (no quantitative batch)
- **Runner / harness:** `Experiment005Runner` on `MazeSystem` (no batch harness)
- **Seeds / conditions:** single Play-mode session; maze generated in scene (28×28 visible in Inspector)
- **Related code:** `Experiment005Runner`, `MazeFrontierClaimField`, `AgentStigmergyController`, `MazeStigmergyVisualizer`, `LocalRrtAgent`
- **Unity scene:** `MazeSystem` with `Experiment 005 Runner` enabled; other experiment runners disabled
- **Protocol doc:** `docs/experiment_005_multi_agent_signals.md`

---

## 1. Research Question

Does the **FrontierClaim** communication mode produce visibly distinct, slowly expanding exploration territories when multiple agents explore the same maze under partial observability?

This session was **not** a controlled ablation. It was a live visual check of whether claim-guided agents spread outward from stratified starts instead of collapsing into one crowded region.

---

## 2. Protocol Summary

1. Enable `Experiment005Runner` on `MazeSystem`; disable competing experiment runners.
2. Set **Communication Mode** to `FrontierClaim`, **Algorithm** to `LocalRrt`, **Agent Count** to 10.
3. Enter Play mode and watch the Game view over time.
4. **No CSV batch was run** for this session; evaluation was by eye only.
5. Capture one late-episode screenshot for the journal (`docs/journal/screenshots/scr2.png`).

---

## 3. Configuration

| Parameter | Value (from screenshot / Inspector) |
|-----------|-------------------------------------|
| Runner | `Experiment005Runner` |
| Communication mode | `FrontierClaim` |
| Algorithm | `LocalRrt` |
| Agent count | 10 |
| Maze size | 28 × 28 |
| Frontier claim radius | 10 cells |
| Frontier claim TTL | 36 steps |
| Frontier claim avoidance weight | 10 |
| Frontier claim expansion weight | 0.35 |
| Signal decay per step | 0.995 |
| Signal follow chance | 0.45 |
| Signal crowding penalty | 0.65 |
| Stigmergy field visible | yes (`showStigmergyField`) |

Each agent is drawn with a **distinct color** (cyan, magenta, yellow, orange, and others from the runner palette). Deposited smell and visited trails tint the maze floor in that agent’s color, so territorial spread is readable at a glance.

---

## 4. Results

### Quantitative metrics

**None recorded for this session.**

`Experiment005MetricsLogger` exists and can write `results/experiment_005_multi_agent_signals.csv`, but this run was not logged as a formal episode batch. The bottom status bar showed live counters (e.g. frontier conflicts, per-agent claim/trail hints) during Play mode; those values were **not** saved or averaged.

### Qualitative observations (visual, in dynamics)

![Game view — multi-agent frontier claim exploration](../screenshots/scr2.png)

*Figure: Late-episode Game view (2026-06-20). Agents leave color-coded trails and claim regions on a 28×28 maze. Source: `docs/journal/screenshots/scr2.png`*

Watching the simulation over time:

- Agents **explore slowly**, extending colored corridors and patches into unexplored green maze space rather than jumping instantly across the map.
- **Territories remain visually separable**: cyan, magenta, yellow, and orange regions occupy different branches and wings of the maze, consistent with per-agent responsibility claims and avoidance of peers’ claimed frontiers.
- Expansion is **gradual and outward**: trails lengthen along corridors; local blobs are less dominant than in early FrontierClaim prototypes (smell deposited once per cell, farthest-tier frontier preference).
- A **bright goal marker** sits near the maze centre; at the time of the screenshot, exploration had not yet converged on a single team completion event (typical for a mid/late visual check, not a timed success run).
- **Overlap and conflict** appear in places where corridors narrow or two agents approach the same junction — expected under claim radius overlap; the HUD reported non-zero frontier conflict activity during the session.

**Informal takeaway:** FrontierClaim produces **readable, color-coded territorial expansion** suitable for qualitative inspection. This does **not** prove coordination benefit over EXP-004 `None` or other EXP-005 modes without a seeded CSV comparison.

---

## 5. Interpretation

The run supports the **visual design goal** of EXP-005 FrontierClaim: multiple agents can co-explore one maze while leaving distinguishable, slowly growing regions. Claim refresh, stratified starts, and peer-avoidance appear to reduce the “single crowded blob near spawn” failure mode described in earlier project notes.

Because no metrics were collected, we cannot say whether coverage, overlap, time-to-first-goal, or claim efficiency improved versus baselines. The session is evidence that the **phenomenon is observable and worth measuring**, not that FrontierClaim **works** in a scientific sense.

---

## 6. Limitations

- Single anecdotal Play-mode session; no fixed seed documentation in this report.
- No CSV export, no repeat runs, no comparison to `None`, `Trail`, or EXP-004.
- Visual judgment only — easy to over-interpret pleasing color patterns as coordination.
- Screenshot is one time slice; dynamics mattered more than the still frame.
- Goal-reaching and team success were not validated quantitatively here.

---

## 7. Decision Gate

| Question | Answer |
|----------|--------|
| Reproducible? | Partially — same runner/settings should reproduce similar visuals; seed not recorded for this entry. |
| Metrics trustworthy? | N/A — no metrics captured. |
| Beats or clarifies baselines? | Unknown — visual only. |
| Failure modes understood? | Partially — junction overlap and slow expansion observed; not counted. |
| Next experiment justified? | Yes — follow with seeded CSV runs across communication modes. |

**Decision:** Treat as **visual validation only**. Proceed to quantitative EXP-005 ablation when ready; do not treat this screenshot as proof of coordination benefit.

---

## 8. Recommended Next Step

1. Run seeded episodes (e.g. seed 42) for `None`, `Trail`, and `FrontierClaim` with metrics logging enabled.
2. Log team coverage %, overlap %, `frontier_claims_created`, `claim_conflicts`, and `steps_to_first_goal`.
3. Capture paired screenshots at matched step counts for the journal.
4. Compare against EXP-004 on the same seed before advancing to EXP-006.

---

## 9. Artifacts

- Screenshot (2026-06-20): `docs/journal/screenshots/scr2.png`
- Markdown: this file
- HTML: [../index.html#exp-005-frontier-claim-visual-2026-06-20](../index.html#exp-005-frontier-claim-visual-2026-06-20)
- Optional future CSV: `results/experiment_005_multi_agent_signals.csv` (not produced in this session)
