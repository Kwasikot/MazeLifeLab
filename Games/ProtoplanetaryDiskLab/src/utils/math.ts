import { Vector3 } from "three";

export function length3(v: Vector3): number {
  return Math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

export function dot3(a: Vector3, b: Vector3): number {
  return a.x * b.x + a.y * b.y + a.z * b.z;
}

export function cross3(a: Vector3, b: Vector3, out: Vector3): Vector3 {
  out.set(
    a.y * b.z - a.z * b.y,
    a.z * b.x - a.x * b.z,
    a.x * b.y - a.y * b.x,
  );
  return out;
}

export function keplerianSpeed(r: number, starMass: number, G: number): number {
  return Math.sqrt((G * starMass) / Math.max(r, 0.1));
}

export function radiusFromMass(mass: number, density: number): number {
  const volume = mass / density;
  return Math.cbrt((3 * volume) / (4 * Math.PI));
}

export function massFromRadius(radius: number, density: number): number {
  const volume = (4 / 3) * Math.PI * radius * radius * radius;
  return volume * density;
}

export function radialVelocity(pos: Vector3, vel: Vector3): number {
  const r = length3(pos);
  if (r < 1e-6) return 0;
  return dot3(vel, pos) / r;
}

export function tangentialVelocity(pos: Vector3, vel: Vector3, out: Vector3): number {
  const r = length3(pos);
  if (r < 1e-6) return 0;
  const vr = dot3(vel, pos) / r;
  out.copy(pos).multiplyScalar(vr / r);
  return length3(vel.clone().sub(out));
}

export function specificAngularMomentum(
  pos: Vector3,
  vel: Vector3,
  out: Vector3,
): Vector3 {
  return cross3(pos, vel, out);
}

export function specificEnergy(
  pos: Vector3,
  vel: Vector3,
  starMass: number,
  G: number,
): number {
  const r = length3(pos);
  const v2 = dot3(vel, vel);
  return 0.5 * v2 - (G * starMass) / Math.max(r, 0.01);
}

export function semiMajorAxis(energy: number, _angularMomentum: number, mu: number): number {
  if (Math.abs(energy) < 1e-10) return Infinity;
  return (-mu) / (2 * energy);
}

export function eccentricity(
  energy: number,
  angularMomentum: number,
  mu: number,
): number {
  const a = semiMajorAxis(energy, angularMomentum, mu);
  if (!isFinite(a) || a <= 0) return 1;
  const e2 = 1 + (2 * energy * angularMomentum * angularMomentum) / (mu * mu);
  return Math.sqrt(Math.max(0, e2));
}

export function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

export function randomInRange(min: number, max: number): number {
  return min + Math.random() * (max - min);
}

export function gaussianRandom(std: number): number {
  let u = 0;
  let v = 0;
  while (u === 0) u = Math.random();
  while (v === 0) v = Math.random();
  return std * Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
}
