---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/supervisor]
aliases: [Ein Enum zwei Rollen, SchoolTypes Filter oder Attribut, unbekannte Schulart]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-149, Entscheidung 4 (Grill-Runde 2026-08-11)
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-11-14
wartet_auf: ""
---

# B-152 · `SchoolTypes` ist Filter und Attribut zugleich — vier Stellen leiten daraus drei Antworten ab

Prüfauftrag, abgespalten von [B-149](B-149-schularten-tabelle-statt-manifest.md) (dort Entscheidung 4).
B-149 sorgt dafür, dass eine **neue** Schulart am Build auffällt; sie fasst aber keine der Stellen an, die
mit einem Wert umgehen müssen, den die Liste nicht kennt. Und die gehen drei verschiedene Wege.

Die tiefere Ursache ist keine Schlamperei, sondern ein Typ mit zwei Aufgaben:

- **Filter** (Übung, Reihe, Fachlehrer-Profil): Eine Kombination ist sinnvoll, `None` heißt *„für alle"*
  (`Pugling.Contracts/Common/LearnBaseTypes.cs`: „no filter exclusion").
- **Attribut** (Kind, `AdminEntities.cs:69`): Ein Kind besucht **eine** Schulart, eine Kombination ist ein
  Modellierungsunfall, und `None` heißt *„nicht angegeben"* (`VaterKind.tsx:240` beschriftet es so).

## Die drei Antworten (Stand `d8abdc9`, in B-149 gemessen)

| Stelle | Verhalten bei einem Wert, den `SCHOOL_TYPES` nicht kennt |
| --- | --- |
| `VaterLehrwerke.tsx:319`, `VaterFachlehrer.tsx:327` | gesperrte Option — sichtbar, unwählbar, überlebt |
| `ExerciseEditModal.tsx:142-143` | **still verworfen** beim Laden ins Formular |
| `VaterKind.tsx:150,186` | überlebt, aber `None` wird dadurch **unerreichbar** |

Die dritte Zeile ist dieselbe Klasse, die am 2026-08-11 in B-148 als Regression behoben wurde — dort im
Fachlehrer-Formular, hier seit Längerem am Kind. Und sie greift **heute schon**, weil `Child.SchoolType`
dasselbe `[Flags]`-Enum ist und eine Kombination tragen kann.

## Warum das ein Prüfauftrag ist und kein Aufräumen

Ein denkbares Ergebnis ist „bleibt". Zwei Rollen an einem Enum können billiger sein als zwei Typen, und
dann ist das richtige Ergebnis eine Begründung im Vertragsprojekt und ein `verworfen` hier — nach den
Regeln dieses Bereichs ein Erfolg. Als `Aufräumen` etikettiert („kein Verhalten ändert sich") wäre dieser
Ausgang gar nicht vorgesehen.

**Kein `entgangen_bei`:** Der Zustand ist älter als B-148/B-149; die beiden haben ihn nur sichtbar gemacht.

## User Story

Als **Vater** möchte ich, dass ein Kind genau eine Schulart (oder keine Angabe) trägt, damit Fachlehrer-Treffer und Übungsfilter
für das Kind eindeutig sind und kein Client still eine Kombination speichert, die das Formular nicht darstellen kann.

## Ist-Stand am Code (2026-11-13)

- **Das Formular ist schon Einfachauswahl** mit erreichbarem „– keine Angabe –":
  [VaterKind.tsx:239-242](../../frontend/src/vater/VaterKind.tsx#L239). Ein Wert, den `SCHOOL_TYPES` nicht kennt, fällt beim Laden auf
  `None` zurück ([VaterKind.tsx:160](../../frontend/src/vater/VaterKind.tsx#L160)); geschickt wird `schoolType` nur bei Änderung (`:196`).
- **Der Server prüft nichts:** [ChildrenController.cs:82](../../backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs#L82)
  (`Create`, `dto.SchoolType ?? None`) und [ChildrenController.cs:123](../../backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs#L123)
  (`Update`) übernehmen jede Flags-Kombination und – weil `JsonStringEnumConverter` Zahlen zulässt
  ([Program.cs:41](../../backend/Pugling.Api/Program.cs#L41)) – auch undefinierte Bits.
- **Vertrag:** `SchoolTypes? SchoolType` in [ChildDtos.cs:18,29](../../backend/Pugling.Contracts/Supervisor/ChildDtos.cs#L18); Modell
  `Child.SchoolType` in [AdminEntities.cs:69](../../backend/Pugling.Api/Models/AdminEntities.cs#L69). Weitere Schreiber gibt es nicht
  (`grep "SchoolType = "` über `Pugling.Api`).
- **Filter-Rolle:** `VaterLehrwerke.tsx:570` zeigt einen unbekannten Wert als gesperrte Option; `ExerciseEditModal.tsx:295-296`
  verwirft ihn still beim Laden.

## Die echte Lücke

Die Entscheidung vom 2026-10-23 („der Server validiert") ist nicht gebaut. Das Formular hält die Regel, jeder andere Client
(`Pugling.Client`, `.http`, KI-Agent) nicht.

## Offene Punkte

1. ~~Darf ein Kind eine Kombination tragen? Server oder Frontend?~~ → Entscheidung 1 (Nutzer, 2026-10-23)
2. ~~Was geschieht mit einer schon gespeicherten Kombination in der echten DB?~~ → Entscheidung 2
3. ~~Was soll eine Oberfläche mit einem unbekannten Filter-Wert tun (Übungsdialog verwirft still)?~~ → Entscheidung 3 (zurückgestellt)

## Entscheidungen

1. **Ein Kind trägt genau einen Wert oder `None`; der Server validiert (`400 validation_error`), das Formular ist Einfachauswahl;
   der Typ bleibt ein Flags-Enum.** Entscheidung des Nutzers in der Grill-Runde G-4 vom 2026-10-23
   ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)). *Kosten:* ein Aufrufer, der heute eine Kombination schickt, bekommt
   `400`.
2. **Bestandszeilen bleiben unangetastet.** Validiert wird nur ein **gesendeter** Wert; ein `PATCH` ohne `schoolType` läuft auch bei
   einem Kind mit Kombination durch. Keine Migration, die Kombinationen umschreibt. Begründung: der Fortschritt der Kinder in der
   echten `pugling.db` darf nicht still brechen, und das Formular normalisiert beim nächsten bewussten Speichern ohnehin auf einen
   Wert. *Kosten:* eine Alt-Kombination lebt weiter, bis jemand die Schulart anfasst.
   Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
3. **Zurückgestellt:** der Umgang der Filter-Oberflächen mit unbekannten Werten (`ExerciseEditModal.tsx:295-296` verwirft still).
   Begründung: seit B-149 fällt eine neue Schulart am Build auf, ein unbekannter Wert entsteht nur noch über ein undefiniertes Bit per
   API; das ist eine eigene, kleinere Frage und gehört nicht zur Kind-Validierung. *Kosten:* die stille Verwerfung im Übungsdialog
   bleibt. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

## Akzeptanzkriterien

1. `POST …/supervisor/children` mit `schoolType: "Gymnasium, Realschule"` antwortet `400 validation_error`; es entsteht kein Kind.
2. `PATCH …/supervisor/children/{id}` mit einer Kombination oder einem undefinierten Zahlenwert (z. B. `1024`) antwortet `400`; das Kind
   ist unverändert.
3. Ein einzelner Wert und `None` gehen auf beiden Wegen durch (`201`/`200`).
4. Ein `PATCH` ohne `schoolType` an einem Kind, das (per Datenbank) eine Kombination trägt, antwortet `200` und lässt sie stehen.

## Schätzung

**Größe: XS** – eine Prüfung (`value == None || (BitOperations.IsPow2((int)value) && Enum.IsDefined(value))`) an zwei Stellen, ein
Test. `migration: nein` (Entscheidung 2), `vertragsbruch: nein` (kein DTO-Feld ändert sich; die engere Prüfung trifft nur Werte, die das
Frontend nie schickt).

**Risiken:**

- Die Prüfung muss **vor** jedem Schreiben stehen (Muster `BonusCapAcceptable`, `ChildrenController.cs:75`), sonst schreibt ein
  abgelehnter PATCH halb.
- Tests oder Seeds, die ein Kind mit Kombination anlegen, würden rot: per `grep` keiner gefunden (`SchoolType` in den Tests nur an
  Reihen und Profilen, die die Filter-Rolle tragen).

**Angriffsplan (Backend zuerst):**

1. Rote Tests, dann eine private Prüfung in `ChildrenController` für `Create` und `Update`; `<summary>` am DTO-Feld ergänzt die Regel
   (fließt in Swagger).
2. Frontend: keine Änderung (bereits Einfachauswahl).

**Testweg:**

- `backend/Pugling.Api.Tests/ChildLifecycleTests.cs`, neuer Fall `Schulart_Des_Kindes_Ist_Ein_Wert_Oder_Keine_Angabe` (AK 1–3). Rot,
  wenn die Prüfung in `Create` **oder** in `Update` fehlt (je eine Zusicherung pro Weg), und rot an der `1024`-Zeile, wenn nur
  `IsPow2` statt auch `Enum.IsDefined` geprüft wird.
- Zweiter Fall `Patch_Ohne_Schulart_Laesst_Eine_Alte_Kombination_Stehen` (AK 4): Kombination per `DbContext` setzen, PATCH nur mit
  `name`. Rot, wenn die Prüfung den gespeicherten statt den gesendeten Wert prüft.

## Verlauf

- **2026-08-11** — angelegt aus der Grill-Runde zu B-149 (Entscheidung 4). Bewusst nicht dort
  mitgenommen: B-149s Ziel ist ohne diese Frage erfüllt, und die Antwort ist eine Produktentscheidung,
  keine Aufräumarbeit.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, alle drei Verhaltensweisen sind da (`VaterLehrwerke.tsx:522`, `VaterFachlehrer.tsx:362`, `ExerciseEditModal.tsx:276-277`, `VaterKind.tsx:153-155,186-191`; Modell jetzt `AdminEntities.cs:75`).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-4 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): ein Kind trägt **keine Kombination** von Schularten. Der **Server validiert** (genau ein Wert oder `None`, sonst 400), und das Kind-Formular wird eine Einfachauswahl mit erreichbarem „keine Angabe“. Der Typ bleibt ein Flags-Enum (kein eigener Typ, keine Migration). Für die Filter-Rolle bleibt die Kombination erlaubt. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-17); trifft er auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an.
- **2026-11-13** — nachgeprüft (Tageslauf, Restzeit): das Kind verwirft jetzt ebenfalls still; zwei statt drei Antworten.
- **2026-11-13** — **ausformuliert** (Backlog-Lauf): Formular ist schon Einfachauswahl (`VaterKind.tsx:160,239`); offen ist nur die
  Server-Prüfung (`ChildrenController.cs:82,123`).
- **2026-11-13** — **gegrillt**: Entscheidung 1 ist die des Nutzers vom 2026-10-23; Entscheidungen 2 und 3 (zurückgestellt) sind
  Stakeholder-Entscheidungen im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — **geschaetzt**: XS, backend, keine Migration, kein Vertragsbruch.
- **2026-11-13** — `in-arbeit`: `ChildrenController.SchoolTypeAcceptable` (`None` oder genau ein definiertes Bit) vor jedem Schreiben in `Create` und `Update`, `400 validation_error`; geprüft wird nur der **gesendete** Wert (Entscheidung 2). Der `<summary>` an `CreateChildDto`/`UpdateChildDto` nennt die Regel. Tests `ChildLifecycleTests.Schulart_Des_Kindes_Ist_Ein_Wert_Oder_Keine_Angabe` (AK 1–3) und `Patch_Ohne_Schulart_Laesst_Eine_Alte_Kombination_Stehen` (AK 4; Absichtserklärung, im Ausgangszustand grün, rot unter B152-AK4). Rote Probe 5/5 rot.
  Rote Probe (Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:15, `mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B152-AK1-Create | backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs:89 | red | red |  |
  | B152-AK2-Update | backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs:130 | red | red |  |
  | B152-AK2-IsDefined | backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs:41 | red | red |  |
  | B152-AK3 | backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs:41 | red | red |  |
  | B152-AK4 | backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs:130 | red | red |  |

- **2026-11-13** — **abgenommen** mit Commit `f88464e8` (Sprint 70, Backlog-Lauf 2026-11-13). Verifikation: Backend 1569/1569 grün (dotnet test Pugling.sln -c Release), dotnet format für die geänderten Dateien sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `pugling-reviewer` ohne Befund (`IsPow2` + `IsDefined` bei POST und PATCH, alte Kombination bleibt, solange das Feld nicht gesendet wird). Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5213, Web 5186) nach dem letzten Edit – creator-lehrwerk-weg, einkleidung, fachlehrer-faecher-fallen-aus, kind-anlegen-id, kind-auswahlliste, kind-lehrbuch-fach, lehrwerk-bearbeiten, lehrwerke, reihe-fachwechsel-gesperrt, uebung-teilspeichern u. a.: **23 von 24 grün**. `lehrwerke` war zuerst rot, weil sie den Zustand vor B-445 zusicherte („Kein Profil passt“) – angepasst und einzeln grün nachgelaufen. `vater-von-null` bleibt rot: zwei „Speichern“-Knöpfe auf der Kind-Seite, eingeführt von `f9df2087` (B-533-Nachtrag, **vor** diesem Lauf), unabhängig von dieser Story; die parallele B-534-Arbeit baut genau diese Fläche um. Sohn ist Regressionszeuge (Suite grün).
