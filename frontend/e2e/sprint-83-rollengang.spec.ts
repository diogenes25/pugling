import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * Rollengang Sprint 83 (B-606, Sicht Sohn): die Startseite liest die Kind-Tagessicht `student/children/{id}/today`
 * statt der Übersicht eines Plans, gliedert nach Fach und führt aus einer Position in den Weg ihres eigenen Plans.
 * Zwei aktive Pläne lassen sich über die API noch nicht anlegen (die Ein-Plan-Sperre fällt erst mit B-539 Teil A);
 * die Zwei-Pläne-Regeln halten `KindTagessichtTests` (Server) und `tagessichtWeg.test.tsx` (Startseite). Hier steht,
 * was nur die laufende App zeigt: mit einem Plan ändert sich für den Sohn nichts außer der Fach-Überschrift, und der
 * Vater liest dieselbe Tagessicht für sein Kind. Die Truhe über die Kind-Sicht geht `truhe.spec.ts` ab.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Die Startseite zeigt die Pflichten nach Fach, und „Los“ führt in den Plan der Position", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8683";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Tagessicht ${RUN}`, pin });
  const FACH = `Tagessicht-Fach ${RUN}`;
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: FACH });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Tagessicht-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const TITEL = `Tiere ${RUN}`;
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: TITEL, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `dog ${RUN}`, back: "Hund" }] },
  });
  const PLAN = `Tagessicht-Plan ${RUN}`;
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, title: PLAN, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  // Der Vater liest die Tagessicht seines Kindes: eine Position mit Plan, Plantitel und Fach, Pflicht offen.
  const sicht = await request.get(`/api/v1/student/children/${childId}/today`, { headers: vater });
  expect(sicht.ok(), await sicht.text()).toBeTruthy();
  const heute = await sicht.json();
  expect(heute.dutyDone).toBe(false);
  expect(heute.positions).toHaveLength(1);
  expect(heute.positions[0]).toMatchObject({ positionId, planId, planTitle: PLAN, subjectName: FACH });

  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByRole("heading", { level: 1, name: "Tagesmission" })).toBeVisible();
  await expect(page.getByText(PLAN)).toBeVisible();
  // Je Fach ein Abschnitt, benannt nach seiner Überschrift.
  const abschnitt = page.getByRole("region", { name: FACH });
  await expect(abschnitt.getByRole("heading", { level: 2, name: FACH })).toBeVisible();
  await expect(abschnitt.locator(".pfad-knoten")).toHaveCount(1);
  await expect(page.locator(".pfad-start")).toHaveCount(1);

  // „Los“ führt in die Position – und das Üben findet sie im gemerkten Plan, also ohne Rücksprung auf die Startseite.
  await abschnitt.locator(".pfad-zeile", { hasText: TITEL }).locator(".pfad-knoten").click();
  await page.getByRole("dialog", { name: TITEL }).getByRole("link", { name: "▶ LOS", exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`/sohn/practice/${positionId}$`));
  // Die Übungsseite nennt keinen Titel; ihr Inhalt ist die Karte der Position – das einzige Wort dieser Übung.
  await expect(page.getByRole("heading", { level: 1, name: "Neues Wort – lerne es" })).toBeVisible();
  await expect(page.getByText("Hund", { exact: true })).toBeVisible();
  await expect(page).toHaveURL(new RegExp(`/sohn/practice/${positionId}$`));
});
