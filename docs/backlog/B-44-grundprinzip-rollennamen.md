---
tags: [typ/story, status/idee, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Rollennamen, Vater ist keine Ebene]
status: idee
prio: P2
art: Aufräumen
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
unverifiziert: true
---

# B-44 · Grundprinzip auf Supervisor/Student umschreiben — „Vater" ist keine Ebene

[docs/grundprinzip.md](../grundprinzip.md) nennt die Ebenen „Creator / **Vater** / **Kind**", der Code
nennt sie durchgehend `Creator`/`Supervisor`/`Student`. Damit stehen im Startdokument der Architektur drei
Namensachsen unsortiert nebeneinander: die **Rolle** (Creator/Supervisor/Student — JWT-Claim, Routenpräfix,
Ordner), die **Entität** (`Adult`/`Child` — `fid`/`cid`, `children/{childId}`) und die **Familiensprache**
(Vater/Sohn — Oberfläche, Seed). Wer das Dokument liest, hält „Vater" und „Supervisor" für Synonyme; sie
sind es nicht: ein Vater ist *ein* Account mit *zwei* Rollen (Creator **und** Supervisor) auf *einer*
`Adult`-Zeile, ein Lehrer-Konto trägt nur Creator.

Die Doku soll die drei Achsen einmal ausdrücklich benennen und danach konsequent die Rollennamen führen;
„Vater" bleibt als **Beispiel einer Rollenkombination** und in der Oberfläche richtig, nicht als
Ebenenname. Mit zu prüfen: der Satz „Der Creator weiß nichts von einzelnen Kindern" — er stimmt für die
Entität (`Exercise` hat keine `ChildId`), aber nicht mehr für den Creator-Arbeitsplatz
(`creator/profiles/match?childId=`, der KI-Creator brieft heute auf ein konkretes Kind). Wohin dieser Satz
fachlich wandert, entscheidet [B-46](B-46-interessenbasierte-uebungen.md).

Reine Doku-Arbeit; verwandt, aber nicht dasselbe wie [B-32](B-32-father-tabellenname.md) (dort geht es um
den Tabellennamen im Schema).

## Verlauf

- **2026-07-31** — angelegt (Quelle: Rollen-Abgleich in der Sitzung, Nutzer bestätigt).
