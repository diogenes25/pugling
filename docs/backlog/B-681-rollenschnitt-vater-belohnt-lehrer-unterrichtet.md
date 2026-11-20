---
tags: [typ/story, status/geschaetzt, bereich/auth, bereich/shop, rolle/supervisor, rolle/creator]
aliases: [Rollenschnitt Vater/Lehrer, Vater nur Belohnung]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
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

**Stand:** Stellvertretend gegrillt im Vorbereitungslauf 2026-11-20 auf Anweisung des Nutzers („Entscheide die offenen Fragen nach deiner Empfehlung“), unbestätigt. Die Rahmenentscheidungen A bis G gelten für den ganzen Cluster B-644, B-680 bis B-683, B-662, B-663, B-702 und B-703; führend ist B-681.

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

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Neue Rolle `Teacher` oder bestehendes Lehrer-Konto erweitern?** Empfehlung: **keine neue Rolle**, sondern die Plan-Endpunkte unter `supervisor/…` für `Creator` mit Lehrer-Zuordnung öffnen (Gate `AnyAdult` plus Eigentumsprüfung). Begründung: Das Lehrer-Konto ist schon Creator-only (`TeacherAccountsController.cs:12`), und eine vierte Rolle verbreitert den Schnitt von API, Code und Token, den CLAUDE.md als Taxonomie führt. Alternative: Rolle `Teacher` – sauberer im Token, kostet einen Claim und Gate-Umbau an ca. 33 Stellen.~~ → Entscheidung 1.
2. ~~**Wie bekommt ein Lehrer Zugriff auf *ein* Kind?** Empfehlung: eine zweite Verknüpfung (z. B. `TeacherLink`) statt `SupervisorLink` wiederzuverwenden – sonst sähe der Lehrer den Familien-Shop. Entscheidend für B-680 (Wer ist „der Lehrer“, wenn es der Agent ist?). Das ist ein Schema-Eingriff (Migration an die Kette).~~ → Entscheidung 1.
3. ~~**Läuft der Agent unter dem Vater-Konto weiter?** Empfehlung: für den Übergang ja (heute so, `AgentCommands.cs:268`); Ziel ist ein eigenes Agenten-Konto mit Creator- und Lehrer-Zuordnung, damit der Vater-PIN nicht in der Agenten- Konfiguration steht.~~ → Entscheidung 2.
4. ~~**Darf der Vater eingreifen?** Empfehlung: ja – Notbremse behalten (Plan pausieren, eine Aufgabe streichen), keine Mengen setzen. Das ist dieselbe Frage wie B-680 Punkt 3 und B-644 Entscheidungen 2, 4, 7, 8; dort nicht doppelt entscheiden.~~ → Entscheidung 3.
5. ~~**Wer setzt Klausurtermin und Note?** Empfehlung: Termin und Note bleiben beim **Vater** (er hat den Schulkontakt: WebUntis, Heft, Klausur) und werden dem Lehrer(-Agenten) gelesen zur Verfügung gestellt; B-684 (Scan) klärt den Eintrag. Ein Agent trägt keine Note ein, die Pflichten und Klausur-Prämien beeinflusst.~~ → Entscheidung 4.
6. ~~**Was passiert mit den laufenden Plänen in der echten `pugling.db`?** Empfehlung: **nichts** – Pläne hängen am Kind, nicht am Steuernden; der Schnitt ist ein Zugriffsumbau, kein Datenumbau. Nur die neue Zuordnung (Punkt 2) kommt als anhängende Migration (CLAUDE.md: Kette wird nur verlängert).~~ → Entscheidung 5.
7. ~~**Frontend:** Empfehlung: Perspektiven bleiben; „Zuweisen“ wird für den Vater zur Leseansicht, sobald der Planer (B-680/B-644) läuft. Nicht vorwegnehmen.~~ → Entscheidung 6.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Die Entscheidungen 7 bis 10 sind die Rahmenentscheidungen A, E, F und G des Clusters; die Entscheidungen 1 bis 3 tragen C und B, Entscheidung 4 trägt D.

1. **Es gibt keine neue Rolle `Teacher` und keine Zuordnung `TeacherLink` (Rahmen C, führend B-681).** Plan-, Positions-, Klassenarbeits- und Shop-Schreibrouten bleiben `Roles.Supervisor`; ein Lehrer-Konto (Creator ohne Betreuungsauftrag, `TeacherAccountsController.cs:12`) steuert keine Pläne. Begründung: Rahmen A macht den Planer zu Server-Code ohne Identität, ein Lehrer-Zugang zu einem Kind wird also nicht gebraucht; er bräuchte Schema (Migration), den Umbau von rund 33 Gate-Stellen (grep 2026-11-19) und eine zweite Eigentumsprüfung neben `SupervisorOwnsChildAsync`. Kosten: Ein menschlicher Lehrer mit Plan-Steuerung für ein Kind ist nicht möglich; kommt er, ist ein eigenes `TeacherLink` (nicht `SupervisorLink`, damit der Shop zu bleibt) die vorgemerkte Lösung, aber heute nicht gebaut. Verworfen: Gate `AnyAdult` plus `TeacherLink` jetzt (Empfehlung 1 und 2 aus B-681). Schließt aus: die Lesart der User Story von B-681, ein Lehrer-Konto steuere Pläne eines Kindes ohne Wallet-Sicht; erfüllt wird sie dadurch, dass der Planer kein Wallet liest und kein Konto hat. (Beantwortet die Punkte 1 und 2.)
2. **Der Konsolen-Agent läuft weiter unter dem Konto des Vaters, ein Agenten-Konto entsteht in diesem Zyklus nicht.** Begründung: Nach Rahmen A entscheidet der Agent keine Pflicht und braucht keine Plan-Rolle; Konto und PIN kommen wie heute aus Konfiguration und User-Secrets (`AgentCommands.cs:268`, `:942`). Kosten: Die PIN des Vaters liegt weiter in der Agenten-Konfiguration, und das Konto trägt mehr Rechte, als der Agent braucht (er legt Pläne weiter inaktiv an, `PlanPlanner.cs:101`). Verworfen: ein eigenes Agenten-Konto mit Creator-Profil – ein weiterer Account samt Zuordnung ohne heutigen Nutzen. Schließt aus: einen Agenten, dessen Spuren der Vater von den eigenen unterscheiden kann.
3. **Der Vater setzt Leitplanken und hat eine Notbremse, er legt keine Tagesmengen fest (Rahmen B, führend B-681).** Je Kind setzt der Vater einmal Boden und Deckel für Pflichtaufgaben und neue Wörter, den Malus-Betrag und Pausen (B-644 Entscheidungen 2 bis 5 und 8) und behält die Notbremse: eine nicht begonnene Aufgabe streichen oder eine eigene dazulegen (B-644 Entscheidung 7). Er baut keinen Plan und bestimmt keine Tagesmenge. Begründung: Ein Planer braucht eine menschliche Grenze gegen Fehlplanung; die vier Zahlen sind die eigenen Entscheidungen des Vaters vom 2026-11-17 und bleiben unberührt; Belohnung, Shop und Einlösen bleiben seine Aufgabe, die Tagesarbeit nicht. Kosten: Der Vater trägt weiter vier Zahlen und eine Pause je Kind; „nur Belohnung“ aus dem Leitbild ist damit nicht wörtlich erfüllt (abgewichen von B-680 Punkt 3, der Boden und Deckel zu Planer-Voreinstellungen machte). Verworfen: Boden und Deckel als reine Planer-Voreinstellung – hübe B-644 Entscheidung 4 auf und ließe den Planer ohne harte Grenze. Schließt aus: einen Vater, der gar nicht mehr in die Mengen eingreifen kann; die User Story „ich möchte die Plansteuerung nicht mehr selbst tragen“ gilt für die tägliche Arbeit, nicht für die Grenzen. (Beantwortet Punkt 4.)
4. **Klausurtermin und Note trägt der Vater ein (Rahmen D, führend B-681).** Die Schreibrouten der Klassenarbeit bleiben `Roles.Supervisor` (`KlassenarbeitenController.cs:92`, `:130`); Planer, Agent und eine spätere Foto-Auswertung (B-684) lesen Termin und Note oder schlagen sie nur vor, die Freigabe bleibt beim Vater. Begründung: Er hat den Schulkontakt (WebUntis, Heft, Klausur), und eine Note beeinflusst Stufe und Pflicht – kein Konto, das davon profitiert oder Pflichten setzt, trägt sie selbst ein. Kosten: ein Handgriff mehr nach jedem Scan; solange B-684 fehlt, bleibt das Eintragen Handarbeit. Verworfen: Note direkt aus dem Scan übernehmen. Schließt aus: ein Kind oder einen Agenten, der die eigene Note einträgt (Selbstbetrug). (Beantwortet Punkt 5.)
5. **Der Rollenschnitt verändert keine Daten und bringt keine Migration.** Begründung: Pläne, Positionen und Fortschritte hängen am Kind, nicht am Steuernden; eine neue Zuordnung entfällt nach Entscheidung 1. Der Stichtag-Reset aus B-644 (Entscheidungen 11 und 12) ist davon unberührt und bleibt dort. Kosten: keine. Verworfen: eine Tabelle `TeacherLink` samt Migration (Entscheidung 1). Schließt aus: einen Schnitt, der Bestandsdaten der echten `pugling.db` anfasst.
6. **Die drei Perspektiven bleiben; „Zuweisen“ wird für das Vater-Konto erst zur Leseansicht, wenn der Planer scharf ist (B-644 Etappe E3).** Begründung: Bis dahin legt der Vater Pläne von Hand an; die Maske vorher wegzunehmen ließe die Kinder ohne Plan. Kosten: Eine Zwischenzeit mit der alten Maske; die Leseansicht ist eigene Arbeit nach E3. Verworfen: die Maske jetzt abschalten. Schließt aus: einen Vater ohne Planungsmöglichkeit, solange kein Planer läuft.
7. **Der serverseitige Planer entscheidet den Tagesplan, nicht ein Sprachmodell (Rahmen A, führend B-681).** Der Tagesplan entsteht durch den Planer aus B-644 mit festen Regeln in C#; „Lehrer-Agent“ ist der Name dieses Planers gegenüber dem Kind, kein Modellaufruf. Der Konsolen-Agent (`Pugling.Agent.Creator`) bleibt das Werkzeug für Inhalte und entscheidet keine Pflicht. Begründung: Die Pflicht kostet Münzen und muss nachrechenbar und wiederholbar sein (B-644 Entscheidung 10); ein Agent mit Plan-Steuerung bräuchte außerdem ein eigenes Konto und einen Zugriff auf Kinder, den es nicht gibt (Eigentum läuft über `SupervisorLink`, `AuthAccess.cs:112`). Kosten: Der Planer ist nur so klug wie seine Regeln; ein unvorhergesehenes Muster bleibt unentdeckt, bis jemand eine Regel schreibt. Verworfen: ein Sprachmodell, das den Plan je Kind täglich selbst festlegt – nicht nachrechenbar und mit dem lokalen Modell ausfallgefährdet. Schließt aus: einen frei „erfundenen“ Plan je Kind, wie ihn der Satz „Lehrer-Agent erzeugt jeden Tag den Plan“ nahelegt; das Kind bekommt einen täglichen Plan nach seinem Lernstand, kein Modell-Ergebnis.
8. **Der Tagesplan steht ab 6 Uhr fest, und Malus kostet nur, was das Kind sehen konnte (Rahmen E, führend B-681).** Der Plan des Tages ist ab 6 Uhr Ortszeit eingefroren (B-644 Entscheidung 9); eine verpasste Pflichtaufgabe eines gesehenen Plans kostet genau einmal den Malus-Betrag des Kindes (B-644 Entscheidung 5); ein Tag, an dem nie ein Plan entstand, zählt wie eine Pause (B-644 Entscheidung 19). Begründung: Heute steht die Pflicht Wochen vorher an der Position (`PlanPosition.PenaltyCoins`); eine Pflicht, die erst heute früh entsteht, darf nicht rückwirkend oder unsichtbar kosten (Leitbild: „nicht unfair machen“). Kosten: Wird der Plan um 6 Uhr nicht erzeugt, entgeht dem Kind ein Pflichttag (als Pause beim Vater gemeldet); ein erzeugter Plan lässt sich nur über die Notbremse des Vaters ändern. Verworfen: Malus über die Pflicht an der Position auch für Planer-Kinder. Schließt aus: Strafe für Aufgaben, die das Kind nie angezeigt bekam oder die der Planer nach dem Erzeugen verändert hätte.
9. **Münzen hängen an der erfüllten Pflicht, nicht an Notenstufe oder Klausurnote (Rahmen F, führend B-681).** Zielpunkte, Truhe und Serie bleiben wie heute an der erfüllten Pflicht; in diesem Zyklus gibt es keine Münzprämie für eine Notenstufe oder eine Klausurnote, die Notenstufe ist Auszeichnung ohne Münzwert (`RewardPoints` 0). Begründung: Die Stufe ist aus Lerndaten geschätzt und keine harte Leistung wie ein bestandener Test, die Note wird von Hand eingetragen; eine Münzprämie darauf trüge Prognosefehler und Eintragsfehler in das Guthaben (Wallet-Invarianten). Die Probeklausur B-249 ist ebenfalls ohne Münzen entschieden. Kosten: Eine gute Klausur belohnt der Vater von Hand (`POST children/{}/points`, Manual); für die Stufe gibt es Ehre statt Geld. Verworfen: Klausurprämie in Münzen. Schließt aus: ein Kind, das für eine Eins in der Klausur automatisch Münzen bekommt.
10. **Modellerzeugter Inhalt wird erst durch einen Menschen Pflicht (Rahmen G, führend B-681).** Förder-Übungen und Vorstufen aus dem Agenten entstehen als Katalog-Entwurf; Pflicht werden sie erst, wenn ein Mensch sie in einen Plan übernimmt (`assign`, Positionsformular, Einschalten der Stufung). Der Planer wählt nur aus Positionen des aktiven Plans und legt aus Modellinhalt keine Position selbst an. Begründung: Das ersetzt die heutige Sicherung „KI-Plan entsteht inaktiv“ (`PlanPlanner.cs:101`) durch die kleinste menschliche Handlung statt einer Freigabe je Aufgabe; C# prüft die Lösbarkeit (B-460), nicht die Didaktik, und die Qualität hängt am Modell. Kosten: Neue Förder-Übungen erreichen das Kind erst nach einem Vater-Klick; der Agent kann nicht unbeaufsichtigt Inhalt bis zum Kind durchreichen. Verworfen: Der Planer nimmt frische Modellübungen selbst auf, Schattenwoche plus Deckel und Boden als einzige Sicherung. Schließt aus: den täglich vollautomatischen Kreislauf „Agent baut, Planer verteilt, Kind lernt ohne Vater“, den das Leitbild andeutet.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. *Absichtserklärung (hält den Ist-Zustand).* Ein Konto nur mit Creator-Profil bekommt auf Plan-, Positions-, Klassenarbeits- und Shop-Schreibrouten `403`. *Rot, wenn* eines dieser Gates auf `Creator` oder `AnyAdult` geöffnet wird (Integrationstest je Routenfamilie mit einem Token ohne Supervisor-Claim).
2. *Absichtserklärung.* `Roles` kennt genau drei Ebenen-Rollen (`Creator`, `Supervisor`, `Student`); `AnyAdult` ist deren Kombination und `Admin` der Break-glass-Claim (`Auth/AuthAccess.cs:34`, `:38`), beides keine vierte Ebene. Das Modell kennt keine Zuordnung `TeacherLink`. *Rot, wenn* eine vierte Ebenen-Rollenkonstante oder eine solche Entität hinzukommt (Reflexionsprüfung in `ConventionGuardTests`).
3. *Absichtserklärung.* Ein Vater (Creator und Supervisor) bedient Shop und Einlösung wie bisher. *Rot, wenn* ein Shop-Gate an etwas Neues gebunden wird.
4. Boden, Deckel, Malus-Betrag und Pause lassen sich nur mit Supervisor-Claim und bestandener `SupervisorOwnsChildAsync`-Prüfung setzen: ein Konto nur mit Creator-Profil bekommt `403`, ein fremder Vater `404`. *Rot, wenn* eines der beiden Gates fehlt (gilt ab dem Bau von B-644 E0).
5. Der Planer nimmt keine Tagesmenge aus einem Request an und hebt eine vom Vater eingetragene Pause nie auf. *Rot, wenn* ein Endpunkt die Menge übernimmt oder der Planer das Pausenfeld überschreibt (gilt ab dem Bau von B-644).

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-20 (Sonnet), stellvertretend, unbestätigt.* Nach den Entscheidungen 1 bis 10 baut B-681 **keine neue Rolle und keinen neuen Zugang**: Die Story hält den Ist-Zustand mit mechanischen Absichtserklärungen fest und
trägt die zwei Kriterien (4 und 5), die erst mit [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md) und
[B-734](B-734-planer-kern-und-schattenmodus.md) greifen.

**Größe: S** — Anker: `childId` aus dem Test-Pfad ziehen (B-01): kein Produktcode, drei Testgruppen und eine Reflexionsprüfung.

- **`migration: nein`** — Entscheidung 5; nachgesehen: keine Entität `TeacherLink` im Modell (Suche in `Models/*.cs` ohne Treffer), nur `SupervisorLink` (`Models/AdminEntities.cs:17`).
- **`vertragsbruch: nein`** — kein Vertragstyp ändert sich.
- **`wo: backend`** — Tests in `Pugling.Api.Tests`.

### Wo die Kriterien gebaut werden

| Kriterium | Wo | Wann |
| --- | --- | --- |
| 1 (Creator-only bekommt `403` auf Plan-, Positions-, Klassenarbeits- und Shop-Schreibrouten) | **diese Story**, neue Klasse `RollenschnittTests` | sofort |
| 2 (drei Ebenen-Rollen, kein `TeacherLink`) | **diese Story**, `ConventionGuardTests` | sofort |
| 3 (Vater bedient Shop wie bisher) | **diese Story** als Absichtserklärung über `ShopFlowTests` | sofort |
| 4 (Boden, Deckel, Malus-Betrag, Pause nur mit Supervisor-Claim und Eigentum) | [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md) Kriterien 5 und 6 | mit B-731 |
| 5 (Planer nimmt keine Tagesmenge an, hebt Pause nie auf) | [B-734](B-734-planer-kern-und-schattenmodus.md) Kriterium 9 | mit B-734 |

### Risiken

- **Heutige Abdeckung ist schmaler als Kriterium 1:** `TeacherAccountTests` prüft nur `GET supervisor/children`, `GET supervisor/class-tests`, `POST supervisor/children` und
  `POST supervisor/study-plans` auf `403` (`Lehrer_KommtNichtInDenBetreuungsBereich`, `Lehrer_KannKeinKindAnlegenUndKeinenPlanBauen`). Positions-, Klassenarbeits-Schreib- und Shop-Routen
  hat kein Test mit einem Creator-Token (ein Konto-Token ohne Supervisor-Claim entsteht in den Tests nur über `RegisterTeacherAsync` in `TeacherAccountTests`). Der Ist-Stand lässt sich deshalb heute unbemerkt öffnen.
- **Rollenzählung:** `Roles` enthält vier Konstanten (`Creator`, `Supervisor`, `Student`, `AnyAdult`) und `Admin`; die Formulierung „genau drei Rollen" war zu eng (Kriterium 2 oben ist angepasst).
- **Die 33 Gate-Stellen sind die Messlatte:** 33 `Roles.Supervisor`-Treffer in 19 Controllern (`grep`, 2026-11-20); die Theorie in `RollenschnittTests` soll je Routenfamilie
  einen Vertreter aufrufen, nicht jede Aktion (Letzteres tut der Ownership-Filter in `OwnershipMatrixTests`).
- **Ein Test, der den Ausgangszustand prüft, kann nicht fallen:** Jede Absichtserklärung nennt hier die Änderung, die sie rot macht (siehe Testweg).

### Angriffsplan

1. `RollenschnittTests`: Theorie über Routenfamilien (Pläne, Positionen, Klassenarbeiten, Shop-Schreibrouten) mit Token eines Kontos ohne Supervisor-Claim (Aufbau wie `TeacherAccountTests.RegisterTeacherAsync`).
2. Reflexionsprüfung in `ConventionGuardTests`: Ebenen-Rollen und `TeacherLink`.
3. Kriterium 3: Verweis auf `ShopFlowTests`; Absichtserklärung im Kommentar der Klasse.

### Testweg

- Neue Klasse `RollenschnittTests` (Integration): rote Probe je Familie — das `[Authorize(Roles = Roles.Supervisor)]` des `ShopController` auf `Roles.AnyAdult` stellen lässt die Shop-Zeile fallen, dasselbe
  am `PlanPositionsController` die Positionszeile, am `KlassenarbeitenController` die Klassenarbeitszeile.
- `ConventionGuardTests`: neues Tor für die Rollenkonstanten (Rot-Probe: `public const string Teacher = "Teacher";` in `Roles` einfügen) und für `TeacherLink` (Rot-Probe: Klasse `TeacherLink` im Modell anlegen).
- Regression: `TeacherAccountTests`, `OwnershipMatrixTests`, `OwnershipTests`, `ShopFlowTests`, `MultiSupervisorTests`.
- Vor der Abnahme: `pugling-reviewer`; ein Rollengang entfällt (keine Oberfläche, nur Gates), im `## Verlauf` benennen.

### Reihenfolge und Abhängigkeiten

Unabhängig von B-730 bis B-739 und sofort baubar. Die Kriterien 4 und 5 laufen mit [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md) und
[B-734](B-734-planer-kern-und-schattenmodus.md); B-681 gilt erst als abgenommen, wenn deren Stellen ihre Kriterien tragen (im `## Verlauf` verlinken).

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
- **2026-11-20** — `ausformuliert` → `gegrillt` (Vorbereitungslauf, Sonnet, stellvertretend auf Anweisung des Nutzers, unbestätigt): Alle sieben Punkte entschieden, dazu die Rahmenentscheidungen A bis G für den Cluster. Kern: Der Planer läuft im Server ohne Identität, deshalb keine Rolle `Teacher` und kein `TeacherLink`; der Vater behält Leitplanken und Notbremse, trägt Termin und Note ein, der Agent bleibt Inhalts-Werkzeug.
- **2026-11-20** — `gegrillt` → `geschaetzt` (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Eintrittsbedingung `gegrillt` erfüllt (sieben Punkte entschieden). S, backend, keine Migration, kein Vertragsbruch: nur Absichtserklärungen und Gate-Tests. Nachgesehen: Die Rollenzählung war zu eng (`AnyAdult` und `Admin` neben den drei Ebenen), die heutigen Tests decken Positions-, Klassenarbeits-Schreib- und Shop-Routen für ein Creator-Konto nicht ab; Kriterium 2 umformuliert, Kriterien 4 und 5 den Teilstories von B-644 zugeordnet.
