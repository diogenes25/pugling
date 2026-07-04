import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { AchievementStatus, MissionPeriod, MissionStatus } from "../lib/types";

const PERIOD_LABEL: Record<MissionPeriod, string> = {
  Daily: "Heute",
  Weekly: "Diese Woche",
  OneOff: "Ziel",
};

/**
 * Missionen des Sohns mit Fortschrittsbalken (Tages-/Wochen-/Zusatzziele). Der Server wertet beim
 * Abruf aus und belohnt fällige Ziele – die Anzeige spiegelt also den echten, gutgeschriebenen Stand.
 */
export function MissionsPanel() {
  const missions = useAsync<MissionStatus[]>(() => api.missions(), []);
  if (missions.loading || missions.error || !missions.data || missions.data.length === 0) return null;

  return (
    <div className="card">
      <div className="row" style={{ marginBottom: 10 }}>
        <b style={{ fontSize: 13 }}>🎯 Missionen</b>
        <span className="sub" style={{ marginLeft: "auto" }}>
          {missions.data.filter((m) => m.completed).length}/{missions.data.length} geschafft
        </span>
      </div>
      {missions.data.map((m) => {
        const pct = Math.min(100, Math.round((m.current / Math.max(1, m.target)) * 100));
        return (
          <div key={m.id} style={{ marginBottom: 10 }}>
            <div className="row" style={{ marginBottom: 4 }}>
              <span style={{ fontSize: 12 }}>{m.completed ? "✅ " : ""}{m.title}</span>
              <span className="sub" style={{ marginLeft: "auto" }}>
                {m.completed
                  ? <span className="pill lime">+{m.rewardPoints}</span>
                  : `${m.current}/${m.target} · ${PERIOD_LABEL[m.period]}`}
              </span>
            </div>
            <div className={`bar ${m.completed ? "" : "cyan"}`}><i style={{ width: `${pct}%` }} /></div>
          </div>
        );
      })}
    </div>
  );
}

/**
 * Auszeichnungs-Galerie (Duolingo-artig): erreichte Badges leuchten in Gold, offene sind gedimmt
 * und zeigen den Fortschritt zur Schwelle. Erreichte erscheinen zuerst (Serverreihenfolge).
 */
export function BadgesGallery() {
  const achievements = useAsync<AchievementStatus[]>(() => api.achievements(), []);
  if (achievements.loading || achievements.error || !achievements.data || achievements.data.length === 0) return null;

  const earnedCount = achievements.data.filter((a) => a.earned).length;
  return (
    <div className="card">
      <div className="row" style={{ marginBottom: 12 }}>
        <b style={{ fontSize: 13 }}>🏅 Auszeichnungen</b>
        <span className="sub" style={{ marginLeft: "auto" }}>{earnedCount}/{achievements.data.length}</span>
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 12 }}>
        {achievements.data.map((a) => {
          const pct = Math.min(100, Math.round((a.current / Math.max(1, a.threshold)) * 100));
          return (
            <div key={a.id} style={{ textAlign: "center", opacity: a.earned ? 1 : 0.55 }}>
              <div
                style={{
                  width: 54, height: 54, margin: "0 auto 6px", borderRadius: 15,
                  display: "grid", placeItems: "center", fontSize: 26,
                  border: "2px solid var(--stroke)",
                  background: a.earned
                    ? "linear-gradient(160deg, var(--gold), var(--gold-2))"
                    : "#141748",
                  filter: a.earned ? "none" : "grayscale(1)",
                  boxShadow: a.earned ? "0 0 14px rgba(255,199,56,.45)" : "none",
                }}
                title={a.title}
              >
                {a.icon ?? "🏅"}
              </div>
              <div style={{ fontSize: 10, fontWeight: 800, lineHeight: 1.2 }}>{a.title}</div>
              <div className="sub" style={{ fontSize: 10 }}>
                {a.earned ? "erreicht" : `${a.current}/${a.threshold} (${pct}%)`}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
