---
tags: [typ/story, status/abgenommen, bereich/auth]
aliases: [PIN-Hash doppelt, Account.PinHash Spiegelung]
status: abgenommen
prio: P3
art: Aufräumen
quelle: Projektanalyse 2026-10-17 (Chat-Sitzung „Ist das Projekt schlüssig?", kein Protokoll)
unverifiziert: true
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-10-27"
---

# B-230 · Der PIN-Hash liegt doppelt: Profil und Konto halten denselben Wert

Derselbe PIN-Hash steht zweimal in der Datenbank: auf der fachlichen Zeile (`Adult.Pin`, `Child.Pin`) und
auf dem Login-Konto (`Account.PinHash`). Drei Login-Pfade prüfen gegen zwei Quellen: `auth/adult` und
`auth/child` gegen das Profil, der konto-zentrische `auth/login` gegen das Konto. Damit beide Pfade dieselbe
PIN akzeptieren, muss jeder Schreibpfad nach dem Hashen `AccountService.MirrorAsync` bzw. `EnsureFor…Async` rufen – heute an
vier Stellen (`AuthController`, `AdultsController`, `ChildrenController`, `TeacherAccountsController`);
dazu kommen fünf PIN-Schreibungen im `Seed`, die sich auf den Seed-Nachlauf als Spiegel verlassen. Die
Root-CLAUDE.md führt das unter „Fallstricke" als Regel, die an Disziplin hängt; kein Tor prüft sie. Ein
vergessener Spiegel fällt erst auf, wenn ein Nutzer über den anderen Login-Pfad kommt.

Naheliegende Richtung, **noch nicht entschieden**: eine Quelle. Entweder `Adult.Pin`/`Child.Pin` entfallen
und alle drei Logins lesen `Account.PinHash`, oder `auth/login` entfällt zugunsten der Rollen-Logins. Beides
ist eine Schemaänderung (Kette neu falten) und berührt den Seed-Nachlauf.

## Entscheidungen

1. **Der Profil-Hash entfällt** (Nutzer, 2026-10-23, G-8): `Adult.Pin`/`Child.Pin` sind gestrichen, alle drei Logins
   prüfen `Account.PinHash`. Migration, kein Vertragsbruch.
2. **Die PIN kommt beim Anlegen des Kontos mit.** `AccountService.EnsureFor…Async(…, pin, …)` hasht sie, Änderungen laufen
   über `AccountService.SetPin`. Der Login legt kein fehlendes Konto mehr nachträglich an: Ohne Profil-PIN gäbe es
   nichts, wogegen er prüfen könnte.
3. **Auslegung (vom Lauf getroffen, zur Kenntnis):** Die Entscheidung sagt, `MirrorAsync` verschwinde. Es spiegelt
   aber auch Anzeigename und E-Mail aufs Konto. Ohne das liefe ein umbenanntes Kind mit altem Namen ins Token, und
   der Unique-Index auf der E-Mail sähe einen veralteten Wert (beides in `AccountService` begründet). Gestrichen ist
   darum nur der PIN-Teil, Name und E-Mail werden weiter gespiegelt. Der Fallstrick „PIN aufs Konto spiegeln“ in der
   Root-`CLAUDE.md` ist ersetzt durch „PINs liegen nur am Konto“.
4. **Seed:** Die geseedeten Personen bekommen ihre PIN aus einer festen Tabelle (E-Mail bzw. Name → PIN) in dem
   Schritt, der die Konten am Ende anlegt. Gehasht wird nur, wenn wirklich ein Konto entsteht.

## Akzeptanzkriterien

1. `Adult` und `Child` haben keine PIN-Spalte mehr. Die Kette ist neu gefaltet, und der Snapshot-Diff zeigt genau die
   zwei entfernten Spalten.
2. Nach einer PIN-Änderung über `PATCH supervisor/children/{id}`, `PATCH supervisor/adults/{id}` oder
   `PATCH auth/me` akzeptieren Rollen-Login und Konto-Login die neue PIN und lehnen die alte ab.
3. Die geseedete Familie meldet sich mit ihren bekannten PINs an (0000, 1111, 0001, 2222, 9999).

## Schätzung

**M**, `wo: backend`, Migration (Kette neu gefaltet), kein Vertragsbruch.

## Verlauf

- **2026-10-17** — Review-Nachtrag: die Zählung der Schreibpfade war zu niedrig (drei statt vier plus Seed);
  der veraltete Kommentar an `Adult.Pin`/`Child.Pin` („Simple PIN login …") ist unabhängig von dieser Story
  korrigiert und nennt jetzt Hash-Pflicht und Spiegelung.
- **2026-10-17** — angelegt aus der Projektanalyse. Beleg: `AccountService.cs:88-106` (Spiegelung),
  `AuthController.cs:57/74/101` (drei Verify-Stellen), `AdminEntities.cs:48/106` (Profil-Hash samt altem
  Kommentar), `CLAUDE.md` → Fallstricke → „PINs sind gehasht".
- **2026-10-17** — **umnummeriert von B-216 auf B-230** im Nachtlauf (zweiter Lauf): Die Id B-216 war seit
  dem 2026-09-04 an „Zwei Wiederhol-Knöpfe verschwinden …" vergeben (abgenommen, mit Commits, Protokoll und
  `entgangen_bei`-Verweisen); die Projektanalyse hat sie am 2026-10-17 ein zweites Mal vergeben. Die
  abgenommene Story behält die Id. Der Index zeigte beide Zeilen als B-216, und kein Tor hat es gemeldet.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Belege stimmen. Die Kommentare an `Models/AdminEntities.cs` zeigten noch auf die doppelt vergebene Id B-216 – in derselben Sitzung auf B-230 korrigiert. Reihenfolge: **nach B-28** (dessen Leer-PIN-Guard gehört zentral in `PinHasher.Verify`). Beide Varianten brauchen eine Migration, eher M; die Wahl (Profil-Hash entfällt vs. `auth/login` entfällt) ist beim Menschen.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-8): Der **Profil-Hash entfällt**. `Adult.Pin`/`Child.Pin` werden gestrichen, alle drei Logins prüfen `Account.PinHash`, und `AccountService.MirrorAsync` samt dem Fallstrick in der Root-`CLAUDE.md` verschwindet. Das ist eine Migration, aber kein Vertragsbruch. **Nach B-28** (dessen Leer-PIN-Guard sitzt zentral in `PinHasher.Verify`). Der Seed-Nachlauf zieht nach. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-3); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-10-26** — ausformuliert und geschätzt (M) unter Freigabe 9, gebaut in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md), NP-3, nach B-28 und B-245). Kette neu gefaltet (`…_InitialCreate`), der Snapshot-Diff zeigt genau die zwei `Pin`-Spalten. Neue `PinSingleSourceTests` (AK 2). Rote Proben, je der Schreibpfad ohne `SetPin`: Kind-PATCH / Erwachsenen-PATCH / `auth/me` → je **1 failed** im passenden Fall. Vier Tests legten Personen mit `Pin = …` direkt an. Drei davon verlieren nur die Zuweisung, einer (`AddItem_FremderVater_Liefert403`) registriert jetzt über die API, weil er sich auf das Nachlegen des Kontos beim Login verließ. Backend **925 + 3 = 928** nach dem letzten Lauf, siehe Verlauf unten.
- **2026-10-26** — Review ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C, NP-3): `pugling-reviewer` hält Entscheidung 3 (Name und E-Mail weiter spiegeln) für tragfähig. Gezählt: (1) AK 3 trug nur 2 von 5 Seed-PINs → Theorien über alle fünf, Probe „Tabelle ohne Demo-Vater“ → **1 failed**; (2) „ein PATCH ohne PIN behält sie“ war ungetestet → `Ein_Patch_Ohne_Pin_Behaelt_Die_Pin`, Probe „Ensure überschreibt die PIN“ (Erwachsener / Kind) → je **1 failed**; (3) aus dem Delta-Review: ein `Ensure`, das eine übergebene PIN auf ein bestehendes Konto schreibt (jeder Neustart setzte die Seed-PINs zurück), bliebe grün → `Ensure_Mit_Pin_Aendert_Die_Pin_Eines_Bestehenden_Kontos_Nicht`, Probe je Ensure-Pfad → **1 failed**. Zurückgestellt (B-282): Seed-PIN eines Kindes nach Namen. Die Summe im vorigen Eintrag („928“) war vorläufig, nach dem letzten Lauf sind es **936**. Rollengang ohne Browser (Extension nicht verbunden): **Vater und Sohn** melden sich in jeder E2E-Spec an (`vaterLogin`/`sohnLogin`), Playwright voll **95 passed** nach dem letzten Produktions-Code-Edit (danach nur Tests und Kommentare). **Creator** (Lehrer-Konto) fährt `lehrer-konto.spec.ts` mit. Backend: Solution-Build 0 Warnungen, **936/936 `exit=0`**. Zähler NP-3 am Ende **5** (Grenze erreicht, nicht überschritten). Zurückgestellt → [B-282](B-282-login-haertung-restpunkte.md). **Abgenommen**; Commit: `b644081`.
- **2026-10-27** — Nachschau in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), frischer Prüfer am Commit `b644081`: Grep `\.Pin\b|PinHash` über alle `.cs` – nur `Account.PinHash`, DTO-Felder, Test-Konstanten; Snapshot trägt nur `PinHash`, Kette genau eine Migration; Seed-PIN-Tabelle (`Seed.cs:134-162`) gegen die fünf geseedeten Personen vollständig; `MirrorAsync` rührt den Hash nicht mehr an. Behauptung „Snapshot-Diff zeigt genau die zwei Pin-Spalten“ am `git show` nachgerechnet (−10 Zeilen, nur `Pin` auf Adults/Children). **Kein Befund.**
