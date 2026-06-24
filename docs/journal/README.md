# MazeLifeLab Experiment Journal

Durable record of completed experiment runs, batch tests, ablations, and validation milestones.

## Purpose

- Preserve **what was tested**, **numbers observed**, **verdict**, and **next steps**
- Complement `CHANGELOG.md` (code changes) and `docs/project_memory.md` (current focus)
- Provide **markdown source** and a **generated mobile-friendly HTML** index

## Source of truth

| File | Role |
|------|------|
| [reports/](reports/) | **Edit here** — one markdown file per experiment run |
| [journal.manifest.json](journal.manifest.json) | Report order, nav ids, verdict badges, Unity scene links |
| [journal.md](journal.md) | **Generated** — full journal assembled from reports |
| [index.html](index.html) | **Generated** — mobile-friendly HTML (rebuilt from markdown) |
| [index.md](index.md) | **Generated** — short index table |
| [screenshots/](screenshots/) | Local images (`screenshots/…` in HTML, `../screenshots/…` in reports) |

## Unity scenes

| Scene | Experiment | Notes |
|-------|------------|-------|
| `Assets/Scenes/EXP-005.unity` | EXP-005 | Renamed from `SampleScene.unity` |
| `Assets/Scenes/EXP-SWARM-005.unity` | EXP-SWARM-005 | Foraging + trail ablation harness |

## Adding or updating an entry

1. Create or edit `reports/YYYY-MM-DD_<experiment-id>_<slug>.md`
2. Add or update an entry in `journal.manifest.json`
3. Rebuild:

```bash
python Tools/build_journal.py
```

4. Update `docs/project_memory.md` and `CHANGELOG.md` when the result affects research direction

## Format

See [.cursor/rules/experiment_journal.mdc](../../.cursor/rules/experiment_journal.mdc) for the required report template.
