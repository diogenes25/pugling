import { afterEach, describe, expect, it } from "vitest";
import * as ui from "./ui";

/**
 * B-352: eine Radiogruppe aus Knöpfen bekommt das Tastaturmodell, das ihre Rolle ansagt. Jeder Fall unterscheidet sich
 * vom Ausgangszustand in genau der geprüften Taste; rot, wenn `radioGroupKeys` fehlt oder die Taste nicht wandert.
 */
function gruppe(labels: string[], gesperrt: string[] = []) {
  const box = document.createElement("div");
  box.setAttribute("role", "radiogroup");
  const gewaehlt: string[] = [];
  for (const label of labels) {
    const b = document.createElement("button");
    b.type = "button";
    b.setAttribute("role", "radio");
    b.textContent = label;
    b.disabled = gesperrt.includes(label);
    b.addEventListener("click", () => gewaehlt.push(label));
    box.appendChild(b);
  }
  // Wie `onKeyDown={radioGroupKeys}` am Container: das Ereignis blubbert von der fokussierten Option hoch.
  box.addEventListener("keydown", (e) => ui.radioGroupKeys(e));
  document.body.appendChild(box);
  const knopf = (label: string) => [...box.querySelectorAll("button")].find((b) => b.textContent === label)!;
  const druecke = (von: string, key: string) => {
    knopf(von).focus();
    knopf(von).dispatchEvent(new KeyboardEvent("keydown", { key, bubbles: true, cancelable: true }));
  };
  return { knopf, druecke, gewaehlt };
}

afterEach(() => { document.body.innerHTML = ""; });

describe("radioGroupKeys", () => {
  it("wählt mit Pfeil rechts die nächste Option und fokussiert sie", () => {
    const g = gruppe(["A", "B", "C"]);
    g.druecke("A", "ArrowRight");
    expect(document.activeElement).toBe(g.knopf("B"));
    expect(g.gewaehlt).toEqual(["B"]);
  });

  it("springt mit Pfeil links vom Anfang ans Ende", () => {
    const g = gruppe(["A", "B", "C"]);
    g.druecke("A", "ArrowLeft");
    expect(g.gewaehlt).toEqual(["C"]);
  });

  it("springt mit Pfeil unten vom Ende an den Anfang", () => {
    const g = gruppe(["A", "B", "C"]);
    g.druecke("C", "ArrowDown");
    expect(g.gewaehlt).toEqual(["A"]);
  });

  it("geht mit Pfeil oben zurück und mit Home/End an die Enden", () => {
    const g = gruppe(["A", "B", "C"]);
    g.druecke("B", "ArrowUp");
    g.druecke("B", "End");
    g.druecke("C", "Home");
    expect(g.gewaehlt).toEqual(["A", "C", "A"]);
  });

  it("überspringt gesperrte Optionen", () => {
    const g = gruppe(["A", "B", "C"], ["B"]);
    g.druecke("A", "ArrowRight");
    expect(g.gewaehlt).toEqual(["C"]);
  });

  // Rot, wenn Modifier nicht ausgenommen sind: Alt+Links ist im Browser „Zurück“ und darf die Wahl nicht ändern.
  it("lässt Pfeile mit Alt, Strg oder Cmd dem Browser", () => {
    const g = gruppe(["A", "B"]);
    g.knopf("A").focus();
    for (const mod of ["altKey", "ctrlKey", "metaKey"]) {
      g.knopf("A").dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowLeft", [mod]: true, bubbles: true, cancelable: true }));
    }
    expect(g.gewaehlt).toEqual([]);
  });

  // Rot, wenn jede Taste wandert: Tab und Leertaste gehören dem Browser (verlassen bzw. wählen die fokussierte Option).
  it("lässt andere Tasten in Ruhe", () => {
    const g = gruppe(["A", "B"]);
    const ev = new KeyboardEvent("keydown", { key: "Tab", bubbles: true, cancelable: true });
    g.knopf("A").focus();
    g.knopf("A").dispatchEvent(ev);
    expect(g.gewaehlt).toEqual([]);
    expect(ev.defaultPrevented).toBe(false);
  });
});
