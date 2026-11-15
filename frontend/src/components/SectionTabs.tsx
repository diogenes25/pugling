import type { ReactNode } from "react";
import { Link, useSearchParams } from "react-router-dom";

/** Ein Reiter einer Seite: Schlüssel (steht in der URL) und Beschriftung. */
export interface SectionTab<K extends string = string> {
  key: K;
  label: string;
}

/** Der Query-Parameter, der den aktiven Reiter trägt. Einer für alle Seiten, damit Links ihn ohne Nachschlagen setzen. */
export const SECTION_PARAM = "bereich";

/**
 * Der aktive Reiter aus dem Query-Wert. Unbekanntes und Fehlendes fällt auf den ersten Reiter zurück – ein alter
 * Link oder ein Tippfehler in der URL soll eine Seite öffnen, keine leere Fläche.
 */
export function activeSection<K extends string>(tabs: readonly SectionTab<K>[], raw: string | null): K {
  return tabs.find((t) => t.key === raw)?.key ?? tabs[0].key;
}

/** Liest den aktiven Reiter aus der URL (`?bereich=`). */
export function useActiveSection<K extends string>(tabs: readonly SectionTab<K>[]): K {
  const [params] = useSearchParams();
  return activeSection(tabs, params.get(SECTION_PARAM));
}

/**
 * Die Reiter-Leiste einer Seite, die mehrere unabhängige Themen trägt (B-534). Sie zeigt **ein** Thema auf einmal,
 * damit jeder Speichern-Knopf eindeutig zu dem gehört, was gerade sichtbar ist – stehen drei Formulare
 * untereinander, rät der Vater, welcher Knopf welche Eingabe sichert.
 *
 * Bewusst **Links**, keine Knöpfe: der Reiter steht in der URL, also öffnet ein Lesezeichen oder ein Neuladen
 * dasselbe Thema wieder, und andere Seiten können direkt auf einen Reiter verlinken („Interessen pflegen →").
 * Der Wechsel **ersetzt** den Verlaufseintrag (`replace`): ein Reiter ist eine Ansicht derselben Seite, kein
 * neuer Ort – der Zurück-Knopf führt darum zur vorigen Seite, nicht durch die zuvor angeklickten Reiter.
 */
export function SectionTabs<K extends string>({ tabs, active, label }: {
  tabs: readonly SectionTab<K>[];
  active: K;
  /** Name der Leiste für Screenreader, z. B. „Bereiche von Theo". */
  label: string;
}) {
  return (
    <nav className="section-tabs" aria-label={label}>
      {tabs.map((t) => (
        <Link key={t.key} to={{ search: `?${SECTION_PARAM}=${t.key}` }} replace
          className={`section-tab${t.key === active ? " active" : ""}`}
          aria-current={t.key === active ? "page" : undefined}>
          {t.label}
        </Link>
      ))}
    </nav>
  );
}

/**
 * Der Inhalt eines Reiters. Er bleibt **eingehängt** und wird nur verborgen (`hidden`), solange ein anderer Reiter
 * aktiv ist: ein Reiterwechsel ist kein Verwerfen, und ausgehängt verlöre ein Formular seine ungespeicherte
 * Eingabe. `hidden` nimmt den Inhalt zugleich aus dem Fokus-Weg und aus dem Barrierefreiheitsbaum.
 */
export function SectionPanel<K extends string>({ section, active, children }: {
  section: K;
  active: K;
  children: ReactNode;
}) {
  return <div hidden={section !== active}>{children}</div>;
}
