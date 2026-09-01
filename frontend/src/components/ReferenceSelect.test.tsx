import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ReferenceSelect, type ReferenceOption } from "./ReferenceSelect";

/*
 * B-172. Die Regel des Bauteils, an einer Stelle geprüft – die sieben Aufrufstellen prüfen dann nur noch,
 * dass sie es benutzen und richtig füttern.
 *
 * Rot macht diese Fälle das Entfernen bzw. Umdrehen der Sonderoption in `specialOption`. Keiner prüft
 * einen Ausgangszustand: das Bauteil gab es vorher nicht.
 */

const OPTIONEN: ReferenceOption[] = [
  { value: "1", label: "Englisch" },
  { value: "2", label: "Mathe" },
];

function zeige(props: Partial<React.ComponentProps<typeof ReferenceSelect>> = {}) {
  const onChange = vi.fn();
  render(
    <>
      <label htmlFor="ref">Fach</label>
      <ReferenceSelect
        id="ref" value="1" onChange={onChange} loadedId={1} loadedName="Englisch"
        options={OPTIONEN} emptyLabel="– keine Angabe –" {...props}
      />
    </>,
  );
  return { onChange, feld: screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement };
}

describe("ReferenceSelect – die Sonderoption", () => {
  it("bietet keine, wenn der geladene Wert in der Liste steht", () => {
    const { feld } = zeige();

    expect(feld.value).toBe("1");
    expect(screen.queryByRole("option", { name: /nicht geladen|Freitext/ })).toBeNull();
    expect(feld.options).toHaveLength(3); // leer + zwei Fächer
  });

  it("bietet keine, wenn gar kein Wert geladen ist", () => {
    const { feld } = zeige({ value: "", loadedId: null, loadedName: null });

    expect(feld.value).toBe("");
    expect(feld.options).toHaveLength(3);
  });

  it("zeigt den geladenen Wert als wählbare Option, wenn die Liste ihn nicht kennt", () => {
    const { feld } = zeige({ options: [OPTIONEN[1]] });

    // Die Zusicherung, um die es geht: das Feld zeigt NICHT „– keine Angabe –".
    expect(feld.value).toBe("1");
    const option = screen.getByRole("option", { name: "Englisch (nicht geladen)" }) as HTMLOptionElement;
    expect(option.disabled).toBe(false);
  });

  it("hält die Option auch, während der Nutzer etwas anderes probiert", () => {
    // Der Kern der Ableitung aus dem GELADENEN Stand: käme die Bedingung aus `value`, verschwände die
    // Option beim ersten Wechsel und der Weg zurück wäre zu.
    const { feld } = zeige({ value: "2", options: [OPTIONEN[1]] });

    expect(feld.value).toBe("2");
    expect(screen.getByRole("option", { name: "Englisch (nicht geladen)" })).toBeTruthy();
  });

  it("sperrt dagegen den Freitext-Zustand", () => {
    const { feld } = zeige({
      value: "__freetext__", loadedId: null, loadedName: "Erdkunde", freetextValue: "__freetext__",
    });

    expect(feld.value).toBe("__freetext__");
    const option = screen.getByRole("option", { name: "Erdkunde (Freitext)" }) as HTMLOptionElement;
    expect(option.disabled).toBe(true);
  });

  it("bietet den Freitext-Zustand nicht an, wo es keinen Sentinel gibt", () => {
    // Reihe, Verlag und Unit kennen den Zustand nicht: ihr Name ist ein Join und fällt mit der Id weg.
    zeige({ value: "", loadedId: null, loadedName: "Erdkunde" });

    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("beschriftet einen namenlosen Wert mit seiner Id, statt ihn zu verschweigen", () => {
    zeige({ loadedName: null, options: [OPTIONEN[1]] });

    expect(screen.getByRole("option", { name: "#1 (nicht geladen)" })).toBeTruthy();
  });

  it("reicht die Auswahl als Wert nach oben, nicht als Ereignis", () => {
    // Der Aufrufer besitzt sein `onChange` weiterhin: zwei der sieben Felder hängen Nebenwirkungen daran
    // (die Unit zurücksetzen, Felder aus der Reihe ableiten).
    const { onChange, feld } = zeige();

    fireEvent.change(feld, { target: { value: "2" } });

    expect(onChange).toHaveBeenCalledWith("2");
  });

  it("lässt sich sperren", () => {
    // Die Unit im Lehrbuch ist gesperrt, solange keine Reihe gewählt ist.
    expect(zeige({ disabled: true }).feld.disabled).toBe(true);
  });
});
