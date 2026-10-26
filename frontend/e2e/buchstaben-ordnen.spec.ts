import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-05 – Buchstaben ordnen, aus dem Sohn-Sitz. Der Stoff entsteht über die API (Gegenstand ist das Spielen, nicht das
 * Anlegen): eine Vokabel-Übung mit zwei Ein-Wort-Antworten und einer Mehrwort-Antwort, eine Position auf der Stufe
 * „Buchstaben ordnen“ (7). Der Sohn legt ein Wort per Tastatur, eines per Klick (AK 5: zwei gleichwertige
 * Bedienarten), und die Mehrwort-Antwort kommt als Buchstabenkästchen (AK 3). Die Karten-Reihenfolge friert der
 * Server ein – die Spec erkennt die Karte darum an ihrem Wort, nicht an ihrer Position.
 *
 * Eigenes Kind: je Kind ist genau ein aktiver Plan spielbar, und `full-flow` belegt Kind #1.
 */

const RUN = Date.now().toString(36).slice(-5);
const CARDS: Record<string, string> = {
  [`to buy ${RUN}`]: "kaufen",
  [`to eat ${RUN}`]: "essen",
  [`the town ${RUN}`]: "die Stadt",
};

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Sohn ordnet Buchstaben per Tastatur und per Klick, eine Mehrwort-Antwort kommt als Kästchen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "7531";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Ordner ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Ordnen-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Ordnen-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Ordnen ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: Object.entries(CARDS).map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, title: `Ordnen-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 7, cadence: "Daily", useLeitner: true });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await page.getByRole("link", { name: /ÜBEN/ }).click();
  const counter = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  await expect(counter).toHaveText(/Karte 1 \/ 3/);

  const bedient = { tastatur: false, klick: false, kaesten: false };
  for (let i = 0; i < 3; i++) {
    await expect(counter).toHaveText(new RegExp(`Karte ${i + 1} / 3`));
    const visible = await Promise.all(Object.keys(CARDS).map(async (p) => [p, await page.getByText(p, { exact: true }).isVisible()] as const));
    const current = visible.find(([, v]) => v)?.[0];
    expect(current, "die Karte nennt eines der drei Wörter").toBeTruthy();
    const answer = CARDS[current!];
    const pool = page.getByRole("group", { name: "Buchstaben zum Ordnen" });

    if (answer.includes(" ")) {
      // AK 3: keine Kacheln, sondern Buchstabenkästchen – das Leerzeichen steht schon fest.
      await expect(pool).toHaveCount(0);
      const boxes = page.locator(".letterboxes input");
      await expect(boxes).toHaveCount(answer.replace(" ", "").length);
      await boxes.first().click();
      await page.keyboard.type(answer.replace(" ", ""));
      bedient.kaesten = true;
    } else {
      await expect(pool.getByRole("button")).toHaveCount(answer.length);
      if (!bedient.tastatur) {
        // Tastatur: Fokus in den Kachel-Block, dann das Wort tippen.
        await pool.getByRole("button").first().focus();
        await page.keyboard.type(answer);
        bedient.tastatur = true;
      } else {
        // Klick: je Buchstabe die erste noch freie Kachel.
        for (const ch of answer) {
          await pool.locator(`button[aria-label="Buchstabe ${ch}"][aria-disabled="false"]`).first().click();
        }
        bedient.klick = true;
      }
      // Gelegt ist genau das Wort – angesagt in der Live-Region, Buchstabe für Buchstabe.
      await expect(page.getByRole("status").filter({ hasText: /^Gelegt:/ })).toHaveText(`Gelegt: ${answer.split("").join(" ")}`);
    }
    await page.getByRole("button", { name: "Prüfen" }).click();
  }

  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();
  expect(bedient).toEqual({ tastatur: true, klick: true, kaesten: true });
});
