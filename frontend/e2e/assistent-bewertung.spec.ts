import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-408: der Assistent ist der Zwilling der Übungsauswahl im Planbau (B-270, `planbau-bewertung.spec.ts`) – jede Zeile
 * zeigt ihre Bewertung, und „beste Bewertung zuerst" stellt die Trefferliste um. Anders als im Planbau ist Sortieren hier
 * KEIN Suchkriterium: eine vorher gewählte Übung bleibt gewählt (Entscheidung 2).
 *
 * Aufbau wie im Planbau: drei eigene Übungen in einem eigenen Fach, zwei davon von einem ZWEITEN Konto bewertet (die
 * eigene darf man nicht bewerten). Im Katalog steht die mittel bewertete (2 ★) vorn, die unbewertete in der Mitte, die gut
 * bewertete (5 ★) hinten – erst damit unterscheidet die Spec „beste zuerst" von „schlechteste zuerst" (B-412).
 *
 * Ein neues Kind ohne Klasse, damit der serverseitige Klassen-Filter keine der Zeilen verschluckt (`assistent.spec.ts`).
 *
 * Rot, wenn die Zeile die Bewertung nicht zeigt, die Sortierwahl die Abfrage nicht umstellt oder aufsteigend sortiert,
 * oder wenn Umsortieren die Auswahl verwirft.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function token(page: Page) {
  return page.evaluate(() => localStorage.getItem("pugling.token"));
}

async function api(page: Page, method: "POST" | "PUT", url: string, data: unknown, bearer?: string | null) {
  const res = await page.request.fetch(url, {
    method, data, headers: { Authorization: `Bearer ${bearer ?? await token(page)}` },
  });
  expect(res.ok(), `${method} ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Der Assistent zeigt die Bewertung, sortiert nach ihr und behält dabei die Auswahl", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const fach = `Assistentenfach ${RUN}`;
  const erste = `Aaa unbewertet ${RUN}`;
  const zweite = `Zzz gut bewertet ${RUN}`;
  const mittel = `Aa0 mittel bewertet ${RUN}`;

  const subjectId = (await api(page, "POST", "/api/v1/creator/subjects", { name: fach })).id;
  const seriesId = (await api(page, "POST", "/api/v1/creator/textbook-series", { name: `Assistentenreihe ${RUN}`, subjectId })).id;
  const unitId = (await api(page, "POST", `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  const uebung = (title: string, orderIndex: number) => api(page, "POST",
    `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, {
      title, orderIndex, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `hello ${title}`, back: "hallo" }] },
    });
  const mittelId = (await uebung(mittel, 0)).id;
  await uebung(erste, 1);
  const gut = (await uebung(zweite, 2)).id;

  const fremd = await page.request.post("/api/v1/supervisor/adults", { data: { name: `Bewerter ${RUN}`, pin: "4321" } });
  expect(fremd.ok()).toBeTruthy();
  const fremdId = (await fremd.json()).id;
  const login = await page.request.post("/api/v1/auth/adult", { data: { adultId: fremdId, pin: "4321" } });
  expect(login.ok()).toBeTruthy();
  const fremdToken = (await login.json()).token;
  await api(page, "PUT", `/api/v1/creator/exercises/${gut}/rating`, { stars: 5, comment: null }, fremdToken);
  await api(page, "PUT", `/api/v1/creator/exercises/${mittelId}/rating`, { stars: 2, comment: null }, fremdToken);

  await page.goto("/vater/wizard");
  await page.getByRole("radio", { name: "Neues Kind anlegen" }).click();
  await page.locator("#wiz-name").fill(`E2E-Bewertung-${RUN}`);
  await page.locator("#wiz-pin").fill("1357");
  await page.getByRole("button", { name: "Weiter" }).click();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: fach });
  // B-423: das vorbelegte Ziel „Klassenarbeit" verlangt einen Termin – diese Spec meint die freie Dauer.
  await page.getByRole("radio", { name: /Regelmäßig üben/ }).click();
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Übungen wählen/ })).toBeVisible();

  const zeilen = page.getByRole("checkbox");
  await expect(zeilen).toHaveCount(3);
  // Die Zeile trägt ihre Bewertung – die unbewertete sagt es auch.
  await expect(page.getByRole("checkbox", { name: new RegExp(`${zweite}.*★ 5,0 \\(1 Bewertung\\)`) })).toBeVisible();
  await expect(page.getByRole("checkbox", { name: new RegExp(`${erste}.*noch unbewertet`) })).toBeVisible();
  await expect(page.getByRole("checkbox", { name: new RegExp(`${mittel}.*★ 2,0`) })).toBeVisible();
  // Katalog-Reihenfolge – der Ausgangszustand, gegen den die Umstellung misst.
  await expect(zeilen.nth(0)).toHaveAccessibleName(new RegExp(mittel));
  await expect(zeilen.nth(1)).toHaveAccessibleName(new RegExp(erste));

  // Vor dem Umsortieren gewählt: die unbewertete.
  const gewaehlt = page.getByRole("checkbox", { name: new RegExp(erste) });
  await gewaehlt.check();
  await expect(page.getByRole("heading", { name: /\(1 gewählt\)/ })).toBeVisible();

  await page.getByLabel("Trefferliste sortieren", { exact: true }).selectOption("rating");
  await expect(zeilen.nth(0)).toHaveAccessibleName(new RegExp(zweite));
  await expect(zeilen.nth(1)).toHaveAccessibleName(new RegExp(mittel));
  await expect(zeilen.nth(2)).toHaveAccessibleName(new RegExp(erste));
  // Entscheidung 2: Umsortieren ist kein Suchkriterium – die Auswahl bleibt, und kein „Auswahl zurückgesetzt".
  await expect(gewaehlt).toBeChecked();
  await expect(page.getByRole("heading", { name: /\(1 gewählt\)/ })).toBeVisible();
  await expect(page.getByText(/Auswahl zurückgesetzt/)).toHaveCount(0);
});
