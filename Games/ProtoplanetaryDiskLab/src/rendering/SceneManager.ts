import {
  PerspectiveCamera,
  Scene,
  WebGLRenderer,
} from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import type { SimBody, SimulationParams } from "../types";
import {
  createStarfield,
  DiskPlaneRenderer,
  ParticleRenderer,
  setupSceneLighting,
  StarRenderer,
} from "./ParticleRenderer";

export type CameraMode = "free" | "top" | "side" | "track";

export class SceneManager {
  private canvas: HTMLCanvasElement;
  private scene: Scene;
  private camera: PerspectiveCamera;
  private renderer: WebGLRenderer;
  private controls: OrbitControls;
  private starRenderer: StarRenderer;
  private particleRenderer: ParticleRenderer;
  private diskPlane: DiskPlaneRenderer;
  private cameraMode: CameraMode = "free";
  private trackTarget: SimBody | null = null;
  private colorByMass = true;
  private qualityParticleCap = 3000;
  private resizeObserver: ResizeObserver;

  constructor(canvas: HTMLCanvasElement, params: SimulationParams) {
    this.canvas = canvas;
    this.scene = new Scene();
    setupSceneLighting(this.scene);
    this.scene.add(createStarfield(2000));

    this.camera = new PerspectiveCamera(55, 1, 0.5, 600);
    this.camera.position.set(0, 55, 70);
    this.camera.lookAt(0, 0, 0);

    this.starRenderer = new StarRenderer();
    this.particleRenderer = new ParticleRenderer();
    this.diskPlane = new DiskPlaneRenderer(params);

    this.scene.add(
      this.starRenderer.getObject3D(),
      this.particleRenderer.getObject3D(),
      this.diskPlane.getObject3D(),
    );

    this.renderer = new WebGLRenderer({
      canvas: this.canvas,
      antialias: true,
      powerPreference: "high-performance",
    });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));

    this.controls = new OrbitControls(this.camera, this.canvas);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.08;
    this.controls.minDistance = 10;
    this.controls.maxDistance = 200;

    this.resizeObserver = new ResizeObserver(() => this.handleResize());
    const parent = canvas.parentElement ?? canvas;
    this.resizeObserver.observe(parent);
    this.handleResize();
  }

  async init(): Promise<string> {
    return "webgl";
  }

  private handleResize(): void {
    const parent = this.canvas.parentElement;
    if (!parent) return;
    const w = parent.clientWidth;
    const h = Math.max(parent.clientHeight, 300);
    if (w === 0 || h === 0) return;
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(w, h, false);
  }

  setCameraMode(mode: CameraMode): void {
    this.cameraMode = mode;
    if (mode === "top") {
      this.camera.position.set(0, 90, 0.01);
      this.camera.lookAt(0, 0, 0);
    } else if (mode === "side") {
      this.camera.position.set(90, 8, 0);
      this.camera.lookAt(0, 0, 0);
    } else if (mode === "free") {
      this.camera.position.set(0, 55, 70);
      this.camera.lookAt(0, 0, 0);
    }
    this.controls.update();
  }

  setTrackTarget(body: SimBody | null): void {
    this.trackTarget = body;
    if (body) this.cameraMode = "track";
  }

  setColorByMass(v: boolean): void {
    this.colorByMass = v;
  }

  setQuality(quality: SimulationParams["quality"]): void {
    switch (quality) {
      case "low":
        this.qualityParticleCap = 800;
        break;
      case "medium":
        this.qualityParticleCap = 2000;
        break;
      case "high":
        this.qualityParticleCap = 5000;
        break;
    }
  }

  update(bodies: readonly SimBody[], params: SimulationParams): void {
    this.starRenderer.update(params.starMass);

    const visible: SimBody[] = [];
    for (const b of bodies) {
      if (!b.active) continue;
      visible.push(b);
      if (visible.length >= this.qualityParticleCap) break;
    }

    this.particleRenderer.update(visible, this.colorByMass);
    this.diskPlane.update(params);

    if (this.cameraMode === "track" && this.trackTarget?.active) {
      const t = this.trackTarget.position;
      this.camera.position.lerp(
        { x: t.x, y: t.y + 20, z: t.z + 25 } as typeof this.camera.position,
        0.05,
      );
      this.camera.lookAt(t);
    }

    this.controls.update();
  }

  render(): void {
    this.renderer.render(this.scene, this.camera);
  }

  getRendererType(): string {
    return "webgl";
  }

  getCamera(): PerspectiveCamera {
    return this.camera;
  }

  getScene(): Scene {
    return this.scene;
  }

  screenshot(): string {
    this.render();
    return this.canvas.toDataURL("image/png");
  }

  dispose(): void {
    this.resizeObserver.disconnect();
    this.particleRenderer.dispose();
    this.starRenderer.dispose();
    this.diskPlane.dispose();
    this.renderer.dispose();
  }
}
