import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { PublisherRow } from "./PublisherAdmin";
import type { PublisherResponse } from "../lib/types";

/*
 * B-160. [B-150](../../../docs/backlog/B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) hat
 * den Löschen-Knopf gesperrt, damit der Creator nicht in einen `409` läuft, und den Grund in ein
 * `title`-Attribut geschrieben — **auf den `disabled` Knopf**. Dort zeigen Chromium und WebKit keinen
 * Tooltip: ein `disabled` Form-Control empfängt keine Pointer-Events. Der Grund war damit nirgends zu
 * lesen, und aus dem Tab-Lauf war der Knopf auch heraus.
 *
 * Diese Datei ist zugleich die **erste** Testebene für `PublisherAdmin` überhaupt — B-150 hatte weder
 * Komponententest noch E2E, und genau deshalb konnte die Hälfte ihres Akzeptanzkriteriums („der Knopf
 * nennt den Grund") gebaut aussehen, ohne es zu sein.
 *
 * Bausteintest und nicht E2E: Die Frage ist eine Zustandsfrage der Zeile (gesperrt/nicht) und eine
 * A11y-Frage — beides trägt ein E2E nicht besser, und ein nachgebauter Bildschirm ist hier ausdrücklich
 * die falsche Ebene (`frontend/CLAUDE.md`).
 */

/** Ein Verlag; `foreignSeriesCount` ist die Größe, an der die Sperre hängt. */
const verlag = (over: Partial<PublisherResponse> = {}): PublisherResponse => ({
  id: 1, name: "Klett", slug: "klett", seriesCount: 3, foreignSeriesCount: 0,
  createdAt: "2026-01-01T00:00:00Z", ...over,
});

const loeschen = () => screen.getByRole("button", { name: /^Löschen:/ });

function zeile(p: PublisherResponse, onDelete = () => {}) {
  // In eine Tabelle gehängt: `PublisherRow` rendert ein `<tr>`, und ohne `<tbody>` verwirft der Parser
  // die Zellen – die Zusicherungen fänden dann nichts, obwohl der Baustein richtig wäre.
  render(<table><tbody><PublisherRow publisher={p} busy={false} onSave={() => {}} onDelete={onDelete} /></tbody></table>);
}

describe("PublisherRow – die Sperre nennt ihren Grund", () => {
  it("macht den Grund ohne Hover lesbar, wenn fremde Reihen dranhängen", () => {
    zeile(verlag({ foreignSeriesCount: 2 }));

    // AK 1: Der Grund steht als Text da, nicht nur in einem `title`, den niemand auslösen kann.
    expect(screen.getByText(/darum nicht löschbar/)).toBeTruthy();
  });

  it("sperrt den Knopf so, dass ein Screenreader die Sperre überhaupt bemerkt", () => {
    zeile(verlag({ foreignSeriesCount: 2 }));
    const knopf = loeschen();

    // AK 3: `aria-disabled` statt `disabled`. Das ist der Kern: Ein `disabled` Knopf ist nicht
    // fokussierbar – wer per Tab durch die Tabelle geht, trifft ihn nie und erfährt nichts von der Sperre.
    expect(knopf.getAttribute("aria-disabled")).toBe("true");
    expect(knopf.hasAttribute("disabled")).toBe(false);

    // Und der Grund hängt am Knopf, nicht nur irgendwo daneben.
    const grundId = knopf.getAttribute("aria-describedby");
    expect(grundId).toBeTruthy();
    expect(document.getElementById(grundId!)?.textContent).toMatch(/nicht löschbar/);
  });

  it("löst in diesem Zustand keine Anfrage aus", () => {
    const onDelete = vi.fn();
    zeile(verlag({ foreignSeriesCount: 2 }), onDelete);

    fireEvent.click(loeschen());

    // AK 2: Der Preis von `aria-disabled` – der Browser hält den Klick nicht mehr auf, also muss der
    // `onClick` es selbst tun. Sonst liefe der Creator wieder in den `409`, den B-150 abschaffen wollte.
    expect(onDelete).not.toHaveBeenCalled();
  });

  it("lässt einen Verlag ohne fremde Reihen unangetastet", () => {
    const onDelete = vi.fn();
    zeile(verlag({ foreignSeriesCount: 0 }), onDelete);
    const knopf = loeschen();

    // AK 4: kein `aria-disabled`, kein Grund-Text, und der Klick geht durch.
    expect(knopf.getAttribute("aria-disabled")).not.toBe("true");
    expect(screen.queryByText(/nicht löschbar/)).toBeNull();
    fireEvent.click(knopf);
    expect(onDelete).toHaveBeenCalledTimes(1);
  });
});
