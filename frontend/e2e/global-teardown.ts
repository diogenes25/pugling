import fs from "node:fs/promises";
import { dbFile, mediaDir } from "./temp-paths";

/**
 * Best-effort delete of this run's throwaway DB and media folder (B-55) – unconditionally, on a red run
 * too: `trace: "retain-on-failure"` already carries the failure's evidence, and a second, conditional
 * teardown would need Playwright's run status inside a module that does not receive it directly, just
 * to guard the same case twice.
 * <p>
 * <b>Best-effort, not guaranteed</b> - and deliberately non-throwing. This runs <i>before</i> Playwright
 * stops the `webServer` processes (its `TaskRunner` tears down user teardowns first, `webServer` plugins
 * last), so the backend is still alive and its `Microsoft.Data.Sqlite` connection pool still holds a
 * native handle on `dbFile` - measured: `EBUSY` survives over two minutes of linear-backoff retries here,
 * because the thing holding the file only stops *after* this function returns, not because of a slow
 * external scanner. Failing the whole run over a lock this function cannot ever see released would be
 * exactly the wrong signal. The actual guarantee against unbounded growth is
 * {@link ../e2e/global-setup.ts}, which sweeps whatever this call could not remove before the *next*
 * run seeds its own files - by then the backend of *this* run is unambiguously gone.
 */
export default async function globalTeardown() {
  const retry = { recursive: true, force: true, maxRetries: 5, retryDelay: 500 };
  await Promise.allSettled([fs.rm(dbFile, retry), fs.rm(mediaDir, retry)]);
}
