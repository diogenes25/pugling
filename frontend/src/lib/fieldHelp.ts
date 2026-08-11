/*
 * Die Erklärungen hinter den „ⓘ" der Vater-Formulare – an EINER Stelle.
 *
 * Warum zentral: Dieselbe Größe wird an mehreren Stellen eingestellt (der Lehrplan-Assistent stellt
 * dieselbe Position ein wie die Plan-Seite, Artikel und Angebot teilen sich den Shop-Wortschatz). Stünde
 * der Text am Feld, gäbe es je Begriff zwei Formulierungen – und irgendwann zwei Bedeutungen.
 *
 * Regeln für die Texte: sagen, **was passiert**, nicht was das Feld heißt; die Vorgabe nennen, wenn es
 * eine gibt; und beim Punktesystem die Währung dazusagen (🪙 Münzen ≠ 💎 Gems – sie werden für
 * Verschiedenes ausgegeben).
 */

/** Ein Hilfetext: Überschrift (der Feldname im Klartext) + die Erklärung. */
export interface FieldHelp {
  title: string;
  text: string;
}

export const FIELD_HELP = {
  // ---- Position im Lehrplan (Plan-Seite und Assistent stellen dieselbe Sache ein) ----
  cadence: {
    title: "Ziel-Rhythmus",
    text: "Wie oft diese Übung Pflicht ist. „Tagesziel“ muss jeden Tag erledigt werden, „Wochenziel“ "
      + "einmal pro Woche (Mo–So). „frei“ heißt: darf geübt werden, muss aber nicht – dann gibt es "
      + "weder Ziel-Punkte noch Münz-Malus.",
  },
  goalThreshold: {
    title: "Bestehen ab %",
    text: "Ab wie viel Prozent das Ziel der Periode als erledigt gilt. Leer lassen heißt 80 %. Was gemessen "
      + "wird, hängt an der Übung: bei prüfbaren Übungen (mit Abschlusstest) sind es Prozent richtiger "
      + "Antworten. Bei Übungen ohne automatische Prüfung gibt es nichts zu bewerten; dort sind es Prozent "
      + "der Runde, die durchgespielt wurde – bloßes Öffnen erfüllt die Pflicht also nicht. Hat eine Übung "
      + "gar keine einzelnen Inhalte, etwa ein Aufsatz, greift der Wert nicht: dort zählt, dass eine Weile "
      + "gearbeitet und die Runde bewusst beendet wurde.",
  },
  itemCount: {
    title: "Inhalte dauerhaft begrenzen",
    text: "Begrenzt die Übung dauerhaft auf ihre ersten N Inhalte – die übrigen werden nie abgefragt, "
      + "auch nicht an einem anderen Tag. Leer = alle Inhalte der Übung, und das ist meist richtig. "
      + "Wie viel an einem einzelnen Tag drankommt, entscheidet der Leitner-Kasten über die Fälligkeit "
      + "und nicht dieses Feld.",
  },
  orderStrategy: {
    title: "Reihenfolge",
    text: "In welcher Reihenfolge der Server die Inhalte ausspielt: „Schwächste zuerst“ nimmt das vor, "
      + "was am schlechtesten sitzt (Vorgabe), „Reihenfolge“ geht der Liste nach, „Zufällig“ mischt, "
      + "„Neue bevorzugt“ zieht noch nicht Gelerntes nach vorn. Beim Start einer Sitzung wird die "
      + "Reihenfolge eingefroren – Neuladen mischt also nicht neu.",
  },
  pointsGoalMet: {
    title: "Punkte, wenn das Ziel erreicht ist",
    text: "🪙 Münzen, die einmal je Periode gutgeschrieben werden, sobald die Pflicht erfüllt ist. Münzen "
      + "gibt das Kind im Familien-Shop für echte Belohnungen aus. Bei „frei“ gibt es sie nicht.",
  },
  penaltyCoins: {
    title: "Münz-Malus bei versäumter Pflicht",
    text: "🪙 Münzen, die abgezogen werden, wenn die Periode vorbei ist und das Ziel nicht erreicht wurde. "
      + "0 = keine Strafe, nur Belohnung. Der Abzug darf ins Minus gehen – Schulden sind gewollt und "
      + "werden abgerechnet, sobald sich das Kind das nächste Mal anmeldet oder im Shop kauft "
      + "(rückwirkend höchstens 14 Tage).",
  },
  newContentPoints: {
    title: "Punkte für einen neuen Inhalt",
    text: "Punkte dafür, dass eine Vokabel/Aufgabe zum ersten Mal richtig beantwortet wurde. Leer lassen = "
      + "den Vorschlag übernehmen, den die Übung selbst mitbringt.",
  },
  comboThreshold: {
    title: "Combo alle … Treffer",
    text: "Nach wie vielen richtigen Antworten am Stück es einen Bonus gibt. 0 = keine Combo. Leer lassen = "
      + "Vorschlag der Übung übernehmen.",
  },
  comboBonusPoints: {
    title: "Combo-Bonuspunkte",
    text: "Wie viele Punkte die Combo einbringt, wenn sie zuschlägt. Leer lassen = Vorschlag der Übung "
      + "übernehmen.",
  },
  useLeitner: {
    title: "Leitner-Kasten",
    text: "Verteiltes Wiederholen: Jeder Inhalt wandert bei richtiger Antwort ein Fach weiter und kommt "
      + "erst nach längerer Pause wieder dran; ein Fehler wirft ihn zurück. Aus heißt: jede Sitzung "
      + "zeigt wieder alles. An ist für Vokabeln fast immer die bessere Wahl.",
  },
  positionTimeSlot: {
    title: "Zeitfenster (Punkte-Faktor)",
    text: "Ein Zeitraum am Tag, in dem diese Pflicht mehr (oder weniger) einbringt – etwa „13–15 Uhr, "
      + "Faktor 2“ für die Hausaufgaben. Der Faktor gilt für die Punkte jeder richtigen Antwort, nicht für "
      + "die Ziel-Punkte. Leer lassen heißt: es gelten nur die Zeitfenster des Servers (vormittags mehr, "
      + "spätabends weniger). Beide werden zusammen betrachtet – liegt ein Zeitpunkt in mehreren Fenstern, "
      + "gewinnt das engste, und Faktoren werden nie multipliziert.",
  },
  requireTypedTest: {
    title: "Nur getippte Tests zählen",
    text: "Gegen Raten: Ein Test erfüllt die Pflicht nur, wenn das Kind die Lösung getippt hat – Anzeigen "
      + "oder Selbsteinschätzung reicht dann nicht. Nur bei Übungstypen wirksam, die überhaupt geprüft "
      + "werden.",
  },
  planDuration: {
    title: "Dauer in Tagen",
    text: "Wie lange der Plan läuft, ab dem Startdatum gerechnet. Nach dem Ende ist er nicht mehr spielbar "
      + "und es fällt kein Malus mehr an. Das Enddatum lässt sich später auf der Plan-Seite verschieben.",
  },
  planActive: {
    title: "Aktiver Plan",
    text: "Je Kind ist immer nur ein aktiver, laufender Plan spielbar. Aktivierst du diesen, werden die "
      + "anderen Pläne desselben Kindes automatisch deaktiviert – sonst könnte sich das Kind den "
      + "bequemsten zum Punktesammeln aussuchen.",
  },

  // ---- Übung im Katalog ----
  exercisePoints: {
    title: "Punkte der Übung",
    text: "Grundwert einer richtigen Antwort in dieser Übung. Was am Ende gutgeschrieben wird, hängt "
      + "zusätzlich am Zeitfenster und an Boni (Combo, schnelle Antwort).",
  },
  defaultItemCount: {
    title: "Standard-Begrenzung",
    text: "Vorschlag für die dauerhafte Inhalte-Begrenzung: auf wie viele der ersten Inhalte die Übung "
      + "beschränkt wird. Er wird übernommen, sobald die Übung in einen Lehrplan aufgenommen wird – dort "
      + "lässt er sich je Plan überschreiben. Leer lassen heißt: alle Inhalte werden abgefragt.",
  },
  defaultStage: {
    title: "Standard-Abfrageform",
    text: "Wie das Kind antworten soll: nur anschauen, selbst einschätzen, aus Buchstaben bauen, tippen, "
      + "aus Vorschlägen wählen oder nach Gehör tippen. Getippte Formen sind schwerer, aber "
      + "fälschungssicher – nur bei ihnen zählt „nur getippte Tests“.",
  },
  exerciseSchoolTypes: {
    title: "Schularten",
    text: "Für welche Schularten die Übung gedacht ist. Dient dem Wiederfinden und dem Vorschlag beim "
      + "Zusammenstellen eines Plans; sie sperrt nichts.",
  },
  exerciseSource: {
    title: "Quelle (Lehrbuch)",
    text: "Woher der Stoff stammt, z. B. „Access 5, Unit 3“. Frei formulierbar – der Filter über den "
      + "Übungen sucht darin.",
  },
  exerciseExecutePublic: {
    title: "Für andere Betreuer zuweisbar",
    text: "Der Startzustand beim Anlegen. Mit Haken darf jeder Betreuer die Übung sofort einem Kind "
      + "zuweisen – das ist die Vorgabe. Ohne Haken entsteht sie zurückgezogen: zuweisen kann sie dann nur, "
      + "wer ein ausdrückliches Recht darauf hat. „Zurückgezogen“ heißt aber nicht „unsichtbar“ – der "
      + "Katalog bleibt für jeden Creator durchsuchbar, gesperrt ist allein das Zuweisen. Später "
      + "umentscheiden kannst du es nicht hier, sondern in der Übungs-Verwaltung mit "
      + "„Zurückziehen“/„Wieder freigeben“; es ist dasselbe Merkmal, nur zu einem anderen Zeitpunkt.",
  },

  /*
   * Zwei Felder, die gleich aussehen und sich verschieden verhalten (B-143, Entscheidungen 3 und 4) —
   * genau darum steht hier je ein Satz. Beim Fach ist ein Freitext ein Unfall und soll wegräumbar sein;
   * bei der Schulart ist eine Kombination Absicht und soll überleben. Ohne die Erklärung wäre der
   * Unterschied eine Falle statt einer Hilfe.
   */
  seriesSubject: {
    title: "Fach",
    text: "Das Fach aus dem Katalog. Steht hier ein Name mit dem Zusatz „(Freitext)“ und lässt sich nicht "
      + "auswählen, dann trägt die Reihe den Namen noch, das Fach selbst gibt es aber nicht mehr – meist, "
      + "weil es jemand gelöscht hat. Du wirst ihn los, indem du „– keine Angabe –“ wählst und speicherst; "
      + "solange du das Feld nicht anfasst, bleibt er stehen.",
  },
  seriesSchoolTypes: {
    title: "Schulart",
    text: "Für welche Schulart die Reihe gedacht ist. Steht hier eine Kombination mehrerer Schularten, "
      + "lässt sie sich in diesem Feld nicht zusammenstellen – aber auch nicht versehentlich verlieren: "
      + "sie bleibt, solange du das Feld nicht anfasst. Wählst du dagegen etwas aus, gilt deine Wahl und "
      + "ersetzt die Kombination; auch „– für alle –“ ist eine Angabe und keine Leerung.",
  },

  /*
   * Zwei Texte für ein Wort, und das ist Absicht (B-69, Entscheidung 3): Die Beschriftung heißt überall
   * „Auch richtig", aber nur an der Vokabel gibt es die zweite Zeile mit demselben Wort — die
   * Homonym-Regel wäre bei Lücke, Liste und Übersetzung sinnloses Rauschen.
   *
   * Der gleiche `title` in beiden ist ebenfalls Absicht: Er ist die sichtbare Feldbezeichnung, und die
   * SOLL überall dieselbe sein. Er trägt zwar auch den Namen des Hinweis-Knopfs, aber die zwei Topics
   * begegnen sich auf keinem Bildschirm — der eine gehört dem Vokabel-Store, der andere dem Übungs- und
   * Lückentext-Editor. Käme das je zusammen, wäre `getByRole` dort mehrdeutig.
   */
  alsoCorrect: {
    title: "Auch richtig",
    text: "Weitere Lösungen, die genauso als richtig zählen wie die eigentliche. Ohne sie wird eine "
      + "richtige Antwort als Fehler gewertet, und das kostet über die Zielerreichung Münzen. Ein Feld "
      + "je Lösung — ein Wert darf selbst ein Komma enthalten.",
  },

  questionChoices: {
    title: "Auswahl",
    text: "Antwortmöglichkeiten zur Frage, die richtige eingeschlossen — anders als bei „Auch richtig“ "
      + "stehen hier also auch die falschen. Das Kind bekommt sie in dieser Reihenfolge zur Auswahl "
      + "(Multiple-Choice). Bleibt das Feld leer, wird die Frage frei beantwortet.",
  },

  // ---- Vokabel-Store ----
  translationAlternatives: {
    title: "Auch richtig",
    text: "Weitere Wörter, die für dieselbe Vokabel als richtig zählen – „huge“ etwa mit „riesig“ und "
      + "„sehr groß“. Ohne sie wird die zweite richtige Antwort als Fehler gewertet, und das kostet über "
      + "die Zielerreichung Münzen. Sie gelten nur in dieser Richtung (Wort → Übersetzung). Lege dafür "
      + "keine zweite Zeile mit demselben Wort an: zwei Zeilen heißen „zwei Bedeutungen“ (bank → Bank, "
      + "bank → Ufer), und die dürfen sich gerade nicht gegenseitig gelten lassen.",
  },

  // ---- Familien-Shop ----
  shopArticleNumber: {
    title: "Artikelnummer",
    text: "Dein eigenes Kürzel, um den Artikel wiederzufinden (z. B. „TV-001“). Muss eindeutig sein; das "
      + "Kind sieht sie nicht.",
  },
  shopActionType: {
    title: "Art der Belohnung",
    text: "Nur zum Einordnen und für das Symbol auf der Shop-Karte des Kindes – auf Preis oder Bestand "
      + "hat die Art keinen Einfluss.",
  },
  shopUnitType: {
    title: "Einheit",
    text: "Worin die Belohnung gemessen wird (Minuten, Stunden, Gramm, Stück, Mal). Zusammen mit „Menge "
      + "je Kauf“ ergibt sie den Text auf der Karte, z. B. „30 Min“.",
  },
  shopUnitsPerPurchase: {
    title: "Menge je Kauf",
    text: "Wie viele Einheiten ein einzelner Kauf bringt – bei „30“ und Einheit „Minuten“ also 30 Minuten "
      + "pro Kauf. Gekaufte Mengen sammeln sich im Inventar des Kindes.",
  },
  shopCoinPrice: {
    title: "Preis in 🪙 Münzen",
    text: "Münzen verdient das Kind mit erreichten Lernzielen. Der Familien-Shop ist der einzige Weg, sie "
      + "auszugeben. Mindestens einer der beiden Preise muss über 0 liegen.",
  },
  shopGemPrice: {
    title: "Preis in 💎 Gems",
    text: "Gems kommen aus Missionen und Auszeichnungen und dienen sonst den Skins. Ein Angebot darf beide "
      + "Währungen verlangen – dann wird beides abgebucht.",
  },
  shopStock: {
    title: "Bestand",
    text: "Wie oft das Angebot aktuell noch gekauft werden kann. Bei 0 ist es für das Kind sichtbar, aber "
      + "nicht kaufbar.",
  },
  shopMaxStock: {
    title: "Max-Bestand",
    text: "Bis wohin das automatische Auffüllen hochzählt – die Obergrenze, z. B. „höchstens 2 × Fernsehen "
      + "am Tag“. Ohne Auffüllen ist der Wert bedeutungslos.",
  },
  shopRefill: {
    title: "Auffüllen",
    text: "Ob und wie oft sich der Bestand von selbst wieder auf den Max-Bestand hebt (täglich, 2× täglich, "
      + "wöchentlich, einmalig). „Kein Auffüllen“ heißt: ist er leer, füllst du ihn von Hand.",
  },

  // ---- Missionen & Auszeichnungen (Gems) ----
  missionMetric: {
    title: "Ziel-Metrik",
    text: "Was gezählt wird: neue Wörter, richtige Wiederholungen, bestandene Tests, Übungsminuten, "
      + "komplette Tage oder Streak-Tage (Tage ohne Unterbrechung).",
  },
  missionTarget: {
    title: "Zielwert",
    text: "Wie oft die Metrik im Zeitraum erreicht sein muss, damit die Mission zählt – z. B. „10 richtige "
      + "Wiederholungen“.",
  },
  missionPeriod: {
    title: "Zeitraum",
    text: "„Täglich“ und „Wöchentlich“ setzen sich zurück und können immer wieder verdient werden, "
      + "„Einmalig“ gibt es genau ein Mal.",
  },
  missionReward: {
    title: "Belohnung in 💎 Gems",
    text: "Gems sind die Spaß-Währung: Das Kind kauft davon Skins – und, wenn du es so einstellst, einen "
      + "Teil von Shop-Artikeln. Für echte Belohnungen brauchst du 🪙 Münzen.",
  },
  achievementThreshold: {
    title: "Schwelle",
    text: "Der Gesamtstand, ab dem die Auszeichnung vergeben wird – anders als eine Mission zählt sie über "
      + "die ganze Zeit und wird genau einmal verliehen.",
  },

  // ---- Lernziele & Objectives ----
  keyResultMetric: {
    title: "Messlatte",
    text: "Woran der Erfolg gemessen wird – etwa wie viele Wörter sicher sitzen oder welche Note eine "
      + "Klassenarbeit bringt. Die Auswertung läuft live aus dem Lernstand, du musst nichts abhaken.",
  },
  keyResultTarget: {
    title: "Zielwert",
    text: "Der Wert, ab dem das Ziel erreicht ist. Bei einer Note ist es eine Obergrenze (kleiner ist "
      + "besser), sonst ein Mindestwert.",
  },
  objectiveKind: {
    title: "Art des Ziels",
    text: "„Verbindlich“ ist das, was ohnehin geschafft werden muss – es zahlt in 🪙 Münzen. „Ambitioniert“ "
      + "ist der freiwillige Aufschlag und zahlt in 💎 Gems. Ein verfehltes Ziel kostet nichts; den Malus "
      + "gibt es nur bei Positions-Pflichten im Lehrplan.",
  },
  objectiveReward: {
    title: "Belohnung bei Abschluss",
    text: "Wird einmalig gutgeschrieben, sobald alle Etappen des Ziels erreicht sind.",
  },
  objectiveRewardPerKr: {
    title: "Belohnung je Etappe",
    text: "Wird für jede einzelne erreichte Etappe gutgeschrieben – hält die Motivation auch bei einem "
      + "Ziel oben, das über Wochen läuft.",
  },

  // ---- Kind ----
  childPin: {
    title: "PIN des Kindes",
    text: "Der Login des Kindes in seiner App – ohne PIN kommt es nicht hinein. Sie lässt sich jederzeit "
      + "auf der Kind-Seite nachtragen oder ändern; gespeichert wird nur ein Hash, ablesen kannst du sie "
      + "später nicht mehr.",
  },
  interestFacet: {
    title: "Art des Interesses",
    text: "Ob es um ein Thema geht (Pokémon, Fußball) oder um einen Stil (Comic, Foto). Beides wird bei der "
      + "Bildauswahl unterschiedlich gewichtet: Das Thema zählt doppelt, der Stil einfach.",
  },
  interestWeight: {
    title: "Wie sehr?",
    text: "Von −3 (mag es gar nicht) bis +3 (liebt es). Negative Angaben schließen passende Bilder "
      + "vollständig aus, positive machen sie wahrscheinlicher. Gibt es keinen Treffer, bleibt die Karte "
      + "ohne Bild – ein beliebiges Motiv wäre schlechter als keins.",
  },

  // ---- Kontostand ----
  grantAmount: {
    title: "Betrag",
    text: "Was du außer der Reihe gutschreibst – für etwas, das die App nicht mitbekommt, oder um "
      + "Malus-Schulden zu erlassen.",
  },
  grantCurrency: {
    title: "Währung",
    text: "🪙 Münzen gibt das Kind im Familien-Shop für echte Belohnungen aus, 💎 Gems für Skins. Beides "
      + "kannst du verschenken.",
  },

  // ---- Lehrwerk-Reihe (Bearbeiten-Formular) ----
  seriesName: {
    title: "Name der Reihe",
    text: "Der Anzeigename, unter dem die Reihe überall in der App erscheint. Er lässt sich ändern – der "
      + "Kurzname darunter bleibt aber, wie er beim Anlegen entstanden ist: an ihm hängen die Verweise "
      + "von Kinder-Lehrbüchern und Fachlehrer-Profilen, und ein wandernder Kurzname würde sie lösen. "
      + "Nach einer Umbenennung passen Name und Kurzname darum nicht mehr zusammen, und das ist "
      + "gewollt. Zwei Reihen dürfen nicht denselben Namen tragen.",
  },
} as const satisfies Record<string, FieldHelp>;

/** Die zulässigen Schlüssel – ein Tippfehler im `topic` fällt so beim Übersetzen auf, nicht im Betrieb. */
export type HelpTopic = keyof typeof FIELD_HELP;
