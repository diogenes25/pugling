---
tags: [typ/story, status/ausformuliert]
aliases: []
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-63 (manueller Chrome-Test, 2026-08-07)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-123 · Lehrwerk-Reihe im Vater-Web bearbeiten

Beim Chrome-Test von B-63 (Verlags-Vokabular) aufgefallen: `VaterLehrwerke.tsx` bietet nur „Reihe
hinzufügen", kein Formular zum Ändern einer schon angelegten Reihe. Ein falsch zugeordneter Verlag, ein
Tippfehler in Name/Fach/Sprachen oder das nachträgliche Entfernen eines Verlags lässt sich über die
Oberfläche gar nicht korrigieren – nur über einen direkten API-Aufruf. Der Backend-Endpunkt dafür
(`PATCH creator/textbook-series/{id}` mit `UpdateTextbookSeriesDto`, inklusive des neuen
`ClearPublisherId`-Schalters) existiert bereits, hat aber keinen UI-Aufrufer.

## User Story

Als Vater (Creator) möchte ich eine bestehende Lehrwerk-Reihe bearbeiten können (Name, Verlag, Fach,
Schulart, Sprachen, Notiz – inklusive „Verlag entfernen"), damit sich ein Tippfehler oder ein falsch
zugeordneter Verlag korrigieren lässt, ohne die API direkt anzusprechen.

## Ist-Stand am Code

Alle vier Schichten unterhalb der Oberfläche sind bereits vollständig, nur der UI-Aufrufer fehlt:

- **Backend-Endpunkt vorhanden und vollständig**: `PATCH creator/textbook-series/{seriesId}`
  (`backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:154-184`). Prüft Eigentümerschaft
  (`163-164`, `NotOwner` sonst), validiert `SubjectId`/`PublisherId` über `ValidateReferencesAsync`
  (`165`), wendet PATCH-Semantik korrekt an (`167-180`) und behandelt den neuen `ClearPublisherId`-Schalter
  (`174`: `if (dto.ClearPublisherId) series.PublisherId = null;`, **nach** dem Wert angewendet – „leeren"
  gewinnt, wie es die Root-`CLAUDE.md`-Regel zur PATCH-Semantik verlangt).
- **Vertrag vorhanden**: `UpdateTextbookSeriesDto` mit `ClearPublisherId`
  (`backend/Pugling.Contracts/Creator/TextbookSeriesDtos.cs:24-30`), dokumentiert und additiv (kein
  Vertragsbruch).
- **JS/TS-Client vorhanden, aber ungenutzt**: `api.updateTextbookSeries` ist definiert
  (`frontend/src/lib/api.ts:749-750`). Eine Volltextsuche über `frontend/src` findet **genau eine**
  Fundstelle – die Definition selbst; kein einziger Aufrufer.
- **C#-Client (KI-Agenten) vorhanden**: `CreatorApi.UpdateSeriesAsync`
  (`backend/Pugling.Client/CreatorApi.cs:89`).
- **Frontend-UI fehlt vollständig**: `VaterLehrwerke.tsx` hat `SeriesRow` (`113-178`), das Name, Verlag,
  Fach, Schulart, Band nur **liest** und neben „Units“ (`151-154`) ausschließlich einen bedingten
  „Löschen“-Knopf zeigt (`156-160`, gated auf `series.isOwn`) – kein „Bearbeiten“. `NewSeries` (`386-514`)
  kann nur **anlegen**. Zum Vergleich: Auf Unit-Ebene existiert das gleiche Muster bereits gebaut –
  `UnitForm` (`261-383`) ist bewusst **ein** Formular für Anlegen *und* Ändern (Kommentar `257-260`), und
  `UnitPanel` (`181-253`) schaltet darüber per Bearbeiten-Toggle (`220-227`) um. Genau dieses Muster fehlt
  eine Ebene höher, bei der Reihe selbst.
- Kein Test deckt eine Bearbeitung der Reihe **über die UI** ab; `PatchSemanticsTests`/
  `PatchClearFieldTests` prüfen `ClearPublisherId` reflexiv nur gegen den Backend-Endpunkt.

## Die echte Lücke

Kein Backend-, Vertrags- oder API-Client-Problem – diese drei Schichten sind fertig und ungetestet-frei.
Die Lücke ist ausschließlich das fehlende React-Formular in `VaterLehrwerke.tsx`, das den längst
vorhandenen `api.updateTextbookSeries` aufruft. Entsprechend: `wo: frontend`, `migration: nein`,
`vertragsbruch: nein` sind praktisch vorentschieden (Bestätigung folgt in `geschaetzt`).

## Offene Punkte

1. **Wo sitzt das Formular?** Inline in `SeriesRow` nach demselben Muster wie `UnitPanel`/`UnitForm`
   (Bearbeiten-Toggle, ein gemeinsames Formular für Anlegen und Ändern) oder ein separat ausklappbarer
   Bereich? *Empfehlung*: dasselbe Muster wie bei den Units – Konsistenz innerhalb derselben Datei, und
   das Formularfeld-Layout von `NewSeries` (`439-514`) lässt sich dafür wiederverwenden.
2. **Wie wird „Verlag entfernen“ von „Verlag unverändert lassen“ unterschieden?** Das Verlag-`<select>`
   in `NewSeries` (`448-452`) kennt nur „– keine Angabe –“ (= beim Anlegen: kein Verlag) und die Liste der
   Verlage; im Edit-Formular müsste „– keine Angabe –“ mangels PATCH-Semantik **nicht** automatisch
   `ClearPublisherId` auslösen (sonst löscht ein Formular, das aus Versehen mit leerer Auswahl abgeschickt
   wird, einen bestehenden Verlag). *Empfehlung*: eine dritte, explizite Option „Verlag entfernen“ im
   Select, getrennt von „unverändert lassen“ (= aktuellen Verlag vorausgewählt lassen).
3. **Umfang der Story**: nur die Reihen-Metadaten editierbar machen (Name, Verlag inkl. Entfernen, Fach,
   Schulart, beide Sprachen, Notiz), oder zusätzlich die Inline-Verlag-Neuanlage aus `NewSeries`
   (`496-510`) im Edit-Formular duplizieren? *Empfehlung*: nur Metadaten-Edit: bereits vorhandener Verlag
   fehlt praktisch nie, weil die Auswahlliste beim Anlegen sowieso zeigt, was existiert.

## Akzeptanzkriterien

1. In `VaterLehrwerke.tsx` lässt sich eine eigene Reihe (`series.isOwn`) über einen „Bearbeiten“-Knopf in
   ein Formular öffnen, das die aktuellen Werte vorausfüllt.
2. Speichern ruft `api.updateTextbookSeries` mit nur den geänderten Feldern auf; die Reihen-Liste
   aktualisiert sich danach (`list.reload`) und `PublisherAdmin`/`publishers.reload` bleiben unberührt.
3. Der Verlag lässt sich sowohl **ändern** als auch **explizit entfernen** (`ClearPublisherId: true`),
   ohne dass ein einfaches erneutes Speichern ohne Auswahl den Verlag versehentlich löscht.
4. Eine fremde Reihe (`!series.isOwn`) zeigt weiterhin keinen Bearbeiten-Knopf (Server antwortet ohnehin
   mit `not_owner`, aber die UI zeigt das gar nicht erst an – bestehendes Muster von `156`).
5. Ein Komponententest oder E2E-Test fährt den Bearbeiten-Weg mindestens einmal durch (Formular öffnen →
   Feld ändern → speichern → geänderter Wert erscheint in der Liste).

## Verlauf

- **2026-08-07** — angelegt (Quelle: manueller Chrome-Test von B-63).
- **2026-08-07** — ausformuliert: Ist-Stand gegen den Code belegt (Backend/Vertrag/beide Clients
  vollständig, reine Frontend-Lücke), drei offene Punkte für die Grill-Runde formuliert.
