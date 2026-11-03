import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-68: das Vater-Web bei Tablet-Breite (768 px, hochkant). Die Zusage: keine Seite scrollt seitlich. Eine
 * breite Tabelle darf in ihrem eigenen Streifen bzw. in `.vater-main` scrollen, das Dokument selbst wird nie
 * breiter als das Fenster – auch nicht, wenn der Vater ein ⓘ aufklappt (B-263).
 *
 * Gemessen je Route als `document.documentElement.scrollWidth <= window.innerWidth`. Das Projekt fährt
 * `Desktop Chrome` (überschreibt die 393 px aus playwright.config.ts), darum setzt die Spec die Breite selbst.
 *
 * Was sie rot macht (gemessen, B-68/B-263 im Verlauf): ohne die Lage-Korrektur in `InfoHint` melden 22 ⓘ bei
 * 768 px rechts über (Popover-Rechtskante bis 981 px) – gemessen an `overRight` gegen das Fenster; ein
 * `InfoHint`, der jedes Popover verschiebt, fällt am Ruhelage-Schritt. Die Seiten selbst liefen mit dem Seed
 * schon vor B-68 nicht über – die Routen-Messung und `overBottom` sind Wächter, keine Belege eines alten Fehlers.
 */

const FATHER = { id: "1", pin: "0000" };
const WIDTH = 768;

/** Die Elemente, die am weitesten überstehen – in der Fehlermeldung, damit der Schuldige zu sehen ist. */
async function overflow(page: Page) {
  return page.evaluate((width) => {
    const culprits = [...document.querySelectorAll("body *")]
      .map((el) => ({ el, right: el.getBoundingClientRect().right }))
      .filter(({ right }) => right > width + 1)
      .sort((a, b) => b.right - a.right)
      .slice(0, 3)
      .map(({ el, right }) => `${el.tagName.toLowerCase()}.${String(el.className).split(" ")[0]} (${Math.round(right)})`);
    return { scrollWidth: document.documentElement.scrollWidth, innerWidth: window.innerWidth, culprits };
  }, WIDTH);
}

/**
 * Lage des offenen Popovers im Fenster. Seit B-263 steht es fest zum Fenster (`position: fixed`); ob es irgendwo
 * abgeschnitten wird, zeigt darum nicht eine Koordinate, sondern der Treffertest an seinen Ecken.
 */
async function popoverFit(page: Page) {
  return page.evaluate(() => {
    const main = document.querySelector(".vater-main") as HTMLElement;
    const pop = document.querySelector(".info-hint-pop") as HTMLElement;
    const p = pop.getBoundingClientRect();
    // 16 px nach innen (runde Ecken, Scrollbalken-Trefferzone) – Begründung bei `INSET` in feldhilfe-lage.spec.ts.
    const corners = [[p.left + 16, p.top + 16], [p.right - 16, p.top + 16], [p.left + 16, p.bottom - 16], [p.right - 16, p.bottom - 16]];
    return {
      overRight: Math.round(p.right - document.documentElement.clientWidth),
      overLeft: Math.round(-p.left),
      hidden: corners.filter(([x, y]) => !document.elementsFromPoint(x, y).some((h) => pop.contains(h))).length,
      // Ein fest stehendes Popover zieht keinen Scrollbalken in `.vater-main` auf; tut es das doch, ist es
      // wieder in den Fluss gerutscht.
      overBottom: main.scrollHeight - main.clientHeight,
    };
  });
}

async function expectFits(page: Page, route: string) {
  await page.goto(route);
  await expect(page.locator(".vater-main h1, .vater-main h2").first()).toBeVisible();
  // Listen laden nach der Überschrift; gemessen wird erst, wenn keine Anfrage mehr läuft.
  await page.waitForLoadState("networkidle");
  const m = await overflow(page);
  expect.soft(m.scrollWidth, `${route}: ${m.culprits.join(", ")}`).toBeLessThanOrEqual(m.innerWidth);

  const hints = page.locator(".vater-main .info-hint-btn");
  for (let i = 0; i < await hints.count(); i++) {
    const hint = hints.nth(i);
    if (!(await hint.isVisible())) continue;
    const name = await hint.getAttribute("aria-label");
    await hint.click();
    await expect(page.locator(".info-hint-pop")).toBeVisible();
    const open = await overflow(page);
    expect.soft(open.scrollWidth, `${route} ${name} offen: ${open.culprits.join(", ")}`).toBeLessThanOrEqual(open.innerWidth);
    const fit = await popoverFit(page);
    expect.soft(fit.overRight, `${route} ${name}: Popover rechts abgeschnitten`).toBeLessThanOrEqual(0);
    expect.soft(fit.overLeft, `${route} ${name}: Popover links abgeschnitten`).toBeLessThanOrEqual(0);
    expect.soft(fit.overBottom, `${route} ${name}: Popover wieder im Fluss (main scrollt)`).toBeLessThanOrEqual(0);
    expect.soft(fit.hidden, `${route} ${name}: Ecken des Popovers abgeschnitten`).toBe(0);
    await page.keyboard.press("Escape");
    await expect(page.locator(".info-hint-pop")).toHaveCount(0);
  }
}

test.use({ viewport: { width: WIDTH, height: 1024 } });

test("Vater-Web bei 768 px: keine Seite scrollt seitlich, auch nicht mit offenem ⓘ", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // Die Detailseite braucht einen Plan des eingeloggten Vaters, nicht eine geratene Id. Hat er keinen, legt
  // die Spec einen inaktiven an (inaktiv, damit die Sperre „ein aktiver Plan je Kind" nicht greift).
  const planId = await page.evaluate(async () => {
    const headers = { Authorization: `Bearer ${localStorage.getItem("pugling.token")}`, "Content-Type": "application/json" };
    const plans = (await (await fetch("/api/v1/supervisor/study-plans", { headers })).json()) as { id: number }[];
    if (plans.length > 0) return plans[0].id;
    // Index mit Absicht (B-357): jeder eigene Plan und jedes eigene Kind trägt die Detailseite, keines ist gemeint. Ein
    // Vater ohne Kind scheitert mit diesem Grund, nicht mit einem TypeError aus `kids[0]`.
    const kids = (await (await fetch("/api/v1/supervisor/children", { headers })).json()) as { id: number }[];
    if (kids.length === 0) throw new Error("Vater 1 hat weder einen Plan noch ein Kind – der Seed fehlt.");
    const res = await fetch("/api/v1/supervisor/study-plans", {
      method: "POST", headers,
      body: JSON.stringify({ childId: kids[0].id, title: "Tablet-Probe", durationDays: 7, active: false }),
    });
    return ((await res.json()) as { id: number }).id;
  });
  expect(planId, "Plan des Vaters gefunden oder angelegt").toBeTruthy();

  const routes = [
    "/vater",
    "/vater/plaene",
    "/vater/inhalte",
    "/vater/exercises",
    "/vater/exercises/neu",
    "/vater/katalog",
    "/vater/lueckentexte",
    "/vater/vocab",
    "/vater/media",
    "/vater/lehrwerke",
    "/vater/fachlehrer",
    "/vater/profil",
    "/vater/rewards",
    "/vater/shop",
    "/vater/konto",
    "/vater/class-tests",
    "/vater/plan/new",
    "/vater/wizard",
    "/vater/kind/1",
    "/vater/kind/1/lernstand",
    "/vater/kind/1/ziele",
    `/vater/plan/${planId}`,
  ];
  for (const route of routes) {
    await test.step(route, () => expectFits(page, route));
  }

  // Wo Platz ist, bleibt das Popover, wo es immer war (B-263 AK 2): links am Knopf, darunter. Rot, wenn die
  // Verschiebung jedes Popover verrückt statt nur die überstehenden.
  await test.step("Ruhelage bleibt, wo Platz ist", async () => {
    await page.goto("/vater/plan/new");
    const hint = page.locator(".vater-main .info-hint").first();
    await hint.locator(".info-hint-btn").click();
    const lage = await hint.evaluate((wrap) => {
      const pop = wrap.querySelector(".info-hint-pop") as HTMLElement;
      const w = wrap.getBoundingClientRect();
      const p = pop.getBoundingClientRect();
      return { dx: Math.round(p.left - w.left), unten: p.top >= w.bottom };
    });
    expect(lage).toEqual({ dx: -6, unten: true });
  });
});

// B-276: die Kernregel von B-68 (`.vater-main { overflow-x: auto }`) trug kein Fall mehr – mit dem Seed läuft
// keine Route über, und das ⓘ steht seit B-263 fest zum Fenster. Gemessen: ohne die Regel blieben diese Spec und
// `feldhilfe-lage.spec.ts` grün (7 passed). Darum eine breite Tabelle ohne eigenen Rahmen als Probe in die echte Seite: der
// Seiten-Container muss sie in sich scrollen, das Fenster nicht. Rot, wenn `overflow-x: auto` fehlt (gemessen, an „scrollt
// seitlich") – dann behält `.vater-main` seine Breite, die Tabelle läuft aus ihm heraus, und `html { overflow-x: hidden }`
// schneidet sie unerreichbar ab. Ein `min-width: 0` daneben trug nichts (gemessen, darum gestrichen; Grund B-363).
test("Vater-Web bei 768 px: eine breite Tabelle scrollt in .vater-main, nicht im Fenster", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.setViewportSize({ width: WIDTH, height: 900 });
  await page.goto("/vater");
  const lage = await page.locator(".vater-main").evaluate((main) => {
    const table = document.createElement("table");
    table.className = "table";
    table.innerHTML = `<tr><td>Spalte</td><td style="min-width: 1600px">sehr breit</td></tr>`;
    main.appendChild(table);
    main.scrollLeft = 200;
    return {
      mainBreite: main.clientWidth,
      mainInhalt: main.scrollWidth,
      verschoben: main.scrollLeft,
      dokument: document.documentElement.scrollWidth,
      senkrecht: main.scrollHeight > main.clientHeight,
    };
  });
  // Vorbedingungen, keine Belege (B-363): beide gelten auch ohne die Regel – die erste ist eine Eigenschaft der Probe, die
  // zweite gilt, weil `.vater-main` auf der Querachse nie mit seinem Inhalt wächst.
  expect(lage.mainInhalt, "Vorbedingung: die Tabelle ist breiter als der Seiten-Container").toBeGreaterThan(lage.mainBreite);
  expect(lage.mainBreite, "Vorbedingung: der Seiten-Container bleibt im Fenster").toBeLessThanOrEqual(WIDTH);
  expect(lage.verschoben, "der Seiten-Container scrollt seitlich").toBeGreaterThan(0);
  expect(lage.dokument, "das Dokument wird nicht breiter").toBeLessThanOrEqual(WIDTH);
  // Mitgemessen, nicht zugesichert (B-276, offene Frage): ob `.vater-main` auch senkrecht scrollt.
  test.info().annotations.push({ type: "senkrecht", description: String(lage.senkrecht) });
});
