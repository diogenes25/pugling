import { test, expect } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-485. Ein Interesse bekommt nur Vokabelbilder, wenn es einen markenneutralen Bild-Prompt trägt; gepflegt wird er in
 * der Schlagwort-Verwaltung. Gefahren werden beide Richtungen: setzen und wieder leeren. Das Leeren ist der heikle Teil –
 * ein leeres Feld als `null` geschickt hieße nach PATCH-Semantik „unverändert", und der alte Prompt stünde nach dem
 * Neuladen wieder da. Beleg ist darum der Stand nach dem Neuladen, nicht die Meldung.
 */

const RUN = Date.now().toString(36).slice(-6);
const LABEL = `Bildprompt ${RUN}`;
const PROMPT = "in a blocky voxel world";

async function openTag(page: import("@playwright/test").Page) {
  await page.getByRole("button", { name: /Schlagwort-Vokabular verwalten/ }).click();
  await page.getByLabel("Schlagwort suchen").fill(LABEL);
  await page.getByLabel("Schlagwort suchen").press("Enter");
  return page.getByLabel(`Bild-Prompt von ${LABEL}`, { exact: true });
}

test("Der Bild-Prompt eines Interesses lässt sich setzen und wieder leeren", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const res = await request.post("/api/v1/creator/interest-tags", {
    headers: { Authorization: `Bearer ${(await login.json()).token}` },
    data: { label: LABEL, facet: "Franchise" },
  });
  expect(res.ok(), await res.text()).toBeTruthy();

  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/media");

  let field = await openTag(page);
  await expect(field).toHaveValue("");
  await field.fill(PROMPT);
  await page.getByRole("button", { name: `Marke/Serie „${LABEL}" speichern` }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  await page.reload();
  field = await openTag(page);
  await expect(field).toHaveValue(PROMPT);

  await field.fill("");
  await page.getByRole("button", { name: `Marke/Serie „${LABEL}" speichern` }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  await page.reload();
  field = await openTag(page);
  await expect(field).toHaveValue("");
});

/*
 * Der Herkunftsfilter der Bild-Bibliothek ist der Nachsorge-Weg für KI-Bilder (B-485, Entscheidung 2). Ein vertippter
 * Query-Parameter fiele nicht als `unknown_field` auf, sondern würde still ignoriert – die Liste zeigte dann alles.
 */
test("Die Bild-Bibliothek filtert auf KI-erzeugte Bilder", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  for (const [key, origin] of [[`ki-${RUN}`, "Generated"], [`hand-${RUN}`, "Upload"]] as const) {
    const res = await request.post("/api/v1/creator/media", {
      headers,
      data: {
        key, description: `Motiv ${key}`, origin, source: origin === "Generated" ? "gpt-image-1-mini: a dog; neutral" : null,
        variants: [{ purpose: "Card", url: `https://cdn.test/${key}.webp`, width: 512, height: 512 }],
      },
    });
    expect(res.ok(), await res.text()).toBeTruthy();
  }

  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/media");
  await page.getByLabel("Bilder durchsuchen").fill(RUN);
  await page.getByLabel("Bilder durchsuchen").press("Enter");
  await expect(page.getByText(`Motiv hand-${RUN}`)).toBeVisible();

  await page.getByLabel("nur KI-erzeugte").check();
  await expect(page.getByText(`Motiv hand-${RUN}`)).toHaveCount(0);
  await expect(page.getByText(`Motiv ki-${RUN}`)).toBeVisible();
  await expect(page.getByText("gpt-image-1-mini: a dog; neutral")).toBeVisible();
});
