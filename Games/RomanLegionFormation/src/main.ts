import { AGENT_BODY_RADIUS, SLOT_MARKER_RADIUS } from "./sim/formationGeometry";
import { World } from "./sim/World";
import type { DebugFlags, SimConfig } from "./types";
import { FORMATION_CENTER_X, FORMATION_CENTER_Y, STATE_COLORS, WORLD_HEIGHT, WORLD_WIDTH } from "./types";
import "./style.css";

const defaultConfig: SimConfig = {
  agentCount: 184,
  perceptionRadius: 140,
  behaviorVersion: "V5",
  oracleMode: false,
  simSpeed: 1,
  running: true,
};

const defaultDebug: DebugFlags = {
  showSlotGrid: true,
  showBelievedSpots: true,
  showClaimedLines: false,
  showPerception: false,
  selectedAgentId: null,
};

const world = new World(defaultConfig, defaultDebug, 42);

function buildUi(root: HTMLElement): void {
  root.innerHTML = `
    <header>
      <h1>Roman Legion Formation Lab</h1>
      <p>Decentralized self-assembly — no telepathy (unless Oracle debug). Inspired by David Shapiro demo.</p>
    </header>
    <div class="layout">
      <div class="canvas-wrap"><canvas id="battlefield"></canvas></div>
      <aside class="panel" id="controls"></aside>
    </div>
  `;

  const panel = root.querySelector("#controls") as HTMLElement;
  panel.innerHTML = `
    <section>
      <h2>Centurion commands — formation</h2>
      <div class="btn-row">
        <button data-cmd="triple" class="primary">Form Triple Line</button>
        <button data-cmd="rect">Form Rectangle</button>
        <button data-cmd="square">Form Square</button>
        <button data-cmd="scatter">Reset / Scatter</button>
      </div>
    </section>
    <section>
      <h2>Facing (in place)</h2>
      <div class="btn-row">
        <button data-face="left">Face Left</button>
        <button data-face="right">Face Right</button>
        <button data-face="about">About Face</button>
      </div>
    </section>
    <section>
      <h2>Simulation</h2>
      <div class="btn-row">
        <button id="toggle-run">Pause</button>
      </div>
      <label class="row">Speed <input id="speed" type="range" min="0.25" max="4" step="0.25" value="1" /></label>
      <label class="row">Agents <input id="agents" type="range" min="50" max="2000" step="50" value="184" /></label>
      <label class="row">Perception <input id="perception" type="range" min="60" max="280" step="10" value="140" /></label>
      <label class="row">Behavior
        <select id="behavior">
          <option value="V4">V4 (tighter)</option>
          <option value="V5" selected>V5 (stuck recovery)</option>
        </select>
      </label>
      <p class="hint">V4 was tighter; V5 adds stuck recovery + wider seat list.</p>
    </section>
    <section>
      <h2>Debug</h2>
      <label class="row"><span>Slot grid</span><input id="dbg-grid" type="checkbox" checked /></label>
      <label class="row"><span>Believed spots</span><input id="dbg-believed" type="checkbox" checked /></label>
      <label class="row"><span>Claimed lines</span><input id="dbg-lines" type="checkbox" /></label>
      <label class="row"><span>Oracle mode</span><input id="dbg-oracle" type="checkbox" /></label>
    </section>
    <section>
      <h2>Metrics</h2>
      <div class="metrics" id="metrics"></div>
    </section>
  `;

  panel.querySelector('[data-cmd="triple"]')!.addEventListener("click", () => world.commandTripleLine());
  panel.querySelector('[data-cmd="rect"]')!.addEventListener("click", () => world.commandRectangle());
  panel.querySelector('[data-cmd="square"]')!.addEventListener("click", () => world.commandSquare());
  panel.querySelector('[data-cmd="scatter"]')!.addEventListener("click", () => world.scatter());

  panel.querySelector('[data-face="left"]')!.addEventListener("click", () => world.commandFacing("left"));
  panel.querySelector('[data-face="right"]')!.addEventListener("click", () => world.commandFacing("right"));
  panel.querySelector('[data-face="about"]')!.addEventListener("click", () => world.commandFacing("about"));

  const toggle = panel.querySelector("#toggle-run") as HTMLButtonElement;
  toggle.addEventListener("click", () => {
    world.config.running = !world.config.running;
    toggle.textContent = world.config.running ? "Pause" : "Play";
  });

  bindRange(panel, "#speed", (v) => (world.config.simSpeed = v));
  bindRange(panel, "#agents", (v) => world.resetAgents(Math.round(v)));
  bindRange(panel, "#perception", (v) => (world.config.perceptionRadius = v));

  const behavior = panel.querySelector("#behavior") as HTMLSelectElement;
  behavior.addEventListener("change", () => {
    world.config.behaviorVersion = behavior.value as "V4" | "V5";
  });

  bindCheck(panel, "#dbg-grid", (v) => (world.debug.showSlotGrid = v));
  bindCheck(panel, "#dbg-believed", (v) => (world.debug.showBelievedSpots = v));
  bindCheck(panel, "#dbg-lines", (v) => (world.debug.showClaimedLines = v));
  bindCheck(panel, "#dbg-oracle", (v) => (world.config.oracleMode = v));
}

function bindRange(panel: HTMLElement, sel: string, fn: (v: number) => void): void {
  const el = panel.querySelector(sel) as HTMLInputElement;
  el.addEventListener("input", () => fn(parseFloat(el.value)));
}

function bindCheck(panel: HTMLElement, sel: string, fn: (v: boolean) => void): void {
  const el = panel.querySelector(sel) as HTMLInputElement;
  el.addEventListener("change", () => fn(el.checked));
}

function render(ctx: CanvasRenderingContext2D, w: number, h: number): void {
  const sx = w / WORLD_WIDTH;
  const sy = h / WORLD_HEIGHT;
  const scale = Math.min(sx, sy);
  const ox = (w - WORLD_WIDTH * scale) * 0.5;
  const oy = (h - WORLD_HEIGHT * scale) * 0.5;

  ctx.fillStyle = "#1a2332";
  ctx.fillRect(0, 0, w, h);

  ctx.save();
  ctx.translate(ox, oy);
  ctx.scale(scale, scale);

  ctx.strokeStyle = "#2d3a4d";
  ctx.lineWidth = 1;
  for (let x = 0; x <= WORLD_WIDTH; x += 40) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, WORLD_HEIGHT);
    ctx.stroke();
  }
  for (let y = 0; y <= WORLD_HEIGHT; y += 40) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(WORLD_WIDTH, y);
    ctx.stroke();
  }

  if (world.template && world.debug.showSlotGrid) {
    for (const slot of world.template.slots) {
      ctx.strokeStyle = "#4b5563";
      ctx.beginPath();
      ctx.arc(slot.x, slot.y, SLOT_MARKER_RADIUS, 0, Math.PI * 2);
      ctx.stroke();
    }
  }

  if (world.debug.showBelievedSpots) {
    for (const a of world.agents) {
      if (a.believedSlotId === null || !world.template) continue;
      const slot = world.template.slots[a.believedSlotId];
      if (!slot) continue;
      ctx.fillStyle = "rgba(156, 163, 175, 0.85)";
      ctx.beginPath();
      ctx.arc(slot.x, slot.y, SLOT_MARKER_RADIUS * 0.85, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  if (world.debug.showClaimedLines) {
    ctx.strokeStyle = "rgba(234, 179, 8, 0.35)";
    for (const a of world.agents) {
      if (a.claimedSlotId === null || !world.template) continue;
      const slot = world.template.slots[a.claimedSlotId];
      if (!slot) continue;
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(slot.x, slot.y);
      ctx.stroke();
    }
  }

  for (const a of world.agents) {
    ctx.fillStyle = STATE_COLORS[a.state];
    ctx.beginPath();
    ctx.arc(a.x, a.y, a.radius, 0, Math.PI * 2);
    ctx.fill();

    ctx.strokeStyle = "#0f172a";
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(
      a.x + Math.cos(a.facing) * (AGENT_BODY_RADIUS + 2),
      a.y + Math.sin(a.facing) * (AGENT_BODY_RADIUS + 2),
    );
    ctx.stroke();
  }

  ctx.fillStyle = "rgba(234, 179, 8, 0.15)";
  ctx.beginPath();
  ctx.arc(FORMATION_CENTER_X, FORMATION_CENTER_Y, 8, 0, Math.PI * 2);
  ctx.fill();

  ctx.restore();
}

function updateMetrics(el: HTMLElement): void {
  const m = world.getMetrics();
  el.innerHTML = `
    Agents: ${m.agentCount}<br/>
    Purple mustering: ${m.mustering} · Blue forming: ${m.forming}<br/>
    Green dressing: ${m.dressing} · Yellow turning: ${m.turning}<br/>
    <span class="${m.dressedPercent >= 85 ? "good" : "warn"}">Dressed: ${m.dressedPercent.toFixed(1)}%</span><br/>
    Spinning: ${m.spinningPercent.toFixed(1)}% · Stuck: ${m.stuckCount}<br/>
    Mean slot error: ${m.meanSlotError.toFixed(2)} px<br/>
    Formation time: ${m.formationElapsed.toFixed(1)} s<br/>
    Mode: ${world.config.oracleMode ? "Oracle" : "Decentralized"}
  `;
}

function main(): void {
  const root = document.querySelector("#app") as HTMLElement;
  buildUi(root);

  const canvas = document.querySelector("#battlefield") as HTMLCanvasElement;
  const ctx = canvas.getContext("2d")!;
  const metricsEl = document.querySelector("#metrics") as HTMLElement;

  const resize = () => {
    const wrap = canvas.parentElement!;
    canvas.width = wrap.clientWidth;
    canvas.height = Math.max(400, window.innerHeight - 120);
  };
  window.addEventListener("resize", resize);
  resize();

  const loop = () => {
    world.step();
    render(ctx, canvas.width, canvas.height);
    updateMetrics(metricsEl);
    requestAnimationFrame(loop);
  };
  requestAnimationFrame(loop);
}

main();
