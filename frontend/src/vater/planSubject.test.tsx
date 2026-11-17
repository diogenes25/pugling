import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PlanResponse, UpdatePlanDto } from "../lib/types";

/**
 * B-539: das Fach ist am Lehrplan Pflicht, die Unit optional. Beide Formulare – Neuer Plan und Plan bearbeiten –
 * sperren Speichern ohne Fach (B-233) und schicken die Unit so, wie der PATCH-Vertrag sie versteht.
 */
const createPlan = vi.fn();
const textbookSeries = vi.fn();
const loadPlan = vi.fn();
const updatePlan = vi.fn();

const seriesOf = ({ subjectId }: { subjectId: number }) =>
  Promise.resolve(subjectId === 3 ? [{ id: 30, name: "Access 1", subjectId: 3 }] : []);

vi.mock("../lib/auth", () => ({ useAuth: () => ({ session: { id: 1 } }) }));
// Die Positionsliste lädt ihren eigenen Bestand – für den Kopf der Plan-Seite ohne Belang.
vi.mock("./PlanPositions", () => ({ PlanPositions: () => null }));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      children: () => Promise.resolve([{ id: 7, name: "Theo" }]),
      child: () => Promise.resolve({ id: 7, name: "Theo" }),
      subjects: () => Promise.resolve([{ id: 3, name: "Englisch" }, { id: 4, name: "Französisch" }]),
      textbookSeries: (...a: unknown[]) => textbookSeries(...a),
      seriesUnits: (seriesId: number) =>
        Promise.resolve(seriesId === 30 ? [{ id: 301, seriesId: 30, label: "Unit 1" }, { id: 302, seriesId: 30, label: "Unit 2" }] : []),
      createPlan: (...a: unknown[]) => createPlan(...a),
      plan: (...a: unknown[]) => loadPlan(...a),
      updatePlan: (...a: unknown[]) => updatePlan(...a),
      overviewProgress: () => Promise.resolve({ totalPoints: 0, daysComplete: 0, totalDays: 0, currentStreak: 0, days: [] }),
    },
  };
});

const { VaterPlanCreate } = await import("./VaterPlanCreate");
const { PlanEditForm, VaterPlanDetail } = await import("./VaterPlanDetail");

const settle = () => act(async () => {});

beforeEach(() => {
  createPlan.mockReset().mockResolvedValue({ id: 99 });
  textbookSeries.mockReset().mockImplementation(seriesOf);
  loadPlan.mockReset();
  updatePlan.mockReset().mockResolvedValue({});
});

const optionLabels = (select: HTMLElement) => Array.from((select as HTMLSelectElement).options).map((o) => o.textContent);

describe("Neuer Lehrplan", () => {
  // Rot, wenn der Knopf wieder nur an `busy` hängt: dann ist er ohne Fach anklickbar und der Server antwortet 400.
  it("sperrt Speichern, bis ein Fach gewählt ist, und schickt Fach und Unit mit", async () => {
    render(<MemoryRouter><VaterPlanCreate /></MemoryRouter>);
    await settle();
    const save = screen.getByRole("button", { name: "Plan anlegen & Übungen hinzufügen" });
    expect((save as HTMLButtonElement).disabled).toBe(true);

    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "3" } });
    await settle();
    expect((save as HTMLButtonElement).disabled).toBe(false);
    fireEvent.change(screen.getByLabelText("Unit (optional)", { exact: true }), { target: { value: "302" } });

    fireEvent.click(save);
    await settle();
    expect(createPlan).toHaveBeenCalledWith(expect.objectContaining({ childId: 7, subjectId: 3, seriesUnitId: 302 }));
  });

  // Rot, wenn der Fachwechsel die Unit nicht leert (`setSeriesUnitId("")` im onChange des Fachs): dann reiste
  // Unit 302 des Englisch-Buchs mit Fach Französisch, und der Server wiese die Kombination ab.
  it("leert die Unit beim Fachwechsel und schickt keine veraltete mit", async () => {
    render(<MemoryRouter><VaterPlanCreate /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "3" } });
    await settle();
    fireEvent.change(screen.getByLabelText("Unit (optional)", { exact: true }), { target: { value: "302" } });
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "4" } });
    await settle();

    fireEvent.click(screen.getByRole("button", { name: "Plan anlegen & Übungen hinzufügen" }));
    await settle();
    expect(createPlan).toHaveBeenCalledWith(expect.objectContaining({ subjectId: 4, seriesUnitId: null }));
  });

  // B-170 im Ladefenster: Rot, wenn `PlanUnitSelect` die geladene Liste ohne Fachabgleich zeigt
  // (`units.data.subjectId === subjectId`) – dann stünden die Units des Englisch-Buchs unter Französisch zur Wahl.
  it("zeigt nach dem Fachwechsel keine Units des alten Fachs, solange das neue lädt", async () => {
    render(<MemoryRouter><VaterPlanCreate /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "3" } });
    await settle();
    const unit = screen.getByLabelText("Unit (optional)", { exact: true }) as HTMLSelectElement;
    fireEvent.change(unit, { target: { value: "302" } });
    expect(optionLabels(unit)).toContain("Unit 2");

    // Die Reihen von Französisch bleiben unterwegs.
    textbookSeries.mockImplementation(() => new Promise(() => {}));
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "4" } });
    await settle();

    expect(unit.value).toBe("");
    expect(optionLabels(unit)).not.toContain("Unit 1");
    expect(optionLabels(unit)).not.toContain("Unit 2");
  });
});

describe("Plan bearbeiten", () => {
  const plan = (over: Partial<PlanResponse>): PlanResponse => ({
    id: 5, childId: 7, title: "Plan", startDate: "2026-11-01", endDate: "2026-11-30", active: true, positionCount: 0,
    isPlayable: true, subjectId: 3, seriesUnitId: 301, seriesUnitLabel: "Unit 1", ...over,
  } as PlanResponse);

  // Rot, wenn der Speichern-Knopf bei einem Plan ohne Fach frei ist (der Server wiese den PATCH ab).
  it("sperrt Speichern für einen Plan ohne Fach, bis eines gewählt ist", async () => {
    const onSave = vi.fn();
    render(<MemoryRouter><PlanEditForm plan={plan({ subjectId: null, seriesUnitId: null, seriesUnitLabel: null })}
      childName="Theo" busy={false} onSave={onSave} /></MemoryRouter>);
    await settle();
    const save = screen.getByRole("button", { name: "Speichern" }) as HTMLButtonElement;
    expect(save.disabled).toBe(true);

    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "4" } });
    expect(save.disabled).toBe(false);
    fireEvent.click(save);
    expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ subjectId: 4 }));
  });

  // Rot, wenn eine geleerte Unit als `seriesUnitId: null` reist – das hieße beim PATCH „nicht angegeben".
  it("entfernt die Unit über den Schalter und lässt das Fach stehen", async () => {
    const onSave = vi.fn();
    render(<MemoryRouter><PlanEditForm plan={plan({})} childName="Theo" busy={false} onSave={onSave} /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Unit (optional)", { exact: true }), { target: { value: "" } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));

    const dto = onSave.mock.calls[0][0] as UpdatePlanDto;
    expect(dto.clearSeriesUnit).toBe(true);
    expect(dto.seriesUnitId).toBeUndefined();
    expect(dto.subjectId).toBeUndefined();
  });

  // Rot, wenn der Fachwechsel im Formular die Unit stehen lässt (`seriesUnitId: ""` im onChange des Fachs): dann
  // reiste nur `subjectId: 4`, Unit 301 (Englisch) bliebe am Plan, und der Server wiese den Widerspruch ab.
  it("entfernt beim Fachwechsel die alte Unit ausdrücklich", async () => {
    const onSave = vi.fn();
    render(<MemoryRouter><PlanEditForm plan={plan({})} childName="Theo" busy={false} onSave={onSave} /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "4" } });
    await settle();
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));

    expect(onSave).toHaveBeenCalledWith({ subjectId: 4, clearSeriesUnit: true });
  });
});

describe("Plan-Seite: Aktivieren", () => {
  const plan = (over: Partial<PlanResponse>): PlanResponse => ({
    id: 5, childId: 7, title: "Plan", startDate: "2026-11-01", endDate: "2026-11-30", active: false, positionCount: 0,
    isPlayable: false, subjectId: null, seriesUnitId: null, seriesUnitLabel: null, description: null, ...over,
  } as PlanResponse);

  const renderPage = async () => {
    render(<MemoryRouter initialEntries={["/vater/plan/5"]}>
      <Routes><Route path="/vater/plan/:planId" element={<VaterPlanDetail />} /></Routes>
    </MemoryRouter>);
    await settle();
  };

  // Rot, wenn der Knopf wieder nur an `busy` hängt: ohne Fach antwortete der Server mit einem englischen 400.
  it("sperrt Aktivieren für einen inaktiven Plan ohne Fach", async () => {
    loadPlan.mockResolvedValue(plan({}));
    await renderPage();
    expect((screen.getByRole("button", { name: "Aktivieren" }) as HTMLButtonElement).disabled).toBe(true);
  });

  // Rot, wenn die Sperre auch das Abschalten trifft: ein aktiver Altplan ohne Fach muss sich abschalten lassen.
  it("lässt einen aktiven Plan ohne Fach abschalten", async () => {
    loadPlan.mockResolvedValue(plan({ active: true, isPlayable: true }));
    await renderPage();
    const off = screen.getByRole("button", { name: "Deaktivieren" }) as HTMLButtonElement;
    expect(off.disabled).toBe(false);
    fireEvent.click(off);
    await settle();
    expect(updatePlan).toHaveBeenCalledWith(5, { active: false });
  });
});
