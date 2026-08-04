---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator]
aliases: [Unit-Stoffnotizen befüllen]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: memory/creator-profile-buchreihe.md
grund: ""
ersetzt_durch: []
---

# B-22 · Unit-Stoffnotizen LLM-gestützt befüllen

`SeriesUnit` trägt `Topics`/`Grammar`/`VocabularyNotes` — genau das, was einen KI-Creator materialkundig
macht statt ihn den Stoff erfinden zu lassen. Heute ist das reine Handarbeit im Vater-Web; niemand befüllt
die Felder maschinell, obwohl der KI-Creator sie längst liest.

## User Story

Als *Creator* (Fachlehrer-Konto, Besitzer einer Lehrwerk-Reihe) möchte ich die Stoffnotizen (`Topics`,
`Grammar`, `VocabularyNotes`) einer `SeriesUnit` aus einer Quelle, die ich selbst liefere (z. B. das
Inhaltsverzeichnis oder eine Kapitelzusammenfassung des Lehrwerks), per KI-Agent strukturiert entwerfen
lassen, damit ich nicht jede Unit von Hand abtippen muss — ohne dass das Modell Stoff erfindet, den das
Kind nie im Unterricht sieht.

## Ist-Stand am Code

- **Die Felder existieren und sind free text.** `backend/Pugling.Api/Models/CurriculumEntities.cs:51-69`
  (`SeriesUnit`): `Topics`/`Grammar`/`VocabularyNotes` sind alle `string?`, ohne eigene
  `[MaxLength]`-Annotation an der Klasse. Der XML-Kommentar (Zeile 47-49) nennt sie explizit „den
  eigentlichen Gewinn dieser Tabelle".
- **Die DB-Längenkonvention behandelt die drei Felder ungleich.** `backend/Pugling.Api/Data/PuglingDbContext.cs:1007-1008`
  listet die Suffixe, die 2000 Zeichen bekommen (`FreeTextSuffixes`: `Description, Notes, Text, Reason,
  Persona, Didactics, Comment, Message, Answer`). `VocabularyNotes` endet auf `Notes` → 2000 Zeichen.
  `Topics` und `Grammar` treffen **keinen** Suffix → fallen auf `DefaultLength = 200`
  (`PuglingDbContext.cs:1000,1042-1045`). Bestätigt im Snapshot
  `backend/Pugling.Api/Data/Migrations/PuglingDbContextModelSnapshot.cs:1838-1859`. Das ist vermutlich ein
  Versehen der Namenskonvention, kein bewusster Unterschied — 200 Zeichen reichen für „Grammatik der Unit"
  kaum über einen Halbsatz hinaus.
- **Handarbeit heute:** `backend/Pugling.Api/Controllers/Creator/SeriesUnitsController.cs` — Route
  `api/v1/creator/textbook-series/{seriesId}/units`, `[Authorize(Roles = Roles.Creator)]`. `POST`
  (Zeile 62-84) und `PATCH {unitId}` (Zeile 91-114) nehmen `CreateSeriesUnitDto`/`UpdateSeriesUnitDto`
  (`backend/Pugling.Contracts/Creator/TextbookSeriesDtos.cs:34-39`) — beide ohne Längen-Validierung, die
  Update-Semantik ist bereits „`null` = unverändert" (PATCH-Konvention, kein `Clear…`-Schalter nötig, weil
  ein leerer String schon über `Trimmed()`, Zeile 147, zu `null` normalisiert wird). Nur der `OwnerAdultId`
  der Reihe darf schreiben (Zeile 66-67, 97-98).
- **Frontend-Editor existiert bereits:** `frontend/src/vater/VaterLehrwerke.tsx` baut/sendet
  `CreateSeriesUnitDto`/`UpdateSeriesUnitDto` (Zeilen 9, 141, 213-251) — das ist die Hand-Eingabe, die B-22
  ergänzen, nicht ersetzen soll.
- **Der KI-Creator liest die Felder bereits — er befüllt sie nicht.**
  `backend/Pugling.Agent.Creator/Briefing/ProfileFacts.cs:59-65` (`ToPromptText()`) schreibt
  „Themen der Unit: …", „Grammatik der Unit: …", „Wortschatz der Unit: …" wortwörtlich in den Prompt, wenn
  eine Unit gesetzt ist. `BriefingBuilder.cs:90-118` (`ResolveMaterialAsync`) lädt Reihe/Unit über
  `creator.GetSeriesAsync`/`ListUnitsAsync` und prüft, dass die Unit zur Reihe gehört. Es gibt **keinen**
  Schreibpfad — keine Referenz auf `SeriesUnit`/`Topics`/`Grammar`/`VocabularyNotes` außerhalb dieses
  Lesepfads im ganzen `Pugling.Agent.Creator`-Projekt.
- **Der Schreib-Client existiert bereits, unbenutzt vom Agenten:**
  `backend/Pugling.Client/CreatorApi.cs:113-119` (`CreateUnitAsync`/`UpdateUnitAsync`) — kein neuer
  Vertrag nötig, ein Konsolen-Agent könnte heute schon per PATCH schreiben.
- **`CreatorProfile`-Matching ist eine andere Baustelle, kein Auswahlmechanismus für diese Story.**
  `backend/Pugling.Api/Services/Creator/CreatorProfileService.cs:14-38` (`MatchAsync`) bestimmt den
  Fachlehrer für ein *Kind* (Reihe 8 > Fach 4 > Klassenstufe 2 > Schulart 1, Zeilen 18-21), nicht, welche
  Unit befüllt wird. Die Unit für B-22 wird immer **direkt** über `seriesId`/`unitId` adressiert, wie es
  `SeriesUnitsController` heute schon tut.
- **Architektur-Leitplanke:** `backend/Pugling.Agent.Creator/CLAUDE.md` — „Deterministische Pipeline: C#
  besitzt den Ablauf … das Modell liefert nur strukturierten Inhalt – kein Tool-Calling", bestätigt in
  `backend/Pugling.Agent.Creator/README.md:46-47`. Root-`CLAUDE.md` verlangt dieselbe Trennung generell
  („kein LLM im Backend"). Verb-Dispatch existiert bereits als Muster:
  `backend/Pugling.Agent.Creator/AgentCommands.cs:25-40` (`types`, `profiles`, `briefing`, `create`, `exam`,
  `help`) — ein neuer Verb reiht sich mechanisch ein.
- **Testmuster existiert:** `backend/Pugling.Api.Tests/CreatorAgentTests.cs` +
  `backend/Pugling.Api.Tests/FakeChatClient.cs` — Pipeline-Tests ohne echtes Ollama.

## Die echte Lücke

Nicht „die Felder fehlen" (sie existieren, werden gelesen, sind schreibbar) — sondern: **es gibt keinen
Erzeuger.** Die Lücke ist ein neuer Verb im bestehenden KI-Creator-Agenten, der aus einer vom Menschen
gelieferten Quelle (nicht aus dem Nichts) `Topics`/`Grammar`/`VocabularyNotes` entwirft und über den längst
vorhandenen `UpdateUnitAsync`-Aufruf zurückschreibt — plus eine Korrektur der Spaltenlänge, die die Lücke
sonst beim ersten echten Versuch sofort wieder zunagelt (200 Zeichen für „Grammatik der Unit" reichen
nicht).

## Offene Punkte

1. ~~Woher kommt der Inhalt (Lehrwerk-Inhaltsverzeichnis? Nutzereingabe?) und wie wird verhindert, dass
   das Modell plausiblen, aber falschen Stoff einträgt?~~ → siehe Entscheidung 1.
2. ~~Wie fügt sich das in die Architektur (kein LLM im Backend) ein — neuer Agent oder Erweiterung des
   bestehenden KI-Creators?~~ → siehe Entscheidung 2.
3. ~~Welche Unit wird befüllt, und wie wird die Auswahl getroffen?~~ → siehe Entscheidung 3.
4. ~~Was passiert, wenn eine Unit schon (handgeschriebene) Notizen trägt — überschreiben, anhängen, oder
   verweigern?~~ → siehe Entscheidung 4.
5. ~~Die Spaltenlänge von `Topics`/`Grammar` (200 Zeichen) — im Rahmen dieser Story mitkorrigieren oder als
   eigener Defekt abspalten?~~ → siehe Entscheidung 5.
6. ~~Gibt es eine Vorschau/einen Trockenlauf, bevor geschrieben wird?~~ → siehe Entscheidung 6.

## Entscheidungen

1. **Die Quelle ist Pflicht, das Modell erfindet nichts.** Der neue Befehl verlangt einen vom Menschen
   gelieferten Quelltext (Datei oder Stdin — z. B. ein abgetipptes Inhaltsverzeichnis, eine
   Verlags-Kapitelübersicht), den das Modell **strukturiert und formuliert**, nicht **erfindet**. Das
   spiegelt die bestehende Kernregel des KI-Creators „Interessen kleiden den Stoff ein, sie ersetzen ihn
   nie" (`backend/Pugling.Agent.Creator/CLAUDE.md`) — hier: die Quelle *ist* der Stoff, das Modell liefert
   nur die Form. Ohne Quelltext bricht der Befehl mit einer Fehlermeldung ab, statt einen Titel wie
   „Access 8, Unit 3 – Growing up" allein zu Grammatik/Wortschatz auszuschmücken. **Kosten:** der
   Systemprompt braucht eine explizite Anti-Erfindungs-Anweisung („nutze ausschließlich die folgende
   Quelle, füge keine Fakten hinzu, die dort nicht stehen"); das ist Prompt-Text, kein Architektur-Aufwand.
2. **Erweiterung des bestehenden KI-Creator-Agenten, kein neuer Agent.** `Pugling.Agent.Creator` trägt
   bereits Ollama-Anbindung, HTTP-Client (`Pugling.Client`) und den Verb-Dispatch
   (`AgentCommands.cs:25-40`). Ein neuer Verb (Arbeitsname `units fill --series --unit --source-file`)
   reiht sich mechanisch ein, statt Infrastruktur zu duplizieren. **Kosten:** eine neue Verb-Route in
   `AgentCommands`, ein neuer Baustein analog `IExerciseStrategy` (aber ohne Regelprüfung/Anlegen/
   Selbsttest — hier gibt es keine spielbare Übung, nur drei Textfelder), plus Doku-Update in
   `backend/Pugling.Agent.Creator/README.md` und im Skill `ki-creator`.
3. **Die Unit wird direkt adressiert (`seriesId`+`unitId`), kein Matching.** Das `CreatorProfile`-Matching
   (`CreatorProfileService.MatchAsync`) beantwortet „welcher Fachlehrer passt zu diesem Kind" — eine
   andere Frage als „welche Unit wird jetzt befüllt". Letztere kennt der aufrufende Creator ohnehin (er
   hat die Reihe angelegt oder besitzt sie). **Kosten:** keine — nutzt den bestehenden
   `SeriesUnitsController`/`GetUnitAsync`-Pfad unverändert.
4. **Nur leere Felder werden befüllt, kein Überschreiben ohne `--force`.** Der Befehl liest die Unit vor
   dem Entwurf; ein Feld, das bereits einen Wert trägt, bleibt unangetastet, es sei denn `--force` ist
   gesetzt. Grund: handgeschriebene Notizen sind Vertrauensgrundlage (siehe Ideen-Text) — ein stiller
   Überschreib-Pfad wäre ein Datenverlust-Risiko genau an der Stelle, die Vertrauen tragen soll.
   **Kosten:** ein Feldweiser Vergleich vor dem `UpdateUnitAsync`-Aufruf (drei `if`s), keine
   Schema-Änderung.
5. **Die Spaltenlänge wird in dieser Story mitkorrigiert.** `Topics`/`Grammar` bekommen ein explizites
   `HasMaxLength(2000)` in `PuglingDbContext.OnModelCreating` (oder eine Erweiterung der
   `FreeTextSuffixes`-Liste, falls das Namensmuster projektweit gelten soll — zu prüfen beim Bauen, ob ein
   anderes `Topics`/`Grammar`-Feld existiert, das *nicht* mitwachsen soll). Ohne diese Korrektur wäre das
   Feature beim ersten echten Testlauf sofort kaputt: ein LLM-Entwurf für „Grammatik der Unit" sprengt 200
   Zeichen fast immer. **Kosten:** eine neu gefaltete Migrationskette (`migration: ja`, Kettenlänge bleibt
   1 laut Projekt-Regel); kein Vertragsbruch, da `Pugling.Contracts` keine Längen-Validierung trägt
   (bestätigt: `TextbookSeriesDtos.cs:34-39` ohne `[MaxLength]`/`[StringLength]`).
6. **`--dry-run` gibt es, wie beim bestehenden `create`-Befehl.** Der Entwurf wird angezeigt, bevor
   geschrieben wird — dasselbe Muster wie die vorhandene Pipeline (Konsole zeigt, Mensch entscheidet). Ein
   automatisierter Selbsttest wie `preview/check` bei Übungen entfällt bewusst: drei Textfelder sind nicht
   spielbar, also nicht automatisch verifizierbar — die Prüfung ist zwangsläufig das menschliche Lesen.
   **Kosten:** keine neue Infrastruktur, nur ein Flag analog zum bestehenden `--dry-run`.

## Akzeptanzkriterien

1. Ein neuer Befehl im KI-Creator-Agenten (`units fill` o. ä.) nimmt `--series`, `--unit` und eine
   Quelltext-Datei (oder Stdin) entgegen und bricht **ohne** Quelltext mit einer verständlichen
   Fehlermeldung ab (Exit-Code 2, wie andere Fehlbedienungen).
2. Der erzeugte Entwurf für `Topics`/`Grammar`/`VocabularyNotes` enthält nachweislich nur Inhalte, die im
   Quelltext angelegt sind (Systemprompt-Anweisung + Stichprobe im Test mit `FakeChatClient`).
3. Bereits befüllte Felder der Ziel-Unit bleiben ohne `--force` unverändert; mit `--force` werden sie
   überschrieben.
4. `--dry-run` zeigt den Entwurf, ohne die Unit zu ändern (kein `PATCH`-Aufruf).
5. Ohne `--dry-run` und ohne Konflikt schreibt der Befehl über den bestehenden
   `CreatorApi.UpdateUnitAsync`-Aufruf zurück; ein `GET` derselben Unit zeigt die neuen Werte.
6. `Topics` und `Grammar` akzeptieren in der DB Texte bis 2000 Zeichen (wie `VocabularyNotes` heute schon);
   ein Integrationstest legt eine Unit mit >200 Zeichen in `Topics`/`Grammar` an und liest sie unverändert
   zurück.
7. `SchemaGuardTests` bleibt grün (Kettenlänge weiterhin 1, kein Modell-Drift).
8. Das README des KI-Creators (`backend/Pugling.Agent.Creator/README.md`) und der Skill `ki-creator`
   nennen den neuen Befehl.

## Schätzung

**Größe: M** — vergleichbarer Umfang wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03): ein
neuer, in sich abgeschlossener Pfad (neuer Verb + neue Prompt-Logik + Schreib-zurück), der bestehende
Bausteine wiederverwendet (Client, Verb-Dispatch, `FakeChatClient`-Testmuster), aber echte Entwurfsarbeit
am Prompt (Anti-Erfindungs-Anweisung) und an der Konfliktlogik (nur leere Felder, `--force`) verlangt.
Nicht S, weil mehrere Teile zusammenspielen (Schema-Fold + neuer Agent-Befehl + Prompt-Sicherheit +
Konfliktbehandlung + Tests); nicht L, weil kein mehrstufiger DB-Umbau und kein Frontend nötig sind.

- **`wo`: backend** — `backend/Pugling.Api` (Schema) und `backend/Pugling.Agent.Creator` (neuer Befehl);
  keine Vater-Web-Änderung nötig, `VaterLehrwerke.tsx` bleibt der Hand-Eingabe-Pfad und braucht keinen
  neuen UI-Zustand.
- **`migration`: ja** — `HasMaxLength`-Änderung an `Topics`/`Grammar` ist eine Modelländerung; Kette wird
  neu gefaltet (`rm -rf backend/Pugling.Api/Data/Migrations` + `migrations add InitialCreate`), Länge
  bleibt 1.
- **`vertragsbruch`: nein** — `CreateSeriesUnitDto`/`UpdateSeriesUnitDto`/`SeriesUnitResponse` ändern sich
  nicht in Form oder Feldern; die Längengrenze ist reine DB-Konvention ohne Contracts-Validierung.
- **Risiken:**
  - Ollama-Halluzination trotz Anti-Erfindungs-Prompt bleibt ein Restrisiko (kein Tool-Calling, keine
    Grounding-Prüfung außer Prompt-Disziplin) — dafür ist `--dry-run` die Gegenmaßnahme, nicht ein
    technischer Filter.
  - Falls `FreeTextSuffixes` statt einer expliziten `HasMaxLength`-Annotation erweitert wird, muss beim
    Bauen geprüft werden, ob ein anderes Feld `…Topics`/`…Grammar` heißt und *nicht* mitwachsen soll
    (aktuell nicht gefunden, aber die Konvention gilt projektweit).
- **Angriffsplan** (Backend zuerst):
  1. `Topics`/`Grammar` auf 2000 Zeichen heben, Migrationskette neu falten, `SchemaGuardTests` grün halten.
  2. Neuen Verb im KI-Creator-Agenten bauen: Quelltext einlesen → Prompt mit Anti-Erfindungs-Anweisung →
     Entwurf → Konfliktprüfung (leer vs. `--force`) → `--dry-run`-Ausgabe oder `UpdateUnitAsync`.
  3. Tests mit `FakeChatClient` (Muster `CreatorAgentTests.cs`), danach README/Skill-Doku nachziehen.
- **Testweg:** neuer Testfall in `backend/Pugling.Api.Tests/CreatorAgentTests.cs` mit `FakeChatClient`
  (Entwurf, Konfliktfall, `--dry-run`); ein Integrationstest für die verlängerte Spalte (Unit mit
  >200-Zeichen-`Topics` anlegen und zurücklesen); `/smoke-test` deckt den Agenten nicht ab (Konsolen-App,
  kein API-Endpunkt) — manueller Lauf gegen die laufende API genügt vor der Abnahme.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (`SeriesUnit`-Felder, Längenkonvention,
  KI-Creator-Lesepfad ohne Schreibpfad, bestehender `UpdateUnitAsync`-Client, Verb-Dispatch-Muster);
  „echte Lücke" auf den fehlenden Erzeuger plus die zu enge Spaltenlänge zugespitzt.
- **2026-08-03** — gegrillt: sechs offene Punkte autonom entschieden (kein Mensch-Dialog geführt) —
  autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe M, `wo: backend`, `migration: ja`, `vertragsbruch: nein`, Angriffsplan
  und Testweg festgelegt — autonom getroffen, Nutzerauftrag 2026-08-04.
