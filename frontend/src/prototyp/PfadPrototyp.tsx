// PROTOTYP – wegwerfen, nicht ausbauen. Drei Varianten der Sohn-Startseite als Lernpfad, umschaltbar über
// `?variant=A|B|C` auf /prototyp/pfad, plus ein gemeinsamer Lektionsbildschirm. Frage: Fühlt sich die von
// Duolingo/Snipwise bekannte Form für die Kinder richtig an – und welche Pfad-Form? Alle Daten sind erfunden
// und leben nur im Speicher. Hintergrund: docs/duolingo-snipwise-vergleich-plan.md (Stufe A).
import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ArcadeScope } from "../lib/chromeText";
import { Mascot } from "../components/Mascot";
import { DEFAULT_SKIN } from "../lib/skins";
import { PFAD, STUFEN_NAMEN, type Abschnitt, type Knoten, type KnotenZustand } from "./daten";
import { Lektion, type LektionErgebnis } from "./Lektion";
import "./prototyp.css";

const VARIANTEN = [
  { key: "A", name: "Schlangenpfad" },
  { key: "B", name: "Heute zuerst" },
  { key: "C", name: "Level-Raster" },
] as const;
type VariantKey = (typeof VARIANTEN)[number]["key"];

interface KnotenSicht extends Knoten { zustand: KnotenZustand; heuteErledigt: boolean; stand: number }
interface AbschnittSicht extends Omit<Abschnitt, "knoten"> { knoten: KnotenSicht[] }
interface Sicht {
  abschnitte: AbschnittSicht[];
  naechster: KnotenSicht | null;
  pflichtGesamt: number;
  pflichtErledigt: number;
  restStunden: number;
  onTipp: (k: KnotenSicht) => void;
  onStart: (k: KnotenSicht) => void;
}

function restStundenBisMitternacht() {
  const jetzt = new Date();
  const ende = new Date(jetzt); ende.setHours(24, 0, 0, 0);
  return Math.max(1, Math.round((ende.getTime() - jetzt.getTime()) / 3_600_000));
}

export function PfadPrototyp() {
  const [params, setParams] = useSearchParams();
  const variant = (VARIANTEN.find((v) => v.key === params.get("variant"))?.key ?? "A") as VariantKey;
  const [stand, setStand] = useState<Record<string, number>>(() =>
    Object.fromEntries(PFAD.flatMap((a) => a.knoten).map((k) => [k.id, k.stufenFertig])));
  const [heute, setHeute] = useState<string[]>([]);
  const [muenzen, setMuenzen] = useState(140);
  const [offen, setOffen] = useState<KnotenSicht | null>(null);
  const [laeuft, setLaeuft] = useState<KnotenSicht | null>(null);

  const wechsle = (richtung: number) => {
    const i = VARIANTEN.findIndex((v) => v.key === variant);
    const next = VARIANTEN[(i + richtung + VARIANTEN.length) % VARIANTEN.length].key;
    setParams({ variant: next }, { replace: true });
  };
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const t = e.target as HTMLElement | null;
      if (t && (t.tagName === "INPUT" || t.tagName === "TEXTAREA" || t.isContentEditable)) return;
      if (e.key === "ArrowLeft") wechsle(-1);
      if (e.key === "ArrowRight") wechsle(1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  });

  // Zustand ableiten: fertig = alle Stufen; dran = alles davor fertig, oder die heutige Wiederholung.
  let davorFertig = true;
  const abschnitte: AbschnittSicht[] = PFAD.map((a) => ({
    ...a,
    knoten: a.knoten.map((k) => {
      const s = stand[k.id];
      const fertig = s >= k.stufen;
      const zustand: KnotenZustand = fertig ? "fertig" : davorFertig || k.art === "wiederholung" ? "dran" : "gesperrt";
      if (!fertig && k.art !== "wiederholung") davorFertig = false;
      return { ...k, zustand, stand: s, heuteErledigt: heute.includes(k.id) };
    }),
  }));
  const alle = abschnitte.flatMap((a) => a.knoten);
  const pflicht = alle.filter((k) => k.pflichtMalus);
  const naechster = alle.find((k) => k.zustand === "dran" && k.pflichtMalus && !k.heuteErledigt)
    ?? alle.find((k) => k.zustand === "dran" && !k.heuteErledigt) ?? null;

  const starte = (k: KnotenSicht) => { setOffen(null); setLaeuft(k); };
  const beende = (e: LektionErgebnis) => {
    if (laeuft && laeuft.zustand === "dran") {
      setStand((s) => ({ ...s, [laeuft.id]: s[laeuft.id] + 1 }));
      setHeute((h) => [...h, laeuft.id]);
      setMuenzen((m) => m + e.muenzen);
    }
    setLaeuft(null);
  };

  const sicht: Sicht = {
    abschnitte, naechster, pflichtGesamt: pflicht.length,
    pflichtErledigt: pflicht.filter((k) => k.heuteErledigt).length,
    restStunden: restStundenBisMitternacht(), onTipp: setOffen, onStart: starte,
  };

  return (
    <ArcadeScope>
      <div className="app-sohn pp-root">
        {laeuft ? (
          <Lektion titel={laeuft.titel} onEnde={beende} onAbbruch={() => setLaeuft(null)} />
        ) : (
          <>
            <div className="sohn-hud">
              <div className="avatar-mini" style={{ background: DEFAULT_SKIN.gradient }} aria-hidden="true">{DEFAULT_SKIN.emoji}</div>
              <div className="chip">🪙<b className="tabnum">{muenzen}</b></div>
              <div className="chip">💎<b className="tabnum">12</b></div>
              <div className="chip flame">🔥<b className="tabnum">6</b></div>
            </div>
            <div className="sohn-body" key={variant}>
              {variant === "A" && <VariantA {...sicht} />}
              {variant === "B" && <VariantB {...sicht} />}
              {variant === "C" && <VariantC {...sicht} />}
            </div>
            <nav className="sohn-nav" aria-label="Navigation (Attrappe)">
              <a className="active"><span className="ic" aria-hidden="true">🗺️</span>Pfad</a>
              <a><span className="ic" aria-hidden="true">🎯</span>Missionen</a>
              <a><span className="ic" aria-hidden="true">🏋️</span>Training</a>
              <a><span className="ic" aria-hidden="true">🛒</span>Shop</a>
              <a><span className="ic" aria-hidden="true">🎭</span>Skins</a>
            </nav>
            {offen && <KnotenBlatt k={offen} restStunden={sicht.restStunden} onStart={starte} onZu={() => setOffen(null)} />}
          </>
        )}
        {!laeuft && (
          <div className="pp-switcher">
            <button type="button" onClick={() => wechsle(-1)} aria-label="Vorige Variante">‹</button>
            <span>{variant} — {VARIANTEN.find((v) => v.key === variant)?.name}</span>
            <button type="button" onClick={() => wechsle(1)} aria-label="Nächste Variante">›</button>
          </div>
        )}
      </div>
    </ArcadeScope>
  );
}

// ---------- geteilte Kleinteile ----------

function PflichtZeile({ k, restStunden }: { k: KnotenSicht; restStunden: number }) {
  if (!k.pflichtMalus) return null;
  return k.heuteErledigt
    ? <span className="pill lime">Heute ✓</span>
    : <span className="pill red">Pflicht · noch {restStunden} Std. · sonst −{k.pflichtMalus} 🪙</span>;
}

function untertitel(k: KnotenSicht) {
  if (k.art === "wiederholung") return `${k.faellig} Karten fällig`;
  if (k.art === "truhe") return "Öffnet sich, wenn du hier ankommst";
  if (k.art === "test") return k.hinweis ?? "Abschlusstest";
  if (k.zustand === "fertig") return "Alle Stufen geschafft";
  return `Stufe ${k.stand + 1} von ${k.stufen} · ${STUFEN_NAMEN[Math.min(k.stand, STUFEN_NAMEN.length - 1)]}`;
}

function Ring({ k, groesse }: { k: KnotenSicht; groesse: number }) {
  const r = groesse / 2 - 4;
  const umfang = 2 * Math.PI * r;
  const seg = umfang / k.stufen;
  const luecke = k.stufen > 1 ? 7 : 0;
  return (
    <svg className="pp-ring" width={groesse} height={groesse} aria-hidden="true">
      <g transform={`rotate(-90 ${groesse / 2} ${groesse / 2})`}>
        {Array.from({ length: k.stufen }, (_, i) => (
          <circle key={i} cx={groesse / 2} cy={groesse / 2} r={r} fill="none" strokeWidth={5} strokeLinecap="round"
            stroke={i < k.stand ? "var(--gold)" : "rgba(255,255,255,.14)"}
            strokeDasharray={`${seg - luecke} ${umfang - seg + luecke}`} strokeDashoffset={-i * seg - luecke / 2} />
        ))}
      </g>
    </svg>
  );
}

function KnotenKnopf({ k, groesse = 78, onTipp }: { k: KnotenSicht; groesse?: number; onTipp: (k: KnotenSicht) => void }) {
  return (
    <button type="button" className={`pp-knoten ${k.zustand} ${k.art}`} style={{ width: groesse + 18, height: groesse + 18 }}
      onClick={() => onTipp(k)} aria-label={`${k.titel} – ${untertitel(k)}`}>
      <Ring k={k} groesse={groesse + 18} />
      <span className="pp-knoten-kern" style={{ width: groesse - 6, height: groesse - 6, fontSize: groesse * 0.42 }}>
        {k.zustand === "gesperrt" && k.art === "lektion" ? "🔒" : k.icon}
      </span>
      {k.heuteErledigt && <span className="pp-haken">✓</span>}
    </button>
  );
}

function KnotenBlatt({ k, restStunden, onStart, onZu }: {
  k: KnotenSicht; restStunden: number; onStart: (k: KnotenSicht) => void; onZu: () => void;
}) {
  return (
    <div className="pp-schleier" onClick={onZu}>
      <div className="pp-blatt pp-knotenblatt" role="dialog" aria-label={k.titel} onClick={(e) => e.stopPropagation()}>
        <div className="row">
          <span style={{ fontSize: 30 }}>{k.icon}</span>
          <div style={{ flex: 1 }}>
            <b className="pp-blatt-titel">{k.titel}</b>
            <div className="sub">{untertitel(k)}</div>
          </div>
        </div>
        <PflichtZeile k={k} restStunden={restStunden} />
        {k.zustand === "dran" && k.art !== "truhe" && (
          <button type="button" className="btn gold" onClick={() => onStart(k)}>
            {k.heuteErledigt ? "Noch eine Lektion" : "Los"} · +10 🪙
          </button>
        )}
        {k.zustand === "fertig" && k.art !== "truhe" && (
          <button type="button" className="btn ghost" onClick={() => onStart(k)}>Nochmal üben · nur 💎</button>
        )}
        {k.zustand === "gesperrt" && <div className="sub">Noch gesperrt – erst die Knoten davor.</div>}
      </div>
    </div>
  );
}

// ---------- Variante A: Schlangenpfad (Duolingo/Snipwise) ----------

const SCHLANGE = [0, -58, -88, -58, 0, 58, 88, 58];

function VariantA({ abschnitte, naechster, restStunden, onTipp }: Sicht) {
  const ziel = useRef<HTMLDivElement | null>(null);
  useEffect(() => { ziel.current?.scrollIntoView({ block: "center" }); }, []);
  let lauf = 0;
  return (
    <>
      {abschnitte.map((a) => (
        <section key={a.id} className="pp-abschnitt">
          <div className="pp-banner">
            <span>{a.kopf}</span>
            <b>{a.titel}</b>
          </div>
          <div className="pp-schlange">
            {a.knoten.map((k) => {
              const dx = SCHLANGE[lauf++ % SCHLANGE.length];
              const istNaechster = naechster?.id === k.id;
              return (
                <div key={k.id} className="pp-schlange-zeile" style={{ transform: `translateX(${dx}px)` }}
                  ref={istNaechster ? ziel : undefined}>
                  {istNaechster && <span className="pp-start">START</span>}
                  <KnotenKnopf k={k} onTipp={onTipp} />
                  {istNaechster && (
                    <div className="pp-begleiter" style={{ [dx > 0 ? "right" : "left"]: "calc(100% + 6px)" }}>
                      <Mascot skin={DEFAULT_SKIN} mood="happy" size={70} />
                    </div>
                  )}
                  {k.pflichtMalus && !k.heuteErledigt && (
                    <span className="pp-uhr">⏰ {restStunden} Std.</span>
                  )}
                </div>
              );
            })}
          </div>
        </section>
      ))}
    </>
  );
}

// ---------- Variante B: Heute zuerst (ein großer Knopf, Pfad als Zeitleiste) ----------

function VariantB({ abschnitte, naechster, pflichtErledigt, pflichtGesamt, restStunden, onTipp, onStart }: Sicht) {
  const geschafft = pflichtErledigt >= pflichtGesamt;
  const aktuell = abschnitte.find((a) => a.knoten.some((k) => k.zustand !== "fertig")) ?? abschnitte[abschnitte.length - 1];
  return (
    <>
      <div className="card pp-held">
        <div className="row">
          <Mascot skin={DEFAULT_SKIN} mood={geschafft ? "hyped" : "happy"} size={76} />
          <div className="pp-blase klein">
            {geschafft ? "Pflicht geschafft! Alles ab jetzt ist Kür." : `Heute noch ${pflichtGesamt - pflichtErledigt} Lektionen – dann hast du frei.`}
          </div>
        </div>
        <div className="pp-punkte" aria-label={`${pflichtErledigt} von ${pflichtGesamt} Pflicht-Lektionen`}>
          {Array.from({ length: pflichtGesamt }, (_, i) => <i key={i} className={i < pflichtErledigt ? "voll" : ""} />)}
          {!geschafft && <span className="pill red">⏰ noch {restStunden} Std.</span>}
        </div>
        {naechster && (
          <button type="button" className="btn gold" onClick={() => onStart(naechster)}>
            {naechster.icon} {naechster.titel}
            <span className="pp-btn-sub">{untertitel(naechster)}</span>
          </button>
        )}
      </div>

      <div className="pp-banner flach"><span>{aktuell.kopf}</span><b>{aktuell.titel}</b></div>
      <ol className="pp-leiste">
        {aktuell.knoten.map((k) => (
          <li key={k.id} className={k.zustand}>
            <button type="button" onClick={() => onTipp(k)}>
              <span className="pp-leiste-punkt">{k.zustand === "fertig" ? "✓" : k.icon}</span>
              <span className="pp-leiste-text">
                <b>{k.titel}</b>
                <span className="sub">{untertitel(k)}</span>
              </span>
              {k.art === "lektion" && k.zustand !== "gesperrt" && (
                <span className="pp-sterne" aria-hidden="true">
                  {Array.from({ length: k.stufen }, (_, i) => <i key={i} className={i < k.stand ? "voll" : ""} />)}
                </span>
              )}
            </button>
          </li>
        ))}
      </ol>
    </>
  );
}

// ---------- Variante C: Level-Raster (Arcade-Levelauswahl) ----------

function VariantC({ abschnitte, naechster, pflichtErledigt, pflichtGesamt, restStunden, onTipp, onStart }: Sicht) {
  return (
    <>
      <div className="card pp-tagesleiste">
        <b>Heute {pflichtErledigt} / {pflichtGesamt}</b>
        <div className="bar gold" style={{ flex: 1 }}><i style={{ width: `${(pflichtErledigt / pflichtGesamt) * 100}%` }} /></div>
        {pflichtErledigt < pflichtGesamt && <span className="pill red">⏰ {restStunden} Std.</span>}
      </div>
      {abschnitte.map((a) => {
        const fertig = a.knoten.filter((k) => k.zustand === "fertig").length;
        return (
          <section key={a.id} className="card pp-welt">
            <div className="row">
              <div style={{ flex: 1 }}>
                <div className="sub">{a.kopf}</div>
                <b className="pp-blatt-titel">{a.titel}</b>
              </div>
              <span className="pill cyan">{fertig} / {a.knoten.length}</span>
            </div>
            <div className="pp-raster">
              {a.knoten.map((k) => (
                <button key={k.id} type="button" onClick={() => onTipp(k)}
                  className={`pp-level ${k.zustand} ${naechster?.id === k.id ? "naechster" : ""}`}>
                  <span className="pp-level-icon">{k.zustand === "gesperrt" ? "🔒" : k.icon}</span>
                  <span className="pp-level-titel">{k.titel}</span>
                  <span className="pp-sterne" aria-hidden="true">
                    {Array.from({ length: k.stufen }, (_, i) => <i key={i} className={i < k.stand ? "voll" : ""} />)}
                  </span>
                  {k.pflichtMalus && !k.heuteErledigt && <span className="pp-level-marke">!</span>}
                  {k.heuteErledigt && <span className="pp-level-marke ok">✓</span>}
                </button>
              ))}
            </div>
          </section>
        );
      })}
      {naechster && (
        <div className="pp-cta">
          <button type="button" className="btn gold" onClick={() => onStart(naechster)}>
            Weiter: {naechster.titel}
          </button>
        </div>
      )}
    </>
  );
}
