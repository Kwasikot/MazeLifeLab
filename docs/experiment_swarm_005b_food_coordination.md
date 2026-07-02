# EXP-SWARM-005b — Food Coordination Without Scent Gradients

## 1. Status and relationship to EXP-SWARM-005

**EXP-SWARM-005 (scent gradient trails)** is **paused** after the 2025-06-24 ablation:

- Verdict: `TRAILS NOT WORKING`
- Trails were active but reduced food return (~−15%)
- Root cause (code): return-path deposits + gradient following steer searchers toward the hive, not food

**EXP-SWARM-005b** replaces *scent gradient following* with **explicit food coordination**:

- agents share **discrete food-location signals** (coordinates + freshness);
- no steering along environmental odor gradients;
- ablation ladder: Independent → Hive bulletin → Local broadcast → (optional) Global oracle.

Journal reference: [`docs/journal/reports/2025-06-24_exp-swarm-005_trail_ablation.md`](journal/reports/2025-06-24_exp-swarm-005_trail_ablation.md)

Unity scene: `Assets/Scenes/EXP-SWARM-005.unity` (reuse; rename harness when implemented).

---

## 2. Research question

> Does **explicit food coordination** improve bee-hive foraging versus independent searchers, when scent **gradient following** is removed?

Sub-questions:

1. Is **hive bulletin board** (report at hive only) enough?
2. Does **local broadcast** (“telepathy”) outperform hive-only?
3. Does **global broadcast** (oracle upper bound) beat local — and by how much?

---

## 3. Communication modes (ablation ladder)

| Mode | ID | Behaviour |
|------|-----|-----------|
| **Independent** | `None` | No shared food signals. Same as foraging without trails. |
| **Hive bulletin** | `HiveBulletin` | On food **discovery** or **pickup**, agent writes/updates entry in hive-shared memory. Searchers read bulletin when near hive **or** always (two sub-variants; start with **always-readable** for simpler debugging). |
| **Local broadcast** | `LocalBroadcast` | On discovery/pickup, agent emits message to all agents within `broadcastRadius`. |
| **Global oracle** | `GlobalBroadcast` | Control upper bound: all agents instantly know freshest food leads. Not biologically plausible; measures max upside of perfect telepathy. |

**Out of scope for v1:**

- full shared maps;
- learned communication;
- LLM narration;
- scent deposits or gradient following (legacy code may remain behind a flag but **default off**).

---

## 4. Message model

```csharp
struct SwarmFoodSignal
{
    public int FoodSiteIndex;
    public Vector3 Position;
    public int StepCreated;
    public int StepExpires;      // TTL in simulation steps
    public SwarmFoodSignalKind Kind;  // Discovered, PickedUp, Depleted
    public int SenderAgentIndex;
}
```

**Rules:**

- `Discovered` — first agent enters `foodDiscoveryRadius` for a site with remaining units.
- `PickedUp` — agent collects one unit and switches to `ReturningHome` (optional stronger weight).
- `Depleted` — site empty; receivers should drop or down-rank that site.
- TTL default: **400 steps** (tunable); stale signals ignored for steering.
- At most **K=8** live signals per mode (drop oldest) to bound memory.

---

## 5. Steering integration

Remove or default-disable `ComputeScentBias` / `ISwarmScentField` gradient following.

Add optional **`ComputeCoordinationBias`** in `SwarmFlightAgent`:

```text
steering += coordinationBias * coordinationWeight   // only in Searching state
```

`TrySampleCoordinationDirection(agentIndex, position, velocity, out direction)`:

1. Collect valid signals (TTL, site still has food if checkable).
2. Score candidates: prefer fresher signals, nearer food, not overcrowded (optional penalty if many agents already targeting same site).
3. Return normalized XZ direction toward best target.

**Weights (initial defaults):**

| Parameter | Value |
|-----------|-------|
| `coordinationWeight` | 12 |
| `foragingWeight` | 18 |
| `explorationWeight` | 8 |
| `broadcastRadius` | 35 (local mode) |
| `hiveReadRadius` | 12 (hive bulletin: must be near hive to read, if using restricted variant) |
| `signalTtlSteps` | 400 |

Returning agents **do not** follow coordination signals (same rule as old scent).

---

## 6. Episode protocol

Same episode loop as EXP-SWARM-004/005:

- 96 agents, 7000 steps, 5 food sites × 24 units, seeds **42** and **137**.
- Scene: `Assets/Scenes/EXP-SWARM-005.unity`.
- Disable `autoStartOnPlay`; use batch harness.

**Batch order (per seed):**

1. `None`
2. `HiveBulletin`
3. `LocalBroadcast`
4. `GlobalBroadcast` (optional 4th condition; can be 3+1 for first gate)

Harness runs automatically; evaluator compares against **None**.

---

## 7. Metrics

Keep all EXP-SWARM-004 foraging metrics, add:

| Column | Definition |
|--------|------------|
| `coordination_mode` | `None`, `HiveBulletin`, `LocalBroadcast`, `GlobalBroadcast` |
| `signals_emitted` | Total messages created |
| `signals_received` | Sum of receive events (can exceed agents if broadcast) |
| `coordination_influenced_steps` | Searching steps where coordination bias changed steering |
| `stale_signal_rejects` | Signals ignored due to TTL or depleted site |
| `duplicate_target_agents` | Agents sharing same primary food target (crowding proxy) |

CSV: `results/experiment_swarm_005b_food_coordination.csv`

---

## 8. Pass / fail criteria (first gate)

Compare each treatment mean vs **None** on seeds 42 + 137 (2 runs per mode minimum).

**Pass** if at least **2 of 3** primary metrics improve by:

- mean food returned ≥ **+5%**
- mean foraging efficiency ≥ **+5%**
- mean time to first food ≥ **+10%** (lower is better)

And:

- `coordination_influenced_steps` > 0 for non-None modes
- mean revisit ratio not worse by > **+5%**

**Fail** → try tuning `coordinationWeight`, TTL, broadcast radius; do not advance to EXP-SWARM-006.

**Inconclusive** → add seeds 200, 300.

---

## 9. Proposed code layout

```text
Assets/Scripts/Communication/
  SwarmFoodCoordinationMode.cs          # enum + extensions
  SwarmFoodSignal.cs                    # struct + kind enum
  SwarmFoodSignalBuffer.cs              # ring buffer, TTL expiry
  SwarmHiveBulletinBoard.cs             # hive-attached shared store
  SwarmFoodBroadcastBus.cs              # local/global dispatch
  ISwarmFoodCoordinationField.cs        # read interface for steering

Assets/Scripts/Agents/
  SwarmFlightAgent.cs                   # + CoordinationField, CoordinationWeight, ComputeCoordinationBias
  SwarmFlightSettings.cs                # (fields on settings struct)

Assets/Scripts/Experiments/
  ExperimentSwarm004Runner.cs           # emit signals on discover/pickup; mode switch; deprecate scent default off
  ExperimentSwarm004MetricsLogger.cs    # + coordination columns
  SwarmFoodCoordinationEvaluator.cs     # pass/fail vs None
  ExperimentSwarmCoordinationAblationHarness.cs   # rename/generalize trail harness
  ExperimentSwarmCoordinationAblationHarnessEditor.cs
```

### Runner changes (`ExperimentSwarm004Runner`)

```text
[SerializeField] SwarmFoodCoordinationMode coordinationMode = None;
[SerializeField] bool enableScentTrails = false;   // legacy, default false

On food discovered  -> Emit(Discovered)
On food picked up   -> Emit(PickedUp)
On site depleted    -> Emit(Depleted)
FixedUpdate order:
  agent steps -> UpdateForagingState -> Emit from state transitions -> ExpireSignals
```

### Harness migration

Rename `ExperimentSwarmTrailAblationHarness` → `ExperimentSwarmCoordinationAblationHarness` (or keep old class as thin wrapper calling new harness).

Batch matrix:

```text
seeds [42, 137] × modes [None, HiveBulletin, LocalBroadcast]  = 6 runs
optional + GlobalBroadcast per seed → 8 runs
```

---

## 10. Implementation phases

### Phase 1 — Minimal hive bulletin (recommended first)

- `SwarmHiveBulletinBoard` only
- `coordinationMode` enum: `None` | `HiveBulletin`
- No broadcast bus yet
- Harness: 4 runs (2 seeds × 2 modes)
- **Goal:** prove explicit coordinates beat gradient trails on same seeds

### Phase 2 — Local broadcast (“telepathy”)

- `SwarmFoodBroadcastBus` with `broadcastRadius`
- Add `LocalBroadcast` to harness
- **Goal:** test if in-flight peer signals beat hive-only

### Phase 3 — Oracle + journal

- `GlobalBroadcast` as ceiling
- Full journal entry `YYYY-MM-DD_exp-swarm-005b_coordination_ablation.md`
- Update `decision_log.md` if scent gradients formally abandoned

---

## 11. Scientific cautions

- **Global broadcast** is an oracle, not a biological claim.
- **Local broadcast** must be compared to **None** and **HiveBulletin**; otherwise “telepathy works” is untestable.
- High food return with global mode but not local → coordination helps only with unrealistic sensing.
- Crowding at one food site can hurt totals; log `duplicate_target_agents`.
- Do not conflate coordination with intelligence or Theory of Mind; this is **explicit signalling**.

---

## 12. Recommended next coding task

1. ~~Add `SwarmFoodCoordinationMode` + `SwarmHiveBulletinBoard`.~~ **Done** (`SwarmFoodCoordinationSystem`)
2. ~~Wire `ExperimentSwarm004Runner` emits on discover/pickup; default `enableScentTrails = false`.~~ **Done**
3. ~~Add `ComputeCoordinationBias` to `SwarmFlightAgent`.~~ **Done**
4. ~~Add `ExperimentSwarmCoordinationAblationHarness` (seeds 42, 137; all four modes).~~ **Done** — on `EXP-SWARM-005.unity`; legacy trail harness disabled
5. ~~Play-mode smoke test → Run All Tests → journal entry~~ **Done** (2026-06-24) — verdict **COORDINATION WORKING**; see `docs/journal/reports/2026-06-24_exp-swarm-005b_coordination_ablation.md`
6. **Next:** CSV logging + seeds 200/300; tune `LocalBroadcast` vs HiveBulletin

---

## 13. Related documents

- [`docs/swarm_flight_research_agenda.md`](swarm_flight_research_agenda.md)
- [`docs/experiment_005_multi_agent_signals.md`](experiment_005_multi_agent_signals.md) — maze stigmergy / FrontierClaim (separate track)
- [`docs/decision_log.md`](decision_log.md)
- [`docs/project_memory.md`](project_memory.md)
