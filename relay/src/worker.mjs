import { DurableObject } from "cloudflare:workers";
import { TOKEN, ROOM, UUID, MAX_FRAME, headers, json, error, sameOrigin, token, digest, equal, route, readBody, requireEmptyBody } from "./protocol.mjs";

const assets = new Set(["/", "/index.html", "/app.js", "/editor.js", "/connection.js", "/style.css"]);
export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (url.protocol !== "https:" && !(env.LOCAL_TEST_ONLY === "true" && ["127.0.0.1", "localhost"].includes(url.hostname)))
      return error(403, "HTTPS 주소로 접속해 주세요.");
    if (url.search) return error(403, "허용되지 않은 요청입니다.");
    // The public connection page must load when opened from an external link.
    // Origin checks still apply to all API and host connection requests below.
    if (request.method === "GET" && assets.has(url.pathname)) {
      const response = await env.ASSETS.fetch(request);
      const secured = new Response(response.body, response);
      for (const [key, value] of Object.entries(headers)) secured.headers.set(key, value);
      return secured;
    }
    if (!sameOrigin(request)) return error(403, "허용되지 않은 요청입니다.");
    if (url.pathname === "/relay/host") {
      if (request.method !== "GET" || request.headers.get("Upgrade")?.toLowerCase() !== "websocket")
        return error(400, "지원하지 않는 연결입니다.");
      if (!TOKEN.test(env.PVKR_CONNECT_KEY || "")) return error(503, "서버 연결 키를 먼저 설정해 주세요.");
      const supplied = token(request);
      if (!supplied || !equal(await digest(supplied), await digest(env.PVKR_CONNECT_KEY))) return error(401, "연결 키를 확인해 주세요.");
      if (!ROOM.test(request.headers.get("X-PVKR-Room") || "") || !TOKEN.test(request.headers.get("X-PVKR-Browser-Hash") || ""))
        return error(400, "연결 정보가 올바르지 않습니다.");
      return env.ROOM.getByName("owner").fetch(request);
    }
    if (!route(request.method, url.pathname)) return error(404, "지원하지 않는 주소입니다.");
    if (!token(request) || !ROOM.test(request.headers.get("X-PVKR-Room") || "")) return error(401, "PVKR의 외부 접속 링크로 연결해 주세요.");
    return env.ROOM.getByName("owner").fetch(request);
  }
};

// One personal PC per deployed Worker. Attachments contain authentication hashes
// only. Listings, passwords and commands are never written to DO storage/logs.
export class PvkrRoom extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env);
    this.pending = new Map(); this.windowStart = 0; this.requests = 0;
    this.ctx.setWebSocketAutoResponse(new WebSocketRequestResponsePair("ping", "pong"));
  }
  host() { return this.ctx.getWebSockets("host").find(ws => ws.readyState === WebSocket.OPEN); }
  failPending(socket) {
    for (const [id, item] of this.pending) if (item.socket === socket) {
      clearTimeout(item.timer); this.pending.delete(id);
      item.resolve(error(504, "PC 연결이 끊겼습니다. 접수 여부와 결과를 다시 확인합니다."));
    }
  }
  async fetch(request) {
    const path = new URL(request.url).pathname;
    if (path === "/relay/host") {
      for (const old of this.ctx.getWebSockets("host")) {
        this.failPending(old); old.close(4001, "connection replaced");
      }
      const [client, server] = Object.values(new WebSocketPair());
      this.ctx.acceptWebSocket(server, ["host"]);
      server.serializeAttachment({ room: request.headers.get("X-PVKR-Room"), hash: request.headers.get("X-PVKR-Browser-Hash") });
      return new Response(null, { status: 101, webSocket: client });
    }
    const socket = this.host();
    if (!socket) return error(503, "게임 PC가 연결되어 있지 않습니다. PVKR의 외부 연결을 켜 주세요.");
    const attached = socket.deserializeAttachment();
    if (request.headers.get("X-PVKR-Room") !== attached?.room || !equal(await digest(token(request)), attached?.hash || ""))
      return error(401, "연결 정보가 만료됐습니다. PVKR에서 새 링크를 복사해 주세요.");
    // Awaiting a digest/body must not let a request cross into another connection.
    if (socket !== this.host()) return error(503, "PC가 다시 연결 중입니다.");
    const now = Date.now();
    if (now - this.windowStart >= 10000) { this.windowStart = now; this.requests = 0; }
    if (++this.requests > 100 || this.pending.size >= 8) return error(429, "요청이 많습니다. 잠시 후 다시 확인해 주세요.");
    let body = "";
    const requestId = request.headers.get("X-PVKR-Request-Id") || "", session = request.headers.get("X-PVKR-Session") || "";
    if (request.method === "POST") {
      if (!UUID.test(requestId) || !ROOM.test(session)) return error(400, "요청 번호 또는 게임 접속 정보가 올바르지 않습니다.");
      if (path === "/api/apply") {
        try { body = await readBody(request); } catch { return error(400, "수정 요청의 형식이나 크기를 확인해 주세요."); }
      } else {
        // HTTP adapters may represent an empty POST as a non-null stream.
        try { await requireEmptyBody(request); } catch { return error(400, "조회 요청에 본문을 넣을 수 없습니다."); }
      }
    }
    if (socket !== this.host() || this.pending.size >= 8) return error(503, "PC 연결 상태가 바뀌었습니다.");
    const id = crypto.randomUUID();
    const frame = JSON.stringify({ v: 1, id, method: request.method, path, requestId, session, body, expiresAt: Date.now() + 5000 });
    // No command queue or automatic resend. A lost response is resolved using
    // the original operation ID kept by the plugin, including after reconnect.
    return new Promise(resolve => {
      const timer = setTimeout(() => {
        this.pending.delete(id); resolve(error(504, "PC 응답이 지연되고 있습니다. 요청 결과를 확인합니다."));
      }, 2500);
      this.pending.set(id, { socket, timer, resolve });
      try { socket.send(frame); }
      catch { this.failPending(socket); }
    });
  }
  webSocketMessage(socket, message) {
    if (typeof message !== "string" || new TextEncoder().encode(message).byteLength > MAX_FRAME) {
      socket.close(1009, "frame too large"); this.failPending(socket); return;
    }
    try {
      const value = JSON.parse(message), item = this.pending.get(value.id);
      if (!item || item.socket !== socket) return;
      if (value.v !== 1 || !Number.isInteger(value.status) || value.status < 200 || value.status > 599 || value.body === undefined)
        throw new Error("invalid reply");
      clearTimeout(item.timer); this.pending.delete(value.id); item.resolve(json(value.status, value.body));
    } catch { socket.close(1008, "invalid reply"); this.failPending(socket); }
  }
  webSocketClose(socket, code) { this.failPending(socket); socket.close(code === 1006 ? 1011 : code, "closed"); }
  webSocketError(socket) { this.failPending(socket); socket.close(1011, "connection error"); }
}
