import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * Sprint 77 (Rollengang Vater): B-547 und B-552 an der laufenden App.
 *
 * B-547: der Vater setzt „Inhalte“ auf 5, speichert, öffnet die Zeile wieder, leert das Feld und speichert erneut – danach
 * steht „5 Inhalte“ nicht mehr in der Zeile. Rot, wenn der Schalter `clearItemCount` fehlt oder die Zeile ihre Merkwerte
 * nicht frisch übernimmt (Review-Fund: der zweite Durchgang schickte keinen Schalter).
 *
 * B-552: „Nur getippte Tests zählen“ auf einer Position ohne getippte Stufe wird beim Speichern abgelehnt, die Meldung ist
 * sichtbar, und die Zeile bleibt unverändert. Rot ohne die Server-Prüfung (dann „Position gespeichert.“).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

test("Der Vater setzt Inhalte zurück auf Standard und kann keine unerreichbare Pflicht speichern", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  // Ein eigenes Kind: ein Plan am geseedeten Kind könnte dessen aktiven Plan berühren.
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Standard ${RUN}`, pin: "8655" });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Standardfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, { name: `Standardreihe ${RUN}`, subjectId });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: "Unit 1", orderIndex: 1 });
  const titel = `Vokabeln Standard ${RUN}`;
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: titel, orderIndex: 1, rewardPoints: 10,
    config: {
      direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [{ front: "dog", back: "Hund" }, { front: "cat", back: "Katze" }, { front: "cow", back: "Kuh" },
        { front: "pig", back: "Schwein" }, { front: "hen", back: "Huhn" }, { front: "fox", back: "Fuchs" }],
    },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, title: `Standardplan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater, { exerciseId, cadence: "Daily" });

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/plan/${planId}`);
  const zeile = page.getByRole("row", { name: new RegExp(titel) });
  // Die Zeile im Bearbeiten-Modus ist die mit „OK“ – das Anlege-Formular trägt dieselben Felder.
  const bearbeiten = page.getByRole("row").filter({ has: page.getByRole("button", { name: "OK" }) });
  const inhalte = bearbeiten.getByRole("spinbutton", { name: "Anzahl Inhalte" });

  // B-547: setzen …
  await zeile.getByRole("button", { name: "Bearbeiten" }).click();
  await inhalte.fill("5");
  await page.getByRole("button", { name: "OK" }).click();
  await expect(page.getByText("Position gespeichert.")).toBeVisible();
  await expect(zeile).toContainText("5 Inhalte");

  // … und im zweiten Durchgang zurück auf „alle“.
  await zeile.getByRole("button", { name: "Bearbeiten" }).click();
  await expect(inhalte).toHaveValue("5");
  await inhalte.fill("");
  await page.getByRole("button", { name: "OK" }).click();
  await expect(page.getByText("Position gespeichert.")).toBeVisible();
  await expect(zeile).not.toContainText("Inhalte");

  // B-552: Häkchen an, Stufe bleibt „Standard der Übung“ (Vokabel-Vorgabe: Selbsteinschätzung) → abgelehnt.
  await zeile.getByRole("button", { name: "Bearbeiten" }).click();
  await bearbeiten.getByRole("checkbox", { name: "nur getippte Tests" }).check();
  await page.getByRole("button", { name: "OK" }).click();
  await expect(page.getByText("„Nur getippte Tests zählen“ braucht eine getippte Stufe – auf Selbsteinschätzung wäre die Pflicht nie erreichbar.")).toBeVisible();
  await page.getByRole("button", { name: "Abbrechen" }).click();
  await expect(zeile).not.toContainText("Ziel nicht erreichbar");
});
