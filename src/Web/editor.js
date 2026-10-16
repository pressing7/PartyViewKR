"use strict";
(() => {
  const normalize = text => text.replace(/\r\n?/g, "\n").trim();
  const sameJobs = (a, b) => a.length === b.length && [...a].sort((x, y) => x - y).every((id, i) => id === [...b].sort((x, y) => x - y)[i]);
  function draft(model) {
    return { values: { ...model.values }, password: "", itemLevelEnabled: model.values.itemLevel > 0,
      levelText: String(model.values.itemLevel || 1), jobs: Object.fromEntries(model.slots.map(s => [s.nativeIndex, [...s.jobs]])) };
  }
  function collect(model, input) {
    const changes = []; let error = "";
    for (const field of model.supported) {
      if (field === "job") continue;
      let value = input.values[field];
      if (field === "description") {
        value = normalize(value);
        if (/\p{Cs}|\ufffd|[\u0000-\u0009\u000b-\u001f\u007f-\u009f]/u.test(value)) error = "소개말에 입력할 수 없는 문자가 있습니다.";
        if (new TextEncoder().encode(value).length > 192) error = "소개말은 192바이트 이내로 입력해 주세요.";
        if (value === normalize(model.values.description)) continue;
      } else if (field === "private") {
        if (value === model.values.private && (!value || input.password === "")) continue;
        if (value && !/^[0-9]{4}$/.test(input.password)) error = "비공개 설정에는 숫자 네 자리 비밀번호가 필요합니다. (0000~9999)";
        changes.push(value ? { field, value, password: input.password } : { field, value }); continue;
      } else if (field === "itemLevel") {
        value = input.itemLevelEnabled ? Number(input.levelText) : 0;
        if (input.itemLevelEnabled && (!/^[0-9]{1,3}$/.test(input.levelText) || value < 1 || value > 999)) error = "평균 아이템 레벨은 1~999로 입력해 주세요.";
        if (value === model.values.itemLevel) continue;
      } else if (value === model.values[field]) continue;
      changes.push({ field, value });
    }
    if (model.supported.includes("job")) for (const slot of model.slots) {
      const jobs = input.jobs[slot.nativeIndex];
      if (sameJobs(jobs, slot.jobs)) continue;
      if (!jobs.length) error = `${slot.position}번 자리의 허용 직업을 하나 이상 선택해 주세요.`;
      changes.push({ field: "job", slot: slot.nativeIndex, jobs: [...jobs] });
    }
    return { changes, error, dirty: changes.length > 0 || !!error };
  }
  const logic = { normalize, sameJobs, draft, collect };
  if (typeof module !== "undefined") module.exports = logic;
  if (typeof document === "undefined") return;

  window.PvkrEditor = { ...logic, create({ byId, el, current, changed, submit }) {
    let model = null, input = null, session = "";
    const panel = byId("editor"), fields = byId("editor-fields"), apply = byId("apply");
    function result() { return model && input ? collect(model, input) : { changes: [], error: "", dirty: false }; }
    function clear() { model = input = null; session = ""; fields.replaceChildren(); panel.hidden = true; changed(); }
    function clearPassword() { if (input) input.password = ""; const pin = byId("edit-password"); if (pin) pin.value = ""; }
    function control(field, label) {
      const box = el("div", "edit-field"), caption = el("label", "", label);
      caption.htmlFor = "edit-" + field; box.append(caption); return { box, caption };
    }
    function unavailable(node, field) {
      node.disabled = !model.supported.includes(field);
      if (node.disabled) node.title = "현재 모집 정보에서 수정할 수 없는 항목입니다.";
    }
    function select(field, title, options) {
      const { box } = control(field, title), node = el("select"); node.id = "edit-" + field;
      for (const [value, label] of options) { const option = el("option", "", label); option.value = String(value); node.append(option); }
      node.value = String(input.values[field]); unavailable(node, field);
      node.addEventListener("change", () => { input.values[field] = Number(node.value); changed(); });
      box.append(node); return box;
    }
    function toggle(field, title, numeric = false) {
      const label = el("label", "edit-toggle"), node = el("input"); node.type = "checkbox"; node.id = "edit-" + field;
      node.checked = !!input.values[field]; unavailable(node, field);
      node.addEventListener("change", () => { input.values[field] = numeric ? Number(node.checked) : node.checked; changed(); });
      label.append(node, el("span", "", title)); return label;
    }
    function open() {
      const state = current(); if (!state?.editor) return;
      if (model) { panel.hidden = false; panel.scrollIntoView({ behavior: "smooth", block: "start" }); return; }
      load(); panel.scrollIntoView({ behavior: "smooth", block: "start" });
    }
    function load() {
      const state = current(); if (!state?.editor) return;
      model = structuredClone(state.editor); session = state.session; input = draft(model);
      fields.replaceChildren(); panel.hidden = false;
      const { box: description } = control("description", "모집 소개말"), area = el("textarea");
      area.id = "edit-description"; area.rows = 4; area.value = input.values.description; unavailable(area, "description");
      const count = el("small", "subtle"); count.id = "description-count";
      area.addEventListener("input", () => { input.values.description = area.value; changed(); });
      description.append(area, count); fields.append(description);
      const grid = el("div", "edit-grid");
      grid.append(select("objective", "모집 목적", [[0, "설정 안 함"], [1, "완료 목적"], [2, "연습"], [3, "반복 공략"]]),
        select("completion", "완료조건", [[1, "설정 안 함"], [2, "공략 완료"], [4, "미완료"]]),
        select("loot", "전리품 규칙", [[0, "일반"], [1, "선입찰 금지"], [2, "파티장 분배"]]));
      const itemBox = el("div", "edit-field"), itemLabel = el("label", "edit-toggle"), itemEnabled = el("input"), itemValue = el("input");
      itemEnabled.type = "checkbox"; itemEnabled.id = "edit-itemLevel-enabled"; itemEnabled.checked = input.itemLevelEnabled; unavailable(itemEnabled, "itemLevel");
      itemLabel.append(itemEnabled, el("span", "", "평균 아이템 레벨 설정"));
      itemValue.type = "number"; itemValue.id = "edit-itemLevel"; itemValue.min = "1"; itemValue.max = "999"; itemValue.step = "1";
      itemValue.inputMode = "numeric"; itemValue.value = input.levelText; itemValue.setAttribute("aria-label", "평균 아이템 레벨 (1~999)");
      itemEnabled.addEventListener("change", () => { input.itemLevelEnabled = itemEnabled.checked; changed(); });
      itemValue.addEventListener("input", () => { input.levelText = itemValue.value; changed(); }); itemBox.append(itemLabel, itemValue); grid.append(itemBox);
      fields.append(grid);
      const toggles = el("div", "edit-grid toggles");
      toggles.append(toggle("beginner", "초행 및 초보자 환영"), toggle("uniqueJobs", "잡 중복 없음"),
        toggle("unrestricted", "제한 해제", true), toggle("minimum_item_level", "최저 아이템 레벨로 참가", true), toggle("silence_echo", "초월하는 힘 무효화", true));
      fields.append(toggles);
      const privateBox = el("div", "private-edit"); privateBox.append(toggle("private", "비공개 설정"));
      const pinLabel = el("label", "", "비밀번호 (숫자 네 자리)"), pin = el("input"), pinHint = el("p", "subtle");
      pin.id = "edit-password"; pinLabel.htmlFor = pin.id; pin.type = "password"; pin.inputMode = "numeric";
      pin.maxLength = 4; pin.autocomplete = "new-password"; pin.placeholder = "0000~9999";
      pinHint.textContent = model.values.private ? "현재 비밀번호는 조회할 수 없습니다. 비워두면 기존 비밀번호를 유지합니다." : "비공개를 켜면 비밀번호를 반드시 입력해 주세요.";
      pin.addEventListener("input", () => { input.password = pin.value; changed(); });
      privateBox.append(pinLabel, pin, pinHint); fields.append(privateBox);
      byId("edit-private").addEventListener("change", () => { if (!input.values.private) clearPassword(); });
      const jobsBox = el("div", "edit-jobs"); jobsBox.append(el("h3", "", "빈자리 직업 제한"), el("p", "subtle", "자리를 눌러 허용할 직업을 선택하세요. 참가자가 있는 자리는 변경할 수 없습니다."));
      const slots = el("div", "edit-slots");
      for (const slot of model.slots) {
        const details = el("details", "edit-slot"), summary = el("summary"), list = el("div", "job-choices");
        details.dataset.nativeIndex = String(slot.nativeIndex); summary.id = "edit-slot-summary-" + slot.nativeIndex;
        const all = el("button", "secondary small", "모든 직업 허용"); all.type = "button";
        all.addEventListener("click", () => { input.jobs[slot.nativeIndex] = model.jobs.map(j => j.id); for (const node of list.querySelectorAll("input")) node.checked = true; changed(); });
        for (const job of model.jobs) {
          const label = el("label", "job-choice"), check = el("input"); check.type = "checkbox"; check.checked = slot.jobs.includes(job.id);
          check.dataset.jobId = String(job.id);
          check.addEventListener("change", () => {
            const ids = new Set(input.jobs[slot.nativeIndex]); if (check.checked) ids.add(job.id); else ids.delete(job.id);
            input.jobs[slot.nativeIndex] = model.jobs.filter(j => ids.has(j.id)).map(j => j.id); changed();
          });
          label.append(check, el("span", "", job.name)); list.append(label);
        }
        details.append(summary, all, list); slots.append(details);
      }
      jobsBox.append(slots); if (!model.slots.length) jobsBox.append(el("p", "subtle", "직업 제한을 수정할 수 있는 빈자리가 없습니다."));
      fields.append(jobsBox); changed();
    }
    function update(available, locked, reason) {
      const state = current(), change = result();
      const outdated = !!model && (session !== state?.session || model.revision !== state?.editor?.revision);
      byId("edit").disabled = !available || locked || !state?.editor?.supported.length;
      byId("reload-editor").disabled = !available || locked || !state?.editor;
      fields.disabled = locked || outdated;
      apply.disabled = !model || !available || locked || outdated || !!change.error || !change.changes.length;
      apply.textContent = `변경사항 적용 및 갱신${change.changes.length ? ` (${change.changes.length})` : ""}`;
      byId("edit-note").textContent = locked ? "요청을 처리하는 중입니다." : outdated ? "모집글 또는 참가자 정보가 바뀌었습니다. 조회 후 ‘현재 정보로 다시 편집’을 눌러 주세요." :
        reason || change.error || (change.changes.length ? `${change.changes.length}개 변경사항을 한 번에 적용합니다. 적용할 수 없는 설정은 건너뛰고 결과를 표시합니다.` : "변경할 항목을 선택하세요.");
      if (!model) return;
      byId("description-count").textContent = `${new TextEncoder().encode(normalize(input.values.description)).length} / 192바이트`;
      byId("edit-itemLevel").disabled = !input.itemLevelEnabled || !model.supported.includes("itemLevel");
      byId("edit-password").disabled = !input.values.private || !model.supported.includes("private");
      for (const slot of model.slots) {
        const ids = input.jobs[slot.nativeIndex], summary = byId("edit-slot-summary-" + slot.nativeIndex);
        summary.textContent = `${slot.position}번 · ${ids.length === model.jobs.length ? "누구나 가능" : `허용 직업 ${ids.length}개`}`;
        summary.title = model.jobs.filter(j => ids.includes(j.id)).map(j => j.name).join(" · ");
      }
    }
    byId("edit").addEventListener("click", open);
    byId("reload-editor").addEventListener("click", load);
    byId("edit-form").addEventListener("submit", event => {
      event.preventDefault(); if (apply.disabled || !model) return;
      const change = result(); if (!change.error && change.changes.length) submit(model.revision, change.changes);
    });
    return { clear, clearPassword, update, result };
  } };
})();
