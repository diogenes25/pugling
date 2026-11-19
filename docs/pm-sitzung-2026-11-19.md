---
tags: [typ/protokoll, bereich/backlog, bereich/shop]
---

# PM-Sitzung: Shop je Kind (Sprint 96)

**Datum:** 2026-11-19  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Der Familien-Shop zählt je Kind – eigener Bestand, eigenes Taschengeld, monatliches Auffüllen –, und der
Vater kann ausnahmsweise selbst aus dem Inventar einlösen.

## Runde 1 — Ausgangslage

Aufruf `/pm-loop` ohne eigenes Ziel. Laut Roadmap des Vortags ([Runde 8](pm-sitzung-2026-11-18.md)) wären B-672 und
Sprint 90 dran. Der Vater hat aber am 2026-11-18 und heute im Chat zwei Wünsche aus dem **echten Betrieb**
nachgereicht; die sind beobachtet und gehen vor (Step 2.1). Kein neuer Befund unter `docs/anmerkungen/`.

- **Vater (O-Ton, Chat 2026-11-19):** „Shops müssen pro Kind sein: Theodor bekommt 10 €, Friedrich 20 €, je einmal im
  Monat; Daddelzeit 1 h täglich je Kind; keiner darf dem anderen etwas wegkaufen." Und (2026-11-18): „Das Kind will
  einlösen, hat aber das Handy nicht dabei – ich will dann selbst aus seinem Inventar einlösen können, als Ausnahme."
- **Sohn:** am Code belegt, nicht erfunden – kauft Friedrich das Monats-Taschengeld, ist es für Theodor weg
  (`ShopService.cs:147`, `:174`); ein wöchentliches Angebot fällt aus, wenn am Stichtag keiner den Shop öffnet.
- **Creator:** kein Pfad in diesem Sprint.

Entscheidungen des Vaters in dieser Runde: Sprint „Shop je Kind" statt Sprint 90; B-645 Punkt 12 → **Zielgruppe am
Angebot** (Entscheidung 12); B-665 → alle sechs Empfehlungen (gegrillt und geschätzt, M).

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Warum jetzt |
| --- | --- | --- | --- | --- |
| P2 | [B-645](backlog/B-645-taschengeld-und-auffuell-plan-fuer-alle-angebote.md) | L | beides | Wunsch aus dem Betrieb; enthält zwei Defekte (Wöchentlich holt nicht nach, Formular ohne Wochentag) |
| P2 | [B-665](backlog/B-665-vater-loest-aus-dem-inventar-des-kindes-ein.md) | M | beides | Wunsch aus dem Betrieb; derselbe Bereich, derselbe Rollengang |
| P2 | [B-672](backlog/B-672-startverlegung-zurueck-holt-unspielbare-tage-zurueck.md) | — | backend | nächste Sitzung (Umgehung: Planstart nicht zurückverlegen) |
| P1 | [B-580](backlog/B-580-plan-aus-unit.md) | L | beides | Sprint 90, danach |

**Roter Faden:** Der Shop gehört jedem Kind einzeln.

## Sprint 96 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich lege für Theodor und Friedrich je ein eigenes monatliches Taschengeld an; jeder
kauft nur aus seinem eigenen Bestand, der sich am 1. von selbst auffüllt – auch wenn keiner reingeschaut hat –, und
wenn ein Kind ohne Handy vor mir steht, löse ich selbst aus seinem Inventar ein und es sieht das hinterher."

**Umfang:** B-645 (L), B-665 (M). Zwei Migrationen, nacheinander in **einer** Kette.

**Entwickler-Brief:**

- **Reihenfolge:** B-645 Backend (Stichtag-Funktion → Schema/Migration → `ShopService` → Vertrag/Controller →
  Client) → B-665 Backend → Frontend beider Stories → E2E.
- **Quelle der Wahrheit:** der Bestand je Kind in `ShopListingStock`; die **Senke** jedes Auffüllens ist eine
  Funktion (`ShopService.RefillStock`), die alle vier Auffüll-Stellen (zwei Listen, Kauf, Einzelansicht) rufen, und die
  Startregel (Entscheidung 11) eine zweite neben ihr, die Migration und Laufzeit teilen.
- **Guards:** Wallet-Invariante beim Kauf (`child.ConcurrencyStamp`); Sperre am Kind-Bestand statt am Angebot;
  Zielgruppe filtert **jeden** Sohn-Pfad (Liste und Kauf); Ausstellerbindung beim Einlösen durch den Vater.
- **Migration:** Daten der Söhne – Probelauf auf einer Kopie der echten `pugling.db`, Migrationstest im Altzustand.
- **Testweg:** wie in den Stories; Rollengang Vater **und** Sohn als E2E `taschengeld-monatlich.spec.ts`.
- **Reviewer:** `pugling-reviewer` und `frontend-reviewer`.
