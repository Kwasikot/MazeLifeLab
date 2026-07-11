import type { MergeEvent, SimBody } from "../types";
import { MAX_VISUAL_RADIUS } from "../config/defaults";
import { isPlanet, isProtoplanet } from "./accretionUtils";

export class AccretionSystem {
  private mergeLog: MergeEvent[] = [];

  getMergeLog(): MergeEvent[] {
    return this.mergeLog;
  }

  clearLog(): void {
    this.mergeLog = [];
  }

  updateVisualRadii(bodies: SimBody[], visualScale: number, starMass: number): void {
    for (const b of bodies) {
      if (!b.active) continue;
      let scale = visualScale;
      if (isPlanet(b.mass, starMass)) scale *= 2.2;
      else if (isProtoplanet(b.mass, starMass)) scale *= 1.5;
      else if (b.isLarge) scale *= 1.2;

      b.visualRadius = Math.max(
        0.15,
        Math.min(b.radius * scale, MAX_VISUAL_RADIUS),
      );
    }
  }

  classifyLargeBodies(bodies: SimBody[], starMass: number): void {
    const threshold = starMass * 0.0008;
    for (const b of bodies) {
      if (!b.active) continue;
      b.isLarge = b.mass >= threshold;
    }
  }

  recordMerge(event: MergeEvent): void {
    this.mergeLog.push(event);
  }

  getLargestMass(bodies: SimBody[]): number {
    let max = 0;
    for (const b of bodies) {
      if (b.active && b.mass > max) max = b.mass;
    }
    return max;
  }

  getTopProtoplanets(bodies: SimBody[], count: number): number[] {
    const masses = bodies
      .filter((b) => b.active)
      .map((b) => b.mass)
      .sort((a, b) => b - a)
      .slice(0, count);
    return masses;
  }
}
