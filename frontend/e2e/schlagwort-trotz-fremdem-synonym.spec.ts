import { test, expect } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-341. Die Schlagwort-Verwaltung schickt beim Speichern immer das Label mit. Der Server prüfte es jedes Mal neu – auch
 * unverändert –, und ein Schlagwort ließ sich nicht mehr speichern, sobald ein anderes inzwischen etwas trug, womit sein
 * Label kollidiert: dessen Synonym oder den Slug, zu dem das Label heute führt.
 *
 * Gefahren wird der Slug-Fall, weil die API ihn noch herstellen kann: ein umbenanntes Schlagwort behält seinen alten Slug,
 * und ein anderes darf mit ausdrücklichem Slug den Slug seines heutigen Labels tragen. Den Synonym-Fall lässt die API seit
 * B-351 nicht mehr entstehen (nur noch Altdaten); ihn hält `InterestTagLabelTests` über den DbContext.
 *
 * Die Schlagwörter entstehen über die API (Gegenstand ist das Speichern in der Verwaltung, nicht das Anlegen). Der Beleg
 * ist der Stand nach dem Neuladen, nicht die Meldung: „Gespeichert." allein sähe auch ein Formular, das gar nicht geschickt
 * hat.
 */

const RUN = Date.now().toString(36).slice(-6);
const LABEL = `Kicker ${RUN}`;

test("Ein umbenanntes Schlagwort bleibt speicherbar, wenn ein anderes den Slug seines Labels trägt", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  const create = async (data: Record<string, unknown>) => {
    const res = await request.post("/api/v1/creator/interest-tags", { headers, data: { facet: "Other", ...data } });
    expect(res.ok(), await res.text()).toBeTruthy();
    return (await res.json()) as { id: number };
  };
  // Erst unter anderem Namen anlegen, dann umbenennen: der Slug bleibt der alte.
  const { id } = await create({ label: `Fussball ${RUN}` });
  const renamed = await request.patch(`/api/v1/creator/interest-tags/${id}`, { headers, data: { label: LABEL } });
  expect(renamed.ok(), await renamed.text()).toBeTruthy();
  // Ein zweites Schlagwort trägt ausdrücklich den Slug, zu dem „Kicker …" heute führen würde.
  await create({ label: `Golf ${RUN}`, slug: `kicker-${RUN}` });

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
