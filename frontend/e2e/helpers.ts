import { expect, type Page } from "@playwright/test";

/** Login credentials for the seeded father (`fid`/`pin`) or a child (`childId`/`pin`). */
export interface LoginCredentials {
  id: string;
  pin: string;
}

/** Logs a supervisor in via id/PIN and waits for the dashboard to render. */
export async function vaterLogin(page: Page, credentials: LoginCredentials) {
  await page.goto("/vater");
  // Erst ausfüllen, wenn die Seite steht, und vor dem Klick prüfen, dass beide Werte noch drinstehen: im vollen Lauf
  // leerte ein spätes Neurendern einmal das Id-Feld, und der Login scheiterte mit „Id oder PIN falsch“ (Backlog-Lauf
  // 2026-11-13, `unit-anlegen-bleibt-im-bild` einzeln 3/3 grün). `toPass` füllt dann erneut statt zu raten.
  await expect(async () => {
    await page.locator("#fid").fill(credentials.id);
    await page.locator("#pin").fill(credentials.pin);
    await expect(page.locator("#fid")).toHaveValue(credentials.id, { timeout: 500 });
    await expect(page.locator("#pin")).toHaveValue(credentials.pin, { timeout: 500 });
  }).toPass({ timeout: 10_000 });
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/**
 * Logs a child in via id + on-screen PIN pad and starts the session. The start button follows the stored arcade
 * language (B-304): "▶ LOS" in German, "▶ GO" in English.
 */
export async function sohnLogin(page: Page, credentials: LoginCredentials) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(credentials.id);
  for (const d of credentials.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: /^▶ (LOS|GO)$/ }).click();
}

/**
 * Creates a child on the supervisor dashboard and returns its id (B-255). The id comes from the create response, not
 * from a table row: the dashboard shows a child's name twice ("Heute" and "Kinder"), and a name that already exists -
 * a retried spec, a second child of the same name - makes any row lookup by name ambiguous.
 */
export async function createChild(page: Page, name: string, pin: string): Promise<string> {
  await page.goto("/vater");
  await page.locator("#new-child-name").fill(name);
  await page.locator("#new-child-pin").fill(pin);
  // Bounded: a form the client refuses (e.g. a too short PIN) sends no request at all, and the screenshot of the form
  // error is worth more than the full test timeout.
  const created = page.waitForResponse((r) =>
    r.request().method() === "POST" && r.url().endsWith("/api/v1/supervisor/children"), { timeout: 10_000 });
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  const response = await created;
  // A refused create fails here with its status and problem detail, not after a timeout with nothing to read.
  expect(response.ok(), `${response.status()} ${await response.text()}`).toBe(true);
  // Without this a missing id becomes the string "undefined", and the failure shows up as a row that never appears (B-357).
  const body = (await response.json()) as { id?: unknown };
  expect(typeof body.id, `create response without a numeric id: ${JSON.stringify(body)}`).toBe("number");
  const id = String(body.id);
  // The "Kinder" table has picked the child up, so the caller continues on a settled page. Its row carries the name as
  // a link and the id in a cell; either alone could repeat (a same-named child, a coin count equal to the id).
  await expect(page.getByRole("row")
    .filter({ has: page.getByRole("link", { name, exact: true }) })
    .filter({ has: page.getByRole("cell", { name: id, exact: true }) })).toBeVisible();
  return id;
}

/** The learn card's labels, German by default (Playwright pins German, B-294); `sprache.spec` plays it in English. */
export interface LernkartenTexte {
  aufdecken: string; neu: string; abschreiben: string; erinnern: string; ok: string; aufgeben: string; weiter: string;
}

export const LERNKARTE_DE: LernkartenTexte = {
  aufdecken: "Aufdecken", neu: "Nein, neu", abschreiben: "Schreib es ab", erinnern: "Und jetzt aus dem Kopf", ok: "OK",
  aufgeben: "Weiß ich nicht", weiter: "Weiter →",
};

/**
 * Plays one learn card (B-598) through: uncover, "new", copy, write from memory, continue. Returns the learnt word the card
 * showed. With `aufgeben` the fourth step ends with "Weiß ich nicht" instead.
 *
 * The first two taps retry until the card reacts: right after a feedback sheet the round swallows taps for a moment
 * (`NACH_BLATT_MS`, so a held Enter cannot run through the card), and Playwright taps faster than a child.
 */
export async function lernkarteDurchspielen(page: Page, aufgeben = false, t: LernkartenTexte = LERNKARTE_DE): Promise<string> {
  const wortFeld = page.locator(".lernkarte-wort .rev");
  await expect(async () => {
    await page.getByRole("button", { name: t.aufdecken, exact: true }).click({ timeout: 2000 });
    await expect(wortFeld).toBeVisible({ timeout: 1000 });
  }).toPass();
  const wort = (await wortFeld.innerText()).replace(/^→\s*/, "").trim();
  const kopie = page.getByLabel(t.abschreiben, { exact: true });
  await expect(async () => {
    await page.getByRole("button", { name: t.neu, exact: true }).click({ timeout: 2000 });
    await expect(kopie).toBeVisible({ timeout: 1000 });
  }).toPass();
  await kopie.fill(wort);
  await page.getByRole("button", { name: t.ok, exact: true }).click();
  if (aufgeben) {
    await page.getByRole("button", { name: t.aufgeben, exact: true }).click();
  } else {
    await page.getByLabel(t.erinnern, { exact: true }).fill(wort);
    await page.getByRole("button", { name: t.ok, exact: true }).click();
  }
  await page.getByRole("button", { name: t.weiter, exact: true }).click();
  return wort;
}

/**
 * Plays the introduction round of a fresh position (B-419) and starts the quiz round: every never-seen card is met on a
 * learn card first (B-598), and the round's end offers "Jetzt abfragen". Specs about the quiz itself start from here, so
 * the introduction does not have to be restated in each of them. Expects to be on the practice screen.
 */
export async function introduceAndQuiz(page: Page, total: number) {
  const counter = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  for (let i = 0; i < total; i++) {
    await expect(counter).toHaveText(`Karte ${i + 1} / ${total}`);
    // Since B-598 the first encounter is a learn card.
    await lernkarteDurchspielen(page);
  }
  await page.getByRole("button", { name: /Jetzt abfragen/ }).click();
  await expect(counter).toHaveText(`Karte 1 / ${total}`);
}

/**
 * Starts play from the learning path on the child's home screen (B-525): taps the node of a position and follows the
 * sheet's link into practice or the test. The link is picked by its target, not by its label - which of the two is
 * the big "Los" button depends on what the server reports as due, and a spec that only wants to get into a round
 * should not break when that changes.
 *
 * Without `title` the plan must carry exactly one position; Playwright's strict mode fails otherwise.
 */
export async function pfadStart(page: Page, weg: "practice" | "test", title?: string) {
  const knoten = title
    ? page.locator(".pfad-zeile", { hasText: title }).locator(".pfad-knoten")
    : page.locator(".pfad-knoten");
  await knoten.click();
  await page.getByRole("dialog").locator(`a[href^="/sohn/${weg}/"]`).click();
}
