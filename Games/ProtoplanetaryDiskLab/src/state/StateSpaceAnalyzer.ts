import type { MetricsSnapshot, StateFeatureVector } from "../types";

interface PcaResult {
  components: number[][];
  mean: number[];
  explainedVariance: number[];
}

export class StateSpaceAnalyzer {
  private container: HTMLElement;
  private canvas: HTMLCanvasElement;
  private snapshots: StateFeatureVector[] = [];
  private maxSnapshots = 200;
  private snapshotInterval = 0.5;
  private lastSnapshotTime = -Infinity;

  constructor(containerId: string) {
    const el = document.getElementById(containerId);
    if (!el) throw new Error(`State space container #${containerId} not found`);
    this.container = el;
    this.container.className = "state-space-panel";

    const header = document.createElement("h3");
    header.textContent = "State-Space Explorer";
    this.container.appendChild(header);

    const note = document.createElement("div");
    note.className = "state-note";
    note.innerHTML = `
      <p><strong>Фазовое пространство</strong> задаётся физическими переменными состояния (положение, скорость, масса).</p>
      <p><strong>Латентное пространство</strong> — выученное или сконструированное сжатое представление (PCA, автоэнкодер и т.д.).</p>
      <p>Между ними существует концептуальная аналогия, но это <em>не одно и то же</em>.</p>
    `;
    this.container.appendChild(note);

    this.canvas = document.createElement("canvas");
    this.canvas.width = 400;
    this.canvas.height = 250;
    this.canvas.className = "state-canvas";
    this.container.appendChild(this.canvas);
  }

  maybeSnapshot(metrics: MetricsSnapshot): void {
    if (metrics.time - this.lastSnapshotTime < this.snapshotInterval) return;
    this.lastSnapshotTime = metrics.time;

    const features = this.extractFeatures(metrics);
    this.snapshots.push(features);
    if (this.snapshots.length > this.maxSnapshots) this.snapshots.shift();
    this.draw();
  }

  extractFeatures(m: MetricsSnapshot): StateFeatureVector {
    return {
      time: m.time,
      objectCount: m.objectCount,
      massDistribution: m.massDistribution,
      radialDistribution: m.radialMassBins,
      meanSpeed: m.avgCollisionSpeed,
      energy: m.totalEnergy,
      angularMomentum: m.angularMomentum,
      largeBodyCount: m.topProtoplanetMasses.filter((x) => x > 0.01).length,
      orbitParams: [m.largestMass, m.mergeCount, m.collisionsPerSimSecond],
    };
  }

  private featureMatrix(): number[][] {
    return this.snapshots.map((s) => [
      s.objectCount,
      ...s.massDistribution.slice(0, 5),
      ...s.radialDistribution.slice(0, 5),
      s.meanSpeed,
      s.energy,
      s.angularMomentum,
      s.largeBodyCount,
      ...s.orbitParams,
    ]);
  }

  private pca(data: number[][], dims: number): PcaResult | null {
    if (data.length < 3) return null;
    const n = data.length;
    const d = data[0].length;

    const mean = new Array(d).fill(0);
    for (const row of data) {
      for (let j = 0; j < d; j++) mean[j] += row[j];
    }
    for (let j = 0; j < d; j++) mean[j] /= n;

    const centered = data.map((row) => row.map((v, j) => v - mean[j]));

    const cov = Array.from({ length: d }, () => new Array(d).fill(0));
    for (let i = 0; i < d; i++) {
      for (let j = 0; j < d; j++) {
        let sum = 0;
        for (const row of centered) sum += row[i] * row[j];
        cov[i][j] = sum / (n - 1);
      }
    }

    const components: number[][] = [];
    const explainedVariance: number[] = [];

    for (let dim = 0; dim < dims; dim++) {
      let vec = new Array(d).fill(0).map(() => Math.random() - 0.5);
      for (let iter = 0; iter < 50; iter++) {
        const newVec = new Array(d).fill(0);
        for (let i = 0; i < d; i++) {
          for (let j = 0; j < d; j++) {
            newVec[i] += cov[i][j] * vec[j];
          }
        }
        const norm = Math.sqrt(newVec.reduce((s, v) => s + v * v, 0)) || 1;
        vec = newVec.map((v) => v / norm);

        for (const prev of components) {
          let dot = 0;
          for (let k = 0; k < d; k++) dot += vec[k] * prev[k];
          for (let k = 0; k < d; k++) vec[k] -= dot * prev[k];
        }
      }
      components.push(vec);

      let variance = 0;
      for (let i = 0; i < d; i++) {
        let row = 0;
        for (let j = 0; j < d; j++) row += cov[i][j] * vec[j];
        variance += vec[i] * row;
      }
      explainedVariance.push(variance);
    }

    return { components, mean, explainedVariance };
  }

  private draw(): void {
    const ctx = this.canvas.getContext("2d");
    if (!ctx) return;
    const { width, height } = this.canvas;
    ctx.clearRect(0, 0, width, height);
    ctx.fillStyle = "#0a0a14";
    ctx.fillRect(0, 0, width, height);

    if (this.snapshots.length < 3) {
      ctx.fillStyle = "#888";
      ctx.font = "12px sans-serif";
      ctx.fillText("Сбор данных для PCA...", 20, height / 2);
      return;
    }

    const matrix = this.featureMatrix();
    const result = this.pca(matrix, 2);
    if (!result) return;

    const projected = matrix.map((row) => {
      const centered = row.map((v, j) => v - result.mean[j]);
      const x = centered.reduce((s, v, j) => s + v * result.components[0][j], 0);
      const y = centered.reduce((s, v, j) => s + v * result.components[1][j], 0);
      return [x, y] as [number, number];
    });

    let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
    for (const [x, y] of projected) {
      minX = Math.min(minX, x); maxX = Math.max(maxX, x);
      minY = Math.min(minY, y); maxY = Math.max(maxY, y);
    }
    const pad = 0.1;
    const rx = maxX - minX || 1;
    const ry = maxY - minY || 1;
    minX -= rx * pad; maxX += rx * pad;
    minY -= ry * pad; maxY += ry * pad;

    const toX = (v: number) => ((v - minX) / (maxX - minX)) * (width - 40) + 20;
    const toY = (v: number) => height - 20 - ((v - minY) / (maxY - minY)) * (height - 40);

    ctx.strokeStyle = "#4af6";
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    for (let i = 0; i < projected.length; i++) {
      const [x, y] = projected[i];
      const px = toX(x);
      const py = toY(y);
      if (i === 0) ctx.moveTo(px, py);
      else ctx.lineTo(px, py);
    }
    ctx.stroke();

    const last = projected[projected.length - 1];
    ctx.fillStyle = "#f44";
    ctx.beginPath();
    ctx.arc(toX(last[0]), toY(last[1]), 5, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = "#aaa";
    ctx.font = "11px sans-serif";
    const ev = result.explainedVariance;
    const total = ev.reduce((a, b) => a + b, 0) || 1;
    ctx.fillText(
      `PCA: ${((ev[0] / total) * 100).toFixed(0)}% + ${((ev[1] / total) * 100).toFixed(0)}% variance`,
      20,
      16,
    );
  }

  clear(): void {
    this.snapshots = [];
    this.lastSnapshotTime = -Infinity;
  }
}
