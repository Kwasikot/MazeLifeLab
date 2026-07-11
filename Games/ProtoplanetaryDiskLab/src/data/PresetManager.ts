import type { Preset, SimulationParams } from "../types";
import { PRESETS } from "../config/presets";
import { DEFAULT_PARAMS } from "../config/defaults";

export class PresetManager {
  getPresets(): Preset[] {
    return PRESETS;
  }

  applyPreset(id: string): Partial<SimulationParams> | null {
    const preset = PRESETS.find((p) => p.id === id);
    return preset ? { ...preset.params } : null;
  }

  saveParams(params: SimulationParams): string {
    const json = JSON.stringify(params, null, 2);
    localStorage.setItem("ppd-lab-params", json);
    return json;
  }

  loadParams(): SimulationParams | null {
    const raw = localStorage.getItem("ppd-lab-params");
    if (!raw) return null;
    try {
      return { ...DEFAULT_PARAMS, ...JSON.parse(raw) };
    } catch {
      return null;
    }
  }
}
