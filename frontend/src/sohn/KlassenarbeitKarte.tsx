import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { arcadeDatum } from "../lib/datum";
import { useAsync } from "../lib/useAsync";
import type { ClassTestReadiness, UpcomingClassTest } from "../lib/types";

/**
 * Die nächste Klassenarbeit auf der Startseite (B-750) samt der Sicherheit im Vokabelstoff (B-751). Der Server liefert
 * nur geplante Arbeiten ab heute, früheste zuerst – gezeigt wird die erste. Ohne Arbeit steht nichts da (kein leerer
 * Kasten). Scheitert das Lesen, bleibt die Karte ebenfalls weg: sie ist ein Hinweis neben der Pflicht, kein Weg. Ist der
 * ganze Server weg, meldet das die Tagessicht der Startseite; scheitert nur dieser Pfad, sieht das Kind keine Arbeit –
 * bewusst hingenommen, ein Fehlerkasten neben der Pflicht wöge für einen Hinweis zu schwer.
 */
export function KlassenarbeitKarte({ childId }: { childId: number }) {
  const liste = useAsync<UpcomingClassTest[]>(() => api.upcomingClassTests(childId), [childId]);
  const arbeit = naechsteArbeit(liste.data ?? []);
  // Die Arbeit bringt ihr Kind nicht mit: nach einem Kindwechsel gehört die alte Liste nicht mehr zur Anzeige (B-170).
  const passend = arbeit !== null && !liste.stale ? arbeit : null;
  const bereit = useAsync<ClassTestReadiness | null>(
    () => (passend ? api.classTestReadiness(childId, passend.id) : Promise.resolve(null)), [childId, passend?.id]);
  if (!passend) return null;
  const stand = bereit.data && bereit.data.classTestId === passend.id ? bereit.data : null;
  return <KlassenarbeitAnzeige arbeit={passend} bereitschaft={stand} />;
}

/** Die Arbeit, auf die das Kind zählt: die früheste – der Server sortiert, hier wird nicht neu gewählt. */
export const naechsteArbeit = (liste: readonly UpcomingClassTest[]): UpcomingClassTest | null => liste[0] ?? null;

/** Die Karte selbst, ohne Laden – damit sie ohne gefälschten Server prüfbar ist. */
export function KlassenarbeitAnzeige({ arbeit, bereitschaft }: {
  arbeit: UpcomingClassTest; bereitschaft: ClassTestReadiness | null;
}) {
  const { t, i18n } = useTranslation();
  const wann = arbeit.daysUntil === 0 ? t("classTest.today")
    : arbeit.daysUntil === 1 ? t("classTest.tomorrow") : t("classTest.inDays", { count: arbeit.daysUntil });
  return (
    <section className="card klassenarbeit-karte" aria-label={t("classTest.label")}>
      <div className="row">
        <span aria-hidden="true">📅</span>
        <b>{arbeit.title}</b>
        {arbeit.subjectName && <span className="pill">{arbeit.subjectName}</span>}
        <span className="sub" style={{ marginLeft: "auto" }}>{arcadeDatum(arbeit.scheduledDate, i18n.language)} · {wann}</span>
      </div>
      <Bereitschaft stand={bereitschaft} />
    </section>
  );
}

/**
 * „x von y Wörtern sicher“ mit Balken; ein Bündel ohne Vokabeln hat kein Maß und sagt das, statt eine Zahl zu
 * schätzen. Ohne Übungen in der Arbeit (oder solange nichts geladen ist) steht hier nichts.
 */
function Bereitschaft({ stand }: { stand: ClassTestReadiness | null }) {
  const { t } = useTranslation();
  if (!stand || stand.exerciseCount === 0) return null;
  if (stand.totalItems == null || stand.secureItems == null) {
    return <p className="sub" style={{ marginTop: 6 }}>{t("classTest.noMeasure")}</p>;
  }
  const pct = Math.round((stand.secureItems / stand.totalItems) * 100);
  return (
    <div style={{ marginTop: 8 }}>
      <div className="sub">{t("classTest.secureWords", { secure: stand.secureItems, count: stand.totalItems })}</div>
      <div className="bar lime" aria-hidden="true"><i style={{ width: `${pct}%` }} /></div>
    </div>
  );
}
