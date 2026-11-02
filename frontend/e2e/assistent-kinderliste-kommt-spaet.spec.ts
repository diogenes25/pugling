import { test, expect } from "@playwright/test";

/*
 * B-177: Kommt die Kinderliste erst an, NACHDEM der Vater angefangen hat, ein neues Kind einzutragen, darf
 * der Assistent ihn nicht still auf das erste bestehende Kind umschalten. Bis B-177 tat er das, solange
 * niemand die Pille „Neues Kind anlegen" angeklickt hatte: der Effekt, der „bestehendes Kind" vorwählt,
 * feuerte auf die späte Liste – auch noch in Schritt 2 oder 3, wo der Vater es nicht mehr sieht –, und der
 * Plan wäre an ein anderes Kind gegangen. Nebenbei verschob derselbe Wechsel den Filter-Schlüssel der
 * Übungssuche mitten in einer Abfrage, die Lage, die B-177 ursprünglich beschrieb.
 *
 * Eigene Datei (frontend/CLAUDE.md, „Eine Fläche je E2E-Spec"): der Fall braucht eine angehaltene
 * Kinderliste, und die soll keinen anderen Ablauf verlangsamen.
 */
test("Eine spät ankommende Kinderliste nimmt dem Vater sein neues Kind nicht weg", async ({ page }) => {
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();

  // Die Liste bis zur Freigabe anhalten – nur sie, alles andere läuft normal.
  let freigeben!: () => void;
  const freigegeben = new Promise<void>((resolve) => { freigeben = resolve; });
  await page.route("**/api/v1/supervisor/children", async (route) => { await freigegeben; await route.continue(); });

  await page.goto("/vater/wizard");
  // Eintragen ohne die Pille anzuklicken – genau der Weg, auf dem der Effekt bisher noch zuschlug.
  await page.locator("#wiz-name").fill("Spät-Neu");
  await page.locator("#wiz-pin").fill("1357");
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  // B-348, dieselbe Regel an den Ziel-Karten: die Vorbelegung und ein Wechsel stehen am Radio, nicht nur im Rahmen, und
  // die Gruppe trägt ihre Frage als Namen.
  await expect(page.getByRole("radiogroup", { name: "Was ist das Ziel?" })).toBeVisible();
  await expect(page.getByRole("radiogroup", { name: "Wie intensiv soll geübt werden?" })).toBeVisible();
  const klassenarbeit = page.getByRole("radio", { name: /Klassenarbeit vorbereiten/ });
  const aufholen = page.getByRole("radio", { name: /Rückstand aufholen/ });
  await expect(klassenarbeit).toHaveAttribute("aria-checked", "true");
  await aufholen.click();
  await expect(aufholen).toHaveAttribute("aria-checked", "true");
  await expect(klassenarbeit).toHaveAttribute("aria-checked", "false");
  // Dasselbe an der Intensität (Nachschau B-348): ein Aufrufer, der `active` falsch verdrahtet, ließe sonst drei
  // ungewählte Optionen ansagen. Vorbelegt ist „Normal".
  const normal = page.getByRole("radio", { name: /^Normal/ });
  const intensiv = page.getByRole("radio", { name: /^Intensiv/ });
  await expect(normal).toHaveAttribute("aria-checked", "true");
  await intensiv.click();
  await expect(intensiv).toHaveAttribute("aria-checked", "true");
  await expect(normal).toHaveAttribute("aria-checked", "false");

  // Jetzt kommt die Liste – während der Vater schon in Schritt 2 ist und den Wechsel nicht sähe.
  const liste = page.waitForResponse((r) => r.url().endsWith("/api/v1/supervisor/children") && r.ok());
  freigeben();
  await liste;

  // Zurück zu Schritt 1: das eingetragene neue Kind steht noch da. Vorher stand hier das Pulldown mit dem
  // ersten bestehenden Kind, und `#wiz-name` war verschwunden.
  await page.getByRole("button", { name: "Zurück" }).click();
  // Erst belegen, dass React die Liste verarbeitet hat: die Pille ist gesperrt, solange keine Kinder da
  // sind. Ohne diese Wartezeile könnten die zwei Zusicherungen darunter vor dem Umschalten grün werden.
  await expect(page.getByRole("radio", { name: "Bestehendes Kind" })).toBeEnabled();
  await expect(page.locator("#wiz-name")).toHaveValue("Spät-Neu");
  await expect(page.getByRole("combobox", { name: "Kind" })).toHaveCount(0);
  await expect(page.getByRole("radio", { name: "Neues Kind anlegen" })).toHaveAttribute("aria-checked", "true");
  await expect(page.getByRole("radio", { name: "Bestehendes Kind" })).toHaveAttribute("aria-checked", "false");
});

/*
 * B-262 (AK 2 von B-177): Ohne Eingabe wählt der Assistent weiter das bestehende Kind vor – auch wenn die Liste spät
 * kommt. Der Fall darüber belegt nur die Sperre; ein Fix, der `touchedMode` zu früh setzt (schon beim Rendern),
 * bestünde ihn und nähme dem Vater ohne neues Kind die Vorwahl. Rot, wenn `touchedMode` mit `true` beginnt.
 */
test("Ohne Eingabe wählt der Assistent nach der späten Liste das bestehende Kind vor", async ({ page }) => {
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();

  let freigeben!: () => void;
  const freigegeben = new Promise<void>((resolve) => { freigeben = resolve; });
  await page.route("**/api/v1/supervisor/children", async (route) => { await freigegeben; await route.continue(); });

  await page.goto("/vater/wizard");
  // Ausgangslage: ohne Liste steht das Neues-Kind-Formular – die Vorwahl unten ist also ein Wechsel, kein Startzustand.
  await expect(page.locator("#wiz-name")).toBeVisible();
  freigeben();

  await expect(page.getByRole("combobox", { name: "Kind" })).toBeVisible();
  await expect(page.getByRole("combobox", { name: "Kind" })).not.toHaveValue("");
  // B-348: die Wahl steht auch für einen Screenreader an der Pille, nicht nur in ihrer Farbe – als Radio einer benannten
  // Gruppe, wie die übrigen Einfachauswahlen des Vater-Webs.
  await expect(page.getByRole("radiogroup", { name: "Welches Kind?" })).toBeVisible();
  await expect(page.getByRole("radio", { name: "Bestehendes Kind" })).toHaveAttribute("aria-checked", "true");
  await expect(page.getByRole("radio", { name: "Neues Kind anlegen" })).toHaveAttribute("aria-checked", "false");
  await expect(page.locator("#wiz-name")).toHaveCount(0);
});
