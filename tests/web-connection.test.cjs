const { test } = require('node:test');
const assert = require('node:assert/strict');
const connection = require('../src/Web/connection.js');
const key = 'A'.repeat(64), room = 'b'.repeat(32), origin = 'https://pvkr.test';
test('connection accepts same-origin links and opaque room codes, not foreign URLs', () => {
  for (const value of [`${origin}/#room=${room}&key=${key}`, `${room}.${key}`])
    assert.deepEqual(connection.parse(value, origin), { room, key });
  for (const value of [`https://evil.test/#room=${room}&key=${key}`, `${origin}/path#key=${key}`, `${origin}/?key=${key}`, 'short', `${room}.1234`])
    assert.equal(connection.parse(value, origin), null);
});
test('fresh connection link clears previous room and pending operation', () => {
  const saved = new Map([['pvkr.room', room], ['pvkr.operation', 'old']]);
  const storage = { getItem: k => saved.get(k), setItem: (k, v) => saved.set(k, v), removeItem: k => saved.delete(k) };
  let url;
  assert.deepEqual(connection.read({ origin, pathname: '/', hash: '#key=' + key }, { replaceState: (_, __, path) => url = path }, storage), { key, room: '' });
  assert.equal(saved.get('pvkr.room'), ''); assert.equal(saved.has('pvkr.operation'), false); assert.equal(url, '/');
});
test('valid fragment works even when browser storage is blocked', () => {
  assert.deepEqual(connection.read({ origin, pathname: '/', hash: `#key=${key}&room=${room}` }, { replaceState() {} }, undefined), { key, room });
});
test('relay/network uncertainty retains the original operation ID', () => {
  for (const code of [undefined, 408, 500, 502, 503, 504]) assert.equal(connection.definiteRejection(code), false);
  for (const code of [400, 401, 403, 409, 429]) assert.equal(connection.definiteRejection(code), true);
});
