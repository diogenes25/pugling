import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * Sprint 81 – maschineller Rollengang an der laufenden App für zwei Stories.
 *
 * B-219 (Vater): „Ausrutscher verzeihen“ an einer Leitner-Position setzen, speichern, neu laden – das Häkchen steht noch,
 * und die Zeile sagt „· verzeihend“. Rot, wenn das Feld nicht gesendet, nicht gespeichert oder nicht zurückgelesen wird,
 * oder die Zeile es nicht nennt. Ohne Leitner ist das Häkchen ausgegraut (rot, wenn `disabled` fehlt) und wird mit dem
 * Leitner-Haken bedienbar (rot, wenn es dauerhaft gesperrt wäre).
 *
 * B-588 (Creator → Sohn): im Übungs-Editor die Buchseite „42–43“ eintragen und speichern; nach erneutem Öffnen steht sie
 * im Feld, und der Sohn sieht im Blatt des Knotens „<Reihe> · <Unit> · S. 42–43“. Eine Position ohne Seite zeigt keine
 * Pille – dieselbe Reihe und Unit, damit nur die Seite den Unterschied macht.
 *
 * Eigene Kinder, Reihen und Übungen je Lauf: die DB ist zwischen den Specs geteilt, und je Kind ist ein aktiver Plan spielbar.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

async function vaterHeaders(request: APIRequestContext) {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { Authorization: `Bearer ${(await login.json()).token}` };
}

/** Fach, Reihe und eine Unit; liefert die Units-Route und die Unit-Id. */
async function reiheMitUnit(request: APIRequestContext, vater: Record<string, string>, reihe: string, unit: string) {
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `${reihe} Fach` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, { name: reihe, subjectId });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: unit, orderIndex: 1 });
  return { subjectId, seriesId, units, unitId };
}

function vokabeln(request: APIRequestContext, vater: Record<string, string>, url: string, title: string, front: string, back: string) {
  return postId(request, url, vater, {
    title, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front, back }] },
  });
}

test("B-219: der Vater verzeiht Ausrutscher an einer Leitner-Position, ohne Leitner ist das Häkchen gesperrt", async ({ page, request }) => {
  const vater = await vaterHeaders(request);
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Verzeih ${RUN}`, pin: "8656" });
  const { subjectId, units, unitId } = await reiheMitUnit(request, vater, `Verzeihreihe ${RUN}`, "Unit 1");
  const MIT = `Mit Leitner ${RUN}`, OHNE = `Ohne Leitner ${RUN}`;
  const exMit = await vokabeln(request, vater, `${units}/${unitId}/vocabulary`, MIT, `bird ${RUN}`, "Vogel");
  const exOhne = await vokabeln(request, vater, `${units}/${unitId}/vocabulary`, OHNE, `fish ${RUN}`, "Fisch");
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Verzeihplan ${RUN}`, durationDays: 7, active: true });
  const positions = `/api/v1/supervisor/study-plans/${planId}/positions`;
  await postId(request, positions, vater, { exerciseId: exMit, cadence: "Daily", useLeitner: true });
  await postId(request, positions, vater, { exerciseId: exOhne, cadence: "Daily", useLeitner: false });

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/plan/${planId}`);
  const zeileMit = page.getByRole("row", { name: new RegExp(MIT) });
  const zeileOhne = page.getByRole("row", { name: new RegExp(OHNE) });
  // Die Zeile im Bearbeiten-Modus ist die mit „OK“ – das Anlege-Formular trägt dieselben Felder.
  const bearbeiten = page.getByRole("row").filter({ has: page.getByRole("button", { name: "OK" }) });
  const verzeihen = bearbeiten.getByRole("checkbox", { name: "Ausrutscher verzeihen" });

  // Ausgangslage: Leitner an, aber nicht verzeihend – sonst bewiese das „verzeihend“ unten nichts.
  await expect(zeileMit).toContainText("an · max");
  await expect(zeileMit).not.toContainText("verzeihend");

  await zeileMit.getByRole("button", { name: "Bearbeiten" }).click();
  await expect(verzeihen).toBeEnabled();
  await expect(verzeihen).not.toBeChecked();
  await verzeihen.check();
  await page.getByRole("button", { name: "OK" }).click();
  await expect(page.getByText("Position gespeichert.")).toBeVisible();
  await expect(zeileMit).toContainText("· verzeihend");

  // Nach dem Neuladen kommt es vom Server, nicht aus dem Zustand der Seite.
  await page.reload();
  await expect(zeileMit).toContainText("· verzeihend");
  await expect(zeileOhne).not.toContainText("verzeihend");
  await zeileMit.getByRole("button", { name: "Bearbeiten" }).click();
  await expect(verzeihen).toBeChecked();
  await page.getByRole("button", { name: "Abbrechen" }).click();

  // Ohne Leitner: ausgegraut – und erst der Leitner-Haken gibt es frei.
  await zeileOhne.getByRole("button", { name: "Bearbeiten" }).click();
  await expect(verzeihen).toBeDisabled();
  await bearbeiten.getByRole("checkbox", { name: "Leitner-Kasten" }).check();
  await expect(verzeihen).toBeEnabled();
  await page.getByRole("button", { name: "Abbrechen" }).click();
});

test("B-588: der Creator trägt die Buchseite ein, der Sohn sieht Reihe, Unit und Seite – ohne Seite keine Pille", async ({ page, request }) => {
  const vater = await vaterHeaders(request);
  const pin = "8657";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Buchseite ${RUN}`, pin });
  const REIHE = `Buchreihe ${RUN}`, UNIT = `Unit 3 ${RUN}`;
  const { subjectId, seriesId, units, unitId } = await reiheMitUnit(request, vater, REIHE, UNIT);
  const MIT = `Mit Seite ${RUN}`, OHNE = `Ohne Seite ${RUN}`;
  const exMit = await vokabeln(request, vater, `${units}/${unitId}/vocabulary`, MIT, `tree ${RUN}`, "Baum");
  const exOhne = await vokabeln(request, vater, `${units}/${unitId}/vocabulary`, OHNE, `house ${RUN}`, "Haus");
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Buchplan ${RUN}`, durationDays: 7, active: true });
  const positions = `/api/v1/supervisor/study-plans/${planId}/positions`;
  await postId(request, positions, vater, { exerciseId: exMit, stage: 4, cadence: "Daily", useLeitner: false });
  await postId(request, positions, vater, { exerciseId: exOhne, stage: 4, cadence: "Daily", useLeitner: false });

  // Creator: Seite im Übungs-Editor eintragen und speichern.
  await vaterLogin(page, FATHER);
  await page.goto(`/vater/exercises?seriesId=${seriesId}`);
  await page.getByRole("group", { name: MIT, exact: true }).getByRole("button", { name: "✏️ Bearbeiten" }).click();
  const dialog = page.getByRole("dialog", { name: `Übung bearbeiten: ${MIT}` });
  const seite = dialog.locator("#ed-book-pages");
  await expect(seite).toHaveValue("");
  await seite.fill("42–43");
  await dialog.getByRole("button", { name: "Übung speichern" }).click();
  await expect(dialog.getByText("Gespeichert.")).toBeVisible();
  await dialog.getByRole("button", { name: "Schließen" }).click();
  await expect(dialog).toHaveCount(0);

  // Zurückgelesen: frisch geöffnet steht die Seite im Feld.
  await page.reload();
  await page.getByRole("group", { name: MIT, exact: true }).getByRole("button", { name: "✏️ Bearbeiten" }).click();
  await expect(seite).toHaveValue("42–43");
  await dialog.getByRole("button", { name: "Schließen" }).click();

  // Sohn: der Kopf der Übungsrunde nennt, wo die Übung im Buch steht (B-686: dort statt im weggefallenen Blatt).
  await sohnLogin(page, { id: String(childId), pin });
  const knoten = (titel: string) => page.locator(".pfad-zeile", { hasText: titel }).locator(".pfad-knoten");
  const zaehler = page.locator(".pill.cyan", { hasText: /^Karte \d+ \/ \d+$/ });
  await knoten(MIT).click();
  await expect(page.locator(".pfad-buchbezug")).toHaveText(`${REIHE} · ${UNIT} · S. 42–43`);
  // … und ebenso im Kopf des Tests: führt der Knoten direkt dorthin, sähe das Kind die Seite sonst nirgends.
  await page.goto(page.url().replace("/sohn/practice/", "/sohn/test/"));
  await expect(page.locator(".lektion.test")).toBeVisible();
  await expect(page.locator(".pfad-buchbezug")).toHaveText(`${REIHE} · ${UNIT} · S. 42–43`);

  // Ohne Seite keine Pille – auch nicht die halbe „Reihe · Unit“. Erst wenn die Übersicht geantwortet hat und die Runde
  // steht: vorher stünde die Abwesenheit schon fest, bevor überhaupt gelesen wurde.
  await page.goto("/sohn");
  const uebersicht = page.waitForResponse((r) => /\/study-plans\/\d+\/overview$/.test(new URL(r.url()).pathname));
  await knoten(OHNE).click();
  await uebersicht;
  await expect(zaehler).toBeVisible();
  await expect(page.locator(".pfad-buchbezug")).toHaveCount(0);
  await expect(page.locator(".lektion-unterkopf").getByText(REIHE)).toHaveCount(0);
});
