import { mkdir, copyFile } from "node:fs/promises";
// Ship the very same UI as the plugin; no separate copy to drift out of sync.
await mkdir(new URL("assets/", import.meta.url), { recursive: true });
for (const name of ["index.html", "style.css", "app.js", "editor.js", "connection.js"])
  await copyFile(new URL("../src/Web/" + name, import.meta.url), new URL("assets/" + name, import.meta.url));
