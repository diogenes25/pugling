import os from "node:os";
import path from "node:path";

// Evaluated once and shared between playwright.config.ts and global-teardown.ts (B-55): a teardown
// module is loaded separately from the config, so recomputing `Date.now()` there would name different,
// nonexistent files and delete nothing.
const runId = Date.now();

/** Throwaway backend DB of this Playwright run – the real `pugling.db` stays untouched. */
export const dbFile = path.join(os.tmpdir(), `pugling-e2e-${runId}.db`);

/** Throwaway media-upload folder of this run – otherwise the dev tree collects files with every run. */
export const mediaDir = path.join(os.tmpdir(), `pugling-e2e-media-${runId}`);
