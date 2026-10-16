const test = require("node:test");
const assert = require("node:assert/strict");
const edits = require("../src/Web/editor.js");

const model = () => ({ revision: "one", supported: ["description", "objective", "beginner", "uniqueJobs", "private", "completion", "itemLevel", "unrestricted", "minimum_item_level", "silence_echo", "loot", "job"],
  values: { description: "기존 소개말", objective: 0, beginner: false, uniqueJobs: false, private: false, completion: 1, itemLevel: 0, unrestricted: 0, minimum_item_level: 0, silence_echo: 0, loot: 0 },
  jobs: [{ id: 19, name: "나이트" }, { id: 24, name: "백마도사" }, { id: 25, name: "흑마도사" }],
  slots: [{ nativeIndex: 8, position: "그림자팀 1", jobs: [19, 24, 25] }, { nativeIndex: 23, position: "관전 8", jobs: [19, 25] }]
});
test("a fresh draft sends nothing and does not mutate its source", () => {
  const source = model(), draft = edits.draft(source); assert.equal(edits.collect(source, draft).dirty, false);
  draft.jobs[23].pop(); draft.values.description = "새 소개말";
  assert.deepEqual(source.slots[1].jobs, [19, 25]); assert.equal(source.values.description, "기존 소개말");
});
test("batch contains only changed fields with physical friendly-match positions", () => {
  const source = model(), draft = edits.draft(source);
  draft.values.description = "  새 소개말\r\n두 번째 줄  "; draft.values.objective = 2; draft.jobs[23] = [19];
  assert.deepEqual(edits.collect(source, draft), { error: "", dirty: true, changes: [
    { field: "description", value: "새 소개말\n두 번째 줄" }, { field: "objective", value: 2 }, { field: "job", slot: 23, jobs: [19] }
  ] });
});
test("job ordering alone is not a change; no jobs is invalid", () => {
  const source = model(), draft = edits.draft(source); draft.jobs[23] = [25, 19];
  assert.equal(edits.collect(source, draft).dirty, false); draft.jobs[23] = [];
  assert.match(edits.collect(source, draft).error, /관전 8/);
});
test("0000 and leading zeros are retained and PIN is only sent when explicitly changed", () => {
  const source = model(), draft = edits.draft(source); draft.values.private = true;
  for (const pin of ["0000", "0001", "9999"]) { draft.password = pin; const result = edits.collect(source, draft);
    assert.equal(result.error, ""); assert.deepEqual(result.changes, [{ field: "private", value: true, password: pin }]); }
  source.values.private = true; draft.password = "";
  assert.deepEqual(edits.collect(source, draft).changes, []);
  draft.values.private = false; draft.password = "1234";
  assert.deepEqual(edits.collect(source, draft).changes, [{ field: "private", value: false }]);
});
test("enabling private requires four ASCII digits", () => {
  const source = model(), draft = edits.draft(source); draft.values.private = true;
  for (const pin of ["", "000", "12345", "１２３４", " 123", "1e03"]) {
    draft.password = pin; assert.ok(edits.collect(source, draft).error, `accepted ${pin}`);
  }
});
test("item-level accepts 1 and 999, rejects empty/noninteger/out-of-range, off sends zero", () => {
  const source = model(), draft = edits.draft(source); draft.itemLevelEnabled = true;
  for (const value of ["1", "555", "999"]) { draft.levelText = value; const result = edits.collect(source, draft);
    assert.equal(result.error, ""); assert.deepEqual(result.changes, [{ field: "itemLevel", value: Number(value) }]); }
  for (const value of ["", "0", "1000", "-1", "1.5", "1e2", "１２"]) { draft.levelText = value; assert.ok(edits.collect(source, draft).error); }
  source.values.itemLevel = 555; draft.itemLevelEnabled = false;
  assert.deepEqual(edits.collect(source, draft).changes, [{ field: "itemLevel", value: 0 }]);
});
test("Korean description counts UTF-8 bytes and empty description remains an explicit change", () => {
  const source = model(), draft = edits.draft(source);
  draft.values.description = "한".repeat(64); assert.equal(edits.collect(source, draft).error, "");
  draft.values.description += "a"; assert.ok(edits.collect(source, draft).error);
  for (const text of ["x\u0000y", "\ufffd", "\ud800", "x\ty"]) { draft.values.description = text; assert.ok(edits.collect(source, draft).error); }
  draft.values.description = "  "; assert.deepEqual(edits.collect(source, draft).changes, [{ field: "description", value: "" }]);
});
test("unsupported fields are omitted, boolean rules and enum values keep correct types", () => {
  const source = model(), draft = edits.draft(source); source.supported = ["beginner", "unrestricted", "completion", "loot"];
  Object.assign(draft.values, { description: "not submitted", beginner: true, unrestricted: 1, completion: 4, loot: 2 });
  assert.deepEqual(edits.collect(source, draft).changes, [ { field: "beginner", value: true }, { field: "unrestricted", value: 1 },
    { field: "completion", value: 4 }, { field: "loot", value: 2 } ]);
});
