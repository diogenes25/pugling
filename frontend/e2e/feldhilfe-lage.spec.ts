import { test, expect, type Locator, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-263: wo ein aufgeklapptes ⓘ steht. Seit der Morgen-Entscheidung vom 2026-10-26 hängt das Popover fest am
 * Fenster (`position: fixed`), damit kein Rahmen mit beschnittenem Überlauf es abschneidet. Die Spec prüft die
 * Folgen davon, die `responsive-vater.spec.ts` nicht berührt:
 *
 * 1. Im `overflowX`-Rahmen der Lückentext-Tabelle ist das Popover ganz zu sehen – per Treffertest an den Ecken
 *    (ein abgeschnittener Teil hat Koordinaten, liegt aber unter keinem Punkt) und per Koordinaten im Fenster.
 * 2. Steht der Knopf am unteren Fensterrand, klappt es über den Knopf (AK 1, „dort klappt es über den Knopf").
 * 3. Passt es weder unten noch oben (Querformat), steht es trotzdem ganz im Fenster – herscrollen ginge nicht;
 *    ist es höher als das Fenster, scrollt es in sich.
 * 4. Scrollen und Größenänderung schließen es – sonst liefe es, fest am Fenster, vom Knopf weg. Ein Scrollen von
 *    VOR dem Öffnen schließt es nicht.
 */

const FATHER = { id: "1", pin: "0000" };
/**
 * Abstand der Prüfpunkte von den Rändern des Popovers. 16 px, weil zwei Dinge in einem schmaleren Saum den
 * Treffertest verfälschen, ohne dass etwas abgeschnitten ist (beide gemessen bei 768 px): die abgerundeten Ecken
 * (`border-radius: 12px` – ein Punkt 3 px vom Eck liegt außerhalb der Rundung) und rechts die Trefferzone des
 * überlagernden Scrollbalkens von `.vater-main`, die im Headless-Chromium nur den Rahmen und seine Vorfahren
 * meldet. Über den Fensterrand prüft `overRight` in responsive-vater.spec.ts.
 */
const INSET = 16;

/**
 * Je Ecke des offenen Popovers (um {@link INSET} nach innen): liegt das Popover dort überhaupt unter dem Punkt?
 * Ein Rahmen, der es abschneidet, nimmt es an dieser Stelle aus `elementsFromPoint` heraus.
 */
async function fullyVisible(page: Page) {
  return page.evaluate((inset) => {
    const pop = document.querySelector(".info-hint-pop") as HTMLElement;
    const r = pop.getBoundingClientRect();
    const corners = [
      [r.left + inset, r.top + inset], [r.right - inset, r.top + inset],
      [r.left + inset, r.bottom - inset], [r.right - inset, r.bottom - inset],
    ];
    return corners.map(([x, y]) => document.elementsFromPoint(x, y).some((hit) => pop.contains(hit)));
  }, INSET);
}

/**
 * Steht das offene Popover mit dem Randabstand von 8 px (`EDGE` in InfoHint.tsx) ganz im Fenster? Genau gemessen,
 * nicht per Treffertest – der sieht wegen {@link INSET} nichts, was weniger als 16 px abschneidet.
 */
async function imFenster(page: Page) {
  return page.locator(".info-hint-pop").evaluate((pop) => {
    const r = pop.getBoundingClientRect();
    const { clientWidth: w, clientHeight: h } = document.documentElement;
    return r.left >= 8 && r.top >= 8 && r.right <= w - 8 && r.bottom <= h - 8;
  });
}

async function openHint(btn: Locator) {
  await btn.click();
  await expect(btn.page().locator(".info-hint-pop")).toBeVisible();
}

test.beforeEach(async ({ page }) => {
  await vaterLogin(page, FATHER);
});

test("ⓘ in der Lückentext-Tabelle ist ganz zu sehen, obwohl ihr Rahmen den Überlauf beschneidet", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto("/vater/lueckentexte");
  // Eine Lücke: die Tabelle ist dann eine Zeile hoch und damit niedriger als das Popover – genau der Fall,
  // in dem es vorher unten abgeschnitten wurde.
  await page.locator("#cz-text").fill("Good {{1}}, Tom.");
  await page.locator("#cz-text").blur();
  const btn = page.locator("th .info-hint-btn");
  await expect(btn).toBeVisible();

  await openHint(btn);
  // Vorbedingung, die den Fall trägt: der Rahmen um die Tabelle beschneidet wirklich, und das Popover ragt über
  // seinen oberen oder unteren Rand hinaus (je nachdem, ob es geklappt ist). Ohne sie bewiese das Grün nichts.
  // Gemessen erst nach dem Klick – der rollt den Knopf ins Bild und verschiebt damit alle Koordinaten.
  const frame = await btn.evaluate((el) => {
    const box = el.closest("table")!.parentElement!;
    const pop = document.querySelector(".info-hint-pop")!;
    const p = pop.getBoundingClientRect();
    const b = box.getBoundingClientRect();
    return { overflowX: getComputedStyle(box).overflowX, ragtHeraus: p.bottom > b.bottom || p.top < b.top };
  });
  expect(frame).toEqual({ overflowX: "auto", ragtHeraus: true });
  expect(await fullyVisible(page)).toEqual([true, true, true, true]);
  expect(await imFenster(page)).toBe(true);
});

test("ⓘ am unteren Fensterrand klappt über den Knopf", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 600 });
  await page.goto("/vater/exercises/neu");
  const btn = page.locator(".vater-main .info-hint-btn").first();
  await expect(btn).toBeVisible();
  // Den Knopf an den unteren Fensterrand bringen. Die Seite ist höher als 600 px, also scrollt das wirklich.
  await btn.evaluate((el) => el.scrollIntoView({ block: "end" }));

  await openHint(btn);
  const lage = await page.evaluate(() => {
    const pop = document.querySelector(".info-hint-pop") as HTMLElement;
    const knopf = document.querySelector(".info-hint-btn[aria-expanded='true']") as HTMLElement;
    const p = pop.getBoundingClientRect();
    const k = knopf.getBoundingClientRect();
    return {
      // Vorbedingung: unter dem Knopf wäre nicht genug Platz – sonst wäre „oben" nicht verlangt.
      untenZuEng: k.bottom + 6 + p.height > document.documentElement.clientHeight - 8,
      oben: p.bottom <= k.top,
      klasse: pop.classList.contains("up"),
    };
  });
  expect(lage).toEqual({ untenZuEng: true, oben: true, klasse: true });
  expect(await fullyVisible(page)).toEqual([true, true, true, true]);
  expect(await imFenster(page)).toBe(true);
});

test("ⓘ im Querformat: passt es weder unten noch oben, steht es trotzdem ganz im Fenster", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 360 });
  await page.goto("/vater/exercises/neu");
  const btn = page.locator(".vater-main .info-hint-btn").first();
  await expect(btn).toBeVisible();
  await btn.evaluate((el) => el.scrollIntoView({ block: "center" }));

  await openHint(btn);
  // Vorbedingung: weder unter noch über dem Knopf ist Platz. Sonst prüfte der Fall nur das Klappen nach.
  const eng = await page.evaluate(() => {
    const p = (document.querySelector(".info-hint-pop") as HTMLElement).getBoundingClientRect();
    const k = (document.querySelector(".info-hint-btn[aria-expanded='true']") as HTMLElement).getBoundingClientRect();
    const h = document.documentElement.clientHeight;
    return { unten: k.bottom + 6 + p.height > h - 8, oben: k.top - 6 - p.height < 8 };
  });
  expect(eng).toEqual({ unten: true, oben: true });
  expect(await imFenster(page)).toBe(true);
});

test("ⓘ in einem Fenster, das niedriger ist als die Erklärung: es scrollt in sich, statt überzustehen", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 200 });
  await page.goto("/vater/exercises/neu");
  const btn = page.locator(".vater-main .info-hint-btn").first();
  await btn.evaluate((el) => el.scrollIntoView({ block: "center" }));

  await openHint(btn);
  const pop = await page.locator(".info-hint-pop").evaluate((el) => ({
    // Vorbedingung: der Text ist höher als das Fenster abzüglich Rand – sonst griffe die Höhenbegrenzung nie.
    hoeherAlsFenster: el.scrollHeight > document.documentElement.clientHeight - 16,
    scrolltInSich: el.scrollHeight > el.clientHeight,
  }));
  expect(pop).toEqual({ hoeherAlsFenster: true, scrolltInSich: true });
  expect(await imFenster(page)).toBe(true);
});

test("ein Scrollen von vor dem Öffnen schließt das ⓘ nicht", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 600 });
  await page.goto("/vater/exercises/neu");
  const btn = page.locator(".vater-main .info-hint-btn").last();
  // Vorbedingung: der Knopf liegt unter dem Falz. Der Klick rollt ihn erst ins Bild, und dieses Scrollen kommt
  // als Ereignis erst nach dem Öffnen an – genau der Fall, in dem ein Schließen bei jedem `scroll` fehlginge.
  const unterDemFalz = await btn.evaluate((el) => el.getBoundingClientRect().top > document.documentElement.clientHeight);
  expect(unterDemFalz).toBe(true);
  await openHint(btn);
  // Zwei Frames abwarten: so lange braucht ein nachgereichtes `scroll`, um anzukommen.
  await page.evaluate(() => new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r))));
  await expect(page.locator(".info-hint-pop")).toBeVisible();
});

test("Scrollen und Größenänderung schließen ein offenes ⓘ", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 600 });
  await page.goto("/vater/exercises/neu");
  const btn = page.locator(".vater-main .info-hint-btn").first();
  await openHint(btn);

  // Scrollen in dem Rahmen, der hier tatsächlich scrollt (seit B-68 `.vater-main`, sonst das Dokument).
  await page.evaluate(() => {
    const main = document.querySelector(".vater-main") as HTMLElement;
    const scroller = main.scrollHeight > main.clientHeight ? main : document.scrollingElement!;
    // 3 px: schon eine kleine Bewegung muss schließen, nicht erst eine große.
    scroller.scrollTop += 3;
  });
  await expect(page.locator(".info-hint-pop")).toHaveCount(0);
  await expect(btn).toHaveAttribute("aria-expanded", "false");

  await openHint(btn);
  await page.setViewportSize({ width: 700, height: 600 });
  await expect(page.locator(".info-hint-pop")).toHaveCount(0);
});
