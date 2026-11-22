import { test, expect, type APIRequestContext } from "@playwright/test";
import { pfadStart, sohnLogin } from "./helpers";

/*
 * B-660 – Rollengang Sprint B3 als Sohn: eine erledigte Pflicht führt im Üben nicht ins Leere. Nach dem bestandenen Test
 * ist nichts mehr fällig; der Leerzustand sagt, was die Übung im Bonus-Training heute noch wert ist, und führt genau dort
 * hinein. Die Regeln (Wert je Runde, Deckel, Sperre) hält der Server (`ExtraTrainingTests`); die Spec belegt den Weg vom
 * Knoten bis in die Runde. Rot, wenn der Leerzustand nur „Nichts fällig“ zeigt oder der Knopf in eine andere Übung führt.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"], ["house", "Haus"], ["tree", "Baum"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Nichts fällig führt weiter: nach erfüllter Pflicht geht es aus dem Üben ins Bonus-Training mit dieser Übung", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8660";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Nochmal ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Nochmal-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Nochmal-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const VOKABELN = `Tiere ${RUN}`, RECHNEN = `Rechnen ${RUN}`;
  const exVokabeln = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: VOKABELN, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  await postId(request, `${units}/${unitId}/arithmetic`, vater, {
    title: RECHNEN, orderIndex: 2, rewardPoints: 10,
    config: { problems: [{ prompt: "2 + 3", answer: 5, tolerance: 0 }, { prompt: "4 × 4", answer: 16, tolerance: 0 }] },
  });

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, subjectId, title: `Nochmal-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId: exVokabeln, stage: 4, cadence: "Daily", useLeitner: false });

  // „Nichts fällig“ selbst ist alter Server-Stand (Leitner-Fälligkeit); ihn über die Oberfläche herzustellen hieße, alle
  // Karten einer Runde zu spielen. Darum antwortet nur die Kartenliste dieser Position leer – Übersicht, Bonus-Training und
  // die Runde dahinter bleiben echt. Der Zähler sichert, dass die Attrappe wirklich griff.
  let leer = 0;
  const kartenListe = new RegExp(`/positions/${positionId}/practice-sessions/[0-9]+/cards$`);
  await page.route((u) => kartenListe.test(u.pathname), (route) => {
    leer++;
    return route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
  });

  // Vor der Pflicht: der Leerzustand sagt, wie viele Tagesziele noch fehlen, und bietet keine bezahlte Runde an.
  await sohnLogin(page, { id: String(childId), pin });
  await pfadStart(page, "practice", VOKABELN);
  await expect(page.getByText("Nichts fällig 🎉")).toBeVisible();
  await expect(page.getByText("Noch 1 Tagesziel offen – danach kannst du Übungen im Bonus-Training für Münzen wiederholen.")).toBeVisible();
  await expect(page.getByRole("link", { name: "🔁 Noch einmal im Bonus-Training" })).toHaveCount(0);

  // Die Pflicht über die API erfüllen: ein bestandener Test der Position.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    // B-597: Freitext fragt das Fremdwort ab, die Vorderseite.
    headers: kind, data: { answers: WORDS.map(([front], itemIndex) => ({ itemIndex, givenAnswer: front })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  // Nach der Pflicht: noch einmal, für Münzen, in genau dieser Übung (der Vorrat trägt auch die Rechen-Übung).
  await page.reload();
  await expect(page.getByText("Nichts fällig 🎉")).toBeVisible();
  await expect(page.getByText("im Bonus-Training ist diese Übung heute noch 100 % wert", { exact: false })).toBeVisible();
  expect(leer).toBeGreaterThan(1);
  await page.getByRole("link", { name: "🔁 Noch einmal im Bonus-Training" }).click();
  await expect(page).toHaveURL(new RegExp(`/sohn/bonus/${exVokabeln}$`));
  // Die Runde läuft in den Vokabeln (vier Karten, ein Wort der Liste) – die Rechen-Übung hätte zwei Aufgaben.
  await expect(page.getByText("Karte 1 / 4")).toBeVisible();
  await expect(page.getByRole("status").filter({ hasText: /Neue Aufgabe: (dog|cat|house|tree)$/ })).toHaveCount(1);
});
