import { useCallback, useEffect, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import { ApiError, api } from "../lib/api";
import { useAction, type ActionMessage } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type {
  Paged, Remark, RemarkCategory, RemarkComment, RemarkStatus,
} from "../lib/types";

/*
 * Die Verwaltungssicht der Test-Anmerkungen – das bequeme Gegenstück zum Widget.
 *
 * Das Widget ist auf Reibungsarmut gebaut (ein Feld, Alt+A, Enter) und 360px breit; ein Verlauf mit fünf
 * Beiträgen liest sich dort schlecht. Hier ist Platz für Kontext-Schnappschuss, Auflösung, Verlauf und
 * Status – und nur hier ist der kontenübergreifende Blick möglich.
 *
 * Werkzeug für die Entwicklung, kein Produktfeature: Die Seite hängt wie das Widget an `import.meta.env.DEV`
 * (siehe Route in VaterApp).
 */

const STATUS: { value: RemarkStatus; label: string }[] = [
  { value: "Open", label: "offen" },
  { value: "Planned", label: "eingeplant" },
  { value: "Done", label: "erledigt" },
  { value: "Rejected", label: "verworfen" },
];

const CATEGORIES: RemarkCategory[] = ["Unspecified", "Bug", "Ui", "Code", "Content", "Idea", "Question"];

const STATUS_LABEL: Record<RemarkStatus, string> = {
  Open: "offen", Planned: "eingeplant", Done: "erledigt", Rejected: "verworfen",
};

export function VaterAnmerkungen() {
  const [status, setStatus] = useState<RemarkStatus | "">("Open");
  const [category, setCategory] = useState<RemarkCategory | "">("");
  const [appArea, setAppArea] = useState("");
  const [allAccounts, setAllAccounts] = useState(false);
  const [skip, setSkip] = useState(0);
  // Rückmeldung auf Seitenebene: Sie muss die Karte überleben, die sie ausgelöst hat (siehe `setStatus`).
  const [notice, setNotice] = useState<ActionMessage | null>(null);

  /*
   * Ob der kontenübergreifende Blick offensteht, entscheidet der **Server** (`Remarks:GlobalRead`, in der
   * Entwicklung an) – das Frontend kann es nicht wissen und soll es auch nicht nachbilden.
   *
   * Der Umschalter steht darum sichtbar da, und ein `403` schaltet ihn aus. Anders als beim ersten Anlauf
   * passiert das **nicht** im Hintergrund: Ein Vorab-Klopfen mit `scope=all&take=1` provozierte einen 403,
   * der über `recordHttpError` im Fehler-Ringpuffer landete – die nächste im Widget erfasste Anmerkung trug
   * dann ein `remark_scope_forbidden` im Kontext, das mit der Beobachtung nichts zu tun hatte. Der
   * Mitschnitt IST hier das Feature. Jetzt entsteht der Fehleintrag nur, wenn der Mensch wirklich geklickt
   * hat – und dann gehört er auch in den Puffer.
   */
  const [scopeRefused, setScopeRefused] = useState(false);

  /**
   * Umschalten wirkt **sofort** und wird bei einer Absage zurückgenommen.
   *
   * Erst nach dem Netz-Rundlauf umzustellen wäre die naheliegende Reihenfolge und fühlt sich falsch an: Man
   * klickt, und einen Moment passiert nichts – ein Kästchen, das nicht auf den Klick reagiert, liest sich wie
   * ein Defekt. Die Prüfung läuft darum daneben; nur wenn der Server ablehnt, springt es zurück und die
   * Erklärung tritt an seine Stelle. Sie steht hier und nicht in der Liste, weil `useAsync` bloß die Meldung
   * durchreicht, nicht den `code` – und auf einen englischen Meldungstext zu matchen wäre eine Regel, die
   * beim nächsten Feinschliff der Formulierung bricht.
   */
  async function toggleAllAccounts(checked: boolean) {
    setAllAccounts(checked);
    if (!checked) return;
    try {
      await api.remarks({ scope: "all", take: 1 });
    } catch (e) {
      if (e instanceof ApiError && e.code === "remark_scope_forbidden") {
        setScopeRefused(true);
        setAllAccounts(false);
      }
    }
  }

  const list = useAsync<Paged<Remark>>(
    () => api.remarks({
      status: status || undefined,
      category: category || undefined,
      appArea: appArea || undefined,
      scope: allAccounts ? "all" : undefined,
      skip, take: PAGE_SIZE,
    }),
    [status, category, appArea, allAccounts, skip],
  );

  // Jede Filteränderung springt auf Seite 1 – sonst landet man jenseits des Bestands.
  const filterKey = `${status}|${category}|${appArea}|${allAccounts}`;
  const [prevKey, setPrevKey] = useState(filterKey);
  if (prevKey !== filterKey) { setPrevKey(filterKey); setSkip(0); }

  return (
    <section>
      <div className="row" style={{ alignItems: "center", gap: 8, flexWrap: "wrap" }}>
        <h2 className="h-section" style={{ margin: 0 }}>
          Anmerkungen {list.data ? `(${list.data.total})` : ""}
        </h2>
        <span className="muted" style={{ fontSize: 13 }}>
          Erfasst wird im Widget (Alt+A). Beantwortet wird in Claude Code – hier lässt sich nachhaken.
        </span>
      </div>

      <div className="row" style={{ gap: 10, alignItems: "center", flexWrap: "wrap", marginTop: 10 }}>
        <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
          <span className="muted">Stand</span>
          <select aria-label="Status-Filter" value={status}
            onChange={(e) => setStatus(e.target.value as RemarkStatus | "")}>
            <option value="">– alle –</option>
            {STATUS.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
          </select>
        </label>
        <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
          <span className="muted">Einordnung</span>
          <select aria-label="Kategorie-Filter" value={category}
            onChange={(e) => setCategory(e.target.value as RemarkCategory | "")}>
            <option value="">– alle –</option>
            {CATEGORIES.map((c) => <option key={c} value={c}>{c === "Unspecified" ? "ohne" : c}</option>)}
          </select>
        </label>
        <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
          <span className="muted">Bereich</span>
          <select aria-label="Bereich-Filter" value={appArea} onChange={(e) => setAppArea(e.target.value)}>
            <option value="">– alle –</option>
            <option value="vater">vater</option>
            <option value="sohn">sohn</option>
          </select>
        </label>
        {!scopeRefused && (
          <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}
            title="Zeigt die Anmerkungen aller Testkonten – so liest sie auch der Skill.">
            <input type="checkbox" checked={allAccounts} onChange={(e) => void toggleAllAccounts(e.target.checked)} />
            alle Konten
          </label>
        )}
        {scopeRefused && (
          <span className="muted" style={{ fontSize: 12 }}>
            Kontenübergreifend lesen ist auf dieser Instanz abgeschaltet.
          </span>
        )}
      </div>

      {list.loading ? <div className="loading">Lade…</div>
        : list.error ? <div className="banner err">{list.error}</div>
          : (
            <div style={{ display: "flex", flexDirection: "column", gap: 8, marginTop: 10 }}>
              {list.data?.items.map((r) => (
                <RemarkCard key={r.id} remark={r} showAccount={allAccounts}
                  onChanged={list.reload} onNotice={(text) => setNotice({ ok: true, text })} />
              ))}
              {list.data?.items.length === 0 && (
                <p className="muted">Keine Anmerkungen für diesen Filter.</p>
              )}
            </div>
          )}

      <StatusBanner message={notice} />
      {list.data && <Pager skip={skip} take={PAGE_SIZE} total={list.data.total} onSkip={setSkip} />}
    </section>
  );
}

/** Eine Anmerkung: Kopf mit Stand, Kontext, gepinnte Auflösung, aufklappbarer Verlauf. */
function RemarkCard({ remark, showAccount, onChanged, onNotice }: {
  remark: Remark; showAccount: boolean; onChanged: () => void;
  /** Rückmeldung, die die Karte überlebt – siehe `setStatus`. */
  onNotice: (text: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const action = useAction();

  /*
   * Die Erfolgsmeldung gehört auf die **Seite**, nicht in die Karte: Ein neuer Stand fällt in der Regel aus
   * dem aktiven Filter (Vorgabe „offen"), die Zeile verschwindet – und mit ihr das Banner, das gerade
   * „Stand gesetzt." sagen wollte. Man hätte eine Änderung ohne jede Rückmeldung.
   */
  async function setStatus(status: RemarkStatus) {
    if (!await action.run(() => api.updateRemark(remark.id, { status }))) return;
    onNotice(`Stand gesetzt: #${remark.id} → ${STATUS_LABEL[status]}`);
    onChanged();
  }

  const refs = [
    remark.context.childId && `Kind ${remark.context.childId}`,
    remark.context.exerciseId && `Übung ${remark.context.exerciseId}`,
    remark.context.studyPlanId && `Plan ${remark.context.studyPlanId}`,
    remark.context.planPositionId && `Position ${remark.context.planPositionId}`,
  ].filter(Boolean);

  return (
    <article className="card" style={{ padding: 10 }}>
      <div className="row" style={{ gap: 8, alignItems: "baseline", flexWrap: "wrap" }}>
        <b>#{remark.id}</b>
        <span className="pill" style={{ fontSize: 11 }}>{STATUS_LABEL[remark.status]}</span>
        {remark.category !== "Unspecified" && (
          <span className="muted" style={{ fontSize: 12 }}>{remark.category}</span>
        )}
        <span className="muted" style={{ fontSize: 12 }}>
          {new Date(remark.createdAt).toLocaleString()}
          {showAccount && ` · Konto ${remark.accountId}`}
        </span>
        <span style={{ marginLeft: "auto" }} />
        <select aria-label={`Stand von Anmerkung ${remark.id}`} value={remark.status}
          disabled={action.busy} onChange={(e) => void setStatus(e.target.value as RemarkStatus)}>
          {STATUS.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
        </select>
      </div>

      <p style={{ margin: "6px 0", whiteSpace: "pre-wrap" }}>{remark.text}</p>

      <div className="muted" style={{ fontSize: 12 }}>
        {remark.context.route ? <code>{remark.context.route}</code> : "ohne Route"}
        {remark.context.appArea && ` (${remark.context.appArea})`}
        {refs.length > 0 && ` · ${refs.join(", ")}`}
      </div>

      {remark.answer && (
        <div style={{ marginTop: 8, paddingLeft: 10, borderLeft: "3px solid var(--stroke)" }}>
          <div className="muted" style={{ fontSize: 12 }}>
            Auflösung{remark.answeredBy ? ` · ${remark.answeredBy}` : ""}
            {remark.answeredAt ? ` · ${new Date(remark.answeredAt).toLocaleString()}` : ""}
          </div>
          <div style={{ fontSize: 13, whiteSpace: "pre-wrap" }}>{remark.answer}</div>
        </div>
      )}

      {remark.context.recentErrorsJson && (
        <details style={{ marginTop: 8 }}>
          <summary className="muted" style={{ fontSize: 12, cursor: "pointer" }}>Letzte Fehler</summary>
          {/* Roh: Das Backend interpretiert den Puffer nirgends fachlich – ein Parser hier bräche,
              sobald das Widget ein Feld ergänzt. */}
          <pre style={{ fontSize: 11, overflowX: "auto" }}>{remark.context.recentErrorsJson}</pre>
        </details>
      )}

      <div style={{ marginTop: 8 }}>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          aria-expanded={open} onClick={() => setOpen((v) => !v)}>
          {open ? "Verlauf zu" : remark.commentCount > 0 ? `💬 Verlauf (${remark.commentCount})` : "💬 Nachhaken"}
        </button>
      </div>

      {open && <Thread remarkId={remark.id} status={remark.status} onReopened={onChanged} />}
      <StatusBanner message={action.message} style={{ marginTop: 8 }} />
    </article>
  );
}

/**
 * Der Verlauf einer Anmerkung. Lazy geladen: Bei 25 Karten je Seite wären 25 Abfragen fällig, von denen
 * man selten mehr als eine braucht.
 */
function Thread({ remarkId, status, onReopened }: {
  remarkId: number; status: RemarkStatus; onReopened: () => void;
}) {
  const [comments, setComments] = useState<RemarkComment[] | null>(null);
  const [body, setBody] = useState("");
  const action = useAction();

  const load = useCallback(async () => {
    setComments(await api.remarkComments(remarkId));
  }, [remarkId]);

  useEffect(() => { void load().catch(() => setComments([])); }, [load]);

  async function send() {
    const text = body.trim();
    if (!text) { action.fail("Bitte etwas schreiben."); return; }
    if (!await action.run(() => api.addRemarkComment(remarkId, { body: text }))) return;
    setBody("");
    await load();
    // Serverseitig holt ein menschlicher Beitrag eine erledigte Anmerkung zurück auf „offen" – die Karte
    // zeigt sonst weiter den alten Stand.
    if (status === "Done" || status === "Rejected") onReopened();
  }

  async function remove(commentId: number) {
    if (await action.run(() => api.deleteRemarkComment(remarkId, commentId), "Beitrag entfernt.")) await load();
  }

  return (
    <div style={{ marginTop: 8, display: "flex", flexDirection: "column", gap: 6 }}>
      {comments === null ? <span className="muted" style={{ fontSize: 12 }}>Lade Verlauf…</span> : (
        <>
          {comments.length === 0 && (
            <span className="muted" style={{ fontSize: 12 }}>Noch kein Beitrag – deine Rückfrage wäre der erste.</span>
          )}
          {comments.map((c) => (
            <div key={c.id} className="row" style={{ gap: 8, alignItems: "flex-start" }}>
              <div style={{ flex: 1 }}>
                <div className="muted" style={{ fontSize: 12 }}>
                  <b>{c.authorLabel ?? c.author}</b> · {new Date(c.createdAt).toLocaleString()}
                  {c.author === "Assistant" && " · 🤖"}
                </div>
                <div style={{ fontSize: 13, whiteSpace: "pre-wrap" }}>{c.body}</div>
              </div>
              {c.isOwn && (
                <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                  aria-label={`Beitrag ${c.id} entfernen`} disabled={action.busy}
                  onClick={() => void remove(c.id)}>×</button>
              )}
            </div>
          ))}
        </>
      )}

      <div className="row" style={{ gap: 6, alignItems: "flex-end" }}>
        <div className="field" style={{ flex: 1 }}>
          <label htmlFor={`c-${remarkId}`}>Beitrag</label>
          <textarea id={`c-${remarkId}`} rows={2} value={body} disabled={action.busy}
            onChange={(e) => setBody(e.target.value)}
            placeholder={status === "Done" || status === "Rejected"
              ? "Nachhaken – holt die Anmerkung zurück auf „offen“"
              : "Rückfrage oder Ergänzung"} />
        </div>
        <button type="button" className="btn" style={{ width: "auto" }}
          disabled={action.busy || !body.trim()} onClick={() => void send()}>
          {action.busy ? "Sende…" : "Senden"}
        </button>
      </div>
      <StatusBanner message={action.message} />
    </div>
  );
}
