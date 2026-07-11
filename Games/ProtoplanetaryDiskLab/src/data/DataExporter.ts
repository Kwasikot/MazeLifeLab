import type {
  EventLogEntry,
  ExportBundle,
  MetricsSnapshot,
  SerializedBody,
  SimulationParams,
} from "../types";
import type { SimBody } from "../types";

export class DataExporter {
  exportParams(params: SimulationParams): void {
    this.download(
      JSON.stringify(params, null, 2),
      "simulation-params.json",
      "application/json",
    );
  }

  exportMetricsCSV(history: MetricsSnapshot[]): void {
    if (history.length === 0) return;
    const keys = Object.keys(history[0]).filter(
      (k) => !["sizeDistribution", "massDistribution", "radialMassBins", "topProtoplanetMasses"].includes(k),
    ) as (keyof MetricsSnapshot)[];
    const header = keys.join(",");
    const rows = history.map((row) =>
      keys.map((k) => String(row[k])).join(","),
    );
    this.download([header, ...rows].join("\n"), "metrics-timeseries.csv", "text/csv");
  }

  exportState(bodies: readonly SimBody[]): void {
    const serialized: SerializedBody[] = bodies
      .filter((b) => b.active)
      .map((b) => ({
        id: b.id,
        x: b.position.x,
        y: b.position.y,
        z: b.position.z,
        vx: b.velocity.x,
        vy: b.velocity.y,
        vz: b.velocity.z,
        mass: b.mass,
        radius: b.radius,
        material: b.material,
        age: b.age,
      }));
    this.download(
      JSON.stringify(serialized, null, 2),
      "simulation-state.json",
      "application/json",
    );
  }

  exportScreenshot(dataUrl: string): void {
    const a = document.createElement("a");
    a.href = dataUrl;
    a.download = "screenshot.png";
    a.click();
  }

  exportReport(bundle: ExportBundle): void {
    this.download(bundle.report, "experiment-report.txt", "text/plain");
  }

  buildReport(
    params: SimulationParams,
    metrics: MetricsSnapshot[],
    events: EventLogEntry[],
  ): string {
    const last = metrics.at(-1);
    const lines = [
      "=== Protoplanetary Disk Lab — Experiment Report ===",
      `Date: ${new Date().toISOString()}`,
      "",
      "--- Parameters ---",
      `Physics mode: ${params.physicsMode}`,
      `Particles: ${params.particleCount}`,
      `Star mass: ${params.starMass}`,
      `Material: ${params.materialType}`,
      "",
      "--- Final Metrics ---",
      last
        ? [
            `Sim time: ${last.time.toFixed(2)}`,
            `Objects: ${last.objectCount}`,
            `Largest mass: ${last.largestMass.toFixed(4)}`,
            `Merges: ${last.mergeCount}`,
            `Energy drift: ${(last.energyDrift * 100).toFixed(2)}%`,
            `Angular momentum drift: ${(last.angularMomentumDrift * 100).toFixed(2)}%`,
          ].join("\n")
        : "No metrics collected",
      "",
      "--- Recent Events ---",
      ...events.slice(-10).map((e) => `[${e.time.toFixed(2)}] ${e.type}: ${e.message}`),
      "",
      "--- Disclaimers ---",
      "This is an educational simplified model, not a rigorous astrophysical simulation.",
      "Numerical drift in energy and angular momentum is expected.",
    ];
    return lines.join("\n");
  }

  private download(content: string, filename: string, mime: string): void {
    const blob = new Blob([content], { type: mime });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
