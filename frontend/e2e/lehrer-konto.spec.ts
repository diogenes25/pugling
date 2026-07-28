import { test, expect } from "@playwright/test";

/*
 * Das **Lehrer-Konto** aus der Sicht dessen, der es benutzt.
 *
 * Bis hierher war „Lehrersicht" die Erstellen-Perspektive eines Vater-Kontos: Herr Schmidt konnte
 * veröffentlichen und zurückziehen, musste sich aber als Vater anmelden und sah zwei Perspektiven, in denen
 * für ihn nichts liegt. Ein Lehrer-Konto trägt **nur** die Creator-Rolle – alles hier Geprüfte folgt daraus.
 */

const RUN = Date.now().toString().slice(-6);
const TEACHER = { name: `Lehrkraft ${RUN}`, pin: "3131" };

test("Lehrer registriert sich, landet in der Werkstatt und sieht keine Betreuung", async ({ page }) => {
  await page.goto("/vater");
  await page.getByRole("radio", { name: "Neu registrieren" }).click();
  await page.getByRole("radio", { name: /Lehrer/ }).click();
  await page.locator("#reg-name").fill(TEACHER.name);
  await page.locator("#reg-pin").fill(TEACHER.pin);
  await page.locator("#reg-pin2").fill(TEACHER.pin);
  await page.getByRole("button", { name: /Konto anlegen|Registrieren/ }).click();

  // Der Login läuft automatisch – und führt in die Werkstatt, nicht in die Vater-Sicht.
  await expect(page).toHaveURL(/\/vater\/inhalte$/);
  await expect(page.getByRole("heading", { name: "Werkstatt" })).toBeVisible();

  /*
   * Der Umschalter fehlt ganz: bei einer einzigen Perspektive wäre er ein Schalter mit einer Stellung.
   * Genau das ist der sichtbare Unterschied zum Vater-Konto.
   */
  await expect(page.locator("nav.perspective-switch")).toHaveCount(0);
  const areaNav = page.locator("nav.vater-nav");
  for (const area of ["Übungen", "Vokabeln", "Katalog", "Lehrwerke", "Fachlehrer", "Bilder"]) {
    await expect(areaNav.getByRole("link", { name: new RegExp(area) })).toBeVisible();
  }
  for (const area of ["Klassenarbeiten", "Belohnungen", "Shop", "Kontostand", "Lehrpläne", "Assistent"]) {
    await expect(areaNav.getByRole("link", { name: new RegExp(area) })).toHaveCount(0);
  }

  // Das eigene Konto ist erreichbar: die Selbstverwaltung liegt bei `auth/me`, außerhalb der
  // Supervisor-Rolle. Vorher war der Link ausgeblendet, weil er auf einen 403 geführt hätte.
  await page.getByRole("link", { name: new RegExp(TEACHER.name) }).click();
  await expect(page).toHaveURL(/\/vater\/profil$/);
  await expect(page.getByText("Lehrer-Id")).toBeVisible();
  await expect(page.getByText("Betreute Kinder")).toHaveCount(0);
});

test("Lehrer ändert Name und PIN selbst und meldet sich damit an", async ({ page }) => {
  const pin = "5151";
  const created = await page.request.post("/api/v1/creator/teacher-accounts",
    { data: { name: `Umbenennen ${RUN}`, email: null, pin } });
  expect(created.ok(), await created.text()).toBeTruthy();
  const creatorId = (await created.json()).creatorId as number;

  await page.goto("/vater");
  await page.locator("#fid").fill(String(creatorId));
  await page.locator("#pin").fill(pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page).toHaveURL(/\/vater\/inhalte$/);

  await page.goto("/vater/profil");
  await page.locator("#prof-name").fill(`Neuer Name ${RUN}`);
  await page.locator("#prof-pin").fill("6161");
  await page.locator("#prof-pin2").fill("6161");
  await page.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText(/Die neue PIN gilt ab der nächsten Anmeldung/)).toBeVisible();

  // Die Gegenprobe, auf die es ankommt: die neue PIN trägt den nächsten Login – der Hash wurde aufs
  // Konto gespiegelt, sonst liefe der konto-zentrische Login aus dem Takt.
  await page.getByRole("button", { name: "Abmelden" }).first().click();
  await page.locator("#fid").fill(String(creatorId));
  await page.locator("#pin").fill("6161");
  await page.getByRole("button", { name: "Anmelden" }).click();
  /*
   * Angemeldet bleibt er, wo er war: der Sprung in die Werkstatt gilt nur für `/vater` – auf einer
   * perspektivlosen Seite wie dem Konto wäre er eine Entführung. Geprüft wird darum die Anmeldung selbst
   * (der Kopf trägt den neuen Namen) und nicht die Adresse.
   */
  await expect(page.getByRole("link", { name: new RegExp(`Neuer Name ${RUN}`) })).toBeVisible();
  await expect(page.locator("#fid")).toHaveCount(0);
});

test("Betreuungs-Seiten sind für den Lehrer nicht erreichbar – auch nicht per Adresse", async ({ page }) => {
  // Anmelden mit der Id aus der Registrierung des vorigen Tests wäre eine Kopplung; darum eigenes Konto.
  const pin = "4242";
  const created = await page.request.post("/api/v1/creator/teacher-accounts",
    { data: { name: `Direktweg ${RUN}`, email: null, pin } });
  expect(created.ok(), await created.text()).toBeTruthy();
  const creatorId = (await created.json()).creatorId as number;

  await page.goto("/vater");
  await page.locator("#fid").fill(String(creatorId));
  await page.locator("#pin").fill(pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page).toHaveURL(/\/vater\/inhalte$/);

  // Ein Lesezeichen auf eine Betreuungs-Seite führt in die Werkstatt statt in einen Bereich, dessen
  // Endpunkte ohnehin mit 403 antworten.
  for (const path of ["/vater", "/vater/plaene", "/vater/shop", "/vater/kind/1"]) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/vater\/inhalte$/);
  }
});

test("Der Vater behält beide Perspektiven", async ({ page }) => {
  // Gegenprobe: die Trennung darf das bestehende Vater-Konto nicht beschneiden.
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();

  await expect(page.locator("nav.perspective-switch")).toBeVisible();
  for (const p of ["Betreuen", "Zuweisen", "Erstellen"]) {
    await expect(page.locator("nav.perspective-switch").getByRole("link", { name: new RegExp(p) })).toBeVisible();
  }
});
