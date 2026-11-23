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
      + "einmal pro Woche (Mo–So). „frei“ heißt: darf geübt werden, muss aber nicht – dann bringt die Position "
      + "nichts: weder Ziel-Punkte noch Übungspunkte, Combo oder Tempo-Bonus, und es gibt keinen Münz-Malus. "
      + "„täglich, bis erledigt“ gibt es nur mit Nacharbeit: jeden Tag eine ganze Runde, bis alle Aufgaben frei getippt "
      + "sind – dann endet die Pflicht von selbst, ohne weiteren Malus.",
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
      + "und nicht dieses Feld. Bei „beide“ Richtungen zählt jede Richtung als eigene Karte.",
  },
  newItemsPerDay: {
    title: "Neue Inhalte je Tag",
    text: "Die Tagesdosis: so viele noch nie gezeigte Inhalte bringt die Übungsrunde höchstens pro Tag, in der "
      + "Reihenfolge der Übung. Dazu kommen die schon bekannten, die der Leitner-Kasten heute fällig macht. So "
      + "trägt eine einzige Pflicht eine große Übung über Wochen – 150 Vokabeln mit 8 je Tag sind in knapp vier "
      + "Wochen durch. Der Abschlusstest prüft dann die Tagesportion (heute neu, heute geübt oder heute fällig) "
      + "und nicht mehr alles bisher Gelernte. Leer = keine Dosis: alle Inhalte sind vom ersten Tag an dran, "
      + "und das passt nur für kleine Übungen. Fragt eine Vokabelübung „beide“ Richtungen ab, ist jede Richtung "
      + "eine eigene Karte: 8 je Tag heißt dann 4 neue Wörter, jedes hin und zurück.",
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
      + "gibt das Kind im Familien-Shop für echte Belohnungen aus. Bei „frei“ gibt es sie nicht. "
      + "Auf der Plan-Seite steht hier beim Hinzufügen einer Übung deren Empfehlung; was du änderst, gilt "
      + "für diese eine Position. Der Assistent belegt den Wert stattdessen aus der gewählten Intensität vor.",
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
  speedThresholdSeconds: {
    title: "Tempo bis … Sekunden",
    text: "Bis zu wie vielen Sekunden eine richtige Antwort als „schnell“ zählt und den Tempo-Bonus bringt. "
      + "0 = kein Tempo-Bonus. Leer lassen = Vorschlag der Übung übernehmen (beim Bearbeiten: bleibt, wie es ist).",
  },
  speedBonusPoints: {
    title: "Tempo-Bonuspunkte",
    text: "Wie viele Punkte eine schnelle richtige Antwort zusätzlich einbringt. 0 = aus. Leer lassen = Vorschlag "
      + "der Übung übernehmen (beim Bearbeiten: bleibt, wie es ist).",
  },
  scope: {
    title: "Inhalte-Auswahl",
    text: "Welche Inhalte der Übung diese Position spielt: alle, nur die noch neuen oder nur die schon "
      + "eingeführten (reine Wiederholung). Vorgabe ist „alle Inhalte“.",
  },
  stage: {
    title: "Stufe",
    text: "Wie der Test abfragt – vom Aufdecken mit Selbsteinschätzung bis zum freien Tippen. Welche Stufen es "
      + "gibt, hängt am Übungstyp; „Standard der Übung“ übernimmt, was der Ersteller der Übung vorgesehen hat. "
      + "Ein Stufenplan je Plantag geht dieser Einstellung vor – etwa der, den der Assistent beim Ziel „Klassenarbeit“ "
      + "schreibt (erst Selbsteinschätzung, dann die gewählte Stufe). Er wechselt die Stufe ab seinem Tag für alle "
      + "Inhalte der Position, nicht je Leitner-Fach.",
  },
  useLeitner: {
    title: "Leitner-Kasten",
    text: "Verteiltes Wiederholen: Jeder Inhalt wandert bei richtiger Antwort ein Fach weiter und kommt "
      + "erst nach längerer Pause wieder dran; ein Fehler wirft ihn zurück. Aus heißt: jede Sitzung "
      + "zeigt wieder alles. An ist für Vokabeln fast immer die bessere Wahl.",
  },
  remediation: {
    title: "Nacharbeit",
    text: "Für einen verpatzten Test: Jede Runde geht der Reihe nach durch alle Aufgaben, die noch offen sind. Jede "
      + "beginnt als Freitext; hängt das Kind fest, schaltet es selbst leichter – Kästchen, Buchstabensalat, Auswahl. "
      + "Das kostet nichts, aber erledigt ist eine Aufgabe erst, wenn sie frei getippt richtig war. Offene Aufgaben "
      + "kommen in der nächsten Runde wieder.",
  },
  completionBonus: {
    title: "Abschlussbonus",
    text: "Münzen, die es genau einmal gibt, wenn die letzte Aufgabe der Nacharbeit frei getippt richtig war. Danach "
      + "verschwindet die Position aus der Tagesmission. 0 = kein Bonus.",
  },
  forgivingDemotion: {
    title: "Ausrutscher verzeihen",
    text: "Nur mit Leitner-Kasten: Ein Fehler wirft ein Wort nur ein Fach zurück statt auf Fach 1; erst ein zweiter "
      + "Fehler in Folge setzt es ganz zurück. Fällig ist es nach einem Fehler trotzdem sofort wieder. Aus heißt: jeder "
      + "Fehler zurück auf Fach 1. Der Lernstand je Vokabel über alle Pläne bleibt in jedem Fall streng.",
  },
  positionTimeSlot: {
    title: "Zeitfenster (Punkte-Faktor)",
    text: "Ein Zeitraum am Tag, in dem diese Pflicht mehr (oder weniger) einbringt – etwa „13–15 Uhr, "
      + "Faktor 2“ für die Hausaufgaben. Der Faktor gilt für die Punkte jeder richtigen Antwort, nicht für "
      + "die Ziel-Punkte. Leer lassen heißt: es gelten nur die Zeitfenster des Servers (vormittags mehr, "
      + "spätabends weniger). Beide werden zusammen betrachtet – liegt ein Zeitpunkt in mehreren Fenstern, "
      + "gewinnt das engste, und Faktoren werden nie multipliziert.",
  },
  lernkontrolle: {
    title: "Lernkontrolle",
    text: "Womit diese Pflicht geprüft wird. „Test getippt“: das Kind muss die Lösung tippen. „Test, "
      + "Selbsteinschätzung zählt“: Aufdecken und „Gewusst!“ zählen mit – schalte „nur getippte Tests“ ein, wenn "
      + "dir das zu weich ist. Ausnahme Stufenleiter („Selbsteinschätzung bis Tag …, ab Tag … getippt“): dort bleibt "
      + "das Häkchen aus, sonst wäre das Ziel an den ersten Tagen unerreichbar – der Server lehnt die Kombination ab; "
      + "getippt wird ab dem genannten Tag ohnehin. „kein Test – zählt Durchspielen“: diese Übungsart hat keinen Test, "
      + "erfüllt ist die Pflicht, sobald genug davon gespielt ist.",
  },
  requireTypedTest: {
    title: "Nur getippte Tests zählen",
    text: "Gegen Raten: Ein Test erfüllt die Pflicht nur, wenn das Kind die Lösung getippt hat – Anzeigen "
      + "oder Selbsteinschätzung reicht dann nicht. Nur bei Übungstypen wirksam, die überhaupt geprüft "
      + "werden. Braucht eine getippte Test-Stufe: auf „Selbsteinschätzung“ (auch als Standard der Übung) wäre die "
      + "Pflicht nie erreichbar.",
  },
  planDuration: {
    title: "Dauer in Tagen",
    text: "Wie lange der Plan läuft, ab dem Startdatum gerechnet. Nach dem Ende ist er nicht mehr spielbar "
      + "und es fällt kein Malus mehr an. Das Enddatum lässt sich später auf der Plan-Seite verschieben.",
  },
  planActive: {
    title: "Aktiver Plan",
    text: "Je Kind ist ein aktiver Plan je Fach spielbar – Englisch und Französisch laufen nebeneinander. "
      + "Aktivierst du diesen, wird der aktive Plan desselben Fachs desselben Kindes automatisch deaktiviert – "
      + "sonst könnte sich das Kind innerhalb eines Fachs den bequemsten zum Punktesammeln aussuchen.",
  },
  planSubject: {
    title: "Fach des Plans",
    text: "Jeder Plan gehört zu einem Fach. Das Fach entscheidet, welchen Plan ein neuer aktiver Plan ablöst: "
      + "nur den aktiven Plan desselben Fachs, die Pläne anderer Fächer laufen weiter. Übungen aus anderen "
      + "Fächern darf der Plan trotzdem enthalten.",
  },
  planUnit: {
    title: "Unit des Plans",
    text: "Optional: die Unit des Lehrwerks, die der Plan durcharbeitet – zum Wiederfinden und als Grundlage "
      + "für Vorschläge. Zur Wahl stehen die Units der Reihen des gewählten Fachs.",
  },

  // ---- Übung im Katalog ----
  /*
   * Der Text sagte bis B-45 „Grundwert einer richtigen Antwort" – das ist die Bedeutung von
   * `newContentPoints` an der Position, und dieses Feld hat sie nie gehabt: kein Bewertungspfad las es.
   * Seit B-45 ist es die Empfehlung für die Zielbelohnung, und der Text sagt das.
   */
  articleRequired: {
    title: "Artikel muss mitgetippt werden",
    text: "Ohne Haken zählt ein weggelassener Artikel nicht als Fehler: „Stadt“ für „die Stadt“, „école“ für "
      + "„l’école“ – gemessen wird das Wort, nicht die Tipp-Disziplin. Ein anderer oder dazuerfundener Artikel "
      + "bleibt falsch. Den Haken setzen, wenn die Übung genau den Artikel trainiert (Geschlecht der Wörter). "
      + "Gilt im Üben und im Test gleich.",
  },
  exercisePoints: {
    title: "Punkte der Übung",
    text: "Deine Empfehlung, wie viele 🪙 Münzen das Erreichen des Ziels wert ist. Wer die Übung auf der "
      + "Plan-Seite hinzufügt, bekommt sie als Vorschlag vorbelegt und kann sie überschreiben; der "
      + "Assistent setzt die Punkte stattdessen aus der gewählten Intensität. Punkte für einen zum ersten "
      + "Mal richtig beantworteten Inhalt stellt dagegen die Position ein.",
  },
  defaultItemCount: {
    title: "Standard-Begrenzung",
    text: "Vorschlag für die dauerhafte Inhalte-Begrenzung: auf wie viele der ersten Inhalte die Übung "
      + "beschränkt wird. Er wird übernommen, sobald die Übung in einen Lehrplan aufgenommen wird – dort "
      + "lässt er sich je Plan überschreiben. Leer lassen heißt: alle Inhalte werden abgefragt.",
  },
  defaultStage: {
    title: "Standard-Abfrageform",
    text: "Wie das Kind antworten soll – welche Formen es gibt, hängt vom Übungstyp ab: etwa selbst einschätzen, aus "
      + "Vorschlägen wählen, Buchstaben ordnen, in Buchstabenkästchen oder frei tippen, beim Lückentext auch mit Wortbank "
      + "oder Wortkasten, bei Vokabeln als Diktat nach Gehör. "
      + "Getippte Formen sind schwerer, aber fälschungssicher – nur bei ihnen zählt „nur getippte Tests“. "
      + "Buchstaben ordnen geht nur bei einem Wort aus 2 bis 12 Buchstaben; jede andere Lösung (etwa „didn't he“) "
      + "kommt als Buchstabenkästchen. Die Auswahl je Lücke braucht mindestens zwei falsche Vorschläge, sonst "
      + "kommen ebenfalls Kästchen.",
  },
  exerciseSchoolTypes: {
    title: "Schularten",
    text: "Für welche Schularten die Übung gedacht ist. Dient dem Wiederfinden und dem Vorschlag beim "
      + "Zusammenstellen eines Plans; sie sperrt nichts.",
  },
  exerciseDressing: {
    title: "Eingekleidet in",
    text: "Die Interessen, in deren Sprache die Übung erzählt – dieselben Stichworte, aus denen auch die "
      + "Bildauswahl schöpft. Sie ändern nicht, WAS geübt wird, nur die Beispiele drumherum. Meist trägt "
      + "der KI-Creator sie ein; hier siehst und korrigierst du sie. Ein Stichwort, das Übungen "
      + "einkleidet, lässt sich nicht mehr löschen.",
  },
  grammarTopics: {
    title: "Grammatik-Themen",
    text: "Welche Grammatik die Übung übt – aus dem geteilten Vokabular des Fachs („Present perfect“, „Passé "
      + "composé“), nicht als Freitext. Nur darüber findet ein Betreuer beim Zusammenstellen Übungen zu einem "
      + "Thema über Lehrwerke und Klassen hinweg. Fehlt ein Thema, lege es hier an; ein zweites Mal mit demselben "
      + "Namen liefert das bestehende. Eine neue Übung übernimmt die Themen ihrer Unit als Vorschlag – abwählen "
      + "geht, vererbt wird nichts.",
  },
  exerciseSource: {
    title: "Quelle (Lehrbuch)",
    text: "Woher der Stoff stammt, z. B. „Access 5, Unit 3“. Frei formulierbar – der Filter über den "
      + "Übungen sucht darin.",
  },
  exerciseBuildPath: {
    title: "Aufbau-Weg",
    text: "Macht diese Übung zu einer leichteren Vorstufe einer anderen Übung derselben Unit: Stufe 1 heißt erkennen, "
      + "Stufe 2 einsetzen mit Auswahl; die Ziel-Übung ist die oberste Stufe. Ein Weg ist eine Ebene tief – eine "
      + "Vorstufe ist nie selbst Ziel.",
  },
  startBuildLevel: {
    title: "Start-Stufe",
    text: "Die Übung hat leichtere Vorstufen. Hier legst du fest, auf welcher Stufe des Wegs dein Kind beginnt – die "
      + "letzte ist die Übung selbst. Ein bestandener Test hebt die Stufe um eins, wirksam ab dem nächsten Tag. Solange "
      + "eine Position auf einem Aufbau-Weg ist, gibt es kein Tages- oder Wochenziel – nur „frei“ oder „täglich, bis "
      + "erledigt“.",
  },
  exerciseBookPages: {
    title: "Buchseite(n)",
    text: "Die Seite im Lehrbuch, wie gedruckt – „42“, „42–43“ oder „WB 12“ (höchstens 20 Zeichen). Anders als "
      + "die Quelle sieht das Kind sie auf seinem Pfad und kann die Übung im Buch nachschlagen.",
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
  /*
   * Dieselbe Sache an zwei weiteren Feldern (B-148, Entscheidung 4) – und darum je ein eigener Text statt
   * eines geteilten: Was der Nutzer verliert, ist hier ein anderes Ding (sein Buch, sein Fachlehrer), und
   * eine Formulierung, die für alle drei passt, passt am Ende zu keinem. Die Beschriftung „(Freitext)" ist
   * dagegen wortgleich – sie ist das Erkennungszeichen, nicht die Erklärung.
   */
  textbookSubject: {
    title: "Fach",
    text: "Das Fach aus dem Katalog. Steht hier ein Name mit dem Zusatz „(Freitext)“ und lässt sich nicht "
      + "auswählen, dann trägt das Buch den Namen noch, das Fach selbst gibt es aber nicht mehr – meist, "
      + "weil es jemand gelöscht hat. Du wirst ihn los, indem du „– keine Angabe –“ wählst und speicherst; "
      + "solange du das Feld nicht anfasst, bleibt er stehen, auch wenn du andere Felder änderst.",
  },
  profileSubject: {
    title: "Fach",
    text: "Das Fach, für das dieser Fachlehrer zuständig ist. Steht hier ein Name mit dem Zusatz "
      + "„(Freitext)“ und lässt sich nicht auswählen, dann gibt es das Fach im Katalog nicht mehr – der "
      + "Name bleibt, damit du siehst, worum es ging. Er zählt beim Zuordnen zu einem Kind aber nicht mehr "
      + "mit; wähle ein Katalog-Fach, wenn das Profil wieder treffen soll.",
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
  sentenceForm: {
    title: "Satzform",
    text: "Die Form, die der Satz verlangt, wenn sie vom Wort im Vokabel-Speicher abweicht – etwa „patients“ statt "
      + "„patient“. Ist sie gesetzt, ist sie die Lösung, und die Grundform allein zählt als falsch. Die Bedeutung im "
      + "Wortkasten kommt weiter aus dem Speicher.",
  },
  cueDistractors: {
    title: "Ablenker im Wortkasten",
    text: "Zusätzliche deutsche Wörter für den Wortkasten, die zu keiner Lücke gehören – am besten aus derselben "
      + "Wortfamilie wie eine Lösung (zu „Rassismus“ etwa „rassistisch“). Sie wirken nur auf der Stufe "
      + "„Wortkasten + Freitext“; die Bedeutungen der Lücken kommen von selbst aus dem Vokabel-Speicher.",
  },
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
      + "(Multiple-Choice); Einträge, die sich nur in Groß/Klein, in Schlusszeichen (. ? ! …), der Schreibweise "
      + "des Apostrophs oder im Leerraum unterscheiden, "
      + "werden ein Knopf, und fehlt die Antwort, kommt sie als letzter Knopf dazu. Bleibt das Feld leer, "
      + "wird die Frage frei beantwortet.",
  },

  // B-607: der Hinweis gehört zur deutschen Antwort der Erkennen-Karte; jede Karte, die das Fremdwort abfragt,
  // verwirft ihn. Der Text sagt das, damit niemand einen Hinweis schreibt, den das Kind nie sieht.
  vocabularyHint: {
    title: "Hinweis",
    text: "Ein Tipp zur deutschen Antwort, etwa „beginnt mit h“. Ohne eigenen Hinweis steht dort meist der Artikel aus "
      + "dem Vokabelspeicher. Das Kind sieht ihn nur beim Üben auf der Auswahlstufe, im Abschlusstest nie, und nur bei "
      + "„Fremdwort → Deutsch“ oder „beides“ (dort auf der Karte vom Fremdwort zum Deutschen; sie wird nie geschrieben, "
      + "höchstens als Auswahl gefragt). Beim Schreiben (Kästchen, Buchstaben ordnen, Freitext, Diktat, Lernkarte) "
      + "erscheint er nie, weil dort das Fremdwort die Antwort ist. Bei „Deutsch → Fremdwort“ erscheint er auf keiner Stufe.",
  },

  // ---- Vokabel-Store ----
  vocabListPaste: {
    title: "Vokabelliste einfügen",
    text: "Eine Zeile je Vokabel: erst das Wort, dann die Übersetzung, getrennt durch Tabulator, Semikolon, "
      + "„ – “ oder „ = “ – so, wie eine Tabelle oder eine abgetippte Liste es liefert. Weitere Spalten "
      + "zählen als „Auch richtig“. Ein Komma trennt nicht: „Abfall, Müll“ bleibt eine Übersetzung. Wörter, "
      + "die es im Vokabelspeicher schon gibt, werden wiederverwendet statt doppelt angelegt. Zeilen ohne "
      + "Trenner (eine Überschrift etwa) werden übersprungen und genannt.",
  },
  gapPrompt: {
    title: "Deutsche Vorgabe",
    text: "Die Aufgabe dieser Lücke auf Deutsch, so wie sie im Test stand – ein ganzer Satz („Während des Films "
      + "schlief ich ein.“) oder ein Wort („Richtung Osten“). Das Kind sieht sie offen über dem Text. Ohne Vorgabe "
      + "gibt es als Tipp die Übersetzung aus dem Vokabelspeicher. Für eine Einzelwort-Aufgabe ist der Text nur {{1}}.",
  },
  gapDistractors: {
    title: "Falsche Vorschläge",
    text: "Nur für die Abfrageform „Auswahl je Lücke“: falsche Antworten, die neben der Lösung dieser Lücke stehen – "
      + "am besten ähnliche Formen („don't she“, „isn't she“ neben „doesn't she“). Ohne sie bietet die Lücke die "
      + "Lösungen der anderen Lücken an. Bis zu vier werden gezeigt; mit weniger als zwei kommen Buchstabenkästchen.",
  },
  translationAlternatives: {
    title: "Auch richtig",
    text: "Weitere deutsche Wörter, die für dieselbe Vokabel als richtig zählen – „huge“ etwa mit „riesig“ und "
      + "„sehr groß“. Sie zählen, wo das Kind die Bedeutung erkennt (Auswahl, Selbsteinschätzung): dort wird die "
      + "zweite richtige Bedeutung nicht als Fehler gewertet und nie als falsche Option angeboten. Geschrieben "
      + "wird immer das Fremdwort, dort spielen sie keine Rolle. Lege dafür "
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
  shopMaxStock: {
    title: "Obergrenze je Kind",
    text: "Jedes Kind hat seinen eigenen Bestand: kauft eines, bleibt es für die Geschwister kaufbar. Die "
      + "Obergrenze ist der Startbestand und das Höchste, bis wohin das Auffüllen hochzählt – z. B. „höchstens "
      + "2 × Fernsehen am Tag“.",
  },
  shopRefill: {
    title: "Auffüllen",
    text: "Ob und wie oft sich der Bestand jedes Kindes von selbst auffüllt (täglich, 2× täglich, wöchentlich am "
      + "gewählten Tag, monatlich am 1., einmalig am Datum). Hat niemand am Stichtag reingeschaut, wird beim nächsten "
      + "Öffnen nachgeholt. „Kein Auffüllen“ heißt: ist er leer, füllst du ihn von Hand.",
  },
  shopRefillQuantity: {
    title: "+ je Termin",
    text: "So viel kommt an jedem Auffüll-Termin dazu, höchstens bis zur Obergrenze. Gleich der Obergrenze heißt: "
      + "Reste verfallen. Kleiner heißt: das Kind kann ansparen – z. B. +4 im Monat bis 8 sind zwei Monate.",
  },
  shopAudience: {
    title: "Für",
    text: "Für welche Kinder das Angebot gilt. Leer heißt alle. So bekommt jedes Kind sein eigenes Taschengeld – "
      + "„Taschengeld Theodor“ und „Taschengeld Friedrich“ als zwei Angebote mit je eigener Menge.",
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
  bonusDailyCoinCap: {
    title: "Tagesdeckel fürs Bonus-Training",
    text: "So viele Münzen kann dein Kind am Tag höchstens mit Extra-Übungen verdienen – zusätzlich zu den "
      + "Münzen aus dem Lernpfad. Leer lassen heißt: die Vorgabe des Servers gilt. 0 heißt: üben ja, Münzen "
      + "nein. Erlaubt sind 0 bis 1000.",
  },
  towerGameMinutesPerDay: {
    title: "Spielzeit Vokabel-Türme",
    text: "So viele Minuten am Tag darf dein Kind das Handyspiel Vokabel-Türme spielen. Gezählt wird nur, "
      + "solange das Spiel im Vordergrund läuft. Ist die Zeit um, startet kein neues Level mehr; ein "
      + "laufendes darf noch bis zu 5 Minuten zu Ende gespielt werden. Leer lassen heißt: Vorgabe des "
      + "Servers (20 Minuten). 0 heißt: das Spiel ist aus. Erlaubt sind 0 bis 240.",
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
