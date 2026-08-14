---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Ladefenster im Assistenten, Kästchen ohne disabled, Auswahl überlebt den Filterwechsel]
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-08-13 zu B-161
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-161]
---

# B-169 · Im Ladefenster sind die alten Zeilen anklickbar — und die Auswahl überlebt

B-161 hat die unsichtbare Auswahl im Lehrplan-Assistenten geschlossen. Ihr Schaden ist über eine zweite Tür
weiter erreichbar: **während** eine neue Suche lädt, stehen die Zeilen der *vorigen* noch da und sind
bedienbar. Ein Haken darin gehört zu einem Filter, der nicht mehr gilt — und nichts leert ihn je wieder.

## Ist-Stand am Code (selbst nachgeprüft)

Drei Bausteine, jeder einzeln richtig, zusammen ein Loch:

1. **Der Effekt hängt am Kriterien-Schlüssel** (`frontend/src/vater/VaterWizard.tsx:184-194`): Bei einer
   Abweichung leert er die Auswahl und setzt `geltenderFilterKey.current = filterKey` — **sofort**, lange
   vor der Antwort.
2. **Gerendert wird `exercises.data`, nicht der Schlüssel.** Der Platzhalter greift bewusst nur bei
   `exercises.loading && exercises.data === null` (`:532`) — nach dem ersten Laden bleiben die alten Zeilen
   also stehen (das ist die B-116-Regel und richtig, sonst hängen aufgeklappte Bereiche aus).
3. **Das Kästchen trägt kein `disabled`** (`:542`). „Alle wählen" daneben trägt
   `disabled={selectAllBusy || exercises.loading}` (`:525`) — **der Wächter steht ein Element neben dem
   Loch.**

## Fehlerszenario

1. Schritt 3, Fach Englisch. Die Liste zeigt „Unit 2 – Wörter" (#42).
2. In „Übung suchen…" `envi` tippen. Kein Debounce (`:475`) — jeder Tastendruck feuert eine Abfrage. Der
   Effekt hat schon geleert und den Schlüssel weitergesetzt; die alten Zeilen stehen noch.
3. **Während die Abfrage läuft** das Kästchen von #42 anklicken → `selected = [42]`, und `toggle` löscht
   dabei zusätzlich den Hinweis (`:220-224`).
4. Die Antwort kommt, die Liste zeigt zwei andere Übungen. `filterKey` hat sich **nicht erneut** geändert →
   der Effekt läuft nicht → `selected` bleibt `[42]`.
5. „Weiter" prüft nur `length === 0` (`:294`) → Feinschliff → `finish` schickt `exerciseIds: [42]`.

**Ergebnis:** eine Tagesziel-Position mit `penaltyCoins` für eine Übung, die der gezeigte Filter
ausschließt. Wörtlich der P1-Schaden, gegen den B-161 gebaut wurde.

Der einzige Resthinweis ist „davon 1 unten nicht sichtbar" (`:465`) — und der ist hier **irreführend**:
`unsichtbar > 0` zieht die zwei Situationen zusammen, die B-161 getrennt haben wollte. Die genehmigte
(„gewählt jenseits der geladenen Seite", Entscheidung 3 — dort ist „unten" richtig) und die verbotene
(„gar nicht in dieser Trefferliste"). Ein Titel steht nirgends; der Vater kann #42 nicht identifizieren.

## Abgrenzung

**Nicht** von [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) gedeckt: dessen AK 3
repariert die **Trefferzahl** während des Ladens. Ein Zahlen-Fix lässt die alten Zeilen anklickbar. B-162
nennt dieses Fenster ausdrücklich als „von B-161s Diff nicht verschlechtert" — das ist richtig und trifft
diesen Fund nicht, denn er war schon vor B-161 da und ist von ihr nur *nicht mitgenommen* worden.

## Offene Punkte

Alle in der Grill-Runde vom 2026-08-14 geschlossen (autonom, `art: Defekt`, Freigabe 1 des Nachtlaufs).

1. ~~Trägt eine Ref das Gate?~~ → Entscheidung 1. **Die Empfehlung dieser Story war falsch** und ist
   korrigiert.
2. ~~Vergleicht man Schlüssel oder Ladezustände?~~ → Entscheidung 2.
3. ~~Braucht „Alle wählen" ein zweites Gate?~~ → Entscheidung 3.
4. ~~Wird die Unsichtbaren-Zahl umformuliert?~~ → Entscheidung 4. **Die Empfehlung ist entfallen**, weil
   der Fall nach Entscheidung 1 unerreichbar wird.
5. ~~Hilft ein Debounce?~~ → Entscheidung 5 (zurückgestellt).

## Entscheidungen

1. **Das Gate kommt aus dem Render, nicht aus einer Ref.** Die Empfehlung im Angriffsplan-Entwurf
   (`seitenSchluessel.current`) **funktioniert nicht**: Eine Ref nimmt am Rendern nicht teil. Der Render,
   der die neuen Zeilen erstmals zeigt, berechnet `disabled` **bevor** der Effekt die Ref nachzieht — die
   frischen Zeilen wären also gesperrt und blieben es bis zum nächsten Rendern aus anderem Grund. Der
   Schlüssel der geladenen Seite gehört darum in `useState`. *Kosten:* ein zusätzlicher Render je Antwort
   (ein `setState` im bestehenden Effekt auf `[exercises.data]`). Das ist billig und der einzige Weg, der
   nicht einen zweiten Fehler einbaut, während er den ersten behebt.
2. **Verglichen werden Schlüssel, nicht Ladezustände.** `exercises.loading && exercises.data !== null` wäre
   **heute** gleichwertig — die Übungsabfrage ruft nirgends `reload()` (nachgesehen: kein einziger Treffer
   in `VaterWizard.tsx`). Es zieht aber „die Kriterien haben sich geändert" und „dieselben Kriterien werden
   neu geladen" zusammen, und das ist **genau die Fehlerfamilie, die diese Story behebt**. Einen Fix in der
   Form des Fehlers zu bauen, den er behebt, wäre die schlechteste Art, Zeilen zu sparen. *Kosten:* ein
   `useState` mehr statt einer abgeleiteten Bedingung.
3. **`selectAll` bleibt unangetastet.** Sein `disabled={selectAllBusy || exercises.loading}` deckt das
   Ladefenster schon ab. *Kosten:* keine — die Entscheidung steht hier nur, damit ein späterer Leser nicht
   ein zweites Gate daneben baut und sich dann fragt, welches gilt.
4. **Der Hinweistext „davon N unten nicht sichtbar" bleibt unverändert.** Nach Entscheidung 1 ist der
   irreführende Fall **unerreichbar**: Eine Auswahl kann einen Filterwechsel nur überleben, wenn sie im
   Ladefenster entstand (der Effekt leert sie bei jedem Schlüsselwechsel), und genau das schließt das Gate.
   Ein zweiter Satz für einen Fall, der nicht eintreten kann, ist toter Text. *Kosten:* Schafft ein späterer
   Weg den Zustand doch wieder, kehrt die irreführende Formulierung zurück — darum bleibt der Befund in
   dieser Story stehen, und AK 3 hält das Gate mechanisch.
5. **Kein Debounce im Suchfeld — zurückgestellt.** Es würde das Fenster verkleinern und ist verlockend,
   behebt aber nichts: ein Fenster von 80 ms ist genauso anklickbar wie eines von 400 ms. Und es ändert das
   Antwortgefühl der Suche, also eine Produktfrage. *Kosten:* Jeder Tastendruck feuert weiter eine Abfrage;
   das ist eine eigene Story wert, wenn es je auffällt — hier nicht mitgenommen.

## Akzeptanzkriterien

1. Solange die gezeigte Trefferliste zu einem **anderen** Filter gehört als dem eingestellten, ist kein
   Kästchen anklickbar.
2. Sobald die Antwort da ist, sind die Kästchen wieder bedienbar — die Sperre ist ein Fenster, kein
   Dauerzustand. (Das ist die Hälfte, die Entscheidung 1 überhaupt nötig machte.)
3. Ein Test wird rot, wenn das Gate entfernt wird, und die rote Probe nennt ihre Zahl.
4. „Alle wählen" bleibt gesperrt wie bisher; kein zweites Gate daneben.
5. Der Hinweistext bleibt unverändert, und ein Kommentar sagt, warum (Entscheidung 4).

## Schätzung

**Größe: XS** — eine `useState`-Zeile, eine Zuweisung im bestehenden Effekt, ein `disabled` am Kästchen, ein
Kommentar. Dazu ein E2E-Fall. Kleiner als B-161 (`S`, dieselbe Datei, aber dort drei neue Funktionen in
`wizardSearch.ts` plus eine Live-Region).

- **`wo: frontend`** — keine Server-Beteiligung; die Abfrage bleibt wie sie ist.
- **`migration: nein`** — kein Schema.
- **`vertragsbruch: nein`** — kein DTO, kein Endpunkt, kein Feld.

**Risiken:**

1. **Der Ausgangs-Render.** `useState(filterKey)` startet mit dem Kriterien-Schlüssel, `exercises.data` ist
   `null` → der Platzhalter zeigt, es gibt keine Zeilen zum Sperren. Kein Risiko, aber der Fall gehört
   benannt, weil er der einzige ist, in dem Schlüssel und Daten nicht zueinander gehören *und* das
   in Ordnung ist.
2. **`subjectId === ""`** ergibt `filterKey === ""` und eine synchron aufgelöste leere Antwort. Die ist bei
   jedem Aufruf ein **neues Objekt**, der Effekt läuft also und zieht den Schlüssel nach — nachgesehen, nicht
   vermutet.
3. **Reihenfolge zweier Antworten** kann das Gate nicht aushebeln: `useAsync` bricht die vorige Anfrage per
   `cancelled`-Flag ab (`useAsync.ts:26,30,37`), `data` trägt also immer die Antwort der neuesten Generation.
   Damit gehört der Schlüssel, der im Effekt gelesen wird, wirklich zu den gezeigten Zeilen.

**Angriffsplan:**

1. `const [seitenSchluessel, setSeitenSchluessel] = useState(filterKey)` neben `geltenderFilterKey`.
2. Im bestehenden Effekt auf `[exercises.data]` (der `merken`-Effekt, `:241`) den Schlüssel nachziehen.
3. `const zeilenVeraltet = seitenSchluessel !== filterKey` und `disabled={zeilenVeraltet}` am Kästchen.
4. Kommentar an beiden Stellen: **warum** State statt Ref (Entscheidung 1) und warum kein Ladezustand
   (Entscheidung 2).
5. Rote Probe, dann Fix, dann grüner Lauf — je mit Zahl.

**Testweg**: `frontend/e2e/assistent.spec.ts` erweitern. `wizardSearch.test.ts` kann es **nicht** tragen (es
kennt keine Ladezustände), und ein Komponententest ist hier keine Option: die Konvention verbietet den
nachgebauten Bildschirm mit gefälschtem `fetch` (`frontend/CLAUDE.md`). Das Ladefenster wird stattdessen mit
`page.route` **verzögert** — echter Server, nur später; Präzedenz ist `vater-von-null.spec.ts:57` mit
`route.fallback()` für alles, was nicht das Ziel ist. Rote Probe: `disabled` entfernen und belegen, dass
genau dieser Fall fällt.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-161. Der Ist-Stand ist von mir gegengeprüft
  (`:184-194`, `:525`, `:532`, `:542`): das Kästchen trägt tatsächlich kein `disabled`, während der Knopf
  daneben eines hat.
- 2026-08-14 · `ausformuliert → gegrillt`, autonom (`art: Defekt`, Freigabe 1 des Nachtlaufs). Fünf
  Entscheidungen. Die tragende hat die **eigene Empfehlung dieser Story widerlegt**: das im Angriffsplan
  vorgeschlagene `seitenSchluessel.current` (eine Ref) kann das Gate nicht tragen, weil eine Ref am Rendern
  nicht teilnimmt — der Render, der die frischen Zeilen erstmals zeigt, hätte sie gesperrt. Und die zweite,
  billigere Variante (`exercises.loading && data !== null`) wurde **verworfen, obwohl sie heute gleichwertig
  ist**: sie zieht „Kriterien geändert" und „dieselben Kriterien neu geladen" zusammen, also genau die
  Fehlerfamilie, die diese Story behebt. Entfallen ist dabei die zweite Hälfte des Vorschlags: der
  irreführende Hinweistext wird nach Entscheidung 1 unerreichbar und bleibt darum unverändert.
- 2026-08-14 · `gegrillt → geschaetzt`. **XS** / `frontend` / `migration: nein` / `vertragsbruch: nein`.
  Drei Dinge nachgesehen statt vermutet: die Übungsabfrage ruft nirgends `reload()` (darum wäre die
  Ladezustands-Variante heute gleichwertig — und wird trotzdem nicht genommen); `useAsync` bricht die vorige
  Anfrage per `cancelled`-Flag ab, der gelesene Schlüssel gehört also wirklich zu den gezeigten Zeilen; und
  für das verzögerte Laden in der E2E gibt es Präzedenz (`vater-von-null.spec.ts:57`, `route.fallback()`),
  ein Komponententest wäre hier konventionswidrig.
- 2026-08-14 · Gebaut, und **die Story hat unterwegs eine zweite Tür bekommen**. Der Ablauf mit Zahlen:

  | Probe | erwartet / gemessen |
  |---|---|
  | Kästchen im Ladefenster, **vor** dem Fix | rot — erwartet `disabled`, gemessen `enabled` |
  | dieselbe Probe nach dem Fix | grün, beide Fälle der Spec |
  | „Alle wählen" im **Fehlerzweig**, vor dem zweiten Fix | rot — erwartet `disabled`, gemessen `enabled` |
  | nach dem zweiten Fix | grün, drei Fälle der Spec |

  **Der zweite Fund kommt vom `frontend-reviewer` und widerlegt Entscheidung 3.** Sie sagte, „Alle wählen"
  brauche kein Gate, weil sein `exercises.loading` dasselbe Fenster decke. Das gilt **nicht im Fehlerzweig**:
  dort bleibt `data` die alte Seite, `loading` fällt auf `false`, und der Effekt auf `[exercises.data]` läuft
  nie (die Referenz ändert sich nicht). Der Knopf war also wieder bedienbar und wählte die Ids der veralteten
  Liste — `unsichtbar` vergleicht gegen genau diese Liste und meldet `0`, es gäbe also kein Signal. Derselbe
  P1-Schaden, nur über den Nachbarknopf. Der Reviewer hatte den Pfad aus dem Code abgeleitet und ausdrücklich
  **nicht** rot geprobt; die Probe oben ist meine. Entscheidung 3 gilt weiter in ihrem Kern (**eine** Wahrheit,
  nicht zwei Gates) — dieselbe Bedingung sitzt jetzt an beiden Steuerelementen.
  Fehlerzähler des Sprints: **1** (der Fix ändert Code).
- 2026-08-14 · Zwei weitere Reviewer-Funde eingearbeitet, beide an der neuen Spec: das Kind wurde **geerbt**
  statt gewählt (die Liste kommt `OrderBy(Name)`, der Durchstich derselben Datei legt ein „E2E-Assistent-…"
  an, das vor „Sohn" sortiert) — und weil Klasse und Schulart des Kindes den Katalog serverseitig filtern
  („Begrüßungen" 5–6, „Vocabulary: The environment" 8–10, **disjunkt**), hätte ein Kind mit gesetzter Klasse
  je eine der beiden Zeilen verschwinden lassen. Jetzt `selectOption("1")` statt Vorbelegung, dazu
  `{ exact: true }` an den drei `getByLabel`-Aufrufen. Fehlerzähler: **2** (Teständerung).
  Nicht mitgenommen und darum abgelegt: [B-177](B-177-seitenschluessel-haengt-an-einer-batching-annahme.md)
  (der Effekt liest `filterKey`, hängt aber an `[exercises.data]` — praktisch unerreichbar, an keinem Tor).
  Nachgezogen: [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) trug eine **falsche**
  Analyse (`data` werde bei einem Fehler `null` — gilt nur für die erste Ladung) und ist P3 → P2, weil aus
  „ein unwahrer Satz" seit diesem Gate „ein bedienungsloser Bildschirm" geworden ist.
- 2026-08-14 · `geschaetzt → abgenommen`. **Rollengang an der laufenden App** (Freigabe 6), Server nach der
  letzten Änderung mit frischer DB gestartet. Gemessen am Assistenten, Schritt 3, mit verzögerter Suche:

  | Zeitpunkt | Zeilen | davon gesperrt | „Alle wählen" |
  |---|---|---|---|
  | vor dem Filterwechsel | 6 | 0 | bedienbar |
  | **im Ladefenster** | 6 (die alten) | **6** | **gesperrt** |
  | nach der Antwort | 1 | 0 | bedienbar |

  Damit sind AK 1, AK 2 und AK 4 **live** belegt, nicht nur im Test. **Anmerkung zum Weg dorthin:** Die
  Anmeldung per Browser-Automatisierung erreicht Reacts Zustand nicht (`form_input` setzt den DOM-Wert, das
  Formular bleibt leer) — ein Werkzeug-Artefakt, kein Produktdefekt. Nach zwei Versuchen habe ich das Mittel
  gewechselt statt es zu wiederholen: Sitzung per echtem `POST auth/adult` in den `localStorage`, wie es die
  E2E-Konfiguration auch tut. Der Rollengang selbst lief danach an der echten Oberfläche.
  **Regressionszeugen:** Creator-Fläche (`/vater/katalog`) und Sohn-Arcade (`/sohn`) unverändert — kein Pfad
  von ihnen liegt im Diff, und ihre Specs sind im vollen Lauf grün.
  **Verifikation:** **828/828** Backend, **280/280** Komponententests, **35 von 36** E2E (der eine Rote ist
  `bilder.spec.ts`, allein grün, Ursache jetzt in [B-153](B-153-bilder-spec-flackert-im-vollen-lauf.md)
  belegt — **außerhalb** dieses Diffs), `dotnet format` und `markdownlint` sauber, `frontend-reviewer` und
  `pugling-reviewer` gelaufen.
