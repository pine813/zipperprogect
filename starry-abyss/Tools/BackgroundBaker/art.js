/* =========================================================
   星渊 · 归途 — procedural Van Gogh cosmos
   All painting is generated on canvas at runtime.
   ========================================================= */

(function () {
  "use strict";

  const TAU = Math.PI * 2;
  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const lerp = (a, b, t) => a + (b - a) * t;

  const THEMES = [
    {
      id: "starry",
      name: "星夜",
      ink: "#030812",
      top: "#01040a",
      mid: "#071a2d",
      low: "#0a1d28",
      blues: ["#142e49", "#1f415e", "#2d5975", "#0b1d33", "#356484"],
      teals: ["#2f6263", "#4b7f79", "#68a098", "#1e4a50"],
      golds: ["#c69a53", "#deb972", "#f0d89a", "#9d733c"],
      cream: "#f8efd6",
      coral: "#c98368",
      violet: "#555c86",
      halo: "#f1d695",
      portal: "#f8dfa2",
      star: "#ffeec4",
      planets: ["#355c7c", "#4b7f87", "#b48563", "#696e9a", "#7e9c94"],
      moon: "#efd49a",
      guide: "#f3dfb1"
    },
    {
      id: "rebirth",
      name: "新宇宙",
      ink: "#05040d",
      top: "#03020a",
      mid: "#17102a",
      low: "#0b1b25",
      blues: ["#292e55", "#3b3e70", "#515080", "#131d32", "#5c5b8e"],
      teals: ["#32676f", "#4f8484", "#71a19a", "#21494d"],
      golds: ["#d4a761", "#e8c482", "#f2ddaf", "#a57943"],
      cream: "#fff1d8",
      coral: "#d68c7f",
      violet: "#8976a8",
      halo: "#f3cf96",
      portal: "#ffe4a6",
      star: "#fff2c9",
      planets: ["#4a4e7a", "#537f80", "#b97f6f", "#7f6a9c", "#7b9991"],
      moon: "#f2d7a0",
      guide: "#f4dfb5"
    },
    {
      id: "verdigris",
      name: "月潮",
      ink: "#020807",
      top: "#010504",
      mid: "#07211f",
      low: "#0c1f24",
      blues: ["#1c3a3e", "#29565a", "#396d6e", "#0a2028", "#477b7c"],
      teals: ["#4f8878", "#6aa18d", "#89b8a4", "#275e58"],
      golds: ["#d0ad68", "#e3c98f", "#f2e3b7", "#9d814b"],
      cream: "#fbf3da",
      coral: "#cb8b6e",
      violet: "#5f7587",
      halo: "#f2dba4",
      portal: "#f9e7af",
      star: "#fff4cf",
      planets: ["#2f5c5a", "#5c8977", "#b78565", "#6f8097", "#91ac95"],
      moon: "#f2ddb0",
      guide: "#f5e5ba"
    },
    {
      id: "amber",
      name: "琥珀",
      ink: "#080604",
      top: "#050301",
      mid: "#24170f",
      low: "#0d1d25",
      blues: ["#263b53", "#38566e", "#4b6c80", "#112331", "#5e7f8d"],
      teals: ["#3f6d69", "#5f8c83", "#82a79b", "#2d514f"],
      golds: ["#d29c55", "#e8b976", "#f4d79d", "#a56f35"],
      cream: "#faedce",
      coral: "#cf8164",
      violet: "#69617c",
      halo: "#f0cf94",
      portal: "#fae0a2",
      star: "#ffedc0",
      planets: ["#3c5b70", "#577f7d", "#ba7f5d", "#786b84", "#8b9e8a"],
      moon: "#f3d49b",
      guide: "#f1d9ae"
    }
  ];

  function mulberry32(a) {
    return function () {
      a |= 0;
      a = (a + 0x6D2B79F5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  function hexToRgb(hex) {
    if (typeof hex !== "string") return [255, 245, 215];
    let h = hex.replace("#", "").trim();
    if (h.length === 3) h = h.split("").map((c) => c + c).join("");
    const n = parseInt(h, 16);
    if (Number.isNaN(n)) return [255, 245, 215];
    return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
  }

  function rgba(hex, alpha) {
    if (!hex) return "rgba(255,255,255," + alpha + ")";
    if (hex.indexOf("rgba") === 0) return hex;
    const c = hexToRgb(hex);
    return "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + alpha + ")";
  }

  function mix(a, b, t) {
    const ca = hexToRgb(a);
    const cb = hexToRgb(b);
    const n = clamp(t, 0, 1);
    return "rgb(" + Math.round(lerp(ca[0], cb[0], n)) + "," + Math.round(lerp(ca[1], cb[1], n)) + "," + Math.round(lerp(ca[2], cb[2], n)) + ")";
  }

  function lighten(hex, t) {
    return mix(hex, "#fff8e8", t);
  }

  function pick(rand, arr) {
    return arr[(rand() * arr.length) | 0];
  }

  function hashSeed(str) {
    let h = 2166136261;
    for (let i = 0; i < str.length; i++) {
      h ^= str.charCodeAt(i);
      h = Math.imul(h, 16777619);
    }
    return h >>> 0;
  }

  /* -------------------------------------------------------
     Glow sprites & brush helpers
     ------------------------------------------------------- */

  const glowCache = new Map();

  function getGlowSprite(color) {
    if (glowCache.has(color)) return glowCache.get(color);
    const size = 192;
    const canvas = document.createElement("canvas");
    canvas.width = size;
    canvas.height = size;
    const g = canvas.getContext("2d");
    const c = hexToRgb(color);
    const grad = g.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
    grad.addColorStop(0, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.92)");
    grad.addColorStop(0.08, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.72)");
    grad.addColorStop(0.26, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.25)");
    grad.addColorStop(0.56, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.065)");
    grad.addColorStop(1, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0)");
    g.fillStyle = grad;
    g.fillRect(0, 0, size, size);
    glowCache.set(color, canvas);
    return canvas;
  }

  function glow(ctx, color, x, y, radius, alpha) {
    if (radius <= 0 || alpha <= 0) return;
    const sprite = getGlowSprite(color);
    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    ctx.globalAlpha = alpha;
    ctx.drawImage(sprite, x - radius, y - radius, radius * 2, radius * 2);
    ctx.restore();
  }

  function dab(ctx, x, y, angle, length, width, color, alpha, bend) {
    if (alpha <= 0) return;
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(angle);
    ctx.globalAlpha = alpha;
    ctx.strokeStyle = color;
    ctx.lineWidth = width;
    ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(-length * 0.5, 0);
    ctx.quadraticCurveTo(0, bend || 0, length * 0.5, 0);
    ctx.stroke();
    ctx.restore();
  }

  function paintDab(ctx, rand, x, y, angle, color, alphaRange, lengthRange, widthRange) {
    const len = lengthRange[0] + rand() * (lengthRange[1] - lengthRange[0]);
    const wid = widthRange[0] + rand() * (widthRange[1] - widthRange[0]);
    const alpha = alphaRange[0] + rand() * (alphaRange[1] - alphaRange[0]);
    const bend = (rand() - 0.5) * len * 0.25;
    dab(ctx, x, y, angle, len, wid, color, alpha, bend);
    if (rand() > 0.55) {
      dab(ctx, x + (rand() - 0.5) * 2.2, y + (rand() - 0.5) * 2.2, angle + (rand() - 0.5) * 0.06, len * 0.8, wid * 0.75, color, alpha * 0.5, bend);
    }
  }

  function spacedText(ctx, text, x, y, spacing) {
    const chars = text.split("");
    let total = 0;
    const widths = chars.map((ch) => {
      const w = ctx.measureText(ch).width;
      total += w;
      return w;
    });
    total += spacing * (chars.length - 1);
    let cursor = x - total / 2;
    chars.forEach((ch, i) => {
      ctx.fillText(ch, cursor, y);
      cursor += widths[i] + spacing;
    });
  }

  /* -------------------------------------------------------
     Backdrop painting
     ------------------------------------------------------- */

  function paintHaloStar(g, rand, x, y, size, theme, strength) {
    const inkOpacity = strength == null ? 1 : strength;
    const warm = rand() > 0.38;
    const haloColor = warm ? theme.halo : theme.cream;
    const accent = rand() > 0.62 ? theme.teal : theme.coral;

    const c = hexToRgb(haloColor);
    const big = g.createRadialGradient(x, y, 0, x, y, size * 4.2);
    big.addColorStop(0, "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + (0.4 * inkOpacity) + ")");
    big.addColorStop(0.1, "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + (0.19 * inkOpacity) + ")");
    big.addColorStop(0.36, "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + (0.055 * inkOpacity) + ")");
    big.addColorStop(1, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0)");
    g.save();
    g.globalCompositeOperation = "lighter";
    g.fillStyle = big;
    g.beginPath();
    g.arc(x, y, size * 4.2, 0, TAU);
    g.fill();
    g.restore();

    g.save();
    g.translate(x, y);
    g.globalCompositeOperation = "lighter";

    // Uneven concentric halo rings, like paint pressed from a palette knife.
    const rings = 2 + ((rand() * 3) | 0);
    for (let r = 0; r < rings; r++) {
      const radius = size * (1.35 + r * 0.48 + rand() * 0.14);
      const start = rand() * TAU;
      const pieces = 2 + ((rand() * 4) | 0);
      for (let p = 0; p < pieces; p++) {
        const arcStart = start + (p / pieces) * TAU + rand() * 0.2;
        const arcLen = 0.3 + rand() * 1.3;
        g.strokeStyle = r % 2 === 0 ? rgba(haloColor, 0.14 * inkOpacity) : rgba(accent, 0.1 * inkOpacity);
        g.lineWidth = 0.7 + rand() * 1.4;
        g.beginPath();
        g.arc(0, 0, radius, arcStart, arcStart + arcLen);
        g.stroke();
      }
    }

    // Short, curling rays.
    const rays = 9 + ((rand() * 8) | 0);
    for (let i = 0; i < rays; i++) {
      const a = (i / rays) * TAU + (rand() - 0.5) * 0.25;
      const r0 = size * (0.62 + rand() * 0.42);
      const len = size * (0.35 + rand() * 0.95);
      const x0 = Math.cos(a) * r0;
      const y0 = Math.sin(a) * r0;
      g.strokeStyle = rgba(warm ? haloColor : theme.cream, (0.09 + rand() * 0.13) * inkOpacity);
      g.lineWidth = 0.7 + rand() * 1.2;
      g.beginPath();
      g.moveTo(x0, y0);
      g.quadraticCurveTo(
        Math.cos(a + 0.18) * (r0 + len * 0.5),
        Math.sin(a + 0.18) * (r0 + len * 0.5),
        Math.cos(a + 0.1) * (r0 + len),
        Math.sin(a + 0.1) * (r0 + len)
      );
      g.stroke();
    }

    const core = g.createRadialGradient(0, 0, 0, 0, 0, size);
    core.addColorStop(0, rgba("#fffdf4", 0.98 * inkOpacity));
    core.addColorStop(0.12, rgba(theme.cream, 0.85 * inkOpacity));
    core.addColorStop(0.36, rgba(haloColor, 0.42 * inkOpacity));
    core.addColorStop(0.7, rgba(haloColor, 0.08 * inkOpacity));
    core.addColorStop(1, rgba(haloColor, 0));
    g.fillStyle = core;
    g.beginPath();
    g.arc(0, 0, size, 0, TAU);
    g.fill();

    // A few bright impasto specks.
    for (let i = 0; i < 6; i++) {
      const a = rand() * TAU;
      const rr = size * (1.5 + rand() * 2.3);
      dab(g, Math.cos(a) * rr, Math.sin(a) * rr, a + 1.2, 3 + rand() * 7, 0.7 + rand(), rgba(haloColor, 0.13 * inkOpacity), 0.4);
    }
    g.restore();
  }

  function paintMoon(g, rand, x, y, size, theme, opacity) {
    g.save();
    g.translate(x, y);
    g.globalCompositeOperation = "lighter";
    const halo = g.createRadialGradient(0, 0, size * 0.4, 0, 0, size * 5);
    halo.addColorStop(0, rgba(theme.moon, 0.2 * opacity));
    halo.addColorStop(0.3, rgba(theme.moon, 0.06 * opacity));
    halo.addColorStop(1, rgba(theme.moon, 0));
    g.fillStyle = halo;
    g.beginPath();
    g.arc(0, 0, size * 5, 0, TAU);
    g.fill();

    for (let i = 0; i < 4; i++) {
      const radius = size * (1.3 + i * 0.48);
      const start = -2.6 + rand() * 0.5;
      g.strokeStyle = rgba(theme.moon, (0.12 - i * 0.018) * opacity);
      g.lineWidth = 1 + rand() * 1.3;
      g.beginPath();
      g.arc(0, 0, radius, start, start + 2.2 + rand() * 1.4);
      g.stroke();
      g.strokeStyle = rgba(theme.coral, 0.06 * opacity);
      g.beginPath();
      g.arc(0, 0, radius + 6, start + 2.9, start + 4.2);
      g.stroke();
    }

    const disc = g.createRadialGradient(-size * 0.2, -size * 0.2, size * 0.1, 0, 0, size);
    disc.addColorStop(0, rgba("#fff8df", 0.92 * opacity));
    disc.addColorStop(0.56, rgba(theme.moon, 0.82 * opacity));
    disc.addColorStop(0.9, rgba(theme.golds[1], 0.38 * opacity));
    disc.addColorStop(1, rgba(theme.moon, 0));
    g.fillStyle = disc;
    g.beginPath();
    g.arc(0, 0, size, 0, TAU);
    g.fill();

    // A crescent shadow, gently placed with a loaded brush.
    g.globalCompositeOperation = "source-over";
    g.fillStyle = rgba(theme.mid, 0.68 * opacity);
    g.beginPath();
    g.arc(size * 0.34, -size * 0.17, size * 0.91, 0, TAU);
    g.fill();

    // The eyelash of halo around the moon.
    g.globalCompositeOperation = "lighter";
    for (let i = 0; i < 15; i++) {
      const a = -2.6 + i * 0.22 + rand() * 0.1;
      const rr = size * (1.03 + rand() * 0.22);
      dab(g, Math.cos(a) * rr, Math.sin(a) * rr, a + Math.PI / 2, 3 + rand() * 5, 0.8 + rand(), rgba(theme.cream, 0.12 * opacity), 0.5);
    }
    g.restore();
  }

  function buildBackdrop(w, h, theme, seed) {
    const dpr = Math.min(window.devicePixelRatio || 1, 1.5);
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.floor(w * dpr));
    canvas.height = Math.max(1, Math.floor(h * dpr));
    const g = canvas.getContext("2d");
    g.scale(dpr, dpr);

    const rand = mulberry32(seed || 92731);
    const S = Math.min(w, h);
    const cx = w / 2;
    const cy = h / 2;

    // Deep ultramarine night.
    const sky = g.createLinearGradient(w * 0.2, 0, w * 0.64, h);
    sky.addColorStop(0, theme.top);
    sky.addColorStop(0.36, theme.mid);
    sky.addColorStop(0.68, theme.mid);
    sky.addColorStop(1, theme.low);
    g.fillStyle = sky;
    g.fillRect(0, 0, w, h);

    // Billowing pigment in the corners.
    const washes = [
      { x: w * 0.17, y: h * 0.18, r: S * 0.62, color: theme.golds[0], a: 0.075 },
      { x: w * 0.84, y: h * 0.3, r: S * 0.54, color: theme.teals[1], a: 0.065 },
      { x: w * 0.65, y: h * 0.86, r: S * 0.64, color: theme.blues[1], a: 0.12 },
      { x: w * 0.49, y: h * 0.48, r: S * 0.54, color: theme.teals[0], a: 0.035 },
      { x: w * 0.08, y: h * 0.82, r: S * 0.4, color: theme.violet, a: 0.08 }
    ];
    washes.forEach((wash) => {
      const c = hexToRgb(wash.color);
      const rg = g.createRadialGradient(wash.x, wash.y, 0, wash.x, wash.y, wash.r);
      rg.addColorStop(0, "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + wash.a + ")");
      rg.addColorStop(0.5, "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + wash.a * 0.36 + ")");
      rg.addColorStop(1, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0)");
      g.fillStyle = rg;
      g.fillRect(wash.x - wash.r, wash.y - wash.r, wash.r * 2, wash.r * 2);
    });

    // A distant river of star-milk.
    g.save();
    g.translate(w * 0.64, h * 0.58);
    g.rotate(-0.43);
    for (let i = 0; i < 9; i++) {
      const c = hexToRgb(pick(rand, [theme.blue || theme.blues[0], theme.teals[1], theme.violet]));
      const y = (rand() - 0.5) * S * 0.24;
      const rx = S * (0.38 + rand() * 0.36);
      const ry = S * (0.026 + rand() * 0.055);
      const rg = g.createRadialGradient(0, y, 0, 0, y, rx);
      rg.addColorStop(0, "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + (0.02 + rand() * 0.022) + ")");
      rg.addColorStop(0.7, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.008)");
      rg.addColorStop(1, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0)");
      g.save();
      g.scale(1, ry / rx);
      g.fillStyle = rg;
      g.beginPath();
      g.arc(0, y / (ry / rx), rx, 0, TAU);
      g.fill();
      g.restore();
    }
    g.restore();

    // Sweeping currents of brushwork, centered on the black hole.
    g.save();
    for (let band = 0; band < 8; band++) {
      const dir = band % 3 === 1 ? -1 : 1;
      const start = rand() * TAU;
      const r0 = S * (0.08 + rand() * 0.32);
      const turns = 0.8 + rand() * 1.65;
      const steps = 44 + ((rand() * 42) | 0);
      for (let i = 0; i < steps; i++) {
        const q = i / steps;
        const a = start + dir * q * turns * TAU * 0.66;
        const radius = r0 + q * S * (0.32 + rand() * 0.36);
        const x = cx + Math.cos(a) * radius * (1.02 + (rand() - 0.5) * 0.12);
        const y = cy + Math.sin(a) * radius * (0.8 + (rand() - 0.5) * 0.13);
        if (x < -50 || x > w + 50 || y < -50 || y > h + 50) continue;
        const colorRoll = rand();
        const color = colorRoll < 0.46 ? pick(rand, theme.blues) : colorRoll < 0.8 ? pick(rand, theme.teals) : pick(rand, theme.golds);
        paintDab(g, rand, x, y, a + Math.PI / 2 * dir + 0.1, color, [0.035, 0.12], [5, 23], [0.8, 2.8]);
      }
    }

    // Loose, noncircular wind strokes across the entire panel.
    for (let i = 0; i < 620; i++) {
      const x = rand() * w;
      const y = rand() * h;
      const dx = x - cx;
      const dy = y - cy;
      const d = Math.sqrt(dx * dx + dy * dy);
      if (d < S * 0.12) continue;
      const a = Math.atan2(dy, dx) + Math.PI / 2 + (rand() - 0.5) * 0.6;
      const color = rand() < 0.55 ? pick(rand, theme.blues) : rand() < 0.76 ? pick(rand, theme.teals) : rand() < 0.9 ? pick(rand, theme.golds) : theme.violet;
      paintDab(g, rand, x, y, a, color, [0.025, 0.085], [3, 16], [0.6, 2.2]);
    }
    g.restore();

    // A dominant golden swirl, like the wind in the original night.
    g.save();
    const swirlA = -2.15;
    for (let i = 0; i < 210; i++) {
      const q = i / 210;
      const a = swirlA + q * 4.5;
      const radius = S * (0.11 + q * 0.29);
      const x = cx + Math.cos(a) * radius;
      const y = cy + Math.sin(a) * radius * 0.72;
      if (x < -20 || x > w + 20 || y < -20 || y > h + 20) continue;
      paintDab(g, rand, x, y, a + Math.PI / 2, pick(rand, theme.golds), [0.04, 0.13], [5, 18], [1, 2.6]);
    }
    g.restore();

    // Tiny pinprick stars.
    for (let i = 0; i < 700; i++) {
      const x = rand() * w;
      const y = rand() * h;
      const dx = x - cx;
      const dy = y - cy;
      if (dx * dx + dy * dy < S * S * 0.022) continue;
      const a = 0.15 + Math.pow(rand(), 2.2) * 0.7;
      const r = 0.35 + rand() * 1.25;
      const color = rand() > 0.82 ? pick(rand, [theme.teal, theme.gold, theme.coral]) : theme.cream;
      g.fillStyle = rgba(color, a);
      g.beginPath();
      g.arc(x, y, r, 0, TAU);
      g.fill();
      if (r > 1.25) {
        g.strokeStyle = rgba(color, a * 0.35);
        g.lineWidth = 0.5;
        g.beginPath();
        g.moveTo(x - r * 3, y);
        g.lineTo(x + r * 3, y);
        g.moveTo(x, y - r * 3);
        g.lineTo(x, y + r * 3);
        g.stroke();
      }
    }

    // Bright Van Gogh stars with pressing halos.
    const halos = [];
    const haloCount = 19;
    for (let i = 0; i < haloCount; i++) {
      let x;
      let y;
      let guard = 0;
      do {
        x = w * (0.06 + rand() * 0.88);
        y = h * (0.08 + rand() * 0.82);
        guard++;
      } while (guard < 12 && Math.sqrt((x - cx) ** 2 + (y - cy) ** 2) < S * 0.2);
      const size = (3.5 + Math.pow(rand(), 1.8) * 13) * (S / 800);
      paintHaloStar(g, rand, x, y, size, theme, 0.9);
      halos.push({ x, y, r: size, phase: rand() * TAU, speed: 0.3 + rand() * 0.6, color: rand() > 0.35 ? theme.halo : theme.cream });
    }

    // The moon — a quiet, golden eye in the upper right.
    paintMoon(g, rand, w * 0.82, h * 0.18, Math.max(15, S * 0.036), theme, 0.82);

    // Occasional large stars with very thin brush rings.
    for (let i = 0; i < 7; i++) {
      const x = w * (0.08 + rand() * 0.84);
      const y = h * (0.1 + rand() * 0.8);
      if (Math.sqrt((x - cx) ** 2 + (y - cy) ** 2) < S * 0.23) continue;
      paintHaloStar(g, rand, x, y, (7 + rand() * 9) * S / 800, theme, 0.5);
    }

    // A central pool of blue-black, so the living black hole has a stage.
    const center = g.createRadialGradient(cx, cy, S * 0.03, cx, cy, S * 0.29);
    center.addColorStop(0, rgba(theme.ink, 0.85));
    center.addColorStop(0.52, rgba(theme.ink, 0.4));
    center.addColorStop(1, rgba(theme.ink, 0));
    g.fillStyle = center;
    g.fillRect(cx - S * 0.3, cy - S * 0.3, S * 0.6, S * 0.6);

    // A few gold flecks floating over the dark.
    g.save();
    g.globalCompositeOperation = "lighter";
    for (let i = 0; i < 34; i++) {
      const a = rand() * TAU;
      const rr = S * (0.18 + rand() * 0.64);
      const x = cx + Math.cos(a) * rr;
      const y = cy + Math.sin(a) * rr * 0.9;
      if (x < 0 || x > w || y < 0 || y > h) continue;
      dab(g, x, y, a + 1.2, 1.5 + rand() * 4, 0.5 + rand(), rgba(theme.golds[1], 0.2 + rand() * 0.25), 0.3);
    }
    g.restore();

    // Store a handful of stars for the live twinkle pass.
    const twinkles = halos
      .filter((_, i) => i % 2 === 0)
      .slice(0, 13)
      .map((star) => ({
        x: star.x,
        y: star.y,
        r: star.r,
        phase: star.phase,
        speed: star.speed,
        color: star.color
      }));

    return { canvas, w, h, theme, twinkles, seed };
  }

  function drawBackdrop(ctx, backdrop, w, h, time) {
    if (!backdrop) return;
    ctx.drawImage(backdrop.canvas, 0, 0, w, h);
    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < backdrop.twinkles.length; i++) {
      const star = backdrop.twinkles[i];
      const pulse = 0.5 + 0.5 * Math.sin(time * star.speed + star.phase);
      const r = star.r * (1.5 + pulse * 0.7);
      glow(ctx, star.color, star.x, star.y, r * 2.4, 0.04 + pulse * 0.07);
      ctx.globalAlpha = 0.12 + pulse * 0.13;
      ctx.fillStyle = rgba("#fff9ea", 0.8);
      ctx.beginPath();
      ctx.arc(star.x, star.y, Math.max(0.6, star.r * 0.16), 0, TAU);
      ctx.fill();
    }
    ctx.restore();
  }

  /* -------------------------------------------------------
     The living black hole
     ------------------------------------------------------- */

  function blackHole(ctx, opts) {
    const t = opts.time;
    const r = opts.radius;
    const theme = opts.theme;
    const danger = opts.danger || 0;
    const collapse = opts.collapse || 0;
    const grow = opts.grow || 0;
    const R = r * (1 + Math.sin(t * 0.68) * 0.008 + grow * 0.3);
    const cGold = theme.golds[1];
    const cCream = theme.cream;
    const cTeal = theme.teals[1];
    const cBlue = theme.blues[1];

    ctx.save();

    // The gravitational breath around everything.
    glow(ctx, cGold, 0, 0, R * 7.8, 0.09 + danger * 0.07 + collapse * 0.08);
    glow(ctx, cTeal, 0, 0, R * 5.6, 0.08 + collapse * 0.04);
    glow(ctx, cBlue, 0, 0, R * 3.3, 0.09);

    // Long arms of nebular light, dragged around the singularity.
    ctx.globalCompositeOperation = "lighter";
    for (let arm = 0; arm < 5; arm++) {
      ctx.beginPath();
      const base = arm / 5 * TAU + t * (0.035 + arm * 0.006) + arm * 0.8;
      for (let i = 0; i < 28; i++) {
        const q = i / 27;
        const radius = R * (1.2 + q * 4.6);
        const a = base + q * 2.8 + Math.sin(t * 0.16 + arm) * 0.08;
        const x = Math.cos(a) * radius;
        const y = Math.sin(a) * radius * 0.66;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.strokeStyle = rgba(arm % 2 === 0 ? cGold : cTeal, 0.055 + collapse * 0.08);
      ctx.lineWidth = arm % 2 === 0 ? 1.6 : 0.9;
      ctx.lineCap = "round";
      ctx.stroke();
    }

    // Accretion disk — tilted, glowing, turning.
    ctx.save();
    ctx.rotate(-0.2);
    ctx.scale(1, 0.55);
    ctx.globalCompositeOperation = "lighter";

    const disk = ctx.createRadialGradient(0, 0, R * 1.0, 0, 0, R * 3.6);
    disk.addColorStop(0, rgba(cCream, 0.42 + collapse * 0.15));
    disk.addColorStop(0.12, rgba(cGold, 0.35));
    disk.addColorStop(0.34, rgba(cGold, 0.15));
    disk.addColorStop(0.58, rgba(cTeal, 0.075));
    disk.addColorStop(0.82, rgba(cBlue, 0.035));
    disk.addColorStop(1, rgba(cBlue, 0));
    ctx.fillStyle = disk;
    ctx.beginPath();
    ctx.arc(0, 0, R * 3.65, 0, TAU);
    ctx.fill();

    let conic = null;
    if (ctx.createConicGradient) {
      conic = ctx.createConicGradient(t * 0.075, 0, 0);
      conic.addColorStop(0, rgba(cGold, 0.04));
      conic.addColorStop(0.12, rgba(cCream, 0.75));
      conic.addColorStop(0.21, rgba(cGold, 0.32));
      conic.addColorStop(0.37, rgba(cTeal, 0.34));
      conic.addColorStop(0.53, rgba(cBlue, 0.02));
      conic.addColorStop(0.68, rgba(cTeal, 0.25));
      conic.addColorStop(0.82, rgba(cGold, 0.55));
      conic.addColorStop(0.93, rgba(theme.coral, 0.08));
      conic.addColorStop(1, rgba(cGold, 0.04));
    }

    for (let i = 0; i < 18; i++) {
      const radius = R * (1.28 + i * 0.115 + Math.sin(t * 0.2 + i) * 0.015);
      const start = t * (0.045 + (i % 3) * 0.01) + i * 0.62;
      const len = 0.7 + ((i * 7) % 10) / 10 * 2.4;
      ctx.beginPath();
      ctx.arc(0, 0, radius, start, start + len + collapse * 1.2);
      ctx.strokeStyle = conic || rgba(cGold, 0.12);
      ctx.globalAlpha = 0.2 + (1 - i / 22) * 0.22 + collapse * 0.14;
      ctx.lineWidth = 0.55 + (1 - i / 22) * 2.0;
      ctx.stroke();
    }

    // Loaded brush daubs around the bright rim.
    for (let i = 0; i < 150; i++) {
      const seedA = i * 2.399963229;
      const radius = R * (1.16 + ((i * 37) % 100) / 100 * 2.15);
      const a = seedA + t * (0.08 + (i % 5) * 0.006) * (i % 2 === 0 ? 1 : -0.62);
      const len = R * (0.035 + ((i * 13) % 10) / 10 * 0.16);
      const x = Math.cos(a) * radius;
      const y = Math.sin(a) * radius;
      const color = i % 7 === 0 ? cCream : i % 3 === 0 ? cTeal : cGold;
      gDab(ctx, x, y, a + Math.PI / 2, len, 0.7 + (i % 4) * 0.4, color, 0.11 + (i % 5) * 0.025);
    }

    // Infalling streaks.
    for (let i = 0; i < 22; i++) {
      const phase = (t * (0.08 + (i % 4) * 0.012) + i * 0.173) % 1;
      const a = i * 2.399963 + t * 0.02;
      const radius = R * (4.3 - phase * 2.65);
      const x = Math.cos(a) * radius;
      const y = Math.sin(a) * radius * 0.64;
      const len = R * (0.25 + (1 - phase) * 0.6);
      ctx.globalAlpha = (1 - phase) * 0.34 + 0.04;
      ctx.strokeStyle = rgba(i % 3 === 0 ? cCream : cGold, 0.55);
      ctx.lineWidth = 0.7 + (1 - phase) * 1.1;
      ctx.beginPath();
      ctx.moveTo(x + Math.cos(a) * len, y + Math.sin(a) * len * 0.64);
      ctx.lineTo(x, y);
      ctx.stroke();
    }
    ctx.restore();

    // A last halo of gold outside the tilted disk.
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < 9; i++) {
      const radius = R * (2.2 + i * 0.12);
      ctx.beginPath();
      ctx.arc(0, Math.sin(t * 0.24 + i) * R * 0.018, radius, t * 0.015 + i * 0.7, t * 0.015 + i * 0.7 + 0.8 + (i % 3) * 0.24);
      ctx.strokeStyle = rgba(i % 2 ? cTeal : cGold, 0.1 + collapse * 0.08);
      ctx.lineWidth = 0.55;
      ctx.stroke();
    }

    // Event horizon: a pure darkness with a thin, impossible rim.
    ctx.globalCompositeOperation = "source-over";
    const dark = ctx.createRadialGradient(0, 0, R * 0.35, 0, 0, R * 1.055);
    dark.addColorStop(0, "#000000");
    dark.addColorStop(0.68, "#000000");
    dark.addColorStop(0.86, "rgba(0,0,0,0.99)");
    dark.addColorStop(0.96, "rgba(3,5,9,0.88)");
    dark.addColorStop(1, "rgba(4,6,10,0)");
    ctx.fillStyle = dark;
    ctx.beginPath();
    ctx.arc(0, 0, R * 1.055, 0, TAU);
    ctx.fill();

    // Photon ring.
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < 3; i++) {
      ctx.beginPath();
      ctx.arc(0, 0, R * (1.012 + i * 0.014), 0, TAU);
      ctx.strokeStyle = rgba(i === 0 ? "#fff9ea" : cGold, i === 0 ? 0.82 : 0.22);
      ctx.lineWidth = i === 0 ? 1.1 : 3.4;
      ctx.stroke();
    }

    // An Einstein ring, compressed by the tilt.
    ctx.save();
    ctx.rotate(-0.38);
    ctx.scale(1, 0.54);
    for (let i = 0; i < 3; i++) {
      const rr = R * (1.31 + i * 0.23);
      const start = -0.3 + i * 0.5 + Math.sin(t * 0.21 + i) * 0.08;
      ctx.beginPath();
      ctx.arc(0, 0, rr, start, start + 2.4 - i * 0.2);
      ctx.strokeStyle = rgba(i === 0 ? cCream : i === 1 ? cGold : cTeal, 0.24 - i * 0.05);
      ctx.lineWidth = i === 0 ? 1.2 : 0.7;
      ctx.stroke();
    }
    ctx.restore();

    // Fine bright flecks circling the rim.
    for (let i = 0; i < 26; i++) {
      const a = i * 2.399963 + t * (0.1 + (i % 4) * 0.01);
      const radius = R * (1.15 + (i % 7) * 0.08);
      const alpha = 0.18 + (i % 3) * 0.1;
      ctx.fillStyle = rgba(i % 4 === 0 ? cCream : cGold, alpha);
      ctx.beginPath();
      ctx.arc(Math.cos(a) * radius, Math.sin(a) * radius * 0.62, 0.7 + (i % 3) * 0.45, 0, TAU);
      ctx.fill();
    }

    // The last light as everything falls.
    if (collapse > 0.05) {
      const pulse = clamp((collapse - 0.2) / 0.8, 0, 1);
      glow(ctx, cCream, 0, 0, R * (2.2 + pulse * 3.5), 0.05 + pulse * 0.2);
      ctx.globalAlpha = 0.18 + pulse * 0.22;
      for (let i = 0; i < 11; i++) {
        const a = i / 11 * TAU + t * 0.3;
        ctx.strokeStyle = rgba(i % 2 ? cGold : cTeal, 0.2 + pulse * 0.4);
        ctx.lineWidth = 0.6;
        ctx.beginPath();
        ctx.moveTo(Math.cos(a) * R * 1.1, Math.sin(a) * R * 1.1);
        ctx.lineTo(Math.cos(a) * R * (2.1 + pulse * 3.2), Math.sin(a) * R * (2.1 + pulse * 3.2));
        ctx.stroke();
      }
    }

    ctx.restore();
  }

  // A local brush dab for use inside an already transformed context.
  function gDab(ctx, x, y, angle, len, wid, color, alpha) {
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(angle);
    ctx.globalAlpha = alpha;
    ctx.strokeStyle = color;
    ctx.lineWidth = wid;
    ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(-len * 0.5, 0);
    ctx.quadraticCurveTo(0, len * 0.07, len * 0.5, 0);
    ctx.stroke();
    ctx.restore();
  }

  /* -------------------------------------------------------
     The wayfarer's gate
     ------------------------------------------------------- */

  function portal(ctx, opts) {
    const { x, y, radius, time, theme, near, flare } = opts;
    const r = radius;
    const t = time;
    const nearAmount = clamp(near || 0, 0, 1);
    const flareAmount = clamp(flare || 0, 0, 1);

    ctx.save();
    ctx.translate(x, y);

    glow(ctx, theme.portal, 0, 0, r * 5.4, 0.24 + nearAmount * 0.13 + flareAmount * 0.2);
    glow(ctx, theme.teals[1], 0, 0, r * 3.2, 0.12 + nearAmount * 0.08);

    const c = hexToRgb(theme.portal);
    const halo = ctx.createRadialGradient(0, 0, 0, 0, 0, r * 2.8);
    halo.addColorStop(0, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.5)");
    halo.addColorStop(0.16, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.18)");
    halo.addColorStop(0.52, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0.035)");
    halo.addColorStop(1, "rgba(" + c[0] + "," + c[1] + "," + c[2] + ",0)");
    ctx.globalCompositeOperation = "lighter";
    ctx.fillStyle = halo;
    ctx.beginPath();
    ctx.arc(0, 0, r * 2.8, 0, TAU);
    ctx.fill();

    // Orbiting rings of pale fire.
    const ringData = [
      { rr: 1.24, speed: 0.12, dash: [10, 16], width: 1.0, color: theme.cream, alpha: 0.42 },
      { rr: 0.96, speed: -0.2, dash: [2, 9], width: 0.7, color: theme.gold, alpha: 0.55 },
      { rr: 0.71, speed: 0.34, dash: [24, 13], width: 1.1, color: theme.teal, alpha: 0.38 }
    ];
    ringData.forEach((ring, i) => {
      ctx.save();
      ctx.rotate(t * ring.speed + i * 0.7);
      ctx.setLineDash(ring.dash);
      ctx.lineDashOffset = t * (8 + i * 5);
      ctx.strokeStyle = rgba(ring.color, ring.alpha + nearAmount * 0.15);
      ctx.lineWidth = ring.width;
      ctx.beginPath();
      ctx.arc(0, 0, r * ring.rr, 0, TAU);
      ctx.stroke();
      ctx.restore();
    });

    // Twelve quiet marks, like a celestial clock.
    for (let i = 0; i < 12; i++) {
      const a = i / 12 * TAU + Math.PI / 12;
      ctx.save();
      ctx.rotate(a);
      ctx.translate(0, -r * 1.11);
      ctx.strokeStyle = rgba(theme.cream, 0.36 + nearAmount * 0.2);
      ctx.lineWidth = i % 3 === 0 ? 1.3 : 0.7;
      ctx.beginPath();
      ctx.moveTo(0, -r * 0.1);
      ctx.lineTo(0, i % 3 === 0 ? r * 0.12 : r * 0.055);
      ctx.stroke();
      ctx.restore();
    }

    // Spiral arms inside the gate.
    for (let arm = 0; arm < 4; arm++) {
      ctx.save();
      ctx.rotate(t * 0.13 + arm / 4 * TAU);
      ctx.beginPath();
      for (let i = 0; i < 18; i++) {
        const q = i / 17;
        const a = q * 2.1;
        const rr = r * (0.12 + q * 0.68);
        const px = Math.cos(a) * rr;
        const py = Math.sin(a) * rr;
        if (i === 0) ctx.moveTo(px, py);
        else ctx.lineTo(px, py);
      }
      ctx.strokeStyle = rgba(arm % 2 ? theme.gold : theme.cream, 0.25 + nearAmount * 0.15);
      ctx.lineWidth = 0.9;
      ctx.stroke();
      ctx.restore();
    }

    // Core light.
    const core = ctx.createRadialGradient(0, 0, 0, 0, 0, r * 0.58);
    core.addColorStop(0, rgba("#fffdf1", 0.82));
    core.addColorStop(0.2, rgba(theme.cream, 0.58));
    core.addColorStop(0.55, rgba(theme.portal, 0.18));
    core.addColorStop(1, rgba(theme.portal, 0));
    ctx.fillStyle = core;
    ctx.beginPath();
    ctx.arc(0, 0, r * 0.58, 0, TAU);
    ctx.fill();

    // Soft horizontal and vertical blades of light.
    const beam = ctx.createLinearGradient(-r * 2.6, 0, r * 2.6, 0);
    beam.addColorStop(0, rgba(theme.portal, 0));
    beam.addColorStop(0.25, rgba(theme.portal, 0.1 + flareAmount * 0.12));
    beam.addColorStop(0.5, rgba(theme.cream, 0.3 + flareAmount * 0.2));
    beam.addColorStop(0.75, rgba(theme.portal, 0.1 + flareAmount * 0.12));
    beam.addColorStop(1, rgba(theme.portal, 0));
    ctx.fillStyle = beam;
    ctx.fillRect(-r * 2.6, -0.7, r * 5.2, 1.4);
    ctx.save();
    ctx.rotate(Math.PI / 2);
    ctx.fillRect(-r * 2.6, -0.4, r * 5.2, 0.8);
    ctx.restore();

    // Dust orbiting the gate.
    for (let i = 0; i < 13; i++) {
      const a = i * 2.399963 + t * (0.1 + (i % 3) * 0.03);
      const rr = r * (1.45 + (i % 4) * 0.16);
      const alpha = 0.18 + (i % 3) * 0.1 + nearAmount * 0.12;
      ctx.fillStyle = rgba(i % 3 === 0 ? theme.cream : theme.portal, alpha);
      ctx.beginPath();
      ctx.arc(Math.cos(a) * rr, Math.sin(a) * rr * 0.82, 0.7 + (i % 2) * 0.8, 0, TAU);
      ctx.fill();
    }

    if (nearAmount > 0.01 || flareAmount > 0.01) {
      ctx.globalAlpha = nearAmount * 0.35 + flareAmount * 0.4;
      ctx.strokeStyle = rgba(theme.cream, 0.5);
      ctx.lineWidth = 0.6;
      for (let i = 0; i < 3; i++) {
        const rr = r * (1.7 + i * 0.3 + Math.sin(t * 2 + i) * 0.04);
        ctx.beginPath();
        ctx.arc(0, 0, rr, t * 0.2 + i, t * 0.2 + i + 2.1);
        ctx.stroke();
      }
    }

    ctx.restore();
  }

  function portalLabel(ctx, x, y, radius, theme, alpha) {
    ctx.save();
    ctx.translate(x, y + radius * 1.55);
    ctx.textAlign = "center";
    ctx.textBaseline = "top";
    ctx.fillStyle = rgba(theme.cream, 0.56 * alpha);
    ctx.font = '11px "Songti SC","STSong","Noto Serif CJK SC","SimSun",serif';
    spacedText(ctx, "归航阵", 0, 0, 4);
    ctx.fillStyle = rgba(theme.gold, 0.3 * alpha);
    ctx.font = '7px "Cormorant Garamond","Palatino Linotype",serif';
    spacedText(ctx, "WAYFARER'S GATE", 0, 18, 2.2);
    ctx.restore();
  }

  /* -------------------------------------------------------
     Arena boundary
     ------------------------------------------------------- */

  function boundary(ctx, opts) {
    const { radius, time, theme, portalAngle } = opts;
    ctx.save();
    ctx.globalCompositeOperation = "lighter";

    ctx.strokeStyle = rgba(theme.cream, 0.09);
    ctx.lineWidth = 0.7;
    ctx.setLineDash([1, 11]);
    ctx.beginPath();
    ctx.arc(0, 0, radius, 0, TAU);
    ctx.stroke();

    ctx.setLineDash([38, 72]);
    ctx.lineDashOffset = -time * 0.7;
    ctx.strokeStyle = rgba(theme.teal, 0.12);
    ctx.beginPath();
    ctx.arc(0, 0, radius + 7, 0, TAU);
    ctx.stroke();

    ctx.setLineDash([]);
    for (let i = 0; i < 54; i++) {
      const a = i / 54 * TAU + time * 0.003;
      const twinkle = 0.5 + 0.5 * Math.sin(time * 0.35 + i * 2.7);
      ctx.globalAlpha = 0.06 + twinkle * 0.12;
      ctx.fillStyle = theme.cream;
      ctx.beginPath();
      ctx.arc(Math.cos(a) * radius, Math.sin(a) * radius, i % 9 === 0 ? 1.4 : 0.65, 0, TAU);
      ctx.fill();
    }

    // A golden cove around the wayfarer's gate.
    const gateArc = 0.18;
    ctx.lineCap = "round";
    for (let i = 0; i < 3; i++) {
      ctx.globalAlpha = 0.24 - i * 0.06;
      ctx.strokeStyle = i === 0 ? theme.cream : theme.portal;
      ctx.lineWidth = i === 0 ? 1 : 4 - i;
      ctx.beginPath();
      ctx.arc(0, 0, radius + (i - 1) * 3, portalAngle - gateArc, portalAngle + gateArc);
      ctx.stroke();
    }

    ctx.restore();
  }

  /* -------------------------------------------------------
     Celestial bodies
     ------------------------------------------------------- */

  const bodyMeta = new WeakMap();

  function getMeta(body) {
    let meta = bodyMeta.get(body);
    if (meta) return meta;
    const rand = mulberry32((body.seed || 12345) >>> 0);
    meta = {};
    if (body.type === "star") {
      meta.rays = [];
      const count = 8 + ((rand() * 6) | 0);
      for (let i = 0; i < count; i++) {
        meta.rays.push({
          a: i / count * TAU + (rand() - 0.5) * 0.28,
          len: 0.34 + rand() * 0.9,
          width: 0.5 + rand() * 1.1,
          alpha: 0.1 + rand() * 0.22,
          bend: (rand() - 0.5) * 0.28
        });
      }
      meta.halos = [];
      for (let i = 0; i < 3; i++) {
        meta.halos.push({
          radius: 1.18 + i * 0.38 + rand() * 0.12,
          start: rand() * TAU,
          pieces: 2 + ((rand() * 3) | 0),
          width: 0.5 + rand() * 0.8,
          alpha: 0.18 + rand() * 0.22
        });
      }
      meta.speckles = [];
      for (let i = 0; i < 7; i++) {
        meta.speckles.push({
          a: rand() * TAU,
          r: 0.82 + rand() * 0.55,
          len: 0.18 + rand() * 0.35,
          width: 0.5 + rand() * 0.8
        });
      }
    } else if (body.type === "planet") {
      meta.hasRing = body.hasRing != null ? body.hasRing : rand() > 0.48;
      meta.ringTilt = -0.5 + rand() * 1.0;
      meta.ringScale = 1.45 + rand() * 0.4;
      meta.bands = [];
      for (let i = 0; i < 4; i++) {
        meta.bands.push({
          y: -0.6 + i * 0.36 + rand() * 0.12,
          bend: (rand() - 0.5) * 0.28,
          width: 0.08 + rand() * 0.12,
          alpha: 0.06 + rand() * 0.12
        });
      }
      meta.craters = [];
      for (let i = 0; i < 3; i++) {
        meta.craters.push({
          x: (rand() - 0.5) * 1.1,
          y: (rand() - 0.5) * 1.1,
          r: 0.06 + rand() * 0.12,
          a: 0.04 + rand() * 0.08
        });
      }
    } else if (body.type === "meteor") {
      meta.points = [];
      const n = 8 + ((rand() * 5) | 0);
      for (let i = 0; i < n; i++) {
        const a = i / n * TAU;
        const rr = 0.68 + rand() * 0.42;
        meta.points.push({ a, r: rr });
      }
      meta.craters = [];
      for (let i = 0; i < 3; i++) {
        meta.craters.push({
          x: (rand() - 0.5) * 1.0,
          y: (rand() - 0.5) * 1.0,
          r: 0.07 + rand() * 0.13,
          a: 0.08 + rand() * 0.12
        });
      }
    } else {
      meta.spread = rand() * TAU;
    }
    bodyMeta.set(body, meta);
    return meta;
  }

  function drawTrail(ctx, body, alphaScale) {
    const trail = body.trail;
    if (!trail || trail.length < 2) return;
    const color = body.trailColor || body.color;
    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    ctx.lineCap = "round";
    const total = trail.length;
    for (let i = 1; i < total; i++) {
      const p = trail[i - 1];
      const q = trail[i];
      const frac = i / total;
      const alpha = (1 - frac) * (body.type === "star" ? 0.16 : body.type === "planet" ? 0.1 : 0.07) * alphaScale;
      if (alpha <= 0.002) continue;
      ctx.strokeStyle = rgba(color, alpha);
      ctx.lineWidth = Math.max(0.45, body.size * (0.24 * (1 - frac) + 0.035));
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(q.x, q.y);
      ctx.stroke();
    }
    ctx.restore();
  }

  function drawBody(ctx, body, time, theme) {
    const x = body.x;
    const y = body.y;
    const s = body.size;
    const alpha = body.grace > 0 ? 0.55 + 0.45 * Math.sin(time * 22 + body.seed) : 1;
    const meta = getMeta(body);

    drawTrail(ctx, body, 1);

    ctx.save();
    ctx.globalAlpha = alpha;
    ctx.translate(x, y);

    if (body.type === "dust") {
      glow(ctx, body.color || theme.cream, 0, 0, s * 4.2, 0.1);
      ctx.globalCompositeOperation = "lighter";
      ctx.fillStyle = rgba(body.color || theme.cream, 0.8);
      ctx.beginPath();
      ctx.arc(0, 0, Math.max(0.65, s * 0.46), 0, TAU);
      ctx.fill();
      ctx.strokeStyle = rgba(theme.gold, 0.16);
      ctx.lineWidth = 0.45;
      ctx.beginPath();
      ctx.moveTo(-s * 2.1, 0);
      ctx.lineTo(s * 2.1, 0);
      ctx.stroke();
      ctx.restore();
      return;
    }

    if (body.type === "star") {
      const color = body.color || theme.star;
      glow(ctx, color, 0, 0, s * 4.4, 0.27);
      glow(ctx, theme.gold, 0, 0, s * 2.8, 0.14);
      ctx.rotate(body.rot || 0);
      ctx.globalCompositeOperation = "lighter";

      // Van Gogh halo fragments.
      for (let i = 0; i < meta.halos.length; i++) {
        const h = meta.halos[i];
        const ringStart = h.start + time * (0.025 + i * 0.007) * (i % 2 ? -1 : 1);
        ctx.strokeStyle = rgba(i === 0 ? theme.cream : color, h.alpha);
        ctx.lineWidth = h.width;
        for (let p = 0; p < h.pieces; p++) {
          const a = ringStart + p / h.pieces * TAU;
          ctx.beginPath();
          ctx.arc(0, 0, s * h.radius, a, a + 0.65 + p * 0.16);
          ctx.stroke();
        }
      }

      // Curled rays.
      for (let i = 0; i < meta.rays.length; i++) {
        const ray = meta.rays[i];
        const x0 = Math.cos(ray.a) * s * 0.8;
        const y0 = Math.sin(ray.a) * s * 0.8;
        const x1 = Math.cos(ray.a + 0.16) * s * (1 + ray.len);
        const y1 = Math.sin(ray.a + 0.16) * s * (1 + ray.len);
        ctx.strokeStyle = rgba(i % 4 === 0 ? theme.teal : theme.cream, ray.alpha);
        ctx.lineWidth = ray.width;
        ctx.beginPath();
        ctx.moveTo(x0, y0);
        ctx.quadraticCurveTo(
          Math.cos(ray.a + ray.bend) * s * (1 + ray.len * 0.55),
          Math.sin(ray.a + ray.bend) * s * (1 + ray.len * 0.55),
          x1,
          y1
        );
        ctx.stroke();
      }

      // Speckled corona.
      for (let i = 0; i < meta.speckles.length; i++) {
        const sp = meta.speckles[i];
        const a = sp.a + time * 0.045;
        gDab(ctx, Math.cos(a) * s * sp.r, Math.sin(a) * s * sp.r, a, s * sp.len, sp.width, rgba(theme.gold, 0.2), 0.4);
      }

      const core = ctx.createRadialGradient(-s * 0.18, -s * 0.22, s * 0.08, 0, 0, s * 1.02);
      core.addColorStop(0, "rgba(255,253,244,0.98)");
      core.addColorStop(0.25, rgba(theme.cream, 0.9));
      core.addColorStop(0.6, rgba(color, 0.55));
      core.addColorStop(0.9, rgba(color, 0.08));
      core.addColorStop(1, rgba(color, 0));
      ctx.fillStyle = core;
      ctx.beginPath();
      ctx.arc(0, 0, s * 1.03, 0, TAU);
      ctx.fill();

      ctx.fillStyle = "rgba(255,250,230,0.95)";
      ctx.beginPath();
      ctx.arc(-s * 0.09, -s * 0.12, s * 0.32, 0, TAU);
      ctx.fill();
      ctx.restore();
      return;
    }

    if (body.type === "planet") {
      const color = body.color || theme.planets[0];
      const accent = body.accent || theme.teals[1];
      glow(ctx, accent, 0, 0, s * 2.9, 0.16);
      ctx.rotate(body.rot || 0);

      if (meta.hasRing) {
        ctx.save();
        ctx.rotate(meta.ringTilt);
        ctx.strokeStyle = rgba(lighten(color, 0.35), 0.28);
        ctx.lineWidth = Math.max(0.7, s * 0.08);
        ctx.beginPath();
        ctx.ellipse(0, 0, s * meta.ringScale, s * 0.27, 0, 0, TAU);
        ctx.stroke();
        ctx.strokeStyle = rgba(theme.gold, 0.13);
        ctx.lineWidth = 0.6;
        ctx.beginPath();
        ctx.ellipse(0, 0, s * meta.ringScale * 1.21, s * 0.31, 0, Math.PI * 0.08, Math.PI * 0.92);
        ctx.stroke();
        ctx.restore();
      }

      const sphere = ctx.createRadialGradient(-s * 0.42, -s * 0.4, s * 0.05, 0, 0, s * 1.08);
      sphere.addColorStop(0, lighten(color, 0.5));
      sphere.addColorStop(0.34, lighten(color, 0.14));
      sphere.addColorStop(0.76, color);
      sphere.addColorStop(1, rgba(mix(color, theme.ink, 0.55), 0.98));
      ctx.fillStyle = sphere;
      ctx.beginPath();
      ctx.arc(0, 0, s, 0, TAU);
      ctx.fill();

      ctx.save();
      ctx.beginPath();
      ctx.arc(0, 0, s, 0, TAU);
      ctx.clip();
      for (let i = 0; i < meta.bands.length; i++) {
        const band = meta.bands[i];
        ctx.strokeStyle = rgba(i % 2 ? accent : theme.cream, band.alpha);
        ctx.lineWidth = s * band.width;
        ctx.beginPath();
        ctx.moveTo(-s, band.y * s);
        ctx.quadraticCurveTo(0, (band.y + band.bend) * s, s, band.y * s);
        ctx.stroke();
      }
      for (let i = 0; i < meta.craters.length; i++) {
        const crater = meta.craters[i];
        ctx.fillStyle = rgba(theme.ink, crater.a);
        ctx.beginPath();
        ctx.arc(crater.x * s, crater.y * s, crater.r * s, 0, TAU);
        ctx.fill();
      }
      // Cool side of the world.
      const shade = ctx.createLinearGradient(-s, -s, s, s);
      shade.addColorStop(0, "rgba(2,6,12,0)");
      shade.addColorStop(0.58, "rgba(2,6,12,0.05)");
      shade.addColorStop(1, "rgba(2,6,12,0.6)");
      ctx.fillStyle = shade;
      ctx.fillRect(-s, -s, s * 2, s * 2);
      ctx.restore();

      ctx.strokeStyle = rgba(theme.cream, 0.38);
      ctx.lineWidth = 0.65;
      ctx.beginPath();
      ctx.arc(0, 0, s * 0.99, Math.PI * 1.05, Math.PI * 1.62);
      ctx.stroke();

      if (meta.hasRing) {
        ctx.save();
        ctx.rotate(meta.ringTilt);
        ctx.strokeStyle = rgba(lighten(color, 0.5), 0.42);
        ctx.lineWidth = Math.max(0.8, s * 0.075);
        ctx.beginPath();
        ctx.ellipse(0, 0, s * meta.ringScale, s * 0.27, 0, 0, Math.PI);
        ctx.stroke();
        ctx.restore();
      }
      ctx.restore();
      return;
    }

    // Meteorite.
    const color = body.color || theme.golds[0];
    glow(ctx, theme.coral, 0, 0, s * 2.4, 0.1);
    ctx.rotate(body.rot || 0);
    ctx.beginPath();
    for (let i = 0; i < meta.points.length; i++) {
      const p = meta.points[i];
      const px = Math.cos(p.a) * s * p.r;
      const py = Math.sin(p.a) * s * p.r;
      if (i === 0) ctx.moveTo(px, py);
      else ctx.lineTo(px, py);
    }
    ctx.closePath();
    const rock = ctx.createLinearGradient(-s, -s, s, s);
    rock.addColorStop(0, lighten(color, 0.2));
    rock.addColorStop(0.42, color);
    rock.addColorStop(1, rgba(mix(color, theme.ink, 0.6), 0.98));
    ctx.fillStyle = rock;
    ctx.fill();
    ctx.strokeStyle = rgba(theme.cream, 0.35);
    ctx.lineWidth = 0.6;
    ctx.stroke();

    for (let i = 0; i < meta.craters.length; i++) {
      const crater = meta.craters[i];
      ctx.fillStyle = rgba(theme.ink, crater.a);
      ctx.beginPath();
      ctx.arc(crater.x * s, crater.y * s, crater.r * s, 0, TAU);
      ctx.fill();
    }
    ctx.strokeStyle = rgba(theme.coral, 0.38);
    ctx.lineWidth = 0.55;
    ctx.beginPath();
    ctx.moveTo(-s * 0.3, -s * 0.7);
    ctx.quadraticCurveTo(s * 0.2, -s, s * 0.65, -s * 0.45);
    ctx.stroke();
    ctx.restore();
  }

  /* -------------------------------------------------------
     The player's star
     ------------------------------------------------------- */

  function drawPlayer(ctx, player, time, theme) {
    if (!player || !player.alive) return;
    const x = player.x;
    const y = player.y;
    const s = player.radius;
    const massRatio = clamp((player.mass || 0) / 100, 0, 1);
    const low = 1 - massRatio;
    const speed = Math.sqrt(player.vx * player.vx + player.vy * player.vy);
    const dir = speed > 1 ? Math.atan2(player.vy, player.vx) : (player.angle || 0);
    const lowColor = mix(theme.coral, theme.ember || "#d88768", low * 0.55);
    const coreColor = low > 0.55 ? lowColor : theme.star;
    const pulse = 1 + Math.sin(time * 2.1) * 0.02;
    const steer = player.steerVisual || 0;
    const alpha = player.alive ? 1 : clamp(player.winAlpha || 0, 0, 1);

    ctx.save();
    ctx.globalAlpha = alpha;

    drawTrail(ctx, player, 1.7);

    ctx.save();
    ctx.translate(x, y);

    // Soft corona.
    glow(ctx, low > 0.6 ? theme.coral : theme.portal, 0, 0, s * 5.5, 0.24);
    glow(ctx, theme.cream, 0, 0, s * 2.5, 0.28);

    // A fine, rotating halo of brushstrokes.
    ctx.rotate(time * (0.08 + speed * 0.0005));
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < 16; i++) {
      const a = i / 16 * TAU + Math.sin(i * 2.4) * 0.1;
      const inner = s * (1.25 + (i % 3) * 0.18);
      const outer = inner + s * (0.35 + (i % 5) * 0.12);
      ctx.strokeStyle = rgba(i % 4 === 0 ? theme.teal : i % 5 === 0 ? theme.gold : theme.cream, i % 3 === 0 ? 0.27 : 0.12);
      ctx.lineWidth = i % 4 === 0 ? 0.85 : 0.55;
      ctx.beginPath();
      ctx.moveTo(Math.cos(a) * inner, Math.sin(a) * inner);
      ctx.quadraticCurveTo(
        Math.cos(a + steer * 0.08) * (inner + outer) * 0.5,
        Math.sin(a + steer * 0.08) * (inner + outer) * 0.5,
        Math.cos(a + 0.08) * outer,
        Math.sin(a + 0.08) * outer
      );
      ctx.stroke();
    }

    // Curling corona.
    const rays = 11;
    for (let i = 0; i < rays; i++) {
      const a = i / rays * TAU + Math.sin(i * 1.7) * 0.16;
      const r0 = s * 0.72;
      const r1 = s * (1.75 + (i % 4) * 0.23);
      ctx.strokeStyle = rgba(i % 3 === 0 ? theme.gold : theme.cream, 0.09 + (i % 4) * 0.025);
      ctx.lineWidth = 0.65 + (i % 3) * 0.35;
      ctx.beginPath();
      ctx.moveTo(Math.cos(a) * r0, Math.sin(a) * r0);
      ctx.quadraticCurveTo(
        Math.cos(a + 0.24) * r1 * 0.66,
        Math.sin(a + 0.24) * r1 * 0.66,
        Math.cos(a + 0.16) * r1,
        Math.sin(a + 0.16) * r1
      );
      ctx.stroke();
    }

    // The star body.
    const bodyGlow = ctx.createRadialGradient(-s * 0.2, -s * 0.24, s * 0.1, 0, 0, s * pulse);
    bodyGlow.addColorStop(0, "rgba(255,254,247,1)");
    bodyGlow.addColorStop(0.28, rgba(theme.cream, 0.96));
    bodyGlow.addColorStop(0.63, rgba(coreColor, 0.7));
    bodyGlow.addColorStop(0.9, rgba(coreColor, 0.12));
    bodyGlow.addColorStop(1, rgba(coreColor, 0));
    ctx.fillStyle = bodyGlow;
    ctx.beginPath();
    ctx.arc(0, 0, s * pulse, 0, TAU);
    ctx.fill();

    ctx.fillStyle = "rgba(255,251,236,0.96)";
    ctx.beginPath();
    ctx.arc(-s * 0.12, -s * 0.16, s * 0.35, 0, TAU);
    ctx.fill();

    // A few bright, impasto flecks.
    if (massRatio > 0.15) {
      for (let i = 0; i < 4; i++) {
        const a = i * 2.4 + time * 0.4;
        gDab(ctx, Math.cos(a) * s * 0.75, Math.sin(a) * s * 0.7, a, s * 0.2, 0.6, rgba(theme.cream, 0.2), 0.5);
      }
    }

    // Wake lines opposite the turn, like the star is leaning into its path.
    if (Math.abs(steer) > 0.04) {
      ctx.globalAlpha = 0.35 * Math.abs(steer);
      ctx.strokeStyle = rgba(theme.teal, 0.4);
      ctx.lineWidth = 0.7;
      for (let i = 0; i < 3; i++) {
        const offset = (i + 1) * s * 0.32;
        ctx.beginPath();
        ctx.arc(0, 0, s * 1.8 + offset, -steer * 0.7 + i * 0.17, -steer * 0.7 + i * 0.17 + 0.8);
        ctx.stroke();
      }
      ctx.globalAlpha = 1;
    }

    // Star matter gauge, written around the star.
    const gaugeR = s * 2.02;
    const startA = -Math.PI / 2;
    ctx.globalAlpha = 0.28;
    ctx.strokeStyle = rgba(theme.cream, 0.5);
    ctx.lineWidth = 0.7;
    ctx.beginPath();
    ctx.arc(0, 0, gaugeR, 0, TAU);
    ctx.stroke();
    ctx.globalCompositeOperation = "lighter";
    ctx.globalAlpha = massRatio < 0.25 ? 0.55 + 0.35 * Math.sin(time * 6) : 0.7;
    ctx.strokeStyle = rgba(low > 0.6 ? theme.coral : theme.cream, 0.85);
    ctx.lineWidth = 1.2;
    ctx.beginPath();
    ctx.arc(0, 0, gaugeR, startA, startA + massRatio * TAU);
    ctx.stroke();
    const endpoint = startA + massRatio * TAU;
    glow(ctx, low > 0.6 ? theme.coral : theme.gold, Math.cos(endpoint) * gaugeR, Math.sin(endpoint) * gaugeR, s * 0.42, 0.65);
    ctx.globalAlpha = 1;

    // Forward constellation, a tiny compass of intent.
    if (speed > 12) {
      ctx.globalCompositeOperation = "lighter";
      for (let i = 1; i <= 3; i++) {
        const d = s * (1.9 + i * 0.54);
        ctx.fillStyle = rgba(theme.cream, 0.2 / i + 0.04);
        ctx.beginPath();
        ctx.arc(Math.cos(dir) * d, Math.sin(dir) * d, 0.8, 0, TAU);
        ctx.fill();
      }
    }

    ctx.restore();

    if (player.collisionPulse > 0) {
      const p = player.collisionPulse;
      ctx.globalCompositeOperation = "lighter";
      ctx.globalAlpha = p * 0.5;
      ctx.strokeStyle = rgba(theme.cream, 0.55);
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.arc(x, y, s * (1.3 + (1 - p) * 3.2), 0, TAU);
      ctx.stroke();
    }
    ctx.restore();
  }

  function blackHoleLabel(ctx, radius, theme, alpha) {
    ctx.save();
    ctx.translate(0, radius * 1.75);
    ctx.textAlign = "center";
    ctx.fillStyle = rgba(theme.cream, 0.24 * alpha);
    ctx.font = '7px "Cormorant Garamond","Palatino Linotype",serif';
    spacedText(ctx, "SAGITTARIUS  ∞", 0, 0, 2.2);
    ctx.fillStyle = rgba(theme.gold, 0.2 * alpha);
    ctx.font = '9px "Songti SC","SimSun",serif';
    spacedText(ctx, "奇点", 0, 13, 4);
    ctx.restore();
  }

  function drawBodyLabel(ctx, text, x, y, theme, alpha) {
    ctx.save();
    ctx.translate(x, y);
    ctx.textAlign = "center";
    ctx.fillStyle = rgba(theme.cream, 0.35 * alpha);
    ctx.font = '8px "Songti SC","SimSun",serif';
    spacedText(ctx, text, 0, 0, 3);
    ctx.restore();
  }

  window.StarArt = {
    TAU,
    THEMES,
    clamp,
    lerp,
    mix,
    rgba,
    lighten,
    mulberry32,
    hashSeed,
    buildBackdrop,
    drawBackdrop,
    blackHole,
    portal,
    portalLabel,
    boundary,
    drawBody,
    drawPlayer,
    drawTrail,
    blackHoleLabel,
    drawBodyLabel,
    glow
  };
})();
