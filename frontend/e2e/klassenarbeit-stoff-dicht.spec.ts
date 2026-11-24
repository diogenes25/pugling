import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * Sprint 135 (B-940, B-941, B-942) – der Rollengang über den Stoff einer Klassenarbeit: Der Vater plant eine Arbeit mit einer
 * eigenen Übung und einem Schlagwort, das zurückgezogenes fremdes Material trägt. Die Liste sagt ihm, dass eine Übung ohne
 * Ausführ-Recht ausgelassen wird (B-941); das Fenster speichert er im Dialog und sieht die Bestätigung (B-942). Das Kind kann
 * das Schlagwort der geplanten Arbeit nicht lösen (B-940, über die API – eine Oberfläche dafür hat es nicht) und findet nur die
 * erlaubte Übung als Pflicht auf seinem Pfad. Stoff über die API, je Lauf eigene Namen; Termin heute + 10, Fenster 12 Tage
 * (sicher im Fenster, auch bei einem Tag Versatz zwischen deutscher Zeit und UTC).
 */

const RUN = Date.now().toString(36).slice(-5);
const TAG = 86_400_000;
const iso = (tage: number) => new Date(Date.now() + tage * TAG).toISOString().slice(0, 10);

async function send(request: APIRequestContext, method: "POST" | "PATCH", url: string, token: string, data: unknown) {
  const res = await request.fetch(url, { method, headers: { Authorization: `Bearer ${token}` }, data });
  expect(res.ok(), `${method} ${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return res.json();
}

async function token(request: APIRequestContext, url: string, data: unknown): Promise<string> {
  const res = await request.post(url, { data });
  expect(res.ok(), `${url}: ${res.status()}`).toBeTruthy();
  return (await res.json()).token;
}

/** Fach, Reihe, Unit und eine Vokabelübung eines Erwachsenen; liefert die Übungs-Id. */
async function uebung(request: APIRequestContext, t: string, name: string, subjectId?: number) {
  const fach = subjectId ?? (await send(request, "POST", "/api/v1/creator/subjects", t, { name: `Fach ${name}` })).id;
  const seriesId = (await send(request, "POST", "/api/v1/creator/textbook-series", t, { name: `Reihe ${name}`, subjectId: fach })).id;
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = (await send(request, "POST", units, t, { label: "Unit 1", orderIndex: 1 })).id;
  const words = Array.from({ length: 12 }, (_, i) => ({ front: `w${i} ${name}`, back: `Wort${i}` }));
  const id = (await send(request, "POST", `${units}/${unitId}/vocabulary`, t, {
    title: name, orderIndex: 1, rewardPoints: 10, defaultUseLeitner: true,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: words },
  })).id;
  return { id, subjectId: fach };
}

test("Der Stoff der Arbeit ist dicht: zurückgezogenes Material fällt sichtbar heraus, das Kind kann nichts lösen", async ({ page, request }) => {
  const vater = await token(request, "/api/v1/auth/adult", { adultId: 1, pin: "0000" });
  const eigen = await uebung(request, vater, `KS-Eigen ${RUN}`);

  // Ein zweiter Erwachsener legt Material an und zieht es zurück – für den Vater nicht ausführbar.
  const reg = await request.post("/api/v1/supervisor/adults", { data: { name: `KS-Fremd ${RUN}`, pin: "4321" } });
  expect(reg.ok(), `register: ${reg.status()}`).toBeTruthy();
  const fremdT = await token(request, "/api/v1/auth/adult", { adultId: (await reg.json()).id, pin: "4321" });
  const fremdName = `KS-Fremd-Übung ${RUN}`;
  const fremd = await uebung(request, fremdT, fremdName);
  await send(request, "PATCH", `/api/v1/creator/exercises/${fremd.id}/sharing`, fremdT, { executePublic: false });

  const pin = "8664";
  const childId = (await send(request, "POST", "/api/v1/supervisor/children", vater, { name: `KS-Kind ${RUN}`, pin })).id;
  await send(request, "POST", "/api/v1/supervisor/study-plans", vater,
    { childId, title: `KS-Plan ${RUN}`, subjectId: eigen.subjectId, durationDays: 30 });
  const tagId = (await send(request, "POST", "/api/v1/creator/tags", vater, { childId, name: `KS-Arbeit ${RUN}` })).id;
  await send(request, "POST", `/api/v1/creator/tags/${tagId}/exercises`, vater, { exerciseIds: [fremd.id] });
  await send(request, "POST", "/api/v1/supervisor/class-tests", vater, {
    childId, title: `KS-Test ${RUN}`, subjectId: eigen.subjectId, scheduledDate: iso(10),
    exerciseIds: [eigen.id], tagIds: [tagId], prepWindowDays: 12,
  });

  // Vater: die Liste nennt die ausgelassene Übung (B-941), das Speichern des Fensters bestätigt sich sichtbar (B-942).
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/class-tests?childId=${childId}`);
  const zeile = page.getByRole("row", { name: new RegExp(`KS-Test ${RUN}`) });
  await expect(zeile).toContainText("1 Übung ohne Ausführ-Recht ausgelassen");
  await zeile.getByRole("button", { name: "Übungen & Vorbereiten" }).click();
  await page.getByLabel(/^Vorbereitung \(Tage, leer = Standard/).fill("11");
  await page.getByRole("button", { name: "Fenster speichern" }).click();
  await expect(page.getByText("Vorbereitung: 11 Tage.")).toBeVisible();
  await expect(zeile).toContainText("11 Tage");

  // Kind: das Schlagwort der geplanten Arbeit bleibt, wie es ist (B-940) …
  const kind = await token(request, "/api/v1/auth/child", { childId, pin });
  const loesen = await request.delete(`/api/v1/creator/tags/${tagId}/exercises/${fremd.id}`,
    { headers: { Authorization: `Bearer ${kind}` } });
  expect(loesen.status()).toBe(409);
  expect((await loesen.json()).code).toBe("tag_locked_by_class_test");
  const loeschen = await request.delete(`/api/v1/creator/tags/${tagId}`, { headers: { Authorization: `Bearer ${kind}` } });
  expect(loeschen.status()).toBe(409);

  // … und auf seinem Pfad steht nur die erlaubte Übung als Pflicht.
  await page.context().clearCookies();
  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.locator(".pfad-zeile", { hasText: `KS-Eigen ${RUN}` })).toBeVisible();
  await expect(page.locator(".pfad-zeile", { hasText: fremdName })).toHaveCount(0);
});
