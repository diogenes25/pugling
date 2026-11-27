import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { formatDateRange } from "../lib/datum";
import type { PlanResponse, UpdatePlanDto } from "../lib/types";

/**
 * B-735: ein Stoffrahmen ohne Termin. Anlegen schickt `withoutEnd`, Bearbeiten leert den Termin über den Schalter
 * (`null` hieße beim PATCH „nicht angegeben“), und kein Ort zeigt ein Platzhalterdatum.
 */
const createPlan = vi.fn();

vi.mock("../lib/auth", () => ({ useAuth: () => ({ session: { id: 1 } }) }));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      children: () => Promise.resolve([{ id: 7, name: "Theo" }]),
      subjects: () => Promise.resolve([{ id: 3, name: "Englisch" }]),
      textbookSeries: () => Promise.resolve([]),
      seriesUnits: () => Promise.resolve([]),
      createPlan: (...a: unknown[]) => createPlan(...a),
    },
  };
});

const { VaterPlanCreate } = await import("./VaterPlanCreate");
const { PlanEditForm } = await import("./VaterPlanDetail");

const settle = () => act(async () => {});

beforeEach(() => createPlan.mockReset().mockResolvedValue({ id: 99 }));

const plan = (over: Partial<PlanResponse>): PlanResponse => ({
  id: 5, childId: 7, title: "Plan", startDate: "2026-11-01", endDate: "2026-11-30", active: true, positionCount: 0,
  isPlayable: true, subjectId: 3, seriesUnitId: null, seriesUnitLabel: null, ...over,
} as PlanResponse);

describe("Offener Plan", () => {
  // Rot, wenn `withoutEnd` nicht mitreist: der Server legte dann einen Plan mit zehn erfundenen Tagen an.
  it("legt mit „Kein Termin“ einen Plan ohne Ende an", async () => {
    render(<MemoryRouter><VaterPlanCreate /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "3" } });
    fireEvent.click(screen.getByLabelText(/Kein Termin/));
    fireEvent.click(screen.getByRole("button", { name: "Plan anlegen & Übungen hinzufügen" }));
    await settle();
    expect(createPlan).toHaveBeenCalledWith(expect.objectContaining({ withoutEnd: true }));
  });

  it("schickt ohne den Haken withoutEnd: false", async () => {
    render(<MemoryRouter><VaterPlanCreate /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Fach (Pflicht)", { exact: true }), { target: { value: "3" } });
    fireEvent.click(screen.getByRole("button", { name: "Plan anlegen & Übungen hinzufügen" }));
    await settle();
    expect(createPlan).toHaveBeenCalledWith(expect.objectContaining({ withoutEnd: false }));
  });

  // Rot, wenn ein geleertes Ende nicht als `clearEndDate` reist (altes `form.endDate && …` schickte gar nichts).
  it("leert den Termin über den Schalter", async () => {
    const onSave = vi.fn();
    render(<MemoryRouter><PlanEditForm plan={plan({})} childName="Theo" busy={false} onSave={onSave} /></MemoryRouter>);
    await settle();
    fireEvent.change(screen.getByLabelText("Ende"), { target: { value: "" } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));
    const dto = onSave.mock.calls[0][0] as UpdatePlanDto;
    expect(dto.clearEndDate).toBe(true);
    expect(dto.endDate).toBeUndefined();
  });

  // Rot, wenn `null` als „1970“ oder „Invalid Date“ gezeigt wird, oder wenn ein offener Plan ohne Eingabe `clearEndDate` schickt.
  it("zeigt einen offenen Plan als „kein Termin“ und trägt einen Termin nach", async () => {
    expect(formatDateRange("2026-11-01", null)).toContain("kein Termin");
    const onSave = vi.fn();
    render(<MemoryRouter><PlanEditForm plan={plan({ endDate: null })} childName="Theo" busy={false} onSave={onSave} /></MemoryRouter>);
    await settle();
    expect((screen.getByLabelText("Ende") as HTMLInputElement).value).toBe("");
    fireEvent.change(screen.getByLabelText("Ende"), { target: { value: "2026-12-15" } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));
    expect(onSave).toHaveBeenCalledWith({ endDate: "2026-12-15" });
  });
});
