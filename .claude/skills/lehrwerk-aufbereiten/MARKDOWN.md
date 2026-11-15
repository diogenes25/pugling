# Seiten-Markdown: Format und Tags

Schritt 1 des Skills macht aus jedem Foto eines Lehrbuchs **eine** Markdown-Datei. Sie ersetzt das Foto für
jeden späteren Leser: ein anderes LLM (Stoffnotizen, Wortlisten, Übungen, Klausuren) soll nie wieder ein Bild
öffnen müssen. Darum gilt: **was auf der Seite steht, steht in der Datei** – als Fakt genau, als Text
beschrieben. `md_pruefen.py` hält Form und Vollständigkeit mechanisch; was es nicht sieht (stimmt der Inhalt?),
prüft eine Stichprobe gegen das Foto.

## Ablage

```text
raw/<fach>/<werk-slug>-pugling/md/      gitignored wie die Fotos
  _buch.md                              Steckbrief des Buchs (Pflicht, eine Datei)
  _index.md                             erzeugt von md_pruefen.py --index, nie von Hand
  umschlag.md                           Umschlag/Titelei
  s004-005.md                           ein Foto = eine Datei, benannt nach den Buchseiten (dreistellig)
  s147.md                               Einzelseite
  s024-025-b.md                         zweites Foto derselben Seiten (Nachfotografie, anderer Ausschnitt)
  foto-070459.md                        Seitenzahl nicht erkennbar: Uhrzeit aus dem Dateinamen
```

Liegt ein Foto in mehreren Dateien? Nein – jedes Foto gehört zu **genau einer** Datei (Feld `fotos`). Zwei
Fotos derselben Doppelseite dürfen sich eine Datei teilen (`fotos: [a.jpg, b.jpg]`), wenn sie zusammen erst die
Seite ergeben.

## Frontmatter (jede Seitendatei)

```yaml
---
typ: seite
titel: "Découvertes Série bleue 2 · S. 24–25 · Unité 1 – Pratique"
buch: "Découvertes Série bleue 2"
fach: Französisch
klasse: 7
verlag: Klett
seiten: "24-25"            # Buchseiten wie gedruckt; "Umschlag", "Titelei" oder "?" wenn nicht erkennbar
fotos: [IMG_20261111_135900.jpg]
kapitel: "Unité 1 – Bienvenue à Paris"   # Kapitel/Unit/Lektion wie gedruckt; "–" außerhalb von Kapiteln
seitentyp: [lektion, uebungen]           # aus der Liste unten, mindestens einer
drehung: 180               # wie das Foto gedreht werden musste, um es zu lesen: 0, 90, 180, 270
lesbarkeit: gut            # gut | teilweise | schlecht
unsicher: 0                # Anzahl der Einträge unter "## Unsicher"
tags: [buch/decouvertes-serie-bleue-2, fach/franzoesisch, stufe/klasse-7, lernjahr/2, schulart/gymnasium,
       verlag/klett, seitentyp/lektion, seitentyp/uebungen, kapitel/unite-1, thema/wegbeschreibung,
       grammatik/passe-compose]
---
```

`buch`, `fach`, `klasse`, `verlag` sind in allen Dateien eines Buchs **identisch** (wie in `_buch.md`).

**Kapitel-Regeln.** `kapitel` ist **ein** Wert: das Kapitel der linken Seite bzw. bei Anhangseiten der
Anhangsteil (`Grammaire`, `Vokabelteil`). Die Tags tragen dagegen **jedes** berührte Kapitel: eine Doppelseite
über eine Kapitelgrenze bekommt beide `kapitel/`-Tags, eine Anhangseite zusätzlich `kapitel/<unit>` für jede
Unit, zu der ihr Stoff gehört (Grammatikanhang G4–G8 zu Unité 1 und 2 → `kapitel/grammaire`,
`kapitel/unite-1`, `kapitel/unite-2`). Ausnahme: Register, Stichwort- und Wörterverzeichnisse tragen nur den
Anhang-Tag (sonst trügen sie jedes Kapitel und wären als Filter wertlos). Lauten Inhaltsverzeichnis und
Kolumnentitel verschieden, gilt das Inhaltsverzeichnis; die Abweichung steht in der Kapiteltabelle. Kapitelnummern werden arabisch getaggt („Kapitel IV“ →
`kapitel/kapitel-4`, Feld `kapitel` bleibt wie gedruckt); Lerneinheiten darunter (IV.1, Volet 2, Atelier A)
bekommen `abschnitt/4-1`, `abschnitt/atelier-a`. Hat das Buch eine Ebene **über** den Kapiteln
(Themenblöcke aus mehreren Lektionen), bleibt `kapitel/` die Lektion und der Block wird zusätzlich als
`kapitel/<block>` getaggt; Seiten, die nur zum Block gehören (Abschluss-/Plateau-Seiten), tragen nur den
Block-Tag.

**Seitenzahl nicht gedruckt** (Auftakt-Doppelseite, angeschnittene Ecke): aus der Nachbarseite erschließen, die
Datei danach benennen und unter Unsicher als `**nicht im Bild**` mit „(abgeleitet aus S. …)“ vermerken; nur wenn
auch das nicht geht, `foto-<zeit>.md` und `seiten: "?"`.

## Tags (Obsidian)

Hierarchisch, klein, ASCII: Umlaute als `ae oe ue ss`, Akzente weg, Wörter mit `-` (`thema/bruchrechnung`,
`grammatik/ablativ`). Nur diese Präfixe:

| Präfix | Pflicht | Inhalt |
|---|---|---|
| `buch/` | ja | Slug des Buchs inkl. Band/Klasse/Land: `buch/lambacher-schweizer-9-sh` |
| `fach/` | ja | `mathematik`, `latein`, `englisch`, `franzoesisch`, `deutsch`, `biologie`, … |
| `stufe/` | ja | `stufe/klasse-9` (Jahrgangsstufe) |
| `verlag/` | ja | `verlag/klett`, `verlag/cornelsen`, `verlag/vandenhoeck-ruprecht` |
| `seitentyp/` | ja | genau die Werte aus Feld `seitentyp` |
| `schulart/` | wenn bekannt | `gymnasium`, `realschule`, … |
| `lernjahr/` | Fremdsprachen | `lernjahr/2` |
| `kapitel/` | wenn im Kapitel | `kapitel/unite-1`, `kapitel/lektion-12`, `kapitel/kapitel-3` |
| `thema/` | Inhaltsseiten | Sachthemen und Stoffgebiete: `thema/quadratische-funktionen`, `thema/london` |
| `grammatik/` | Fremdsprachen | Grammatik-Themen der Seite: `grammatik/present-perfect` |
| `abschnitt/` | optional | Lerneinheit innerhalb eines Kapitels: `abschnitt/4-1`, `abschnitt/atelier-a` |
| `kompetenz/` | optional | `hoerverstehen`, `leseverstehen`, `sprechen`, `schreiben`, `mediation`, `aussprache`, `wortschatz`, `argumentieren`, `modellieren`, `problemloesen`, `kommunizieren`, `werkzeuge` |

**Das Tag-Vokabular steht in `_buch.md`** (Abschnitt `## Tags`): jeder `kapitel/`-, `abschnitt/`-, `thema/`-,
`grammatik/`- und `kompetenz/`-Tag, den eine Seitendatei benutzt, muss dort stehen – sonst meldet `md_pruefen.py` einen
Fehler. Zuerst aus dem Inhaltsverzeichnis befüllen (Kapitel, Grammatik-Themen, Sachthemen); fehlt beim Lesen
einer Seite ein Thema, wird es **dort ergänzt**, nicht als Variante erfunden. Vor dem Ergänzen nachsehen, ob es
das Thema unter anderem Namen schon gibt (`relativpronomen-qui-que-ou`, nicht daneben `relativsatz`). Für
Verben und Formen: `grammatik/verb-voir`, nicht `grammatik/voir`. **Ein Begriff, ein Tag:** derselbe Slug steht
nie unter zwei Präfixen (Aussprache und Betonung sind `kompetenz/aussprache`, nicht zusätzlich `thema/` oder
`grammatik/`). `thema/` benennt das Sachthema einer Seite (London, Ernährung, Römisches Bad) – nicht die
Personen oder Gegenstände aus Beispielsätzen; reine Grammatik- und Tabellenseiten haben meist kein `thema/`.

**Seitentypen:** `umschlag`, `titelei` (Impressum, Vorwort, Aufbau des Buchs), `inhaltsverzeichnis`,
`einstieg` (Kapitel-/Unit-Auftakt), `lektion` (Lehrtext, Lesetext, Dialog, Lernabschnitt), `merkwissen`
(Merkkasten, Regel, Definition, Satz), `uebungen`, `test` (Bilan, Training, Klassenarbeitsvorbereitung,
Selbsttest), `methoden` (Strategien, Skills, Lernmethoden), `grammatik` (Grammatikanhang), `vokabeln`
(Wortschatzteil, Wortliste), `loesungen`, `register` (Stichwort-/Wörterverzeichnis), `landeskunde`, `anhang`
(sonstiger Anhang), `sonstiges`.

Eine Seite trägt **alle** zutreffenden Typen. Grenzfälle: ein Merk- oder Regelkasten mitten in einer
Lektion → zusätzlich `merkwissen` (auch eine kleine Randtabelle mit Formen); ein Strategie-/Methodenkasten in
einer Lektion → zusätzlich `methoden`; ein Sachtext über Land und Leute (auch ohne eigenen Kasten) →
`landeskunde`; Wiederholungs- oder Startkapitel vor Unit 1 → wie eine Unit (`einstieg`/`lektion`/`uebungen`);
Zusatz-/Differenzierungsübungen im Anhang → `uebungen` (+ `anhang` nur, wenn kein Stoff dabei ist);
Kapitelauftakt mit Bildern und Lernzielen → `einstieg`; Rückblick/Zusammenfassung am Kapitelende →
`merkwissen` (+ `test`, wenn ein Selbsttest folgt); „Wiederholen – Vertiefen – Vernetzen“ → `uebungen`;
Exkursion/Zusatzstoff → `lektion`. Legt `_buch.md` unter `## Aufbau` für eine Seitenart dieses Buchs etwas
anderes fest (etwa „So geht's“-Kästen im Grammatikteil sind Grammatik, nicht `methoden`), **gilt `_buch.md`**.

## Körper (jede Seitendatei, alle Abschnitte, in dieser Reihenfolge)

```markdown
# <titel>

## Überblick
2–4 Sätze: Was ist das für eine Seite, welcher Stoff, wozu dient sie im Kapitel.

## Seitenelemente
Jedes Element der Seite in Lesereihenfolge (links vor rechts, oben vor unten), eins je Zeile:
- **S. 24 · Überschrift** „Pratique: les verbes“
- **S. 24 · Merkkasten** Bildung des passé composé mit être (Inhalt → Fakten)
- **S. 24 · Aufgabe 1** Lückensätze mit Verbformen (→ Aufgaben)
- **S. 25 · Lesetext** „…Titel…“: Inhalt in 2–3 eigenen Sätzen, Personen, Ort, neue Strukturen
- **S. 25 · Abbildung** Stadtplan mit Metrostationen (was zu sehen ist und wozu)

## Fakten
Alles Sachliche, das ein Leser zum Lernen braucht – genau übertragen, nichts zusammengefasst:
Vokabeln (Tabelle), Formeln (LaTeX `$…$`), Definitionen und Sätze, Regeln, Paradigmen/Konjugations- und
Deklinationstabellen, Zahlen, Daten, Namen, Inhaltsverzeichnis-Einträge mit Seitenzahl.

## Aufgaben
| Nr | Art | Was verlangt wird | Stoff | Niveau/Verweis |
|---|---|---|---|---|
| S. 24 · 1 | Lückentext | Formen von *aller* im passé composé einsetzen | passé composé mit être | – |
| S. 25 · 9A | Hörverstehen | Aussagen zu einem Gespräch als richtig/falsch einordnen | Uhrzeit | leichter; Cahier 12, 3 |

## Unsicher
- Kein Eintrag → genau die Zeile `– keine –`.
- **unleserlich** S. 24, Aufgabe 3: Arbeitsanweisung durch Spiegelung verdeckt [beste Lesart]
- **nicht im Bild** S. 25 unten: Fußnote abgeschnitten
- **Druckfehler?** S. 22: „4e A“ gedruckt, Lektionstitel sagt „4e B“ – wie gedruckt übernommen
```

Ist ein Abschnitt leer (Umschlag hat keine Aufgaben), steht dort `– keine –`.

**`Stoff` nennt das Thema, nie den Ansatz oder das Ergebnis** („quadratische Ergänzung“, nicht „erst
normieren, dann pq-Formel“ oder $A(s)=(s-9)^2$): Die Dateien dienen auch zum Erzeugen von Übungen und dürfen
keine Lösungen verraten. Lösungen stehen nur auf Seiten vom Typ `loesungen`.

**Aufgaben-Tabelle:** genau diese fünf Spalten. `Nr` immer mit Seite (`S. 24 · 9A`, `S. 142 · Station 3 · 1A`,
`S. 27 · Bilan`), weil Nummern auf einer Doppelseite doppelt vorkommen. `Niveau/Verweis`: Schwierigkeitssymbol
(leichter/schwerer/Wahl; steht es an einem Kasten, gilt es für jede Aufgabe darin), Querverweise (Cahier, CD,
„zu 7A5“, Grammatik G12, „wie 13“, Beispiel 2, Lösungen S. …), sonst `–`. Querverweise gehören hierhin, nie in
`Stoff`.

**Unsicher-Einträge** beginnen mit genau einer Kategorie: `**unleserlich**` oder `**nicht im Bild**` (ein neues
Foto hilft), `**Druckfehler?**` (eindeutig lesbar, aber offensichtlich verdruckt: Tippfehler, falscher Verweis)
bzw. `**fraglich**` (sauber gedruckt, aber fachlich zweifelhaft, etwa eine merkwürdige Übersetzung). Sind zwei
Lesarten möglich (Doppeldruck, verschmierte Ziffer), ist es `**unleserlich**`. **Kein** Eintrag für einen
Doppeldruck, der trotzdem eindeutig lesbar ist, und für absichtliche Sonderzeichen (Betonungsakzente, Makrons):
die werden einfach wie gedruckt übernommen.

**Handschrift im Buch** (Schülernotizen, Unterstreichungen, Schwärzungen) ist ein Seitenelement „Handschrift“
und kommt nie in die Fakten; verdeckt sie Druck, steht die Stelle unter Unsicher.

**Keine HTML-Tags** und keine `<…>` im Text (das Prüfskript hält sie für Platzhalter). Farbige Hervorhebungen
im Buch (Endungen in Paradigmen, markierte Satzglieder) werden **fett** gesetzt, die Farbe einmal je Tabelle
bzw. Beispiel in Klammern genannt („Endungen fett = im Buch rot“).

**Eigene Ergänzungen** (eine Regel, die nicht gedruckt ist, eine erschlossene Seitenzahl) stehen mit dem
Zusatz `(abgeleitet)` – ein späterer Leser muss Buch und Bearbeiter unterscheiden können.

### Was genau, was beschrieben

| Art | Behandlung |
|---|---|
| Vokabeln, Wortlisten | **buchstabengetreu**, Zeile für Zeile, als Tabelle `Wort \| Übersetzung \| Hinweis` (siehe „Wortlisten“) |
| Formeln, Tabellen, Paradigmen, Zahlen, Namen, Daten | **genau** |
| Inhaltsverzeichnis | **jeder Eintrag** mit Seitenzahl |
| Definitionen, Merksätze, Regeln | **sinngemäß vollständig** in eigenen Worten; Fachbegriffe und Formeln genau |
| Sprachmaterial zum Lernen: Redemittel-/„On dit“-Kästen, Wortkästen, Satzanfänge, Grammatik-Beispielsätze im Merkkasten, Checklisten-Punkte | **genau** (das ist Lernstoff wie eine Vokabel) |
| Aufgaben | Nummer, Art, was verlangt wird, der dafür nötige Stoff; bei Mathe die Zahlenwerte/Terme (Fakten); **nicht** die Aufgabensätze/Items selbst (Lückensätze, Fragen) |
| Material **in** einer Aufgabe, ohne das sie nicht lösbar ist: vorgerechnete (auch fehlerhafte) Rechnungen, Kärtchen zum Zuordnen, Wertetabellen, Graphen, zu prüfende Aussagen („Gilt immer …?“) | **genau** unter Fakten, mit Verweis auf die Aufgabennummer |
| Lese-/Lehrtexte, Dialoge, Geschichten | **nicht abschreiben** (Urheberrecht): Titel, Inhalt in eigenen Sätzen, Personen, neue Wörter/Strukturen, die vorkommen |
| Abbildungen, Fotos, Grafiken | was zu sehen ist und welche Information sie tragen (Achsen, Werte, Beschriftungen genau) |

Beispiel Latein-Übungsseite: Wortreihen „Welches Wort passt nicht?“, Formenkästen zum Bestimmen und
Tabellenvorgaben sind Material der Aufgabe → genau unter Fakten mit Aufgabennummer; die lateinischen
Übungssätze zum Übersetzen → beschrieben (Anzahl, geübte Form, Wortschatz der Lektion).

Die Grenze: was das Kind **lernen** soll, steht genau; was es **bearbeiten** soll (Aufgabe, Text), wird
beschrieben. Einzelwörter und kurze Wendungen aus Texten als Belege sind in Ordnung, ganze Textsätze nicht.

### Wortlisten (Vokabelteil, Wörterverzeichnis, Namensliste)

Immer genau diese drei Spalten, eine gedruckte Zeile = eine Tabellenzeile, Reihenfolge wie im Buch:

```markdown
| Wort | Übersetzung | Hinweis |
|---|---|---|
| avoir l'air | aussehen | [avwaʀlɛʀ] · Beispielsatz der Randspalte |
| ↳ avoir lieu | stattfinden | [avwaʀljø] · II6D |
| hortus, ī | Garten | Hort · Nr. 20 |
```

- `Wort` wie gedruckt (Artikel, Stammformen, Genus, Makrons, Akzente), **ohne** Lautschrift und **nicht fett**.
  Untereintrag (Wendung unter einem Stichwort) beginnt mit `↳ `.
- `Hinweis`: zuerst die Lautschrift `[…]`, dann alles Weitere mit ` · ` getrennt – Beispielsatz der Randspalte,
  Merkhilfen, verwandte Wörter, Fundstelle (`II6D`, `Nr. 20`). Leer, wenn nichts gedruckt ist; nie `–`.
- Eine Wortliste mit anderer Richtung (deutsch → Fremdsprache) behält die Spalten: `Wort` ist dann das deutsche
  Stichwort.
- Beispielsätze dürfen hier stehen (private Notiz unter `raw/`); `laden.py` übernimmt nur Wort und Übersetzung.

`md_migrieren.py` bringt ältere Dateien mechanisch in diese Form, `md_pruefen.py` hält sie.

### Weitere Grenzfälle

- **Unit-Tags nur für Stoff, der einer Unit gehört** (Vokabeln, Grammatik, Zusatzübungen einer Unit) – nicht
  für unit-übergreifende Methoden- oder Strategieseiten, auch wenn sie auf Unit-Seiten verweisen.
- **Namensliste mit Aussprache** → `register`.
- **Zuordnungsaufgaben:** das zuzuordnende Material (Satzhälften, Kärtchen, Bilder mit Nummern) steht genau
  unter Fakten; Lückensätze, Fragen und Übersetzungssätze werden beschrieben.
- **Absichtlich Unscharfes oder Verdecktes im Druck** (Mustertext als Grafik, abgedeckter Ausschnitt) ist ein
  Seitenelement mit Beschreibung, kein Unsicher-Eintrag.
- **Karten und Pläne:** was sie zeigen und die lernrelevanten Beschriftungen genau; eine bewusste Auswahl wird im
  Seitenelement gesagt („Metroplan, 20 von ca. 300 Stationen übernommen“), nicht unter Unsicher.
- **Unterstreichung** im Buch wie farbige Hervorhebung: **fett** plus einmal „(im Buch unterstrichen)“.
- `_buch.md` darf unter `## Aufbau` für dieses Buch **abweichende** Regeln festlegen (welcher Wert im Feld
  `kapitel` bei Anhangseiten steht, welche Seitenart welchen Typ bekommt); sie gehen den allgemeinen vor.

## `_buch.md` (Steckbrief)

```yaml
---
typ: buch
titel: "Lambacher Schweizer 9 – Mathematik für Gymnasien, Schleswig-Holstein"
buch: "Lambacher Schweizer 9"
fach: Mathematik
klasse: 9
verlag: Klett
reihe: "Lambacher Schweizer"
schulart: Gymnasium
region: Schleswig-Holstein      # optional
sprache: de                     # Unterrichtssprache des Buchs; bei Fremdsprachen zusätzlich zielsprache: fr
lernjahr: 2                     # nur Fremdsprachen: dann trägt jede Seite den Tag lernjahr/2 (geprüft)
fotos-ordner: "raw/Math"
tags: [buch/lambacher-schweizer-9-sh, fach/mathematik, stufe/klasse-9, verlag/klett, schulart/gymnasium]
---
```

Körper, in dieser Reihenfolge:

- `## Überblick` – wofür das Buch ist, Klasse/Lernjahr mit Beleg (Titelei, Impressum) oder `(abgeleitet)`.
- `## Kapitel` – Tabelle Nr · Titel (wie gedruckt, = Wert des Feldes `kapitel`) · Tag · Seiten ·
  Themen · fotografiert ja/teilweise/nein, aus dem Inhaltsverzeichnis. Lerneinheiten (`abschnitt/`) als eigene,
  eingerückte Zeilen unter ihrem Kapitel.
- `## Aufbau` – wiederkehrende Seitenarten des Buchs und welcher `seitentyp` ihnen entspricht
  (Atelier → `lektion`, Bilan → `test` …).
- `## Tags` – das verbindliche Tag-Vokabular: je Präfix (`kapitel`, `thema`, `grammatik`, `kompetenz`) eine
  Zeile mit allen erlaubten Tags als `#präfix/slug`.
- `## Abdeckung` – welche Buchseiten fotografiert sind und welche fehlen. Wird **nach** dem letzten Fotoblock
  geschrieben bzw. aktualisiert (die Seitenliste steht dann in `_index.md`).
