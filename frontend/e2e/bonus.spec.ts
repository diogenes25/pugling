import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, tippen } from "./helpers";

/*
 * Bonus-Training – Extra-Übungen zum aktuellen Thema, offen erst nach der Tagespflicht. Die Regeln (Form je Durchgang,
 * Münzen je Form – Karteikarte 1, Auswahl 1, Tippen 3 –, Tagesdeckel, Halbierung je weiterer Runde am selben Tag) hält der Server und prüft
 * `ExtraTrainingTests`; hier liegt, was nur die laufende App zeigt: die Sperre auf dem Pfad, das Öffnen nach erfüllter
 * Pflicht, eine getippte Rechen-Übung, die Münzen bringt und danach heute noch die Hälfte wert ist (B-538), und eine
 * Vokabel-Runde zum Aufwärmen, die je gewusstem Wort eine Münze bringt (B-657).
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

test("Das Bonus-Training öffnet nach der Tagespflicht, zahlt eine getippte Übung einmal und wärmt Vokabeln für eine Münze je Wort auf", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8653";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Bonus ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Bonus-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Bonus-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
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

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, subjectId, title: `Bonus-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId: exVokabeln, stage: 4, cadence: "Daily", useLeitner: false });

  // Vor der Pflicht: der Einstieg steht gesperrt unter dem Pfad.
  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByText("🔒 Erst die Tagesziele – dann öffnet das Bonus-Training.")).toBeVisible();
  await expect(page.getByRole("link", { name: "Zum Bonus-Training" })).toHaveCount(0);

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

  await page.reload();
  await page.getByRole("link", { name: "Zum Bonus-Training" }).click();
  await expect(page.getByRole("heading", { name: "Bonus-Training" })).toBeVisible();
  const zeile = (titel: string) => page.locator(".bonus-zeile", { hasText: titel });
  await expect(zeile(VOKABELN)).toContainText("Karteikarte · 1 🪙 je gewusstem Wort");
  await expect(zeile(RECHNEN)).toContainText("Tippen · 3 🪙 je Treffer");

  // Die Rechen-Übung: getippt, am Ende ausgewertet – danach bleibt sie im Vorrat und ist heute noch die Hälfte wert (B-538).
  await zeile(RECHNEN).getByRole("link", { name: `Spielen: ${RECHNEN}` }).click();
  for (let i = 0; i < 2; i++) {
    const frage = (await page.locator(".fcard").innerText()).includes("2 + 3") ? "5" : "16";
    await tippen(page.getByRole("textbox", { name: "Lösung" }), frage);
    await page.getByRole("button", { name: "Weiter →" }).click();
  }
  await expect(page.getByText("2 / 2 richtig")).toBeVisible();
  await expect(page.getByText("+6 🪙 verdient")).toBeVisible();
  await page.getByRole("button", { name: "Zurück zum Bonus-Training" }).click();
  await expect(zeile(RECHNEN)).toContainText("heute noch 50 % wert");
  await expect(zeile(RECHNEN).getByRole("link", { name: `Spielen: ${RECHNEN}` })).toBeVisible();
  await expect(page.getByText("Heute verdient: 6 / 90 🪙")).toBeVisible();

  // Die Vokabeln: Aufwärmen per Karteikarte zahlt 1 je gewusstem Wort (B-657), und die Runde zählt als gespielt.
  // Zwei Karten, dann „Später weiter“: die Runde setzt bei Karte 3 fort, statt von vorn zu beginnen.
  // B-778: nach „Aufdecken“ steht der Fokus auf dem ersten Urteil, nicht auf `body`.
  const karteWeiter = async () => {
    await page.getByRole("button", { name: "Aufdecken 🔄" }).click();
    await expect(page.getByRole("button", { name: "Nicht gewusst" })).toBeFocused();
    await page.getByRole("button", { name: "Gewusst", exact: true }).click();
  };
  await zeile(VOKABELN).getByRole("link", { name: `Spielen: ${VOKABELN}` }).click();
  // B-785: der Aufwärm-Hinweis verspricht mehr erst beim Tippen – die Auswahl zahlt so viel wie die Karteikarte.
  await expect(page.getByText(/jedes gewusste Wort bringt 1 🪙, beim Tippen gibt es mehr/)).toBeVisible();
  await karteWeiter();
  await karteWeiter();
  await expect(page.getByText("Karte 3 / 4")).toBeVisible();
  await page.getByRole("button", { name: "Später weiter" }).click();
  await zeile(VOKABELN).getByRole("link", { name: `Weiterspielen: ${VOKABELN}` }).click();
  await expect(page.getByText("Karte 3 / 4")).toBeVisible();
  await karteWeiter();
  await karteWeiter();
  // B-778: das Ergebnisblatt nimmt den Fokus auf seine Überschrift.
  await expect(page.getByText("4 / 4 richtig")).toBeFocused();
  await expect(page.getByText("+4 🪙 verdient")).toBeVisible();
  await page.getByRole("button", { name: "Zurück zum Bonus-Training" }).click();
  await expect(zeile(VOKABELN)).toContainText("1× gespielt");
});
