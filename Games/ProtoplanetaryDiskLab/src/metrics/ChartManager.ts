export class ChartManager {
  private container: HTMLElement;
  private charts: { id: string; canvas: HTMLCanvasElement; title: string }[] = [];

  constructor(containerId: string) {
    const el = document.getElementById(containerId);
    if (!el) throw new Error(`Chart container #${containerId} not found`);
    this.container = el;
    this.container.className = "charts-grid";
    this.createCharts();
  }

  private createCharts(): void {
    const specs = [
      { id: "size-dist", title: "Распределение по размерам" },
      { id: "mass-dist", title: "Распределение по массам" },
      { id: "object-count", title: "Число объектов" },
      { id: "largest-mass", title: "Масса крупнейшего тела" },
      { id: "energy", title: "Энергия (E, K, U)" },
      { id: "angular-momentum", title: "Угловой момент" },
      { id: "energy-drift", title: "Дрейф энергии (%)" },
      { id: "am-drift", title: "Дрейф углового момента (%)" },
      { id: "radial-mass", title: "Радиальное распределение массы" },
      { id: "protoplanets", title: "Массы протопланет" },
    ];

    for (const spec of specs) {
      const wrap = document.createElement("div");
      wrap.className = "chart-card";
      const h = document.createElement("h4");
      h.textContent = spec.title;
      const canvas = document.createElement("canvas");
      canvas.width = 280;
      canvas.height = 140;
      wrap.append(h, canvas);
      this.container.appendChild(wrap);
      this.charts.push({ id: spec.id, canvas, title: spec.title });
    }
  }

  update(
    sizeDist: number[],
    massDist: number[],
    history: {
      time: number;
      objectCount: number;
      largestMass: number;
      kineticEnergy: number;
      potentialEnergy: number;
      totalEnergy: number;
      angularMomentum: number;
      energyDrift: number;
      angularMomentumDrift: number;
      radialMassBins: number[];
      topProtoplanetMasses: number[];
    }[],
  ): void {
    this.drawBar("size-dist", sizeDist, "#6af");
    this.drawBar("mass-dist", massDist, "#f96");
    this.drawLine("object-count", history.map((h) => h.objectCount), "#4f4");
    this.drawLine("largest-mass", history.map((h) => h.largestMass), "#fa4");
    this.drawMultiLine(
      "energy",
      [
        history.map((h) => h.totalEnergy),
        history.map((h) => h.kineticEnergy),
        history.map((h) => h.potentialEnergy),
      ],
      ["#fff", "#4af", "#f44"],
    );
    this.drawLine(
      "angular-momentum",
      history.map((h) => h.angularMomentum),
      "#a4f",
    );
    this.drawLine(
      "energy-drift",
      history.map((h) => h.energyDrift * 100),
      "#f88",
    );
    this.drawLine(
      "am-drift",
      history.map((h) => h.angularMomentumDrift * 100),
      "#8af",
    );
    this.drawBar("radial-mass", history.at(-1)?.radialMassBins ?? [], "#6f6");
    this.drawBar(
      "protoplanets",
      history.at(-1)?.topProtoplanetMasses ?? [],
      "#ff6",
    );
  }

  private getChart(id: string) {
    return this.charts.find((c) => c.id === id);
  }

  private drawBar(id: string, data: number[], color: string): void {
    const chart = this.getChart(id);
    if (!chart || data.length === 0) return;
    const ctx = chart.canvas.getContext("2d");
    if (!ctx) return;
    const { width, height } = chart.canvas;
    ctx.clearRect(0, 0, width, height);
    const max = Math.max(...data, 1e-6);
    const barW = width / data.length - 2;
    for (let i = 0; i < data.length; i++) {
      const h = (data[i] / max) * (height - 20);
      ctx.fillStyle = color;
      ctx.fillRect(i * (barW + 2) + 1, height - h - 10, barW, h);
    }
  }

  private drawLine(id: string, data: number[], color: string): void {
    const chart = this.getChart(id);
    if (!chart || data.length < 2) return;
    const ctx = chart.canvas.getContext("2d");
    if (!ctx) return;
    const { width, height } = chart.canvas;
    ctx.clearRect(0, 0, width, height);
    const min = Math.min(...data);
    const max = Math.max(...data);
    const range = max - min || 1;
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    for (let i = 0; i < data.length; i++) {
      const x = (i / (data.length - 1)) * (width - 10) + 5;
      const y = height - 10 - ((data[i] - min) / range) * (height - 20);
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  private drawMultiLine(
    id: string,
    series: number[][],
    colors: string[],
  ): void {
    for (let s = 0; s < series.length; s++) {
      const chart = this.getChart(id);
      if (!chart) return;
      const ctx = chart.canvas.getContext("2d");
      if (!ctx) return;
      if (s === 0) ctx.clearRect(0, 0, chart.canvas.width, chart.canvas.height);
      this.drawLineOn(
        ctx,
        chart.canvas.width,
        chart.canvas.height,
        series[s],
        colors[s] ?? "#fff",
        s === 0,
        series,
      );
    }
  }

  private drawLineOn(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    data: number[],
    color: string,
    computeRange: boolean,
    allSeries?: number[][],
  ): void {
    if (data.length < 2) return;
    let min: number;
    let max: number;
    if (computeRange && allSeries) {
      const flat = allSeries.flat();
      min = Math.min(...flat);
      max = Math.max(...flat);
    } else {
      min = Math.min(...data);
      max = Math.max(...data);
    }
    const range = max - min || 1;
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.2;
    ctx.beginPath();
    for (let i = 0; i < data.length; i++) {
      const x = (i / (data.length - 1)) * (width - 10) + 5;
      const y = height - 10 - ((data[i] - min) / range) * (height - 20);
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }
}
