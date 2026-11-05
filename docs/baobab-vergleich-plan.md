---
tags: [typ/plan, bereich/gamification, bereich/training, bereich/klassenarbeit, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Baobab-Vergleich, Baobab, Heftseite zur Übung, Sammelalbum, Lob-Kanal]
---

# Baobab als Vergleichsmaßstab: was wir übernehmen, was nicht

Status: **Plan** (2026-11-05). Die Übernahme läuft als Stories im [Backlog](backlog/README.md); die
Sammel-Story ist [B-374](backlog/B-374-baobab-vergleich-sammel-story.md). Dieses Dokument hält den
Vergleich und die Reihenfolge, **nicht** den Zustand der Etappen.

← [Zurück zur Wissenskarte](obsidian.md) · Vorgänger-Vergleiche: [Khan Academy](khan-academy-vergleich-plan.md),
[Knowunity](knowunity-vergleich-plan.md) · Fachmodell: [grundprinzip.md](grundprinzip.md)

## Warum dieser Vergleich

[Baobab](https://www.baobab-labs.com/) ist eine französische Revisions-App für die Mittelstufe (Collège,
etwa Klasse 6 bis 9; laut FAQ auch Grundschulende bis Oberstufe). Eigenangaben: 250.000 Familien,
4,7 Sterne im App Store bei rund 3.800 Bewertungen. Käufer sind die **Eltern**: ein Jahresabo von
65,99 € deckt bis zu drei Kinder.

Anders als Knowunity hat Baobab eine Eltern-Ebene, aber mit der entgegengesetzten Haltung zu Pugling:
Eltern sollen **ermutigen statt kontrollieren**, Selbstvertrauen gilt als Antrieb des Lernens. Damit ist
Baobab das Gegenmodell zu unserer Kernidee „Vater erzwingt Lernerfolg" – und gerade deshalb lehrreich.

Quellenstand: Website, Über-uns-Seite, Blog und App-Store-Eintrag, abgerufen am 2026-11-05 (Links am
Ende). Die App selbst wurde nicht mit Konto durchgespielt; Aussagen über Baobab sind Anbieterangaben.

## Was Baobab anbietet

- **Heftseite zur Übung.** Das Kind fotografiert eine Lektion; die KI macht daraus Lernzettel,
  Karteikarten (Vorder-/Rückseite) und Quizze mit steigender Schwierigkeit – nach den Definitionen und
  dem Format der Lehrkraft. Werbeversprechen: genau das, was in der Arbeit drankommt.
- **„InterroBao".** Eine KI stellt offene Fragen mündlich, wie eine Abfrage im Unterricht.
- **Spielelemente.** Währung „Babz", **Sammelkarten** mit Affen-Figuren, Herausforderungen gegen
  Freunde; laut Presse auch XP und die Planung des nächsten Quiz-Termins.
- **Eltern.** Ein Dashboard zeigt in Echtzeit, was sitzt und was noch nicht; Eltern können
  Herausforderungen stellen.

## Gemeinsamkeiten

| Baobab | Pugling | Bewertung |
| --- | --- | --- |
| Eltern stellen Herausforderungen | `Mission` je Kind mit Metrik, Ziel, Periode, Belohnung (`Models/GamificationEntities.cs:15`) | Gleichwertig; bei uns zusätzlich Pflicht und Malus. |
| Babz als Währung | Münzen (Lernen → Familien-Shop) und Gems (Boni → Skins) | Wir haben zwei Währungen und einen realen Gegenwert. |
| Sammelkarten | Skins aus dem `SkinCatalog` (`Models/AdminEntities.cs:185`), am Kind `OwnedSkins` | Baobab sammelt in **Sets** – das trägt länger als einzelne Skins (→ B-376). |
| Eltern-Dashboard „sitzt / sitzt nicht" | Tagesübersicht als Tabelle, Lernstand je Kind in Reitern | Baobab einfacher lesbar; Beleg für [B-39](backlog/B-39-supervisor-dashboard.md). |
| Familienabo für drei Kinder | mehrere Kinder, mehrere Supervisor, gemeinsames Wallet | Wir sind weiter. |
| Quizze mit steigender Schwierigkeit | Leitner-Stufe je Item, viele typisierte Übungen | Wir haben die reichere Lernmechanik. |

## Wo Baobab weiter ist

1. **Der Stoff kommt aus dem eigenen Unterricht.** Kein Autor nötig, und der Stoff passt zur nächsten
   Klassenarbeit. Bei uns entsteht er über den Creator oder den KI-Creator aus Briefing und
   Lehrwerks-Unit; [B-251](backlog/B-251-foto-der-vokabelliste-in-den-store.md) deckt nur
   Vokabellisten ab. Eine App-Store-Rezension benennt Baobabs Lücke genau: der Elternteil will prüfen,
   ob der Scan alles erfasst hat, und den Lernzettel anpassen.
2. **„Was kommt in der Arbeit dran" ist das Verkaufsargument.** Das stützt
   [B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md): unsere `Klassenarbeit` trägt ein
   Datum (`Models/KlassenarbeitEntities.cs:79`), aber keine `.tsx`-Datei des Frontends liest es.
3. **Mündliche Abfrage.** Wir haben keine Form der offenen, gesprochenen Antwort.
4. **Ein Eltern-Dashboard, das man auf einen Blick versteht.**
5. **Sammeln in Sets** als langfristige Belohnung.
6. **Ein positiver Kanal der Eltern.** Baobab verspricht Eltern Entlastung: loben statt korrigieren.
   Unser Vater-Web meldet vor allem Pflicht, Zielverfehlung und Malus.

## Wo Pugling stärker ist

- **Pflicht mit Konsequenz.** Pflichtziele, `PenaltyCoins`, Klassenarbeiten – Baobab setzt allein auf
  Motivation.
- **Anti-Cheat.** Der Server ist autoritativ, Lösungsfelder sind vor dem Studenten gegated. Baobab
  nennt keine Sicherung für die Richtigkeit der KI-Ergebnisse.
- **Gezielte Förderung.** KI-Creator-Fördermodus ([B-21](backlog/B-21-ki-creator-foerdermodus.md),
  abgenommen); Baobab schreibt in einer Store-Antwort, gezielte Quizze zu nicht beherrschten Themen
  seien noch in Arbeit.
- **Kuratierter Stoff aus dem Lehrwerk** ([B-12](backlog/B-12-uebungen-kuratieren.md), abgenommen)
  und **kein Abo**.

## Bewusst nicht übernommen

- **Scan und KI-Auswertung beim Kind.** Bricht „kein LLM im Backend" und öffnet einen Weg zum
  Hausaufgaben-Löser. Übernommen wird nur die Creator-Form mit Prüfung durch den Vater (B-375).
- **KI bewertet die offene Antwort des Kindes.** Bricht dieselbe Entscheidung und die Server-Autorität.
  Auch eine Selbsteinschätzungs-Form wurde verworfen (B-378): die Stufe gibt es schon (B-70).
- **Herausforderungen gegen Freunde oder Geschwister.** Der Nutzer hat am 2026-10-23 entschieden, dass
  das Kind sich nur mit der eigenen Vorwoche vergleicht (G-6 in
  [B-253](backlog/B-253-wochenvergleich-familie-statt-liga.md)). Baobab ändert daran nichts.
- **Abo-Modell.**
- **Gerätesperre**, von einem Baobab-Rezensenten gewünscht: Betriebssystem-Ebene, außerhalb unseres
  Bereichs.

## Übernahmeplan

Backend zuerst (API-First); jede Story läuft die normale Kette im Backlog. Die Beschreibung hier ist
der Anlass, nicht der Ist-Stand – beim Ausformulieren wird gegen den Code belegt.

| Etappe | Story | Was | Hängt ab von |
| --- | --- | --- | --- |
| 1 | [B-375](backlog/B-375-heftseite-wird-uebungsentwurf.md) | Foto oder PDF einer Heftseite → KI-Creator legt Übungsentwürfe an, der Vater prüft vor dem Zuweisen | keine; verallgemeinert [B-251](backlog/B-251-foto-der-vokabelliste-in-den-store.md) |
| 2 | [B-377](backlog/B-377-lob-kanal-des-vaters.md) | Der Vater schickt Lob; Erfolge des Kindes erreichen den Vater | keine |
| 3 | [B-376](backlog/B-376-sammelalbum-fuer-gems.md) | Sammelalbum mit Karten-Sets für Gems, Bonus für ein komplettes Set | berührt [B-221](backlog/B-221-level-leiter-fuer-auszeichnungen.md) |
| 4 | [B-378](backlog/B-378-muendliche-abfrage-als-selbsteinschaetzung.md) | Prüfauftrag: mündliche Abfrage als Selbsteinschätzung ohne KI-Bewertung – **verworfen** am 2026-11-05 ([Protokoll](pm-sitzung-2026-11-05-delegation.md)) | — |

Dazu stützt der Vergleich die Priorität zweier bestehender Stories, ohne eine neue anzulegen:
[B-248](backlog/B-248-klassenarbeit-countdown-treibt-den-plan.md) (Klassenarbeit treibt den Plan) und
[B-39](backlog/B-39-supervisor-dashboard.md) (Supervisor-Dashboard; „sitzt / sitzt nicht" als
einfachste Ansicht).

Etappe 1 ist der Kern: sie übernimmt Baobabs stärkste Idee in einer Form, die zu Pugling passt – der
Stoff kommt aus dem Unterricht, aber über den Creator und mit Prüfung, nicht direkt beim Kind.
Etappe 2 ist die Lehre aus Baobabs Haltung: sie ergänzt die Pflicht um einen positiven Kanal, ohne sie
aufzugeben.

## Quellen

- [baobab-labs.com](https://www.baobab-labs.com/) · [Über uns](https://www.baobab-labs.com/a-propos) ·
  [Blog](https://www.baobab-labs.com/blog)
- [App-Store-Eintrag FR](https://apps.apple.com/FR/app/id6503043086) (Funktionsbeschreibung,
  In-App-Käufe, Rezensionen)
- [presse-citron.net](https://www.presse-citron.net/telecharger/loisirs/education_et_scolarite/baobab-labs-lecons-devoirs.html) ·
  [journaldugeek.com](https://www.journaldugeek.com/telecharger/loisirs/education_et_scolarite/baobab-labs-lecons-devoirs.html)
  (XP, Quiz-Planung, Freunde herausfordern)

---

**Verwandt:** [knowunity-vergleich-plan.md](knowunity-vergleich-plan.md) ·
[khan-academy-vergleich-plan.md](khan-academy-vergleich-plan.md) · [grundprinzip.md](grundprinzip.md) ·
[docs/backlog/](backlog/README.md)
