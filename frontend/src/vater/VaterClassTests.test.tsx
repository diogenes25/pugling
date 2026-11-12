import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RepeatPanel } from "./VaterClassTests";
import type { KlassenarbeitRepeat } from "../lib/types";

/*
 * B-496: „keine schwach benotete Arbeit" ist auch die Antwort für ein Kind ganz ohne bewertete Arbeit. Der Leerfall darf
 * darum kein Erfolg sein. Rot, wenn er wieder 👍 bzw. „nichts nachzuholen" sagt.
 */
const state = (data: KlassenarbeitRepeat) =>
  ({ data, loading: false, error: null, showPlaceholder: false, stale: false, reload: () => {} });

describe("Wiederholen aus Klassenarbeiten – leer", () => {
  it("meldet keinen Erfolg", () => {
    render(<RepeatPanel repeat={state({ minBadGrade: 4, sources: [] } as unknown as KlassenarbeitRepeat)} />);
    // Der genaue neutrale Satz (B-507): ein Ausschluss zweier Erfolgswörter ließe jedes dritte durch.
    expect(screen.getByText("Keine Arbeit mit Note ≥ 4.0 – aus Klassenarbeiten ist nichts zu wiederholen.")).toBeTruthy();
  });
});
