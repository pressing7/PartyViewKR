"use strict";
(() => {
  const keyPattern = /^[a-f0-9]{64}$/i, roomPattern = /^[a-f0-9]{32}$/;
  function parse(value, origin) {
    value = value.trim();
    let key = "", room = "";
    if (value.includes("://")) {
      let url; try { url = new URL(value); } catch { return null; }
      if (url.origin !== origin || url.username || url.password || url.pathname !== "/" || url.search) return null;
      const params = new URLSearchParams(url.hash.slice(1)); key = params.get("key") || ""; room = params.get("room") || "";
    } else {
      const parts = value.split(".");
      if (parts.length === 1) [key] = parts;
      else if (parts.length === 2) [room, key] = parts;
      else return null;
    }
    return keyPattern.test(key) && (!room || roomPattern.test(room)) ? { key: key.toUpperCase(), room } : null;
  }
  function read(location, history, storage) {
    let result = null;
    try {
      if (location.hash) {
        result = parse(location.origin + "/" + location.hash, location.origin);
        // A new link replaces all credentials, never mix its key with an old room.
        try { storage.removeItem("pvkr.connection"); storage.removeItem("pvkr.room"); storage.removeItem("pvkr.operation"); } catch { }
      } else {
        const key = storage.getItem("pvkr.connection") || "", room = storage.getItem("pvkr.room") || "";
        result = parse(room ? room + "." + key : key, location.origin);
      }
    } catch { }
    history.replaceState(null, "", location.pathname);
    if (result) {
      try { storage.setItem("pvkr.connection", result.key); storage.setItem("pvkr.room", result.room); } catch { }
    }
    return result || { key: "", room: "" };
  }
  // Only definite rejections clear an uncertain POST. Network/relay timeouts
  // can occur after acceptance; observe that request ID instead of resending.
  const definiteRejection = status => status >= 400 && status < 500 && status !== 408;
  const api = { parse, read, definiteRejection };
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else window.PvkrConnection = api;
})();
