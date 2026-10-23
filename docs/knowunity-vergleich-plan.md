---
tags: [typ/plan, bereich/gamification, bereich/training, bereich/klassenarbeit, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Knowunity-Vergleich, Knowunity, Klausur-Countdown, Notenprognose]
---

# Knowunity als Vergleichsmaßstab: was wir übernehmen, was nicht

Status: **Plan** (2026-10-23). Die Übernahme läuft als Stories im [Backlog](backlog/README.md); die
Sammel-Story ist [B-247](backlog/B-247-knowunity-vergleich-sammel-story.md). Dieses Dokument hält den
Vergleich und die Reihenfolge, **nicht** den Zustand der Etappen.

← [Zurück zur Wissenskarte](obsidian.md) · Vorgänger-Vergleich: [Khan Academy](khan-academy-vergleich-plan.md) ·
Fachmodell: [grundprinzip.md](grundprinzip.md)

## Warum dieser Vergleich

[Knowunity](https://knowunity.de/) ist die verbreitetste Lern-App für Schüler im deutschsprachigen Raum
(Eigenangabe: über 22 Mio. Schüler:innen in Europa). Sie ist **schülerzentriert**: KI-Tutor, Lernmaterial
von Mitschülern (Eigenangabe: über 1,6 Mio. Lernzettel), Freemium mit Pro-Abo. Eine Eltern-Ebene hat
sie nicht – weder im App-Store-Text noch auf der Website ist eine Eltern- oder Betreuerfunktion
beschrieben. Damit ist sie das Gegenstück zu Pugling: dieselben Lernbausteine, aber ohne den Vater, der
Pflicht mit Konsequenz verknüpft. Interessant ist sie dort, wo sie Bausteine **zusammensetzt**, die wir
einzeln schon haben.

Quellenstand: Website, App-Store-Eintrag und Pro-Seite, abgerufen am 2026-10-23 (Links am Ende). Die
App selbst wurde nicht mit Konto durchgespielt; Aussagen über Knowunity sind Anbieterangaben.

## Gemeinsamkeiten

| Knowunity | Pugling | Bewertung |
| --- | --- | --- |
| „Personalisierte Lernpläne" (KI) | `StudyPlan` aus `PlanPosition`s mit Pflichtziel, Rhythmus, Malus | Wir sind bei der Steuerung weiter, Knowunity bei der **Terminplanung** (siehe unten). |
| Karteikarten, Quizze | Leitner-Box je Item, viele typisierte Übungen | Wir haben die reichere Lernmechanik. |
| KI-bewertete Probeklausuren | Positions-Abschlusstest, `Klassenarbeit` mit Datum und Note, KI-Creator `exam` | Bausteine vorhanden, bei uns aber unverbunden. |
| XP, Wochen-Liga (bis 30 Mitschüler), Ränge Bronze → Gold | Münzen, Gems, Missionen, Auszeichnungen | Leiter/Ränge liegen als [B-221](backlog/B-221-level-leiter-fuer-auszeichnungen.md) vor. |
| Serie („Halte deine Serie am Laufen") | Tages-Streak aus [B-105](backlog/B-105-taegliche-belohnungsbox.md) | Gleichwertig; Taktungsfrage in [B-222](backlog/B-222-wochen-streak-als-default.md). |
| Stundenplan hinzufügen, Plan baut darum herum | `TimetableEntry` (Wochentag → Fach) | Knowunity **nutzt** ihn; bei uns liest ihn kein Service. |
| Fortschritt je Thema „von wackelig zu sicher" | Vokabel-Lernstand flach und hierarchisch | Wir sind feiner, Knowunity zeigt es dem Schüler sichtbarer. |
| Sprach-Wiederholung, Sprachmodus im Chat | Übungstyp Hörverstehen mit hochgeladenem Audio | Knowunity weiter. |
| Creator-Programm, „verifizierte Inhalte" | Geteilte Bibliothek; [B-11](backlog/B-11-uebungen-veroeffentlichen.md) abgenommen, [B-12](backlog/B-12-uebungen-kuratieren.md) offen | „Verifiziert" ist bei Knowunity ein Pro-Verkaufsargument – ein Prioritäts-Beleg für B-12. |

## Wo Knowunity weiter ist

1. **Prüfungstermin treibt den Plan.** „Sag mir, wann deine Prüfung ist und was du können musst" – der
   Tagesplan verdichtet sich, je näher der Termin rückt. Unsere `Klassenarbeit` trägt `ScheduledDate`
   und ein Übungsbündel (direkt und über Tags), wirkt aber auf nichts: der Plan ändert sich nicht, und die
   Sohn-App zeigt die Arbeit gar nicht an.
2. **Probeklausur mit Rückkopplung.** Vollständige, bewertete Probeklausuren, deren Ergebnis in den Plan
   zurückfließt. Wir haben Abschlusstests je Position und die echte Note an der Klassenarbeit, aber keine
   Probe über das ganze Bündel und keine Prognose.
3. **Stundenplan im Plan.** Der Plan kennt die Schulwoche. Bei uns ist der Stundenplan reine Ablage.
4. **Material wird zur Übung.** Upload von PDF, Folie, Foto oder Handschrift → Karteikarten, Quiz,
   Zusammenfassung. Unser KI-Creator arbeitet aus Briefing und Lehrwerks-Unit, nicht aus einer Vorlage.
5. **Sprache.** Vorlesen und Abfragen per Stimme. Bei uns gibt es Audio nur, wenn ein Creator es hochlädt.
6. **Soziale Taktung.** Wochen-Liga mit Mitschülern als Motivator.

## Wo Pugling stärker ist

- **Die Eltern-Ebene.** Pflicht mit Konsequenz (`PenaltyCoins`), Multi-Supervisor mit gemeinsamem Wallet,
  Familien-Shop mit realem Gegenwert – Knowunity hat nichts davon.
- **Anti-Cheat.** Knowunitys Foto-Lösung „Schritt für Schritt" und der KI-Chat sind faktisch ein
  Hausaufgaben-Löser. Bei uns ist der Server autoritativ, und Lösungsfelder sind vor dem Studenten gegated.
- **Kein Abo-Druck.** Neun In-App-Käufe im deutschen Store; Nutzerstimmen berichten von ungewollten
  Abbuchungen nach dem Probeabo.
- **Lehrwerk statt Masse.** Inhalte hängen am Lehrwerk des Kindes; Knowunitys Hauptschwäche ist die
  schwankende Qualität des Schülermaterials.

## Bewusst nicht übernommen

- **KI-Tutor mit Foto-Lösung für das Kind.** Bricht Anti-Cheat und die Entscheidung „kein LLM im
  Backend". Die passende Form der Hilfe ist der gestufte Hinweis aus
  [B-223](backlog/B-223-gestufte-hinweise-vor-der-antwort.md).
- **Liga mit fremden Mitschülern.** Für ein Kind um 11 Jahre ohne Moderation nicht vertretbar; nur eine
  familien- oder selbstbezogene Variante (Etappe 6).
- **Nutzer-Content-Masse und Freemium.** Passt nicht zu einer Familien-App; übernommen wird nur die
  Lehre, dass Verifikation ein Wert ist (→ B-12).
- **Zusammenfassungen/Lernzettel als eigener Inhaltstyp.** Der Ausspiel-Modus „Info" deckt das
  Vorstellen von Stoff ab; wie beim Khan-Vergleich bleibt die App übungszentriert.

## Übernahmeplan

Backend zuerst (API-First); jede Story läuft die normale Kette im Backlog. Die Beschreibung hier ist
der Anlass, nicht der Ist-Stand – beim Ausformulieren wird gegen den Code belegt.

| Etappe | Story | Was | Hängt ab von |
| --- | --- | --- | --- |
| 1 | [B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md) | Klassenarbeit sichtbar beim Kind (Countdown, Bereitschaft) und verdichtend im Plan | keine |
| 2 | [B-249](backlog/B-249-probeklausur-mit-notenprognose.md) | Probeklausur über das Bündel einer Klassenarbeit mit Notenprognose, Abgleich mit der echten Note | Etappe 1 (Bündel-Auflösung, Sichtbarkeit) |
| 3 | [B-250](backlog/B-250-stundenplan-in-der-tagesmission.md) | Stundenplan gewichtet die Tagesmission („morgen Englisch") | keine; berührt [B-220](backlog/B-220-learning-queue-im-sohn-dashboard.md) |
| 4 | [B-251](backlog/B-251-foto-der-vokabelliste-in-den-store.md) | Foto/PDF einer Vokabelliste → KI-Creator legt Einträge im Vokabelspeicher an | keine; Creator-Ebene |
| 5 | [B-252](backlog/B-252-vorlesen-und-sprechen-bei-vokabeln.md) | Vorlesen der Vokabel (Browser-TTS), optional Spracheingabe | Vorbehalt [B-31](backlog/B-31-geraete-vorbehalt-klang.md) |
| 6 | [B-253](backlog/B-253-wochenvergleich-familie-statt-liga.md) | Wochenvergleich gegen die eigene Vorwoche oder Geschwister statt Liga | Druck-Frage wie [B-222](backlog/B-222-wochen-streak-als-default.md); darf `verworfen` werden |

Etappen 1 und 2 sind der Kern: sie verbinden Bausteine, die schon da sind, zu genau dem, was die
Kernidee verlangt – der Vater setzt den Termin, die App erzwingt die Vorbereitung. 3 bis 6 sind
voneinander unabhängig.

## Quellen

- [knowunity.de](https://knowunity.de/) · [Knowunity Pro (Free vs. Pro, Preise)](https://knowunity.de/pro-plan)
- [App-Store-Eintrag DE](https://apps.apple.com/DE/app/id1484296272) (Funktionsbeschreibung, In-App-Käufe)
- [Mistral-Fallstudie](https://mistral.ai/customers/knowunity/) · [moge.ai-Produktübersicht](https://moge.ai/de/product/knowunity)
- [synt.app – Preisübersicht](https://synt.app/blog/en/knowunity-alternatives.html) ·
  [gutefrage.net – Nutzererfahrungen](https://www.gutefrage.net/frage/habt-ihr-erfahrungen-mit-der-app-knowunity)

---

**Verwandt:** [khan-academy-vergleich-plan.md](khan-academy-vergleich-plan.md) ·
[grundprinzip.md](grundprinzip.md) · [docs/backlog/](backlog/README.md)
