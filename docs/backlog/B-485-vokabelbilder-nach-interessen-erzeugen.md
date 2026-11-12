---
tags: [typ/story, status/gegrillt, bereich/medien, bereich/katalog, lerntechnik/vokabeln, rolle/creator, rolle/student]
aliases: [Vokabelbilder erzeugen, KI-Bilder nach Interessen, Medien Etappe 7b]
status: gegrillt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: Auftrag des Nutzers 2026-11-12 („kleine Bilder je Vokabel, nach den Interessen des Kindes")
wartet_auf: ""
grund: ""
ersetzt_durch: []
---

# B-485 · Kleine Bilder zu jeder Vokabel – erzeugt nach den Interessen des Kindes

Ein Kind, das Fußball mag, sieht bei *to jump* einen Torwart im Sprung; eines, das Roboter mag, einen
springenden Roboter. Dieselbe Vokabel, verschiedene Bilder, jedes Kind bleibt bei *seinem* Bild.

## User Story

Als Vater möchte ich, dass die Vokabelkarten meiner Söhne kleine Bilder zeigen, die zu ihren Interessen
passen, ohne dass ich jedes Bild selbst suche und hochlade, damit sie sich die Wörter über ein Motiv merken,
das sie anspricht.

## Ist-Stand am Code

Die Hälfte, die man für schwer halten würde, **steht schon** ([medien-bilder.md](../medien-bilder.md), Etappen
1–6 ✅). Es fehlt nur die Quelle der Bilder.

| Baustein | Stand | Fundstelle |
| --- | --- | --- |
| Geteilte Interessen-Taxonomie (Kind **und** Bild referenzieren dieselben Tags, Gewicht −3…+3, Abneigung schließt aus) | ✅ | `backend/Pugling.Api/Models/InterestEntities.cs:16`, `:56` |
| Auswahl je Kind: Kandidaten → harter Filter (Rating, Abneigung) → eingefrorene Wahl → Punktzahl über die Tag-Schnittmenge (Thema ×2, Stil ×1) → einfrieren | ✅ | `backend/Pugling.Api/Services/Shared/MediaSelector.cs:55`, `:229`, `:239` |
| Bild ohne Tag ist wählbar (Punktzahl 0) – also ein Grundbestand für jedes Kind | ✅ | `MediaSelector.cs:229` (kein Tag-Zwang), `:239` |
| Eingefrorene Wahl gewinnt immer (Merkeffekt) | ✅ | `MediaSelector.cs:209` |
| „Anderes Bild" (Reshuffle, abgelehnt = nie wieder) | ✅ | `MediaSelector.cs:99` |
| Bild nur auf ungetippten Stufen (Anti-Cheat) | ✅ | `backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:121` |
| Bild auf der Sohn-Karte | ✅ | `frontend/src/sohn/SohnPractice.tsx:371` |
| Upload mit serverseitigen Varianten (Thumb/Card/Full) | ✅ | `POST creator/media/upload`, `backend/Pugling.Api/Controllers/Creator/MediaAssetsController.cs:214` |
| Herkunft „KI-generiert" samt Prompt/Modell | Feld da, nie benutzt | `MediaOrigin.Generated`, `MediaAsset.Source` (`backend/Pugling.Api/Models/MediaEntities.cs:42`, `:45`) |
| Bild-Schnittstelle im Agenten | Abstraktion da | `Microsoft.Extensions.AI` 10.8.1 (`backend/Pugling.Agent.Creator/Pugling.Agent.Creator.csproj:27`) bringt `IImageGenerator` mit; die OpenAI-Anbindung liegt im Paket `Microsoft.Extensions.AI.OpenAI` (noch nicht referenziert) |
| Erzeuger | **fehlt** | Etappe 7 in [medien-bilder.md](../medien-bilder.md) „offen" |

Hardware (nachgesehen 2026-11-12): RTX 4050 Laptop, 6 GB, geteilt mit Ollama – Grund für Entscheidung 1.

## Die echte Lücke

1. **Niemand erzeugt Bilder.** Der Store ist heute nur so voll, wie ein Creator von Hand hochlädt – bei
   einigen hundert Wörtern je Lehrwerk und mehreren Interessen je Kind passiert das nie.
2. **Niemand weiß, welche Bilder fehlen.** Es gibt keine Abfrage „Wörter im aktiven Plan von Kind X, für die
   kein Bild mit einem seiner Lieblings-Tags existiert".
3. **Ein Tag weiß nicht, wie man ihn malt.** `InterestTag` hat Slug, Label und Synonyme, aber keinen
   bildtauglichen, markenneutralen Prompt-Baustein („pokemon" darf nicht zu Pikachu werden).

## Lösungsskizze

**Grundsatz (wie B-46): Bilder entstehen je Vokabel × Interessen-Tag, nicht je Kind.** Ein zweites Kind mit
denselben Interessen bekommt dieselben Bilder; der Erzeuger sieht nie Name, Alter oder Freitext eines Kindes,
nur Wort, Übersetzung und Tags. Die Zuordnung zum Kind macht weiter allein der `MediaSelector` – an ihm
ändert sich nichts.

### Etappe 1 · Bedarf abfragen (Backend)

- `GET api/v1/creator/media/demand?childId=&max=` (Creator-Route, aber Betreuungs-Prüfung über `AuthAccess`
  wie `profiles/match`, B-310): Vokabeln aus den Positionen des aktiven Plans, die entweder **kein neutrales
  Bild** haben oder kein Bild zu einem der **zwei stärksten Interessen (Gewicht ≥ 2)** des Kindes. Antwort
  enthält nur `vocabularyId`, Wort, Übersetzung, Hinweis, `needsNeutral`, `wantedTagSlugs`, `allowedRating` –
  keine Kind-Daten.
- Zusätzlich `?seriesUnitId=&tag=` für „ganze Unit vorproduzieren" (kindunabhängig).
- Neues Feld `InterestTag.ImagePrompt` (Freitext, englisch, markenneutral: „fussball" → „on a soccer
  pitch, with a soccer ball"). Migration, Bestandszeilen bleiben `null`. Pflege im Creator-CRUD der
  Interessen und im Vater-Web, wo Tags gepflegt werden. Ein Tag ohne `ImagePrompt` erscheint in
  `wantedTagSlugs` mit Markierung `noPrompt`, damit der Agent ihn melden kann.

### Etappe 2 · Erzeuger als Agent-Befehl (Pugling.Agent.Creator)

Neuer Befehl `images` neben `create`/`plan`/`fill` – **kein Bild-Modell im Backend**, wie beim Text-Creator.

```text
images --child 1 [--max 20] [--dry-run]
images --unit 12 --tag fussball
```

Pipeline je Vokabel – neutrales Bild und Interessen-Bilder **im selben Lauf** (Entscheidung 4):

1. **Bebilderbar?** Konkrete Nomen und Verben ja; Funktionswörter, Abstrakta, Redewendungen nein
   („kein Treffer = kein Bild"). Entscheidung per lokalem LLM mit fester Ja/Nein-Antwort, in C# geprüft
   (Muster B-460). Nicht bebilderbar → das Wort wird für alle Tags übersprungen.
2. **Prompt bauen – deterministisch in C#**:
   `"{Motiv aus Wort+Übersetzung}[, {Tag.ImagePrompt}], {fester Stil}, plain background, no text, no letters"`.
   Das Motiv **ist** das Wort; das Interesse ist Kulisse/Figur. Homonyme über die Übersetzung auflösen.
   „No text" ist Pflicht. Tag ohne `ImagePrompt` → übersprungen und im Lauf-Bericht genannt.
3. **Bild erzeugen** über `IImageGenerator` aus `Microsoft.Extensions.AI`, zuerst mit OpenAI
   `gpt-image-1-mini` (niedrige Qualität, quadratisch). API-Schlüssel als user-secret des Agenten.
4. **Hochladen** über `POST creator/media/upload` mit `origin = Generated`, `source = "{modell}: {prompt}"`,
   `rating = Everyone`, Tag = Interessen-Tag (neutrales Bild ohne Tag), Alt-Text = Motivbeschreibung
   **ohne** das Zielwort; dann **direkt** an die Vokabel hängen (`creator/vocabulary/{id}/media`).
5. `--dry-run` erzeugt nichts im Store und schreibt nur die geplanten Prompts samt Kosten-Schätzung –
   ohne Bild-Aufruf, also kostenlos.

Neue Client-Methoden in `Pugling.Client` (je eine Zeile) für `demand` und die Zuordnung.

### Etappe 3 · Nachsorge

Keine Freigabe vor dem Kind (Entscheidung 2). Das Kind verwirft mit „anderes Bild" (gibt es), der Vater
filtert in der Bild-Bibliothek nach `origin=Generated` und löscht.

### Etappe 4 · Rollengang

Theodor (Englisch, Access Unit 1) und Friedrich (Französisch) mit je unterschiedlichen Interessen;
dieselbe Vokabel zeigt beiden verschiedene Bilder, jedes bleibt über Sitzungen gleich, getippte Stufen
zeigen keins. Erst gegen eine Kopie der `pugling.db`, dann auf der echten (Migration!).

## Entscheidungen

1. **Bildquelle: Cloud-Dienst hinter einer austauschbaren Schnittstelle** (`IImageGenerator`). Begründung:
   die RTX 4050 (6 GB) teilt sich der Bild-Erzeuger mit Ollama, lokale Modelle malen oft doch Schrift ins Bild,
   und für Kinderkarten zählt gleichmäßige Qualität. Ins Netz gehen nur Wort, Übersetzung und Interessen-Prompt.
   Kosten: API-Schlüssel und etwa 1–4 Cent je Bild; ein Anbieterwechsel ist eine neue Klasse, kein Umbau.
2. **Direkt verknüpfen, keine Freigabe vor dem Kind.** Begründung: geringster Bau, keine Migration für einen
   Freigabe-Status, „anderes Bild" ist das vorhandene Korrektiv. Kosten: Ein misslungenes Bild sieht das Kind
   mindestens einmal; die Qualitätskontrolle liegt beim Prompt-Bau (Motiv = Wort, kein Text).
3. **Menge: zwei stärkste Interessen (Gewicht ≥ 2) plus ein neutrales Bild je Vokabel.** Begründung: Jedes
   Kind hat sofort ein Bild, auch ohne gepflegte Interessen, und „anderes Bild" hat fast immer eine
   Alternative. Kosten: etwa drei Bilder je Wort, ~360 je Unit (~4–14 €).
4. **Eingefrorene Wahl bleibt; neutrales und Interessen-Bilder entstehen im selben Lauf.** Begründung:
   Bildkonstanz ist der Merkeffekt (`MediaSelector.cs:209`); am Selektor ändert sich nichts. Kosten: Kommt ein
   Interesse später dazu, sieht das Kind bei schon bekannten Wörtern weiter das alte Bild – nur
   „anderes Bild" holt das neue.
5. **Ein fester Zeichenstil für alle Bilder**, eingestellt in den `appsettings` des Agenten; Stil-Interessen
   („comic") werden nicht eigens bebildert. Begründung: einheitliche, ruhige Karten, kleine Menge. Kosten:
   Stil-Vorlieben eines Kindes bleiben unberücksichtigt.
6. **Markenneutraler `ImagePrompt` ist Pflicht für ein bebildertes Interesse.** Ohne ihn überspringt der Agent
   den Tag und nennt ihn im Bericht. Begründung: keine geschützten Figuren, kein Modell entscheidet über
   „neutral genug". Kosten: einmal je Interesse Pflegearbeit, ein neues Feld samt Migration.
7. **Erster Anbieter: OpenAI `gpt-image-1-mini`, niedrige Qualität**, über `Microsoft.Extensions.AI.OpenAI`.
   Begründung: fertige Anbindung an die schon genutzte Abstraktion, befolgt „no text" gut, reicht für kleine
   Kartenbilder. Kosten: neues Paket, OpenAI-Schlüssel als user-secret beim Nutzer (nicht aus der Sandbox
   setzbar).
8. **Stufe „Bild → Wort" bleibt draußen.** Begründung: Sie bräuchte eine Ausnahme von der Anti-Cheat-Regel
   (`VocabularyExerciseType.cs:121`), die bei B-03 bewusst nicht gelockert wurde. Kosten: Bilder bleiben reine
   Merkhilfe auf ungetippten Stufen.

## Akzeptanzkriterien

1. `demand` liefert für ein Kind genau die Vokabeln seines aktiven Plans, denen ein neutrales Bild oder ein
   Bild zu einem seiner zwei stärksten Interessen (Gewicht ≥ 2) fehlt; ein passendes Bild nimmt den Bedarf
   heraus. Rot, wenn der Tag-Abgleich oder die Gewichtsgrenze fehlt (ein Interesse mit Gewicht 1 darf nie
   erscheinen, ein drittes Interesse ebenso wenig).
2. `demand` für ein nicht betreutes Kind → 404 wie ein unbekanntes Kind.
3. `InterestTag.ImagePrompt` ist per Creator-CRUD les- und schreibbar, mit `Clear…`-Schalter (PATCH-Semantik);
   die Migration lässt Bestandszeilen auf `null`.
4. `images --dry-run` ruft weder den Bild-Dienst noch einen schreibenden Endpunkt auf; ohne `--dry-run`
   entsteht je Paar ein Asset mit `origin = Generated`, gefülltem `source`, dem Interessen-Tag (neutral: ohne
   Tag) und einer Variante `Card`, direkt an der Vokabel.
5. Zwei Kinder mit disjunkten Interessen bekommen für dieselbe Vokabel verschiedene Bilder; ein Kind ohne
   Interessen bekommt das neutrale; ein Kind mit Abneigung gegen einen Tag sieht dessen Bild nie
   (Integrationstest über die Karten-Ausspielung mit generierten Assets).
6. Ein zweiter Lauf erzeugt nichts doppelt (idempotent über `demand`).
7. Nicht bebilderbare Wörter und Tags ohne `ImagePrompt` werden übersprungen und im Lauf-Bericht gezählt.
8. Prompt-Bau ist Unit-getestet: enthält Motiv, `ImagePrompt`, festen Stil und „no text"; enthält weder einen
   Kindnamen noch das Zielwort im Alt-Text.
9. Agent-Pipeline mit einem Fake-`IImageGenerator` getestet (kein Netz im Test).

## Offene Punkte (fürs Grillen)

1. ~~Woher kommen die Bilder?~~ → Entscheidung 1 und 7.
2. ~~Freigabe vor dem Kind?~~ → Entscheidung 2.
3. ~~Wie viele Tags je Kind?~~ → Entscheidung 3; Folgefrage Nachrücken → Entscheidung 4.
4. ~~Stil~~ → Entscheidung 5.
5. ~~Stufe „Bild → Wort"~~ → Entscheidung 8.
6. ~~Markennamen in Interessen~~ → Entscheidung 6.

## Verlauf

- 2026-11-12 · `ausformuliert` angelegt, Ist-Stand gegen den Code belegt.
- 2026-11-12 · `gegrillt`: acht Entscheidungen mit dem Nutzer (Cloud/OpenAI, direkt verknüpfen, Top 2 + neutral,
  Wahl bleibt, fester Stil, neutraler Prompt Pflicht, Bild → Wort draußen).
