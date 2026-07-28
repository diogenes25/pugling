import { beforeEach, describe, expect, it } from "vitest";
import {
  PERSPECTIVES, perspective, perspectiveOfPath, rememberPerspective, rememberedPerspective,
} from "./navigation";

/*
 * Die Zuordnung Pfad → Perspektive entscheidet, welche Navigation der Vater sieht. Sie ist reine Logik mit
 * zwei Fallen, die im Browser nur als „falsche Nav" auffallen würden: `/vater` ist Präfix von allem, und
 * `/vater/plaene` sieht `/vater/plan` zum Verwechseln ähnlich.
 */
describe("perspectiveOfPath", () => {
  it("ordnet die Startseiten ihrer eigenen Perspektive zu", () => {
    expect(perspectiveOfPath("/vater")).toBe("betreuen");
    expect(perspectiveOfPath("/vater/plaene")).toBe("zuweisen");
    expect(perspectiveOfPath("/vater/inhalte")).toBe("erstellen");
  });

  it("hält `/vater` auf exaktem Treffer – sonst gewönne es jede Unterseite", () => {
    expect(perspectiveOfPath("/vater/vocab")).toBe("erstellen");
    expect(perspectiveOfPath("/vater/shop")).toBe("betreuen");
  });

  it("trennt Plan-Liste und einzelnen Plan trotz gemeinsamer Anfangszeichen", () => {
    expect(perspectiveOfPath("/vater/plaene")).toBe("zuweisen");
    expect(perspectiveOfPath("/vater/plan/7")).toBe("zuweisen");
    expect(perspectiveOfPath("/vater/plan/new")).toBe("zuweisen");
  });

  it("zieht Unterseiten in die Perspektive ihres Bereichs", () => {
    expect(perspectiveOfPath("/vater/exercises")).toBe("erstellen");
    expect(perspectiveOfPath("/vater/exercises/neu")).toBe("erstellen");
    expect(perspectiveOfPath("/vater/kind/3")).toBe("betreuen");
    expect(perspectiveOfPath("/vater/kind/3/lernstand")).toBe("betreuen");
    expect(perspectiveOfPath("/vater/kind/3/ziele")).toBe("betreuen");
  });

  it("wählt bei mehreren Treffern das längste Präfix", () => {
    // `/vater/kind` (betreuen) und `/vater` (betreuen) treffen beide; entscheidend ist, dass ein längeres
    // Präfix ein kürzeres schlägt – sonst landete `/vater/exercises/neu` irgendwo.
    expect(perspectiveOfPath("/vater/exercises/neu")).toBe("erstellen");
  });

  it("fällt für Konto-Seiten auf Betreuen zurück", () => {
    expect(perspectiveOfPath("/vater/profil")).toBe("betreuen");
    expect(perspectiveOfPath("/vater/gibt-es-nicht")).toBe("betreuen");
  });
});

describe("PERSPECTIVES", () => {
  it("führt jede Startseite als eigenen Eintrag mit `end`", () => {
    for (const p of PERSPECTIVES) {
      const home = p.entries.find((e) => e.to === p.home);
      expect(home, `Startseite von ${p.key} fehlt in den Einträgen`).toBeDefined();
      // Ohne `end` wäre der Startseiten-Eintrag auf jeder Unterseite mit-aktiv – zwei aktive Einträge.
      expect(home!.end, `Startseite von ${p.key} braucht end`).toBe(true);
    }
  });

  it("gibt jedem Eintrag ein Symbol und einen eindeutigen Pfad", () => {
    const all = PERSPECTIVES.flatMap((p) => p.entries);
    expect(new Set(all.map((e) => e.to)).size).toBe(all.length);
    for (const e of all) expect(e.label).not.toMatch(/^[A-Za-zÄÖÜ]/);
  });

  it("findet jede Perspektive über ihren Schlüssel", () => {
    for (const p of PERSPECTIVES) expect(perspective(p.key)).toBe(p);
  });
});

describe("gemerkte Perspektive", () => {
  beforeEach(() => localStorage.clear());

  it("ist ohne Entscheidung leer", () => {
    expect(rememberedPerspective()).toBeNull();
  });

  it("gibt zurück, was gewählt wurde", () => {
    rememberPerspective("erstellen");
    expect(rememberedPerspective()).toBe("erstellen");
  });

  it("verwirft einen ungültigen Eintrag statt ihn weiterzureichen", () => {
    // Ein alter oder von Hand verbogener Wert würde sonst in `perspective(...)` auf `undefined` laufen –
    // und die Anmeldung führte ins Nichts.
    localStorage.setItem("pugling.vater.perspective", "verwalten");
    expect(rememberedPerspective()).toBeNull();
  });
});
