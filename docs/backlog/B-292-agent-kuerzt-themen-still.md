---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [Agent kürzt Themen still auf 200 Zeichen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 5 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22]
wartet_auf: ""
---

# B-292 · Der `fill`-Agent kürzt ein zu langes Thema still auf 200 Zeichen – die API lehnt genau das bewusst ab

`UnitNotesFiller.Clamp` (`UnitNotesFiller.cs:138`) schneidet einen Themen-Eintrag des Entwurfs auf 200 Zeichen, während
`SeriesUnitsController.ValidateTopics` (`:229-230`) seit B-130 ausdrücklich **ablehnt statt zu kürzen**: „ein still
gekürztes Thema ist genau das, was der KI-Creator als gesetzten Stoff liest – ein halbes Thema würde still zu einem
falschen.“ Der Agent führt die stille Kürzung für dieselben Daten wieder ein, und im Schreib-Lauf steht der gekürzte
Entwurf erst **nach** dem PATCH auf der Konsole (`AgentCommands.FillAsync`). Dazu: `Distinct` läuft vor dem Schnitt
(zwei Themen, die sich erst nach Zeichen 200 unterscheiden, werden nach dem Kürzen Dubletten), und ein Schnitt bei
2000 kann ein Surrogat-Paar oder ein Wort teilen.

**Behebung (Vorschlag des Prüfers):** zu lange Themen aus dem Entwurf **streichen und melden** (Liste `Dropped` im
`UnitFillOutcome`, unter „Entwurf“ gedruckt) oder mit `AgentException` abbrechen und das Modell erneut fragen; den
Freitext-Schnitt bei 2000 (Spaltenlänge) behalten, aber ebenso melden und wortgerecht schneiden.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 5 (`f033f70`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 2 des frischen Prüfers; `entgangen_bei: B-22`.
- **2026-10-27** — in Lauf E, Teil 7, autonom gegrillt (Freigabe 1, `Defekt`), geschätzt (XS, nur Agent) und gebaut: `Clamp` lässt Themen über 200 Zeichen **weg und nennt sie** (`UnitFillOutcome.Dropped`), kürzt Freitext über 2000 am Wortende und nennt das Feld (`Shortened`); `FillAsync` druckt beides vor der Schreib-Meldung; die Unit kommt per `GetUnitAsync` (404 → Fehlbedienung), die Klassen-Summary nennt den Filler. **Rote Proben:** `Fill_Laesst_Zu_Lange_Themen_Weg_Und_Kuerzt_Freitext_Am_Wortende` – Mutante „kein Weglassen“ → **1 failed** (die API lehnt den gekürzten Eintrag ab), Mutante „Schnitt mitten im Wort“ → **1 failed** (Abgrenzung: 2000 fällt bei „Wortes“-Wörtern mitten ins Wort); danach `Fill_*` **10/10**.
- **2026-10-27** — Review (`pugling-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Zähler 1: **🟡 (1)** `Take(50)` war dieselbe stille Kürzung eine Stufe weiter – das 51. Thema verschwand ungenannt → Überzahl wird weggelassen **und genannt**; Test auf 52 Themen (50 bleiben, `Dropped` = langes Thema + T49 + T50), Mutante „Überzahl unbenannt“ → **1 failed**. 🟢 ohne Zählung: Wortgrenze auch an Zeilenumbruch/Tab und wenn die Grenze selbst auf Leerraum fällt; „mid-surrogate“ aus dem Kommentar gestrichen (im Zweig ohne Leerraum wird geteilt, laut Story akzeptiert); `using System.Net` statt `global::`; „höchstens 2000“; Grenzwert 200 bleibt (Test). `Fill_*` **10/10**, Suite **970/970 `exit=0`**. **Rollengang:** Konsolen-Agent ohne Oberfläche, die Tests sind der Weg; Vater/Sohn Zeugen über die Suite. `abgenommen`; Commit `4a3fa61`.
