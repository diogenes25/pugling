import { test, expect } from "@playwright/test";
import { vaterLogin, sohnLogin } from "./helpers";

/*
 * B-62 (Punkt 3): "Runde beenden" war bisher nur auf Sichtbarkeit geprüft (`full-flow.spec.ts`), nie
 * tatsächlich geklickt - ein Klick hätte den restlichen Durchstich dort abgeschnitten. Dieses eigene,
 * kurze Spec holt den fehlenden Klick nach.
 *
 * Der Knopf selbst ruft `end` nicht auf (siehe Kommentar in `SohnPractice.tsx`): er navigiert nur zur
 * Basis, und das Cleanup des Heartbeat-Effekts beim Unmount schickt die Rest-Sekunden und schließt die
 * Sitzung. Geprüft wird darum nicht die Sitzungs-Id (`practice-sessions`'s `Start` legt bei JEDEM Eintritt
 * ohnehin eine neue Zeile an, mit oder ohne Fix - Üben kennt kein Resume wie die Klausur), sondern die
 * einzig echte Serverseite des Ausstiegs: `EndedAt` der verlassenen Sitzung ist nach dem Klick gesetzt.
 */

const FATHER = { id: "1", pin: "0000" };
const CHILD = { id: "1", pin: "1111" };
const EXERCISE = "Vokabeln: En ville";

test("Runde beenden schließt die Sitzung serverseitig (EndedAt gesetzt)", async ({ browser }) => {
  // ---------- VATER: Plan mit Position anlegen ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);

  await vater.getByRole("link", { name: /Zuweisen/ }).first().click();
  await expect(vater).toHaveURL(/\/vater\/plaene$/);
  await vater.getByRole("link", { name: /Neuer Plan/ }).click();
  await expect(vater.getByRole("heading", { name: /Neuer Lehrplan/ })).toBeVisible();
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(CHILD.id);
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);

  const exRadio = vater.getByRole("radio", { name: new RegExp(EXERCISE) });
  await expect(exRadio).toBeVisible();
  await exRadio.check();
  await vater.locator('select[aria-label="Ziel-Rhythmus"]').selectOption("Daily");
  await vater.getByRole("checkbox", { name: /Leitner/ }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  await expect(vater.getByRole("row", { name: new RegExp(EXERCISE) })).toBeVisible();

  // ---------- SOHN: Runde starten und wieder verlassen ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, CHILD);

  // planId/positionId kommen aus der abgefangenen Start-Anfrage selbst, nicht aus der Sohn-URL: die Route
  // dort ist nur `/sohn/practice/:positionId`, `planId` steht ausschließlich im Kontext (`useSohn`).
  let sessionId: number | null = null;
  let planId: string | null = null;
  let positionId: string | null = null;
  sohn.on("response", async (res) => {
    const match = res.url().match(/study-plans\/(\d+)\/positions\/(\d+)\/practice-sessions$/);
    if (res.request().method() === "POST" && match && res.ok()) {
      [, planId, positionId] = match;
      sessionId = (await res.json()).id;
    }
  });

  await sohn.locator(".card", { hasText: EXERCISE }).getByRole("link", { name: /ÜBEN/ }).click();
  await expect(sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ })).toBeVisible();
  await expect.poll(() => sessionId).not.toBeNull();

  await sohn.getByRole("button", { name: "Runde beenden" }).click();
  await expect(sohn.getByText("Tagesmission")).toBeVisible();

  // Direkte Serverprobe statt einer Sicht-Prüfung: `EndedAt` ist die einzig echte Aussage, dass die
  // Sitzung wirklich beendet wurde (das UI selbst zeigt nach dem Verlassen nichts davon).
  const token = await sohn.evaluate(() => localStorage.getItem("pugling.token"));
  await expect.poll(async () => {
    const res = await sohn.request.get(
      `/api/v1/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}`,
      { headers: { Authorization: `Bearer ${token}` } });
    return (await res.json()).endedAt;
  }).not.toBeNull();

  await vaterCtx.close();
  await sohnCtx.close();
});
