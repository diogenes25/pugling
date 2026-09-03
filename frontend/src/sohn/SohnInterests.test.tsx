import { describe, expect, it, vi, beforeEach } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import {
  INTRO_SEEN_KEY, TagTiles, markIntroSeen, nextLikeWeight, shouldOfferIntro, toggledDislikeWeight,
} from "./SohnInterests";
import type { InterestTagOption } from "../lib/types";

const TAGS: InterestTagOption[] = [
  { id: 1, slug: "einhorn", label: "Einhorn", facet: "Animal" },
  { id: 2, slug: "comic", label: "Comic", facet: "Style" },
];

describe("Gewichte der Kacheln", () => {
  // Die Skala ist grob, weil ein Kind sie bedient - drei Stufen, die im Kreis laufen.
  it("schaltet aus -> mag ich -> Lieblingsthema -> aus", () => {
    expect(nextLikeWeight(0)).toBe(2);
    expect(nextLikeWeight(2)).toBe(3);
    expect(nextLikeWeight(3)).toBe(0);
  });

  // Sonst liefe das Kind aus der Abneigung heraus erst durch "aus", bevor etwas passiert.
  it("holt eine Abneigung mit dem ersten Tipp direkt auf „mag ich“", () => {
    expect(nextLikeWeight(-3)).toBe(2);
  });

  it("überschreibt mit der Abneigung eine bestehende Zuneigung", () => {
    expect(toggledDislikeWeight(3)).toBe(-3);
    expect(toggledDislikeWeight(0)).toBe(-3);
    expect(toggledDislikeWeight(-3)).toBe(0);
  });
});

describe("Kacheln", () => {
  it("zeigt den gewählten Zustand per aria-pressed und als Text, nicht nur als Farbe", () => {
    render(<TagTiles tags={TAGS} weights={{ 1: 3 }} onPick={() => {}} disabled={false} />);

    const gewaehlt = screen.getByRole("button", { name: /Einhorn/ });
    expect(gewaehlt.getAttribute("aria-pressed")).toBe("true");
    expect(gewaehlt.textContent).toContain("Lieblingsthema");

    const offen = screen.getByRole("button", { name: /Comic/ });
    expect(offen.getAttribute("aria-pressed")).toBe("false");
    expect(offen.textContent).toContain("auswählen");
  });

  it("meldet den angetippten Tag nach oben", () => {
    const onPick = vi.fn();
    render(<TagTiles tags={TAGS} weights={{}} onPick={onPick} disabled={false} />);

    fireEvent.click(screen.getByRole("button", { name: /Comic/ }));
    expect(onPick).toHaveBeenCalledWith(2);
  });

  // Die Sperre sitzt im Primitiv, der sichtbare Teil ist `disabled` - beim Speichern darf keine Kachel
  // mehr kippen, sonst schickt der laufende PUT einen Stand, den das Kind schon wieder verlassen hat.
  it("sperrt die Kacheln, solange gespeichert wird", () => {
    const onPick = vi.fn();
    render(<TagTiles tags={TAGS} weights={{}} onPick={onPick} disabled={true} />);

    fireEvent.click(screen.getByRole("button", { name: /Einhorn/ }));
    expect(onPick).not.toHaveBeenCalled();
  });

  it("sagt es, wenn die Taxonomie noch leer ist, statt eine leere Fläche zu zeigen", () => {
    render(<TagTiles tags={[]} weights={{}} onPick={() => {}} disabled={false} />);
    expect(screen.getByText(/noch nichts zur Auswahl/)).toBeTruthy();
  });

  it("zeigt im Abneigungs-Schritt den negativen Zustand, nicht den positiven", () => {
    render(<TagTiles tags={TAGS} weights={{ 1: -3 }} onPick={() => {}} mode="dislike" disabled={false} />);

    const abgelehnt = screen.getByRole("button", { name: /Einhorn/ });
    expect(abgelehnt.getAttribute("aria-pressed")).toBe("true");
    expect(abgelehnt.textContent).toContain("geht gar nicht");
    expect(screen.getByRole("button", { name: /Comic/ }).textContent).toContain("ist okay");
  });
});

describe("Anstoß beim ersten Mal", () => {
  beforeEach(() => localStorage.clear());

  it("bietet den Prozess nur bei leerem Profil an", () => {
    expect(shouldOfferIntro(7, true)).toBe(true);
    expect(shouldOfferIntro(7, false)).toBe(false);
  });

  // `needsIntro` kommt aus dem leeren Bestand (kein Server-Feld). Ein Kind, das bewusst nichts wählt,
  // bliebe damit für immer "neu" - der lokale Vermerk ist genau dagegen da.
  it("bietet ihn nach dem ersten Besuch nicht mehr an, auch wenn das Profil leer blieb", () => {
    markIntroSeen(7);
    expect(shouldOfferIntro(7, true)).toBe(false);
  });

  it("merkt den Besuch je Kind, nicht je Gerät", () => {
    markIntroSeen(7);
    expect(shouldOfferIntro(8, true)).toBe(true);
    expect(localStorage.getItem(INTRO_SEEN_KEY(7))).toBe("1");
  });
});
