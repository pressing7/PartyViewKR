export const TOKEN = /^[A-F0-9]{64}$/;
export const ROOM = /^[a-f0-9]{32}$/;
export const UUID = /^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i;
export const MAX_BODY = 65536;
export const MAX_FRAME = 524288;
export const headers = {
  "Cache-Control": "no-store", "Referrer-Policy": "no-referrer", "X-Content-Type-Options": "nosniff",
  "Content-Security-Policy": "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'",
  "Permissions-Policy": "camera=(), microphone=(), geolocation=()"
};
export function json(status, body) {
  return new Response(JSON.stringify(body), { status, headers: { ...headers, "Content-Type": "application/json; charset=utf-8" } });
}
export const error = (status, message) => json(status, { error: message });
export function sameOrigin(request) {
  const origin = request.headers.get("Origin"), site = request.headers.get("Sec-Fetch-Site");
  return (!origin || origin === new URL(request.url).origin) && (!site || ["same-origin", "none"].includes(site));
}
export function token(request) { return /^Bearer ([A-F0-9]{64})$/.exec(request.headers.get("Authorization") || "")?.[1] || ""; }
export async function digest(value) {
  const bytes = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(bytes), b => b.toString(16).padStart(2, "0")).join("").toUpperCase();
}
export function equal(a, b) {
  if (a.length !== b.length) return false;
  let different = 0; for (let i = 0; i < a.length; i++) different |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return different === 0;
}
export function route(method, path) {
  if (method === "GET") return path === "/api/state" ||
    path.startsWith("/api/queries/") && UUID.test(path.slice(13)) ||
    path.startsWith("/api/operations/") && UUID.test(path.slice(16));
  return method === "POST" && ["/api/query", "/api/apply"].includes(path);
}
export async function requireEmptyBody(request) {
  const length = request.headers.get("Content-Length");
  if (length && (!/^\d+$/.test(length) || Number(length) !== 0)) throw new Error("body");
  const reader = request.body?.getReader(); if (!reader) return;
  const deadline = Date.now() + 3000;
  try {
    while (true) {
      let timer;
      const { value, done } = await Promise.race([reader.read(), new Promise((_, reject) => {
        timer = setTimeout(() => reject(new Error("timeout")), Math.max(0, deadline - Date.now()));
      })]).finally(() => clearTimeout(timer));
      if (done) return;
      if (value.byteLength) { value.fill(0); throw new Error("body"); }
    }
  } finally { await reader.cancel().catch(() => {}); }
}
export async function readBody(request) {
  const length = request.headers.get("Content-Length");
  if (length && (!/^\d+$/.test(length) || Number(length) > MAX_BODY)) throw new Error("size");
  if (!/^application\/json(?:\s*;\s*charset=utf-8)?$/i.test(request.headers.get("Content-Type") || "") ||
      request.headers.has("Content-Encoding")) throw new Error("type");
  const reader = request.body?.getReader(); if (!reader) throw new Error("body");
  const chunks = []; let size = 0;
  const deadline = Date.now() + 3000;
  try {
    while (true) {
      let timer;
      const { value, done } = await Promise.race([reader.read(), new Promise((_, reject) => {
        timer = setTimeout(() => reject(new Error("timeout")), Math.max(0, deadline - Date.now()));
      })]).finally(() => clearTimeout(timer));
      if (done) break;
      size += value.byteLength; if (size > MAX_BODY) throw new Error("size"); chunks.push(value);
    }
    if (!size) throw new Error("body");
    const body = new Uint8Array(size); let offset = 0;
    for (const chunk of chunks) { body.set(chunk, offset); offset += chunk.byteLength; }
    try { return new TextDecoder("utf-8", { fatal: true }).decode(body); }
    finally { body.fill(0); }
  } finally { for (const chunk of chunks) chunk.fill(0); await reader.cancel().catch(() => {}); }
}
