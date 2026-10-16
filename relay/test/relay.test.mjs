import { test } from "node:test";
import assert from "node:assert/strict";
import { Miniflare, convertV4MiniflareOptions } from "miniflare";
import { spawn } from "node:child_process";
import { createInterface } from "node:readline";
import { fileURLToPath } from "node:url";
import { readFileSync } from "node:fs";
import { digest } from "../src/protocol.mjs";

const secret = "A".repeat(64), browser = "B".repeat(64), room = "c".repeat(32);
const root = fileURLToPath(new URL("../", import.meta.url));
function options(extra = {}) {
  return convertV4MiniflareOptions({ modulesRoot: root + "src", modules: ["worker.mjs", "protocol.mjs"].map(name => ({
    type: "ESModule", path: root + "src/" + name, contents: readFileSync(root + "src/" + name, "utf8") })),
    compatibilityDate: "2026-10-16", durableObjects: { ROOM: { className: "PvkrRoom", useSQLite: true } },
    bindings: { PVKR_CONNECT_KEY: secret, LOCAL_TEST_ONLY: "true", ...extra },
    assets: { directory: root + "assets", binding: "ASSETS", run_worker_first: true, routerConfig: { has_user_worker: true } } });
}
const auth = (key = browser, id = room) => ({ Authorization: "Bearer " + key, "X-PVKR-Room": id });
async function host(mf, key = secret, id = room, browserKey = browser) {
  return mf.dispatchFetch("https://pvkr.test/relay/host", { headers: { Upgrade: "websocket", Authorization: "Bearer " + key,
    "X-PVKR-Room": id, "X-PVKR-Browser-Hash": await digest(browserKey) } });
}
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));

test("relay authentication, route boundaries, WebSocket forwarding, timeout and replacement", { timeout: 60000 }, async () => {
  const mf = new Miniflare(options());
  try {
    const initial = await mf.dispatchFetch("https://pvkr.test/api/state");
    assert.equal(initial.status, 401, await initial.text());
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/state", { headers: { ...auth(), Origin: "https://evil.test" } })).status, 403);
    assert.equal((await mf.dispatchFetch("http://pvkr.test/api/state", { headers: auth() })).status, 403);
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/callback", { headers: auth() })).status, 404);
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/state?key=x", { headers: auth() })).status, 403);
    const page = await mf.dispatchFetch("https://pvkr.test/");
    assert.equal(page.status, 200); assert.match(await page.text(), /connection-code/);
    assert.match(page.headers.get("Content-Security-Policy"), /frame-ancestors 'none'/);
    assert.equal((await host(mf, "D".repeat(64))).status, 401);
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/state", { headers: auth() })).status, 503);
    const connected = await host(mf); assert.equal(connected.status, 101);
    const ws = connected.webSocket; ws.accept(); const frames = []; let respond = true;
    ws.addEventListener("message", event => {
      const f = JSON.parse(event.data); frames.push(f);
      if (respond) ws.send(JSON.stringify({ v: 1, id: f.id, status: 200, body: { apiVersion: 2, echoedPath: f.path } }));
    });
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/state", { headers: auth("D".repeat(64)) })).status, 401);
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/state", { headers: auth(browser, "d".repeat(32)) })).status, 401);
    const state = await mf.dispatchFetch("https://pvkr.test/api/state", { headers: auth() });
    assert.equal((await state.json()).echoedPath, "/api/state"); assert.equal(frames.length, 1);
    assert.ok(frames[0].expiresAt > Date.now() && frames[0].expiresAt <= Date.now() + 5000);
    const post = { ...auth(), "Content-Type": "application/json", "X-PVKR-Request-Id": crypto.randomUUID(), "X-PVKR-Session": room };
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/apply", { method: "POST", headers: post, body: "x".repeat(65537) })).status, 400);
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/apply", { method: "POST", headers: { ...auth(), "Content-Type": "application/json" }, body: "{}" })).status, 400);
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/query", { method: "POST", headers: post, body: "{}" })).status, 400);
    assert.equal(frames.length, 1);
    respond = false;
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/apply", { method: "POST", headers: post, body: "{\"changes\":[]}" })).status, 504);
    assert.equal(frames.length, 2, "timed out command resent");
    ws.send(JSON.stringify({ v: 1, id: frames[1].id, status: 202, body: { id: post["X-PVKR-Request-Id"] } }));
    const replacement = await host(mf, secret, "d".repeat(32), "E".repeat(64)); replacement.webSocket.accept();
    assert.equal((await mf.dispatchFetch("https://pvkr.test/api/state", { headers: auth() })).status, 401);
    replacement.webSocket.close(1000, "test end");
  } finally { await mf.dispose(); }
});

test("real .NET relay executes existing batch once and retains results across a Worker restart", { timeout: 90000 }, async () => {
  const dotnet = process.env.PVKR_TEST_DOTNET;
  assert.ok(dotnet, "Set PVKR_TEST_DOTNET to the .NET 10 executable; build tests/RefreshTests.csproj first.");
  const mf = new Miniflare(options()); let child;
  try {
    const url = await mf.ready;
    child = spawn(dotnet, [fileURLToPath(new URL("../../tests/bin/Release/net10.0/RefreshTests.dll", import.meta.url)), "--relay-fixture", url.origin],
      { env: { ...process.env, PVKR_TEST_KEY: secret }, stdio: ["pipe", "pipe", "pipe"] });
    const lines = []; let childError = "";
    createInterface({ input: child.stdout }).on("line", line => lines.push(line));
    child.stderr.on("data", bytes => childError += bytes.toString());
    async function until(fn, timeout = 15000) {
      const end = Date.now() + timeout;
      while (Date.now() < end) { const value = await fn(); if (value) return value; if (child.exitCode !== null) throw new Error(childError || "fixture exited"); await wait(200); }
      child.stdin.write("status\n"); await wait(300); throw new Error("timed out: " + JSON.stringify(lines.slice(-2)) + childError);
    }
    const link = new URL(await until(() => lines.find(x => x.startsWith("http"))));
    const params = new URLSearchParams(link.hash.slice(1)), headers = auth(params.get("key"), params.get("room"));
    const request = (path, init = {}) => mf.dispatchFetch("https://pvkr.test" + path, { ...init, headers: { ...headers, ...init.headers } });
    let state = await until(async () => { const r = await request("/api/state"); return r.ok ? r.json() : null; });
    assert.equal(state.apiVersion, 2); assert.equal(state.editor.slots.length, 17); assert.equal(state.editor.slots.at(-1).nativeIndex, 23);
    const queryId = crypto.randomUUID();
    // Exercise an actual HTTP bodyless POST, as sent by mobile fetch(), too.
    const query = await fetch(url.origin + "/api/query", { method: "POST", headers: { ...headers,
      Origin: url.origin, "X-PVKR-Request-Id": queryId, "X-PVKR-Session": state.session } });
    assert.equal(query.status, 202, await query.text());
    await until(async () => { const r = await request("/api/queries/" + queryId); return r.ok && (await r.json()).state === "complete"; });
    state = await (await request("/api/state")).json();
    child.stdin.write("rule-disabled\n"); await wait(100);
    const id = crypto.randomUUID();
    const payload = JSON.stringify({ revision: state.editor.revision, changes: [
      { field: "description", value: "외부에서 수정한 소개말" }, { field: "private", value: true, password: "0000" },
      { field: "job", slot: 23, jobs: [19] }, { field: "unrestricted", value: 1 } ] });
    const mutation = { method: "POST", headers: { "Content-Type": "application/json", "X-PVKR-Request-Id": id, "X-PVKR-Session": state.session }, body: payload };
    const accepted = await request("/api/apply", mutation); assert.equal(accepted.status, 202);
    assert.ok(!(await accepted.text()).includes("0000"));
    assert.equal((await request("/api/apply", mutation)).status, 200);
    // Drop the network/Worker while an accepted job is running. No command is
    // persisted or replayed by the relay; the plugin owns completion/results.
    await mf.setOptions({ ...options({ TEST_RESTART: "1" }), port: Number(url.port) });
    assert.equal((await mf.ready).origin, url.origin);
    const result = await until(async () => { const r = await request("/api/operations/" + id); if (!r.ok) return null;
      const body = await r.json(); return ["queued", "running"].includes(body.state) ? null : body; }, 30000);
    assert.equal(result.state, "partial"); assert.equal(result.results.filter(x => x.applied === true).length, 3);
    assert.equal((await request("/api/apply", mutation)).status, 200);
    child.stdin.write("status\n");
    const native = JSON.parse(await until(() => lines.find(x => x.startsWith("{"))));
    assert.equal(native.StartCalls, 1); assert.equal(native.SubmitCalls, 1);
    const replacement = await host(mf, secret, "d".repeat(32), "E".repeat(64)); replacement.webSocket.accept();
    await until(async () => {
      child.stdin.write("status\n");
      const line = lines.findLast(x => x.startsWith("{"));
      const value = line && JSON.parse(line);
      return value && !value.Connected && value.Status.includes("다른 PC");
    });
    await wait(2200);
    assert.equal((await request("/api/state")).status, 401, "superseded PC reclaimed the room");
    replacement.webSocket.close(1000, "test end");
    child.stdin.write("quit\n");
    await new Promise(resolve => child.once("exit", resolve)); child = null;
    await untilOffline(request);
  } finally { child?.kill(); await mf.dispose(); }
});
async function untilOffline(request) {
  for (let i = 0; i < 20; i++) { if ((await request("/api/state")).status === 503) return; await wait(100); }
  assert.fail("shutdown retained an active host");
}
