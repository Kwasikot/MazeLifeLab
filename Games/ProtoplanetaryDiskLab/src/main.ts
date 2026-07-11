import "./style.css";
import { SimulationEngine } from "./core/SimulationEngine";
import { SceneManager } from "./rendering/SceneManager";
import { MetricsCollector } from "./metrics/MetricsCollector";
import { ChartManager } from "./metrics/ChartManager";
import { PhaseSpaceView } from "./phase/PhaseSpaceView";
import { StateSpaceAnalyzer } from "./state/StateSpaceAnalyzer";
import { SimulationControls } from "./ui/SimulationControls";
import { PresetManager } from "./data/PresetManager";
import { DataExporter } from "./data/DataExporter";

async function main(): Promise<void> {
  const canvas = document.getElementById("viewport") as HTMLCanvasElement;
  if (!canvas) throw new Error("Canvas not found");

  const engine = new SimulationEngine();
  const scene = new SceneManager(canvas, engine.getParams());
  const rendererType = await scene.init();

  const metrics = new MetricsCollector();
  const charts = new ChartManager("charts-panel");
  const phaseSpace = new PhaseSpaceView("phase-space-panel");
  const stateSpace = new StateSpaceAnalyzer("state-space-panel");
  const presets = new PresetManager();
  const exporter = new DataExporter();

  phaseSpace.setOnSelect((id) => {
    if (id !== null) {
      const body = engine.getBodies().find((b) => b.id === id);
      if (body) scene.setTrackTarget(body);
    }
  });
  let lastFpsTime = performance.now();
  let frameCount = 0;
  let fps = 0;
  let autoQualityReduced = false;

  const controls = new SimulationControls("sidebar", {
    onStart: () => {
      engine.start();
      controls.appendLog("Симуляция запущена");
    },
    onPause: () => {
      engine.pause();
      controls.appendLog("Пауза");
    },
    onReset: () => {
      engine.reset();
      metrics.clear();
      stateSpace.clear();
      controls.appendLog("Сброс");
    },
    onSingleStep: () => {
      engine.singleStep();
      collectAndUpdate();
    },
    onGenerateDisk: () => {
      engine.generateDisk();
      metrics.clear();
      stateSpace.clear();
      controls.appendLog("Новый диск сгенерирован");
    },
    onParamChange: (partial) => {
      engine.setParams(partial);
      const p = engine.getParams();
      scene.setQuality(p.quality);
      phaseSpace.setStarMass(p.starMass);
      if (partial.particleCount !== undefined && engine.getState() === "idle") {
        engine.generateDisk();
      }
    },
    onPreset: (id) => {
      const partial = presets.applyPreset(id);
      if (partial) {
        engine.setParams(partial);
        controls.setParams(engine.getParams());
        scene.setQuality(engine.getParams().quality);
        engine.generateDisk();
        controls.appendLog(`Пресет: ${id}`);
      }
    },
    onSaveParams: () => {
      presets.saveParams(engine.getParams());
      controls.appendLog("Параметры сохранены в localStorage");
    },
    onLoadParams: () => {
      const loaded = presets.loadParams();
      if (loaded) {
        engine.setParams(loaded);
        controls.setParams(loaded);
        controls.appendLog("Параметры загружены");
      } else {
        controls.appendLog("Нет сохранённых параметров");
      }
    },
    onExport: (type) => {
      const p = engine.getParams();
      switch (type) {
        case "params":
          exporter.exportParams(p);
          break;
        case "metrics":
          exporter.exportMetricsCSV(metrics.getHistory());
          break;
        case "state":
          exporter.exportState(engine.getBodies());
          break;
        case "screenshot":
          exporter.exportScreenshot(scene.screenshot());
          break;
        case "report":
          exporter.exportReport({
            params: p,
            metrics: metrics.getHistory(),
            bodies: [],
            events: engine.getEventLog(),
            report: exporter.buildReport(
              p,
              metrics.getHistory(),
              engine.getEventLog(),
            ),
          });
          break;
      }
      controls.appendLog(`Экспорт: ${type}`);
    },
    onCameraMode: (mode) => {
      scene.setCameraMode(mode);
    },
  });

  let metricsInterval = 0;
  engine.generateDisk();
  collectAndUpdate();
  scene.setQuality(engine.getParams().quality);
  phaseSpace.setStarMass(engine.getParams().starMass);

  function collectAndUpdate(): void {
    const snapshot = metrics.collect(engine);
    const history = metrics.getHistory();
    charts.update(
      snapshot.sizeDistribution,
      snapshot.massDistribution,
      history,
    );
    phaseSpace.update(engine.getBodies());
    stateSpace.maybeSnapshot(snapshot);

    controls.updateMetrics(
      [
        `Объектов: ${snapshot.objectCount}`,
        `Крупнейшая масса: ${snapshot.largestMass.toFixed(3)}`,
        `Слияний: ${snapshot.mergeCount}`,
        `E дрейф: ${(snapshot.energyDrift * 100).toFixed(2)}%`,
        `L дрейф: ${(snapshot.angularMomentumDrift * 100).toFixed(2)}%`,
        `K=${snapshot.kineticEnergy.toFixed(1)} U=${snapshot.potentialEnergy.toFixed(1)}`,
      ].join("\n"),
    );
  }

  function loop(now: number): void {
    frameCount++;
    if (now - lastFpsTime >= 1000) {
      fps = frameCount;
      frameCount = 0;
      lastFpsTime = now;

      if (fps < 25 && !autoQualityReduced && engine.getParams().quality !== "low") {
        engine.setParams({ quality: "low" });
        scene.setQuality("low");
        controls.setParams(engine.getParams());
        autoQualityReduced = true;
        controls.appendLog("⚠ Автоснижение качества (FPS < 25)");
      }
    }

    const dt = 1 / 60;
    engine.update(dt);

    metricsInterval += dt;
    if (metricsInterval >= 0.5) {
      metricsInterval = 0;
      if (engine.getState() === "running") {
        collectAndUpdate();
      }
    }

    scene.update(engine.getBodies(), engine.getParams());
    scene.render();

    controls.updateStatus(
      engine.getState(),
      fps,
      engine.getPhysicsStepMs(),
      rendererType,
    );

    const overlay = document.getElementById("fps-overlay");
    if (overlay) {
      overlay.textContent = `${fps} FPS | ${engine.getActiveCount()} объектов | t=${engine.getSimTime().toFixed(1)}`;
    }

    requestAnimationFrame(loop);
  }

  requestAnimationFrame(loop);
  controls.appendLog(`Рендер: ${rendererType.toUpperCase()}`);
  controls.appendLog("Готово. Нажмите Start или Generate New Disk.");
}

main().catch((err) => {
  console.error(err);
  document.body.innerHTML = `<pre style="color:red;padding:2rem">Ошибка запуска: ${err}</pre>`;
});
