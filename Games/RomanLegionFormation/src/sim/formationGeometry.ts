/** Shared soldier + slot geometry — keep spacing and body radius in sync. */
export const AGENT_BODY_RADIUS = 5;
export const SLOT_GAP = 4;
export const FILE_SPACING = AGENT_BODY_RADIUS * 2 + SLOT_GAP;
export const RANK_SPACING = AGENT_BODY_RADIUS * 2 + SLOT_GAP + 2;
export const SLOT_POS_TOLERANCE = AGENT_BODY_RADIUS * 1.2;
export const MIN_BODY_DIST = AGENT_BODY_RADIUS * 2;
export const SEPARATION_RADIUS = MIN_BODY_DIST + SLOT_GAP;
export const SLOT_MARKER_RADIUS = AGENT_BODY_RADIUS - 1;
