import { test, expect } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * Rollengang B-735: der Vater legt einen Stoff ohne Klausurtermin an, sieht „kein Termin“ statt eines erfundenen Datums und
 * trägt den Termin später nach (Lesen zurück). Die Regeln des offenen Plans halten `OffenerPlanTests`; hier steht der Weg
 * durch die laufende App.
 */

const RUN = Date.now().toString(36).slice(-5);

test("Ein Stoffrahmen ohne Termin wird angelegt und der Termin später nachgetragen", async ({ page, request }) => {
  await vaterLogin(page, { id: "1", pin: "0000" });
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const vater = { Authorization: `Bearer ${token}` };
  const childId = await createChild(page, `Offen ${RUN}`, "8685");
  const FACH = `Offen-Fach-${RUN}`;
  const subject = await request.post("/api/v1/creator/subjects", { headers: vater, data: { name: FACH } });
  expect(subject.ok(), await subject.text()).toBeTruthy();

  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Stoff ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("combobox", { name: "Fach (Pflicht)" }).selectOption({ label: FACH });
  await page.getByLabel(/Kein Termin/).check();
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  const planId = Number(/\/vater\/plan\/(\d+)$/.exec(page.url())![1]);

  // Kein Platzhalterdatum: die Kopfzeile sagt es, und der Server hat kein Ende gespeichert.
  await expect(page.getByText(/kein Termin/).first()).toBeVisible();
  const angelegt = await (await request.get(`/api/v1/supervisor/study-plans/${planId}`, { headers: vater })).json();
  expect(angelegt.endDate).toBeNull();

  // Der Termin kommt später: im Bearbeiten-Formular nachtragen.
  await page.getByRole("button", { name: "Bearbeiten" }).click();
  await page.locator("#plan-edit-end").fill("2099-01-15");
  await page.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText(/15\.01\.2099|2099/).first()).toBeVisible();
  const nachgetragen = await (await request.get(`/api/v1/supervisor/study-plans/${planId}`, { headers: vater })).json();
  expect(nachgetragen.endDate).toBe("2099-01-15");
});
