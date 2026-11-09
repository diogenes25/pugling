import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-424: Der Vater sieht in der Übungsauswahl – im Assistenten wie im Planbau –, wie groß eine Übung ist, und kann sie
 * ausprobieren, bevor er sein Kind darauf verpflichtet. Rollengang aus der Vater-Rolle mit einer eigenen Übung in einem
 * eigenen Fach (drei Vokabeln), damit die Zahl in der Zeile eine bekannte ist.
 *
 * Rot, wenn eine der Zeilen den Umfang nicht nennt, „Ausprobieren" den Testmodus nicht öffnet – oder ihn öffnet und dabei
 * die Auswahl verändert (der Knopf säße dann im Label des Kästchens).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function api(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Übungsauswahl nennt den Umfang und lässt ausprobieren – im Assistenten und im Planbau", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const fach = `Umfangfach ${RUN}`;
  const titel = `Drei Wörter ${RUN}`;
  const subjectId = (await api(page, "/api/v1/creator/subjects", { name: fach })).id;
  const seriesId = (await api(page, "/api/v1/creator/textbook-series", { name: `Umfangreihe ${RUN}`, subjectId })).id;
  const unitId = (await api(page, `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  await api(page, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, {
    title: titel, orderIndex: 1, rewardPoints: 10,
    config: {
      direction: "front-to-back", sourceLang: "fr", targetLang: "de",
      items: [{ front: "la ville", back: "die Stadt" }, { front: "la rue", back: "die Straße" }, { front: "le pont", back: "die Brücke" }],
    },
  });

  // ---------- Assistent ----------
  await page.goto("/vater/wizard");
  await page.getByRole("radio", { name: "Bestehendes Kind" }).click();
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: "Weiter" }).click();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: fach });
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Übungen wählen/ })).toBeVisible();

  const kaestchen = page.getByRole("checkbox", { name: new RegExp(`${titel}.*3 Karten`) });
  await expect(kaestchen).toBeEnabled();
  // Der Knopf gehört nicht zum Namen des Kästchens – säße er im Label, hinge sein „ausprobieren" daran.
  await expect(kaestchen).not.toHaveAccessibleName(/ausprobieren/);
  await kaestchen.check();
  await page.getByRole("button", { name: `„${titel}" ausprobieren` }).click();
  const dialog = page.getByRole("dialog", { name: `Testmodus: ${titel}` });
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: "Schließen" }).click();
  await expect(dialog).toHaveCount(0);
  // Ausprobieren ist keine Wahl: das Kästchen steht wie vorher.
  await expect(kaestchen).toBeChecked();

  // ---------- Planbau ----------
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Umfangplan ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: fach });

  const radio = page.getByRole("radio", { name: new RegExp(`${titel}.*3 Karten`) });
  await expect(radio).toBeEnabled();
  await expect(radio).not.toHaveAccessibleName(/ausprobieren/);
  // Erst wählen, dann ausprobieren: nur so kann die Zusicherung unten an einer Änderung fallen (Review-Fund).
  await radio.check();
  await page.getByRole("button", { name: `„${titel}" ausprobieren` }).click();
  await expect(page.getByRole("dialog", { name: `Testmodus: ${titel}` })).toBeVisible();
  await page.getByRole("dialog").getByRole("button", { name: "Schließen" }).click();
  await expect(radio).toBeChecked();
});
