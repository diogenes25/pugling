import { test, expect, type APIRequestContext, type Page } from "@playwright/test";
import { pfadStart, sohnLogin, vaterLogin } from "./helpers";

/*
 * B-423: der Assistent fragt beim Ziel „Klassenarbeit" den Termin, lässt den Plan am Vortag enden, gibt jeder
 * Vokabel-Position die Stufenleiter (erstes Drittel Selbsteinschätzung, danach getippt) und legt die Klassenarbeit mit an.
 *
 * Der Stoff entsteht über die API (Gegenstand ist der Assistent, nicht das Anlegen): ein eigenes Fach mit genau einer
 * Vokabelübung und ein eigenes Kind je Fall – je Kind ist genau ein aktiver Plan spielbar, und ein geseedetes Kind trüge
 * fremde Pläne. Termin = heute + 8 Tage (UTC, wie der Server zählt): Plan 8 Tage, getippt ab Tag ceil(8/3)+1 = 4.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const TAG = 86_400_000;
const iso = (tage: number) => new Date(Date.now() + tage * TAG).toISOString().slice(0, 10);
const deutsch = (d: string) => d.split("-").reverse().join(".");

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

/** Fach, Reihe, Unit, eine Vokabelübung mit einem Wort und ein Kind ohne Plan. */
async function stoff(request: APIRequestContext, fall: string) {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  const fach = `KA-Fach ${fall} ${RUN}`;
  const titel = `KA-Vokabeln ${fall} ${RUN}`;
  const pin = "8661";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `KA-Kind ${fall} ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: fach });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, { name: `KA-Reihe ${fall} ${RUN}`, subjectId });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, headers, { label: "Unit 1", orderIndex: 1 });
  await postId(request, `${units}/${unitId}/vocabulary`, headers, {
    title: titel, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `tree ${RUN}`, back: "Baum" }] },
  });
  return { childId: String(childId), pin, fach, titel };
}

/** Schritt 1 bis Schritt 5 mit Ziel „Klassenarbeit"; prüft unterwegs Terminfeld (AK 1), Feinschliff-Satz und Überblick (AK 2). */
async function bisZumUeberblick(page: Page, s: { childId: string; fach: string; titel: string }, termin: string) {
  await page.goto("/vater/wizard");
  await page.getByRole("radio", { name: "Bestehendes Kind" }).click();
  // Per Wert (der Id), nicht per Position: die Liste ist nach Namen sortiert und wächst mit jedem Lauf.
  await page.getByRole("combobox", { name: "Kind" }).selectOption(s.childId);
  await page.getByRole("button", { name: "Weiter" }).click();

  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: s.fach });
  await expect(page.getByRole("radio", { name: /Klassenarbeit vorbereiten/ })).toHaveAttribute("aria-checked", "true");
  const weiter = page.getByRole("button", { name: "Weiter" });
  const feld = page.getByLabel("Termin der Klassenarbeit (Pflicht)", { exact: true });
  // AK 1: ohne Termin gesperrt – rot, wenn der Knopf nicht am Termin hängt.
  await expect(feld).toHaveValue("");
  await expect(weiter).toBeDisabled();
  // AK 1: morgen ist zu nah, und das Feld nennt den Grund.
  await feld.fill(iso(1));
  await expect(weiter).toBeDisabled();
  await expect(page.getByText(`Frühestens am ${deutsch(iso(2))}`)).toBeVisible();
  await feld.fill(termin);
  await expect(weiter).toBeEnabled();
  await weiter.click();

  await expect(page.getByRole("heading", { name: /Übungen wählen/ })).toBeVisible();
  await page.getByLabel("Übung suchen", { exact: true }).fill(s.titel);
  await page.getByRole("checkbox", { name: new RegExp(s.titel) }).check();
  await expect(page.getByRole("heading", { name: /\(1 gewählt/ })).toBeVisible();
  await weiter.click();

  // Feinschliff: Dauer berechnet, die Leiter als Satz (Entscheidungen 1, 2, 5).
  await expect(page.getByRole("heading", { name: /Feinschliff/ })).toBeVisible();
  await expect(page.locator("#wiz-duration")).toHaveValue("8");
  await expect(page.getByText(/Vokabeln: Selbsteinschätzung bis Tag 3, dann Freitext\. „Nur getippte Tests zählen" gilt hier nur/)).toBeVisible();
  await weiter.click();

  // AK 2: Zeitraum bis zum Vortag, und der Überblick sagt die Arbeit vorher an.
  await expect(page.getByRole("heading", { name: "Überblick" })).toBeVisible();
  await expect(page.getByText(`${deutsch(iso(0))} – ${deutsch(iso(7))} · 8 Tage`, { exact: true })).toBeVisible();
  await expect(page.getByText(`legt die Klassenarbeit am ${deutsch(termin)} an`, { exact: true })).toBeVisible();
}

test("Klassenarbeit aus dem Assistenten: Plan bis zum Vortag, Leiter, Arbeit – und am Tag 1 zählt die Selbsteinschätzung",
  async ({ page, request, browser }) => {
    const s = await stoff(request, "A");
    const termin = iso(8);
    await vaterLogin(page, FATHER);
    await bisZumUeberblick(page, s, termin);

    await page.getByRole("button", { name: "✅ Lehrplan erstellen" }).click();
    await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

    // AK 7: die Positionszeile zeigt die Leiter lesbar.
    const zeile = page.getByRole("row", { name: new RegExp(s.titel) });
    await expect(zeile).toContainText("Stufen: Selbsteinschätzung bis Tag 3, dann Freitext");
    // AK 8 sichtbar: mit Leiter zählt die Selbsteinschätzung (B-469-Text), obwohl das Ziel „nur getippt" vorbelegt.
    await expect(zeile).toContainText("Test, Selbsteinschätzung zählt");

    // AK 4: die Arbeit steht mit Termin, Fach und der gewählten Übung unter „Klassenarbeiten".
    await page.goto(`/vater/class-tests?childId=${s.childId}`);
    const arbeit = page.getByRole("row", { name: new RegExp(`${s.fach}-Arbeit`) });
    await expect(arbeit).toContainText(deutsch(termin));
    await expect(arbeit).toContainText(s.fach);
    await arbeit.getByRole("button", { name: "Übungen & Vorbereiten" }).click();
    await expect(page.getByText(s.titel).first()).toBeVisible();

    // AK 9: Plantag 1, Selbsteinschätzungs-Stufe – ein bestandener Test erfüllt das Tagesziel. Rot ohne AK 8: dann ist der
    // Versuch ungewertet und zählt bei `requireTypedTest` nicht (`PositionProgressService`).
    const sohnKontext = await browser.newContext();
    const sohn = await sohnKontext.newPage();
    await sohnLogin(sohn, { id: s.childId, pin: s.pin });
    await pfadStart(sohn, "test");
    await sohn.getByRole("button", { name: "Aufdecken 🔄" }).click();
    await sohn.getByRole("button", { name: "Gewusst", exact: true }).click();
    await expect(sohn.locator(".vtitle", { hasText: "SIEG!" })).toBeVisible();
    await sohn.locator("nav.sohn-nav").getByRole("link", { name: "Pfad", exact: true }).click();
    await expect(sohn.locator(".pfad-knoten")).toHaveAccessibleName(/Ziel erfüllt/);
    await sohnKontext.close();
  });

test("Scheitert die Klassenarbeit, steht der Plan, und das Banner nennt die fehlende Arbeit mit Link", async ({ page, request }) => {
  const s = await stoff(request, "B");
  await vaterLogin(page, FATHER);
  await bisZumUeberblick(page, s, iso(8));

  // AK 5: nur das Anlegen der Arbeit scheitert; Plan und Position laufen durch.
  await page.route("**/api/v1/supervisor/class-tests", (route) => route.request().method() === "POST"
    ? route.fulfill({ status: 500, contentType: "application/problem+json",
      body: JSON.stringify({ status: 500, title: "Server error", detail: "B-423 Probe" }) })
    : route.fallback());
  await page.getByRole("button", { name: "✅ Lehrplan erstellen" }).click();

  const kasten = page.getByRole("alert").filter({ hasText: "Der Plan ist angelegt, aber die Klassenarbeit fehlt" });
  await expect(kasten).toBeVisible();
  await expect(kasten.getByRole("link", { name: "Klassenarbeit von Hand anlegen" }))
    .toHaveAttribute("href", `/vater/class-tests?childId=${s.childId}`);
  await expect(kasten.getByRole("link", { name: "Zum Plan" })).toHaveAttribute("href", /\/vater\/plan\/\d+$/);

  // Der zweite Anlauf legt nur die Arbeit an – kein zweiter Plan (Risiko 3).
  await page.unroute("**/api/v1/supervisor/class-tests");
  const planPosts: string[] = [];
  page.on("request", (r) => { if (r.method() === "POST" && r.url().endsWith("/api/v1/supervisor/study-plans")) planPosts.push(r.url()); });
  await page.getByRole("button", { name: "Klassenarbeit erneut anlegen" }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  expect(planPosts, "Der zweite Anlauf darf keinen zweiten Plan anlegen").toHaveLength(0);
});
