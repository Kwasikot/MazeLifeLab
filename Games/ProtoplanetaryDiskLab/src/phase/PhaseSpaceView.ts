import { Vector3 } from "three";
import type { PhaseProjection, SimBody } from "../types";
import {
  eccentricity,
  radialVelocity,
  specificAngularMomentum,
  specificEnergy,
  length3,
} from "../utils/math";
import { G } from "../config/defaults";

export class PhaseSpaceView {
  private container: HTMLElement;
  private canvas: HTMLCanvasElement;
  private projection: PhaseProjection = "r-vr";
  private selectedId: number | null = null;
  private trails = new Map<number, [number, number][]>();
  private showTrails = true;
  private maxTrailLength = 200;
  private onSelect: ((id: number | null) => void) | null = null;
  private starMass = 1000;

  constructor(containerId: string) {
    const el = document.getElementById(containerId);
    if (!el) throw new Error(`Phase space container #${containerId} not found`);
    this.container = el;
    this.container.className = "phase-space-panel";

    const note = document.createElement("p");
    note.className = "phase-note";
    note.innerHTML =
      "Полная система представляется одной точкой в многомерном фазовом пространстве. " +
      "Отдельные точки на графике — проекции состояний отдельных объектов.";
    this.container.appendChild(note);

    const controls = document.createElement("div");
    controls.className = "phase-controls";
    const select = document.createElement("select");
    select.id = "phase-projection";
    const options: [PhaseProjection, string][] = [
      ["r-vr", "r — vᵣ"],
      ["r-vt", "r — vₜ"],
      ["x-vx", "x — vₓ"],
      ["L-E", "L — E"],
      ["a-e", "a — e"],
      ["m-Eorb", "m — Eₒᵣᵦ"],
    ];
    for (const [val, label] of options) {
      const opt = document.createElement("option");
      opt.value = val;
      opt.textContent = label;
      select.appendChild(opt);
    }
    select.addEventListener("change", () => {
      this.projection = select.value as PhaseProjection;
    });

    const trailCheck = document.createElement("label");
    trailCheck.innerHTML =
      '<input type="checkbox" id="phase-trails" checked> Следы';
    trailCheck.querySelector("input")?.addEventListener("change", (e) => {
      this.showTrails = (e.target as HTMLInputElement).checked;
    });

    controls.append(select, trailCheck);
    this.container.appendChild(controls);

    this.canvas = document.createElement("canvas");
    this.canvas.width = 500;
    this.canvas.height = 300;
    this.canvas.className = "phase-canvas";
    this.canvas.addEventListener("click", (e) => this.handleClick(e));
    this.container.appendChild(this.canvas);
  }

  setOnSelect(cb: (id: number | null) => void): void {
    this.onSelect = cb;
  }

  setStarMass(m: number): void {
    this.starMass = m;
  }

  selectBody(id: number | null): void {
    this.selectedId = id;
  }

  update(bodies: readonly SimBody[]): void {
    const active = bodies.filter((b) => b.active);
    const points: { id: number; x: number; y: number; mass: number }[] = [];

    for (const b of active) {
      const [x, y] = this.project(b);
      points.push({ id: b.id, x, y, mass: b.mass });

      if (this.showTrails) {
        let trail = this.trails.get(b.id);
        if (!trail) {
          trail = [];
          this.trails.set(b.id, trail);
        }
        trail.push([x, y]);
        if (trail.length > this.maxTrailLength) trail.shift();
      }
    }

    for (const id of this.trails.keys()) {
      if (!active.find((b) => b.id === id)) this.trails.delete(id);
    }

    this.draw(points);
  }

  private project(b: SimBody): [number, number] {
    const r = length3(b.position);
    const mu = G * this.starMass;

    switch (this.projection) {
      case "r-vr":
        return [r, radialVelocity(b.position, b.velocity)];
      case "r-vt": {
        const vt =
          b.velocity.length() - Math.abs(radialVelocity(b.position, b.velocity));
        return [r, vt];
      }
      case "x-vx":
        return [b.position.x, b.velocity.x];
      case "L-E": {
        const L = new Vector3();
        specificAngularMomentum(b.position, b.velocity, L);
        const E = specificEnergy(b.position, b.velocity, this.starMass, G);
        return [L.length(), E];
      }
      case "a-e": {
        const L = new Vector3();
        specificAngularMomentum(b.position, b.velocity, L);
        const E = specificEnergy(b.position, b.velocity, this.starMass, G);
        const a = (-mu) / (2 * E);
        const e = eccentricity(E, L.length(), mu);
        return [isFinite(a) ? a : 100, e];
      }
      case "m-Eorb": {
        const E = specificEnergy(b.position, b.velocity, this.starMass, G);
        return [b.mass, E];
      }
      default:
        return [r, 0];
    }
  }

  private draw(points: { id: number; x: number; y: number; mass: number }[]): void {
    const ctx = this.canvas.getContext("2d");
    if (!ctx) return;
    const { width, height } = this.canvas;
    ctx.clearRect(0, 0, width, height);

    ctx.fillStyle = "#111";
    ctx.fillRect(0, 0, width, height);

    if (points.length === 0) return;

    let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
    for (const p of points) {
      minX = Math.min(minX, p.x);
      maxX = Math.max(maxX, p.x);
      minY = Math.min(minY, p.y);
      maxY = Math.max(maxY, p.y);
    }
    const padX = (maxX - minX) * 0.1 || 1;
    const padY = (maxY - minY) * 0.1 || 1;
    minX -= padX; maxX += padX;
    minY -= padY; maxY += padY;

    const toX = (v: number) => ((v - minX) / (maxX - minX)) * (width - 40) + 20;
    const toY = (v: number) => height - 20 - ((v - minY) / (maxY - minY)) * (height - 40);

    ctx.strokeStyle = "#333";
    ctx.beginPath();
    ctx.moveTo(20, height - 20);
    ctx.lineTo(width - 20, height - 20);
    ctx.moveTo(20, 20);
    ctx.lineTo(20, height - 20);
    ctx.stroke();

    if (this.showTrails) {
      for (const [id, trail] of this.trails) {
        if (trail.length < 2) continue;
        ctx.strokeStyle = id === this.selectedId ? "#4af8" : "#fff2";
        ctx.lineWidth = 1;
        ctx.beginPath();
        for (let i = 0; i < trail.length; i++) {
          const [tx, ty] = trail[i];
          const px = toX(tx);
          const py = toY(ty);
          if (i === 0) ctx.moveTo(px, py);
          else ctx.lineTo(px, py);
        }
        ctx.stroke();
      }
    }

    for (const p of points) {
      const px = toX(p.x);
      const py = toY(p.y);
      const radius = Math.min(6, 2 + Math.log10(p.mass + 1));
      ctx.beginPath();
      ctx.arc(px, py, radius, 0, Math.PI * 2);
      ctx.fillStyle = p.id === this.selectedId ? "#4af" : `hsl(${30 + Math.log10(p.mass + 1) * 20}, 70%, 60%)`;
      ctx.fill();
    }

    this._points = points.map((p) => ({
      ...p,
      px: toX(p.x),
      py: toY(p.y),
    }));
  }

  private _points: { id: number; px: number; py: number }[] = [];

  private handleClick(e: MouseEvent): void {
    const rect = this.canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;
    let closest: number | null = null;
    let minDist = 15;
    for (const p of this._points) {
      const d = Math.hypot(p.px - mx, p.py - my);
      if (d < minDist) {
        minDist = d;
        closest = p.id;
      }
    }
    this.selectedId = closest;
    this.onSelect?.(closest);
  }
}
