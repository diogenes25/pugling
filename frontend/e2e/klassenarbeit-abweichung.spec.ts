import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-744 und B-746, Rollengang Vater: am Klausurtag hält die App die Notenstufe des Fachs an der Arbeit fest, und der Vater
 * sieht sie in der Klassenarbeiten-Liste neben der Note samt Abstand. Ein frisches Kind mit drei englischen Wörtern, keins
 * sicher, steht auf Stufe 6; seine Anmeldung am Klausurtag ist der Lauf, der den Schnappschuss schreibt. Die Arbeit ohne
 * Fach am selben Tag bekommt keinen.
 *
 * Eigenes Kind: die Stufe hängt am Kind, und die Liste soll nur die Arbeiten dieses Laufs zeigen.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

// Der Lerntag des Servers ist deutsche Zeit (LearningDay, B-730).
const heute = () => new Date().toLocaleDateString("sv-SE", { timeZone: "Europe/Berlin" });

test("Prognose vom Klausurtag neben der Note, ohne Fach keine", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8746";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Prognosekind ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Englisch ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Prognose-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Wörter ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [["dog", "Hund"], ["cat", "Katze"], ["house", "Haus"]].map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  const plane = async (title: string, fach: number | null) => {
    const res = await request.post("/api/v1/supervisor/class-tests", { headers: vater, data: {
      childId, title, subjectId: fach, scheduledDate: heute(), prepWindowDays: 0,
    } });
    expect(res.ok(), await res.text()).toBeTruthy();
    return (await res.json()).klassenarbeit.id as number;
  };
  const mitFach = await plane(`Vokabeltest ${RUN}`, subjectId);
  await plane(`Ohne Fach ${RUN}`, null);

  // Der Lauf des Klausurtags: die Anmeldung des Kindes bewertet die Stufe und hält sie an der Arbeit fest.
  const kind = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kind.ok(), await kind.text()).toBeTruthy();

  // Die Note trägt der Vater ein; 4 gegen die prognostizierte 6: zwei Noten besser als erwartet.
  const note = await request.patch(`/api/v1/supervisor/class-tests/${mitFach}`, { headers: vater, data: { grade: 4, status: "Written" } });
  expect(note.ok(), await note.text()).toBeTruthy();

  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/class-tests?childId=${childId}`);
  const zeile = page.getByRole("row", { name: new RegExp(`Vokabeltest ${RUN}`) });
  // Spalte 7 von 8 (0-basiert 6): Titel, Termin, Status, Übungen, Vorbereitung, Note, Prognose, Aktion.
  await expect(zeile.getByRole("cell").nth(6)).toHaveText("Stufe 6 · 2 besser");
  await expect(page.getByRole("row", { name: new RegExp(`Ohne Fach ${RUN}`) }).getByRole("cell").nth(6))
    .toHaveText("keine Prognose");
});
