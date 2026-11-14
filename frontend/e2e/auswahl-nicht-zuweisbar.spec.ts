import { test, expect, type Page } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * B-140 AK 5: eine zurückgezogene Übung ist für einen Erwachsenen ohne Grant im Planbau sichtbar, aber gesperrt – mit
 * Grund. Vorher war sie wählbar, und erst das Speichern scheiterte mit `exercise_not_executable` (403).
 *
 * Aufbau per API, geprüft wird die Oberfläche: Der geseedete Vater (Adult 1) legt eine Übung an und zieht sie zurück
 * (`PATCH …/sharing`, der Weg aus B-11), dazu eine freigegebene als Gegenprobe. Ein frisch registrierter zweiter
 * Erwachsener – ohne Grant, also nicht zuweisend – baut im UI einen Plan und sieht beide Zeilen.
 *
 * Rot, wenn das Radio nur an `zeilenVeraltet` hängt (dann ist die zurückgezogene wählbar) oder der Grund fehlt.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function api(page: Page, method: "POST" | "PATCH", url: string, data: unknown, bearer: string) {
  const res = await page.request.fetch(url, { method, data, headers: { Authorization: `Bearer ${bearer}` } });
  expect(res.ok(), `${method} ${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return res.json();
}

async function tokenFor(page: Page, adultId: string | number, pin: string): Promise<string> {
  const res = await page.request.post("/api/v1/auth/adult", { data: { adultId: Number(adultId), pin } });
  expect(res.ok(), `login ${adultId}: ${res.status()}`).toBeTruthy();
  return (await res.json()).token;
}

/**
 * Aufbau beider Fälle: der geseedete Vater legt in einem eigenen Fach eine zurückgezogene und eine freigegebene Übung an,
 * ein frisch registrierter Erwachsener ohne Grant meldet sich im UI an und legt ein Kind an. Je Test ein eigenes Fach –
 * Fachnamen sind im geteilten Katalog eindeutig.
 */
async function aufbau(page: Page, tag: string) {
  const fach = `Freigabefach ${tag} ${RUN}`;
  const zurueck = `Zurückgezogen ${tag} ${RUN}`;
  const frei = `Freigegeben ${tag} ${RUN}`;

  const ersteller = await tokenFor(page, FATHER.id, FATHER.pin);
  const subjectId = (await api(page, "POST", "/api/v1/creator/subjects", { name: fach }, ersteller)).id;
  const seriesId = (await api(page, "POST", "/api/v1/creator/textbook-series",
    { name: `Freigabereihe ${tag} ${RUN}`, subjectId }, ersteller)).id;
  const unitId = (await api(page, "POST", `/api/v1/creator/textbook-series/${seriesId}/units`,
    { label: "Unit 1", orderIndex: 1 }, ersteller)).id;
  const uebung = (title: string, orderIndex: number) => api(page, "POST",
    `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, {
      title, orderIndex, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `word ${title}`, back: "Wort" }] },
    }, ersteller);
  const zurueckId = (await uebung(zurueck, 0)).id;
  await uebung(frei, 1);
  await api(page, "PATCH", `/api/v1/creator/exercises/${zurueckId}/sharing`, { executePublic: false }, ersteller);

  // Anonym registrieren – die Id ist der Login-Name.
  const reg = await page.request.post("/api/v1/supervisor/adults", { data: { name: `Fremder ${tag} ${RUN}`, pin: "4321" } });
  expect(reg.ok(), `register: ${reg.status()}`).toBeTruthy();
  const fremdId = String((await reg.json()).id);
  await vaterLogin(page, { id: fremdId, pin: "4321" });
  const childId = await createChild(page, `Kind ${tag} ${RUN}`, "1234");
  return { fach, zurueck, frei, zurueckId, childId, fremdId };
}

test("Eine zurückgezogene Übung ist im Planbau eines Fremden gesperrt und nennt den Grund", async ({ page }) => {
  const { fach, zurueck, frei, zurueckId, childId } = await aufbau(page, "Planbau");

  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Freigabeplan ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  const planId = Number(/\/vater\/plan\/(\d+)$/.exec(page.url())![1]);
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: fach });

  const radios = page.getByRole("radiogroup", { name: "Übung wählen" }).getByRole("radio");
  await expect(radios).toHaveCount(2);

  // Gesperrt, nicht ausgeblendet – und der Grund steht als Text daneben, am Radio als Beschreibung.
  const gesperrt = page.getByRole("radio", { name: new RegExp(zurueck) });
  await expect(gesperrt).toBeDisabled();
  await expect(gesperrt).toHaveAccessibleDescription(/Nicht zuweisbar – der Ersteller hat sie nicht für alle freigegeben/);
  // Gegenprobe: die freigegebene ist wählbar, also sperrt nicht etwa die ganze Liste.
  const waehlbar = page.getByRole("radio", { name: new RegExp(frei) });
  await expect(waehlbar).toBeEnabled();
  await waehlbar.check();
  await expect(waehlbar).toBeChecked();

  // Die Sperre im UI sagt dasselbe wie der Server: das Zuweisen der zurückgezogenen scheitert mit 403.
  const fremdToken = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const versuch = await page.request.post(`/api/v1/supervisor/study-plans/${planId}/positions`, {
    data: { exerciseId: zurueckId }, headers: { Authorization: `Bearer ${fremdToken}` },
  });
  expect(versuch.status()).toBe(403);
  expect(await versuch.text()).toContain("exercise_not_executable");
});

/*
 * B-140 AK 4 an der laufenden App: „Alle wählen" im Assistenten übernimmt nur, was der Vater zuweisen darf. Der Vitest hält
 * die Regel (`nurZuweisbare`); diese Spec hält die Aufrufstelle in `selectAll` – ein Rückbau auf alle Ids wählte die
 * zurückgezogene mit und verschwiege die übersprungene.
 */
test("„Alle wählen“ im Assistenten überspringt die zurückgezogene Übung und sagt es", async ({ page }) => {
  const { fach, zurueck, frei, childId } = await aufbau(page, "Assistent");

  await page.goto("/vater/wizard");
  await page.getByRole("radio", { name: "Bestehendes Kind" }).click();
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: fach });
  // B-423: das vorbelegte Ziel „Klassenarbeit" verlangt einen Termin – diese Spec meint die freie Dauer.
  await page.getByRole("radio", { name: /Regelmäßig üben/ }).click();
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Übungen wählen/ })).toBeVisible();

  const gesperrt = page.getByRole("checkbox", { name: new RegExp(zurueck) });
  const waehlbar = page.getByRole("checkbox", { name: new RegExp(frei) });
  await expect(gesperrt).toBeDisabled();
  await expect(gesperrt).toHaveAccessibleDescription(/Nicht zuweisbar – der Ersteller hat sie nicht für alle freigegeben/);
  await expect(waehlbar).toBeEnabled();

  await page.getByRole("button", { name: "Alle wählen" }).click();
  await expect(page.getByText("1 Übung gewählt, 1 nicht zuweisbare übersprungen.")).toBeVisible();
  await expect(waehlbar).toBeChecked();
  await expect(gesperrt).not.toBeChecked();
});

// Review Sprint 74: auch die Klassenarbeit-Auswahl sperrt die zurückgezogene Übung – vorher stand dort „zuweisen“, und
// erst der Klick scheiterte mit 403. Rot, wenn `VaterClassTests` den Knopf ohne Blick auf `canAssign` rendert.
test("Die Klassenarbeit-Auswahl bietet eine zurückgezogene Übung nicht zum Zuweisen an und nennt den Grund", async ({ page }) => {
  const { zurueck, frei, childId, fremdId } = await aufbau(page, "Klassenarbeit");
  const fremd = await tokenFor(page, fremdId, "4321");
  await api(page, "POST", "/api/v1/supervisor/class-tests",
    { childId: Number(childId), title: `Arbeit ${RUN}`, scheduledDate: "2026-12-01" }, fremd);

  await page.goto(`/vater/class-tests?childId=${childId}`);
  await page.getByRole("button", { name: "Übungen & Vorbereiten" }).click();
  await page.getByRole("textbox", { name: "Suche im Katalog" }).fill(`${RUN}`);
  await page.getByRole("button", { name: "Suchen" }).click();

  const zeile = (titel: string) => page.locator("li", { hasText: titel });
  await expect(zeile(frei).getByRole("button", { name: "zuweisen" })).toBeVisible();
  await expect(zeile(zurueck)).toContainText("Nicht zuweisbar");
  await expect(zeile(zurueck).getByRole("button", { name: "zuweisen" })).toHaveCount(0);
});
