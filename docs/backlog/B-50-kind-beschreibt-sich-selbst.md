---
tags: [typ/story, status/ausformuliert, bereich/medien, bereich/frontend, bereich/auth, rolle/student, rolle/supervisor]
aliases: [Interessen vom Kind, Selbstbeschreibung, Interessen-Onboarding]
status: ausformuliert
prio: P2
art: Wunsch
quelle: Sitzung 2026-08-01 (Rollen-Durchgang, Nutzer-Entscheidung)
---

# B-50 · Das Kind beschreibt sich selbst: Interessen in einem geführten Prozess

Heute pflegt der **Supervisor** die Interessen des Kindes; das Kind hat keinen Schreibweg. Es soll sie
selbst angeben können — in einem geführten Prozess mit **Auswahllisten** statt Freitext, damit es für ein
Kind so leicht wie möglich ist. Der Anti-Cheat-Einwand greift hier nicht: Nach der Hausregel „Interessen
kleiden den Stoff ein, sie ersetzen ihn nie" ändern sie weder *welche* Inhalte geübt werden noch wie oft
oder mit welcher Schwelle. Es ist damit die einzige Stelle, an der sich das Kind ohne Risiko selbst
einbringen kann.

## User Story

Als Kind möchte ich in ein paar geführten Schritten aus Bildern und Kacheln auswählen, was ich mag und was
gar nicht, damit die Übungen und Bilder zu mir passen — ohne tippen zu müssen und ohne meine Eltern zu
fragen.

## Ist-Stand am Code

Das Fundament ist stärker als erwartet — es fehlt fast nur die Kindseite.

- **Die Taxonomie trennt Thema und Stil bereits.** `InterestTag`
  ([InterestEntities.cs:16](../../backend/Pugling.Api/Models/InterestEntities.cs#L16)) trägt eine
  `Facet`; die Werte
  ([MediaBaseTypes.cs:72](../../backend/Pugling.Contracts/Common/MediaBaseTypes.cs#L72)) sind acht
  thematische (Franchise, Sport, Animal, Vehicle, Music, Hobby, Nature, Other) **plus `Style`**
  („comic, foto, pixel art – orthogonal zum Thema",
  [:91](../../backend/Pugling.Contracts/Common/MediaBaseTypes.cs#L91)). Genau die zwei Achsen, die eine
  Auswahlliste braucht.
- **Abneigungen sind schon modellierbar.** `ChildInterest.Weight` läuft von
  `MinWeight = -3` bis `MaxWeight = 3`
  ([InterestEntities.cs:67](../../backend/Pugling.Api/Models/InterestEntities.cs#L67) ff.), Skala
  „bewusst grob, weil ein Mensch sie pflegt".
- **Ein Trichter für Text → Tag existiert**, samt Synonymen und Duplikat-Schutz:
  [InterestTagService.cs:25](../../backend/Pugling.Api/Services/Shared/InterestTagService.cs#L25)
  (`EnsureAsync` / `EnsureManyAsync`). Drei Pfade laufen dort schon zusammen (Creator taggt ein Bild,
  Supervisor tippt ein Interesse, Backfill übernimmt Freitext) — ein vierter (Kind wählt aus) wäre kein
  neuer Mechanismus.
- **Schreiben darf heute nur der Supervisor**:
  [ChildInterestsController.cs:26](../../backend/Pugling.Api/Controllers/Supervisor/ChildInterestsController.cs#L26)
  (`[Authorize(Roles = Roles.Supervisor)]`, Route `supervisor/children/{childId}/interests`). Unter
  `Controllers/Student/` kommt `ChildInterest` **nicht vor** — es gibt keinen Kind-Schreibweg, auch keinen
  lesenden.
- **Die Taxonomie gehört dem Creator**:
  [InterestTagsController.cs:23](../../backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs#L23)
  (`[Authorize(Roles = Roles.Creator)]`) — Tags anlegen ist heute keine Kind-Handlung und soll es nicht
  werden (siehe „Die echte Lücke").
- **Am `Child` liegen daneben zwei Freitextfelder**: `Interests` (JSON-Liste,
  [AdminEntities.cs:88](../../backend/Pugling.Api/Models/AdminEntities.cs#L88)) und `ProfileNotes`
  ([:92](../../backend/Pugling.Api/Models/AdminEntities.cs#L92)) — laut Kommentar ergänzt die Taxonomie
  den Freitext ausdrücklich, statt ihn zu ersetzen. Verbraucher des Freitexts ist allein der KI-Creator.
- **Die Bildwahl ist eingefroren**: `ChildMediaPick`
  ([MediaEntities.cs:157](../../backend/Pugling.Api/Models/MediaEntities.cs#L157)) — „Bildkonstanz *ist*
  der Merkeffekt" ([medien-bilder.md](../medien-bilder.md)). Der einzige vorhandene Rückkanal vom Kind ist
  heute „anderes Bild"
  ([ChildMediaPicksController.cs:25](../../backend/Pugling.Api/Controllers/Student/ChildMediaPicksController.cs#L25)),
  und dieses Signal wird nirgends als Interessenhinweis ausgewertet.
- **Die Produktbeschreibung der Sohn-App sagt heute das Gegenteil**: „Er legt keine Inhalte an und
  **steuert nichts**" ([sohn-app-funktionsbeschreibung.md](../sohn-app-funktionsbeschreibung.md),
  Abschnitt 1). Der Satz braucht mit dieser Story eine Ausnahme — Interessen sind keine Steuerung.

## Die echte Lücke

Nicht das Datenmodell, sondern drei Dinge daneben:

1. **Ein kindsicherer Schreibweg** (`student/me/interests`) — auswählen aus der Taxonomie, gewichten,
   löschen. **Ohne** das Recht, neue `InterestTag`-Zeilen anzulegen: die Taxonomie ist die geteilte
   Matching-Achse zu den Bildern; ein langer Schwanz aus Einzel-Tags träfe *nie* ein Bild und machte die
   Auswahl schlechter, nicht besser. Ein Wunsch außerhalb der Liste geht als Freitext („Sonstiges: …"),
   den später ein Erwachsener über den vorhandenen Backfill zum Tag macht.
2. **Der geführte Prozess** in der Sohn-App — Kacheln statt Formular, Paarvergleiche statt Skalen,
   Top-3-Wahl als Quelle der Gewichte, ein eigener kurzer Durchgang „was geht gar nicht?" für die
   negativen. Abbrechbar und fortsetzbar.
3. **Die Wirkungsregel gegenüber der eingefrorenen Bildwahl**: Eine Profiländerung wirkt auf **künftige**
   Wahlen; bestehende bleiben stehen, bis das Kind bewusst „anderes Bild" drückt. Ohne diese Regel lernt
   ein Kind, das gern an seinem Profil schraubt, jede Woche neue Bilder zum selben Wort — und der
   Merkeffekt, für den das Einfrieren gebaut wurde, ist weg.

Klar abzugrenzen: **Keine** Interessen sind Klassenstufe, Schulart und Lehrwerk/Unit — das sind
Lehrplan-Fakten und bleiben beim Supervisor.

## Offene Punkte

1. **Gehört das Geschlecht zur Zielgruppe oder nur zur Einkleidung?** *Empfehlung: nur Einkleidung* — als
   hartes Filtermerkmal schlösse es Inhalte aus, statt sie zu kleiden, und es kollidierte mit
   [B-46](B-46-interessenbasierte-uebungen.md), wo dieselbe Frage für die Übungs-Zielgruppe offen ist.
   Beide Stories müssen dieselbe Antwort bekommen.
2. **Schreibt das Kind direkt, oder muss der Supervisor freigeben?** *Empfehlung: direkt, mit
   Sichtbarkeit* — eine Genehmigungsschlange widerspricht „so leicht wie möglich", und der Supervisor kann
   über den vorhandenen Endpunkt jederzeit korrigieren.
3. **Deckel gegen häufiges Ändern?** *Empfehlung: ja, weich* — z. B. einmal pro Woche neu durchlaufen, und
   der Supervisor sieht Änderungen. Grund ist Kosten 3 oben, nicht Misstrauen.
4. **Bleiben `Child.Interests` (Freitext) und `ProfileNotes` beide?** *Empfehlung: zusammenführen zu
   **einem** kurzen Selbstbeschreibungsfeld*, sobald die Tags vom Kind kommen. Zwei Freitextfelder mit
   überlappender Bedeutung sind zwei Stellen zum Vergessen. Kosten: Vertragsänderung, Verbraucher ist der
   KI-Creator.
5. **Wird das Ablehnen eines Bildes als Signal ausgewertet?** *Empfehlung: zurückstellen* — verlockend
   („implizite Interessen"), aber es vermischt „mag ich nicht" mit „kenne ich schon" und braucht eine
   eigene Auswertung.
6. **Darf das Kind seinen eigenen Freitext schreiben?** *Empfehlung: ja, kurz und optional, aber für den
   Supervisor sichtbar* — er landet in einem LLM-Prompt, ein geheimer Kanal wäre an dieser Stelle falsch.
7. **Wann wird der Prozess angestoßen?** *Empfehlung: beim ersten Login und danach angestoßen* (nach einem
   Halbjahr, zum Geburtstag) statt als Dauer-Menüpunkt.

## Akzeptanzkriterien (Entwurf)

1. Ein eingeloggtes Kind kann seine Themen- und Stil-Interessen **selbst** setzen, gewichten und
   entfernen; fremde Kinder liefern `403`.
2. Das Kind kann dabei **keine** neue `InterestTag`-Zeile erzeugen — ein Versuch endet mit einem
   fachlichen Fehlercode, nicht mit einem stillen Anlegen.
3. Der geführte Prozess ist ohne Tippen bedienbar, abbrechbar und fortsetzbar; am Ende stehen gewichtete
   Einträge inklusive mindestens einer möglichen Abneigung (negatives Gewicht).
4. Eine Profiländerung ändert **keine bereits eingefrorene** Bildwahl; ein Test hält das fest.
5. Der Supervisor sieht die vom Kind gesetzten Interessen an der gewohnten Stelle und kann sie ändern.
6. Die Produktbeschreibung der Sohn-App ist um die Ausnahme ergänzt („steuert nichts" gilt für Inhalte und
   Pflicht, nicht für die eigene Beschreibung).

## Verlauf

- **2026-08-01** — angelegt und direkt ausformuliert (Quelle: Rollen-Durchgang; Kernentscheidung „das Kind
  gibt seine Interessen selbst an" vom Nutzer). Ist-Stand am Code belegt; die drei Fragen zu Geschlecht,
  Freigabe und Änderungs-Deckel bleiben für die Grill-Runde.
