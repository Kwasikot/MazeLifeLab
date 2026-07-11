import { Vector3 } from "three";
import type {
  EventLogEntry,
  SimBody,
  SimulationParams,
  SimulationState,
} from "../types";
import {
  DEFAULT_PARAMS,
  G,
  LARGE_BODY_MASS_RATIO,
  MATERIALS,
  MIN_RADIUS,
} from "../config/defaults";
import { gaussianRandom, keplerianSpeed, randomInRange } from "../utils/math";
import { PhysicsEngine } from "./PhysicsEngine";
import { CollisionSystem } from "./CollisionSystem";
import { AccretionSystem } from "./AccretionSystem";
import {
  collisionRadiusForBody,
  estimateDiskSpacing,
  isPlanet,
  isProtoplanet,
} from "./accretionUtils";

let nextBodyId = 1;

export class SimulationEngine {
  private params: SimulationParams;
  private bodies: SimBody[] = [];
  private physics: PhysicsEngine;
  private collisions: CollisionSystem;
  private accretion: AccretionSystem;
  private state: SimulationState = "idle";
  private simTime = 0;
  private eventLog: EventLogEntry[] = [];
  private initialEnergy = 0;
  private initialAngularMomentum = 0;
  private totalMerges = 0;
  private physicsStepMs = 0;

  constructor(params: Partial<SimulationParams> = {}) {
    this.params = { ...DEFAULT_PARAMS, ...params };
    this.physics = new PhysicsEngine(this.params);
    this.collisions = new CollisionSystem(this.params);
    this.accretion = new AccretionSystem();
  }

  getParams(): SimulationParams {
    return { ...this.params };
  }

  setParams(partial: Partial<SimulationParams>): void {
    this.params = { ...this.params, ...partial };
    this.physics.updateParams(this.params);
    this.collisions.updateParams(this.params);
  }

  getBodies(): readonly SimBody[] {
    return this.bodies;
  }

  getState(): SimulationState {
    return this.state;
  }

  getSimTime(): number {
    return this.simTime;
  }

  getEventLog(): EventLogEntry[] {
    return this.eventLog;
  }

  getTotalMerges(): number {
    return this.totalMerges;
  }

  getPhysicsStepMs(): number {
    return this.physicsStepMs;
  }

  getInitialEnergy(): number {
    return this.initialEnergy;
  }

  getInitialAngularMomentum(): number {
    return this.initialAngularMomentum;
  }

  generateDisk(): void {
    this.bodies = [];
    nextBodyId = 1;
    const {
      particleCount,
      starMass,
      diskMass,
      innerRadius,
      outerRadius,
      diskThickness,
      velocityDispersion,
      materialType,
      diskThicknessExaggeration,
    } = this.params;

    const avgMass = diskMass / particleCount;
    const density = MATERIALS[materialType].density;
    const baseRadius = Math.cbrt((3 * avgMass) / (4 * Math.PI * density));
    const thickness = diskThickness * diskThicknessExaggeration;

    const avgSpacing = estimateDiskSpacing(innerRadius, outerRadius, particleCount);

    for (let i = 0; i < particleCount; i++) {
      const r = randomInRange(innerRadius, outerRadius);
      const theta = randomInRange(0, Math.PI * 2);
      const z = gaussianRandom(thickness);

      const pos = new Vector3(r * Math.cos(theta), z, r * Math.sin(theta));
      const vK = keplerianSpeed(r, starMass, G);
      const vel = new Vector3(
        -vK * Math.sin(theta) + gaussianRandom(velocityDispersion),
        gaussianRandom(velocityDispersion * 0.3),
        vK * Math.cos(theta) + gaussianRandom(velocityDispersion),
      );

      const massVar = avgMass * randomInRange(0.3, 2.0);
      const radius = Math.max(MIN_RADIUS, baseRadius * randomInRange(0.5, 1.5));
      const collisionRadius = collisionRadiusForBody(radius, avgSpacing, massVar / avgMass);

      this.bodies.push({
        id: nextBodyId++,
        position: pos,
        velocity: vel,
        mass: massVar,
        radius,
        collisionRadius,
        visualRadius: radius * this.params.visualScale,
        material: materialType,
        age: 0,
        active: true,
        isLarge: massVar > starMass * LARGE_BODY_MASS_RATIO,
      });
    }

    const seedCount = Math.max(10, Math.floor(particleCount * 0.01));
    const usedIndices = new Set<number>();
    for (let s = 0; s < seedCount; s++) {
      let idx = Math.floor(Math.random() * this.bodies.length);
      let guard = 0;
      while (usedIndices.has(idx) && guard++ < 20) {
        idx = Math.floor(Math.random() * this.bodies.length);
      }
      usedIndices.add(idx);
      const seed = this.bodies[idx];
      const embryoFactor = randomInRange(12, 45);
      seed.mass *= embryoFactor;
      seed.radius = Math.max(MIN_RADIUS, baseRadius * Math.cbrt(embryoFactor) * 1.2);
      seed.collisionRadius = collisionRadiusForBody(
        seed.radius,
        avgSpacing,
        embryoFactor,
      );
      seed.isLarge = true;
    }

    this.collisions.setCellSize(avgSpacing * 0.9);

    const energy = this.physics.computeSystemEnergy(this.bodies);
    this.initialEnergy = energy.kinetic + energy.potential;
    this.initialAngularMomentum = this.physics.computeAngularMomentum(this.bodies);
    this.simTime = 0;
    this.totalMerges = 0;
    this.eventLog = [
      {
        time: 0,
        type: "info",
        message: `Диск: ${particleCount} частиц, ${seedCount} зародышей, режим ${this.params.physicsMode}`,
      },
    ];
    this.collisions.clearEvents();
    this.accretion.clearLog();
    this.accretion.updateVisualRadii(this.bodies, this.params.visualScale, starMass);
    this.state = "idle";
  }

  start(): void {
    if (this.bodies.length === 0) this.generateDisk();
    this.state = "running";
  }

  pause(): void {
    if (this.state === "running") this.state = "paused";
  }

  reset(): void {
    this.state = "idle";
    this.generateDisk();
  }

  singleStep(): void {
    if (this.bodies.length === 0) this.generateDisk();
    this.stepInternal();
    this.state = "paused";
  }

  update(_realDt: number): void {
    if (this.state !== "running") return;
    const steps = Math.max(1, Math.floor(this.params.simulationSpeed));
    for (let i = 0; i < steps; i++) {
      this.stepInternal();
    }
  }

  private stepInternal(): void {
    const t0 = performance.now();
    const dt = this.params.integrationStep * this.params.timeScale;

    this.physics.step(this.bodies, dt);
    this.collisions.setTime(this.simTime);
    this.collisions.detectAndResolve(this.bodies, dt, (a, b, result) => {
      this.totalMerges++;
      const starMass = this.params.starMass;
      let label = "Слияние";
      if (isPlanet(result.mass, starMass)) label = "Планета";
      else if (isProtoplanet(result.mass, starMass)) label = "Протопланета";
      this.eventLog.push({
        time: this.simTime,
        type: "merge",
        message: `${label} #${a.id}+#${b.id} → масса ${result.mass.toFixed(3)}`,
      });
    });

    this.accretion.updateVisualRadii(
      this.bodies,
      this.params.visualScale,
      this.params.starMass,
    );
    this.accretion.classifyLargeBodies(this.bodies, this.params.starMass);

    for (const b of this.bodies) {
      if (b.active) b.age += dt;
    }

    this.simTime += dt;
    this.physicsStepMs = performance.now() - t0;
  }

  getActiveCount(): number {
    return this.bodies.filter((b) => b.active).length;
  }
}
