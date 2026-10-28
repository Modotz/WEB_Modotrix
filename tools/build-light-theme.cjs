// Membuat wwwroot/css/theme-light.css dari wwwroot/css/site.css (tema gelap).
// Jalankan ulang setiap kali site.css berubah:   node tools/build-light-theme.cjs
//
// Cara kerja: setiap deklarasi warna di site.css yang "gelap" (latar/border) atau "terang" (teks)
// dibalik kecerahannya (hue dipertahankan) dan ditulis ulang di bawah html[data-theme="light"].
// Warna aksen (jenuh, kecerahan sedang) dibiarkan. Sidebar sengaja tetap gelap.
// Penyesuaian manual ada di wwwroot/css/theme-light-overrides.css.
const fs = require("fs");
const path = require("path");
const postcss = require("postcss");

const root = path.join(__dirname, "..");
const src = path.join(root, "wwwroot/css/site.css");
const out = path.join(root, "wwwroot/css/theme-light.css");

const PREFIX = 'html[data-theme="light"]';

// Selector milik sidebar → tidak diubah (sidebar tetap gelap).
const KEEP_DARK = /(^|[\s.>+~])(sidebar|brand|nav-link-main|nav-group|nav-sub|sidebar-menu)\b/;

const COLOR_PROPS = new Set([
  "color", "background", "background-color", "border", "border-color", "border-top", "border-bottom",
  "border-left", "border-right", "border-top-color", "border-bottom-color", "border-left-color", "border-right-color",
  "outline-color", "fill", "stroke", "caret-color", "--bs-table-bg", "--bs-table-color", "--bs-table-hover-bg",
  "--bs-table-striped-bg", "--bs-table-border-color", "box-shadow", "color-scheme",
]);

// ---------- warna ----------
function parseColor(str) {
  let m = str.match(/^#([0-9a-f]{3,8})$/i);
  if (m) {
    let h = m[1];
    if (h.length === 3 || h.length === 4) h = [...h].map(c => c + c).join("");
    const a = h.length === 8 ? parseInt(h.slice(6, 8), 16) / 255 : 1;
    return { r: parseInt(h.slice(0, 2), 16), g: parseInt(h.slice(2, 4), 16), b: parseInt(h.slice(4, 6), 16), a };
  }
  m = str.match(/^rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+)\s*)?\)$/i);
  if (m) return { r: +m[1], g: +m[2], b: +m[3], a: m[4] === undefined ? 1 : +m[4] };
  if (/^white$/i.test(str)) return { r: 255, g: 255, b: 255, a: 1 };
  if (/^black$/i.test(str)) return { r: 0, g: 0, b: 0, a: 1 };
  return null;
}

function toHsl({ r, g, b }) {
  r /= 255; g /= 255; b /= 255;
  const max = Math.max(r, g, b), min = Math.min(r, g, b);
  let h = 0, s = 0;
  const l = (max + min) / 2;
  if (max !== min) {
    const d = max - min;
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
    h = max === r ? (g - b) / d + (g < b ? 6 : 0) : max === g ? (b - r) / d + 2 : (r - g) / d + 4;
    h /= 6;
  }
  return { h, s, l };
}

function fromHsl({ h, s, l }) {
  if (s === 0) { const v = Math.round(l * 255); return { r: v, g: v, b: v }; }
  const hue = (p, q, t) => {
    if (t < 0) t += 1; if (t > 1) t -= 1;
    if (t < 1 / 6) return p + (q - p) * 6 * t;
    if (t < 1 / 2) return q;
    if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
    return p;
  };
  const q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
  return { r: Math.round(hue(p, q, h + 1 / 3) * 255), g: Math.round(hue(p, q, h) * 255), b: Math.round(hue(p, q, h - 1 / 3) * 255) };
}

function fmt(c, a) {
  const hex = "#" + [c.r, c.g, c.b].map(v => Math.max(0, Math.min(255, v)).toString(16).padStart(2, "0")).join("");
  return a === undefined || a >= 1 ? hex : `rgba(${c.r}, ${c.g}, ${c.b}, ${+a.toFixed(3)})`;
}

// Permukaan utama tema gelap → padanan terang yang dipilih manual (kartu putih, halaman abu muda).
const SURFACES = {
  "#0d111c": "#f3f5f9", // latar halaman
  "#0f1522": "#ffffff", // topbar
  "#101a2e": "#ffffff", // kartu
  "#0d1527": "#ffffff", // custom-card
  "#0f172a": "#f8fafc", // kepala tabel
  "#0f192c": "#ffffff", // input
  "#172338": "#f1f5f9",
  "#162032": "#f1f5f9", // hover baris
  "#172033": "#e9eef5", // garis tabel
  "#1e293b": "#e2e8f0",
};

/** Ubah satu warna sesuai perannya: 'bg' (latar/border) atau 'text'. null = biarkan. */
function convert(colorStr, role) {
  const c = parseColor(colorStr);
  if (!c) return null;
  const hsl = toHsl(c);

  // Overlay putih transparan (hover di tema gelap) → overlay gelap transparan.
  if (c.a < 1 && hsl.l > 0.9) return fmt({ r: 15, g: 23, b: 42 }, c.a);
  // Overlay hitam transparan (bayangan) → biarkan.
  if (c.a < 1 && hsl.l < 0.1) return null;

  const accent = hsl.s > 0.45 && hsl.l > 0.3 && hsl.l < 0.7;
  if (accent) return null;

  const key = fmt(c).toLowerCase();
  if ((role === "bg" || role === "border") && c.a >= 1 && SURFACES[key]) return SURFACES[key];

  if (role === "bg" && hsl.l < 0.4) {
    // Latar gelap → terang: makin gelap makin putih (kartu ~#fff, halaman ~#f3f5f9).
    const l = Math.min(0.99, 1 - hsl.l * 0.35);
    return fmt(fromHsl({ h: hsl.h, s: Math.min(hsl.s, 0.35), l }), c.a);
  }

  if (role === "text" && hsl.l > 0.55) {
    // Teks terang → gelap.
    const l = Math.max(0.1, (1 - hsl.l) * 0.85 + 0.08);
    return fmt(fromHsl({ h: hsl.h, s: Math.min(hsl.s, 0.4), l }), c.a);
  }

  if (role === "border" && hsl.l < 0.45) {
    const l = Math.min(0.93, 1 - hsl.l * 0.55);
    return fmt(fromHsl({ h: hsl.h, s: Math.min(hsl.s, 0.3), l }), c.a);
  }

  return null;
}

const COLOR_RE = /#[0-9a-f]{3,8}\b|rgba?\([^)]*\)|\bwhite\b|\bblack\b/gi;

function roleOf(prop) {
  if (prop === "color" || prop === "caret-color" || prop === "--bs-table-color" || prop === "fill" || prop === "stroke") return "text";
  if (prop.startsWith("border") || prop === "outline-color" || prop === "--bs-table-border-color") return "border";
  return "bg";
}

function convertValue(prop, value) {
  if (prop === "color-scheme") return value.includes("dark") ? "light" : null;
  if (prop === "box-shadow") return null; // bayangan tetap
  let changed = false;
  const role = roleOf(prop);
  const next = value.replace(COLOR_RE, (m) => {
    const r = convert(m, role);
    if (r) { changed = true; return r; }
    return m;
  });
  return changed ? next : null;
}

// ---------- proses CSS ----------
const css = fs.readFileSync(src, "utf8");
const input = postcss.parse(css);
const output = postcss.root();
let rules = 0, decls = 0;

function scopeSelector(sel) {
  sel = sel.trim();
  if (sel === ":root" || sel === "html") return PREFIX;
  if (sel.startsWith("html")) return PREFIX + sel.slice(4);
  return `${PREFIX} ${sel}`;
}

function processRule(rule, parent) {
  const selectors = rule.selectors.filter(s => !KEEP_DARK.test(s));
  if (selectors.length === 0) return;

  const newDecls = [];

  // Latar milik rule ini: bila berwarna aksen/terang, teksnya (biasanya putih) dibiarkan.
  let bgIsColorful = false;
  rule.walkDecls(/^background(-color)?$/i, (d) => {
    const m = d.value.match(COLOR_RE);
    if (m && m.some(x => { const c = parseColor(x); return c && c.a >= 1 && convert(x, "bg") === null; })) bgIsColorful = true;
    if (/gradient/i.test(d.value) && m && m.some(x => convert(x, "bg") === null)) bgIsColorful = true;
    // Variabel warna aksen (mis. var(--primary)) juga dianggap berwarna.
    if (/var\(--(primary|sidebar-active)/i.test(d.value)) bgIsColorful = true;
  });

  rule.walkDecls((d) => {
    const prop = d.prop.toLowerCase();
    // Variabel tema di :root
    const isVar = prop.startsWith("--") && !prop.startsWith("--bs-");
    if (!COLOR_PROPS.has(prop) && !isVar) return;
    if (prop.startsWith("--sidebar")) return;
    if (bgIsColorful && (prop === "color" || prop === "fill")) return;
    const role = isVar ? (/text|muted/.test(prop) ? "text" : /border/.test(prop) ? "border" : "bg") : null;
    const next = isVar
      ? d.value.replace(COLOR_RE, (m) => convert(m, role) ?? m)
      : convertValue(prop, d.value);
    if (next && next !== d.value) newDecls.push(postcss.decl({ prop: d.prop, value: next, important: d.important }));
  });

  if (newDecls.length === 0) return;
  const r = postcss.rule({ selectors: selectors.map(scopeSelector) });
  newDecls.forEach(d => r.append(d));
  parent.append(r);
  rules++; decls += newDecls.length;
}

input.each((node) => {
  if (node.type === "rule") processRule(node, output);
  else if (node.type === "atrule" && node.name === "media") {
    const at = postcss.atRule({ name: "media", params: node.params });
    node.each((child) => { if (child.type === "rule") processRule(child, at); });
    if (at.nodes && at.nodes.length) output.append(at);
  }
});

const header = `/* =====================================================================
   TEMA TERANG — DIBUAT OTOMATIS dari site.css oleh tools/build-light-theme.cjs.
   Jangan diedit manual; penyesuaian di theme-light-overrides.css.
   ===================================================================== */\n`;

fs.writeFileSync(out, header + output.toString() + "\n");
console.log(`theme-light.css: ${rules} rule, ${decls} deklarasi`);
