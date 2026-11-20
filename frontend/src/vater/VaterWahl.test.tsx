import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { VaterWahl } from "./VaterWahl";

/*
 * B-663 AK 1 am Baustein: der Wahlschritt bietet Vater und Creator an, die gemerkte Wahl hat den Fokus, und der Klick
 * meldet genau diese Perspektive. Rot, wenn die Vorbelegung ignoriert wird (Fall 2 schlägt fehl, weil sonst immer
 * „Vater" den Fokus hätte) oder ein Knopf die falsche Perspektive meldet (Fall 3).
 */
describe("VaterWahl", () => {
  it("bietet genau die zwei Antworten und einen Ausgang an", () => {
    render(<VaterWahl name="Papa" vorbelegt="betreuen" onWahl={() => {}} onAbmelden={() => {}} />);
    expect(screen.getAllByRole("button").map((b) => b.getAttribute("aria-label") ?? b.textContent))
      .toEqual(["Als Vater", "Als Creator", "Abmelden"]);
    expect(screen.getByRole("heading", { name: /woran arbeitest du jetzt/ })).toBeTruthy();
  });

  it("legt den Fokus auf die vorbelegte Wahl", () => {
    render(<VaterWahl name="Papa" vorbelegt="erstellen" onWahl={() => {}} onAbmelden={() => {}} />);
    expect(document.activeElement).toBe(screen.getByRole("button", { name: "Als Creator" }));
  });

  it("meldet die gewählte Perspektive", () => {
    const onWahl = vi.fn();
    render(<VaterWahl name="Papa" vorbelegt="betreuen" onWahl={onWahl} onAbmelden={() => {}} />);
    fireEvent.click(screen.getByRole("button", { name: "Als Creator" }));
    fireEvent.click(screen.getByRole("button", { name: "Als Vater" }));
    expect(onWahl.mock.calls).toEqual([["erstellen"], ["betreuen"]]);
  });
});
