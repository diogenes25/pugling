import { test, expect } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-341. Die Schlagwort-Verwaltung schickt beim Speichern immer das Label mit. Seit der Server prüft, ob ein anderes
 * Schlagwort dieses Label als Synonym führt, ließ sich ein Schlagwort nicht mehr speichern, sobald ein anderes sein Label
 * später als Synonym aufgenommen hatte – auch keine reine Facetten-Änderung.
 *
 * Die beiden Schlagwörter entstehen über die API (Gegenstand ist das Speichern in der Verwaltung, nicht das Anlegen).
 * Der Beleg ist der Stand nach dem Neuladen, nicht die Meldung: „Gespeichert." allein sähe auch ein Formular, das gar
 * nicht geschickt hat.
 */

const RUN = Date.now().toString(36).slice(-6);
const LABEL = `Kicker ${RUN}`;

test("Ein Schlagwort bleibt speicherbar, wenn ein anderes sein Label als Synonym führt", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  const create = async (label: string) => {
    const res = await request.post("/api/v1/creator/interest-tags", { headers, data: { label, facet: "Other" } });
    expect(res.ok(), await res.text()).toBeTruthy();
    return (await res.json()).id as number;
  };
  await create(LABEL);
  const other = await create(`Fussball ${RUN}`);
  const synonym = await request.patch(`/api/v1/creator/interest-tags/${other}`, {
    headers, data: { synonyms: [LABEL.toLowerCase()] },
  });
  expect(synonym.ok(), await synonym.text()).toBeTruthy();

  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/media");
  await page.getByRole("button", { name: /Schlagwort-Vokabular verwalten/ }).click();
  await page.getByLabel("Schlagwort suchen").fill(LABEL);
  await page.getByLabel("Schlagwort suchen").press("Enter");

  await page.getByLabel(`Facette von ${LABEL}`, { exact: true }).selectOption("Sport");
  await page.getByRole("button", { name: `Sport „${LABEL}" speichern` }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  await page.reload();
  await page.getByRole("button", { name: /Schlagwort-Vokabular verwalten/ }).click();
  await page.getByLabel("Schlagwort suchen").fill(LABEL);
  await page.getByLabel("Schlagwort suchen").press("Enter");
  await expect(page.getByLabel(`Facette von ${LABEL}`, { exact: true })).toHaveValue("Sport");
});
