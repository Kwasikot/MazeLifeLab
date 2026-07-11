import { DEFAULT_PARAMS } from "../config/defaults";
import type { SimBody, MetricsSnapshot } from "../types";
import type { SimulationEngine } from "../core/SimulationEngine";
import { PhysicsEngine } from "../core/PhysicsEngine";

const RADIAL_BINS = 20;
const SIZE_BINS = 15;
const MASS_BINS = 15;

export class MetricsCollector {
  private history: MetricsSnapshot[] = [];
  private maxHistory = 300;
  private physics = new PhysicsEngine(DEFAULT_PARAMS);

  collect(engine: SimulationEngine): MetricsSnapshot {
    const bodies = engine.getBodies();
    const active = bodies.filter((b) => b.active);
    const params = engine.getParams();
    this.physics.updateParams(params);

    const energy = this.physics.computeSystemEnergy(bodies as SimBody[]);
    const angularMomentum = this.physics.computeAngularMomentum(bodies as SimBody[]);
    const totalEnergy = energy.kinetic + energy.potential;
    const initE = engine.getInitialEnergy();
    const initL = engine.getInitialAngularMomentum();

    const collisions = engine.getEventLog().filter((e) => e.type === "merge");
    const collisionEvents = collisions.length;

    const snapshot: MetricsSnapshot = {
      time: engine.getSimTime(),
      objectCount: active.length,
      largestMass: this.getLargestMass(active),
      avgCollisionSpeed: this.avgCollisionSpeed(active),
      collisionsPerSimSecond: collisionEvents / Math.max(engine.getSimTime(), 0.01),
      mergeCount: engine.getTotalMerges(),
      kineticEnergy: energy.kinetic,
      potentialEnergy: energy.potential,
      totalEnergy,
      angularMomentum,
      initialEnergy: initE,
      initialAngularMomentum: initL,
      energyDrift: initE !== 0 ? (totalEnergy - initE) / Math.abs(initE) : 0,
      angularMomentumDrift:
        initL !== 0 ? (angularMomentum - initL) / Math.abs(initL) : 0,
      sizeDistribution: this.binByRadius(active, SIZE_BINS),
      massDistribution: this.binByMass(active, MASS_BINS),
      radialMassBins: this.radialMassDistribution(active, params.outerRadius, RADIAL_BINS),
      topProtoplanetMasses: this.topMasses(active, 5),
    };

    this.history.push(snapshot);
    if (this.history.length > this.maxHistory) this.history.shift();
    return snapshot;
  }

  getHistory(): MetricsSnapshot[] {
    return this.history;
  }

  clear(): void {
    this.history = [];
  }

  private getLargestMass(bodies: SimBody[]): number {
    let max = 0;
    for (const b of bodies) if (b.mass > max) max = b.mass;
    return max;
  }

  private avgCollisionSpeed(bodies: SimBody[]): number {
    if (bodies.length < 2) return 0;
    let sum = 0;
    let count = 0;
    const sample = Math.min(bodies.length, 50);
    for (let i = 0; i < sample; i++) {
      for (let j = i + 1; j < sample; j++) {
        const dx = bodies[i].velocity.x - bodies[j].velocity.x;
        const dy = bodies[i].velocity.y - bodies[j].velocity.y;
        const dz = bodies[i].velocity.z - bodies[j].velocity.z;
        sum += Math.sqrt(dx * dx + dy * dy + dz * dz);
        count++;
      }
    }
    return count > 0 ? sum / count : 0;
  }

  private binByRadius(bodies: SimBody[], bins: number): number[] {
    const result = new Array(bins).fill(0);
    if (bodies.length === 0) return result;
    let maxR = 0;
    for (const b of bodies) if (b.radius > maxR) maxR = b.radius;
    if (maxR <= 0) maxR = 1;
    for (const b of bodies) {
      const idx = Math.min(bins - 1, Math.floor((b.radius / maxR) * bins));
      result[idx]++;
    }
    return result;
  }

  private binByMass(bodies: SimBody[], bins: number): number[] {
    const result = new Array(bins).fill(0);
    if (bodies.length === 0) return result;
    let maxM = 0;
    for (const b of bodies) if (b.mass > maxM) maxM = b.mass;
    if (maxM <= 0) maxM = 1;
    for (const b of bodies) {
      const idx = Math.min(bins - 1, Math.floor((Math.log10(b.mass + 1e-6) / Math.log10(maxM + 1e-6)) * bins));
      result[Math.max(0, idx)]++;
    }
    return result;
  }

  private radialMassDistribution(
    bodies: SimBody[],
    outerR: number,
    bins: number,
  ): number[] {
    const result = new Array(bins).fill(0);
    for (const b of bodies) {
      const r = b.position.length();
      const idx = Math.min(bins - 1, Math.floor((r / outerR) * bins));
      result[idx] += b.mass;
    }
    return result;
  }

  private topMasses(bodies: SimBody[], count: number): number[] {
    return bodies
      .map((b) => b.mass)
      .sort((a, b) => b - a)
      .slice(0, count);
  }
}
