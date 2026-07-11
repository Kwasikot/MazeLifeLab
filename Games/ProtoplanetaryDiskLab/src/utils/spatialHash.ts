import type { SimBody } from "../types";
import { Vector3 } from "three";

export class SpatialHash {
  private cellSize: number;
  private buckets = new Map<string, number[]>();

  constructor(cellSize: number) {
    this.cellSize = cellSize;
  }

  clear(): void {
    this.buckets.clear();
  }

  private key(x: number, y: number, z: number): string {
    const cx = Math.floor(x / this.cellSize);
    const cy = Math.floor(y / this.cellSize);
    const cz = Math.floor(z / this.cellSize);
    return `${cx},${cy},${cz}`;
  }

  insert(index: number, pos: Vector3): void {
    const k = this.key(pos.x, pos.y, pos.z);
    let bucket = this.buckets.get(k);
    if (!bucket) {
      bucket = [];
      this.buckets.set(k, bucket);
    }
    bucket.push(index);
  }

  query(pos: Vector3, radius: number, out: number[]): void {
    out.length = 0;
    const range = Math.ceil(radius / this.cellSize);
    const cx = Math.floor(pos.x / this.cellSize);
    const cy = Math.floor(pos.y / this.cellSize);
    const cz = Math.floor(pos.z / this.cellSize);

    for (let dx = -range; dx <= range; dx++) {
      for (let dy = -range; dy <= range; dy++) {
        for (let dz = -range; dz <= range; dz++) {
          const bucket = this.buckets.get(`${cx + dx},${cy + dy},${cz + dz}`);
          if (bucket) out.push(...bucket);
        }
      }
    }
  }

  build(bodies: SimBody[]): void {
    this.clear();
    for (let i = 0; i < bodies.length; i++) {
      const b = bodies[i];
      if (b.active) this.insert(i, b.position);
    }
  }
}
