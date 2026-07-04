import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type {
  ChildResponse, CreatePlanDto, ExerciseSummary, SchoolType, SubjectResponse, VocabularyResponse,
} from "../lib/types";

/*
 * Lehrplan-Assistent: führt den Vater in fünf Schritten von „welches Kind" über einen kurzen
 * Fragenkatalog (Problemfeld, Ziel, Intensität) und die Auswahl vorhandener Inhalte bis zum
 * fertigen Vokabel-Lehrplan. Die Fragen setzen sinnvolle Voreinstellungen – der Vater kann alles
 * im Schritt „Feinschliff" überschreiben. Der manuelle Weg (Dashboard → Vokabeln → Neuer Plan)
 * bleibt daneben bestehen; der Assistent ist die geführte Abkürzung.
 */

const SCHOOL_TYPES: SchoolType[] = [
  "Grundschule", "Hauptschule", "Realschule", "Gymnasium", "Gesamtschule", "Berufsschule",
];

// Fach -> Quellsprache des Vokabel-Stores (filtert die auswählbaren Vokabeln).
const SUBJECT_LANG: Record<string, string> = { "Französisch": "fr", "Englisch": "en", "Spanisch": "es", "Latein": "la" };

type Goal = "Klassenarbeit" | "Aufholen" | "Regelmaessig";
type Intensity = "Locker" | "Normal" | "Intensiv";

// Intensität -> Tages-Pensum (Minuten, neue Wörter, Bestehensgrenze).
const INTENSITY: Record<Intensity, { minutes: number; newWords: number; pass: number; label: string; hint: string }> = {
  Locker: { minutes: 10, newWords: 3, pass: 70, label: "Locker", hint: "10 Min · 3 neue Wörter/Tag" },
  Normal: { minutes: 15, newWords: 5, pass: 80, label: "Normal", hint: "15 Min · 5 neue Wörter/Tag" },
  Intensiv: { minutes: 25, newWords: 8, pass: 90, label: "Intensiv", hint: "25 Min · 8 neue Wörter/Tag" },
};

const GOALS: Record<Goal, { label: string; emoji: string; hint: string; duration: number; stage: number; typed: boolean }> = {
  Klassenarbeit: { label: "Klassenarbeit vorbereiten", emoji: "📝", hint: "Kurzer, strammer Plan mit getipptem Test", duration: 14, stage: 4, typed: true },
  Aufholen: { label: "Rückstand aufholen", emoji: "🪜", hint: "Mehrere Wochen dranbleiben", duration: 21, stage: 2, typed: false },
  Regelmaessig: { label: "Regelmäßig üben", emoji: "🔁", hint: "Dauerhaftes, entspanntes Training", duration: 30, stage: 2, typed: false },
};

const STEPS = ["Kind", "Problemfeld", "Inhalte", "Feinschliff", "Überblick"] as const;

function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export function VaterWizard() {
  const { session } = useAuth();
  const nav = useNavigate();
  const children = useAsync<ChildResponse[]>(() => api.children(), [session!.id]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  const [step, setStep] = useState(0);

  // --- Schritt 1: Kind ---
  const [mode, setMode] = useState<"existing" | "new">("new");
  const [childId, setChildId] = useState<number | "">("");
  const [newName, setNewName] = useState("");
  const [newBirthYear, setNewBirthYear] = useState("");
  const [grade, setGrade] = useState<number | "">("");
  const [schoolType, setSchoolType] = useState<SchoolType>("Gymnasium");
  const [newPin, setNewPin] = useState("");

  // --- Schritt 2: Fragenkatalog ---
  const [subjectId, setSubjectId] = useState<number | "">("");
  const [topic, setTopic] = useState("");
  const [goal, setGoal] = useState<Goal>("Klassenarbeit");
  const [intensity, setIntensity] = useState<Intensity>("Normal");

  // --- Schritt 3: Inhalte ---
  const [contentSearch, setContentSearch] = useState("");
  const [selected, setSelected] = useState<string[]>([]);

  // --- Schritt 4: Feinschliff ---
  const [title, setTitle] = useState("");
  const [startDate, setStartDate] = useState(todayIso());
  const [durationDays, setDurationDays] = useState(14);
  const [dailyMinutes, setDailyMinutes] = useState(15);
  const [newItemsPerLesson, setNewItemsPerLesson] = useState(5);
  const [passPercent, setPassPercent] = useState(80);
  const [defaultStage, setDefaultStage] = useState(4);
  const [requireTyped, setRequireTyped] = useState(true);
  const [useLeitner, setUseLeitner] = useState(true);
  const [comboThreshold, setComboThreshold] = useState(5);
  const [comboBonusPoints, setComboBonusPoints] = useState(5);
  const [touchedFineTune, setTouchedFineTune] = useState(false);

  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const selectedChild = children.data?.find((c) => c.id === childId);
  const effectiveGrade = mode === "new" ? (grade === "" ? undefined : Number(grade)) : selectedChild?.grade ?? undefined;
  const effectiveSchoolType: SchoolType | undefined =
    mode === "new" ? schoolType : (selectedChild?.schoolType as SchoolType | undefined);
  const subject = subjects.data?.find((s) => s.id === subjectId);
  const subjectLang = subject ? SUBJECT_LANG[subject.name] : undefined;

  // Vokabel-Store (auswählbare Inhalte eines Vokabel-Plans). Nach Fachsprache vorfiltern.
  const vocab = useAsync<VocabularyResponse[]>(() => api.vocabulary(), []);
  // Passende Katalog-Übungen (nur zur Orientierung: „solche Übungen gibt es zu Klasse/Fach").
  const exercises = useAsync<ExerciseSummary[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.searchExercises({
      subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
    })),
    [subjectId, effectiveGrade, effectiveSchoolType],
  );

  // Erstes Kind vorwählen; wenn schon Kinder existieren, „bestehendes" als Standard.
  useEffect(() => {
    if (children.data && children.data.length > 0) {
      if (mode === "new" && childId === "") { setMode("existing"); setChildId(children.data[0].id); }
    }
  }, [children.data]); // eslint-disable-line react-hooks/exhaustive-deps

  const filteredVocab = useMemo(() => {
    let v = vocab.data ?? [];
    if (subjectLang) v = v.filter((x) => x.sourceLanguage === subjectLang);
    const s = contentSearch.trim().toLowerCase();
    if (s) v = v.filter((x) => x.word.toLowerCase().includes(s) || x.translation.toLowerCase().includes(s) || x.key.toLowerCase().includes(s));
    return v;
  }, [vocab.data, subjectLang, contentSearch]);

  // Voreinstellungen aus dem Fragenkatalog ableiten, solange der Vater den Feinschliff nicht angefasst hat.
  function applyDefaults() {
    if (touchedFineTune) return;
    const g = GOALS[goal];
    const it = INTENSITY[intensity];
    setDurationDays(g.duration);
    setDefaultStage(g.stage);
    setRequireTyped(g.typed);
    setDailyMinutes(it.minutes);
    setNewItemsPerLesson(it.newWords);
    setPassPercent(it.pass);
    const subjName = subject?.name ?? "Lernplan";
    setTitle(topic.trim() ? `${subjName} – ${topic.trim()}` : `${subjName} – ${g.label}`);
  }

  function toggle(key: string) {
    setSelected((sel) => (sel.includes(key) ? sel.filter((k) => k !== key) : [...sel, key]));
  }
  function selectAll() { setSelected(filteredVocab.map((v) => v.key)); }

  function canAdvance(): string | null {
    if (step === 0) {
      if (mode === "existing") return childId === "" ? "Bitte ein Kind wählen." : null;
      return newName.trim() ? null : "Bitte einen Namen eingeben.";
    }
    if (step === 1) return subjectId === "" ? "Bitte ein Fach wählen." : null;
    if (step === 2) return selected.length === 0 ? "Bitte mindestens eine Vokabel wählen." : null;
    return null;
  }

  function next() {
    const problem = canAdvance();
    if (problem) { setError(problem); return; }
    setError(null);
    if (step === 1) applyDefaults();
    setStep((s) => Math.min(s + 1, STEPS.length - 1));
  }
  function back() { setError(null); setStep((s) => Math.max(s - 1, 0)); }

  async function finish() {
    setError(null);
    setBusy(true);
    try {
      let targetChildId = mode === "existing" ? Number(childId) : 0;
      if (mode === "new") {
        const created = await api.createChild({
          name: newName.trim(),
          pin: newPin || undefined,
          birthYear: newBirthYear ? Number(newBirthYear) : null,
          grade: grade === "" ? null : Number(grade),
          schoolType,
        });
        targetChildId = created.id;
      }
      const dto: CreatePlanDto = {
        childId: targetChildId,
        title: title.trim() || "Neuer Lehrplan",
        method: "Vocabulary",
        subjectId: subjectId === "" ? null : Number(subjectId),
        durationDays,
        startDate,
        newItemsPerLesson,
        contentKeys: selected,
        dailyMinutesRequired: dailyMinutes,
        dailyTestPassPercent: passPercent,
        defaultStage,
        requireTypedTest: requireTyped,
        useLeitner,
        comboThreshold,
        comboBonusPoints,
      };
      const plan = await api.createPlan(dto);
      nav(`/vater/plan/${plan.id}`);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  const currentYear = new Date().getFullYear();
  const gradeHint = newBirthYear && Number(newBirthYear) > 1990
    ? `≈ ${Math.max(1, currentYear - Number(newBirthYear) - 6)}. Klasse`
    : null;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      <div className="row">
        <h2 className="h-section" style={{ margin: 0 }}>🧭 Lehrplan-Assistent</h2>
        <span className="muted" style={{ marginLeft: "auto", fontSize: 13 }}>Schritt {step + 1} von {STEPS.length}</span>
      </div>

      <Stepper step={step} onGo={(i) => i < step && setStep(i)} />

      {/* -------- Schritt 1: Kind -------- */}
      {step === 0 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h3 style={{ margin: 0 }}>Für welches Kind?</h3>
          <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
            <ChoicePill active={mode === "existing"} disabled={(children.data?.length ?? 0) === 0}
              onClick={() => setMode("existing")}>Bestehendes Kind</ChoicePill>
            <ChoicePill active={mode === "new"} onClick={() => setMode("new")}>Neues Kind anlegen</ChoicePill>
          </div>

          {mode === "existing" ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label>Kind</label>
              <select aria-label="Kind" value={childId} onChange={(e) => setChildId(Number(e.target.value))}>
                {children.data?.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}{c.grade ? ` · ${c.grade}. Klasse` : ""} (#{c.id})</option>
                ))}
              </select>
            </div>
          ) : (
            <div className="form-grid">
              <div className="field"><label>Name</label><input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="z.B. Max" /></div>
              <div className="field"><label>Geburtsjahr</label><input type="number" value={newBirthYear} onChange={(e) => setNewBirthYear(e.target.value)} placeholder="z.B. 2012" />{gradeHint && <span className="sub">{gradeHint}</span>}</div>
              <div className="field"><label>Klasse</label><input type="number" min={1} max={13} value={grade} onChange={(e) => setGrade(e.target.value === "" ? "" : Number(e.target.value))} placeholder="z.B. 8" /></div>
              <div className="field"><label>Schulart</label>
                <select aria-label="Schulart" value={schoolType} onChange={(e) => setSchoolType(e.target.value as SchoolType)}>
                  {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
              <div className="field"><label>PIN (Login des Kindes)</label><input value={newPin} onChange={(e) => setNewPin(e.target.value)} placeholder="z.B. 1111" /></div>
            </div>
          )}
        </section>
      )}

      {/* -------- Schritt 2: Fragenkatalog -------- */}
      {step === 1 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <h3 style={{ margin: 0 }}>Wo hakt es?</h3>
          <div className="form-grid">
            <div className="field"><label>Fach</label>
              <select aria-label="Fach" value={subjectId} onChange={(e) => setSubjectId(e.target.value === "" ? "" : Number(e.target.value))}>
                <option value="">– Fach wählen –</option>
                {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div className="field"><label>Thema / Kapitel (optional)</label><input value={topic} onChange={(e) => setTopic(e.target.value)} placeholder="z.B. Unité 2 – En ville" /></div>
          </div>

          <div>
            <label className="sub" style={{ display: "block", marginBottom: 8 }}>Was ist das Ziel?</label>
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
              {(Object.keys(GOALS) as Goal[]).map((g) => (
                <OptionCard key={g} active={goal === g} onClick={() => setGoal(g)}
                  title={`${GOALS[g].emoji} ${GOALS[g].label}`} hint={GOALS[g].hint} />
              ))}
            </div>
          </div>

          <div>
            <label className="sub" style={{ display: "block", marginBottom: 8 }}>Wie intensiv soll geübt werden?</label>
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
              {(Object.keys(INTENSITY) as Intensity[]).map((i) => (
                <OptionCard key={i} active={intensity === i} onClick={() => setIntensity(i)}
                  title={INTENSITY[i].label} hint={INTENSITY[i].hint} />
              ))}
            </div>
          </div>
          <p className="sub">Aus diesen Angaben schlägt der Assistent Dauer, Pensum und Test-Stufe vor – anpassbar im Schritt „Feinschliff".</p>
        </section>
      )}

      {/* -------- Schritt 3: Inhalte -------- */}
      {step === 2 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <div className="row">
            <h3 style={{ margin: 0 }}>Inhalte wählen <span className="muted">({selected.length} gewählt)</span></h3>
            <input style={{ marginLeft: "auto", maxWidth: 220 }} placeholder="Vokabel suchen…" value={contentSearch} onChange={(e) => setContentSearch(e.target.value)} aria-label="Vokabel suchen" />
          </div>

          {exercises.data && exercises.data.length > 0 && (
            <div className="banner ok" style={{ background: "rgba(38,217,255,.1)", color: "var(--cyan)", borderColor: "var(--stroke)" }}>
              📚 Passende Katalog-Übungen für {subject?.name}{effectiveGrade ? `, ${effectiveGrade}. Klasse` : ""}:{" "}
              {exercises.data.map((e) => `${e.title}${e.source ? ` (${e.source})` : ""}`).join(" · ")}
            </div>
          )}

          {selected.length > 0 && (
            <div className="tokenlist">
              {selected.map((k) => {
                const v = vocab.data?.find((x) => x.key === k);
                return <span className="token" key={k}>{v ? `${v.word}→${v.translation}` : k}<button type="button" onClick={() => toggle(k)}>×</button></span>;
              })}
            </div>
          )}

          <div className="row">
            <span className="sub">
              Vokabeln aus dem Store{subjectLang ? ` (${subjectLang.toUpperCase()})` : ""} – die Bausteine des Lehrplans.
            </span>
            {filteredVocab.length > 0 && <button type="button" className="btn ghost inline-btn" style={{ marginLeft: "auto" }} onClick={selectAll}>Alle wählen</button>}
          </div>

          {vocab.loading ? <div className="loading">Lade Vokabeln…</div> : filteredVocab.length === 0 ? (
            <div className="banner err">
              Keine passenden Vokabeln im Store. Lege welche unter „Vokabeln" an{subjectLang ? ` (Quellsprache „${subjectLang}")` : ""}.
            </div>
          ) : (
            <div style={{ maxHeight: 320, overflowY: "auto", display: "grid", gridTemplateColumns: "repeat(auto-fill,minmax(220px,1fr))", gap: 6 }}>
              {filteredVocab.map((v) => (
                <label key={v.id} className="checkline" style={{ padding: 8, border: "1px solid var(--stroke)", borderRadius: 8 }}>
                  <input type="checkbox" checked={selected.includes(v.key)} onChange={() => toggle(v.key)} />
                  <span>{v.word} <span className="muted">→ {v.translation}</span></span>
                </label>
              ))}
            </div>
          )}
        </section>
      )}

      {/* -------- Schritt 4: Feinschliff -------- */}
      {step === 3 && (
        <section className="card">
          <h3 style={{ margin: "0 0 12px" }}>Feinschliff</h3>
          <div className="form-grid" onChange={() => setTouchedFineTune(true)}>
            <div className="field"><label>Titel</label><input value={title} onChange={(e) => setTitle(e.target.value)} /></div>
            <div className="field"><label>Start</label><input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></div>
            <div className="field"><label>Dauer (Tage)</label><input type="number" min={1} value={durationDays} onChange={(e) => setDurationDays(Number(e.target.value))} /></div>
            <div className="field"><label>Neue Wörter/Tag</label><input type="number" min={1} value={newItemsPerLesson} onChange={(e) => setNewItemsPerLesson(Number(e.target.value))} /></div>
            <div className="field"><label>Minuten/Tag</label><input type="number" min={1} value={dailyMinutes} onChange={(e) => setDailyMinutes(Number(e.target.value))} /></div>
            <div className="field"><label>Bestehen ab %</label><input type="number" min={1} max={100} value={passPercent} onChange={(e) => setPassPercent(Number(e.target.value))} /></div>
            <div className="field"><label>Combo alle … Treffer</label><input type="number" min={0} value={comboThreshold} onChange={(e) => setComboThreshold(Number(e.target.value))} /></div>
            <div className="field"><label>Combo-Bonuspunkte</label><input type="number" min={0} value={comboBonusPoints} onChange={(e) => setComboBonusPoints(Number(e.target.value))} /></div>
            <div className="field"><label>Test-Stufe</label>
              <select aria-label="Test-Stufe" value={defaultStage} onChange={(e) => setDefaultStage(Number(e.target.value))}>
                <option value={1}>1 · Zeigen</option>
                <option value={2}>2 · Selbstcheck</option>
                <option value={3}>3 · Buchstaben</option>
                <option value={4}>4 · Tippen</option>
                <option value={5}>5 · Hören</option>
              </select>
            </div>
          </div>
          <div className="row" style={{ marginTop: 12, gap: 20, flexWrap: "wrap" }}>
            <label className="checkline"><input type="checkbox" checked={useLeitner} onChange={(e) => { setTouchedFineTune(true); setUseLeitner(e.target.checked); }} /> Leitner-Kasten (Übungspunkte)</label>
            <label className="checkline"><input type="checkbox" checked={requireTyped} onChange={(e) => { setTouchedFineTune(true); setRequireTyped(e.target.checked); }} /> Nur getippte Tests zählen</label>
          </div>
        </section>
      )}

      {/* -------- Schritt 5: Überblick -------- */}
      {step === 4 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          <h3 style={{ margin: 0 }}>Überblick</h3>
          <SummaryRow label="Kind" value={mode === "new" ? `${newName || "?"} (neu${grade ? `, ${grade}. Klasse` : ""})` : `${selectedChild?.name ?? "?"}${selectedChild?.grade ? `, ${selectedChild.grade}. Klasse` : ""}`} />
          <SummaryRow label="Fach" value={subject?.name ?? "–"} />
          <SummaryRow label="Ziel · Intensität" value={`${GOALS[goal].label} · ${INTENSITY[intensity].label}`} />
          <SummaryRow label="Titel" value={title || "Neuer Lehrplan"} />
          <SummaryRow label="Zeitraum" value={`${startDate} · ${durationDays} Tage`} />
          <SummaryRow label="Pensum" value={`${dailyMinutes} Min/Tag · ${newItemsPerLesson} neue Wörter · Test-Stufe ${defaultStage} · bestehen ab ${passPercent}%`} />
          <SummaryRow label="Vokabeln" value={`${selected.length} gewählt`} />
          <p className="sub">Danach erscheint der Plan in der Übersicht; der Sohn sieht ihn sofort in seiner App.</p>
        </section>
      )}

      {error && <div className="banner err">{error}</div>}

      <div className="row" style={{ gap: 10 }}>
        {step > 0 && <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={back}>← Zurück</button>}
        <span style={{ marginLeft: "auto" }} />
        {step < STEPS.length - 1
          ? <button type="button" className="btn" style={{ width: "auto" }} onClick={next}>Weiter →</button>
          : <button type="button" className="btn lime" style={{ width: "auto" }} disabled={busy} onClick={finish}>{busy ? "…" : "✅ Lehrplan erstellen"}</button>}
      </div>
    </div>
  );
}

function Stepper({ step, onGo }: { step: number; onGo: (i: number) => void }) {
  return (
    <div className="row" style={{ gap: 6, flexWrap: "wrap" }}>
      {STEPS.map((s, i) => (
        <button key={s} type="button" onClick={() => onGo(i)}
          className={`pill ${i === step ? "cyan" : i < step ? "lime" : ""}`}
          style={{ border: "1.5px solid var(--stroke)", cursor: i < step ? "pointer" : "default", background: i === step ? undefined : i < step ? undefined : "#0c0e2c" }}>
          {i + 1}. {s}
        </button>
      ))}
    </div>
  );
}

function ChoicePill({ active, disabled, onClick, children }: { active: boolean; disabled?: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button type="button" disabled={disabled} onClick={onClick}
      className={`btn ${active ? "" : "ghost"} inline-btn`} style={{ width: "auto", opacity: disabled ? 0.4 : 1 }}>
      {children}
    </button>
  );
}

function OptionCard({ active, onClick, title, hint }: { active: boolean; onClick: () => void; title: string; hint: string }) {
  return (
    <button type="button" onClick={onClick}
      style={{
        flex: "1 1 180px", textAlign: "left", cursor: "pointer", padding: "12px 14px", borderRadius: 14,
        border: active ? "2px solid var(--cyan)" : "1.5px solid var(--stroke)",
        background: active ? "rgba(38,217,255,.1)" : "#0c0e2c", color: "var(--ink)",
        boxShadow: active ? "0 0 0 3px rgba(38,217,255,.15)" : "none",
      }}>
      <div style={{ fontWeight: 800, fontSize: 14 }}>{title}</div>
      <div className="muted" style={{ fontSize: 12, marginTop: 4 }}>{hint}</div>
    </button>
  );
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="row" style={{ alignItems: "baseline", gap: 12 }}>
      <span className="muted" style={{ minWidth: 150, fontSize: 12, textTransform: "uppercase", letterSpacing: ".06em" }}>{label}</span>
      <span style={{ fontWeight: 600 }}>{value}</span>
    </div>
  );
}
