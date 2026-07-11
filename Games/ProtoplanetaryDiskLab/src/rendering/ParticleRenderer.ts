import {
  AmbientLight,
  BufferAttribute,
  BufferGeometry,
  Color,
  DirectionalLight,
  DynamicDrawUsage,
  Group,
  InstancedMesh,
  Matrix4,
  Mesh,
  MeshBasicMaterial,
  Points,
  PointsMaterial,
  PointLight,
  RingGeometry,
  Scene,
  SphereGeometry,
} from "three";
import type { SimBody, SimulationParams } from "../types";

const MAX_INSTANCES = 8000;
const _matrix = new Matrix4();
const _color = new Color();

export class ParticleRenderer {
  private group = new Group();
  private instancedMesh: InstancedMesh;
  private maxInstances: number;

  constructor(maxInstances = MAX_INSTANCES) {
    this.maxInstances = maxInstances;
    const geo = new SphereGeometry(1, 6, 4);
    const mat = new MeshBasicMaterial({ vertexColors: true });
    this.instancedMesh = new InstancedMesh(geo, mat, maxInstances);
    this.instancedMesh.instanceMatrix.setUsage(DynamicDrawUsage);
    this.instancedMesh.count = 0;
    this.instancedMesh.frustumCulled = false;
    this.group.add(this.instancedMesh);
    this.group.name = "particles";
  }

  getObject3D(): Group {
    return this.group;
  }

  update(bodies: readonly SimBody[], colorByMass: boolean): void {
    let count = 0;

    for (const body of bodies) {
      if (!body.active || count >= this.maxInstances) break;

      const s = Math.max(body.visualRadius, 0.12);
      _matrix.makeScale(s, s, s);
      _matrix.setPosition(body.position.x, body.position.y, body.position.z);

      this.instancedMesh.setMatrixAt(count, _matrix);

      if (colorByMass) {
        const t = Math.min(1, Math.log10(body.mass + 1) / 3);
        _color.setHSL(0.08 - t * 0.08, 0.75, 0.45 + t * 0.25);
      } else {
        _color.setHex(0xc4a882);
      }
      this.instancedMesh.setColorAt(count, _color);
      count++;
    }

    this.instancedMesh.count = count;
    this.instancedMesh.instanceMatrix.needsUpdate = true;
    if (this.instancedMesh.instanceColor) {
      this.instancedMesh.instanceColor.needsUpdate = true;
    }
  }

  dispose(): void {
    this.instancedMesh.geometry.dispose();
    (this.instancedMesh.material as MeshBasicMaterial).dispose();
  }
}

export class StarRenderer {
  private mesh: Mesh;
  private glow: Mesh;
  private light: PointLight;
  private group = new Group();

  constructor() {
    const geo = new SphereGeometry(1, 24, 16);
    const mat = new MeshBasicMaterial({ color: 0xffdd44 });
    this.mesh = new Mesh(geo, mat);

    const glowGeo = new SphereGeometry(1, 12, 8);
    const glowMat = new MeshBasicMaterial({
      color: 0xffaa00,
      transparent: true,
      opacity: 0.3,
    });
    this.glow = new Mesh(glowGeo, glowMat);

    this.light = new PointLight(0xffcc66, 2, 300);
    this.group.add(this.mesh, this.glow, this.light);
    this.group.name = "star";
  }

  getObject3D(): Group {
    return this.group;
  }

  update(starMass: number): void {
    const radius = Math.max(1.5, Math.cbrt(starMass) * 0.15);
    this.mesh.scale.setScalar(radius);
    this.glow.scale.setScalar(radius * 1.5);
    this.light.intensity = 2 + Math.log10(starMass) * 0.3;
  }

  dispose(): void {
    (this.mesh.geometry as SphereGeometry).dispose();
    (this.mesh.material as MeshBasicMaterial).dispose();
    (this.glow.geometry as SphereGeometry).dispose();
    (this.glow.material as MeshBasicMaterial).dispose();
  }
}

export class DiskPlaneRenderer {
  private mesh: Mesh;
  private group = new Group();
  private lastInner = -1;
  private lastOuter = -1;

  constructor(params: SimulationParams) {
    const geo = new RingGeometry(params.innerRadius, params.outerRadius, 64);
    const mat = new MeshBasicMaterial({
      color: 0x4466aa,
      transparent: true,
      opacity: 0.2,
      side: 2,
    });
    this.mesh = new Mesh(geo, mat);
    this.mesh.rotation.x = -Math.PI / 2;
    this.group.add(this.mesh);
    this.group.name = "disk-plane";
    this.lastInner = params.innerRadius;
    this.lastOuter = params.outerRadius;
  }

  getObject3D(): Group {
    return this.group;
  }

  update(params: SimulationParams): void {
    if (
      params.innerRadius === this.lastInner &&
      params.outerRadius === this.lastOuter
    ) {
      return;
    }
    this.lastInner = params.innerRadius;
    this.lastOuter = params.outerRadius;
    this.mesh.geometry.dispose();
    this.mesh.geometry = new RingGeometry(
      params.innerRadius,
      params.outerRadius,
      64,
    );
  }

  dispose(): void {
    (this.mesh.geometry as RingGeometry).dispose();
    (this.mesh.material as MeshBasicMaterial).dispose();
  }
}

export function createStarfield(count: number): Points {
  const positions = new Float32Array(count * 3);
  for (let i = 0; i < count; i++) {
    const r = 120 + Math.random() * 80;
    const theta = Math.random() * Math.PI * 2;
    const phi = Math.acos(2 * Math.random() - 1);
    positions[i * 3] = r * Math.sin(phi) * Math.cos(theta);
    positions[i * 3 + 1] = r * Math.sin(phi) * Math.sin(theta);
    positions[i * 3 + 2] = r * Math.cos(phi);
  }
  const geo = new BufferGeometry();
  geo.setAttribute("position", new BufferAttribute(positions, 3));
  const mat = new PointsMaterial({ color: 0xffffff, size: 0.8, sizeAttenuation: true });
  const points = new Points(geo, mat);
  points.name = "starfield";
  return points;
}

export function setupSceneLighting(scene: Scene): void {
  scene.background = new Color(0x050510);
  scene.add(new AmbientLight(0x334466, 0.6));
  const dir = new DirectionalLight(0x8888cc, 0.4);
  dir.position.set(10, 20, 10);
  scene.add(dir);
}
