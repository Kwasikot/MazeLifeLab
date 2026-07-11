import { FILE_SPACING, RANK_SPACING } from "../sim/formationGeometry";
import type { FormationTemplate, Slot } from "../types";
import { FORMATION_CENTER_X, FORMATION_CENTER_Y } from "../types";

function makeSlots(
  name: string,
  positions: Array<{ x: number; y: number; facing?: number }>,
): FormationTemplate {
  const facingDefault = -Math.PI / 2;
  const slots: Slot[] = positions.map((p, i) => ({
    id: i,
    x: FORMATION_CENTER_X + p.x,
    y: FORMATION_CENTER_Y + p.y,
    facing: p.facing ?? facingDefault,
    claimedBy: null,
  }));
  return { name, slots, anchorX: FORMATION_CENTER_X, anchorY: FORMATION_CENTER_Y };
}

export function tripleLine(count: number): FormationTemplate {
  const ranks = 3;
  const perRank = Math.ceil(count / ranks);
  const positions: Array<{ x: number; y: number }> = [];
  const width = (perRank - 1) * FILE_SPACING;
  for (let r = 0; r < ranks; r++) {
    const inRank = r === ranks - 1 ? count - r * perRank : perRank;
    const rankWidth = (inRank - 1) * FILE_SPACING;
    const xOff = (width - rankWidth) * 0.5;
    for (let i = 0; i < inRank; i++) {
      positions.push({
        x: i * FILE_SPACING + xOff - width * 0.5,
        y: r * RANK_SPACING - RANK_SPACING,
      });
    }
  }
  return makeSlots("Triple Line", positions);
}

export function rectangle(count: number): FormationTemplate {
  const cols = Math.ceil(Math.sqrt(count * 1.4));
  const rows = Math.ceil(count / cols);
  const positions: Array<{ x: number; y: number }> = [];
  const width = (cols - 1) * FILE_SPACING;
  const height = (rows - 1) * RANK_SPACING;
  for (let r = 0; r < rows && positions.length < count; r++) {
    for (let c = 0; c < cols && positions.length < count; c++) {
      positions.push({
        x: c * FILE_SPACING - width * 0.5,
        y: r * RANK_SPACING - height * 0.5,
      });
    }
  }
  return makeSlots("Rectangle", positions);
}

export function square(count: number): FormationTemplate {
  const side = Math.ceil(Math.sqrt(count));
  return rectangle(side * side).slots.length >= count
    ? makeSlots(
        "Square",
        Array.from({ length: count }, (_, i) => {
          const c = i % side;
          const r = Math.floor(i / side);
          const width = (side - 1) * FILE_SPACING;
          const height = (side - 1) * RANK_SPACING;
          return {
            x: c * FILE_SPACING - width * 0.5,
            y: r * RANK_SPACING - height * 0.5,
          };
        }),
      )
    : rectangle(count);
}

export function clearSlotClaims(template: FormationTemplate | null): void {
  if (!template) return;
  for (const slot of template.slots) slot.claimedBy = null;
}
