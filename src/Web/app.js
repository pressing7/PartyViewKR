"use strict";
(() => {
  const byId = id => document.getElementById(id);
  const connection = byId("connection"), notice = byId("notice"), queryButton = byId("query"), refreshButton = byId("refresh");
  let key = "", state = null, online = false, posting = false, authFailed = false, actionMessage = "";
  let groupsSignature = "", settingsSignature = "", session = "";
  const storageKey = "pvkr.connection";
  const operationKey = "pvkr.operation";
  let pendingWrite = null, operationSignature = "";
  try { const saved = JSON.parse(sessionStorage.getItem(operationKey) || "null");
    if (saved && /^[0-9a-f-]{36}$/.test(saved.id) && typeof saved.session === "string" && Number.isFinite(saved.sentAt)) pendingWrite = saved;
  } catch { /* Never store form values or passwords. */ }
  try {
    const fragmentKey = new URLSearchParams(location.hash.slice(1)).get("key");
    history.replaceState(null, "", location.pathname);
    key = fragmentKey || "";
    if (!key) { try { key = sessionStorage.getItem(storageKey) || ""; } catch { /* Use the link's key in memory when storage is unavailable. */ } }
    if (!/^[A-F0-9]{64}$/.test(key)) key = "";
    if (key) { try { sessionStorage.setItem(storageKey, key); } catch { /* Refresh then requires a fresh connection link. */ } }
  } catch { key = ""; }

  function text(id, value) { const el = byId(id); if (el.textContent !== value) el.textContent = value; }
  function el(tag, className, value) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (value !== undefined) node.textContent = value;
    return node;
  }
  const editor = window.PvkrEditor.create({ byId, el, current: () => state, changed: renderStatus, submit: applyChanges });
  const active = operation => ["queued", "running"].includes(operation?.state);
  function savePending(value) {
    pendingWrite = value;
    try { if (value) sessionStorage.setItem(operationKey, JSON.stringify(value)); else sessionStorage.removeItem(operationKey); } catch { /* Keep the request ID in memory. */ }
  }
  function badge(value, tone) { connection.textContent = value; connection.dataset.tone = tone; }
  function stale() { return !state || !Number.isFinite(Date.parse(state.updatedAt)) || Date.now() - Date.parse(state.updatedAt) > 5000; }
  function time(value) {
    const date = new Date(value);
    return value && Number.isFinite(date.getTime()) ? date.toLocaleTimeString("ko-KR", { hour12: false }) : "확인 불가";
  }
  function renderStatus() {
    const old = stale(), busy = posting || active(state?.query) || active(state?.operation) || !!pendingWrite;
    queryButton.disabled = !key || !online || old || busy || !state?.canQuery || authFailed;
    const available = !!key && online && !old && !authFailed && !!state?.canRefresh;
    refreshButton.disabled = !available || busy || editor.result().dirty;
    refreshButton.title = editor.result().dirty ? "편집 중인 변경사항은 수정 화면의 적용 버튼으로 갱신해 주세요." : state?.refreshReason || "현재 모집 조건으로 한 번 갱신합니다.";
    text("refresh-reason", state?.refreshReason || "");
    editor.update(available, busy, !online ? "연결을 기다리는 중입니다." : old ? "게임 응답을 기다리는 중입니다." : state?.refreshReason || "");
    if (!key || authFailed) badge("연결 필요", "warning");
    else if (!online) badge("연결 끊김", "warning");
    else if (old) badge("게임 응답 대기", "warning");
    else if (!state?.loggedIn) badge("로그인 대기", "muted");
    else badge("PVKR 연결됨", "good");
    let message = "조회 버튼을 누르면 내 모집글을 다시 읽습니다.";
    if (!key || authFailed) message = "게임의 /pvkr → 브라우저 조회에서 접속 링크를 복사해 다시 열어 주세요.";
    else if (!online) message = "PVKR에 연결할 수 없습니다. 게임에서 브라우저 조회가 켜져 있는지 확인해 주세요. 마지막으로 받은 정보는 아래에 남아 있습니다.";
    else if (old) message = "게임의 응답을 기다리는 중입니다. 표시된 내용은 마지막 조회 결과입니다.";
    else if (posting) message = "게임에 요청을 전달하는 중입니다.";
    else if (pendingWrite) message = "갱신 요청의 처리 결과를 확인하는 중입니다. 같은 요청은 다시 보내지 않습니다.";
    else if (actionMessage) message = actionMessage;
    else if (state?.reason) message = state.reason;
    else if (state?.query?.message) message = state.query.message;
    text("notice", message);
    notice.dataset.tone = !online || old || authFailed || !!actionMessage || state?.query?.state === "failed" ? "warning" : "muted";
    if (state?.listing) {
      const live = online && !old && state.listing.live;
      text("freshness", live ? "게임에서 확인 중" : "마지막 조회 결과");
      byId("freshness").dataset.tone = live ? "good" : "muted";
      text("remaining-label", live ? "게임에 표시된 남은 시간" : "확인한 남은 시간");
    }
  }
  function render(view) {
    if (view.apiVersion !== 2) throw new Error("PVKR와 브라우저의 연결 버전이 다릅니다. 새 접속 링크로 열어 주세요.");
    if (session !== view.session) {
      session = view.session; groupsSignature = ""; settingsSignature = ""; actionMessage = ""; editor.clear();
      if (pendingWrite && pendingWrite.session !== session) savePending(null);
    }
    state = view;
    acceptOperation(view.operation);
    renderOperation();
    text("version", view.version);
    const listing = view.listing;
    byId("listing").hidden = !listing;
    byId("empty").hidden = !!listing;
    if (!listing) {
      byId("groups").replaceChildren(); byId("settings").replaceChildren();
      groupsSignature = ""; settingsSignature = "";
      text("empty-title", view.loggedIn ? "내 모집글을 불러오세요" : "게임 로그인 대기 중");
      text("empty-text", view.loggedIn ? "내 모집글 조회를 눌러 주세요. 본인 모집글로 확인된 정보가 표시됩니다." : "게임에 로그인하면 이곳에서 내 모집글을 조회할 수 있습니다.");
    } else {
      text("duty", listing.duty || "모집 대상 확인 불가");
      text("leader", listing.leader || "확인 불가");
      text("members", listing.total === null ? "확인 불가" : `${listing.filled} / ${listing.total}명`);
      text("remaining", listing.remaining || "확인 불가");
      text("read-at", time(listing.readAt));
      text("description", listing.description || "등록된 소개말이 없습니다.");
      byId("description").classList.toggle("empty-text", !listing.description);
      const settingsKey = JSON.stringify(listing.settings);
      if (settingsSignature !== settingsKey) {
        const nodes = listing.settings.map(item => {
          const row = el("div", "setting"); row.append(el("dt", "", item.label), el("dd", "", item.value)); return row;
        });
        byId("settings").replaceChildren(...nodes); settingsSignature = settingsKey;
      }
      const groupsKey = JSON.stringify(listing.groups);
      if (groupsSignature !== groupsKey) {
        const open = new Set(Array.from(byId("groups").querySelectorAll("details[open]")).map(n => n.dataset.slot));
        const groups = listing.groups.map(group => {
          const section = el("div", "group"), head = el("div", "group-head"), slots = el("ol", "slots");
          head.append(el("h3", "", group.name), el("span", "", `${group.slots.filter(s => s.hasMember).length} / ${group.slots.length}명`));
          for (const slot of group.slots) {
            const row = el("li", "slot"), top = el("div", "slot-top");
            top.append(el("span", "position", slot.position));
            if (slot.hasMember) top.append(el("span", "job filled", slot.job || "직업 확인 불가"));
            else if (slot.anyone) top.append(el("span", "job", "누구나 가능"));
            else {
              const details = el("details", "allowed"); details.dataset.slot = String(slot.nativeIndex); details.open = open.has(String(slot.nativeIndex));
              const label = slot.rulesComplete ? `직업 제한 · ${slot.allowedJobs.length}개` : "허용 직업 확인 필요";
              const summary = el("summary", "", label); summary.title = slot.allowedJobs.join(" · ") || "아직 허용 직업을 읽지 못했습니다.";
              const jobs = el("div", "job-list");
              for (const name of slot.allowedJobs) jobs.append(el("span", "job-chip", name));
              if (!slot.allowedJobs.length) jobs.append(el("span", "subtle", "허용 직업을 확인하지 못했습니다."));
              details.append(summary, jobs); top.append(details);
            }
            row.append(top); slots.append(row);
          }
          section.append(head, slots); return section;
        });
        byId("groups").replaceChildren(...groups); byId("groups").dataset.count = String(groups.length); groupsSignature = groupsKey;
      }
      const slotNote = !listing.slotsReadable ? "파티 구성을 아직 읽지 못했습니다. 다시 조회해 주세요." : !listing.countsAgree ? "모집 인원과 읽은 자리 정보가 일치하지 않습니다. 다시 조회해 주세요." : "";
      text("slot-note", slotNote); byId("slot-note").hidden = !slotNote;
    }
    renderStatus();
  }

  async function api(path, options = {}) {
    const response = await fetch(path, { ...options, cache: "no-store", credentials: "omit", redirect: "error",
      signal: AbortSignal.timeout(3500), headers: { "Authorization": "Bearer " + key, ...options.headers } });
    if (response.status === 401) {
      authFailed = true; key = ""; state = null; editor.clear(); savePending(null);
      try { sessionStorage.removeItem(storageKey); } catch { /* Browser storage may be disabled. */ }
      byId("listing").hidden = true; byId("empty").hidden = false;
      text("empty-title", "새 접속 링크가 필요합니다"); text("empty-text", "PVKR에서 접속 링크를 다시 복사해 주세요.");
      byId("groups").replaceChildren(); byId("settings").replaceChildren();
      throw new Error("접속 링크를 다시 열어 주세요.");
    }
    const body = await response.json();
    if (!response.ok) { const error = new Error(body.error || "요청을 처리하지 못했습니다."); error.status = response.status; throw error; }
    return body;
  }
  function acceptOperation(operation) {
    if (!operation || !pendingWrite || operation.id !== pendingWrite.id) return;
    editor.clear();
    if (!active(operation)) savePending(null);
  }
  function renderOperation() {
    const operation = state?.operation, signature = JSON.stringify(operation);
    if (operationSignature === signature) return;
    operationSignature = signature; byId("operation").hidden = !operation;
    if (!operation) { byId("operation-results").replaceChildren(); return; }
    const labels = { queued: "대기 중", running: "진행 중", complete: "완료", partial: "일부 미반영", unconfirmed: "결과 확인 필요", failed: "중단", unknown: "결과 확인 필요" };
    text("operation-state", labels[operation.state] || "확인 중");
    byId("operation-state").dataset.tone = operation.state === "complete" ? "good" : active(operation) ? "muted" : "warning";
    text("operation-message", operation.message);
    byId("operation-results").replaceChildren(...operation.results.map(result => {
      const row = el("li", "", result.message); row.dataset.tone = result.applied === true ? "good" : result.applied === false ? "warning" : "muted"; return row;
    }));
  }
  async function applyChanges(revision, changes) {
    if (posting || pendingWrite || !state?.canRefresh || !online || stale() || active(state?.operation)) return;
    const request = { id: crypto.randomUUID(), session: state.session, sentAt: Date.now() };
    savePending(request); posting = true; actionMessage = ""; renderStatus();
    try {
      const operation = await api("/api/apply", { method: "POST", headers: { "Content-Type": "application/json",
        "X-PVKR-Request-Id": request.id, "X-PVKR-Session": request.session }, body: JSON.stringify({ revision, changes }) });
      if (state?.session === request.session) { state.operation = operation; acceptOperation(operation); renderOperation(); }
    } catch (error) {
      if (error.status) savePending(null);
      actionMessage = error.status ? error.message : "응답이 끊겨 요청 결과를 확인하고 있습니다. 갱신을 다시 전송하지 않습니다.";
    } finally { editor.clearPassword(); posting = false; renderStatus(); }
  }
  refreshButton.addEventListener("click", () => { if (!refreshButton.disabled && state?.editor) applyChanges(state.editor.revision, []); });
  queryButton.addEventListener("click", async () => {
    if (queryButton.disabled || !state) return;
    posting = true; actionMessage = ""; renderStatus();
    try {
      const result = await api("/api/query", { method: "POST", headers: {
        "X-PVKR-Request-Id": crypto.randomUUID(), "X-PVKR-Session": state.session
      } });
      if (state) state.query = result;
    } catch (error) { actionMessage = error instanceof Error ? error.message : "조회 요청을 확인하지 못했습니다."; }
    finally { posting = false; renderStatus(); }
  });
  async function poll() {
    if (!key || authFailed) { renderStatus(); return; }
    try {
      const view = await api("/api/state"); online = true; render(view);
      if (pendingWrite && !posting) {
        const request = pendingWrite;
        try {
          const operation = await api("/api/operations/" + request.id);
          if (state?.session === request.session) { state.operation = operation; actionMessage = ""; acceptOperation(operation); renderOperation(); }
        } catch (error) {
          if (error.status === 409 || error.status === 404 && Date.now() - request.sentAt > 10000) {
            savePending(null); actionMessage = "요청 결과를 확인하지 못했습니다. 내 모집글을 다시 조회한 뒤 확인해 주세요.";
          }
        }
      }
      renderStatus();
    }
    catch { online = false; renderStatus(); }
    if (key && !authFailed) setTimeout(poll, document.hidden ? 4000 : 1000);
  }
  poll();
})();
