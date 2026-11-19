---
tags: [typ/story, status/ausformuliert, bereich/auth, bereich/shop, rolle/supervisor, rolle/creator]
aliases: [Rollenschnitt Vater/Lehrer, Vater nur Belohnung]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/leitbild-privatlehrer.md
grund: ""
ersetzt_durch: []
---

# B-681 · Rollenschnitt: der Vater belohnt, der Lehrer(-Agent) unterrichtet

Heute ist der Vater Creator **und** Supervisor: Pläne, Positionen, Ziele und Klassenarbeiten liegen unter
`supervisor/…` – zusammen mit dem Familien-Shop. Das Leitbild vom 2026-11-19 trennt das: Der **Vater** kümmert sich
nur noch um die Belohnung (Shop füllen, gekaufte und aktivierte Artikel einlösen), der **Lehrer** – im Regelfall der
KI-Agent, B-680 – analysiert den Lernstand und steuert Übungen und Pläne. Der Supervisor zerfällt damit in zwei
Zuständigkeiten, was Routen und Rollen-Gates trifft.

**Vorbehalt:** Die Story hängt am Richtungswechsel „Leitbild Privatlehrer“ (2026-11-19), den der Nutzer entscheidet. Sie
wurde im Vorbereitungslauf nur **ausformuliert**; jede Frage unter „Offene Punkte“ steht mit Empfehlung da und ist
**nicht entschieden**. Es gibt bewusst keinen Abschnitt `## Entscheidungen`.

## User Story

Als *Vater* möchte ich die Belohnung (Shop füllen, einlösen) verwalten, ohne die Plan-Steuerung selbst tragen zu müssen,
damit ich nicht zum Lehrer meiner Kinder werde. Als *Lehrer(-Agent)* möchte ich Pläne, Positionen und Klausuren eines
Kindes steuern dürfen, ohne seine Familien-Belohnung oder sein Wallet zu sehen, damit die Rollen sauber getrennt sind
und der Vater die Münzen nicht aus der Hand gibt.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main` (Stand `47791819`). Pfade relativ zu `backend/Pugling.Api/`.

### Rollen und Gates: drei Ebenen, aber Plan-Steuerung und Shop sind **eine** Rolle

- Die Rollen heißen `Creator`, `Supervisor`, `Student` (`Auth/AuthAccess.cs:16`, `:18`, `:20`); dazu `AnyAdult` =
  „Creator oder Supervisor“ (`:34`, im Controller-Bestand nur einmal verwendet: `Controllers/Creator/TagsController.cs:347`).
  Eine **Teacher**-Rolle gibt es nicht (nicht gefunden).
- Ein `Account` trägt über `AccountProfile` mehrere Rollen (`Models/IdentityEntities.cs:29`, `:37`, `:42`); das
  **Lehrer-Konto** ist ein Konto nur mit Creator-Profil (`Controllers/Creator/TeacherAccountsController.cs:12`, `:30`) –
  dessen Token fehlt der Supervisor-Claim, daher weisen alle `[Authorize(Roles = Roles.Supervisor)]`-Endpunkte es ab.
- **Plan-Steuerung und Belohnung hängen an derselben Rolle `Supervisor`:** Pläne (`Controllers/Supervisor/StudyPlansController.cs:19`,
  Schreibpfade `:85`, `:172`, `:243`), Positionen (`PlanPositionsController.cs:23`), Klassenarbeiten
  (`KlassenarbeitenController.cs:92`, `:130`, `:172`, `:188`), Ziele (`ObjectivesController.cs`, `KeyResultsController.cs`) und
  der Familien-Shop (`ShopController.cs:17`, `:20`) – insgesamt 33 `Roles.Supervisor`-Stellen unter `Controllers/`
  (`grep`, 2026-11-19), davon 15 Endpunkte allein im Shop-Controller.
- **Eigentum läuft über `SupervisorLink`, nicht über die Rolle:** `AuthAccess.SupervisorOwnsChildAsync`
  (`Auth/AuthAccess.cs:112`) und `OwnsPlanAsync` (`:103`) prüfen eine Zeile in `SupervisorLinks`. Ein Lehrer(-Agent), der Pläne
  eines Kindes steuern soll, bräuchte also entweder eine solche Verknüpfung (und würde dann auch Shop und Wallet sehen)
  oder eine zweite Art von Zuordnung. Nicht gefunden: irgendeine Zuordnung „Lehrer ⇢ Kind“.
- Der Shop-Eigentümer ist der **ausstellende** Supervisor (`SupervisorId`-Momentaufnahme der Einlösung, siehe
  `Controllers/Supervisor/ShopController.cs:432` für die Einlösung aus dem Inventar); Entitäten in
  `Models/GamificationEntities.cs:163` (`ShopArticle`), `:187` (`ShopListing`), `:303` (`ShopPurchase`), `:334` (`ActivationRequest`).

### Wer die Pläne heute tatsächlich anlegt

- Der KI-Creator läuft als Konsolen-App **mit dem Konto des Vaters** (`Pugling:AccountId`/`Pugling:Pin`,
  `Pugling.Agent.Creator/AgentCommands.cs:268`) und legt Pläne **inaktiv** an (`PlanPlanner.cs:101`); die Freigabe liegt beim
  Vater. Der Agent braucht dafür heute die Supervisor-Rolle – es gibt keinen „Agenten-Login“ mit eigener Rolle.

### Oberfläche

- Das Vater-Web ordnet Bereiche in drei Perspektiven Betreuen/Zuweisen/Erstellen (`frontend/src/vater/navigation.ts:57`,
  `:72`); ein Lehrer-Konto sieht dort nur die Erstellen-Perspektive. Eine Perspektive „nur Belohnung“ gibt es nicht
  (nicht gefunden).

### Pausieren

- Eine **Notbremse** (Plan pausieren) existiert nicht: `StudyPlan` kennt nur `Active` (`Models/StudyPlanEntities.cs:90`); die
  Suche nach `Pause` unter `Models/`, `Controllers/` und `Services/` findet nichts. B-644 plant Pausen als Regel.

## Die echte Lücke

Die Lücke ist schmaler als „neue Rolle bauen“: Die **Gates** sind bereits die richtige Taxonomie (`supervisor/…` = Steuerung).
Was fehlt, ist:

1. Eine **Rollenzuordnung unterhalb von „Supervisor“**: heute gibt es nur „alles oder nichts“ – wer Pläne steuern darf,
   darf auch den Shop und das Wallet sehen, und ein Konto ohne Supervisor-Claim darf keinen Plan anfassen.
2. Eine **Zuordnung Lehrer ⇢ Kind** (Eigentum), die nicht `SupervisorLink` ist.
3. Ein Weg für den **Agenten**, unter einer eigenen Identität zu laufen statt unter dem Vater-Konto.

Nicht die Lücke: das Datenmodell der Pläne selbst – das bleibt am Kind.

## Offene Punkte

Jede Frage mit Empfehlung. **Nichts davon ist entschieden** (Rollenschnitt: Nutzer entscheidet).

1. **Neue Rolle `Teacher` oder bestehendes Lehrer-Konto erweitern?** Empfehlung: **keine neue Rolle**, sondern die
   Plan-Endpunkte unter `supervisor/…` für `Creator` mit Lehrer-Zuordnung öffnen (Gate `AnyAdult` plus Eigentumsprüfung).
   Begründung: Das Lehrer-Konto ist schon Creator-only (`TeacherAccountsController.cs:12`), und eine vierte Rolle
   verbreitert den Schnitt von API, Code und Token, den CLAUDE.md als Taxonomie führt. Alternative: Rolle `Teacher`
   – sauberer im Token, kostet einen Claim und Gate-Umbau an ca. 33 Stellen.
2. **Wie bekommt ein Lehrer Zugriff auf *ein* Kind?** Empfehlung: eine zweite Verknüpfung (z. B. `TeacherLink`)
   statt `SupervisorLink` wiederzuverwenden – sonst sähe der Lehrer den Familien-Shop. Entscheidend für B-680 (Wer ist
   „der Lehrer“, wenn es der Agent ist?). Das ist ein Schema-Eingriff (Migration an die Kette).
3. **Läuft der Agent unter dem Vater-Konto weiter?** Empfehlung: für den Übergang ja (heute so, `AgentCommands.cs:268`);
   Ziel ist ein eigenes Agenten-Konto mit Creator- und Lehrer-Zuordnung, damit der Vater-PIN nicht in der Agenten-
   Konfiguration steht.
4. **Darf der Vater eingreifen?** Empfehlung: ja – Notbremse behalten (Plan pausieren, eine Aufgabe streichen), keine
   Mengen setzen. Das ist dieselbe Frage wie B-680 Punkt 3 und B-644 Entscheidungen 2, 4, 7, 8; dort nicht doppelt
   entscheiden.
5. **Wer setzt Klausurtermin und Note?** Empfehlung: Termin und Note bleiben beim **Vater** (er hat den Schulkontakt:
   WebUntis, Heft, Klausur) und werden dem Lehrer(-Agenten) gelesen zur Verfügung gestellt; B-684 (Scan) klärt den
   Eintrag. Ein Agent trägt keine Note ein, die Pflichten und Klausur-Prämien beeinflusst.
6. **Was passiert mit den laufenden Plänen in der echten `pugling.db`?** Empfehlung: **nichts** – Pläne hängen am Kind,
   nicht am Steuernden; der Schnitt ist ein Zugriffsumbau, kein Datenumbau. Nur die neue Zuordnung (Punkt 2) kommt als
   anhängende Migration (CLAUDE.md: Kette wird nur verlängert).
7. **Frontend:** Empfehlung: Perspektiven bleiben; „Zuweisen“ wird für den Vater zur Leseansicht, sobald der Planer
   (B-680/B-644) läuft. Nicht vorwegnehmen.

## Akzeptanzkriterien (Entwurf)

Final erst nach Entscheidung der Punkte 1 und 2. Je Kriterium die Änderung, die es rot macht:

1. Ein Konto mit Lehrer-Zuordnung zu Kind A darf einen Plan für A lesen und ändern und bekommt für B `403/404`. *Rot,
   wenn* das Gate weiterhin nur `Roles.Supervisor` verlangt (Lehrer bekommt `403` für A) oder die Eigentumsprüfung fehlt
   (Lehrer darf B).
2. Dasselbe Konto bekommt für **jede** Shop-Route des Kindes `403`. *Rot, wenn* die Lehrer-Zuordnung versehentlich
   `SupervisorLink` erzeugt und damit den Shop öffnet.
3. Ein Vater ohne Lehrer-Zuordnung kann Shop und Einlösung wie bisher bedienen. *Rot, wenn* der Umbau die Shop-Gates
   an die neue Zuordnung bindet.
4. Das Setzen einer Pflicht (`GoalThreshold`, `PenaltyCoins`) durch den Lehrer-Zugang ist nach denselben Regeln begrenzt
   wie durch den Vater (Boden/Deckel aus B-644). *Rot, wenn* ein Lehrer-Konto Strafmünzen über dem Deckel setzen kann.
5. Eine Migration lässt vorhandene Pläne, Positionen und Fortschritte der echten Datenbank unverändert (Test gegen eine
   Kopie mit Altzeilen). *Rot, wenn* die Migration Zeilen löscht oder neu nummeriert.
6. Der Vater kann einen Plan pausieren, der Lehrer(-Agent) nicht entpausieren, was der Vater pausierte (Frage 4, falls
   so entschieden). *Rot, wenn* beide dasselbe Feld überschreiben.

## Beziehungen zu anderen Stories (nur gelesen)

- [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md) und [B-644](B-644-tagesplaner-statt-fester-lauf.md): Wer die
  Plan-Steuerung trägt, ist deren Voraussetzung (B-680 Punkt 3).
- [B-684](B-684-korrigierte-klausur-einscannen.md): wer die Note einträgt (Punkt 5).
- [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): Vater nimmt Hausaufgaben ab – passt zu „Vater = Belohnung/Pflicht
  durchsetzen“.

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Die Gates folgen
  schon der richtigen Taxonomie; es fehlt eine Zuordnung Lehrer ⇢ Kind und ein Agenten-Konto, keine neue Rolle
  zwingend. Sieben offene Punkte mit Empfehlung, **nichts entschieden** (Rollenschnitt liegt beim Nutzer).
