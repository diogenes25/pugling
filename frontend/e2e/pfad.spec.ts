import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-525 – die Startseite des Sohns ist ein Lernpfad. Die Regeln (nächster Knoten, Frist, Startweg) prüfen `pfad.test.ts`
 * und `SohnHome.test.tsx`; hier liegt, was nur die laufende App zeigt: dass der Server Malus und Unit wirklich
 * liefert, dass der Pfad sie in seiner Reihenfolge zeichnet und dass ein bestandener Test den Knoten abhakt und
 * „START" weiterrückt.
 *
 * Stoff über die API: drei Positionen in zwei Units – ein Tagesziel mit Malus, ein Wochenziel ohne, eine freie.
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Der Pfad gliedert nach Unit, nennt Frist und Malus, und ein bestandener Test rückt START weiter", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8652";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Pfad ${RUN}`, pin });
  // Eine Mission, die der Test nicht erfüllt: ein frisches Kind hat keine, und das Panel rendert dann gar nicht –
  // „steht nicht auf der Startseite“ wäre ohne sie eine Zusicherung, die nie fallen kann.
  const MISSION = `Pfad-Mission ${RUN}`;
  await postId(request, `/api/v1/supervisor/children/${childId}/missions`, vater,
    { title: MISSION, metric: "MinutesPracticed", target: 99, period: "Daily", rewardPoints: 5 });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Pfad-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Pfad-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitA = await postId(request, units, vater, { label: `Unit A ${RUN}`, orderIndex: 1 });
  const unitB = await postId(request, units, vater, { label: `Unit B ${RUN}`, orderIndex: 2 });
  const vokabeln = (unitId: number, title: string, front: string, back: string) =>
    postId(request, `${units}/${unitId}/vocabulary`, vater, {
      title, orderIndex: 1, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front, back }] },
    });
  const TAG = `Tagesziel ${RUN}`, WOCHE = `Wochenziel ${RUN}`, FREI = `Frei ${RUN}`;
  const exTag = await vokabeln(unitA, TAG, `tree ${RUN}`, "Baum");
  const exWoche = await vokabeln(unitB, WOCHE, `house ${RUN}`, "Haus");
  const exFrei = await vokabeln(unitB, FREI, `dog ${RUN}`, "Hund");

  const PLAN = `Pfad-Plan ${RUN}`;
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, subjectId, title: PLAN, durationDays: 7, active: true });
  const positions = `/api/v1/supervisor/study-plans/${planId}/positions`;
  // Stufe 4 (Freitext) ohne Karteikasten: prüfbar, und der Knoten führt zuerst ins Üben (B-454).
  const posTag = await postId(request, positions, vater, { exerciseId: exTag, stage: 4, cadence: "Daily", useLeitner: false, penaltyCoins: 5 });
  await postId(request, positions, vater, { exerciseId: exWoche, stage: 4, cadence: "Weekly", useLeitner: false, penaltyCoins: 0 });
  await postId(request, positions, vater, { exerciseId: exFrei, stage: 4, cadence: "None", useLeitner: false });

  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByText(PLAN)).toBeVisible();

  // Zwei Abschnitte in Plan-Reihenfolge, je unter dem Titel ihrer Unit; die freie Position steht nicht im
  // Pflicht-Pfad, sondern zugeklappt unter den Zusatzübungen des Fachs.
  const abschnittA = page.getByRole("region", { name: `Unit A ${RUN}` });
  const abschnittB = page.getByRole("region", { name: `Unit B ${RUN}` });
  await expect(abschnittA.locator(".pfad-knoten")).toHaveCount(1);
  await expect(abschnittB.locator(".pfad-knoten")).toHaveCount(1);
  await expect(page.locator(".pfad-zeile", { hasText: FREI })).toHaveCount(0);
  await page.getByRole("button", { name: /1 Zusatzübung/ }).click();

  const knoten = (titel: string) => page.locator(".pfad-zeile", { hasText: titel }).locator(".pfad-knoten");
  // Genau ein START, und er steht am offenen Tagesziel.
  await expect(page.locator(".pfad-start")).toHaveCount(1);
  await expect(knoten(TAG)).toHaveClass(/naechste/);
  await expect(knoten(TAG)).toHaveAccessibleName(/als Nächstes dran/);
  // Die Missionen stehen nicht mehr auf der Startseite, sondern unter „Erfolge“ (geprüft weiter unten). Erst wenn
  // nichts mehr lädt: ein Panel, das nur noch nicht da ist, sähe sonst aus wie eines, das fehlt.
  await page.waitForLoadState("networkidle");
  await expect(page.getByText(MISSION)).toHaveCount(0);
  // Frist nach Server-Uhr und der Malus als Betrag; ohne Malus kein Betrag; frei ohne Frist.
  await expect(knoten(TAG)).toHaveAccessibleName(/noch \d+ Std\., sonst −5 Münzen/);
  await expect(knoten(WOCHE)).toHaveAccessibleName(/bis Sonntag|noch \d+ Std\./);
  await expect(knoten(WOCHE)).not.toHaveAccessibleName(/sonst/);
  await expect(knoten(FREI)).not.toHaveAccessibleName(/Std\.|Sonntag/);
  // … und sichtbar unter dem Knoten, nicht nur im Vorlesenamen.
  await expect(page.locator(".pfad-zeile", { hasText: TAG }).locator(".pfad-frist")).toHaveText(/noch \d+ Std\. · sonst −5 Münzen/);
  await expect(page.locator(".pfad-zeile", { hasText: FREI }).locator(".pfad-frist")).toHaveCount(0);

  // B-686: kein Blatt mehr – der Knoten selbst startet ins Üben, obwohl die Position heute prüfbar ist.
  await expect(knoten(TAG)).toHaveAttribute("href", `/sohn/practice/${posTag}`);

  // Der Test des Tagesziels wird bestanden (über die API – den Klickweg trägt full-flow.spec.ts).
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${posTag}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, { headers: kind, data: { answers: [{ itemIndex: 0, givenAnswer: `tree ${RUN}` }] } }); // B-597: das Fremdwort
  expect(submit.ok(), await submit.text()).toBeTruthy();
  expect((await submit.json()).passed).toBe(true);

  // Zurück auf dem Pfad: das Tagesziel ist abgehakt, START steht jetzt am Wochenziel.
  await page.locator("nav.sohn-nav").getByRole("link", { name: "Erfolge", exact: true }).click();
  await expect(page.getByText(MISSION)).toBeVisible();
  await expect(page.getByText(TAG)).toHaveCount(0);
  await page.locator("nav.sohn-nav").getByRole("link", { name: "Pfad", exact: true }).click();
  await expect(knoten(TAG)).toHaveAccessibleName(/Ziel erfüllt/);
  await expect(knoten(TAG)).not.toHaveAccessibleName(/Std\./);
  await expect(page.locator(".pfad-start")).toHaveCount(1);
  await expect(knoten(WOCHE)).toHaveClass(/naechste/);
});
