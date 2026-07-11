# Roman Legion Formation Lab

Browser-based **agent-based modeling** demo: Roman soldiers **self-assemble** into formations using **local perception only** (no global telepathy).

Inspired by [David Shapiro’s Grok Build demo](https://www.youtube.com/watch?v=yTolO_nxJiU&t=1455s).

Master spec: [`../../docs/prompts/roman_legion_formation_sim_recreation.md`](../../docs/prompts/roman_legion_formation_sim_recreation.md)

## Design principles

- **No oracle by default** — agents see only nearby formation slots (`perceptionRadius`, max 32/48 seats).
- **Self-assembly** — template slots exist globally, but each agent claims a slot locally.
- **FSM colors** — Purple mustering → Blue forming → Green dressing.
- **Facing commands** — Face Left / Right / About Face rotate in place without reforming.
- **Slot loyalty** — displaced agents return toward claimed slot.

## Run

```bash
cd Games/RomanLegionFormation
npm install
npm run dev
```

Open http://localhost:5174 — click **Form Triple Line**.

## Controls

| Action | Effect |
|--------|--------|
| Form Triple Line / Rectangle / Square | Reform + self-assembly |
| Face Left / Right / About Face | Rotate in place |
| Reset / Scatter | Random scatter |
| V4 / V5 | Behavior profile (tightness vs stuck recovery) |
| Oracle mode | Debug: see all slots globally |

## Known failure modes (intentional)

- Twirling when perception is too low
- Picking occupied **believed spots** (gray dots)
- Some agents never reach final slot under V4

## V4 vs V5

| | V4 | V5 |
|---|----|----|
| Visible seats | 32 | 48 |
| Stuck recovery | weak | stronger + perception boost |
| Tightness | often tighter | may trade for recovery |

## Docs

- [`docs/DEV_NOTES.md`](docs/DEV_NOTES.md) — slot claiming algorithm
