import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";
import { formatDate, heuteImLerntag } from "../src/lib/datum";

/*
 * B-734 (Rollengang Vater und Sohn, AK 7 und 11): der Vater setzt im Kind-Hub unter „Lerntage“ die Leitplanken, schaltet den
 * Tagesplaner im Schattenmodus ein und liest den heutigen Plan – die Aufgabe mit ihrer Begründung und dem ausdrücklichen
 * Hinweis, dass der Plan nicht wirkt. Der Tag des Sohns antwortet danach genau wie vorher. Die Regeln selbst (fällige
 * Wiederholungen zuerst, Gescheitertes leichter, Boden und Deckel) belegen `TagesplanerRegelnTests` und `TagesplanerTests`.
 *
 * Der Plan entsteht ab 6 Uhr deutscher Zeit und nie sonntags: davor bzw. sonntags läuft der Fall nicht (Skip mit Grund),
 * denn die echte Uhr des Servers lässt sich hier nicht stellen.
 *
 * Rot, wenn der Schalter nicht an den Server geht, die Ansicht den Plan nicht vom Server liest, eine Aufgabe ohne
 * Begründung erscheint, ein Schattenplan als wirksam beschriftet ist oder der Schattenplan den Tag des Sohns verändert.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const WOERTER = [[`sun${RUN}`, "Sonne"], [`moon${RUN}`, "Mond"], [`star${RUN}`, "Stern"]] as const;

type Kopf = Record<string, string>;

async function postId(request: APIRequestContext, url: string, headers: Kopf, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

function berlinJetzt(): { stunde: number; sonntag: boolean } {
  const teile = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/Berlin", hour: "2-digit", hourCycle: "h23", weekday: "short" })
    .formatToParts(new Date());
  return {
    stunde: Number(teile.find((t) => t.type === "hour")?.value),
    sonntag: teile.find((t) => t.type === "weekday")?.value === "Sun",
  };
}

test("Der Vater liest den Schattenplan mit Begründung, der Tag des Sohns bleibt gleich", async ({ page, request }) => {
  const jetzt = berlinJetzt();
  test.skip(jetzt.stunde < 6, "Der Tagesplan entsteht erst ab 6 Uhr deutscher Zeit.");
  test.skip(jetzt.sonntag, "Sonntag ist kein Lerntag – der Planer macht keinen Plan.");

  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "6" + String(Date.now() % 1000).padStart(3, "0");
  const name = `Planer ${RUN}`;
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Planerfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Planer-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const titel = `Himmel ${RUN}`;
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: titel, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WOERTER.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: true });

  // Der Tag des Sohns, bevor es einen Planer gibt.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin: childPin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const todayUrl = `/api/v1/student/children/${childId}/today`;
  const vorher = await (await request.get(todayUrl, { headers: kind })).json();

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/kind/${childId}?bereich=lerntage`);
  // Ohne Leitplanken schaltet nichts.
  await expect(page.getByRole("button", { name: "Im Schattenmodus einschalten" })).toBeDisabled();

  const felder: [string, string][] = [
    ["Pflichtaufgaben am Tag – mindestens", "1"], ["Pflichtaufgaben am Tag – höchstens", "4"],
    ["Neue Wörter am Tag – mindestens", "2"], ["Neue Wörter am Tag – höchstens", "6"],
    ["Malus je verpasster Pflichtaufgabe (🪙)", "0"],
  ];
  for (const [feld, wert] of felder) await page.getByLabel(feld, { exact: true }).fill(wert);
  await page.getByRole("button", { name: "Speichern", exact: true }).click();
  await expect(page.getByText("Leitplanken gespeichert.")).toBeVisible();

  await page.getByRole("button", { name: "Im Schattenmodus einschalten" }).click();
  await expect(page.getByText("Status: im Schattenmodus", { exact: false })).toBeVisible();

  const heute = formatDate(heuteImLerntag());
  const plan = page.getByRole("article", { name: `Tagesplan ${heute}` });
  await expect(plan).toContainText(`Schattenplan – wirkt nicht auf Pflicht, Malus und die App von ${name}.`);
  const zeile = plan.getByRole("row").filter({ hasText: titel });
  await expect(zeile).toContainText(`Planerfach ${RUN}`);
  // Drei Wörter, nie gezeigt; der Boden von 2 neuen Wörtern ist das Budget, ein neues Kind hat noch kein Tempo.
  await expect(zeile).toContainText("2 neue Wörter (Boden 2, noch kein Tempo)");

  // Der Server hat den Plan, und der Tag des Sohns antwortet wie vorher.
  const plaene = await (await request.get(`/api/v1/supervisor/children/${childId}/daily-plans`, { headers: vater })).json();
  expect(plaene).toHaveLength(1);
  expect(plaene[0]).toMatchObject({ mode: "Shadow", newWordsBudget: 2 });
  const nachher = await (await request.get(todayUrl, { headers: kind })).json();
  expect(nachher).toEqual(vorher);
});
