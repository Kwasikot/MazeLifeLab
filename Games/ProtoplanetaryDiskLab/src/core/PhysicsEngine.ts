import { Vector3 } from "three";
import type { SimBody, SimulationParams } from "../types";
import { G } from "../config/defaults";
import { keplerianSpeed } from "../utils/math";

const _accel = new Vector3();
const _dir = new Vector3();
const _tang = new Vector3();
const _keplerVel = new Vector3();

export class PhysicsEngine {
  private starMass: number;
  private mode: SimulationParams["physicsMode"];
  private largeBodyThreshold: number;
  private largeBodyInteraction: number;
  private integrator: SimulationParams["integrator"];
  private nBodyLimit: number;

  constructor(params: SimulationParams) {
    this.starMass = params.starMass;
    this.mode = params.physicsMode;
    this.largeBodyThreshold = params.starMass * 0.005;
    this.largeBodyInteraction = params.largeBodyInteraction;
    this.integrator = params.integrator;
    this.nBodyLimit = params.nBodyLimit;
  }

  updateParams(params: SimulationParams): void {
    this.starMass = params.starMass;
    this.mode = params.physicsMode;
    this.largeBodyThreshold = params.starMass * 0.005;
    this.largeBodyInteraction = params.largeBodyInteraction;
    this.integrator = params.integrator;
    this.nBodyLimit = params.nBodyLimit;
  }

  step(bodies: SimBody[], dt: number): void {
    switch (this.mode) {
      case 1:
        this.stepVisual(bodies, dt);
        break;
      case 2:
        this.stepSimplified(bodies, dt);
        break;
      case 3:
        this.stepNBody(bodies, dt);
        break;
    }
  }

  private stepVisual(bodies: SimBody[], dt: number): void {
    for (const body of bodies) {
      if (!body.active) continue;
      const r = body.position.length();
      if (r < 0.5) continue;

      const vK = keplerianSpeed(r, this.starMass, G);
      _dir.copy(body.position).normalize();
      _tang.set(-_dir.z, 0, _dir.x).normalize();
      if (_tang.lengthSq() < 0.01) {
        _tang.set(0, 0, 1);
      }

      _keplerVel.copy(_tang).multiplyScalar(vK);
      body.velocity.lerp(_keplerVel, 0.12);
      body.position.addScaledVector(body.velocity, dt);

      const flatten = 0.985;
      body.position.y *= flatten;
    }
  }

  private stepSimplified(bodies: SimBody[], dt: number): void {
    const largeIndices: number[] = [];
    for (let i = 0; i < bodies.length; i++) {
      if (bodies[i].active && bodies[i].mass >= this.largeBodyThreshold) {
        largeIndices.push(i);
      }
    }

    for (let i = 0; i < bodies.length; i++) {
      const body = bodies[i];
      if (!body.active) continue;

      _accel.set(0, 0, 0);
      this.addStarGravity(body.position, _accel);

      for (const j of largeIndices) {
        if (j === i) continue;
        const other = bodies[j];
        this.addMutualGravity(body.position, other.position, other.mass, _accel);
      }

      if (this.integrator === "euler") {
        body.velocity.addScaledVector(_accel, dt);
        body.position.addScaledVector(body.velocity, dt);
      } else {
        body.velocity.addScaledVector(_accel, dt * 0.5);
        body.position.addScaledVector(body.velocity, dt);
      }
    }
  }

  private stepNBody(bodies: SimBody[], dt: number): void {
    const activeIndices: number[] = [];
    for (let i = 0; i < bodies.length; i++) {
      if (bodies[i].active) activeIndices.push(i);
    }

    const count = Math.min(activeIndices.length, this.nBodyLimit);
    const accels = new Float64Array(count * 3);

    for (let ai = 0; ai < count; ai++) {
      const i = activeIndices[ai];
      const body = bodies[i];
      let ax = 0;
      let ay = 0;
      let az = 0;
      this.addStarGravityVec(body.position, (x, y, z) => {
        ax += x;
        ay += y;
        az += z;
      });

      for (let aj = 0; aj < count; aj++) {
        if (ai === aj) continue;
        const j = activeIndices[aj];
        const other = bodies[j];
        const dx = other.position.x - body.position.x;
        const dy = other.position.y - body.position.y;
        const dz = other.position.z - body.position.z;
        const distSq = dx * dx + dy * dy + dz * dz + 0.01;
        const dist = Math.sqrt(distSq);
        const f = (G * other.mass) / distSq;
        ax += (f * dx) / dist;
        ay += (f * dy) / dist;
        az += (f * dz) / dist;
      }
      accels[ai * 3] = ax;
      accels[ai * 3 + 1] = ay;
      accels[ai * 3 + 2] = az;
    }

    const subDt = this.integrator === "rk4" ? dt / 2 : dt;
    for (let ai = 0; ai < count; ai++) {
      const i = activeIndices[ai];
      const body = bodies[i];
      body.velocity.x += accels[ai * 3] * subDt;
      body.velocity.y += accels[ai * 3 + 1] * subDt;
      body.velocity.z += accels[ai * 3 + 2] * subDt;
      body.position.x += body.velocity.x * dt;
      body.position.y += body.velocity.y * dt;
      body.position.z += body.velocity.z * dt;
    }

    const largeIndices: number[] = [];
    for (let i = 0; i < bodies.length; i++) {
      if (bodies[i].active && bodies[i].mass >= this.largeBodyThreshold) {
        largeIndices.push(i);
      }
    }

    for (let i = count; i < activeIndices.length; i++) {
      this.stepSimplifiedForBody(bodies[activeIndices[i]], bodies, largeIndices, dt);
    }
  }

  private stepSimplifiedForBody(
    body: SimBody,
    bodies: SimBody[],
    largeIndices: number[],
    dt: number,
  ): void {
    _accel.set(0, 0, 0);
    this.addStarGravity(body.position, _accel);
    for (const j of largeIndices) {
      const other = bodies[j];
      if (other === body) continue;
      this.addMutualGravity(body.position, other.position, other.mass, _accel);
    }
    body.velocity.addScaledVector(_accel, dt);
    body.position.addScaledVector(body.velocity, dt);
  }

  private addStarGravity(pos: Vector3, out: Vector3): void {
    const r2 = pos.lengthSq() + 0.25;
    const r = Math.sqrt(r2);
    const f = (G * this.starMass) / r2;
    out.addScaledVector(pos, -f / r);
  }

  private addStarGravityVec(
    pos: Vector3,
    add: (x: number, y: number, z: number) => void,
  ): void {
    const r2 = pos.lengthSq() + 0.25;
    const r = Math.sqrt(r2);
    const f = (G * this.starMass) / r2;
    add((-f * pos.x) / r, (-f * pos.y) / r, (-f * pos.z) / r);
  }

  private addMutualGravity(
    pos: Vector3,
    otherPos: Vector3,
    otherMass: number,
    out: Vector3,
  ): void {
    _dir.subVectors(otherPos, pos);
    const distSq = _dir.lengthSq() + 0.1;
    const dist = Math.sqrt(distSq);
    const f = (G * otherMass * this.largeBodyInteraction) / distSq;
    out.addScaledVector(_dir, f / dist);
  }

  computeSystemEnergy(bodies: SimBody[]): { kinetic: number; potential: number } {
    let kinetic = 0;
    let potential = 0;
    for (const b of bodies) {
      if (!b.active) continue;
      kinetic += 0.5 * b.mass * b.velocity.lengthSq();
      const r = b.position.length();
      potential -= (G * this.starMass * b.mass) / Math.max(r, 0.1);
    }
    return { kinetic, potential };
  }

  computeAngularMomentum(bodies: SimBody[]): number {
    let L = 0;
    const cross = new Vector3();
    for (const b of bodies) {
      if (!b.active) continue;
      cross.crossVectors(b.position, b.velocity);
      L += b.mass * cross.length();
    }
    return L;
  }
}
