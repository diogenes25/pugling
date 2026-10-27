---
tags: [typ/story, status/idee, bereich/auth, rolle/supervisor, rolle/student]
aliases: [Kind-Konto bleibt nach Vater-Löschung als leere Hülle, Kind-Token überlebt das Löschen des Vaters]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu NP-3 in Lauf E (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-245]
wartet_auf: ""
---

# B-285 · Löscht sich ein Vater, bleibt das Konto seines Kindes als leere Hülle – und der Kind-Token bleibt gültig

[B-245](B-245-token-mit-fid-auf-geloeschten-adult.md) hat den Token-Check `TokenSubjectCheck` eingezogen: ein Token,
dessen Konto nicht mehr existiert, bekommt 401. Die Begründung dort („deleting an adult or a child removes every
account whose profiles all point at it … a live account means live fid/cid“) stimmt auf **einem** Pfad nicht:
`AdultsController.Delete` (`:138-150`) entfernt die verwaisenden Kinder per `RemoveRange` und **nur das eigene**
Konto. Die `AccountProfile`-Zeile des Kindes kaskadiert mit (`PuglingDbContext.cs:138-139`), das `Account` des Kindes
hat keinen Fremdschlüssel zum Kind und überlebt mit null Profilen – genau die leere Hülle, die
`ChildrenController.Delete` (`:140-147`) im eigenen Löschpfad ausdrücklich beseitigt.

Folge: `TokenSubjectCheck.cs:32` findet das Konto per `aid`, der vorher ausgestellte Kind-Token (mit `cid` auf das
gelöschte Kind) bleibt gültig; `GET auth/me` antwortet 200, und jeder Student-Schreiber, der `cid` in einen
Fremdschlüssel setzt, läuft wieder in die 500, die B-245 schließen wollte. Zusätzlich stellt `auth/login` mit
`accountId` + Kind-PIN weiter einen rollenlosen Token aus.

**Reproduktion:** Vater (alleiniger Supervisor) legt ein Kind an, das Kind loggt sich ein, der Vater ruft
`DELETE supervisor/adults/{self}` → `GET auth/me` mit dem Kind-Token ist 200 statt 401.

**Behebung (Vorschlag des Prüfers):** in `AdultsController.Delete` vor `RemoveRange(verwaisende)` dieselbe Regel wie
in `ChildrenController.Delete:143-147` auf die Ids der verwaisenden Kinder anwenden (Konten entfernen, deren Profile
*alle* auf verwaisende Kinder zeigen), dazu ein Fall in `DeletedAccountTokenTests` (Mutante: ohne den Zusatz → 200
statt 401). Nicht in [B-282](B-282-login-haertung-restpunkte.md) enthalten.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu NP-3 (`b644081`) in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-245`.
