import type { Vector3 } from "three";

export type MaterialType = "dust" | "rock" | "ice" | "metal";
export type PhysicsMode = 1 | 2 | 3;
export type QualityLevel = "low" | "medium" | "high";
export type PhaseProjection =
  | "r-vr"
  | "r-vt"
  | "x-vx"
  | "L-E"
  | "a-e"
  | "m-Eorb";

export type SimulationState = "idle" | "running" | "paused";

export interface MaterialProperties {
  name: string;
  density: number;
  stickProbability: number;
  criticalVelocity: number;
  color: number;
}

export interface SimBody {
  id: number;
  position: Vector3;
  velocity: Vector3;
  mass: number;
  radius: number;
  /** Effective radius for collision / accretion (educational scale, >> physical). */
  collisionRadius: number;
  visualRadius: number;
  material: MaterialType;
  age: number;
  active: boolean;
  isLarge: boolean;
}

export interface SimulationParams {
  particleCount: number;
  starMass: number;
  diskMass: number;
  innerRadius: number;
  outerRadius: number;
  diskThickness: number;
  velocityDispersion: number;
  collisionIntensity: number;
  stickProbability: number;
  criticalVelocity: number;
  largeBodyInteraction: number;
  timeScale: number;
  integrationStep: number;
  visualScale: number;
  simulationSpeed: number;
  physicsMode: PhysicsMode;
  materialType: MaterialType;
  quality: QualityLevel;
  diskThicknessExaggeration: number;
  integrator: "euler" | "verlet" | "rk4";
  nBodyLimit: number;
}

export interface CollisionEvent {
  time: number;
  bodyA: number;
  bodyB: number;
  relativeSpeed: number;
  merged: boolean;
}

export interface MergeEvent {
  time: number;
  bodyIds: [number, number];
  newMass: number;
  newRadius: number;
}

export interface SimulationMetrics {
  time: number;
  objectCount: number;
  largestMass: number;
  avgCollisionSpeed: number;
  collisionsPerSimSecond: number;
  mergeCount: number;
  kineticEnergy: number;
  potentialEnergy: number;
  totalEnergy: number;
  angularMomentum: number;
  initialEnergy: number;
  initialAngularMomentum: number;
  energyDrift: number;
  angularMomentumDrift: number;
}

export interface MetricsSnapshot extends SimulationMetrics {
  sizeDistribution: number[];
  massDistribution: number[];
  radialMassBins: number[];
  topProtoplanetMasses: number[];
}

export interface EventLogEntry {
  time: number;
  type: "collision" | "merge" | "info" | "warning";
  message: string;
}

export interface StateFeatureVector {
  time: number;
  objectCount: number;
  massDistribution: number[];
  radialDistribution: number[];
  meanSpeed: number;
  energy: number;
  angularMomentum: number;
  largeBodyCount: number;
  orbitParams: number[];
}

export interface Preset {
  id: string;
  name: string;
  description: string;
  params: Partial<SimulationParams>;
}

export interface ExportBundle {
  params: SimulationParams;
  metrics: MetricsSnapshot[];
  bodies: SerializedBody[];
  events: EventLogEntry[];
  report: string;
}

export interface SerializedBody {
  id: number;
  x: number;
  y: number;
  z: number;
  vx: number;
  vy: number;
  vz: number;
  mass: number;
  radius: number;
  material: MaterialType;
  age: number;
}
