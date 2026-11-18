import { test, expect, type APIRequestContext, type Page } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

/*
 * Rollengang Sprint 84 (B-539 Teil A mit B-606): der Vater legt im Plan-Formular für dasselbe Kind einen Plan je Fach an,
 * und keiner schaltet den anderen ab; erst ein zweiter Plan desselben Fachs löst den ersten ab. Der Sohn sieht beide
 * Pflichten nach Fach getrennt, und die Tagestruhe gibt es erst, wenn beide erfüllt sind (B-606, AK 1/2).
 * Die Regeln selbst halten `PlanSubjectTests` und `KindTagessichtTests`; hier steht der Weg durch die laufende App.
 */

const RUN = Date.now().toString(36).slice(-5);
const PIN = "8684";

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

/** Ein Fach mit Reihe, Unit und einer Vokabelübung über ein Wort – über die API, mit dem Token des angemeldeten Vaters. */
async function fachMitUebung(request: APIRequestContext, vater: Record<string, string>, fach: string, front: string, back: string) {
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: fach });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `${fach} Reihe`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `${fach} Unit`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `${fach} Wort`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front, back }] },
  });
  return { exerciseId, front };
}

/** Legt im Plan-Formular einen Plan an und gibt seine Id aus der Ziel-URL zurück. */
async function planAnlegen(page: Page, title: string, childId: string, fach: string): Promise<number> {
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(title);
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("combobox", { name: "Fach (Pflicht)" }).selectOption({ label: fach });
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  return Number(/\/vater\/plan\/(\d+)$/.exec(page.url())![1]);
}

test("Ein Plan je Fach: zwei Fächer bleiben aktiv, der Sohn sieht beide, die Truhe kommt erst nach beiden", async ({ page, request }) => {
  await vaterLogin(page, { id: "1", pin: "0000" });
  const vater = { Authorization: `Bearer ${await page.evaluate(() => localStorage.getItem("pugling.token"))}` };
  const childId = await createChild(page, `Zwei Fächer ${RUN}`, PIN);

  const ENGLISCH = `Englisch-${RUN}`, FRANZ = `Französisch-${RUN}`;
  const en = await fachMitUebung(request, vater, ENGLISCH, `dog ${RUN}`, "Hund");
  const fr = await fachMitUebung(request, vater, FRANZ, `chat ${RUN}`, "Katze");

  // Der Vater legt je Fach einen Plan an – im Formular, wie er es täte.
  const PLAN_EN = `Plan EN ${RUN}`, PLAN_FR = `Plan FR ${RUN}`;
  const planEn = await planAnlegen(page, PLAN_EN, childId, ENGLISCH);
  const planFr = await planAnlegen(page, PLAN_FR, childId, FRANZ);
  const posEn = await postId(request, `/api/v1/supervisor/study-plans/${planEn}/positions`, vater,
    { exerciseId: en.exerciseId, stage: 4, cadence: "Daily", useLeitner: false });
  const posFr = await postId(request, `/api/v1/supervisor/study-plans/${planFr}/positions`, vater,
    { exerciseId: fr.exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  // Beide aktiv, keiner „mehrfach aktiv“: der zweite Plan hat den ersten nicht abgeschaltet.
  await page.goto(`/vater/plaene?childId=${childId}`);
  const zeile = (titel: string) => page.getByRole("row", { name: new RegExp(titel) });
  await expect(zeile(PLAN_EN)).toContainText("aktiv");
  await expect(zeile(PLAN_EN)).not.toContainText("inaktiv");
  await expect(zeile(PLAN_FR)).not.toContainText("inaktiv");
  await expect(page.getByText("mehrfach aktiv")).toHaveCount(0);

  // Der Sohn sieht beide Pflichten, je Fach ein Abschnitt; die Truhe ist zu.
  await sohnLogin(page, { id: childId, pin: PIN });
  // Je Fach ein aufklappbarer Reiter: offen steht nur das Fach mit dem START, das andere klappt das Kind auf.
  await expect(page.getByRole("region", { name: ENGLISCH }).locator(".pfad-knoten")).toHaveCount(1);
  const franz = page.getByRole("region", { name: FRANZ });
  await expect(franz.locator(".pfad-knoten")).toHaveCount(0);
  await expect(franz.getByRole("heading")).toContainText("Heute 0/1");
  await franz.getByRole("button", { expanded: false }).click();
  await expect(franz.locator(".pfad-knoten")).toHaveCount(1);
  const truhe = page.getByRole("region", { name: "Tagestruhe" });
  await expect(truhe).toContainText("Schaffe deine Tagesziele");

  // Eine Pflicht erfüllt (bestandener Test im Englisch-Plan): die Truhe bleibt zu – das leichtere Fach allein zählt nicht.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId: Number(childId), pin: PIN } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const bestehen = async (planId: number, positionId: number, front: string) => {
    const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
    const attemptId = await postId(request, tests, kind, {}, "attemptId");
    const submit = await request.post(`${tests}/${attemptId}/submit`,
      { headers: kind, data: { answers: [{ itemIndex: 0, givenAnswer: front }] } });
    expect(submit.ok(), await submit.text()).toBeTruthy();
  };
  await bestehen(planEn, posEn, en.front);
  await page.reload();
  await expect(truhe).toContainText("Schaffe deine Tagesziele");
  await expect(truhe.getByRole("button")).toHaveCount(0);

  // Beide erfüllt: jetzt leuchtet die Truhe.
  await bestehen(planFr, posFr, fr.front);
  await page.reload();
  await expect(truhe).toHaveClass(/bereit/);
  await expect(truhe.getByRole("button", { name: "Truhe öffnen" })).toBeVisible();

  // Gegenprobe der Aktivierungsregel: ein zweiter Englisch-Plan löst den ersten ab, Französisch bleibt.
  await vaterLogin(page, { id: "1", pin: "0000" });
  const PLAN_EN2 = `Plan EN neu ${RUN}`;
  await planAnlegen(page, PLAN_EN2, childId, ENGLISCH);
  await page.goto(`/vater/plaene?childId=${childId}`);
  await expect(zeile(PLAN_EN)).toContainText("inaktiv");
  await expect(zeile(PLAN_FR)).not.toContainText("inaktiv");
  await expect(zeile(PLAN_EN2)).not.toContainText("inaktiv");
});
