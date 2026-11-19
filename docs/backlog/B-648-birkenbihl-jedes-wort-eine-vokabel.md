---
tags: [typ/story, status/geschaetzt]
aliases: []
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-646
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-648 · Birkenbihl: jedes Wort eines Textes hat eine Vokabel

Teil (b) aus der Teilung von [B-646](B-646-vokabel-schema-nachfuellen.md). Ist-Stand (Dekodierung nur über die
exakte Oberfläche, CRUD-Anlage verankert keine Glossen, Übung #3 mit 0 von 8 verknüpft) und die Begründungen
stehen dort. **Baut auf [B-647](B-647-vokabeln-vollstaendig-ladeweg-und-bestand.md) auf:** Ohne strukturierte
Artikel- und Verbangaben findet die Suche keine Grundform.

## User Story

Als **Creator** möchte ich, dass jedes Wort eines Birkenbihl-Textes mit einer Vokabel verknüpft ist, und zwar
auch bei gebeugten Formen und Wörtern, die im Speicher mit Artikel oder `to` stehen. **Damit** kann die
Birkenbihl-Methode ausgebaut werden und auf den Vokabeln aufbauen.

## Ist-Stand am Code

Ausführlich in B-646. Das Wichtigste:

- Die Dekodierung sucht nur die exakte Oberfläche, ohne Grundform und ohne Artikel/`to`
  (`backend/Pugling.Api/Services/Creator/BirkenbihlDecodingService.cs:48-62`).
- `POST …/sentences` speichert ein nicht gefundenes Wort ohne Glosse und ohne Vokabel
  (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:551-562`).
- Anlegen/Ersetzen der ganzen Übung normalisiert nur die Ids (`ExerciseControllers.cs:500-512`). Die
  Übersetzungsübung verankert dagegen jedes Paar (`ExerciseControllers.cs:437-445`).
- Nur der Einzeltausch mit freier Glosse legt eine Vokabel an (`ExerciseControllers.cs:613-619`).
- DB-Kopie vom 2026-11-17: Übung #3 hat 8 Wörter, davon 0 verknüpft und 1 im Speicher auffindbar.

## Entscheidungen

Aus B-646 übernommen (vom Vater im Dialog getroffen): **1** (die Suche lässt Artikel und `to` beim Vergleichen
weg), **4** (anlegen statt abweisen, Übung ohne Glosse wird im Editor als unvollständig angezeigt und bleibt
spielbar), **5** (gebeugte Formen bei Bedarf, die Grundform schlägt die Suche vor, der Creator bestätigt).

_Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt (gilt für 10 bis 12)._

- Entscheidung 10: **Die Grundform-Erkennung ist eine kleine Liste unregelmäßiger Formen je Sprache (en, fr) plus wenige
    Endungsregeln für Englisch (`-s`, `-es`, `-ed`, `-ing`). Vorgeschlagen wird nur eine Grundform, die im
    Speicher schon steht.** Latein bekommt keine Regel. Begründung: Das Backend hat bewusst kein LLM, und
    Entscheidung 5 nennt „Regel je Sprache oder Liste“ selbst; ein Vorschlag, der nicht im Speicher liegt, wäre
    ein Wort ohne Vokabel und löste die User Story nicht. Verworfen: ein LLM-Aufruf im Backend. Kosten: falsche
    oder fehlende Vorschläge (die Liste ist nie vollständig), und lateinische Texte bleiben beim exakten Treffer.
    Der Fall, den das ausschließt: eine Form, deren Grundform fehlt, bekommt keinen Vorschlag; sie wird erst über
    die Glosse verankert (Nr. 11) und hat keine `BaseFormId`. Gegen die User Story gerechnet: „jedes Wort eine
    Vokabel“ bleibt erfüllt, nur die Grundform-Kette fehlt.
- Entscheidung 11: **`POST …/sentences` bleibt unverändert; verankert wird beim Setzen der Glosse (`PUT …/words/{wordId}`,
    schon heute) und beim Anlegen/Ersetzen der ganzen Übung.** Begründung: Die Eingabe des Satzes trägt keine
    Glosse (`BirkenbihlSentenceInput`), es gibt dort also nichts zu verankern; das alte Akzeptanzkriterium hätte
    nichts getestet. Verworfen: optionale Glossen im Satz-Input. Kosten: ein neuer Satz mit unbekannten Wörtern
    bleibt bis zum Setzen der Glossen unvollständig (sichtbar, siehe 4). Ausgeschlossener Fall: ein Satz, dessen
    unbekannte Wörter gleich beim Hinzufügen verankert werden; dafür bräuchte der Vertrag ein neues Eingabefeld.
- Entscheidung 12: **„Unvollständig“ berechnet das Frontend aus der Config (Wort ohne Glosse), nicht der Server.** Begründung:
    Alle Daten stehen schon in der Antwort, ein Server-Feld wäre eine zweite Wahrheit und ein Vertragsfeld mehr.
    Verworfen: ein `incomplete`-Flag in `BirkenbihlConfig`. Kosten: Andere Clients (der KI-Creator) rechnen die
    Regel selbst nach. Ausgeschlossener Fall: ein Server, der eine unvollständige Übung abweist; Entscheidung 4
    verbietet das ohnehin.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. **Faltung.** Die Dekodierung findet `ville` → `la ville` und `live` → `(to) live`; ein exakter Treffer geht
   vor einem gefalteten. Für `is` liefert sie keinen Treffer, aber einen Grundform-Vorschlag auf den Eintrag von
   `be`. Ein Artikel allein (`la`) trifft nicht `la ville`. Tests in `BirkenbihlDecodingServiceTests` (Dienst)
   und `BirkenbihlExerciseTests` (Endpunkt). Rot macht sie: den Faltungsschritt wieder entfernen (`ville` ohne
   Treffer) bzw. die Faltung auf die Eingabeseite legen (`la` träfe `la ville`).
2. **Verankern beim Anlegen und Ersetzen.** Ein Wort mit Glosse und ohne `vocabularyId` trägt nach dem Anlegen
   und nach dem Ersetzen der ganzen Übung eine `vocabularyId`; im Speicher steht ein Eintrag mit dem Wort, der
   Glosse und dem Sprachpaar der Übung. Ein zweites Ersetzen legt keinen zweiten Eintrag an, ein Wort ohne
   Glosse bleibt ohne Id und ohne Eintrag. Rot macht sie: `NormalizeConfigAsync` im Controller entfernen (Id
   bleibt `null`) bzw. `GetOrCreate` durch ein blindes Anlegen ersetzen (zweiter Eintrag).
3. **Grundform bestätigen.** `PUT …/words/{wordId}` mit dem neuen, additiven Feld (Id der Grundform plus
   optional Zeit, Person, Zahl) legt die gebeugte Form als eigenen Eintrag mit `BaseFormId`,
   `Verb.IsBaseForm = false` und den übergebenen Angaben an und verknüpft das Wort. Ein zweites Bestätigen
   derselben Form legt nichts doppelt an; eine Grundform aus einem anderen Sprachpaar gibt `invalid_reference`.
   Rot macht sie: das Feld im Vertrag (heute `400 unknown_field`), `BaseFormId` weglassen, die
   Sprachpaar-Prüfung weglassen.
4. **Sichtbar unvollständig.** Ein Wort ohne Glosse trägt im Übungseditor eine Marke „unvollständig“, die mit
   der Glosse verschwindet. Komponententest in `BirkenbihlDecoding.test.tsx`, E2E-Spec
   `frontend/e2e/birkenbihl-unvollstaendig.spec.ts` (Satz mit unbekanntem Wort, Marke da, Glosse setzen, Marke
   weg). Rot macht sie: die Marke entfernen.
5. **Spielbar bleibt es (Absichtserklärung, kein Test des Ausgangszustands).** Eine Übung mit Wort ohne Glosse
   bleibt dem Kind spielbar. Das bestehende `PositionsTest_ZeigtDieselbeDekodierungWieDieUebungskarte` und
   `AddSentence_VerlinktBekannteWoerter_LaesstUnbekannteLeer` bleiben unverändert grün; sie fallen, sobald
   jemand die Pflicht hart macht (Entscheidung 4).
6. **Bestand.** Übung #3 „Getting to know each other“ ist nach einem Lauf auf einer Kopie der echten DB
   vollständig verknüpft (8 von 8 Wörtern mit `vocabularyId`, vorher 0 von 8), `ItemProgress` und
   `PositionItemProgress` sind zeilengleich. Beleg ist die SQL-Zählung vor und nach dem Lauf, kein Test.

## Schätzung

**Größe: L.** Anker ist E6 (ein Eingriff am Code **und** ein Datenlauf gegen die echte DB), wie bei B-647. Es
kommen zusammen: eine Suche mit Faltung und Grundform-Regeln, ein neuer Bestätigungsweg am Wort-Endpunkt
(Backend), das Verankern beim Anlegen und Ersetzen, eine Marke und ein Bestätigungsdialog im Editor (Frontend)
und der Lauf auf Kopie und echter DB. Jeder Teil für sich wäre S bis M. Nicht XL: alle Teile hängen an einer
Aussage („jedes Wort hat eine Vokabel“) und werden zusammen abgenommen.

- **`migration: nein`.** Die Birkenbihl-Config ist JSON in `ConfigJson`; `WordPair` ändert sich nicht. Die neuen
  Einträge entstehen als `Vocabulary`-Zeilen im bestehenden Schema (`BaseFormId` und `Verb` gibt es schon,
  `backend/Pugling.Api/Models/VocabEntities.cs:41-56`). `SchemaGuardTests` bestätigt das beim Bauen.
- **`vertragsbruch: nein`.** Additiv: ein Feld in `WordOverride` und ein Vorschlagsfeld in `DecodedWord`
  (`backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:118`, `:135`). `Pugling.Client` bekommt die
  neuen Felder über `OverrideBirkenbihlWordAsync` (`backend/Pugling.Client/CreatorApi.cs:280`).
  `ContractDocumentTests` zieht `docs/openapi/v1.json` nach.
- **`wo: beides`.** Backend zuerst (Suche, Verankern, Bestätigungsweg), dann Editor-Marke und Dialog
  (`frontend/src/components/BirkenbihlDecoding.tsx`, `frontend/src/vater/exerciseConfig.tsx`).

### Risiken

1. **Faltung vergrößert die Trefferliste.** `live` trifft jetzt `(to) live` und eine Adjektiv-Vokabel `live`,
   also ein Homonym mehr, das der Creator auflöst. Gegenmaßnahme: ein exakter Treffer geht vor, die
   Kandidatenliste folgt der bestehenden Regel (nur bei mehr als einem Treffer, `ExerciseControllers.cs:526`).
2. **SQL kann nicht falten.** Die Abfrage vergleicht `v.Word.ToLower()` (`BirkenbihlDecodingService.cs:51`).
   Weg: je Oberfläche die möglichen gespeicherten Schreibweisen (`la x`, `l'x`, `(to) x`, `to x`) erzeugen und
   mit `Contains` abfragen, eine Abfrage, kein N+1. Die Artikelliste liegt in `StageMechanics`
   (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:62-67`), kennt aber `to` nicht.
3. **Neue Einträge sind `Other`.** Das Verankern legt Einträge über `GetOrCreateAsync` mit
   `PartOfSpeech = Other` an (`VocabularyStoreService.cs:37-46`). Damit zeigt B-647s Filter `?incomplete=true`
   nach jedem Birkenbihl-Lauf wieder Treffer. Abstimmung mit B-647 nötig; bis dahin gilt die Zahl 0 nur zum
   Zeitpunkt des Nachfülllaufs.
4. **Bestätigte Formen sind unvollständig, wenn Zeit/Person/Zahl fehlen.** B-647 Entscheidung 2 verlangt sie
   bei gebeugten Formen. Der Bestätigungsdialog muss sie also abfragen, sonst entsteht ein Eintrag, den der
   Filter sofort wieder meldet. Im Vertrag sind sie optional, der Editor fragt sie ab.
5. **Reihenfolge.** B-648 baut auf B-647 auf (Verbangaben, Genus, Artikelstruktur). Der Vorschlag auf `be`
   findet nur Einträge, die im Speicher als Grundform stehen (`(to) be`); ohne B-647 steht dort `Other` und
   kein `Verb.IsBaseForm`. Die Faltung selbst braucht B-647 nicht.
6. **Datenlauf.** Ersetzen der Übung #3 ändert nur `vocabularyId` je Wort (und legt Einträge an); erst auf
   einer Kopie (`scripts/heimnetz.ps1 -Database <kopie> -Port <anderer>`), dann mit Freigabe gegen die echte DB.

### Angriffsplan

1. **Backend, Dienst:** `BirkenbihlDecodingService.LookupAsync` fragt zusätzlich die gefalteten Schreibweisen
   ab, ein exakter Treffer geht vor. Dazu eine kleine Klasse für Grundform-Vorschläge (Liste und Endungsregeln,
   Nr. 10), die nur gegen Einträge des Speichers vorschlägt. `TokenLookup` bekommt den Vorschlag.
2. **Vertrag:** `DecodedWord` bekommt ein optionales Vorschlagsfeld (Wort, Id der Grundform), `WordOverride` das
   Bestätigungsfeld (Id der Grundform, Zeit, Person, Zahl); beide mit `/// <summary>`, englisch.
3. **Backend, Controller:** `BirkenbihlController` überschreibt `NormalizeConfigAsync` und verankert Wörter mit
   Glosse ohne Id über `GetOrCreateAsync` (Vorbild `TranslationController`, `ExerciseControllers.cs:437-446`).
   `SetWord` bekommt den Bestätigungszweig: Form als eigener Eintrag mit `BaseFormId` und `Verb`-Angaben,
   Sprachpaar-Prüfung wie im Zweig `VocabularyId` (`:597-606`).
4. **Client:** neue Felder an `OverrideBirkenbihlWordAsync`; `dotnet build Pugling.sln`.
5. **Frontend:** Marke „unvollständig“ je Wort ohne Glosse, Bestätigungsdialog für den Vorschlag; Typen in
   `frontend/src/lib/contract.ts` nachziehen.
6. **Datenlauf** auf der Kopie (SQL-Zählung vorher/nachher), dann Freigabe des Vaters, dann die echte DB.
7. `dotnet build Pugling.sln` und `dotnet format Pugling.sln` vor dem Commit.

### Testweg

- `BirkenbihlDecodingServiceTests`: Faltung, Vorrang des exakten Treffers, `la` ohne Treffer, Vorschlag `is` →
  `be` (AK 1). Aufbau wie `Lookup_WortCaseInsensitiv_AberSprachcodeExakt`.
- `BirkenbihlExerciseTests`: Anlegen und Ersetzen verankern, zweites Ersetzen ohne Dublette, Wort ohne Glosse
  bleibt leer (AK 2); Bestätigen, doppeltes Bestätigen, fremdes Sprachpaar (AK 3); der bestehende Fall
  `AddSentence_VerlinktBekannteWoerter_LaesstUnbekannteLeer` bleibt grün (AK 5).
- `ContractDocumentTests` (OpenAPI nachziehen), `SchemaGuardTests` (keine Migration), `EndpointCoverageGuard`
  (keine neue Action, nur Felder).
- Frontend: `BirkenbihlDecoding.test.tsx` (Marke) und die neue E2E `frontend/e2e/birkenbihl-unvollstaendig.spec.ts`.
- Datenlauf: Zählung auf der Kopie (AK 6), danach der Rollengang als Sohn an Übung #3.
- Vor der Abnahme: Agenten `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- **2026-11-17** — aus B-646 geteilt (Entscheidung 8), Stufe `gegrillt` mit den Entscheidungen 1, 4, 5 aus B-646.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): L, beides, keine Migration, kein Vertragsbruch
  (additive Felder). Akzeptanzkriterien umgeschrieben, je Kriterium die rote Änderung; das alte Kriterium zu
  `POST …/sentences` prüfte nichts, weil der Satz-Input keine Glosse trägt. Entscheidungen 10 bis 12
  stellvertretend, unbestätigt (`wartet_auf`).
