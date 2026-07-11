import type { MaterialProperties, MaterialType, SimulationParams } from "../types";

export const MATERIALS: Record<MaterialType, MaterialProperties> = {
  dust: {
    name: "Пыль",
    density: 1.0,
    stickProbability: 0.9,
    criticalVelocity: 0.5,
    color: 0xc4a882,
  },
  rock: {
    name: "Камень",
    density: 2.5,
    stickProbability: 0.6,
    criticalVelocity: 1.5,
    color: 0x8b7355,
  },
  ice: {
    name: "Лёд",
    density: 0.9,
    stickProbability: 0.75,
    criticalVelocity: 0.8,
    color: 0xa8d8ea,
  },
  metal: {
    name: "Металл",
    density: 7.8,
    stickProbability: 0.4,
    criticalVelocity: 3.0,
    color: 0xb0b0b0,
  },
};

export const DEFAULT_PARAMS: SimulationParams = {
  particleCount: 1200,
  starMass: 1000,
  diskMass: 50,
  innerRadius: 5,
  outerRadius: 40,
  diskThickness: 0.5,
  velocityDispersion: 0.15,
  collisionIntensity: 1.3,
  stickProbability: 0.85,
  criticalVelocity: 2.5,
  largeBodyInteraction: 1.2,
  timeScale: 1.0,
  integrationStep: 0.02,
  visualScale: 8.0,
  simulationSpeed: 1.0,
  physicsMode: 2,
  materialType: "dust",
  quality: "medium",
  diskThicknessExaggeration: 5.0,
  integrator: "verlet",
  nBodyLimit: 50,
};

export const G = 1.0;
export const LARGE_BODY_MASS_RATIO = 0.005;
export const MIN_RADIUS = 0.02;
export const MAX_VISUAL_RADIUS = 2.0;
