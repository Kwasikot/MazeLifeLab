# Dev Notes — Slot Claiming

## Overview

1. **Formation template** defines slot positions (global geometry only).
2. Each **agent** independently selects a slot using local rules.
3. **Claim** is stored on `Slot.claimedBy` and `Agent.claimedSlotId`.

## Per tick (FORMING_UP)

1. Collect candidate slots within `perceptionRadius` (or all slots if Oracle mode).
2. Sort by distance to agent.
3. Take up to `maxVisibleSeats` (32 V4 / 48 V5).
4. Pick nearest slot that passes `isSlotAvailable`:
   - not claimed by another agent
   - no other agent with lower id targeting same believed slot
   - local occupancy check via spatial hash
5. Seek slot position + rotate toward slot facing.
6. Transition to DRESSING when within position/angle tolerance.

## DRESSING

- Hold slot; if pushed away, seek back (slot loyalty).
- If claim lost, return to FORMING_UP.

## Stuck detection (V5)

- High angular velocity + low progress → increment `stuckTimer`.
- On timeout: release claim, blacklist failed slot briefly, re-search.

## Facing commands

- Set `TURNING_IN_PLACE`; rotate `targetFacing` by ±90° or 180°.
- Slot **positions unchanged**; slot facings rotate with unit.

## Related MazeLifeLab research

- Compare to EXP-SWARM-005b: hive bulletin vs local broadcast vs none.
- Future: **centurion agents** that broadcast formation hints locally (not oracle).
