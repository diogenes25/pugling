---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Doppelter Reihenname, Slug schützt den Namen nicht, Umbenennen erzeugt Namensdublette]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-123 (Grill-Runde 2026-08-09)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-124]
nachgeschaut: 2026-08-10
wartet_auf: ""
---

# B-133 · Nach einer Umbenennung können zwei Reihen denselben Anzeigenamen tragen

Abgespalten von [B-123](B-123-lehrwerk-reihe-bearbeiten.md) (Entscheidung 5): B-123s Ziel — eine Reihe
über die Oberfläche bearbeiten zu können — ist ohne diesen Fehler erfüllt, und seine Behebung braucht eine
Produktentscheidung, die B-123s Akzeptanzkriterien nicht beantworten. Darum eigene Story statt Anhang.

## User Story

Als **Creator**, der eine Reihe im geteilten Katalog auswählt, möchte ich, dass ein Anzeigename genau eine
Reihe meint — damit ich in einem Auswahlfeld nicht zwischen zwei identisch beschrifteten Zeilen raten muss.

## Ist-Stand am Code

Der Wächter aus [B-124](B-124-umbenennen-umgeht-die-eindeutigkeit.md) existiert und greift — aber er
vergleicht **Slug gegen Slug**, während er laut eigenem Kommentar den *Anzeigenamen* schützen soll:

- `TextbookSeriesController.cs:153-156` — der XML-Kommentar begründet die Prüfung mit „otherwise two
  series share a display name in every picker".
- `TextbookSeriesController.cs:181` — geprüft wird
  `db.TextbookSeries.AnyAsync(s => s.Id != seriesId && s.Slug == slug)`, also der aus dem **neuen** Namen
  abgeleitete Slug gegen die gespeicherten Slugs.
- `TextbookSeriesController.cs:154`, `:185` — der Slug wird beim Umbenennen **nicht** neu abgeleitet
  (`series.Name = name`); er ist laut Vertrag der „normalized, globally unique and **immutable** key"
  (`Pugling.Contracts/Creator/TextbookSeriesDtos.cs:8-9`).
- `Data/PuglingDbContext.cs:220` — `e.HasIndex(s => s.Slug).IsUnique()`; auf `Name` liegt **kein**
  Unique-Index, die DB fängt den Fall also auch nicht.

Damit driften Name und Slug nach der ersten Umbenennung auseinander, und der Wächter prüft danach die
falsche der beiden Größen.

## Die echte Lücke

Der belegte Ablauf, rein aus den obigen Zeilen:

1. Reihe A wird als „Access" angelegt → `Slug = access` (`:125`, `:136`).
2. A wird in „Green Line" umbenannt → Prüfung leitet `green-line` ab, findet keine Kollision, `Name`
   ändert sich, `Slug` bleibt `access` (`:177-185`).
3. Jemand legt „Green Line" an → `DeriveRequiredSlug` liefert `green-line`, die Suche über `Slug`
   (`:130`) findet nichts, also entsteht Reihe B (`:133-147`).
4. A und B heißen jetzt beide „Green Line". Jede Auswahlliste zeigt zwei ununterscheidbare Zeilen.

Spiegelbildlich ist „Access" danach **nicht** mehr anlegbar: der abgeleitete Slug `access` trifft A, und
`Create` gibt idempotent die Reihe zurück, die inzwischen „Green Line" heißt (`:130-131`).

**Warum es bisher niemandem aufgefallen ist:** Umbenennen war über die Oberfläche gar nicht erreichbar —
`VaterLehrwerke.tsx` bot bis B-123 nur Anlegen und Löschen. Der Fehler lag also nur auf dem direkten
API-Weg offen.

## Offene Punkte

1. **Welche der beiden Größen trägt die Eindeutigkeit — Name oder Slug?** Zwei Wege, die sich
   ausschließen: (a) die Vorprüfung zusätzlich über den **Namen** laufen lassen (Slug bleibt unveränderlich
   und idempotent, aber fremdes Material kann mein Anlegen blockieren — ein Creator, der „Access" schon
   angelegt hat, verhindert meins); (b) den **Slug beim Umbenennen mitwandern** lassen (Name und Kurzname
   passen immer zusammen, aber die dokumentierte Unveränderlichkeit fällt und mit ihr die Idempotenz des
   Anlegens: ein Skript, das täglich „Access" anlegt, erzeugt nach einer Umbenennung eine Dublette).
   *Empfehlung*: (a) — die Idempotenz des Anlegens ist eine zugesicherte Vertragseigenschaft, die
   Kollision beim Umbenennen dagegen ein seltener, gut erklärbarer `409`.
2. **Gilt die Namensgleichheit global oder je Creator?** Der Slug ist global eindeutig (`:179-180`
   benennt das ausdrücklich). *Empfehlung*: ebenfalls global — alles andere ergäbe zwei verschiedene
   Eindeutigkeitsbegriffe an derselben Ressource.
3. **Was passiert mit den Dubletten, die heute schon in einer Datenbank stehen können?**
   *Empfehlung*: nichts erzwingen — die Prüfung greift beim nächsten Schreibzugriff; ein Reparaturlauf
   wäre Aufwand für einen Fall, der bisher nur über direkte API-Aufrufe entstehen konnte.

## Entscheidungen

Autonom gegrillt im Nachtlauf am 2026-08-09 (Freigabe 1: `art: Defekt`), Protokoll
[pm-sitzung-2026-08-09.md](../pm-sitzung-2026-08-09.md).

1. **Weg (a): Der Name trägt die Eindeutigkeit *zusätzlich*, der Slug bleibt unveränderlich.**
   *Begründung*: Weg (b) — den Slug beim Umbenennen mitwandern lassen — bräche eine im Vertrag
   zugesicherte Eigenschaft (`TextbookSeriesDtos.cs:8-9`: „normalized, globally unique and **immutable**
   key") und mit ihr die Idempotenz des Anlegens, auf die der KI-Creator und die eigene Oberfläche bauen.
   Ein `409` beim Umbenennen ist dagegen selten, erklärbar und behebbar. *Kosten*: Name und Slug dürfen
   dauerhaft auseinanderlaufen — die Zeile zeigt weiter einen Kurznamen, der nicht zum Namen passt. Genau
   dafür schreibt [B-123](B-123-lehrwerk-reihe-bearbeiten.md) (Entscheidung 5) einen Hinweis ans
   Namensfeld.
2. **Global, nicht je Creator.** *Begründung*: Der Slug ist global eindeutig, und der Controller sagt das
   ausdrücklich (`:179-180`: „series slugs are unique across creators, not per owner"). Zwei verschiedene
   Eindeutigkeitsbegriffe an derselben Ressource wären die teurere Sorte Inkonsistenz. *Kosten*: ein
   fremder Creator kann mir einen Namen wegnehmen — dieselbe Eigenschaft, die der Slug schon hat, also
   keine neue Klasse von Ärger.
3. **Beide Schreibwege, wie bei [B-124](B-124-umbenennen-umgeht-die-eindeutigkeit.md).** Die Prüfung sitzt
   im `POST` **und** im `PATCH`. *Begründung*: Beim Anlegen greift der Slug-Treffer nur, solange Name und
   Slug synchron sind; sobald irgendeine Reihe umbenannt wurde, geht der Name am Slug-Vergleich vorbei.
   *Kosten*: eine zusätzliche `AnyAsync`-Abfrage je Schreibvorgang.
4. **Schreibweisen zählen als derselbe Name — geschenkt aus [B-128](B-128-katalogsuche-case-sensitiv.md).**
   Die dort gelegte `NOCASE`-Collation auf `TextbookSeries.Name` wirkt auf **Gleichheit**, und der
   Dublettenvergleich ist eine. *Begründung*: „Access" und „ACCESS" sind für einen Menschen derselbe
   Anzeigename. *Kosten*: keine eigenen — B-128 hat die Faltung schon bezahlt. Ohne B-128 hätte diese
   Story `migration: ja` getragen.
5. **Bestandsdubletten werden nicht repariert.** *Begründung*: Sie können bisher nur über direkte
   API-Aufrufe entstanden sein (die Oberfläche konnte gar nicht umbenennen), und die Prüfung greift beim
   nächsten Schreibzugriff. *Kosten*: eine Datenbank, in der schon zwei gleichnamige Reihen stehen,
   behält sie — bis jemand eine davon anfasst.

## Akzeptanzkriterien

1. Ein `PATCH` auf einen Namen, den eine **andere** Reihe bereits als Anzeigenamen trägt, wird abgewiesen
   (`409`), auch wenn die Slugs verschieden sind.
2. Ein `POST` mit einem Namen, den eine andere Reihe bereits als Anzeigenamen trägt, führt weder zu einer
   Dublette noch zu einem stillen Treffer auf die falsche Reihe.
3. Ein Regressionstest fährt die vier Schritte aus „Die echte Lücke" durch und ist vor der Behebung rot
   (mit genannter Zahl erwartet/gemessen).
4. Die Idempotenz des Anlegens über den Slug bleibt erhalten: derselbe Name liefert weiterhin dieselbe
   Reihe zurück.

## Schätzung

**S** (`wo: backend`, `migration: nein`, `vertragsbruch: nein`) — zwei `AnyAsync`-Vorprüfungen, ein
geschärfter Vorgabetext in `ApiErrors`, eine neue Testklasse. **Keine Migration**, weil B-128 die
`NOCASE`-Collation im selben Sprint schon gelegt hat; ohne sie wäre es `ja`. Kein Vertragsbruch: der
`code` `duplicate_textbook_series` bleibt, nur sein Vorgabe-`detail` wird weiter gefasst — und `detail`
ist ausdrücklich kein stabiler Vertragsbestandteil.

**Testweg:** neue Klasse `backend/Pugling.Api.Tests/ReihenNamensDubletteTests.cs` mit vier Fällen — der
vierschrittige Ablauf aus „Die echte Lücke" (der einzige, der wirklich rot war), Umbenennen auf einen
vergebenen Namen, andere Schreibweise, und als Gegenprobe die erhaltene Idempotenz des Anlegens.

## Verlauf

- **2026-08-09** — angelegt und zugleich ausformuliert: der Ist-Stand entstand in der Grill-Runde zu
  B-123 (Entscheidung 5) und ist Zeile für Zeile belegt; drei offene Punkte für die Grill-Runde
  formuliert. `entgangen_bei: [B-124]` — die Prüfung, die dieser Defekt umgeht, wurde dort abgenommen.
- **2026-08-09** — Nachtlauf, Sprint 1: autonom gegrillt (fünf Entscheidungen), geschätzt (**S**,
  `backend`) und gebaut (Namens-Vorprüfung in `Create` **und** `Update` von `TextbookSeriesController`).
  **Rote Probe mit Zahl:** **1 von 4 rot** — und das ist der Befund, nicht eine schwache Probe. Rot war
  nur der vierschrittige Ablauf aus „Die echte Lücke"; die beiden anderen Verbotsfälle waren **schon
  vorher grün**, weil bei einer nie umbenannten Reihe Name und Slug synchron sind und der B-124-Wächter
  greift. Genau darum ist der Defekt schwer zu sehen: er entsteht erst durch die erste Umbenennung. Der
  vierte Fall ist die Idempotenz-Gegenprobe und war ebenfalls grün. Nach dem Fix **4/4**, Suite
  **784/784 grün**.
- **2026-08-10** — `pugling-reviewer`, Re-Review der Korrekturen: **zwei echte Lücken in meinen eigenen
  Tests**, beide geschlossen. (a) Der gestern eingebaute Fix (Slug-Treffer mit abweichendem Namen → 409)
  hatte **keinen Test** — ersetzte man ihn durch ein bedingungsloses `Ok(existing)`, blieb die Suite grün;
  genau AK 2 war also weiter unbelegt. (b) Der Schreibweisen-Fall prüfte nicht, was seine Doku behauptete:
  `ToUpperInvariant()` leitet **denselben** Slug ab, also antwortete der ältere Slug-Wächter, und die
  Namensprüfung samt Collation wurde nie erreicht — der Fall war mit und ohne `NOCASE` grün. Beide
  umgebaut (Slug und Name erst durch eine Umbenennung entkoppeln), dazu der bis dahin offene Fund zur
  **creator-übergreifenden** Reichweite: ein zweites Konto über `POST supervisor/adults` belegt jetzt
  Entscheidung 2. Sechs Fälle, **6/6 grün**, Suite **788/788**. Ergänzt: `[ProducesResponseType(409)]` und
  ein Nebensatz im `<summary>` — `Create` gab den neuen Ausgang heraus, ohne ihn zu deklarieren.
  Bewusst dokumentiert statt behoben: die Prüfung im Slug-Zweig faltet Unicode (C#), die beiden anderen
  nur ASCII (SQLite `NOCASE`). Die gefährliche Richtung ist zu (NOCASE-gleich ⇒ C#-gleich); der Rest
  („ökotest" neben „Ökotest" nach einer Umbenennung) bräuchte eine ICU-Collation und steht als benannte
  Grenze am Code.
- **2026-08-10** — **abgenommen.** Commit `0663aa8` (gemeinsam mit B-128). Verifikation: sechs eigene
  Fälle **6/6**, Suite **788/788**, E2E **29/29** als Rollengang, `pugling-reviewer` zweimal — der zweite
  Lauf hat die zwei Test-Lücken gefunden, die diese Abnahme sonst hohl gemacht hätten.
- **2026-08-10** — nachgeschaut (Nachtlauf, Retro des Folge-Sprints), und zwar besonders genau, weil
  [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md) die Regel wörtlich übernimmt. Geprüft: beide
  Richtungen sind da (Slug-Treffer nur bei Namensgleichheit; freier Slug heißt nicht freier Name), der
  Selbstausschluss `s.Id != id` steht im PATCH, und der Kommentar benennt den verbleibenden
  nicht-ASCII-Fall selbst statt ihn zu verschweigen. Kein durchgekommener Defekt.
