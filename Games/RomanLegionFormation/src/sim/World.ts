import { clearSlotClaims, rectangle, square, tripleLine } from "../formations/templates";
import { distance, normalizeAngle, SpatialHash, turnToward } from "./math";
import type {
  Agent,
  BehaviorVersion,
  DebugFlags,
  FacingCommand,
  FormationTemplate,
  SimConfig,
  SimMetrics,
  Slot,
} from "../types";
import {
  AGENT_BODY_RADIUS,
  FILE_SPACING,
  MIN_BODY_DIST,
  RANK_SPACING,
  SEPARATION_RADIUS,
  SLOT_POS_TOLERANCE,
} from "./formationGeometry";
import {
  FORMATION_CENTER_X,
  FORMATION_CENTER_Y,
  WORLD_HEIGHT,
  WORLD_WIDTH,
} from "../types";

const DT = 1 / 60;
const SLOT_ANGLE_TOLERANCE = 0.45;
const FAILED_SLOT_TTL = 4;
const COLLISION_ITERATIONS = 1;
const STALE_CLAIM_DISTANCE = SLOT_POS_TOLERANCE * 4;
const MAX_PERCEPTION_RADIUS = 220;
const NEIGHBOR_DIST = Math.max(FILE_SPACING, RANK_SPACING) * 1.25;
const NEIGHBOR_DIST_SQ = NEIGHBOR_DIST * NEIGHBOR_DIST;

export interface BehaviorProfile {
  maxVisibleSeats: number;
  stuckSpinThreshold: number;
  stuckTimeSeconds: number;
  perceptionBoostWhenStuck: number;
  slotSearchInterval: number;
  musteringRadius: number;
}

export function profileFor(version: BehaviorVersion): BehaviorProfile {
  if (version === "V4") {
    return {
      maxVisibleSeats: 64,
      stuckSpinThreshold: 0.28,
      stuckTimeSeconds: 3,
      perceptionBoostWhenStuck: 40,
      slotSearchInterval: 0.25,
      musteringRadius: 220,
    };
  }
  return {
    maxVisibleSeats: 128,
    stuckSpinThreshold: 0.22,
    stuckTimeSeconds: 1.8,
    perceptionBoostWhenStuck: 120,
    slotSearchInterval: 0.15,
    musteringRadius: 200,
  };
}

export class World {
  agents: Agent[] = [];
  template: FormationTemplate | null = null;
  config: SimConfig;
  debug: DebugFlags;
  formationCommandTime = 0;
  facingOnly = false;
  private failedSlotExpiry = new Map<number, number>();
  private spatial = new SpatialHash(24);
  private neighborBuf: number[] = [];
  private pendingDx: number[] = [];
  private pendingDy: number[] = [];
  private previousTargetDistance: number[] = [];
  private slotDepthCache: number[] = [];
  private slotAdjacency: number[][] = [];
  private dressedNeighborCache: number[] = [];
  private rngSeed: number;

  constructor(config: SimConfig, debug: DebugFlags, seed = 42) {
    this.config = config;
    this.debug = debug;
    this.rngSeed = seed;
    this.resetAgents(config.agentCount);
  }

  resetAgents(count: number): void {
    this.config.agentCount = count;
    this.agents = [];
    let s = this.rngSeed;
    const rand = () => {
      s = (s * 1664525 + 1013904223) >>> 0;
      return s / 0xffffffff;
    };
    for (let i = 0; i < count; i++) {
      const angle = rand() * Math.PI * 2;
      const rad = 40 + rand() * 160;
      this.agents.push({
        id: i,
        x: FORMATION_CENTER_X + Math.cos(angle) * rad,
        y: FORMATION_CENTER_Y + Math.sin(angle) * rad,
        vx: 0,
        vy: 0,
        facing: rand() * Math.PI * 2,
        state: "MUSTERING",
        claimedSlotId: null,
        believedSlotId: null,
        targetFacing: -Math.PI / 2,
        perceptionRadius: this.config.perceptionRadius,
        maxSpeed: 42,
        maxTurnRate: 2.8,
        radius: AGENT_BODY_RADIUS,
        angularVelocity: 0,
        stuckTimer: 0,
        failedSlots: new Set(),
        slotSearchCooldown: rand() * 0.3,
      });
    }
    this.template = null;
    this.facingOnly = false;
    this.formationCommandTime = 0;
    this.previousTargetDistance = new Array(count).fill(Infinity);
  }

  scatter(): void {
    this.resetAgents(this.config.agentCount);
  }

  commandTripleLine(): void {
    this.beginFormation(tripleLine(this.config.agentCount));
  }

  commandRectangle(): void {
    this.beginFormation(rectangle(this.config.agentCount));
  }

  commandSquare(): void {
    this.beginFormation(square(this.config.agentCount));
  }

  commandFacing(cmd: FacingCommand): void {
    if (!this.template) return;
    this.facingOnly = true;
    const delta =
      cmd === "left" ? -Math.PI / 2 : cmd === "right" ? Math.PI / 2 : Math.PI;
    for (const agent of this.agents) {
      agent.targetFacing = normalizeAngle(agent.facing + delta);
      agent.state = "TURNING_IN_PLACE";
    }
    for (const slot of this.template.slots) {
      slot.facing = normalizeAngle(slot.facing + delta);
    }
  }

  private beginFormation(template: FormationTemplate): void {
    clearSlotClaims(this.template);
    this.template = template;
    this.facingOnly = false;
    this.formationCommandTime = 0;
    for (const agent of this.agents) {
      agent.state = "FORMING_UP";
      agent.claimedSlotId = null;
      agent.believedSlotId = null;
      agent.failedSlots.clear();
      agent.slotSearchCooldown = 0;
      agent.stuckTimer = 0;
    }
    clearSlotClaims(template);
    this.failedSlotExpiry.clear();
    this.previousTargetDistance.fill(Infinity);
    this.buildFormationCaches(template);
  }

  private buildFormationCaches(template: FormationTemplate): void {
    const n = template.slots.length;
    this.slotDepthCache = new Array(n);
    this.slotAdjacency = new Array(n);
    this.dressedNeighborCache = new Array(n).fill(0);

    for (let i = 0; i < n; i++) {
      const slot = template.slots[i];
      this.slotDepthCache[i] = distance(
        slot.x,
        slot.y,
        FORMATION_CENTER_X,
        FORMATION_CENTER_Y,
      );

      const neighbors: number[] = [];
      for (let j = 0; j < n; j++) {
        if (i === j) continue;
        const other = template.slots[j];
        const dx = slot.x - other.x;
        const dy = slot.y - other.y;
        if (dx * dx + dy * dy <= NEIGHBOR_DIST_SQ) neighbors.push(j);
      }
      this.slotAdjacency[i] = neighbors;
    }
  }

  private refreshDressedNeighborCache(): void {
    if (!this.template) return;
    this.dressedNeighborCache.fill(0);
    for (const slot of this.template.slots) {
      if (slot.claimedBy === null) continue;
      const holder = this.agents[slot.claimedBy];
      if (holder?.state !== "DRESSING") continue;
      for (const neighborId of this.slotAdjacency[slot.id]) {
        this.dressedNeighborCache[neighborId]++;
      }
    }
  }

  private effectivePerception(agent: Agent, profile: BehaviorProfile): number {
    let radius = this.config.perceptionRadius;
    if (this.template) {
      const dx = FORMATION_CENTER_X - agent.x;
      const dy = FORMATION_CENTER_Y - agent.y;
      const distCenter = Math.sqrt(dx * dx + dy * dy);
      radius = Math.max(radius, distCenter * 0.85 + 40);
    }
    if (agent.stuckTimer > profile.stuckTimeSeconds * 0.4) {
      radius += profile.perceptionBoostWhenStuck;
    }
    return Math.min(radius, MAX_PERCEPTION_RADIUS);
  }

  step(): void {
    if (!this.config.running) return;
    const steps = Math.max(1, Math.round(this.config.simSpeed));
    for (let i = 0; i < steps; i++) this.integrate(DT);
  }

  private integrate(dt: number): void {
    if (this.template && !this.facingOnly) this.formationCommandTime += dt;

    this.spatial.clear();
    for (const a of this.agents) this.spatial.insert(a);

    const profile = profileFor(this.config.behaviorVersion);
    this.expireFailedSlots(this.formationCommandTime);
    if (this.template) this.refreshDressedNeighborCache();

    for (const agent of this.agents) {
      agent.perceptionRadius = this.effectivePerception(agent, profile);

      const prevFacing = agent.facing;

      if (agent.state === "TURNING_IN_PLACE") {
        this.stepTurning(agent, dt);
      } else if (!this.template) {
        this.wander(agent, dt);
      } else if (agent.state === "MUSTERING") {
        this.stepMustering(agent, dt, profile);
      } else if (agent.state === "FORMING_UP") {
        this.stepForming(agent, dt, profile);
      } else if (agent.state === "DRESSING") {
        this.stepDressing(agent, dt, profile);
      }

      agent.angularVelocity = Math.abs(normalizeAngle(agent.facing - prevFacing)) / dt;
    }

    this.resolveAgentContacts(dt);

    for (const agent of this.agents) {
      this.clampToWorld(agent);
    }
  }

  private expireFailedSlots(now: number): void {
    for (const [key, expiry] of this.failedSlotExpiry) {
      if (expiry <= now) this.failedSlotExpiry.delete(key);
    }
  }

  private markSlotFailed(agent: Agent, slotId: number): void {
    agent.failedSlots.add(slotId);
    this.failedSlotExpiry.set(agent.id * 10000 + slotId, this.formationCommandTime + FAILED_SLOT_TTL);
  }

  private isSlotFailed(agent: Agent, slotId: number): boolean {
    if (!agent.failedSlots.has(slotId)) return false;
    const expiry = this.failedSlotExpiry.get(agent.id * 10000 + slotId);
    if (expiry !== undefined && expiry <= this.formationCommandTime) {
      agent.failedSlots.delete(slotId);
      this.failedSlotExpiry.delete(agent.id * 10000 + slotId);
      return false;
    }
    return true;
  }

  private stepTurning(agent: Agent, dt: number): void {
    const prev = agent.facing;
    agent.facing = turnToward(agent.facing, agent.targetFacing, agent.maxTurnRate * dt);
    agent.vx *= 0.85;
    agent.vy *= 0.85;
    agent.x += agent.vx * dt;
    agent.y += agent.vy * dt;
    if (Math.abs(normalizeAngle(agent.targetFacing - agent.facing)) < 0.05) {
      agent.facing = agent.targetFacing;
      agent.state = "DRESSING";
    }
    agent.angularVelocity = Math.abs(normalizeAngle(agent.facing - prev)) / dt;
  }

  private stepMustering(agent: Agent, dt: number, _profile: BehaviorProfile): void {
    this.idleWhileSearching(agent, dt);
    agent.state = "FORMING_UP";
  }

  private stepForming(agent: Agent, dt: number, profile: BehaviorProfile): void {
    agent.slotSearchCooldown -= dt;

    if (agent.claimedSlotId !== null) {
      const held = this.getSlot(agent.claimedSlotId);
      if (held && (held.claimedBy === null || held.claimedBy === agent.id)) {
        if (held.claimedBy === null) held.claimedBy = agent.id;
        this.moveAgentTowardSlot(agent, held, dt, profile);
        return;
      }
    }

    if (agent.slotSearchCooldown <= 0) {
      const slot = this.pickSlot(agent, profile);
      if (slot && this.canClaim(agent, slot)) {
        agent.believedSlotId = slot.id;
        this.releaseClaim(agent);
        slot.claimedBy = agent.id;
        agent.claimedSlotId = slot.id;
        agent.stuckTimer = 0;
        this.previousTargetDistance[agent.id] = Infinity;
        this.moveAgentTowardSlot(agent, slot, dt, profile);
        return;
      }

      const hint = this.pickHintSlot(agent);
      agent.believedSlotId = hint?.id ?? null;
      agent.slotSearchCooldown = profile.slotSearchInterval;
    }

    if (agent.believedSlotId !== null) {
      const hint = this.getSlot(agent.believedSlotId);
      if (hint) {
        this.seek(agent, hint.x, hint.y, agent.maxSpeed * 0.55, dt);
        agent.facing = turnToward(
          agent.facing,
          Math.atan2(hint.y - agent.y, hint.x - agent.x),
          agent.maxTurnRate * dt,
        );
        this.updateStuck(agent, dt, profile, true);
        return;
      }
    }

    this.idleWhileSearching(agent, dt);
    this.updateStuck(agent, dt, profile, false);
  }

  private moveAgentTowardSlot(
    agent: Agent,
    slot: Slot,
    dt: number,
    profile: BehaviorProfile,
  ): void {
    agent.believedSlotId = slot.id;
    const previousDistance = this.previousTargetDistance[agent.id];
    this.seek(agent, slot.x, slot.y, agent.maxSpeed, dt);
    const currentDistance = distance(agent.x, agent.y, slot.x, slot.y);
    this.previousTargetDistance[agent.id] = currentDistance;
    agent.facing = turnToward(agent.facing, slot.facing, agent.maxTurnRate * dt);
    const posOk = currentDistance < SLOT_POS_TOLERANCE;
    const angOk =
      Math.abs(normalizeAngle(agent.facing - slot.facing)) < SLOT_ANGLE_TOLERANCE;
    if (posOk && angOk) {
      agent.state = "DRESSING";
      agent.stuckTimer = 0;
    }
    this.updateStuck(
      agent,
      dt,
      profile,
      currentDistance < previousDistance - 0.02,
    );
  }

  private canClaim(agent: Agent, slot: Slot): boolean {
    return slot.claimedBy === null || slot.claimedBy === agent.id;
  }

  private stepDressing(agent: Agent, dt: number, profile: BehaviorProfile): void {
    const slot = this.getSlot(agent.claimedSlotId);
    if (!slot) {
      agent.state = "FORMING_UP";
      return;
    }

    if (slot.claimedBy !== agent.id) {
      agent.state = "FORMING_UP";
      agent.claimedSlotId = null;
      agent.believedSlotId = null;
      return;
    }

    const dist = distance(agent.x, agent.y, slot.x, slot.y);
    if (dist > SLOT_POS_TOLERANCE * 1.8) {
      this.seek(agent, slot.x, slot.y, agent.maxSpeed * 0.85, dt);
      agent.facing = turnToward(agent.facing, slot.facing, agent.maxTurnRate * dt);
      if (dist > SLOT_POS_TOLERANCE * 3) agent.state = "FORMING_UP";
    } else {
      agent.vx *= 0.7;
      agent.vy *= 0.7;
      agent.x += (slot.x - agent.x) * 0.18;
      agent.y += (slot.y - agent.y) * 0.18;
      agent.facing = turnToward(agent.facing, slot.facing, agent.maxTurnRate * dt);
    }

    this.updateStuck(agent, dt, profile, true);
  }

  private pickSlot(agent: Agent, profile: BehaviorProfile): Slot | null {
    if (!this.template) return null;

    if (agent.slotSearchCooldown > 0 && agent.believedSlotId !== null) {
      const believed = this.getSlot(agent.believedSlotId);
      if (believed && this.isSlotAvailable(agent, believed)) return believed;
    }

    return this.findBestSlot(agent, profile.maxVisibleSeats, true);
  }

  private pickHintSlot(agent: Agent): Slot | null {
    return this.findBestSlot(agent, 48, false);
  }

  private findBestSlot(
    agent: Agent,
    _scanLimit: number,
    requireAvailable: boolean,
  ): Slot | null {
    if (!this.template) return null;

    const px = agent.x;
    const py = agent.y;
    const perceive = this.config.oracleMode ? Infinity : agent.perceptionRadius;
    const perceiveSq = perceive * perceive;

    let best: Slot | null = null;
    let bestScore = Infinity;

    for (const slot of this.template.slots) {
      if (this.isSlotFailed(agent, slot.id)) continue;

      const dx = slot.x - px;
      const dy = slot.y - py;
      const distSq = dx * dx + dy * dy;
      if (!this.config.oracleMode && distSq > perceiveSq) continue;

      const score =
        this.dressedNeighborCache[slot.id] * 1000 -
        this.slotDepthCache[slot.id] +
        Math.sqrt(distSq);

      if (score >= bestScore) continue;
      if (requireAvailable && !this.isSlotAvailable(agent, slot)) continue;

      bestScore = score;
      best = slot;
    }

    return best;
  }

  private idleWhileSearching(agent: Agent, dt: number): void {
    agent.vx *= 0.94;
    agent.vy *= 0.94;
    agent.facing +=
      Math.sin(agent.id * 0.61 + this.formationCommandTime * 0.7) * 0.25 * dt;
    agent.x += Math.cos(agent.facing) * 5 * dt;
    agent.y += Math.sin(agent.facing) * 5 * dt;
  }

  private isClaimHolderActive(slot: Slot): boolean {
    if (slot.claimedBy === null) return false;
    const holder = this.agents[slot.claimedBy];
    if (!holder || holder.claimedSlotId !== slot.id) return false;
    if (holder.state === "DRESSING") return true;
    return distance(holder.x, holder.y, slot.x, slot.y) < STALE_CLAIM_DISTANCE;
  }

  private clearStaleClaim(slot: Slot): void {
    if (slot.claimedBy === null) return;
    const holder = this.agents[slot.claimedBy];
    if (holder && holder.claimedSlotId === slot.id) {
      holder.claimedSlotId = null;
      if (holder.believedSlotId === slot.id) holder.believedSlotId = null;
    }
    slot.claimedBy = null;
  }

  private isSlotAvailable(agent: Agent, slot: Slot): boolean {
    if (slot.claimedBy !== null && slot.claimedBy !== agent.id) {
      if (this.isClaimHolderActive(slot)) return false;
      this.clearStaleClaim(slot);
    }
    if (this.isAgentOnSlot(agent, slot)) return true;

    this.spatial.queryRadius(
      slot.x,
      slot.y,
      AGENT_BODY_RADIUS * 2,
      this.agents,
      this.neighborBuf,
    );
    for (const id of this.neighborBuf) {
      if (id === agent.id) continue;
      const other = this.agents[id];
      if (other.claimedSlotId === slot.id && other.state === "DRESSING") return false;
      if (
        other.claimedSlotId === slot.id &&
        other.state === "FORMING_UP" &&
        distance(other.x, other.y, slot.x, slot.y) < SLOT_POS_TOLERANCE * 2
      ) {
        return false;
      }
    }
    return true;
  }

  private isAgentOnSlot(agent: Agent, slot: Slot): boolean {
    return distance(agent.x, agent.y, slot.x, slot.y) < SLOT_POS_TOLERANCE * 1.5;
  }

  private updateStuck(
    agent: Agent,
    dt: number,
    profile: BehaviorProfile,
    makingProgress: boolean,
  ): void {
    if (makingProgress) {
      agent.stuckTimer = Math.max(0, agent.stuckTimer - dt * 2);
    } else {
      const spinMultiplier =
        agent.angularVelocity > profile.stuckSpinThreshold ? 1.5 : 1;
      agent.stuckTimer += dt * spinMultiplier;
    }

    if (agent.stuckTimer > profile.stuckTimeSeconds) {
      if (agent.believedSlotId !== null) this.markSlotFailed(agent, agent.believedSlotId);
      this.releaseClaim(agent);
      agent.believedSlotId = null;
      agent.claimedSlotId = null;
      agent.state = "FORMING_UP";
      agent.slotSearchCooldown = profile.slotSearchInterval;
      agent.stuckTimer = 0;
      this.previousTargetDistance[agent.id] = Infinity;
    }
  }

  private releaseClaim(agent: Agent): void {
    if (agent.claimedSlotId !== null && this.template) {
      const slot = this.getSlot(agent.claimedSlotId);
      if (slot && slot.claimedBy === agent.id) slot.claimedBy = null;
    }
  }

  private getSlot(id: number | null): Slot | null {
    if (id === null || !this.template) return null;
    return this.template.slots[id] ?? null;
  }

  private seek(
    agent: Agent,
    tx: number,
    ty: number,
    speed: number,
    dt: number,
  ): void {
    const dx = tx - agent.x;
    const dy = ty - agent.y;
    const dist = Math.sqrt(dx * dx + dy * dy) || 1;
    const desiredVx = (dx / dist) * speed;
    const desiredVy = (dy / dist) * speed;
    agent.vx += (desiredVx - agent.vx) * Math.min(1, dt * 4);
    agent.vy += (desiredVy - agent.vy) * Math.min(1, dt * 4);
    agent.x += agent.vx * dt;
    agent.y += agent.vy * dt;
  }

  private wander(agent: Agent, dt: number): void {
    agent.vx *= 0.95;
    agent.vy *= 0.95;
    agent.facing += (Math.sin(agent.id * 0.7 + this.formationCommandTime * 0.001) * 0.5) * dt;
    agent.x += Math.cos(agent.facing) * 12 * dt;
    agent.y += Math.sin(agent.facing) * 12 * dt;
  }

  private isLockedInSlot(agent: Agent): boolean {
    if (agent.state !== "DRESSING" || agent.claimedSlotId === null) return false;
    const slot = this.getSlot(agent.claimedSlotId);
    if (!slot) return false;
    return distance(agent.x, agent.y, slot.x, slot.y) < SLOT_POS_TOLERANCE;
  }

  /** Dressed soldiers ignore same-slot neighbors; still repel everyone else. */
  private shouldApplyContact(a: Agent, b: Agent): boolean {
    if (a.id === b.id) return false;

    const aLocked = this.isLockedInSlot(a);
    const bLocked = this.isLockedInSlot(b);

    if (aLocked && bLocked) {
      if (a.claimedSlotId === b.claimedSlotId) return false;
      return true;
    }

    if (aLocked && b.state === "FORMING_UP" && b.claimedSlotId !== a.claimedSlotId) {
      return false;
    }
    if (bLocked && a.state === "FORMING_UP" && a.claimedSlotId !== b.claimedSlotId) {
      return false;
    }

    if (a.state === "DRESSING" && !aLocked) return true;
    if (b.state === "DRESSING" && !bLocked) return true;

    return true;
  }

  private resolveAgentContacts(dt: number): void {
    const n = this.agents.length;
    if (this.pendingDx.length !== n) {
      this.pendingDx = new Array(n).fill(0);
      this.pendingDy = new Array(n).fill(0);
    }

    const minDist = MIN_BODY_DIST;
    const queryRadius = SEPARATION_RADIUS;

    for (let iter = 0; iter < COLLISION_ITERATIONS; iter++) {
      this.pendingDx.fill(0);
      this.pendingDy.fill(0);

      for (const agent of this.agents) {
        const baseX = agent.x;
        const baseY = agent.y;

        this.spatial.queryRadius(baseX, baseY, queryRadius, this.agents, this.neighborBuf);
        for (const id of this.neighborBuf) {
          if (id <= agent.id) continue;

          const other = this.agents[id];
          if (!this.shouldApplyContact(agent, other)) continue;

          const ax = baseX + this.pendingDx[agent.id];
          const ay = baseY + this.pendingDy[agent.id];
          const bx = other.x + this.pendingDx[id];
          const by = other.y + this.pendingDy[id];
          const dx = ax - bx;
          const dy = ay - by;
          const dist = Math.sqrt(dx * dx + dy * dy) || 0.001;
          const nx = dx / dist;
          const ny = dy / dist;

          if (dist < minDist) {
            const overlap = (minDist - dist) * 0.5;
            this.pendingDx[agent.id] += nx * overlap;
            this.pendingDy[agent.id] += ny * overlap;
            this.pendingDx[id] -= nx * overlap;
            this.pendingDy[id] -= ny * overlap;
            this.dampApproachVelocity(agent, other, nx, ny);
          } else if (dist < SEPARATION_RADIUS) {
            const push = ((SEPARATION_RADIUS - dist) / (SEPARATION_RADIUS - minDist)) * 28 * dt;
            this.pendingDx[agent.id] += nx * push * 0.5;
            this.pendingDy[agent.id] += ny * push * 0.5;
            this.pendingDx[id] -= nx * push * 0.5;
            this.pendingDy[id] -= ny * push * 0.5;
          }
        }
      }

      for (let i = 0; i < n; i++) {
        this.agents[i].x += this.pendingDx[i];
        this.agents[i].y += this.pendingDy[i];
      }
    }
  }

  private dampApproachVelocity(a: Agent, b: Agent, nx: number, ny: number): void {
    const relVx = a.vx - b.vx;
    const relVy = a.vy - b.vy;
    const closing = relVx * nx + relVy * ny;
    if (closing <= 0) return;

    const damp = 0.35;
    a.vx -= nx * closing * damp;
    a.vy -= ny * closing * damp;
    b.vx += nx * closing * damp;
    b.vy += ny * closing * damp;
  }

  private clampToWorld(agent: Agent): void {
    agent.x = Math.max(8, Math.min(WORLD_WIDTH - 8, agent.x));
    agent.y = Math.max(8, Math.min(WORLD_HEIGHT - 8, agent.y));
  }

  getMetrics(): SimMetrics {
    let mustering = 0;
    let forming = 0;
    let dressing = 0;
    let turning = 0;
    let dressed = 0;
    let spinning = 0;
    let stuck = 0;
    let errorSum = 0;
    let errorN = 0;
    const profile = profileFor(this.config.behaviorVersion);

    for (const a of this.agents) {
      if (a.state === "MUSTERING") mustering++;
      else if (a.state === "FORMING_UP") forming++;
      else if (a.state === "DRESSING") dressing++;
      else if (a.state === "TURNING_IN_PLACE") turning++;

      if (a.state === "DRESSING" && a.claimedSlotId !== null) {
        dressed++;
        const slot = this.getSlot(a.claimedSlotId);
        if (slot) {
          errorSum += distance(a.x, a.y, slot.x, slot.y);
          errorN++;
        }
      }
      if (a.angularVelocity > profile.stuckSpinThreshold) spinning++;
      if (a.stuckTimer > profile.stuckTimeSeconds * 0.6) stuck++;
    }

    const n = this.agents.length || 1;
    return {
      agentCount: n,
      mustering,
      forming,
      dressing,
      turning,
      dressedPercent: (dressed / n) * 100,
      spinningPercent: (spinning / n) * 100,
      stuckCount: stuck,
      meanSlotError: errorN > 0 ? errorSum / errorN : 0,
      formationElapsed: this.formationCommandTime,
    };
  }
}
