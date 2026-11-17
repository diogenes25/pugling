import { describe, expect, it } from "vitest";
import { markiereSchreibfehler } from "./schreibfehler";

/** Die Lösung mit den markierten Stellen in eckigen Klammern – so liest sich jeder Fall auf einen Blick. */
const zeige = (gegeben: string, loesung: string) => {
  const s = markiereSchreibfehler(gegeben, loesung);
  return s && s.map((x) => (x.falsch ? `[${x.text}]` : x.text)).join("");
};

describe("markiereSchreibfehler", () => {
  // Je Fall: rot, wenn die markierte Stelle wanderte oder fehlte.
  it("markiert einen vertauschten Buchstaben", () => expect(zeige("vet", "wet")).toBe("[w]et"));
  it("markiert einen vergessenen Buchstaben", () => expect(zeige("strase", "Straße")).toBe("Stra[ß]e"));
  it("markiert den Nachbarn eines überzähligen Buchstabens", () => expect(zeige("wett", "wet")).toBe("we[t]"));
  it("markiert einen fehlenden Akzent", () => expect(zeige("ecole", "école")).toBe("[é]cole"));
  it("markiert zwei Fehler einzeln", () => expect(zeige("bycicle", "bicycle")).toBe("b[i]c[y]cle"));

  // Rot, wenn Groß-/Kleinschreibung als Fehler zählte – der Server wertet sie nicht.
  it("ignoriert Groß-/Kleinschreibung", () => expect(zeige("Wet", "wet")).toBeNull());

  // Rot, wenn ein ganz anderes Wort Markierungen bekäme statt keiner.
  it("markiert nichts bei mehr als zwei Fehlern", () => expect(zeige("house", "maison")).toBeNull());
  it("markiert bei zwei Buchstaben noch einen Fehler", () => expect(zeige("ox", "on")).toBe("o[n]"));
  it("markiert nichts bei einem kurzen ganz anderen Wort", () => expect(zeige("up", "on")).toBeNull());
  it("markiert nichts ohne eigene Antwort", () => expect(zeige("", "walk")).toBeNull());
});
