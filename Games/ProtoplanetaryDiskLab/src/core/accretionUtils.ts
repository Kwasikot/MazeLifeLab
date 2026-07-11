import { MATERIALS } from "../config/defaults";
import { radiusFromMass } from "../utils/math";
import type { SimBody, MaterialType } from "../types";

export function estimateDiskSpacing(
  innerRadius: number,
  outerRadius: number,
  particleCount: number,
): number {
  const area = Math.PI * (outerRadius * outerRadius - innerRadius * innerRadius);
  return Math.sqrt(Math.max(area / particleCount, 0.5));
}

export function collisionRadiusForBody(
  physicalRadius: number,
  avgSpacing: number,
  massRatio = 1,
): number {
  const base = avgSpacing * 0.38 * Math.cbrt(massRatio);
  return Math.max(physicalRadius * 2, base);
}

export function updateBodyCollisionRadius(body: SimBody): void {
  const density = MATERIALS[body.material].density;
  body.radius = radiusFromMass(body.mass, density);
  body.collisionRadius = Math.max(
    body.radius * 1.5,
    body.collisionRadius * 0.85 + body.radius * 2.2 * 0.15,
  );
}

export function isProtoplanet(mass: number, starMass: number): boolean {
  return mass >= starMass * 0.0008;
}

export function isPlanet(mass: number, starMass: number): boolean {
  return mass >= starMass * 0.003;
}

export function materialForMerge(a: MaterialType, b: MaterialType): MaterialType {
  const order: MaterialType[] = ["dust", "ice", "rock", "metal"];
  return order.indexOf(a) >= order.indexOf(b) ? a : b;
}
