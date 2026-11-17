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
// B-634: „as soon as“, je Wort gemischt – referenzstabil wie vom Server (eine neue Liste je Render wäre eine neue Karte).
const MEHRWORT = ["s", "a", "n", "o", "s", "o", "a", "s"];

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

  it("überlässt Kürzel mit Strg, Alt oder Cmd dem Browser, statt eine Kachel zu legen", () => {
    render(<Harness />);
    const block = kacheln("K")[0];
    fireEvent.keyDown(block, { key: "k", ctrlKey: true });
    fireEvent.keyDown(block, { key: "a", altKey: true });
    fireEvent.keyDown(block, { key: "f", metaKey: true });
    expect(wert()).toBe("");
  });

  /*
   * Fokus bei neuer Karte (Review Lauf D, Fund 6). Die beiden Fälle unterscheiden sich allein darin, WO der Fokus
   * vor dem Kartenwechsel stand: im Kachel-Block (Enter-Prüfer) oder auf einem lebendigen Bedienelement außerhalb
   * (das „×“ des Vorschau-Dialogs, wenn die Kacheln erst nach dem Öffnen eintreffen). Eine Komponente, die immer
   * oder nie fokussiert, fällt in genau einem der beiden. Den dritten Weg – gesperrter „Prüfen“-Knopf gibt den
   * Fokus an `body` ab – kann nur der Browser zeigen; ihn fährt die E2E-Spec.
   */
  // Zwei Karten als Konstanten; der Wert wird bewusst NICHT geleert, damit allein der Kartenwechsel zurücksetzen kann.
  const KARTE_1 = ["b", "a"];
  const KARTE_2 = ["d", "c"];
  function MitWert({ tiles }: { tiles: string[] }) {
    const [value, setValue] = useState("");
    return <LetterTiles tiles={tiles} value={value} onChange={setValue} />;
  }

  it("holt den Fokus auf die erste Kachel der nächsten Karte, wenn er im Block stand", () => {
    const { rerender } = render(<MitWert tiles={KARTE_1} />);
    // Kachel 0 ist gelegt, der Fokus steht auf Kachel 1: so trägt die Zusicherung unten etwas – ohne Reset
    // bliebe Index 0 der neuen Karte gesperrt (B-287: vorher war nichts gelegt, die Zeile konnte nie fallen).
    fireEvent.click(kacheln("b")[0]);
    expect(kacheln("b")[0].getAttribute("aria-disabled")).toBe("true");
    kacheln("a")[0].focus();
    rerender(<MitWert tiles={KARTE_2} />);
    expect(document.activeElement).toBe(kacheln("d")[0]);
    // Und die gelegten Kacheln der alten Karte sind weg, ohne `key` von außen.
    expect(kacheln("d")[0].getAttribute("aria-disabled")).toBe("false");
  });

  it("lässt den Fokus in Ruhe, wenn er beim Kartenwechsel außerhalb des Blocks stand", () => {
    const { rerender } = render(<><button type="button">Prüfen</button><LetterTiles tiles={["b", "a"]} value="" onChange={() => {}} /></>);
    const pruefen = screen.getByRole("button", { name: "Prüfen" });
    pruefen.focus();
    rerender(<><button type="button">Prüfen</button><LetterTiles tiles={["d", "c"]} value="" onChange={() => {}} /></>);
    expect(document.activeElement).toBe(pruefen);
  });

  // Der Wert bleibt hier absichtlich stehen: der Neustart muss aus den neuen Kacheln kommen, nicht aus dem Leeren.
  function Kartenwechsel() {
    const [tiles, setTiles] = useState<string[]>(["b", "a"]);
    const [value, setValue] = useState("");
    return (
      <>
        <LetterTiles tiles={tiles} value={value} onChange={setValue} />
        <button type="button" onClick={() => setTiles(["d", "c"])}>nächste Karte</button>
      </>
    );
  }

  it("verwirft die gelegten Kacheln, sobald neue Kacheln kommen", () => {
    render(<Kartenwechsel />);
    fireEvent.click(kacheln("b")[0]);
    expect(screen.getByRole("status").textContent).toBe("Gelegt: b");
    fireEvent.click(screen.getByRole("button", { name: "nächste Karte" }));
    expect(screen.getByRole("status").textContent).toBe("");
    expect(kacheln("d")[0].getAttribute("aria-disabled")).toBe("false");
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

  /*
   * B-634 AK 1: mehrere Wörter. Die Kacheln kommen Wort für Wort („as“, „soon“, „as“ – je Wort gemischt), das Muster
   * trägt die Grenzen. Rot, wenn die Komponente das Muster überhört: dann gibt es einen Block statt drei Wortgruppen, und
   * das gelegte Wort käme als „assoonas“ ohne Leerzeichen an.
   */
  it("legt mehrere Wörter je Wort und setzt die Leerzeichen selbst", () => {
    function Mehrwort() {
      const [value, setValue] = useState("");
      return <><LetterTiles tiles={MEHRWORT} value={value} onChange={setValue}
        pattern="__ ____ __" /><span data-testid="wert">{value}</span></>;
    }
    const { container } = render(<Mehrwort />);
    expect(container.querySelectorAll(".lettertiles-pool .lettertiles-wort")).toHaveLength(3);
    // B-634, Review: jedes Wort ist eine benannte Gruppe – ein Screenreader hört Zahl und Länge der Wörter.
    expect(screen.getByRole("group", { name: "Wort 2, 4 Buchstaben" })).toBeTruthy();
    expect(container.querySelectorAll(".lettertiles-word .lettertiles-wort")).toHaveLength(3);
    const block = kacheln("a")[0];
    for (const key of "assoonas") fireEvent.keyDown(block, { key });
    expect(wert()).toBe("as soon as");
    expect(screen.getByRole("status").textContent).toBe("Gelegt: a s / s o o n / a s");
  });

  it("teilt ohne passendes Muster nicht", async () => {
    const { wortBereiche } = await import("./LetterTiles");
    expect(wortBereiche(4, null)).toEqual([{ start: 0, laenge: 4 }]);
    expect(wortBereiche(4, "__ ___")).toEqual([{ start: 0, laenge: 4 }]);
    expect(wortBereiche(6, "__ ____")).toEqual([{ start: 0, laenge: 2 }, { start: 2, laenge: 4 }]);
  });
});
