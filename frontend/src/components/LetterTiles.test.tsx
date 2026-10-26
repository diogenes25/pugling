import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { LetterTiles } from "./LetterTiles";

/*
 * B-05: Buchstaben ordnen, mit Klick und mit Tastatur (AK 5). Die Kacheln sind absichtlich NICHT in Lösungsreihen-
 * folge und tragen einen doppelten Buchstaben („Kaffee“ hat zwei E): so fällt auf, wenn die Komponente Buchstaben
 * statt Kacheln verfolgt oder die Reihenfolge der Kacheln statt der Reihenfolge des Legens meldet.
 */
const TILES = ["e", "K", "f", "a", "e", "f"];

function Harness({ onSubmit = () => {} }: { onSubmit?: () => void }) {
  const [value, setValue] = useState("");
  return (
    <>
      <LetterTiles tiles={TILES} value={value} onChange={setValue} onSubmit={onSubmit} />
      <span data-testid="wert">{value}</span>
      <button type="button" onClick={() => setValue("")}>leeren</button>
    </>
  );
}

const wert = () => screen.getByTestId("wert").textContent;
const kacheln = (b: string) => screen.getAllByRole("button", { name: `Buchstabe ${b}` });

describe("LetterTiles", () => {
  it("legt per Klick in der Reihenfolge des Klickens und sperrt die gelegte Kachel", () => {
    render(<Harness />);
    fireEvent.click(kacheln("K")[0]);
    fireEvent.click(kacheln("a")[0]);
    fireEvent.click(kacheln("f")[1]);
    expect(wert()).toBe("Kaf");
    expect(kacheln("f")[1].getAttribute("aria-disabled")).toBe("true");
    expect(kacheln("f")[0].getAttribute("aria-disabled")).toBe("false");
  });

  it("legt per Tastatur, auch zwei gleiche Buchstaben auf zwei verschiedene Kacheln", () => {
    render(<Harness />);
    const block = kacheln("K")[0];
    for (const key of ["k", "a", "f", "f", "e", "e"]) fireEvent.keyDown(block, { key });
    expect(wert()).toBe("Kaffee");
    expect(kacheln("e").every((b) => b.getAttribute("aria-disabled") === "true")).toBe(true);
    // Ein siebter Buchstabe ohne freie Kachel ändert nichts.
    fireEvent.keyDown(block, { key: "e" });
    expect(wert()).toBe("Kaffee");
  });

  it("nimmt mit Backspace bzw. „Zurück“ die zuletzt gelegte Kachel zurück und prüft mit Enter", () => {
    const onSubmit = vi.fn();
    render(<Harness onSubmit={onSubmit} />);
    const block = kacheln("K")[0];
    for (const key of ["k", "a", "f"]) fireEvent.keyDown(block, { key });
    fireEvent.keyDown(block, { key: "Backspace" });
    expect(wert()).toBe("Ka");
    fireEvent.click(screen.getByRole("button", { name: "⌫ Zurück" }));
    expect(wert()).toBe("K");
    fireEvent.keyDown(block, { key: "Enter" });
    expect(onSubmit).toHaveBeenCalledTimes(1);
  });

  it("lässt Enter auf „Neu anfangen“ den Knopf bedienen statt das halbe Wort abzuschicken", () => {
    const onSubmit = vi.fn();
    render(<Harness onSubmit={onSubmit} />);
    fireEvent.click(kacheln("K")[0]);
    fireEvent.click(kacheln("a")[0]);
    const neu = screen.getByRole("button", { name: "Neu anfangen" });
    fireEvent.keyDown(neu, { key: "Enter" });
    expect(onSubmit).not.toHaveBeenCalled();
    fireEvent.click(neu);
    expect(wert()).toBe("");
  });

  it("fängt von vorn an, wenn der Aufrufer den Wert leert", () => {
    render(<Harness />);
    fireEvent.click(kacheln("K")[0]);
    fireEvent.click(screen.getByRole("button", { name: "leeren" }));
    expect(wert()).toBe("");
    expect(kacheln("K")[0].getAttribute("aria-disabled")).toBe("false");
  });

  // B-134: die Region steht, bevor etwas hineinkommt – sonst bliebe die erste Ansage stumm.
  it("sagt das gelegte Wort in einer Region an, die schon vorher im DOM steht", () => {
    render(<Harness />);
    const region = screen.getByRole("status");
    expect(region.textContent).toBe("");
    fireEvent.click(kacheln("K")[0]);
    expect(screen.getByRole("status")).toBe(region);
    expect(region.textContent).toBe("Gelegt: K");
  });
});
