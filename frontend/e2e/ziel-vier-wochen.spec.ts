import { test, expect } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

/*
 * Das Produktziel als Rollengang (Fertigstellungs-Lauf 2026-11-10): der Vater fügt eine Französisch-Liste ein, macht
 * daraus EINE tägliche Pflicht mit Tagesdosis, und das Kind bekommt am ersten Tag nur seine Portion – alles in der
 * Oberfläche. Die 28 Tage selbst spielt `VierWochenTests` im Backend auf einer vorgestellten Uhr; hier steht der
 * Tag 1 so, wie Vater und Kind ihn sehen.
 */

const RUN = Date.now().toString().slice(-6);
const SERIES = `Découvertes 4 ${RUN}`;
const UNIT = `Unité 1 – ${RUN}`;
const TITLE = `Vocabulaire Unité 1 ${RUN}`;
const CHILD = { name: `Neuntklässler ${RUN}`, pin: "2468" };
const DOSE = 4;
const PAIRS: [string, string][] = [
  "la ville|die Stadt", "la rue|die Straße", "le magasin|das Geschäft", "le quartier|das Viertel",
  "la gare|der Bahnhof", "le pont|die Brücke", "la mairie|das Rathaus", "le marché|der Markt",
  "la place|der Platz", "le bâtiment|das Gebäude", "la banlieue|der Vorort", "le trottoir|der Gehweg",
].map((l) => { const [w, t] = l.split("|"); return [`${w} ${RUN}`, t]; });

test("Vater gibt eine Liste als tägliche Pflicht mit Tagesdosis auf, das Kind bekommt seine Portion", async ({ browser }) => {
  const vater = await (await browser.newContext()).newPage();
  await vaterLogin(vater, { id: "1", pin: "0000" });
  const childId = await createChild(vater, CHILD.name, CHILD.pin);
  // B-453: die Karte „Heute" kennt das neue Kind sofort, ohne Neuladen.
  await expect(vater.locator("section", { has: vater.getByRole("heading", { name: "Heute" }) }).getByText(CHILD.name)).toBeVisible();

  // ---------- Stoff: Reihe, Unit, Übung aus einer eingefügten Liste ----------
  await vater.goto("/vater/lehrwerke");
  await vater.locator("#ns-name").fill(SERIES);
  await vater.locator("#ns-subject").selectOption({ label: "Französisch" });
  await vater.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(vater.getByText(/steht im Katalog/)).toBeVisible();
  await vater.getByRole("row", { name: new RegExp(SERIES) }).getByRole("button", { name: "Units" }).click();
  await vater.locator('[id^="unit-label-new"]').fill(UNIT);
  await vater.getByRole("button", { name: "Unit hinzufügen" }).click();
  await vater.getByRole("link", { name: `+ Übung zu „${UNIT}"` }).click();
  await vater.locator('select[aria-label="Übungstyp"]').selectOption("Vocabulary");
  await vater.locator("#ex-title").fill(TITLE);
  await vater.locator('select[aria-label="Quellsprache"]').selectOption("fr");
  await vater.locator('select[aria-label="Zielsprache"]').selectOption("de");
  await vater.getByRole("button", { name: "📋 Liste einfügen" }).click();
  await vater.getByLabel("Vokabelliste", { exact: true }).fill(PAIRS.map(([w, t]) => `${w}\t${t}`).join("\n"));
  await vater.getByRole("button", { name: `${PAIRS.length} als fr→de anlegen & wählen` }).click();
  await expect(vater.getByText(`${PAIRS.length} Vokabeln aus der Liste gewählt.`)).toBeVisible();
  await vater.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(vater.getByText(`Übung „${TITLE}" angelegt.`)).toBeVisible();

  // ---------- Pflicht: ein Plan über vier Wochen, eine Position mit Tagesdosis ----------
  await vater.goto("/vater/plan/new");
  await vater.locator("#plan-title").fill(`Vier Wochen ${RUN}`);
  await vater.getByLabel("Dauer (Tage)", { exact: true }).fill("28");
  await vater.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);
  await vater.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Französisch" });
  await vater.locator('select[aria-label="Reihe-Filter"]').selectOption({ label: SERIES });
  // Erst die Dosis, dann die Übung: die Wahl der Übung darf das mitgekommene Leitner-Häkchen nicht zurücknehmen.
  await vater.getByLabel("Neue Inhalte je Tag", { exact: true }).fill(String(DOSE));
  await vater.getByRole("radio", { name: new RegExp(TITLE) }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  const positionRow = vater.getByRole("row", { name: new RegExp(TITLE) });
  await expect(positionRow).toContainText(`${DOSE} neue je Tag`);

  // ---------- Kind, Tag 1: die Portion, nicht die Liste ----------
  const sohn = await (await browser.newContext()).newPage();
  await sohnLogin(sohn, { id: childId, pin: CHILD.pin });
  await sohn.getByRole("link", { name: /ÜBEN/ }).click();
  const counter = sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  for (let i = 0; i < DOSE; i++) {
    // Buchreihenfolge: die ersten vier Wörter der Liste, je mit Lösung.
    await expect(counter).toHaveText(`Karte ${i + 1} / ${DOSE}`);
    await expect(sohn.getByText(PAIRS[i][0], { exact: true })).toBeVisible();
    await expect(sohn.getByText(`→ ${PAIRS[i][1]}`)).toBeVisible();
    await sohn.getByRole("button", { name: "Weiter →" }).click();
  }
  await expect(sohn.getByText(`${DOSE} neue Wörter kennengelernt`)).toBeVisible();

  // Die Startseite sagt, wie weit der Stoff ist – und die zweite Runde bringt keine weiteren neuen Wörter.
  await sohn.goto("/sohn");
  await expect(sohn.getByText(`${DOSE} von ${PAIRS.length} schon kennengelernt`)).toBeVisible();
  await expect(sohn.getByRole("link", { name: new RegExp(`ÜBEN \\(${DOSE} fällig\\)`) })).toBeVisible();

  // ---------- Vater: der Fortschritt steht in der Oberfläche ----------
  await vater.reload();
  await vater.getByRole("row", { name: new RegExp(TITLE) }).getByRole("button", { name: "📊 Report" }).click();
  await expect(vater.getByText(`${DOSE}/${PAIRS.length} eingeführt`, { exact: false })).toBeVisible();
});
