import type { Preset } from "../types";

export const PRESETS: Preset[] = [
  {
    id: "stable-keplerian",
    name: "Stable Keplerian Disk",
    description: "Стабильный кеплеровский диск с минимальными столкновениями",
    params: {
      physicsMode: 1,
      particleCount: 3000,
      velocityDispersion: 0.05,
      collisionIntensity: 0.2,
      stickProbability: 0.3,
      simulationSpeed: 1.0,
    },
  },
  {
    id: "high-collision",
    name: "High Collision Rate",
    description: "Высокая частота столкновений",
    params: {
      physicsMode: 2,
      particleCount: 2500,
      collisionIntensity: 2.5,
      stickProbability: 0.5,
      criticalVelocity: 0.8,
    },
  },
  {
    id: "rapid-accretion",
    name: "Rapid Accretion",
    description: "Быстрая аккреция и слияние частиц",
    params: {
      physicsMode: 2,
      particleCount: 2000,
      stickProbability: 0.95,
      criticalVelocity: 2.0,
      collisionIntensity: 1.5,
      materialType: "dust",
    },
  },
  {
    id: "chaotic-disk",
    name: "Chaotic Disk",
    description: "Хаотичный диск с высокой дисперсией скоростей",
    params: {
      physicsMode: 2,
      particleCount: 2000,
      velocityDispersion: 0.5,
      collisionIntensity: 2.0,
      stickProbability: 0.4,
    },
  },
  {
    id: "few-protoplanets",
    name: "Few Large Protoplanets",
    description: "Немного крупных протопланет среди мелких частиц",
    params: {
      physicsMode: 2,
      particleCount: 1500,
      largeBodyInteraction: 2.0,
      stickProbability: 0.8,
      materialType: "rock",
    },
  },
  {
    id: "nbody-experiment",
    name: "N-body Experiment",
    description: "Экспериментальный N-body режим (мало объектов)",
    params: {
      physicsMode: 3,
      particleCount: 80,
      integrator: "rk4",
      integrationStep: 0.01,
      nBodyLimit: 80,
      largeBodyInteraction: 3.0,
    },
  },
];
