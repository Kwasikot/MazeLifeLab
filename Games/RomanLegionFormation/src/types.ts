export type AgentState = "MUSTERING" | "FORMING_UP" | "DRESSING" | "TURNING_IN_PLACE";

export type BehaviorVersion = "V4" | "V5";

export type FacingCommand = "left" | "right" | "about";

export interface Slot {
  id: number;
  x: number;
  y: number;
  facing: number;
  claimedBy: number | null;
}

export interface FormationTemplate {
  name: string;
  slots: Slot[];
  anchorX: number;
  anchorY: number;
}

export interface Agent {
  id: number;
  x: number;
  y: number;
  vx: number;
  vy: number;
  facing: number;
  state: AgentState;
  claimedSlotId: number | null;
  believedSlotId: number | null;
  targetFacing: number;
  perceptionRadius: number;
  maxSpeed: number;
  maxTurnRate: number;
  radius: number;
  angularVelocity: number;
  stuckTimer: number;
  failedSlots: Set<number>;
  slotSearchCooldown: number;
}

export interface SimConfig {
  agentCount: number;
  perceptionRadius: number;
  behaviorVersion: BehaviorVersion;
  oracleMode: boolean;
  simSpeed: number;
  running: boolean;
}

export interface DebugFlags {
  showSlotGrid: boolean;
  showBelievedSpots: boolean;
  showClaimedLines: boolean;
  showPerception: boolean;
  selectedAgentId: number | null;
}

export interface SimMetrics {
  agentCount: number;
  mustering: number;
  forming: number;
  dressing: number;
  turning: number;
  dressedPercent: number;
  spinningPercent: number;
  stuckCount: number;
  meanSlotError: number;
  formationElapsed: number;
}

export const STATE_COLORS: Record<AgentState, string> = {
  MUSTERING: "#a855f7",
  FORMING_UP: "#3b82f6",
  DRESSING: "#22c55e",
  TURNING_IN_PLACE: "#eab308",
};

export const WORLD_WIDTH = 960;
export const WORLD_HEIGHT = 640;
export const FORMATION_CENTER_X = WORLD_WIDTH * 0.5;
export const FORMATION_CENTER_Y = WORLD_HEIGHT * 0.52;
