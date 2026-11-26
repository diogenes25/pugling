import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-731 (Rollengang Vater, AK 8): der Vater trägt im Kind-Hub unter „Lerntage“ (B-1020: eigener Reiter, damit je Fläche
 * nur ein „Speichern“ steht) eine Pause für heute ein und setzt die Leitplanken des Tagesplaners; nach dem Neuladen der
 * Seite stehen beide so da, wie der Server sie gespeichert hat.
 * Dass Sonntag und Pause weder Serie noch Malus kosten, rechnet der Server an vergangenen Tagen – das belegen die
 * Integrationstests (`LerntageTests`); die API spielt nur heute.
 *
 * Rot, wenn die Maske die Pause nicht an den Server schickt, die Liste nicht vom Server liest oder die Leitplanken nach
 * dem Neuladen nicht den gespeicherten Stand zeigen – und (B-1020) wenn das Neuladen des Kindes nach dem Speichern die
 * Bestätigung wegnimmt: die Zusicherung wartet erst auf das `GET` des Kindes, sonst sähe sie den Banner davor.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

test("Der Vater trägt eine Pause und die Leitplanken ein und liest sie zurück", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };
  const name = `Pause ${RUN}`;
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name, pin: "7" + String(Date.now() % 1000).padStart(3, "0") });

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/kind/${childId}?bereich=lerntage`);

  await expect(page.getByText("Keine Pause eingetragen.")).toBeVisible();
  await page.getByRole("textbox", { name: "Notiz (optional)" }).fill("krank");
  await page.getByRole("button", { name: "Pause eintragen" }).click();
  const pausen = page.getByRole("list", { name: "Eingetragene Pausen" });
  await expect(pausen).toContainText("krank");

  const felder: [string, string][] = [
    ["Pflichtaufgaben am Tag – mindestens", "2"], ["Pflichtaufgaben am Tag – höchstens", "5"],
    ["Neue Wörter am Tag – mindestens", "3"], ["Neue Wörter am Tag – höchstens", "8"],
    ["Malus je verpasster Pflichtaufgabe (🪙)", "4"],
  ];
  for (const [feld, wert] of felder) await page.getByLabel(feld, { exact: true }).fill(wert);
  const neuGeladen = page.waitForResponse((r) => r.request().method() === "GET"
    && new URL(r.url()).pathname === `/api/v1/supervisor/children/${childId}`);
  await page.getByRole("button", { name: "Speichern", exact: true }).click();
  await neuGeladen;
  await expect(page.getByText("Leitplanken gespeichert.")).toBeVisible();
  for (const [feld, wert] of felder) await expect(page.getByLabel(feld, { exact: true })).toHaveValue(wert);

  await page.reload();
  await expect(page.getByRole("list", { name: "Eingetragene Pausen" })).toContainText("krank");
  for (const [feld, wert] of felder) await expect(page.getByLabel(feld, { exact: true })).toHaveValue(wert);

  // Der Server hat, was die Maske zeigt.
  const kind = await request.get(`/api/v1/supervisor/children/${childId}`, { headers: vater });
  expect(await kind.json()).toMatchObject({ dailyTasksMin: 2, dailyTasksMax: 5, dailyNewWordsMin: 3, dailyNewWordsMax: 8, penaltyCoinsPerMissedTask: 4 });
});
