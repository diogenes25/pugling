import { test, expect, type APIRequestContext } from "@playwright/test";

/*
 * B-12 – geteilte Übungen bewerten, aus beiden Sitzen.
 *
 * Der Vater stöbert in der geteilten Bibliothek und soll Bewährtes erkennen, bevor er es zuweist: er sieht
 * Durchschnitt und Anzahl, sortiert danach, bewertet selbst, ändert und löscht seine Bewertung. Der Autor
 * sieht die Urteile über sein Material, kann es aber nicht selbst bewerten (AK 2/7).
 *
 * Der Stoff entsteht über die API: Gegenstand ist hier das Bewerten, nicht das Anlegen (das tragen
 * `uebungstypen.spec.ts` und `vater-von-null.spec.ts`). Zwei Lehrer-Konten, damit die Abgrenzung
 * „eigene Bewertung / fremde Bewertung / Autor" drei verschiedene Personen trifft.
 */

const RUN = Date.now().toString().slice(-6);
// Drei Zeilen: bewährt (5 Sterne), schwach (2) und unbewertet. Titel und Anlage ergeben beide [bewährt, neu,
// schwach]; die Bewertung aufsteigend ist [schwach, bewährt, neu], absteigend [bewährt, schwach, neu] (Unbewertetes
// in BEIDEN Richtungen hinten, AK 6). Keine Titel-/Anlage-Reihenfolge (auf- oder absteigend) trifft eine der beiden,
// und auch die Spiegel ([neu, schwach, bewährt] bzw. [neu, bewährt, schwach]) sind eigene Antworten. Innerhalb der
// bewerteten Zeilen laufen Titel und Anlage dem AUFSTEIGENDEN Durchschnitt entgegen – „bewertet zuerst, dann nach
// Id/Titel" fällt darum am aufsteigenden Schritt. Ob absteigend wirklich der Durchschnitt entscheidet, kann diese
// Spec mit zwei bewerteten Zeilen nicht trennen (jede feste Reihenfolge trifft eine der beiden Richtungen); das
// trägt `ExerciseRatingsTests.SortRating_…_In_Beiden_Richtungen_Am_Ende`.
const PROVEN = `Bewährt ${RUN}`;
const FRESH = `Neu ${RUN}`;
const WEAK = `Wackelig ${RUN}`;

async function teacher(request: APIRequestContext, name: string, pin: string) {
  const created = await request.post("/api/v1/creator/teacher-accounts", { data: { name, email: null, pin } });
  expect(created.ok(), await created.text()).toBeTruthy();
  const id = (await created.json()).creatorId as number;
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: id, pin } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { id, pin, headers: { Authorization: `Bearer ${(await login.json()).token}` } };
}

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Vater bewertet eine geteilte Übung, sortiert danach – der Autor liest mit, bewertet aber nicht", async ({ page, request }) => {
  const author = await teacher(request, `Autorin ${RUN}`, "7171");
  const other = await teacher(request, `Kollege ${RUN}`, "7272");

  const subjectId = await postId(request, "/api/v1/creator/subjects", author.headers, { name: `Bewertungsfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", author.headers, {
    name: `Bewertungsreihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, author.headers,
    { label: "Unit 1", orderIndex: 1 });
  const exercise = (title: string) => postId(request,
    `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/arithmetic`, author.headers, {
      title, orderIndex: 1, rewardPoints: 10,
      config: { problems: [{ prompt: "3 + 4", answer: 7, tolerance: 0 }] },
    });
  const provenId = await exercise(PROVEN);
  await exercise(FRESH);
  const weakId = await exercise(WEAK);
  const rated = await request.put(`/api/v1/creator/exercises/${provenId}/rating`,
    { headers: other.headers, data: { stars: 5, comment: "Klar aufgebaut" } });
  expect(rated.ok(), await rated.text()).toBeTruthy();
  const ratedWeak = await request.put(`/api/v1/creator/exercises/${weakId}/rating`,
    { headers: other.headers, data: { stars: 2, comment: null } });
  expect(ratedWeak.ok(), await ratedWeak.text()).toBeTruthy();

  // ── Vater: stöbern ────────────────────────────────────────────────────────────────────────────────
  const listUrl = `/vater/exercises?subjectId=${subjectId}&seriesId=${seriesId}&seriesUnitId=${unitId}`;
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
  await page.goto(listUrl);
  await page.getByLabel("geteilte Übungen anderer Väter anzeigen").check();

  const proven = page.getByRole("group", { name: PROVEN, exact: true });
  const fresh = page.getByRole("group", { name: FRESH, exact: true });
  await expect(proven.getByText("★ 5,0 (1 Bewertung)")).toBeVisible();
  await expect(fresh.getByText("noch unbewertet")).toBeVisible();

  // AK 6 im UI: das Unbewertete steht in beiden Richtungen hinten (geändert 2026-10-26, Morgen-Entscheidung).
  // Nur die Zeilen dieses Laufs: auch das Entwicklungs-Widget trägt eine benannte Gruppe.
  const rowTitles = async () => (await page.locator("[role=group][aria-label]")
    .evaluateAll((els) => els.map((e) => e.getAttribute("aria-label") ?? ""))).filter((t) => t.endsWith(RUN));
  // Ausgangslage (Titel aufsteigend): erst feststellen, damit die Probe danach nicht auf dem Startzustand steht.
  await expect.poll(rowTitles).toEqual([PROVEN, FRESH, WEAK]);
  await page.getByLabel("Sortieren").selectOption("rating");
  await expect.poll(rowTitles).toEqual([WEAK, PROVEN, FRESH]);
  await page.getByRole("button", { name: "Aufsteigend – zu absteigend wechseln" }).click();
  await expect.poll(rowTitles).toEqual([PROVEN, WEAK, FRESH]);

  // ── Vater: bewerten, ändern, löschen ──────────────────────────────────────────────────────────────
  await proven.getByRole("button", { name: "★ Bewertungen" }).click();
  const ratings = proven.getByRole("list", { name: "Bewertungen" });
  await expect(ratings).toContainText(`Kollege ${RUN}`);
  await expect(ratings).toContainText("Klar aufgebaut");
  // Die Sterne als benanntes Bild: ein `aria-label` auf einem blanken `span` sagen Screenreader nicht verlässlich an.
  await expect(ratings.getByRole("img", { name: "5 von 5 Sternen" })).toBeVisible();

  await proven.getByLabel("2 Sterne").check();
  await proven.getByLabel(/Kommentar/).fill("Für Klasse 5 zu schwer");
  await proven.getByRole("button", { name: "Bewertung speichern" }).click();
  await expect(proven.getByText("Bewertung gespeichert.")).toBeVisible();
  // (5 + 2) / 2 – der Schnitt aus beiden, nicht der eigene Wert und nicht der fremde.
  await expect(proven.getByText("★ 3,5 (2 Bewertungen)")).toBeVisible();
  // Der Bild-Name folgt der Sternzahl der Zeile, nicht einem festen Wert.
  await expect(ratings.getByRole("img", { name: "2 von 5 Sternen" })).toBeVisible();
  await expect(ratings).toContainText("(du)");
  await expect(ratings).toContainText("Für Klasse 5 zu schwer");

  await proven.getByLabel("4 Sterne").check();
  await proven.getByRole("button", { name: "Bewertung ändern" }).click();
  await expect(proven.getByText("★ 4,5 (2 Bewertungen)")).toBeVisible();

  // Rückweg: nach dem Neuladen liest das Formular die gespeicherte eigene Bewertung zurück.
  await page.reload();
  await page.getByLabel("geteilte Übungen anderer Väter anzeigen").check();
  await proven.getByRole("button", { name: "★ Bewertungen" }).click();
  await expect(proven.getByLabel("4 Sterne")).toBeChecked();
  await expect(proven.getByLabel(/Kommentar/)).toHaveValue("Für Klasse 5 zu schwer");

  await proven.getByRole("button", { name: "Bewertung löschen" }).click();
  await expect(proven.getByText("★ 5,0 (1 Bewertung)")).toBeVisible();
  await expect(proven.getByRole("button", { name: "Bewertung speichern" })).toBeVisible();
  await expect(proven.getByRole("list", { name: "Bewertungen" })).not.toContainText("(du)");

  // ── Autorin: liest mit, bewertet nicht ─────────────────────────────────────────────────────────────
  await page.getByRole("button", { name: "Abmelden" }).first().click();
  await page.locator("#fid").fill(String(author.id));
  await page.locator("#pin").fill(author.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  // Angemeldet bleibt sie auf der Seite, auf der sie sich angemeldet hat (siehe lehrer-konto.spec.ts).
  await expect(page.locator("#fid")).toHaveCount(0);
  await page.goto(listUrl);
  await proven.getByRole("button", { name: "★ Bewertungen" }).click();
  await expect(proven.getByRole("list", { name: "Bewertungen" })).toContainText(`Kollege ${RUN}`);
  await expect(proven.getByText("Deine eigene Übung kannst du nicht bewerten")).toBeVisible();
  await expect(proven.getByRole("button", { name: /Bewertung (speichern|ändern)/ })).toHaveCount(0);
  await expect(proven.getByLabel("1 Stern", { exact: true })).toHaveCount(0);

  // ── Kollege mit Schreibrecht: bearbeiten darf er, verwalten nicht – also bewertet er (B-12 AK 7, B-275) ──────────
  // Er ist der einzige der drei, bei dem `isOwn` und `isOwner` auseinanderfallen; nur er trennt die Aufrufstelle, die
  // `isOwner` fragen muss, von einer, die `isOwn` fragt.
  const granted = await request.post(`/api/v1/creator/exercises/${provenId}/grants`,
    { headers: author.headers, data: { creatorId: other.id, permission: "Write" } });
  expect(granted.ok(), await granted.text()).toBeTruthy();
  await page.getByRole("button", { name: "Abmelden" }).first().click();
  await page.locator("#fid").fill(String(other.id));
  await page.locator("#pin").fill(other.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.locator("#fid")).toHaveCount(0);
  await page.goto(listUrl);
  await proven.getByRole("button", { name: "★ Bewertungen" }).click();
  // Seine Bewertung von oben (5 Sterne) steht im Formular – „ändern", nicht „speichern".
  await expect(proven.getByRole("button", { name: "Bewertung ändern" })).toBeVisible();
  await expect(proven.getByText("Deine eigene Übung kannst du nicht bewerten")).toHaveCount(0);
});
