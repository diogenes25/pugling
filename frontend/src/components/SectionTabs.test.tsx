import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { activeSection, SectionPanel, SectionTabs, useActiveSection } from "./SectionTabs";

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

/** Eine Seite mit zwei Reitern, wie der Kind-Hub sie baut: ein Formular im ersten, Text im zweiten. */
function ZweiReiter() {
  const tab = useActiveSection(TABS);
  return (
    <>
      <SectionTabs tabs={TABS} active={tab} label="Profil von Theo" />
      <SectionPanel section="profil" active={tab}><label>Name <input /></label></SectionPanel>
      <SectionPanel section="schule" active={tab}><p>Stundenplan</p></SectionPanel>
    </>
  );
}

describe("SectionPanel", () => {
  // Rot, sobald ein Reiter wieder per `tab === … &&` ausgehängt wird: dann ist die ungespeicherte Eingabe nach dem
  // Hin und Zurück leer (B-534, Review).
  it("behält die ungespeicherte Eingabe über einen Reiterwechsel", () => {
    render(<MemoryRouter><ZweiReiter /></MemoryRouter>);
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Theodor" } });

    fireEvent.click(screen.getByRole("link", { name: "Schule" }));
    expect(screen.getByText("Stundenplan").closest("[hidden]")).toBeNull();
    fireEvent.click(screen.getByRole("link", { name: "Profil" }));

    expect((screen.getByLabelText("Name") as HTMLInputElement).value).toBe("Theodor");
  });

  // Rot, wenn der inaktive Reiter sichtbar bleibt: dann stünden wieder alle Speichern-Knöpfe untereinander, und der
  // verborgene Inhalt wäre per Tab erreichbar.
  it("verbirgt den inaktiven Reiter", () => {
    render(<MemoryRouter><ZweiReiter /></MemoryRouter>);
    expect(screen.getByText("Stundenplan").closest("[hidden]")).not.toBeNull();
    expect(screen.getByLabelText("Name").closest("[hidden]")).toBeNull();
  });
});
