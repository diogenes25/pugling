import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusBanner } from "./StatusBanner";

/**
 * Die Zusicherung dieses Bauteils ist eine, die man **nicht sehen** kann: die Live-Region steht auch ohne
 * Meldung im DOM, weil viele Screenreader nur ansagen, was in eine *bereits vorhandene* Region hineinwächst.
 * Ein `return null` im Leerfall sieht identisch aus und macht die Ansage stumm – kein Klick-Test und kein
 * Screenshot findet das.
 */
describe("StatusBanner", () => {
  it("hält die Live-Region auch ohne Meldung im DOM – leer, aber vorhanden", () => {
    render(<StatusBanner message={null} />);

    const region = screen.getByRole("status");
    expect(region.getAttribute("aria-live")).toBe("polite");
    // Vorhanden **und** leer: ein `return null` fällt bei `getByRole` durch, ein immer sichtbarer Kasten hier.
    expect(region.textContent).toBe("");
    expect(region.querySelector(".banner")).toBeNull();
  });

  it("färbt den Erfolg grün ein und nennt den Text", () => {
    render(<StatusBanner message={{ ok: true, text: "Gespeichert." }} />);

    const region = screen.getByRole("status");
    expect(region.textContent).toBe("Gespeichert.");
    expect(region.querySelector(".banner")?.className).toBe("banner ok");
  });

  it("färbt den Fehler rot ein", () => {
    render(<StatusBanner message={{ ok: false, text: "Titel nötig." }} />);

    // Die Einfärbung ist die eine Hälfte der Rückmeldung, der Text in der Live-Region die andere.
    expect(screen.getByRole("status").querySelector(".banner")?.className).toBe("banner err");
  });
});
