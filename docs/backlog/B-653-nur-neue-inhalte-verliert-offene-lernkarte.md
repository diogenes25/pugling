---
tags: [typ/story, status/idee, lerntechnik/vokabeln, rolle/student]
aliases: [Scope New und Lernkarte, offene Lernkarte verschwindet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "PM-Sitzung 2026-11-18, Nachschau von B-643 (e10cdd34)"
entgangen_bei: [B-643]
grund: ""
ersetzt_durch: []
---

# B-653 · Bei „nur neue Inhalte“ kommt ein nicht eingeprägtes Wort nie wieder als Lernkarte

## Beobachtung

Gefunden in der Nachschau von [B-643](B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md), am Code
bestätigt (`HEAD` 7c642bb9): `PositionPlayService.ScopeMatch` lässt bei `ItemScope.New` nur Inhalte durch, die nie oder
heute eingeführt wurden (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:614-616`). B-643 hat den Zustand
„eingeführt, aber nicht eingeprägt“ eingeführt, den dieser Filter nicht kennt.

**Szenario:** Position mit Inhalte-Auswahl „nur neue“. Das Kind drückt bei einem Wort „Später nochmal“ und beendet die
Runde. Ab dem Folgetag fällt das Wort aus jeder Runde der Position – es kommt nie wieder als Lernkarte, der goldene
Ring bleibt unter der Gesamtzahl.

**Widerspricht** B-643, Entscheidung 2 („kommt am nächsten Tag wieder als Lernkarte, bis es eingeprägt ist“) und AK 5.

**Warum P3:** die Plan-Skripte der Söhne setzen keinen Scope (Standard `All`); der Fall braucht eine Position, die der
Vater ausdrücklich auf „nur neue“ stellt.
