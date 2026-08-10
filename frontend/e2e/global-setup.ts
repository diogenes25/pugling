import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { dbFile, mediaDir } from "./temp-paths";

/**
 * Sweeps stale `pugling-e2e-*` entries left behind by an <b>earlier</b> run (B-55) - the actual
 * guarantee against unbounded growth, not `global-teardown.ts`'s own delete of *this* run's files.
 * <p>
 * <b>Runs after this run's own backend has already started</b> (Playwright's `TaskRunner` sets up the
 * `webServer` plugins - which start the backend and thus create this run's `dbFile`/`mediaDir` - before
 * running the user's `globalSetup`), so the own two paths are excluded explicitly: without that, the
 * sweep would delete files it just created for itself, relying only on the backend recreating them on
 * first write to still work by accident.
 */
export default async function globalSetup() {
  // Best-effort: a leftover from the immediately preceding run can still be locked (its backend process
  // is still alive - Playwright tears down `webServer` only *after* global-teardown.ts, see there).
  // Failing setup over a lock this run does not own would block this run's own tests for it - a
  // `Promise.all` did exactly that on this machine before this was changed to `allSettled`. Whatever
  // outlasts this sweep too just waits for the run after that.
  const tmp = os.tmpdir();
  // Ausschluss über das **Präfix**, nicht über zwei exakte Namen (B-139). SQLite legt Beidateien neben
  // die Datenbank – `…​.db-journal`, bei WAL `-wal`/`-shm` –, und die tragen dasselbe `pugling-e2e-`
  // Präfix wie das, was hier gefegt werden soll. Eine Ausnahmeliste aus zwei exakten Namen ließ sie
  // durch, und weil dieser Sweep laut Kommentar oben **nach** dem Start des eigenen Backends läuft,
  // löschte er das lebende Journal seiner eigenen Datenbank.
  //
  // Warum das nur in CI weh tat: unter Linux gelingt `unlink` auf eine offene Datei, SQLite verliert sein
  // Journal mitten in einer Transaktion und antwortet fortan sporadisch mit `SQLITE_IOERR`
  // („disk I/O error"). Unter Windows scheitert dasselbe `rm` mit `EBUSY`, und `allSettled` schluckt es –
  // dieselbe Suite lief hier grün, während der Nachtlauf sechs Nächte rot war.
  const ownPrefixes = [path.basename(dbFile), path.basename(mediaDir)];
  const entries = await fs.readdir(tmp).catch(() => [] as string[]);
  await Promise.allSettled(
    entries
      .filter((name) => name.startsWith("pugling-e2e-")
        && !ownPrefixes.some((own) => name.startsWith(own)))
      .map((name) => fs.rm(path.join(tmp, name), { recursive: true, force: true })),
  );
}
