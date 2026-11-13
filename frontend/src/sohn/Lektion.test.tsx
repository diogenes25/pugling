import { act, fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import { aufgabenform } from "./aufgabenform";
import { Antwortflaeche, Aufgabe, LektionsRahmen, RueckmeldungsBlatt, type LektionsKarte, type Rueckmeldung } from "./Lektion";

/*
 * B-526 – der gemeinsame Aufbau von Üben und Test. Je Fall steht dabei, welche Änderung ihn rot macht.
 */

const karte = (over: Partial<LektionsKarte>): LektionsKarte => ({
  itemIndex: 0, type: "Vocabulary", prompt: "la gare", gapIndex: null, passage: null, audioUrl: null, decoding: null,
  anyOrder: false, cueBox: null, choices: null, letterTiles: null, answerLength: null, answerPattern: null, ...over,
});

/** Die Antwortfläche mit dem Zustand, den sonst der Bildschirm hält. */
function Flaeche({ k, onAbschicken, gesperrt = false, kartenId = 0 }: {
  k: LektionsKarte; onAbschicken: (wert: string) => void; gesperrt?: boolean; kartenId?: number;
}) {
  const [wert, setWert] = useState("");
  return (
    <Antwortflaeche
      karte={k} kartenId={kartenId} wert={wert} onWert={setWert} gesperrt={gesperrt} knopf="Prüfen"
      feldName="Deine Antwort" platzhalter="Antwort…" onAbschicken={() => onAbschicken(wert)}
    />
  );
}

describe("Antwortfläche, Auswahl in zwei Schritten", () => {
  it("wertet beim Antippen noch nichts und sperrt den Hauptknopf ohne Markierung", () => {
    // Rot, wenn ein Tipp auf eine Option wieder sofort abschickt (der Stand vor B-526) – ein Vertipper wäre im Test
    // endgültig – oder wenn „Prüfen“ ohne Wahl drückbar wäre.
    const abschicken = vi.fn();
    render(<Flaeche k={karte({ choices: ["der Bahnhof", "die Brücke"] })} onAbschicken={abschicken} />);
    const pruefen = screen.getByRole("button", { name: "Prüfen" }) as HTMLButtonElement;
    expect(pruefen.disabled).toBe(true);

    fireEvent.click(screen.getByRole("button", { name: "der Bahnhof" }));
    expect(abschicken).not.toHaveBeenCalled();
    expect(pruefen.disabled).toBe(false);
    expect(screen.getByRole("button", { name: "der Bahnhof" }).getAttribute("aria-pressed")).toBe("true");
  });

  it("lässt die Wahl ändern und schickt mit dem Hauptknopf die zuletzt markierte Option", () => {
    // Rot, wenn die erste Markierung kleben bliebe oder der Hauptknopf nicht die sichtbare Wahl schickte.
    const abschicken = vi.fn();
    render(<Flaeche k={karte({ choices: ["der Bahnhof", "die Brücke"] })} onAbschicken={abschicken} />);
    fireEvent.click(screen.getByRole("button", { name: "der Bahnhof" }));
    fireEvent.click(screen.getByRole("button", { name: "die Brücke" }));
    expect(screen.getByRole("button", { name: "der Bahnhof" }).getAttribute("aria-pressed")).toBe("false");

    fireEvent.click(screen.getByRole("button", { name: "Prüfen" }));
    expect(abschicken).toHaveBeenCalledTimes(1);
    expect(abschicken).toHaveBeenCalledWith("die Brücke");
  });

  it("nimmt die Markierung beim zweiten Tipp zurück und sperrt den Hauptknopf wieder", () => {
    // Rot, wenn „abwählen“ den Wert stehen ließe – „Prüfen“ schickte dann eine Antwort, die niemand mehr sieht.
    render(<Flaeche k={karte({ choices: ["der Bahnhof", "die Brücke"] })} onAbschicken={() => {}} />);
    const option = screen.getByRole("button", { name: "der Bahnhof" });
    fireEvent.click(option);
    fireEvent.click(option);
    expect(option.getAttribute("aria-pressed")).toBe("false");
    expect((screen.getByRole("button", { name: "Prüfen" }) as HTMLButtonElement).disabled).toBe(true);
  });

  it("markiert bei doppelt eingetragener Option nur den angetippten Platz", () => {
    // Rot, wenn die Markierung am Text hinge: der Autor darf dieselbe Option zweimal eintragen, beide leuchteten.
    render(<Flaeche k={karte({ choices: ["Leeds", "Leeds", "York"] })} onAbschicken={() => {}} />);
    const beide = screen.getAllByRole("button", { name: "Leeds" });
    fireEvent.click(beide[1]);
    expect(beide.map((b) => b.getAttribute("aria-pressed"))).toEqual(["false", "true"]);
  });

  it("nimmt während einer laufenden Bewertung nichts an", () => {
    // Rot, wenn `gesperrt` nur den Hauptknopf träfe: die Wahl ließe sich unter der laufenden Wertung umstellen.
    render(<Flaeche k={karte({ choices: ["der Bahnhof", "die Brücke"] })} onAbschicken={() => {}} gesperrt />);
    for (const name of ["der Bahnhof", "die Brücke", "Prüfen"])
      expect((screen.getByRole("button", { name }) as HTMLButtonElement).disabled, name).toBe(true);
  });

  it("färbt keine Option nach richtig oder falsch", () => {
    // Die Fläche bekommt die Lösung gar nicht gereicht – im Test darf hier nie etwas darüber stehen. Rot, sobald
    // eine Option eine andere Klasse trägt als „gewählt“ (Absichtserklärung: heute gibt es keinen Weg dahin).
    render(<Flaeche k={karte({ choices: ["der Bahnhof", "die Brücke"] })} onAbschicken={() => {}} />);
    fireEvent.click(screen.getByRole("button", { name: "der Bahnhof" }));
    expect(screen.getByRole("button", { name: "der Bahnhof" }).className).toBe("btn ghost lektion-option gewaehlt");
    expect(screen.getByRole("button", { name: "die Brücke" }).className).toBe("btn ghost lektion-option");
  });
});

describe("Antwortfläche beim Kartenwechsel", () => {
  it("nimmt die Markierung nicht auf die nächste Karte mit", () => {
    // Rot, wenn `kartenId` die Markierung nicht zurücksetzt: dieselben Optionen (eine Wortbank über mehrere Lücken)
    // stünden auf der nächsten Karte schon markiert da.
    const k = karte({ choices: ["der Bahnhof", "die Brücke"] });
    const { rerender } = render(<Flaeche k={k} kartenId={1} onAbschicken={() => {}} />);
    fireEvent.click(screen.getByRole("button", { name: "der Bahnhof" }));
    expect(screen.getByRole("button", { name: "der Bahnhof" }).getAttribute("aria-pressed")).toBe("true");

    rerender(<Flaeche k={k} kartenId={2} onAbschicken={() => {}} />);
    expect(screen.getByRole("button", { name: "der Bahnhof" }).getAttribute("aria-pressed")).toBe("false");
  });

  it("behält das Eingabefeld und seinen Fokus", () => {
    // Rot, wenn die Fläche je Karte neu eingehängt wird (ein `key` von außen): das Feld wäre ein neuer Knoten, der
    // Fokus fiele auf `body`, und am Handy klappte nach jeder Antwort die Tastatur zu (Review B-526).
    const { rerender } = render(<Flaeche k={karte({ itemIndex: 0 })} kartenId={1} onAbschicken={() => {}} />);
    const feld = screen.getByRole("textbox", { name: "Deine Antwort" });
    feld.focus();

    rerender(<Flaeche k={karte({ itemIndex: 1, prompt: "la rue" })} kartenId={2} onAbschicken={() => {}} />);
    expect(screen.getByRole("textbox", { name: "Deine Antwort" })).toBe(feld);
    expect(document.activeElement).toBe(feld);
  });
});

describe("Antwortfläche, getippt", () => {
  it("schickt den Freitext mit Enter genau wie mit dem Hauptknopf, aber nichts Leeres", () => {
    // Rot, wenn Enter im Feld nichts täte (am Handy die „Los“-Taste der Tastatur) oder Leerzeichen als Antwort gälten.
    const abschicken = vi.fn();
    render(<Flaeche k={karte({})} onAbschicken={abschicken} />);
    const feld = screen.getByRole("textbox", { name: "Deine Antwort" });
    fireEvent.change(feld, { target: { value: "   " } });
    fireEvent.submit(feld.closest("form")!);
    expect(abschicken).not.toHaveBeenCalled();

    fireEvent.change(feld, { target: { value: "der Bahnhof" } });
    act(() => { fireEvent.submit(feld.closest("form")!); });
    expect(abschicken).toHaveBeenCalledWith("der Bahnhof");
  });
});

describe("Aufgabe", () => {
  const zeige = (k: LektionsKarte, extra: object = {}) =>
    render(<Aufgabe karte={k} form={aufgabenform({ reveal: null, ...k, ...extra })} skin={DEFAULT_SKIN} />);

  it("steht als Wort in der Sprechblase am Maskottchen", () => {
    // Rot, wenn die Vokabelkarte ihr Maskottchen verlöre.
    const { container } = zeige(karte({}));
    expect(container.querySelector(".lektion-frage .fcard.blase")).not.toBeNull();
    expect(container.querySelector(".lektion-begleiter")).not.toBeNull();
  });

  it("steht mit Wortkasten als volle Karte ohne Maskottchen", () => {
    // Rot, wenn der Schul-Vokabeltest (B-524) in die schmale Blase gezwängt würde.
    const { container } = zeige(karte({ cueBox: ["Klinik", "Patient"] }));
    expect(container.querySelector(".fcard")).not.toBeNull();
    expect(container.querySelector(".fcard.blase")).toBeNull();
    expect(container.querySelector(".lektion-begleiter")).toBeNull();
  });
});

describe("Rahmen", () => {
  it("nennt am ✕, was es tut, und zeigt Zähler und Anweisung", () => {
    // Rot, wenn das ✕ namenlos wäre – dem Zeichen sieht man nicht an, ob es die Runde beendet oder nur verlässt.
    const schliessen = vi.fn();
    render(
      <LektionsRahmen schliessenName="Später weiter" onSchliessen={schliessen} zaehler="Frage 2 / 4" anteil={0.25}
        anweisung="Test · Schreibe die Antwort">
        <p>Inhalt</p>
      </LektionsRahmen>,
    );
    fireEvent.click(screen.getByRole("button", { name: "Später weiter" }));
    expect(schliessen).toHaveBeenCalledTimes(1);
    expect(screen.getByText("Frage 2 / 4")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Test · Schreibe die Antwort" })).toBeTruthy();
  });
});

describe("Blatt", () => {
  const r = (over: Partial<Rueckmeldung>): Rueckmeldung => ({
    richtig: true, gegeben: "der Bahnhof", loesung: null, muenzen: 2, comboBonus: 0, tempoBonus: 0, faelligAm: null, lob: 0, ...over,
  });
  const zeige = (rueck: Rueckmeldung, onWeiter = () => {}) =>
    render(<RueckmeldungsBlatt r={rueck} skin={DEFAULT_SKIN} onWeiter={onWeiter} />).container.querySelector(".lektion-blatt")!;

  it("zeigt bei richtig Münzen, Boni und die nächste Fälligkeit", () => {
    // Rot, wenn ein Bonus oder die Fälligkeit im Blatt fehlte – beides stand vorher im Toast bzw. unter der Karte.
    const blatt = zeige(r({ comboBonus: 1, tempoBonus: 3, faelligAm: "2026-11-16" }));
    expect(blatt.className).toContain("richtig");
    expect(blatt.textContent).toContain("+2 🪙");
    expect(blatt.textContent).toContain("Combo +1 🪙 · Tempo +3 🪙");
    expect(blatt.textContent).toContain("Nächste Fälligkeit: 16.11.2026");
  });

  it("zeigt bei falsch die eigene Antwort durchgestrichen und die Lösung, aber keine Münzen", () => {
    // Rot, wenn die eigene Antwort oder die Lösung fehlte – das ist die Lehre der Karte (B-420).
    // `muenzen: 2` mit Absicht: nur so trägt die Zusicherung „keine Münzen“ etwas – sie fällt, wenn ein falsches Blatt
    // einen Betrag zeigte, den der Server (aus welchem Grund auch immer) mitlieferte.
    const blatt = zeige(r({ richtig: false, gegeben: "die Strasse", loesung: "die Straße", muenzen: 2 }));
    expect(blatt.className).toContain("falsch");
    expect(blatt.querySelector("s")!.textContent).toBe("die Strasse");
    expect(blatt.textContent).toContain("Lösung: die Straße");
    expect(blatt.textContent).not.toContain("🪙");
  });

  it("sagt ohne Lösung vom Server nur ab", () => {
    // Rot, wenn das Blatt eine Lösung erfände: in einer Menge wäre sie willkürlich und verriete einen offenen Eintrag.
    const blatt = zeige(r({ richtig: false, gegeben: "Hamburg", loesung: null, muenzen: 0 }));
    expect(blatt.textContent).toContain("Leider nicht.");
    expect(blatt.textContent).not.toContain("Lösung");
  });

  // B-529 AK 5 (Blatt): die Regel steht nach der Antwort da – bei falsch und bei richtig. Rot, wenn das Blatt die
  // Erklärung nicht zeigt oder nur bei einem der beiden Urteile.
  it("zeigt die Erklärung bei falsch und bei richtig", () => {
    expect(zeige(r({ richtig: false, gegeben: "à le", loesung: "au", erklaerung: "à + le wird zu au" })).textContent)
      .toContain("💡 Regel: à + le wird zu au");
    expect(zeige(r({ erklaerung: "de + les wird zu des" })).textContent).toContain("💡 Regel: de + les wird zu des");
  });

  // B-529 AK 5: ohne Erklärung keine leere Zeile. Rot, wenn die Zeile auch ohne Inhalt gerendert würde.
  it("zeigt ohne Erklärung keine Regelzeile", () => {
    const blatt = zeige(r({ richtig: false, gegeben: "x", loesung: "y", erklaerung: null }));
    expect(blatt.querySelector(".lektion-erklaerung")).toBeNull();
    expect(blatt.textContent).not.toContain("💡");
  });

  it("ist selbst keine Live-Region und führt mit Weiter weiter", () => {
    // Rot, wenn das Blatt `role="status"` trüge: es entsteht zusammen mit seinem Text und bliebe stumm (B-134) –
    // angesagt wird über die Region des Bildschirms.
    const weiter = vi.fn();
    const blatt = zeige(r({}), weiter);
    expect(blatt.getAttribute("role")).toBeNull();
    expect(blatt.querySelector("[role=status], [aria-live]")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Weiter →" }));
    expect(weiter).toHaveBeenCalledTimes(1);
  });
});
