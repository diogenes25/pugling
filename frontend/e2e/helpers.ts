import { expect, type Page } from "@playwright/test";

/** Login credentials for the seeded father (`fid`/`pin`) or a child (`childId`/`pin`). */
export interface LoginCredentials {
  id: string;
  pin: string;
}

/** Logs a supervisor in via id/PIN and waits for the dashboard to render. */
export async function vaterLogin(page: Page, credentials: LoginCredentials) {
  await page.goto("/vater");
  await page.locator("#fid").fill(credentials.id);
  await page.locator("#pin").fill(credentials.pin);
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

/**
 * Plays the introduction round of a fresh position (B-419) and starts the quiz round: every never-seen card is shown
 * with its solution first ("Weiter →"), and the round's end offers "Jetzt abfragen". Specs about the quiz itself start
 * from here, so the introduction does not have to be restated in each of them. Expects to be on the practice screen.
 */
export async function introduceAndQuiz(page: Page, total: number) {
  const counter = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  for (let i = 0; i < total; i++) {
    await expect(counter).toHaveText(`Karte ${i + 1} / ${total}`);
    await page.getByRole("button", { name: "Weiter →" }).click();
  }
  await page.getByRole("button", { name: /Jetzt abfragen/ }).click();
  await expect(counter).toHaveText(`Karte 1 / ${total}`);
}
