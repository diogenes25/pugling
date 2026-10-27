---
tags: [typ/story, status/abgenommen, bereich/test, bereich/auth, rolle/supervisor]
aliases: [All-Klausel des Konten-Sweeps ohne rote Probe]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-285]
nachgeschaut: "2026-10-27"
wartet_auf: ""
---

# B-289 · Die `All`-Hälfte des Konten-Sweeps in `AdultsController.Delete` hat keine rote Probe

[B-285](B-285-kind-konto-bleibt-nach-vater-loeschung.md) entfernt Konten, deren Profile **alle** auf den gelöschten
Erwachsenen oder seine verwaisenden Kinder zeigen (`AdultsController.cs:150-155`). Die Mutante „`.Where(a =>
a.Profiles.All(…))` entfernen“ (nur noch `Any`) bleibt in der Suite grün: kein Test hat ein Konto, auf dem ein
Profil *im* Set neben einem *außerhalb* liegt. Der Misch-Test prüft nur die Einschluss-Seite (gemischt mit
verwaistem Kind verschwindet); die Ausschluss-Seite fehlt – ein gemischtes Konto aus Vater und **ko-betreutem** Kind
muss stehen bleiben, sonst nimmt das Löschen das Login eines nicht verwaisten Kindes mit.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 2 (`01631ad`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-285`. In Teil 3 mitgenommen (`Defekt`, Freigabe 1, XS, nur Test).
- **2026-10-27** — gebaut in Lauf E, Teil 3: `Gemischtes_Konto_Mit_Ko_Betreutem_Kind_Bleibt_Stehen` sät das gemischte Konto (Vater + Student-Profil eines ko-betreuten Kindes) über den Kontext; nach dem Löschen des Vaters muss das Konto stehen und das Kind sich darüber anmelden können. Abgrenzung zum Einschluss-Fall: die zweite Betreuerin. **Rote Probe:** `All`-Klausel des Sweeps entfernt → **1 failed** (vorher grün), danach `DeletedAccountTokenTests` + `AdultLifecycleTests` **15/15**.
- **2026-10-27** — Review (`pugling-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)): 🟡 der umformulierte Summary in `TokenSubjectCheck.cs` behauptete die Folgerung ohne ihre Prämisse → Prämisse wiederhergestellt, Token-Vektor als Restpunkt an B-282 (nur Kommentar/Doku, ohne Zählung). 🟢 Sä-Block doppelt → Helfer `MixIntoAdultAccountAsync` mit Form-Zusicherung (nur Test, ohne Zählung). Danach 25/25. `abgenommen`; Rollengang als Integrationstest. Commit `12eab76`.
- **2026-10-27** — Nachschau in Lauf E, Teil 4 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), frischer Prüfer an einem `git archive` von `12eab76`: PIN „4545“ gegen `LoginChild`/`FindForChildAsync` nachgerechnet, Helfer-Zusicherungen gescoped, Mutante „`All` entfernt“ fällt an Zeile 149 und nirgends früher, `AdultLifecycleTests` bleibt dabei grün (eigenes Kind-Konto) – die neue Probe ist die einzige für die Klausel. **Kein Befund.**
