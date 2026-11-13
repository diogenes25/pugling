import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { activeSection, SectionTabs } from "./SectionTabs";

const TABS = [
  { key: "profil", label: "Profil" },
  { key: "schule", label: "Schule" },
] as const;

describe("activeSection", () => {
  // Rot, wenn der Query-Wert ignoriert wird – dann öffnete „Interessen pflegen →" immer den ersten Reiter.
  it("nimmt den Reiter aus der URL", () => {
    expect(activeSection(TABS, "schule")).toBe("schule");
  });

  // Rot, wenn ein alter oder vertippter Link eine leere Seite öffnet statt den ersten Reiter.
  it("fällt bei Unbekanntem und Fehlendem auf den ersten Reiter zurück", () => {
    expect(activeSection(TABS, "gibtsnicht")).toBe("profil");
    expect(activeSection(TABS, null)).toBe("profil");
  });
});

describe("SectionTabs", () => {
  // Rot, wenn der aktive Reiter nur farblich markiert ist: ein Screenreader erführe nicht, wo er steht.
  it("markiert genau den aktiven Reiter mit aria-current", () => {
    render(<MemoryRouter><SectionTabs tabs={TABS} active="schule" label="Profil von Theo" /></MemoryRouter>);
    expect(screen.getByRole("link", { name: "Schule" }).getAttribute("aria-current")).toBe("page");
    expect(screen.getByRole("link", { name: "Profil" }).getAttribute("aria-current")).toBeNull();
    expect(screen.getByRole("link", { name: "Schule" }).getAttribute("href")).toBe("/?bereich=schule");
  });
});
