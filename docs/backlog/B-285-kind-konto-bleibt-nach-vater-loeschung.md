---
tags: [typ/story, status/abgenommen, bereich/auth, rolle/supervisor, rolle/student]
aliases: [Kind-Konto bleibt nach Vater-Löschung als leere Hülle, Kind-Token überlebt das Löschen des Vaters]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu NP-3 in Lauf E (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-245]
wartet_auf: ""
---

# B-285 · Löscht sich ein Vater, bleibt das Konto seines Kindes als leere Hülle – und der Kind-Token bleibt gültig

## User Story

Als **Sohn** eines Vaters, der sein Konto gelöscht hat, darf ich mit meinem alten Token nichts mehr erreichen – so
wie nach dem Löschen meines eigenen Profils –, damit kein Schreiber mit einer `cid` auf ein gelöschtes Kind in einen
Fremdschlüssel läuft (die 500, die [B-245](B-245-token-mit-fid-auf-geloeschten-adult.md) schließen wollte).

## Ist-Stand am Code

- `AdultsController.Delete` (`AdultsController.cs:138-150`) entfernt die verwaisenden Kinder per `RemoveRange` und
  **nur das eigene** Konto (`Profiles.All(p => p.AdultId == adultId)`).
- Die `AccountProfile`-Zeile des Kindes kaskadiert mit (`PuglingDbContext.cs:138-139`, Cascade auf `ChildId`); das
  `Account` des Kindes hat keinen Fremdschlüssel zum Kind und überlebt mit null Profilen.
- `ChildrenController.Delete` (`:140-147`) kennt die Regel und beseitigt die leere Hülle im eigenen Löschpfad.
- `TokenSubjectCheck.cs:32` prüft nur, ob das Konto zur `aid` existiert – die Hülle genügt ihm.

## Die echte Lücke

Eine **Asymmetrie zweier Löschpfade**: dieselbe Regel steht am Kind-Pfad, fehlt aber am Vater-Pfad, obwohl der
Vater-Pfad Kinder mitlöscht. Die Begründung in `TokenSubjectCheck.cs:15-17` („a live account means live fid/cid“)
war auf diesem Pfad falsch.

## Akzeptanzkriterien

1. Nach `DELETE supervisor/adults/{self}` eines alleinigen Betreuers antwortet `GET auth/me` mit dem Kind-Token 401.
2. Ein Kind mit einem **zweiten** Betreuer bleibt samt Konto bestehen (die Regel greift nur für verwaisende Kinder) –
   gehalten durch die bestehende Bedingung `SupervisorLinks.All(…)`, die die Konten-Auswahl übernimmt.
3. Integrationstest in `DeletedAccountTokenTests`, rot ohne den Zusatz.

## Schätzung

**XS**, backend, keine Migration, kein Vertragsbruch: eine zweite Konten-Abfrage im bestehenden Löschpfad nach dem
Muster von `ChildrenController.Delete`, ein Testfall.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu NP-3 (`b644081`) in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-245`.
- **2026-10-27** — in Lauf E, Teil 2, autonom gegrillt (Freigabe 1, `Defekt`), geschätzt und gebaut. **Rote Probe:**
  `Token_Eines_Mit_Dem_Vater_Geloeschten_Kindes_Bekommt_401` vor dem Fix → erwartet 401, gemessen **200** (1 failed,
  2 passed). Fix: `AdultsController.Delete` entfernt die Konten, deren Profile alle auf verwaisende Kinder zeigen
  (Muster `ChildrenController.Delete`). Danach `DeletedAccountTokenTests` + `AdultLifecycleTests` +
  `PinSingleSourceTests` **23/23 `exit=0`**.
- **2026-10-27** — Review (`pugling-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Zähler 2: **🟡 (1)** Das Überleben des Kontos eines ko-betreuten Kindes war nicht zugesichert – Zeile in `Loeschen_Laesst_Das_Ko_Betreute_Kind_Stehen`; Mutante „Konten aller betreuten Kinder“ → **3 failed** (darunter dieser Fall). **🟡 (2)** Ein gemischtes Konto (Erwachsenen- und Student-Profil auf einem Login; vom Modell erlaubt, von keinem Anlege-Pfad gebaut) fiel durch beide getrennten Abfragen – jetzt **ein** Prädikat für Erwachsenen und verwaisende Kinder; neuer Fall `Gemischtes_Konto_…` sät das gemischte Konto über den Kontext. Rote Probe gegen die getrennten Abfragen: **1 failed**, danach 14/14. (Ein erster Lauf fiel an einer falschen Aufbau-Zusicherung – 3 statt 2 Profile, der Vater trägt Creator *und* Supervisor – und ist keine Probe.) 🟢 ohne Zählung: Summary nennt jetzt auch die Kinder-Konten; die Leer-Abfrage bleibt der Symmetrie wegen. Außerhalb des Diffs notiert: `ChildrenController.Delete` hat dieselbe latente Lücke für gemischte Konten → als Restpunkt an [B-282](B-282-login-haertung-restpunkte.md). **Rollengang:** kein Browser; die drei Fälle in `DeletedAccountTokenTests` sind der Weg (Vater löscht sich → Kind-Token 401) und das Gegenstück (Ko-Betreuung bleibt). Suite **951/951 `exit=0`**. `abgenommen`; Commit siehe Protokoll.
