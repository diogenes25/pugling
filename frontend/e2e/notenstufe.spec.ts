import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * B-741 bis B-743, Rollengang Sohn und Vater: die Notenstufe eines Vokabel-Fachs. Ein frisches Kind hat noch kein Wort
 * sicher, steht also auf Stufe 6 – der Sohn liest auf der Fachkarte nur sein nächstes Ziel und kein Stufenwort, der
 * Vater im Kind-Hub die Zahl mit Bestwert. Das Mathe-Fach daneben hat keinen Wortschatz und darum keine Stufe.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar, und die Stufe hängt am Kind.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"], ["house", "Haus"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Stufe 6: der Sohn sieht nur sein Ziel, der Vater die Zahl, Mathe bleibt ohne Stufe", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8743";
  const childName = `Stufenkind ${RUN}`;
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: childName, pin });

  const fachPlan = async (fach: string, art: "vocabulary" | "arithmetic") => {
    const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `${fach} ${RUN}` });
    const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
      name: `${fach}-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
      sourceLanguage: null, targetLanguage: null, notes: null,
    });
    const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
    const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
    const config = art === "vocabulary"
      ? { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) }
      : { problems: [{ prompt: "7 × 6", answer: 42, tolerance: 0 }] };
    const exerciseId = await postId(request, `${units}/${unitId}/${art}`, vater, {
      title: `${fach}-Übung ${RUN}`, orderIndex: 1, rewardPoints: 10, config,
    });
    const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
      { childId, subjectId, title: `${fach}-Plan ${RUN}`, durationDays: 7, active: true });
    await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
      { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });
  };
  await fachPlan("Englisch", "vocabulary");
  await fachPlan("Mathe", "arithmetic");

  // Sohn: drei Wörter, keins sicher -> Stufe 6; die 5 beginnt bei 30 Prozent, also bei einem Wort.
  await sohnLogin(page, { id: String(childId), pin });
  const englisch = page.getByRole("region", { name: `Englisch ${RUN}` });
  await expect(englisch.getByTestId("fach-stufe")).toHaveText("noch 1 Wort sicher bis zur nächsten Stufe");
  await expect(page.getByRole("region", { name: `Mathe ${RUN}` }).getByTestId("fach-stufe")).toHaveCount(0);
  // Rot, wenn eine Stufe unter 3 ein Stufenwort trüge.
  await expect(page.getByText(/Sechser|Fünfer|Vierer/)).toHaveCount(0);

  // Vater: im Kind-Hub, Reiter Schule, die Zahl samt Bestwert - und nur das Vokabel-Fach.
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/kind/${childId}?bereich=schule`);
  const zeile = page.getByRole("row", { name: new RegExp(`Englisch ${RUN}`) });
  await expect(zeile.getByRole("cell").nth(1)).toHaveText("6");
  await expect(zeile.getByRole("cell").nth(2)).toHaveText("6");
  await expect(zeile.getByRole("cell").nth(3)).toHaveText("noch 1 Wort");
  await expect(page.getByRole("row", { name: new RegExp(`Mathe ${RUN}`) })).toHaveCount(0);
});
