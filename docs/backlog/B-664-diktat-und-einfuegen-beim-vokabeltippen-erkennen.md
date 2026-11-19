---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Diktierfunktion erkennen, Buchstabe für Buchstabe, Anti-Cheat Eingabe]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-664 · Diktieren, Einfügen und Autovervollständigung beim Vokabeltippen erkennen

Beim Vokabellernen müssen die Kinder die Wörter schreiben – richtig so, denn im Vokabeltest wird auch
geschrieben. Sie nutzen dafür aber die **Diktierfunktion** des Handys. Der Vater will das nicht: jeder
Buchstabe soll bewusst eingegeben werden. Ebenso unerwünscht sind **Copy-Paste** und die
**Autovervollständigung** der Tastatur.

Idee des Vaters: das Frontend erkennt, ob ein Wort Buchstabe für Buchstabe getippt wurde. Kommen mehrere
Zeichen auf einmal ins Feld (oder in unrealistisch kurzer Zeit), war es wahrscheinlich kein Tippen.

**Vorbehalt:** Art `Wunsch`; der Vorbereitungslauf hat nur **ausformuliert**. Die Fragen unter „Offene Punkte“ sind
**nicht entschieden**, es gibt keinen Abschnitt `## Entscheidungen`. **Befund vorab:** Der größte Teil der Idee ist am
2026-11-18 bereits gebaut (siehe Ist-Stand); die Story schrumpft auf eine Restlücke.

## User Story

Als *Vater* möchte ich, dass ein Vokabelwort im Antwortfeld nur durch Tippen einzelner Zeichen ins Feld kommt (nicht per
Diktat, Einfügen oder Tastaturvorschlag), damit das Kind das Schreiben wirklich übt und im Vokabeltest bestehen kann.
Als *Student* möchte ich ehrlich getippte Zeichen – auch mit Akzent über eine Tottaste oder die Sonderzeichen-Leiste –
nie als Schummeln gewertet bekommen.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main` (Stand `47791819`). Pfade relativ zu `frontend/`.

### Schon gebaut: die Kopier- und Einfügesperre (Commit `0f8f0328`, 2026-11-18)

- `src/sohn/kopierSperre.ts:24` hängt an die Wurzel der Sohn-App (`src/sohn/SohnApp.tsx:94`, `useKopierSperre`) drei Schichten:
  `copy`/`cut`/`dragstart`/`paste`/`drop` gesperrt (`:45-47`), jedes `beforeinput` mit Typ `insertFrom…` außer
  `insertFromComposition` verworfen (`:37`) – das schließt `insertFromPaste` **und** `insertFromDictation`, ebenso Ziehen und
  Yank –, und ein `insertText`/`insertReplacementText` mit **mehr als einem Zeichen** (Codepunkte gezählt) wird verworfen
  und meldet `beiMehrzeichen` (`:39-42`).
- Das Kind bekommt einen Hinweis „Bitte Buchstabe für Buchstabe tippen.“ (`src/i18n/de.ts:10`, `typeLetters`; Anzeige in
  `SohnApp.tsx:94-98` mit 3 Sekunden Timer).
- **Ausnahmen mit Absicht:** `insertCompositionText` (Android-Tastaturen) und `insertFromComposition` (Safari, Akzent über
  Tottaste) bleiben erlaubt (`kopierSperre.ts:13-19`); das Anmerkungs-Widget und der Wunsch-Text tragen
  `data-einfuegen-erlaubt` (`KartenMeldung.tsx:95`, `SohnInterests.tsx:172`).
- **Felder konfiguriert:** `autoComplete="off" autoCapitalize="off" autoCorrect="off" spellCheck={false}` am Freitextfeld
  (`src/sohn/Lektion.tsx:200`), an den Lernkarten-Feldern (`src/sohn/Lernkarte.tsx:137`, `:172`) und an den Buchstabenkästchen
  (`src/components/LetterBoxes.tsx:107-110`); dort zusätzlich `maxLength={1}` (`:106`).
- **Sonderzeichen-Leiste (B-661, abgenommen) ist kein Schummeln:** sie setzt das Zeichen über den React-Zustand
  (`Lektion.tsx:204-205`, `einsetzen(...)`), nicht über ein `beforeinput`; die Sperre sieht sie nicht.
- **Tests:** `src/sohn/kopierSperre.test.ts` (u. a. `insertFromPaste`/`Drop`/`Yank`, Mehrzeichen, ein Zeichen `s`/`é`/`😀`
  bleibt, Komposition bleibt, Ausnahme-Bereich, Aufräumen) und E2E `e2e/kopiersperre.spec.ts` (Schritt 5: ganzes Wort auf
  einmal bleibt draußen, Hinweis erscheint).

### Was nicht gebaut ist

- **`insertFromDictation` ist kein eigener Testfall** (kein Treffer im Test oder in `e2e/`): die Sperre fängt ihn über den
  `insertFrom`-Präfix; der Fall steht nirgends ausdrücklich fest. *(Am Code gelesen, nicht per Test nachgestellt.)*
- **Android-Diktat/Tastaturvorschlag mitten in einer Komposition:** kommt durch (so im Kopf der Datei dokumentiert,
  `kopierSperre.ts:17-18`). Ob Gboard-Diktat als `insertCompositionText` mit mehreren Zeichen ankommt, ist **nicht gemessen**
  (kein Gerät, keine Aufnahme in Repo oder Doku gefunden).
- **Zeitabstände zwischen Zeichen:** keine Fundstelle (`beforeinput`-Hörer prüft nur Typ und Länge).
- **Meldung an den Vater:** ein Treffer wird nirgends gezählt oder gespeichert; der Hinweis geht nur ans Kind
  (`SohnApp.tsx:94-98`). Kein Backend-Feld, kein Endpunkt (nicht gefunden).
- **Server-Seite:** prüft nichts (kann es nicht): Antworten kommen als Text an (`AnswerGrader`), ohne Eingabeweg.

## Die echte Lücke

Nicht „erkennen, ob getippt wurde“ – das tut die Sperre schon für Einfügen, Ziehen, iOS-Diktat und mehrzeichige Vorschläge.
Verblieben ist:

1. **Android-Diktat/Vorschlag in der Komposition** (bewusst offen gelassen, weil `insertCompositionText` nicht abbrechbar ist
   und Wischtippen/Akzente sonst brächen). Ob das eine reale Lücke ist, ist ohne Gerätemessung offen.
2. **Kein Zeitsignal** (Zeichen/Sekunde) – wäre die einzige Schicht gegen die Android-Lücke.
3. **Keine Rückmeldung an den Vater**, ob ein Kind es versucht hat.
4. Ein ausdrücklicher Test für `insertFromDictation`.

Vorschlag (kein Beschluss): Die Story ist **weitgehend erledigt**; sie sollte nicht weitergeführt werden, bevor jemand mit
Friedrichs Gerät misst, ob Diktat noch durchkommt. Ob sie dann `abgenommen` (mit Restlücke als eigene Story) oder
`verworfen` wird, ist Sache des Nutzers.

## Offene Punkte

Jede Frage mit Empfehlung. **Nichts davon ist entschieden.**

1. **Kommt das Diktat auf Friedrichs Handy durch?** Empfehlung: erst messen, ein Gerät, eine Aufnahme der `inputType`-Folge
   (Debug-Hörer, nicht im Produkt). Ohne diese Messung ist jedes Zeitsignal Vermutung.
2. **Reicht ein clientseitiges Signal als Hürde für ein Kind?** Empfehlung: ja als Hürde, nicht als Beweis – es ist umgehbar
   (andere Tastatur, abgeschriebene Wörter), und der Server sieht nichts. Der Wert liegt im Aufhalten des bequemen Weges.
3. **Was passiert bei einem Treffer?** Heute: Eingabe verworfen, Hinweis. Empfehlung: dabei bleiben (nicht als falsch
   werten – ein Mehrzeichen-Treffer ist auch ein ehrlicher iPhone-Wischer); nur den Vater informieren, falls er es will.
4. **Soll der Vater es erfahren?** Empfehlung: nein im ersten Schritt (Zähler wäre ein Vertrags-/Schemaeingriff und eine
   neue Überwachung); erst wenn Punkt 1 zeigt, dass es Versuche gibt.
5. **Zeitsignal als Zusatzschicht?** Empfehlung: nicht ohne Messung (Punkt 1); eine Zeichenrate-Schwelle träfe schnelle
   Tipper und LRS-Ausgleich (Friedrich) ungerecht.
6. **Autovervollständigung auf Android:** `autoCorrect="off"`/`autoComplete="off"` sind Hinweise, die manche Android-Tastaturen
   ignorieren (nicht gemessen). Empfehlung: nicht ausweiten; die Mehrzeichen-Sperre fängt den Vorschlag als Ganzes.

## Akzeptanzkriterien (Entwurf)

Nur für den, der die Restlücke baut; je Kriterium die Änderung, die es rot macht:

1. Ein `beforeinput` vom Typ `insertFromDictation` wird verworfen, das Kind erhält den Hinweis. *Rot, wenn* der
   `insertFrom`-Präfix-Zweig in `kopierSperre.ts:37` auf eine feste Typenliste ohne Diktat verkürzt oder entfernt wird.
2. Ein einzelnes Zeichen mit Akzent über eine Tottaste (`insertFromComposition`) und ein Zeichen aus der Sonderzeichen-Leiste
   bleiben erlaubt. *Rot, wenn* die Sperre `insertFromComposition` oder das Setzen über den Zustand blockiert.
3. Ein ganzes Wort in **einer** `insertText`-Eingabe wird verworfen und gemeldet. *Rot, wenn* die Codepunkt-Zählung
   (`:39`) auf `data.length` oder `> 2` fällt (Emoji als zwei Einheiten).
4. Nur, wenn Punkt 1 eine Lücke zeigt: eine Aufnahme der realen Android-Eingabe als Test-Fixture belegt, dass die neue
   Schicht sie fängt und ehrliches Wischtippen nicht. *Rot, wenn* die Fixture fehlt oder erfunden ist.
5. Das Anmerkungs-Widget und der Wunsch-Text bleiben einfügbar (`data-einfuegen-erlaubt`). *Rot, wenn* die Ausnahme
   verloren geht.

## Beziehungen zu anderen Stories (nur gelesen)

- [B-661](B-661-fehlende-sonderzeichen-auf-der-tastatur-blockieren-die-mission.md) (`abgenommen`): Sonderzeichen-Leiste,
  setzt über den Zustand – verträglich mit der Sperre.

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Die Kopier- und
  Einfügesperre (`kopierSperre.ts`, Commit `0f8f0328`, 2026-11-18) deckt Einfügen, iOS-Diktat und mehrzeichige Vorschläge
  bereits ab; offen sind nur Android-Komposition, Zeitsignal und Vater-Meldung. Vorschlag im Protokoll: Story ist
  weitgehend erledigt, erst mit Gerätemessung weiterführen. **Nichts entschieden.**
