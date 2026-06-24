# MazeLifeLab Experiment Journal

Durable record of completed experiment runs, batch tests, ablations, and validation milestones.

## Purpose

- Preserve **what was tested**, **numbers observed**, **verdict**, and **next steps**
- Complement `CHANGELOG.md` (code changes) and `docs/project_memory.md` (current focus)
- Provide a **mobile-friendly HTML index** for reading results outside Unity

## Files

| File | Role |
|------|------|
| [index.html](index.html) | Master navigable report (mobile-ready) |
| [index.md](index.md) | Markdown index of all reports |
| [reports/](reports/) | One markdown file per completed experiment run |

## Format

See [.cursor/rules/experiment_journal.mdc](../../.cursor/rules/experiment_journal.mdc) for the required report template and update checklist.

## Adding a new entry

1. Create `reports/YYYY-MM-DD_<experiment-id>_<slug>.md`
2. Add section + nav link to `index.html`
3. Link from `index.md`
4. Update `docs/project_memory.md` and `CHANGELOG.md` Research Notes
