import { describe, expect, it } from "vitest";
import { leerOptionen } from "./VaterZiele";

// B-518: die leere Wahl sagt, was gespeichert wird. Die Reihe geht nicht in den Scope des Ziels – rot, wenn die leere
// Unit wieder „ganze Reihe" verspricht oder die leere Übung bei gewählter Unit vom Fach spricht.
describe("Ziel-Bereich – leere Wahl", () => {
  it("nennt das ganze Fach ohne Unit und die Unit mit Unit", () => {
    expect(leerOptionen({ seriesUnitId: "", exerciseId: "" })).toEqual({
      unit: "keine – das Ziel zählt das ganze Fach", exercise: "alle Übungen des Fachs",
    });
    expect(leerOptionen({ seriesUnitId: 3, exerciseId: "" }).exercise).toBe("alle Übungen der Unit");
    // Review: mit gewählter Übung zählt nur sie – die leere Unit darf dann nicht „ganzes Fach" sagen.
    expect(leerOptionen({ seriesUnitId: "", exerciseId: 9 }).unit).not.toMatch(/ganze Fach/);
  });
});
