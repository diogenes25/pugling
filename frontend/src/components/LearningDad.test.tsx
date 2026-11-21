import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LearningDad } from "./LearningDad";
import { CelebrationLayer, type Celebration } from "./Celebration";

describe("LearningDad Component", () => {
  it("rendert standardmäßig die Daumen-hoch-Animation", () => {
    render(<LearningDad action="thumbs-up" />);
    const img = screen.getByRole("img", { name: /Daumen hoch/i });
    expect(img).toBeTruthy();
    expect(img.getAttribute("src")).toContain("dad_thumbs_up.webp");
  });

  it("rendert die Facepalm-Animation für Fehler und Aufmunterung", () => {
    render(<LearningDad action="facepalm" />);
    const img = screen.getByRole("img", { name: /Kopf/i });
    expect(img).toBeTruthy();
    expect(img.getAttribute("src")).toContain("dad_facepalm.webp");
  });

  it("rendert die Uhr-Animation", () => {
    render(<LearningDad action="clock" />);
    const img = screen.getByRole("img", { name: /Uhr/i });
    expect(img).toBeTruthy();
    expect(img.getAttribute("src")).toContain("dad_clock.webp");
  });

  it("zeigt Sprechblase und Untertitel an", () => {
    render(
      <LearningDad
        action="thumbs-up"
        caption="Super gemacht!"
        subCaption="+5 Münzen"
      />
    );
    expect(screen.getByText("Super gemacht!")).toBeTruthy();
    expect(screen.getByText("+5 Münzen")).toBeTruthy();
  });

  it("zeigt den Münz-Untertitel einer kleinen Feier auch ohne Titel", () => {
    // Red if the bubble hangs on `caption` alone: the small tier passes only "+N 🪙" (SohnBonus), which then vanished.
    const kleine: Celebration = { id: 1, tier: "small", emoji: "⭐", sub: "+3 🪙" };
    render(<CelebrationLayer celebration={kleine} />);
    expect(screen.getByText("+3 🪙")).toBeTruthy();
  });

  it("unterstützt HTML5 Video-Modus", () => {
    const { container } = render(<LearningDad action="thumbs-up" mode="video" />);
    const video = container.querySelector("video");
    expect(video).toBeTruthy();
    expect(video?.querySelector("source")?.getAttribute("src")).toContain("dad_thumbs_up.webm");
  });

  it("rendert Avatar im Kreisformat", () => {
    render(<LearningDad action="avatar" variant="circle" />);
    const img = screen.getByRole("img", { name: /Profil/i });
    expect(img).toBeTruthy();
    expect(img.getAttribute("src")).toContain("dad_avatar.png");
  });
});

describe("CelebrationLayer mit Learning Dad", () => {
  it("zeigt Learning Dad bei einer Erfolgsfeier mit Daumen hoch", () => {
    const celebration: Celebration = {
      id: 1,
      tier: "big",
      emoji: "🏆",
      title: "MEILENSTEIN!",
      sub: "+10 🪙",
    };
    render(<CelebrationLayer celebration={celebration} />);
    expect(screen.getByText("MEILENSTEIN!")).toBeTruthy();
    expect(screen.getByText("+10 🪙")).toBeTruthy();
    const dadImg = screen.getByRole("img", { name: /Daumen hoch/i, hidden: true });
    expect(dadImg).toBeTruthy();
  });

  it("zeigt Learning Dad bei einer Aufmunterung mit Facepalm", () => {
    const celebration: Celebration = {
      id: 2,
      tier: "encourage",
      emoji: "💪",
      title: "Kein Problem!",
      sub: "Das schaffen wir!",
    };
    render(<CelebrationLayer celebration={celebration} />);
    expect(screen.getByText("Kein Problem!")).toBeTruthy();
    expect(screen.getByText("Das schaffen wir!")).toBeTruthy();
    const dadImg = screen.getByRole("img", { name: /Kopf/i, hidden: true });
    expect(dadImg).toBeTruthy();
    expect(dadImg.getAttribute("src")).toContain("dad_facepalm.webp");
  });
});

describe("LearningDad Boxen & Wut", () => {
  it.each([
    ["punch", /Treffer/i, "dad_punch.webp"],
    ["boxing-win", /gewinnt/i, "dad_boxing_win.webp"],
    ["angry", /ist wütend/i, "dad_angry.webp"],
    ["become-angry", /wird wütend/i, "dad_become_angry.webp"],
    ["become-monster", /Monster/i, "dad_become_monster.webp"],
    ["starts-fight", /Kampf/i, "dad_starts_fight.webp"],
    ["boxing-complete", /boxt eine Serie/i, "dad_boxing_complete.webp"],
    ["applause", /Beifall/i, "dad_applause.webp"],
  ] as const)("rendert %s mit eigener Animation", (action, name, file) => {
    render(<LearningDad action={action} />);
    expect(screen.getByRole("img", { name }).getAttribute("src")).toContain(file);
  });

  it("fällt im Video-Modus auf WebP zurück, wo es kein Video gibt", () => {
    render(<LearningDad action="punch" mode="video" />);
    expect(screen.getByRole("img", { name: /Treffer/i }).tagName).toBe("IMG");
  });

  it("Combo-Feier spielt den Treffer", () => {
    const c: Celebration = { id: 1, tier: "medium", emoji: "🎉", title: "COMBO ×3", dadAction: "punch" };
    render(<CelebrationLayer celebration={c} />);
    // The layer is aria-hidden (decorative), so query hidden elements too.
    expect(screen.getByRole("img", { name: /Treffer/i, hidden: true }).getAttribute("src")).toContain("dad_punch.webp");
  });
});
