---
tags: [typ/konzept, bereich/doku, bereich/gamification, bereich/klassenarbeit, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Leitbild Privatlehrer, Privatunterricht, Täglicher Lehrplan, Notenstufe]
---

# Leitbild: vom Einheitsplan zum Privatlehrer

Festgehalten am 2026-11-19 aus einem Gespräch mit dem Vater. Das Dokument beschreibt die **Richtung**, in die Pugling
sich entwickeln soll, und gleicht sie mit dem Stand an diesem Tag ab. Es ist **kein** Abbild des heutigen Codes – das
bleiben [grundprinzip.md](grundprinzip.md) und die `CLAUDE.md`-Dateien, bis der Umbau gebaut ist. Die einzelnen
Bausteine liegen als Stories im [Backlog](backlog/README.md) (Liste unten).

← [Wissenskarte](obsidian.md) · Heutiges Rollenmodell: [grundprinzip.md](grundprinzip.md)

---

## Die Wende in einem Absatz

Ursprünglich war ein Lehrplan gedacht, der wie bei Duolingo einmal erstellt und dann abgearbeitet wird. **Diese Idee
ist verworfen.** Pugling soll eher **Privatunterricht** sein: Ein Lehrer-Agent kennt den Leistungsstand des Kindes, den
nächsten Klausurtermin und dessen Stoff und legt daraus **jeden Tag neu** fest, was heute dran ist – *just in time*,
einmal am Tag, je Fach ein eigener Plan. Der Umfang richtet sich danach, was bis zum Ziel noch fehlt. Gibt es keine
Klausur, läuft ein freier Plan, damit das Kind im Stoff bleibt.

## Was unverändert bleibt

- **Jeden Tag wird etwas getan.** Die tägliche Pflicht mit dem Stick (`PenaltyCoins`, Nacharbeit bis erledigt, Serie)
  ist der Grundgedanke und bleibt. Ein Plan, der jeden Tag neu entsteht, darf ihn nicht aufweichen – und auch nicht
  unfair machen (eine Pflicht, die erst heute früh entstand, kostet nicht dasselbe wie eine angekündigte).
- **Belohnung mit Münzen**, die das Kind im Familien-Shop ausgibt; Gems für Skins.
- **Alle Motivationsschritte:** Auszeichnungen, Missionen, Truhe, Serie, Animationen, Ton und Haptik.

## Die Rollen danach

| Rolle | heute | künftig |
| --- | --- | --- |
| **Vater** | Creator **und** Supervisor: legt Übungen und Pläne an, setzt Ziele, Klassenarbeiten, Shop | **nur Belohnung**: füllt den Shop, löst gekaufte und aktivierte Artikel ein, ggf. Taschengeld |
| **Lehrer-Agent** (Privatlehrer, KI) | Konsolen-App, die man von Hand startet; legt Pläne **inaktiv** an | analysiert Lernstand und Lernfortschritt, erstellt die Übungen und den täglichen Plan je Fach |
| **Kind** | lernt, erfüllt Pflichten, verdient und kauft | unverändert |

Offen ist, ob und wie der Vater noch **eingreifen darf**, wenn der Agent Unsinn plant – und was an die Stelle der
heutigen Sicherung „ungeprüfter KI-Inhalt wird nicht über Nacht zur Pflicht“ tritt.

## Motivation: Notenstufe statt Liga

Eine Liga gegen andere Kinder gibt es **nicht** (das bleibt aus G-6 vom 2026-10-23: kein Vergleich mit Fremden). An
die Stelle der Liga-Idee tritt eine **Notenstufe je Fach**: Das Kind ist auf dem Stand eines Einser-, Zweier- … bis
Sechser-Schülers.

- Der Lehrer-Agent weiß, welchen Stand ein Schüler für eine 1 braucht, und sagt: „Aktuell bist du auf dem Stand eines
  Zweier-Schülers.“ Das Kind steigt auf, indem es mehr oder schwerere Übungen schafft.
- **Die echte Klausur verifiziert das.** Die korrigierte Arbeit wird eingescannt; je nach Note steigt das Kind auf
  oder ab. Die Notenstufe darf also **sinken** (Entscheidung des Vaters, 2026-11-19).
- Die Notenstufe ist zugleich das **Ziel des täglichen Plans**: Der Agent plant so, dass die Stufe bis zum
  Klausurtermin die Zielnote erreicht. Planer und Motivation sind damit ein System, nicht zwei Features.

Zu klären (in den Stories): wie die unteren Stufen dem Kind gegenüber heißen („Fünfer-Schüler“ kann gerade die
Motivation zerstören, um die es geht), wie eine Klausurnote mit der Prognose verrechnet wird (Tagesform), und dass der
Lernstand dafür nur aus serverseitig gemessenen, getippten Leistungen entstehen darf – sonst lernt sich das Kind zur 1.

## Abgleich mit dem Stand am 2026-11-19

| Baustein | Stand | Lücke |
| --- | --- | --- |
| Individueller Plan je Kind | ✅ `StudyPlan` je Kind, Positionen mit eigenem Ziel und Leitner-Stufe | – |
| Ein Plan je Fach | ✅ ein aktiver Plan je Kind und Fach (B-539) | Tagesbudget über Fächer: B-618 |
| Täglich neu erzeugter Plan | ❌ Plan ist statisch; täglich wandern nur Dosis, Leitner-Fälligkeit, Tagesmission | B-680 |
| Klausur treibt den Plan | ⚠️ `Klassenarbeit` hat Termin, Bündel und Note, wirkt aber auf nichts | B-248, B-680 |
| Freier Plan ohne Klausur | ⚠️ nur von Hand | B-680 |
| Lehrer-Agent | ⚠️ `Pugling.Agent.Creator` kann `create`/`plan`/`exam`/`assign`, läuft aber nur auf Zuruf | B-680, B-630 |
| Vater nur Belohnung | ❌ Pläne und Shop liegen beide unter `supervisor/…` | B-681 |
| Tägliche Pflicht mit Stick | ✅ | – |
| Shop, Münzen, Einlösen | ✅ (Shop je Kind in Arbeit, Sprint 96) | B-645, B-665 |
| Auszeichnungen, Animation, Haptik | ✅ Grundlage (`Celebration`, `lib/feedback.ts`) | B-221 |
| Notenstufe statt Liga | ❌ | B-682, B-683, B-684 |

## Stories

| Id | Baustein |
| --- | --- |
| [B-680](backlog/B-680-taeglicher-lehrplan-je-fach-just-in-time.md) | Der Lehrer-Agent erzeugt jeden Tag den Plan je Fach aus Klausur, Stoff und Lernstand |
| [B-681](backlog/B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md) | Rollenschnitt: der Vater belohnt, der Lehrer(-Agent) unterrichtet |
| [B-682](backlog/B-682-notenstufe-je-fach-statt-liga.md) | Notenstufe je Fach: Einser- bis Sechser-Schüler |
| [B-683](backlog/B-683-notenstufe-an-der-echten-klausur-eichen.md) | Die echte Klausurnote lässt aufsteigen oder absteigen und eicht die Prognose |
| [B-684](backlog/B-684-korrigierte-klausur-einscannen.md) | Die korrigierte Klausur einscannen: Note und Fehler erfassen |

Ersetzt: [B-595](backlog/B-595-rang-der-nie-sinkt.md) (Rang, der nie sinkt) – die Notenstufe darf sinken.
Weiter gültig und verwandt: [B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md),
[B-249](backlog/B-249-probeklausur-mit-notenprognose.md), [B-253](backlog/B-253-wochenvergleich-familie-statt-liga.md)
(Wochenvergleich bleibt), [B-618](backlog/B-618-faecher-verteilung-backend.md),
[B-630](backlog/B-630-rueckmeldung-im-ki-creator.md).

---

Verwandt: [grundprinzip.md](grundprinzip.md) · [endpunkt-beziehungen.md](endpunkt-beziehungen.md) ·
[pflicht-malus-schenkung-plan.md](pflicht-malus-schenkung-plan.md)
