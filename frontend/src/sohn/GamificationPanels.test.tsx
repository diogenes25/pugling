import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { de } from "../i18n/de";
import { setLanguage } from "../lib/i18n";
import type { MissionStatus } from "../lib/types";

/**
 * B-528: jede offene Mission nennt ihre Frist („noch 4 Tage", „nur noch heute"); die Restzeit rechnet der Server in
 * UTC (`daysLeft`), das Panel formuliert nur. Eine erledigte Mission zeigt weiter die Belohnung, keine Frist.
 */
const missions = vi.fn();

vi.mock("./SohnApp", () => ({ useSohn: () => ({ childId: 7, celebrate: () => {} }) }));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return { ...original, api: { missions: () => missions() } };
});

const { MissionsPanel } = await import("./GamificationPanels");

const mission = (over: Partial<MissionStatus>): MissionStatus => ({
  id: 1, title: "Wörter", metric: "NewWords", period: "Weekly", target: 10, current: 3, completed: false, rewardPoints: 5,
  daysLeft: 4, ...over,
} as MissionStatus);

beforeEach(async () => {
  await setLanguage("de");
  missions.mockReset();
});

describe("MissionsPanel, Restzeit (B-528)", () => {
  // AK 4. Rot, wenn die Frist fehlt (dann stünde wieder „Diese Woche") oder die Zahl nicht vom Server käme.
  it("zeigt bei einer offenen Wochenmission die Tage bis zum Periodenende", async () => {
    missions.mockResolvedValue([mission({ daysLeft: 4 })]);
    render(<MissionsPanel />);

    expect(await screen.findByText("3/10 · noch 4 Tage")).toBeTruthy();
  });

  // AK 4, letzter Tag. Rot, wenn 0 als „noch 0 Tage" erscheint statt „nur noch heute".
  it("sagt am letzten Tag nur noch heute", async () => {
    missions.mockResolvedValue([mission({ daysLeft: 0 })]);
    render(<MissionsPanel />);

    expect(await screen.findByText(`3/10 · ${de.sohn.missions.lastDay}`)).toBeTruthy();
  });

  // AK 4, erledigt. Rot, wenn die Frist auch bei einer erledigten Mission erscheint.
  it("zeigt bei einer erledigten Mission die Belohnung und keine Frist", async () => {
    missions.mockResolvedValue([mission({ completed: true, current: 10, daysLeft: 4 })]);
    render(<MissionsPanel />);

    expect(await screen.findByText("+5")).toBeTruthy();
    expect(screen.queryByText(/noch 4 Tage/)).toBeNull();
  });

  // Einmalige Mission: ohne Frist bleibt der Zeitraum stehen. Rot, wenn `null` als Zahl formatiert würde.
  it("nennt bei einer einmaligen Mission den Zeitraum statt einer Frist", async () => {
    missions.mockResolvedValue([mission({ period: "OneOff", daysLeft: null })]);
    render(<MissionsPanel />);

    expect(await screen.findByText(`3/10 · ${de.sohn.missions.periodOneOff}`)).toBeTruthy();
  });
});
