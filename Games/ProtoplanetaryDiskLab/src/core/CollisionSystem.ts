import { Vector3 } from "three";
import type {
  CollisionEvent,
  MergeEvent,
  SimBody,
  SimulationParams,
} from "../types";
import { MATERIALS } from "../config/defaults";
import { radiusFromMass } from "../utils/math";
import { SpatialHash } from "../utils/spatialHash";
import {
  materialForMerge,
  updateBodyCollisionRadius,
} from "./accretionUtils";

const _relVel = new Vector3();
const _comPos = new Vector3();
const _comVel = new Vector3();
const _neighborBuf: number[] = [];

export class CollisionSystem {
  private spatialHash: SpatialHash;
  private collisionIntensity: number;
  private stickProbability: number;
  private criticalVelocity: number;
  private collisionEvents: CollisionEvent[] = [];
  private mergeEvents: MergeEvent[] = [];
  private simTime = 0;
  private cellSize = 2;

  constructor(params: SimulationParams) {
    this.cellSize = 2;
    this.spatialHash = new SpatialHash(this.cellSize);
    this.collisionIntensity = params.collisionIntensity;
    this.stickProbability = params.stickProbability;
    this.criticalVelocity = params.criticalVelocity;
  }

  updateParams(params: SimulationParams): void {
    this.collisionIntensity = params.collisionIntensity;
    this.stickProbability = params.stickProbability;
    this.criticalVelocity = params.criticalVelocity;
  }

  setCellSize(size: number): void {
    this.cellSize = Math.max(0.5, size);
    this.spatialHash = new SpatialHash(this.cellSize);
  }

  setTime(t: number): void {
    this.simTime = t;
  }

  getCollisionEvents(): CollisionEvent[] {
    return this.collisionEvents;
  }

  getMergeEvents(): MergeEvent[] {
    return this.mergeEvents;
  }

  clearEvents(): void {
    this.collisionEvents = [];
    this.mergeEvents = [];
  }

  detectAndResolve(
    bodies: SimBody[],
    _dt: number,
    onMerge: (a: SimBody, b: SimBody, result: SimBody) => void,
  ): void {
    if (this.collisionIntensity <= 0) return;

    this.spatialHash.build(bodies);
    const checked = new Set<string>();

    for (let i = 0; i < bodies.length; i++) {
      const a = bodies[i];
      if (!a.active) continue;

      const queryR = a.collisionRadius * 2.5;
      this.spatialHash.query(a.position, queryR, _neighborBuf);

      for (const j of _neighborBuf) {
        if (j <= i) continue;
        const b = bodies[j];
        if (!b.active) continue;

        const key = i < j ? `${i}-${j}` : `${j}-${i}`;
        if (checked.has(key)) continue;
        checked.add(key);

        const dist = a.position.distanceTo(b.position);
        const touchDist = a.collisionRadius + b.collisionRadius;
        if (dist > touchDist * 1.25) continue;

        _relVel.subVectors(a.velocity, b.velocity);
        const relSpeed = _relVel.length();

        this.collisionEvents.push({
          time: this.simTime,
          bodyA: a.id,
          bodyB: b.id,
          relativeSpeed: relSpeed,
          merged: false,
        });

        const matA = MATERIALS[a.material];
        const matB = MATERIALS[b.material];
        const stickMat = a.mass >= b.mass ? matA : matB;

        const stickThresh =
          this.criticalVelocity *
          stickMat.criticalVelocity *
          (1.2 / Math.max(0.3, this.collisionIntensity));
        const stickChance = Math.min(
          1,
          this.stickProbability * stickMat.stickProbability * this.collisionIntensity * 1.2,
        );

        const overlap = touchDist - dist;
        const closing = overlap > 0 || relSpeed < stickThresh * 1.5;

        if (closing && relSpeed < stickThresh && Math.random() < stickChance) {
          const merged = this.mergeBodies(a, b);
          onMerge(a, b, merged);
          this.collisionEvents[this.collisionEvents.length - 1].merged = true;
          this.mergeEvents.push({
            time: this.simTime,
            bodyIds: [a.id, b.id],
            newMass: merged.mass,
            newRadius: merged.radius,
          });
        } else if (overlap > 0) {
          this.separateAndDamp(a, b, overlap, relSpeed);
        }
      }
    }
  }

  private mergeBodies(a: SimBody, b: SimBody): SimBody {
    const keeper = a.mass >= b.mass ? a : b;
    const other = keeper === a ? b : a;

    const totalMass = a.mass + b.mass;
    _comPos
      .copy(a.position)
      .multiplyScalar(a.mass)
      .addScaledVector(b.position, b.mass)
      .divideScalar(totalMass);
    _comVel
      .copy(a.velocity)
      .multiplyScalar(a.mass)
      .addScaledVector(b.velocity, b.mass)
      .divideScalar(totalMass);

    const material = materialForMerge(a.material, b.material);
    const density = MATERIALS[material].density;
    const newRadius = radiusFromMass(totalMass, density);

    keeper.mass = totalMass;
    keeper.radius = newRadius;
    keeper.position.copy(_comPos);
    keeper.velocity.copy(_comVel);
    keeper.material = material;
    keeper.age = Math.max(a.age, b.age);
    keeper.collisionRadius = Math.max(
      newRadius * 2,
      keeper.collisionRadius,
      other.collisionRadius,
    );
    updateBodyCollisionRadius(keeper);

    other.active = false;
    return keeper;
  }

  private separateAndDamp(a: SimBody, b: SimBody, overlap: number, relSpeed: number): void {
    _relVel.subVectors(b.position, a.position);
    if (_relVel.lengthSq() < 1e-8) {
      _relVel.set(Math.random() - 0.5, 0, Math.random() - 0.5);
    }
    _relVel.normalize();

    a.position.addScaledVector(_relVel, -overlap * 0.45);
    b.position.addScaledVector(_relVel, overlap * 0.45);

    const damp = Math.min(0.5, relSpeed * 0.08);
    a.velocity.addScaledVector(_relVel, -damp);
    b.velocity.addScaledVector(_relVel, damp);
  }
}
