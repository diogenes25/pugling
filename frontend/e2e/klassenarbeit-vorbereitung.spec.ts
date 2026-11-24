import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * B-752/B-757/B-758 – der Rollengang des Vaters und des Sohns: der Vater plant im Klassenarbeiten-Dialog eine Arbeit mit
 * eigenem Vorbereitungs-Fenster, sieht „etwa N Karten je Tag“ vom Server und, für ein Kind ohne Plan im Fach, den Grund;
 * das Kind meldet sich an und findet die Vorbereitungs-Pflicht als Knoten auf seinem Pfad. Stoff über die API (Gegenstand ist
 * der Dialog und die Pflicht, nicht das Anlegen), je Lauf eigene Kinder. Termin = heute + 10, Fenster 12 Tage: liegt sicher
 * im Fenster, auch wenn der Server (deutsche Zeit, B-730) schon einen Tag weiter zählt als die UTC-Uhr dieser Spec.
 */

const RUN = Date.now().toString(36).slice(-5);
const TAG = 86_400_000;
const iso = (tage: number) => new Date(Date.now() + tage * TAG).toISOString().slice(0, 10);

async function postJson(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return res.json();
}

test("Vater stellt das Fenster ein, sieht die Last, das Kind findet die Pflicht auf seinem Pfad", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  const fach = `KV-Fach ${RUN}`;
  const titel = `KV-Wörter ${RUN}`;
  const pin = "8663";
  const childId = (await postJson(request, "/api/v1/supervisor/children", headers, { name: `KV-Kind ${RUN}`, pin })).id;
  const ohnePlan = (await postJson(request, "/api/v1/supervisor/children", headers, { name: `KV-Ohne ${RUN}`, pin })).id;
  const subjectId = (await postJson(request, "/api/v1/creator/subjects", headers, { name: fach })).id;
  const seriesId = (await postJson(request, "/api/v1/creator/textbook-series", headers, { name: `KV-Reihe ${RUN}`, subjectId })).id;
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = (await postJson(request, units, headers, { label: "Unit 1", orderIndex: 1 })).id;
  const words = Array.from({ length: 24 }, (_, i) => ({ front: `word${i} ${RUN}`, back: `Wort${i}` }));
  await postJson(request, `${units}/${unitId}/vocabulary`, headers, {
    title: titel, orderIndex: 1, rewardPoints: 10, defaultUseLeitner: true,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: words },
  });
  await postJson(request, "/api/v1/supervisor/study-plans", headers, { childId, title: `KV-Plan ${RUN}`, subjectId, durationDays: 30 });

  await vaterLogin(page, { id: "1", pin: "0000" });

  // Das Kind mit Plan: Arbeit im Dialog planen, mit eigenem Fenster.
  await page.goto(`/vater/class-tests?childId=${childId}`);
  await page.getByLabel("Titel (Pflicht)").fill(`Vokabeltest ${RUN}`);
  await page.getByTitle("Fach").selectOption({ label: fach });
  await page.getByLabel("Termin (Pflicht)").fill(iso(10));
  await page.getByLabel("Vorbereitung (Tage)").fill("12");
  await page.getByRole("button", { name: "Planen" }).click();
  const zeile = page.getByRole("row", { name: new RegExp(`Vokabeltest ${RUN}`) });
  await expect(zeile).toBeVisible();
  // Noch kein Bündel: der Server meldet 0 Karten – erst die Übung zuweisen.
  await zeile.getByRole("button", { name: "Übungen & Vorbereiten" }).click();
  await page.getByLabel("Suche im Katalog").fill(titel);
  await page.getByRole("button", { name: "Suchen" }).click();
  await page.getByRole("listitem").filter({ hasText: titel }).getByRole("button", { name: "zuweisen" }).click();
  // Das Zuweisen gleicht sofort ab (6162599c): das Fenster läuft schon, die Pflicht startet heute und verteilt 24 Karten auf
  // die verbleibenden 10 Tage (9, wenn der Server in deutscher Zeit schon einen Tag weiter ist) – beides 3 je Tag. Die Zahl
  // rechnet der Server.
  await expect(zeile).toContainText("etwa 3 Karten je Tag");
  await expect(zeile).toContainText("12 Tage");

  // Das Kind ohne Plan im Fach: keine Pflicht, und die Liste sagt warum.
  await page.goto(`/vater/class-tests?childId=${ohnePlan}`);
  await page.getByLabel("Titel (Pflicht)").fill(`Ohne Plan ${RUN}`);
  await page.getByTitle("Fach").selectOption({ label: fach });
  await page.getByLabel("Termin (Pflicht)").fill(iso(10));
  await page.getByRole("button", { name: "Planen" }).click();
  await expect(page.getByRole("row", { name: new RegExp(`Ohne Plan ${RUN}`) })).toContainText("kein aktiver Plan im Fach");

  // Der Sohn meldet sich an: der Abgleich legt die Pflicht an, sie steht als Knoten auf seinem Pfad.
  await page.context().clearCookies();
  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.locator(".pfad-zeile", { hasText: titel })).toBeVisible();
  await expect(page.getByRole("region", { name: "Nächste Klassenarbeit" })).toContainText(`Vokabeltest ${RUN}`);
});
