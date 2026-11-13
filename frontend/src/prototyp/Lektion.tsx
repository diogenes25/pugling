// PROTOTYP – Lektionsbildschirm nach festem Raster: Anweisung, Sprechblase am Maskottchen, Antwortfläche,
// ein Knopf unten, Rückmeldung als Blatt an derselben Stelle. Bewertet wird lokal; in der echten App
// entscheidet das der Server.
import { useMemo, useRef, useState } from "react";
import { Mascot } from "../components/Mascot";
import { DEFAULT_SKIN } from "../lib/skins";
import { LEKTION, type Aufgabe } from "./daten";

export interface LektionErgebnis { muenzen: number; richtig: number; gesamt: number; sekunden: number }

const norm = (s: string) => s.trim().toLowerCase().replace(/\s+/g, " ");

export function Lektion({ titel, onEnde, onAbbruch }: {
  titel: string; onEnde: (e: LektionErgebnis) => void; onAbbruch: () => void;
}) {
  const start = useRef(Date.now());
  const [index, setIndex] = useState(0);
  const [antwort, setAntwort] = useState("");
  const [geprueft, setGeprueft] = useState<null | boolean>(null);
  const [erklaerung, setErklaerung] = useState(false);
  const [richtig, setRichtig] = useState(0);
  const [fertig, setFertig] = useState<LektionErgebnis | null>(null);

  const aufgabe = LEKTION[index];

  if (fertig) return <Abschluss ergebnis={fertig} onWeiter={() => onEnde(fertig)} />;

  const pruefen = () => {
    const ok = norm(antwort) === norm(aufgabe.loesung);
    setGeprueft(ok);
    if (ok) setRichtig((n) => n + 1);
  };
  const weiter = () => {
    if (index + 1 >= LEKTION.length) {
      setFertig({
        muenzen: richtig * 2, richtig, gesamt: LEKTION.length,
        sekunden: Math.round((Date.now() - start.current) / 1000),
      });
      return;
    }
    setIndex(index + 1); setAntwort(""); setGeprueft(null); setErklaerung(false);
  };

  const anteil = ((index + (geprueft === null ? 0 : 1)) / LEKTION.length) * 100;

  return (
    <div className="pp-lektion">
      <div className="pp-lektion-kopf">
        <button type="button" className="pp-x" onClick={onAbbruch} aria-label="Lektion verlassen">✕</button>
        <div className="bar gold" style={{ flex: 1 }}><i style={{ width: `${anteil}%` }} /></div>
        <span className={`pill ${aufgabe.neu ? "cyan" : "mag"}`}>{aufgabe.neu ? "Neu" : "Wiederholung"}</span>
      </div>

      <h1 className="pp-anweisung">{aufgabe.anweisung}</h1>

      <div className="pp-frage">
        <Mascot skin={DEFAULT_SKIN} mood={geprueft ? "hyped" : "happy"} size={84} />
        <div className="pp-blase">
          {aufgabe.art === "luecke" ? (
            <>
              {aufgabe.vor}{" "}
              <span className={`pp-luecke ${antwort ? "voll" : ""} ${geprueft === true ? "ok" : geprueft === false ? "falsch" : ""}`}>
                {antwort || " "}
              </span>{" "}
              {aufgabe.nach}
            </>
          ) : aufgabe.frage}
        </div>
      </div>

      <div className="pp-antwort" key={index}>
        <Antwortflaeche aufgabe={aufgabe} antwort={antwort} setAntwort={setAntwort} gesperrt={geprueft !== null} geprueft={geprueft} />
      </div>

      {geprueft === null ? (
        <div className="pp-fuss">
          <button type="button" className="btn" disabled={!antwort} onClick={pruefen}>Prüfen</button>
        </div>
      ) : (
        <div className={`pp-blatt ${geprueft ? "ok" : "falsch"}`} role="status">
          <div className="row">
            <b className="pp-blatt-titel">{geprueft ? "✓ Stark!" : "✗ Fast."}</b>
            {geprueft && <span className="pill gold">+2 🪙</span>}
            <button type="button" className="pp-birne" onClick={() => setErklaerung((e) => !e)} aria-label="Erklärung">💡</button>
          </div>
          {!geprueft && <div className="pp-blatt-loesung">Lösung: <b>{aufgabe.loesung}</b></div>}
          {erklaerung && <div className="sub" style={{ color: "var(--ink)" }}>{aufgabe.erklaerung}</div>}
          <button type="button" className={`btn ${geprueft ? "lime" : "red"}`} onClick={weiter}>Weiter</button>
        </div>
      )}
      <div className="sr-only">{titel}</div>
    </div>
  );
}

function Antwortflaeche({ aufgabe, antwort, setAntwort, gesperrt, geprueft }: {
  aufgabe: Aufgabe; antwort: string; setAntwort: (s: string) => void; gesperrt: boolean; geprueft: null | boolean;
}) {
  if (aufgabe.art === "auswahl" || aufgabe.art === "luecke") {
    const chips = aufgabe.art === "luecke" || aufgabe.optionen.length === 2;
    return (
      <div className={chips ? "pp-chips" : "pp-optionen"}>
        {aufgabe.optionen.map((o) => {
          const gewaehlt = o === antwort;
          const farbe = !gesperrt ? (gewaehlt ? "gewaehlt" : "")
            : o === aufgabe.loesung ? "ok" : gewaehlt ? "falsch" : "blass";
          return (
            <button key={o} type="button" className={`pp-option ${farbe}`} disabled={gesperrt}
              aria-pressed={gewaehlt} onClick={() => setAntwort(gewaehlt ? "" : o)}>
              {o}
            </button>
          );
        })}
      </div>
    );
  }
  if (aufgabe.art === "ordnen") return <Ordnen loesung={aufgabe.loesung} setAntwort={setAntwort} gesperrt={gesperrt} geprueft={geprueft} />;
  return (
    <input className={`pp-tippen ${geprueft === true ? "ok" : geprueft === false ? "falsch" : ""}`} value={antwort}
      onChange={(e) => setAntwort(e.target.value)} disabled={gesperrt} autoFocus autoCapitalize="none"
      autoComplete="off" spellCheck={false} placeholder="Deine Antwort" aria-label="Deine Antwort" />
  );
}

function Ordnen({ loesung, setAntwort, gesperrt, geprueft }: {
  loesung: string; setAntwort: (s: string) => void; gesperrt: boolean; geprueft: null | boolean;
}) {
  // Feste, sichtbar gemischte Reihenfolge – kein Zufall, damit der Prototyp bei jedem Durchlauf gleich aussieht.
  const kacheln = useMemo(() => loesung.split("").map((b, i) => ({ b, i })).reverse(), [loesung]);
  const [gelegt, setGelegt] = useState<number[]>([]);
  const setze = (next: number[]) => {
    setGelegt(next);
    setAntwort(next.length === loesung.length ? next.map((i) => loesung[i]).join("") : "");
  };
  return (
    <>
      <div className={`pp-slots ${geprueft === true ? "ok" : geprueft === false ? "falsch" : ""}`}>
        {loesung.split("").map((_, pos) => (
          <button key={pos} type="button" className="pp-slot" disabled={gesperrt || gelegt[pos] === undefined}
            onClick={() => setze(gelegt.filter((_, p) => p !== pos))}>
            {gelegt[pos] !== undefined ? loesung[gelegt[pos]] : ""}
          </button>
        ))}
      </div>
      <div className="pp-chips">
        {kacheln.map(({ b, i }) => gelegt.includes(i)
          ? <span key={i} className="pp-option leer" aria-hidden="true">{b}</span>
          : <button key={i} type="button" className="pp-option" disabled={gesperrt} onClick={() => setze([...gelegt, i])}>{b}</button>)}
      </div>
    </>
  );
}

function Abschluss({ ergebnis, onWeiter }: { ergebnis: LektionErgebnis; onWeiter: () => void }) {
  const min = Math.floor(ergebnis.sekunden / 60);
  const sek = String(ergebnis.sekunden % 60).padStart(2, "0");
  return (
    <div className="pp-lektion pp-abschluss">
      <div style={{ flex: 1 }} />
      <Mascot skin={DEFAULT_SKIN} mood="hyped" size={150} />
      <h1 className="pp-anweisung" style={{ textAlign: "center" }}>Stark gemacht!</h1>
      <div className="pp-kacheln">
        <div className="pp-kachel"><span>Münzen</span><b>🪙 {ergebnis.muenzen}</b></div>
        <div className="pp-kachel"><span>Genauigkeit</span><b>🎯 {Math.round((ergebnis.richtig / ergebnis.gesamt) * 100)} %</b></div>
        <div className="pp-kachel"><span>Zeit</span><b>⏱ {min}:{sek}</b></div>
      </div>
      <div style={{ flex: 1 }} />
      <div className="pp-fuss"><button type="button" className="btn gold" onClick={onWeiter}>Weiter</button></div>
    </div>
  );
}
