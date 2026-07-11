import type { SimulationParams, SimulationState } from "../types";
import { DEFAULT_PARAMS, MATERIALS } from "../config/defaults";
import { PRESETS } from "../config/presets";

export interface ControlCallbacks {
  onStart: () => void;
  onPause: () => void;
  onReset: () => void;
  onSingleStep: () => void;
  onGenerateDisk: () => void;
  onParamChange: (partial: Partial<SimulationParams>) => void;
  onPreset: (id: string) => void;
  onSaveParams: () => void;
  onLoadParams: () => void;
  onExport: (type: "params" | "metrics" | "state" | "screenshot" | "report") => void;
  onCameraMode: (mode: "free" | "top" | "side" | "track") => void;
}

type SliderSpec = {
  key: keyof SimulationParams;
  label: string;
  min: number;
  max: number;
  step: number;
};

const SLIDERS: SliderSpec[] = [
  { key: "particleCount", label: "Число частиц", min: 100, max: 5000, step: 100 },
  { key: "starMass", label: "Масса звезды", min: 100, max: 5000, step: 50 },
  { key: "diskMass", label: "Масса диска", min: 1, max: 200, step: 1 },
  { key: "innerRadius", label: "Внутр. радиус", min: 1, max: 20, step: 0.5 },
  { key: "outerRadius", label: "Внешн. радиус", min: 10, max: 80, step: 1 },
  { key: "diskThickness", label: "Толщина диска", min: 0.1, max: 3, step: 0.1 },
  { key: "velocityDispersion", label: "Дисперсия скоростей", min: 0, max: 1, step: 0.01 },
  { key: "collisionIntensity", label: "Интенсивность столкновений", min: 0, max: 3, step: 0.1 },
  { key: "stickProbability", label: "Вероятность слипания", min: 0, max: 1, step: 0.05 },
  { key: "criticalVelocity", label: "Крит. скорость", min: 0.1, max: 5, step: 0.1 },
  { key: "largeBodyInteraction", label: "Взаимодействие крупных тел", min: 0, max: 5, step: 0.1 },
  { key: "timeScale", label: "Временной масштаб", min: 0.1, max: 5, step: 0.1 },
  { key: "integrationStep", label: "Шаг интегрирования", min: 0.005, max: 0.1, step: 0.005 },
  { key: "visualScale", label: "Визуальный масштаб", min: 0.5, max: 10, step: 0.5 },
  { key: "simulationSpeed", label: "Скорость симуляции", min: 1, max: 10, step: 1 },
  { key: "diskThicknessExaggeration", label: "Преувеличение толщины", min: 1, max: 20, step: 1 },
];

export class SimulationControls {
  private container: HTMLElement;
  private params: SimulationParams;
  private callbacks: ControlCallbacks;
  private statusEl!: HTMLElement;
  private metricsEl!: HTMLElement;
  private logEl!: HTMLElement;
  private sliderInputs = new Map<keyof SimulationParams, HTMLInputElement>();

  constructor(containerId: string, callbacks: ControlCallbacks) {
    const el = document.getElementById(containerId);
    if (!el) throw new Error(`Controls container #${containerId} not found`);
    this.container = el;
    this.callbacks = callbacks;
    this.params = { ...DEFAULT_PARAMS };
    this.build();
  }

  getParams(): SimulationParams {
    return { ...this.params };
  }

  setParams(p: SimulationParams): void {
    this.params = { ...p };
    for (const [key, input] of this.sliderInputs) {
      const val = this.params[key];
      if (typeof val === "number") {
        input.value = String(val);
        const label = input.parentElement?.querySelector(".val");
        if (label) label.textContent = String(val);
      }
    }
    const modeSelect = this.container.querySelector("#physics-mode") as HTMLSelectElement;
    if (modeSelect) modeSelect.value = String(p.physicsMode);
    const matSelect = this.container.querySelector("#material-type") as HTMLSelectElement;
    if (matSelect) matSelect.value = p.materialType;
    const qualSelect = this.container.querySelector("#quality") as HTMLSelectElement;
    if (qualSelect) qualSelect.value = p.quality;
  }

  updateStatus(state: SimulationState, fps: number, physicsMs: number, renderer: string): void {
    this.statusEl.textContent =
      `Состояние: ${state} | FPS: ${fps.toFixed(0)} | Физика: ${physicsMs.toFixed(1)} мс | Рендер: ${renderer}`;
  }

  updateMetrics(text: string): void {
    this.metricsEl.textContent = text;
  }

  appendLog(message: string): void {
    const line = document.createElement("div");
    line.className = "log-line";
    line.textContent = message;
    this.logEl.prepend(line);
    while (this.logEl.children.length > 50) {
      this.logEl.lastChild?.remove();
    }
  }

  private build(): void {
    this.container.className = "sidebar-controls";

    const buttons = document.createElement("div");
    buttons.className = "btn-row";
    const btnDefs: [string, () => void][] = [
      ["Start", () => this.callbacks.onStart()],
      ["Pause", () => this.callbacks.onPause()],
      ["Reset", () => this.callbacks.onReset()],
      ["Single Step", () => this.callbacks.onSingleStep()],
      ["Generate New Disk", () => this.callbacks.onGenerateDisk()],
    ];
    for (const [label, handler] of btnDefs) {
      const btn = document.createElement("button");
      btn.textContent = label;
      btn.addEventListener("click", handler);
      buttons.appendChild(btn);
    }
    this.container.appendChild(buttons);

    const presetSection = document.createElement("div");
    presetSection.className = "section";
    presetSection.innerHTML = "<h3>Пресеты</h3>";
    const presetSelect = document.createElement("select");
    presetSelect.innerHTML = '<option value="">— выберите —</option>';
    for (const p of PRESETS) {
      const opt = document.createElement("option");
      opt.value = p.id;
      opt.textContent = p.name;
      opt.title = p.description;
      presetSelect.appendChild(opt);
    }
    presetSelect.addEventListener("change", () => {
      if (presetSelect.value) this.callbacks.onPreset(presetSelect.value);
    });
    presetSection.appendChild(presetSelect);
    this.container.appendChild(presetSection);

    const modeSection = document.createElement("div");
    modeSection.className = "section";
    modeSection.innerHTML = "<h3>Режим физики</h3>";
    const modeSelect = document.createElement("select");
    modeSelect.id = "physics-mode";
    modeSelect.innerHTML = `
      <option value="1">1 — Визуальная демонстрация</option>
      <option value="2">2 — Упрощённая физика</option>
      <option value="3">3 — Экспериментальный N-body</option>
    `;
    modeSelect.value = String(this.params.physicsMode);
    modeSelect.addEventListener("change", () => {
      const mode = Number(modeSelect.value) as 1 | 2 | 3;
      this.params.physicsMode = mode;
      this.callbacks.onParamChange({ physicsMode: mode });
      if (mode === 3) {
        this.appendLog("⚠ Режим 3: возможна численная нестабильность при большом шаге.");
      }
    });
    modeSection.appendChild(modeSelect);

    const matSelect = document.createElement("select");
    matSelect.id = "material-type";
    for (const [key, mat] of Object.entries(MATERIALS)) {
      const opt = document.createElement("option");
      opt.value = key;
      opt.textContent = mat.name;
      matSelect.appendChild(opt);
    }
    matSelect.addEventListener("change", () => {
      this.params.materialType = matSelect.value as SimulationParams["materialType"];
      this.callbacks.onParamChange({ materialType: this.params.materialType });
    });
    modeSection.appendChild(matSelect);

    const qualSelect = document.createElement("select");
    qualSelect.id = "quality";
    qualSelect.innerHTML = `
      <option value="low">Low</option>
      <option value="medium">Medium</option>
      <option value="high">High</option>
    `;
    qualSelect.addEventListener("change", () => {
      this.params.quality = qualSelect.value as SimulationParams["quality"];
      this.callbacks.onParamChange({ quality: this.params.quality });
    });
    modeSection.appendChild(qualSelect);
    this.container.appendChild(modeSection);

    const sliderSection = document.createElement("div");
    sliderSection.className = "section sliders";
    sliderSection.innerHTML = "<h3>Параметры</h3>";
    for (const spec of SLIDERS) {
      const row = document.createElement("label");
      row.className = "slider-row";
      row.innerHTML = `<span>${spec.label}</span> <span class="val">${this.params[spec.key]}</span>`;
      const input = document.createElement("input");
      input.type = "range";
      input.min = String(spec.min);
      input.max = String(spec.max);
      input.step = String(spec.step);
      input.value = String(this.params[spec.key]);
      input.addEventListener("input", () => {
        const val = Number(input.value);
        (this.params as unknown as Record<string, number>)[spec.key] = val;
        row.querySelector(".val")!.textContent = String(val);
        this.callbacks.onParamChange({ [spec.key]: val });
      });
      this.sliderInputs.set(spec.key, input);
      row.appendChild(input);
      sliderSection.appendChild(row);
    }
    this.container.appendChild(sliderSection);

    const camSection = document.createElement("div");
    camSection.className = "section";
    camSection.innerHTML = "<h3>Камера</h3>";
    for (const [label, mode] of [
      ["Свободная", "free"],
      ["Сверху", "top"],
      ["Сбоку", "side"],
    ] as const) {
      const btn = document.createElement("button");
      btn.textContent = label;
      btn.addEventListener("click", () => this.callbacks.onCameraMode(mode));
      camSection.appendChild(btn);
    }
    this.container.appendChild(camSection);

    const dataSection = document.createElement("div");
    dataSection.className = "section";
    dataSection.innerHTML = "<h3>Данные</h3>";
    const dataBtns: [string, "params" | "metrics" | "state" | "screenshot" | "report"][] = [
      ["Save Parameters", "params"],
      ["Load Parameters", "params"],
      ["Export Params JSON", "params"],
      ["Export Metrics CSV", "metrics"],
      ["Export State JSON", "state"],
      ["Screenshot", "screenshot"],
      ["Export Report", "report"],
    ];
    for (const [label, type] of dataBtns) {
      const btn = document.createElement("button");
      btn.textContent = label;
      if (label === "Save Parameters") {
        btn.addEventListener("click", () => this.callbacks.onSaveParams());
      } else if (label === "Load Parameters") {
        btn.addEventListener("click", () => this.callbacks.onLoadParams());
      } else {
        btn.addEventListener("click", () => this.callbacks.onExport(type));
      }
      dataSection.appendChild(btn);
    }
    this.container.appendChild(dataSection);

    this.statusEl = document.createElement("div");
    this.statusEl.className = "status-bar";
    this.container.appendChild(this.statusEl);

    this.metricsEl = document.createElement("div");
    this.metricsEl.className = "metrics-readout";
    this.container.appendChild(this.metricsEl);

    const logSection = document.createElement("div");
    logSection.className = "section log-section";
    logSection.innerHTML = "<h3>Журнал</h3>";
    this.logEl = document.createElement("div");
    this.logEl.className = "event-log";
    logSection.appendChild(this.logEl);
    this.container.appendChild(logSection);
  }
}
