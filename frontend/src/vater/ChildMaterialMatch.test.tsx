import { act, render } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import { ChildMaterialSection } from "./ChildMaterialSection";

/*
 * B-534 (E2E-Fund): der Reiter „Schule" bleibt eingehängt. Speichert der Vater im Profil Klasse oder Schulart, muss die
 * Fachlehrer-Suche neu fragen – sie bewertet genau danach, und vorher stand nach dem Speichern „passt nur allgemein",
 * bis die Seite neu geladen wurde. Gefälscht wird der Abruf, nicht das `fetch`.
 */
describe("ChildMaterialSection – Fachlehrer-Suche", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn `kindStand` nicht in den Abhängigkeiten der Suche steht: dann bleibt es bei einem Abruf.
  it("fragt neu, wenn sich Klasse oder Schulart des Kindes ändern", async () => {
    vi.spyOn(api, "childTextbooks").mockResolvedValue([]);
    vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
    const match = vi.spyOn(api, "matchCreatorProfiles").mockResolvedValue([]);
    const seite = (kindStand: string) => (
      <AuthProvider><MemoryRouter>
        <ChildMaterialSection childId={3} childName="Theo" subjects={[]} kindStand={kindStand} />
      </MemoryRouter></AuthProvider>
    );

    const { rerender } = render(seite("-:None"));
    await act(async () => {});
    expect(match).toHaveBeenCalledTimes(1);

    rerender(seite("8:Gymnasium"));
    await act(async () => {});
    expect(match).toHaveBeenCalledTimes(2);
  });
});
