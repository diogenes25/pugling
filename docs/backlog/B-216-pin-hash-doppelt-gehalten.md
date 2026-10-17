---
tags: [typ/story, status/idee, bereich/auth]
aliases: [PIN-Hash doppelt, Account.PinHash Spiegelung]
status: idee
prio: P3
art: Aufräumen
quelle: Projektanalyse 2026-10-17 (Chat-Sitzung „Ist das Projekt schlüssig?", kein Protokoll)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-216 · Der PIN-Hash liegt doppelt: Profil und Konto halten denselben Wert

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

## Verlauf

- **2026-10-17** — Review-Nachtrag: die Zählung der Schreibpfade war zu niedrig (drei statt vier plus Seed);
  der veraltete Kommentar an `Adult.Pin`/`Child.Pin` („Simple PIN login …") ist unabhängig von dieser Story
  korrigiert und nennt jetzt Hash-Pflicht und Spiegelung.
- **2026-10-17** — angelegt aus der Projektanalyse. Beleg: `AccountService.cs:88-106` (Spiegelung),
  `AuthController.cs:57/74/101` (drei Verify-Stellen), `AdminEntities.cs:48/106` (Profil-Hash samt altem
  Kommentar), `CLAUDE.md` → Fallstricke → „PINs sind gehasht".
