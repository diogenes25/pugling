import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-469 AK 5: der Vater sieht am Plan, womit jede Pflicht geprüft wird. Drei Positionen per API – eine getippte und eine
 * ungetippte Vokabel-Position und eine Grammatik-Position (Typ ohne Test, `checkMode: None`) –, dann liest er im
 * Plan-Detail die drei Texte in den drei Zeilen.
 *
 * Rot, wenn die Zeile den Text nicht rendert (Ausgangszustand: „· getippt" bzw. nichts).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

test("Die Positionszeilen nennen die Art der Lernkontrolle", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  // Ein eigenes Kind: ein Plan am geseedeten Kind könnte dessen aktiven Plan berühren.
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Kontrolle ${RUN}`, pin: "8655" });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Kontrollfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, { name: `Kontrollreihe ${RUN}`, subjectId });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: "Unit 1", orderIndex: 1 });
  const vokabeln = (title: string, orderIndex: number) => postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title, orderIndex, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `dog ${title}`, back: "Hund" }] },
  });
  const getipptTitel = `Vokabeln getippt ${RUN}`;
  const ungetipptTitel = `Vokabeln frei ${RUN}`;
  const grammatikTitel = `Grammatik ${RUN}`;
  const exGetippt = await vokabeln(getipptTitel, 1);
  const exUngetippt = await vokabeln(ungetipptTitel, 2);
  const exGrammatik = await postId(request, `${units}/${unitId}/grammar`, vater, {
    title: grammatikTitel, orderIndex: 3, rewardPoints: 5,
    config: { instruction: "Setze ein.", tasks: [{ prompt: "I ___ tired.", answer: "am" }] },
  });

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, title: `Kontrollplan ${RUN}`, durationDays: 7, active: true });
  const positions = `/api/v1/supervisor/study-plans/${planId}/positions`;
  await postId(request, positions, vater, { exerciseId: exGetippt, requireTypedTest: true });
  await postId(request, positions, vater, { exerciseId: exUngetippt, requireTypedTest: false });
  await postId(request, positions, vater, { exerciseId: exGrammatik });

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/plan/${planId}`);

  const zeile = (titel: string) => page.getByRole("row", { name: new RegExp(titel) });
  await expect(zeile(getipptTitel)).toContainText("Test getippt");
  await expect(zeile(ungetipptTitel)).toContainText("Test, Selbsteinschätzung zählt");
  await expect(zeile(grammatikTitel)).toContainText("kein Test – zählt Durchspielen");
  // Die schwachen Arten sind hervorgehoben, die getippte nicht.
  await expect(zeile(ungetipptTitel).locator(".pill.amber", { hasText: "Selbsteinschätzung" })).toBeVisible();
  await expect(zeile(grammatikTitel).locator(".pill.amber", { hasText: "kein Test" })).toBeVisible();
  await expect(zeile(getipptTitel).locator(".pill.amber")).toHaveCount(0);
  // Die alte Angabe ist ersetzt, nicht ergänzt.
  await expect(zeile(getipptTitel)).not.toContainText("· getippt");
});
