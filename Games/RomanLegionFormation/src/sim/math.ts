import type { Agent } from "../types";

export class SpatialHash {
  private readonly cellSize: number;
  private buckets = new Map<string, number[]>();

  constructor(cellSize: number) {
    this.cellSize = cellSize;
  }

  clear(): void {
    this.buckets.clear();
  }

  insert(agent: Agent): void {
    const key = this.key(agent.x, agent.y);
    let bucket = this.buckets.get(key);
    if (!bucket) {
      bucket = [];
      this.buckets.set(key, bucket);
    }
    bucket.push(agent.id);
  }

  queryRadius(x: number, y: number, radius: number, agents: Agent[], out: number[]): void {
    out.length = 0;
    const r = Math.ceil(radius / this.cellSize);
    const cx = Math.floor(x / this.cellSize);
    const cy = Math.floor(y / this.cellSize);
    const r2 = radius * radius;
    for (let dy = -r; dy <= r; dy++) {
      for (let dx = -r; dx <= r; dx++) {
        const bucket = this.buckets.get(`${cx + dx},${cy + dy}`);
        if (!bucket) continue;
        for (const id of bucket) {
          const a = agents[id];
          const ddx = a.x - x;
          const ddy = a.y - y;
          if (ddx * ddx + ddy * ddy <= r2) out.push(id);
        }
      }
    }
  }

  private key(x: number, y: number): string {
    return `${Math.floor(x / this.cellSize)},${Math.floor(y / this.cellSize)}`;
  }
}

export function normalizeAngle(a: number): number {
  while (a > Math.PI) a -= Math.PI * 2;
  while (a < -Math.PI) a += Math.PI * 2;
  return a;
}

export function turnToward(current: number, target: number, maxStep: number): number {
  const diff = normalizeAngle(target - current);
  if (Math.abs(diff) <= maxStep) return target;
  return current + Math.sign(diff) * maxStep;
}

export function distance(x1: number, y1: number, x2: number, y2: number): number {
  const dx = x2 - x1;
  const dy = y2 - y1;
  return Math.sqrt(dx * dx + dy * dy);
}
