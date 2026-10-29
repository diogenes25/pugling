import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-277. Der Schnellanleger im Assistenten („+ anlegen & wählen") rief das Store-POST ohne Schlüssel auf; für ein Wort,
 * das es im Sprachpaar mit derselben Übersetzung schon gab, legte der Server daraufhin einen `_2`-Eintrag an – und die
 * mitgeschickte Variante hing am Duplikat statt an dem Eintrag, den `/vater/vokabeln` zeigt (B-71, AK 5).
 *
 * Zwei Fälle, die sich nur in der Übersetzung unterscheiden: dieselbe Übersetzung trifft den vorhandenen Eintrag, eine
 * andere ist ein Homonym und bekommt einen eigenen (B-65, Entscheidung 6). Ohne den zweiten Fall bliebe ein Fix grün,
 * der nur nach dem Wort sucht.
 *
 * Der Store entsteht über die API (Gegenstand ist das Anlegen im Assistenten, nicht der Store), der Beleg ist der
 * Store-Stand danach – das Token in der Auswahl sähe für Original und Duplikat gleich aus. Welche Schreibweisen der
 * Server-Schlüssel zusammenfasst (Akzente, ß), hält `VocabularyStoreTests.ReuseExisting_…`.
 */

const RUN = Date.now().toString(36).slice(-6);
const WORT = `huge${RUN}`;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Der Schnellanleger wählt ein vorhandenes Wort samt neuer Variante, statt es doppelt anzulegen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const name = `Schnellanlage ${RUN}`;
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const original = await postId(request, "/api/v1/creator/vocabulary", headers, {
    sourceLanguage: "en", targetLanguage: "de", word: WORT, translation: "riesig", translationAlternatives: ["sehr groß"],
  });

  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });

  // Das Token zeigt `Wort→Übersetzung` nur für Einträge, die die Store-Liste geladen hat (erste Seite) – darum filtern.
  await page.getByLabel("Vokabel-Store durchsuchen").fill(WORT);

  // Dasselbe Paar in anderer Schreibweise, mit einer neuen Variante. Groß/Klein faltet der Store-Schlüssel – ein Fix,
  // der exakt vergliche, legte hier wieder ein `_2` an.
  await page.locator("#vp-word").fill(WORT);
  await page.locator("#vp-translation").fill("Riesig");
  await page.getByRole("button", { name: "+ Variante (neues Wort)" }).click();
  await page.getByLabel("Auch richtig 1 (neues Wort)", { exact: true }).fill("gewaltig");
  await page.getByRole("button", { name: "+ anlegen & wählen" }).click();
  await expect(page.getByText(`${WORT}→riesig`)).toBeVisible();

  // Dasselbe Wort mit anderer Übersetzung: ein Homonym, also ein eigener Eintrag.
  await page.locator("#vp-word").fill(WORT);
  await page.locator("#vp-translation").fill("ungeheuer");
  await page.getByRole("button", { name: "+ anlegen & wählen" }).click();
  await expect(page.getByText(`${WORT}→ungeheuer`)).toBeVisible();

  const res = await request.get(`/api/v1/creator/vocabulary?word=${WORT}&sourceLanguage=en&targetLanguage=de`, { headers });
  expect(res.ok(), await res.text()).toBeTruthy();
  const entries = (await res.json()) as { id: number; translation: string; translationAlternatives: string[] | null }[];
  expect(entries.map((e) => e.translation).sort()).toEqual(["riesig", "ungeheuer"]);
  const riesig = entries.find((e) => e.translation === "riesig")!;
  expect(riesig.id).toBe(original);
  // Die alte Variante bleibt, die neue kommt dazu – der Schnellanleger ersetzt nicht, was ein anderer gepflegt hat.
  expect([...(riesig.translationAlternatives ?? [])].sort()).toEqual(["gewaltig", "sehr groß"]);
});
