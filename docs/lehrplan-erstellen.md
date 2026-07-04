# Einen Lehrplan erstellen — Handbuch für den Vater

Diese Doku erklärt **genau**, wie ein Vater einen kompletten Lehrplan („Lehrplan") mit einzelnen
Fächern (Modulen) und Übungen anlegt, den seine Söhne danach **unbeaufsichtigt** abarbeiten können.
Enthalten: alle Dateiformate im Detail, ein **vollständig ausgearbeitetes Beispiel** und ein
Schritt-für-Schritt-Tutorial – auch für den Fall **mehrerer Söhne**.

> **Abgrenzung:** Es gibt in diesem Repo zwei Dinge, die „Lehrplan/Plan" heißen:
>
> 1. **Der markdown-basierte Lehrplan** (dieses Dokument) — von Claude über die Skills `vater`/`sohn`
>    erstellt und abgearbeitet, ohne dass die App läuft. Ideal für jedes Thema (Mathe, Englisch,
>    Programmieren …).
> 2. **Der Study-Plan der Pugling-App** (Vokabeln, Lückentext, Matching per REST-API) —
>    kompakt in [tutorial.md](tutorial.md), ausführlich im Wiki:
>    [04 · Lernplan bauen](../wiki/04-lernplan-bauen.md) (Aufbau via API durch einen Menschen/Agent)
>    und [09 · LLM-Kochbuch](../wiki/09-llm-kochbuch.md) (Plan aus einem Prompt).
>
> Dieses Handbuch behandelt **Variante 1**. Die [drei „Pläne" im Überblick](../README.md#️-die-drei-pläne--bitte-nicht-verwechseln).

---

## 1. Das Grundprinzip: einmal schreiben, dann weggehen

Der Vater ist die **Autorenrolle** (Skill `vater`). Er schreibt den Lehrplan **genau einmal** und ist
danach nicht mehr im Raum. Wer den Plan später abarbeitet (typischerweise Claude selbst über den Skill
`sohn`), ist unmotiviert und unbeaufsichtigt. Deshalb muss der Plan die Disziplin **selbst erzwingen** —
nicht der Vater durch Nachfragen.

Daraus folgen drei feste Regeln, die beim Erstellen jedes Plans gelten:

1. **Alles steht im Ordner.** Ziele, Lehrinhalt, Aufgaben, Punktwerte, Bewertungskriterien,
   Lösungsschlüssel und die Bestehensschwellen. Kein „frag mich, wenn du nicht weiterkommst" — der Vater
   ist weg.
2. **Bewertung ist so objektiv wie möglich.** Ein Lerner, der sich selbst benotet, schummelt sich
   sonst durch. Bevorzuge Aufgaben mit eindeutig prüfbarem Ergebnis (exakte Zahl, exakter String,
   Multiple-Choice mit Schlüssel, Code, der Tests bestehen muss). Für offene „Erkläre"-Aufgaben schreibst
   du einen **punktgenauen Rubric**, damit die Selbstbenotung kaum Spielraum hat.
3. **Fortschritt ist durch Punkte gesperrt.** Der Sohn darf erst ins nächste Modul, wenn er die
   Modul-Schwelle erreicht, und den Kurs erst abschließen, wenn er die Gesamt-Schwelle erreicht. Diese
   „Gates" leben in `manifest.json` — die `sohn`-Skill liest sie als verbindliches Gesetz.

---

## 2. Schnellstart

Der schnellste Weg: Claude die `vater`-Skill nutzen lassen.

```text
/vater erstelle mir einen Lehrplan "Bruchrechnen Grundlagen" für meinen Sohn,
Level Anfänger, 2 Module, ca. 20 Punkte
```

Claude fragt höchstens das Nötigste ab (Thema, Level, Umfang), setzt sonst sinnvolle Standards und legt
den kompletten Ordner an. Du kannst den Plan aber auch **von Hand** anlegen — dieses Dokument beschreibt
jedes Detail dafür.

Wenn der Sohn (oder Claude) loslegen soll:

```text
/sohn arbeite den Lehrplan unter ./lehrplan/bruchrechnen-grundlagen durch
```

---

## 3. Ordnerstruktur

Jeder Kurs liegt unter `./lehrplan/<kurs-slug>/` (kebab-case, aus dem Thema abgeleitet). Lege
`./lehrplan/` an, falls es fehlt.

```text
lehrplan/<kurs-slug>/
├── manifest.json                     # Maschinenlesbarer Vertrag — die Quelle der Wahrheit für die Gates
├── curriculum.md                     # Menschlicher Überblick + Fächerliste + Hausregeln
├── modules/
│   ├── 01-<slug>.md                  # Lehrinhalt + Übungen (KEINE Lösungen)
│   └── 02-<slug>.md
└── answer-key/
    ├── 01-<slug>.md                  # Lösungen + Bewertungs-Rubric, eine Datei pro Modul
    └── 02-<slug>.md
```

**Wichtig:** Lösungen liegen in `answer-key/`, strikt **getrennt** von den Übungen in `modules/`. Genau
das macht ehrliche Selbstbenotung möglich: Der Sohn schreibt seine Antwort **zuerst** auf und öffnet
**dann** den Schlüssel. Leake niemals Lösungen in die Modul-Dateien.

Die Dateien `progress.json`, `ledger.md` und der Ordner `work/` werden **später vom Sohn** angelegt —
der Vater erstellt sie nicht.

> **„Fach" vs. „Modul":** Ein Kurs-Ordner ist typischerweise **ein Fach** (z. B. Mathe). Die
> **Module** darin sind die Themen/Lektionen dieses Fachs. Willst du mehrere Schul-Fächer abbilden,
> legst du **pro Fach einen eigenen Kurs-Ordner** an (siehe [§7](#7-tutorial-ein-kompletter-plan-für-die-söhne)).

---

## 4. Die Dateien im Detail

### 4.1 `manifest.json` — der Vertrag

Diese Datei sorgt dafür, dass deine Autorität deine Abwesenheit überlebt. Die `sohn`-Skill behandelt sie
als Gesetz: Punktwerte und Schwellen kommen von hier, und sie verweigert das Weiterrücken oder
Abschließen, bis sie erfüllt sind. Schreibe sie **präzise**.

```json
{
  "course": "Bruchrechnen Grundlagen",
  "slug": "bruchrechnen-grundlagen",
  "topic": "Grundlagen der Bruchrechnung",
  "level": "beginner",
  "authored_by": "vater",
  "pass_threshold_pct": 80,
  "total_points": 20,
  "house_rules": [
    "Arbeite die Module strikt der Reihe nach — Modul 02 setzt Modul 01 als bestanden voraus.",
    "Versuche jede Übung und schreibe deine Antwort in ledger.md, BEVOR du den Lösungsschlüssel öffnest.",
    "Rechne bei Rechenaufgaben wirklich — schätze das Ergebnis nicht.",
    "Benote dich ehrlich am Rubric. Das Ledger ist prüfbar.",
    "Der Kurs gilt erst als bestanden, wenn die Gesamtpunkte 16/20 (80 %) erreichen."
  ],
  "modules": [
    {
      "id": "01",
      "slug": "was-ist-ein-bruch",
      "title": "Was ist ein Bruch?",
      "file": "modules/01-was-ist-ein-bruch.md",
      "answer_key": "answer-key/01-was-ist-ein-bruch.md",
      "points": 10,
      "pass_threshold_pct": 70,
      "exercises": [
        { "id": "01.1", "type": "multiple-choice", "points": 3, "checkable": true },
        { "id": "01.2", "type": "short-answer",    "points": 4, "checkable": true },
        { "id": "01.3", "type": "explain",         "points": 3, "checkable": false }
      ]
    },
    {
      "id": "02",
      "slug": "brueche-addieren",
      "title": "Brüche addieren",
      "file": "modules/02-brueche-addieren.md",
      "answer_key": "answer-key/02-brueche-addieren.md",
      "points": 10,
      "pass_threshold_pct": 70,
      "exercises": [
        { "id": "02.1", "type": "multiple-choice", "points": 3, "checkable": true },
        { "id": "02.2", "type": "short-answer",    "points": 4, "checkable": true },
        { "id": "02.3", "type": "explain",         "points": 3, "checkable": false }
      ]
    }
  ]
}
```

**Feld-Erklärungen:**

| Feld | Bedeutung |
|------|-----------|
| `course` / `slug` / `topic` | Klartext-Name, Ordner-Slug, Kurzbeschreibung des Themas. |
| `level` | `beginner` / `intermediate` / `advanced`. |
| `authored_by` | Immer `"vater"`. |
| `pass_threshold_pct` (oben) | Gesamt-Bestehensschwelle des Kurses in Prozent. |
| `total_points` | Summe aller Modulpunkte. |
| `house_rules` | Die Regeln, die der Sohn befolgen muss — in `curriculum.md` für Menschen wiederholt. |
| `modules[].points` | Punkte des Moduls. |
| `modules[].pass_threshold_pct` | Schwelle, um das **nächste** Modul freizuschalten (70–80 % ist fair). |
| `exercises[].type` | `multiple-choice` \| `short-answer` \| `code` \| `explain`. |
| `exercises[].checkable` | `true` = mechanisch prüfbar (Schlüssel/Test/exakter Wert); `false` = am Rubric benotet. |

**Punktarithmetik (unbedingt prüfen):**

- `summe(module.points) == total_points`
- Pro Modul: `summe(exercise.points) == module.points`

Eine Abweichung zerstört die Gate-Rechnung des Sohns. Prüfe das, bevor du fertig bist.

### 4.2 `curriculum.md` — der menschliche Überblick

Kurz und lesbar: Was deckt der Kurs ab, Modulliste mit Punktgewichten, die Bestehensschwelle und die
Hausregeln für einen menschlichen Leser.

### 4.3 `modules/NN-slug.md` — Lehrinhalt + Übungen

Jede Modul-Datei ist **selbst-erklärend**, weil niemand live erklärt. Aufbau:

```markdown
# Modul 01 — Was ist ein Bruch?

## Objective
Was der Lerner nach dem Modul kann (2–4 konkrete, prüfbare Fähigkeiten).

## Learn
Der eigentliche Lehrinhalt. Erkläre so gut, dass ein hinreichend motivierter Lerner keine
externe Quelle braucht. Mit kurzen durchgerechneten Beispielen. Hier steckt die eigentliche
Arbeit des Vaters — ein dünner Lehrtext produziert einen Lerner, der an den Übungen scheitert
und den Plan beschuldigt.

## Exercises
### Exercise 01.1  (multiple-choice, 3 pts)
Die Frage und Optionen A–D. Verrate die Lösung NICHT.

### Exercise 01.2  (short-answer, 4 pts)
Eine präzise Aufgabe mit eindeutig prüfbarem Ergebnis (exakte Zahl / exakter Wert).

### Exercise 01.3  (explain, 3 pts)
Eine offene Frage. Weise darauf hin, dass sie am Rubric im Lösungsschlüssel benotet wird.
```

### 4.4 `answer-key/NN-slug.md` — Lösungen + Bewertung

Gib dem Sohn pro Übung genau, was er zur Selbstbenotung braucht — mit wenig Schummel-Spielraum:

- **multiple-choice / short-answer:** die korrekte Antwort, klar benannt, plus die Punktregel
  („3 Punkte nur für B, sonst 0").
- **code:** eine Referenzlösung **und** wie man sie prüft (welche Tests laufen, erwartete Ausgabe).
  Verweise den Sohn aufs **Ausführen**, nicht aufs Draufschauen.
- **explain:** ein **punktweiser Rubric** — z. B. „1 Punkt: nennt den Zähler; 1 Punkt: nennt den Nenner;
  1 Punkt: erklärt das Verhältnis". Objektive Teilkriterien machen aus einer Bauchnote eine Rechnung.

---

## 5. Aufgabentypen im Überblick

| Typ | Wann verwenden | `checkable` | Bewertung im Schlüssel |
|-----|----------------|-------------|------------------------|
| `multiple-choice` | Verständnis-Check mit fester Lösung | `true` | Korrekter Buchstabe, volle Punkte nur bei Treffer |
| `short-answer` | Exakte Zahl / exaktes Wort / kurze Formel | `true` | Exakter Sollwert, ggf. Teilpunkte je Teilaufgabe |
| `code` | Programmieraufgabe | `true` | Referenzlösung + Tests/Assertions zum Selbstprüfen |
| `explain` | „In eigenen Worten"-Verständnis | `false` | Punktweiser Rubric mit Teilkriterien |

Faustregel: So viel `checkable: true` wie möglich. Jede `explain`-Aufgabe braucht einen echten Rubric,
sonst benotet sich der unbeaufsichtigte Lerner zu großzügig.

---

## 6. Vollständiges Beispiel: „Bruchrechnen Grundlagen"

Ein kompletter, kopierfertiger Kurs mit zwei Modulen. Die `manifest.json` steht bereits oben in
[§4.1](#41-manifestjson--der-vertrag). Es folgen `curriculum.md`, beide Modul-Dateien und beide
Lösungsschlüssel.

### `lehrplan/bruchrechnen-grundlagen/curriculum.md`

```markdown
# Bruchrechnen Grundlagen — Curriculum

**Level:** Anfänger · **Gesamt:** 20 Punkte · **Bestehen ab:** 16 / 20 (80 %)

Eine kurze Einführung in die Bruchrechnung. Zwei Module. Einmal vom `vater`-Skill verfasst,
unbeaufsichtigt über den `sohn`-Skill abgearbeitet.

## Module

| # | Titel | Punkte | Modul-Bestehensschwelle |
|---|-------|--------|-------------------------|
| 01 | Was ist ein Bruch? | 10 | 70 % (7/10) |
| 02 | Brüche addieren    | 10 | 70 % (7/10) |

Jedes Modul hat drei Übungen: einen Multiple-Choice-Check, eine Rechenaufgabe mit exaktem
Ergebnis und eine „Erkläre in eigenen Worten"-Frage mit Rubric.

## Hausregeln

- Arbeite die Module der Reihe nach — Modul 02 setzt Modul 01 als bestanden voraus.
- Versuche jede Übung und schreibe die Antwort in `ledger.md`, BEVOR du den Schlüssel öffnest.
- Rechne bei Rechenaufgaben wirklich — schätze nicht.
- Benote ehrlich am Rubric. Das Ledger ist prüfbar.
- Der Kurs ist erst fertig, wenn die Gesamtpunkte 16/20 erreichen.
```

### `lehrplan/bruchrechnen-grundlagen/modules/01-was-ist-ein-bruch.md`

```markdown
# Modul 01 — Was ist ein Bruch?

## Objective
Nach diesem Modul kannst du:
- Zähler und Nenner eines Bruchs benennen und ihre Bedeutung erklären.
- Einen Bruch vollständig kürzen.
- Beurteilen, ob zwei Brüche denselben Wert haben.

## Learn
Ein **Bruch** beschreibt einen Teil eines Ganzen. Er wird als zwei Zahlen übereinander geschrieben:

```

   Zähler        (wie viele Teile wir haben)
  ─────────
   Nenner        (in wie viele gleiche Teile das Ganze zerlegt ist)

```text

Beispiel: Bei `3/4` ist das Ganze in **4** gleiche Teile geteilt (Nenner), und wir betrachten **3**
davon (Zähler).

**Kürzen** heißt: Zähler und Nenner durch dieselbe Zahl teilen, ohne den Wert zu ändern. `2/4` ist
derselbe Anteil wie `1/2`, denn beide bedeuten „die Hälfte". Vollständig gekürzt ist ein Bruch, wenn
Zähler und Nenner keinen gemeinsamen Teiler außer 1 mehr haben.

Beispiel: `6/8` → beide durch 2 teilen → `3/4`. Weiter geht nicht (3 und 4 haben keinen gemeinsamen
Teiler), also ist `3/4` vollständig gekürzt.

## Exercises

### Exercise 01.1  (multiple-choice, 3 pts)
In welchem Bruch ist das Ganze in **8** gleiche Teile geteilt und **5** davon sind gemeint?

- A) 8/5
- B) 5/8
- C) 5/13
- D) 3/8

Schreibe deinen gewählten Buchstaben ins Ledger, bevor du prüfst.

### Exercise 01.2  (short-answer, 4 pts)
Kürze die folgenden vier Brüche **vollständig**. Schreibe jeweils das Ergebnis als `Zähler/Nenner`:

1. 4/8
2. 6/9
3. 10/15
4. 12/16

Notiere deine vier Antworten im Ledger.

### Exercise 01.3  (explain, 3 pts)
Erkläre in eigenen Worten, was **Zähler** und **Nenner** bedeuten und wie sie zusammen einen Anteil
beschreiben. Deine Antwort wird am Rubric im Lösungsschlüssel benotet.
```

### `lehrplan/bruchrechnen-grundlagen/answer-key/01-was-ist-ein-bruch.md`

```markdown
# Lösungsschlüssel — Modul 01

## Exercise 01.1  (multiple-choice, 3 pts)
**Korrekte Antwort: B) 5/8.**
3 Punkte nur für B. Alles andere: 0 Punkte. (Der Nenner 8 = Anzahl gleicher Teile, Zähler 5 = gemeinte
Teile.)

## Exercise 01.2  (short-answer, 4 pts)
Sollwerte (vollständig gekürzt):
1. 4/8  → **1/2**
2. 6/9  → **2/3**
3. 10/15 → **2/3**
4. 12/16 → **3/4**

Bewertung: **1 Punkt pro exakt korrekt gekürztem Bruch** (max. 4). Ein nicht vollständig gekürztes
Ergebnis (z. B. `2/4` statt `1/2`) gibt 0 Punkte für diese Teilaufgabe. Teilpunkte sind erlaubt.

## Exercise 01.3  (explain, 3 pts)
Rubric — pro Kriterium vergeben, max. 3:
- 1 Punkt — erklärt, dass der **Nenner** angibt, in wie viele gleiche Teile das Ganze geteilt ist.
- 1 Punkt — erklärt, dass der **Zähler** angibt, wie viele dieser Teile gemeint sind.
- 1 Punkt — bringt beides zu einem **Anteil/Verhältnis** zusammen (z. B. „3 von 4 Teilen").
```

### `lehrplan/bruchrechnen-grundlagen/modules/02-brueche-addieren.md`

```markdown
# Modul 02 — Brüche addieren

## Objective
Nach diesem Modul kannst du:
- Brüche mit gleichem Nenner addieren.
- Brüche mit verschiedenen Nennern auf einen gemeinsamen Nenner bringen und addieren.
- Erklären, warum ein gemeinsamer Nenner nötig ist.

## Learn
**Gleicher Nenner:** Haben zwei Brüche denselben Nenner, addierst du einfach die Zähler und lässt den
Nenner stehen:

```

  2/5 + 1/5 = 3/5

```text

**Verschiedene Nenner:** Zuerst musst du beide Brüche auf denselben Nenner bringen (einen
**gemeinsamen Nenner**). Dazu erweiterst du jeden Bruch (Zähler und Nenner mit derselben Zahl
multiplizieren), bis die Nenner gleich sind. Erst dann addierst du.

```

  1/3 + 1/4
  = 4/12 + 3/12     (1/3 mit 4 erweitert, 1/4 mit 3 erweitert)
  = 7/12

```text

Nach dem Addieren immer prüfen, ob sich das Ergebnis kürzen lässt. Beispiel: `1/6 + 1/6 = 2/6 = 1/3`.

## Exercises

### Exercise 02.1  (multiple-choice, 3 pts)
Was musst du tun, **bevor** du `1/3 + 1/4` rechnen kannst?

- A) Die Zähler direkt addieren: 1+1 = 2
- B) Beide Brüche auf einen gemeinsamen Nenner bringen
- C) Die Nenner addieren: 3+4 = 7
- D) Den größeren Bruch weglassen

Schreibe deinen Buchstaben ins Ledger, bevor du prüfst.

### Exercise 02.2  (short-answer, 4 pts)
Rechne und gib das Ergebnis **vollständig gekürzt** als `Zähler/Nenner` an:

1. 1/4 + 1/4
2. 1/3 + 1/6
3. 2/5 + 1/5
4. 1/2 + 1/3

Notiere deine vier Antworten im Ledger.

### Exercise 02.3  (explain, 3 pts)
Erkläre in eigenen Worten, **warum** man `1/3 + 1/4` nicht durch einfaches Addieren der Zähler
berechnen kann. Deine Antwort wird am Rubric benotet.
```

### `lehrplan/bruchrechnen-grundlagen/answer-key/02-brueche-addieren.md`

```markdown
# Lösungsschlüssel — Modul 02

## Exercise 02.1  (multiple-choice, 3 pts)
**Korrekte Antwort: B) Beide Brüche auf einen gemeinsamen Nenner bringen.**
3 Punkte nur für B, sonst 0.

## Exercise 02.2  (short-answer, 4 pts)
Sollwerte (vollständig gekürzt):
1. 1/4 + 1/4 = 2/4 = **1/2**
2. 1/3 + 1/6 = 2/6 + 1/6 = 3/6 = **1/2**
3. 2/5 + 1/5 = **3/5**
4. 1/2 + 1/3 = 3/6 + 2/6 = **5/6**

Bewertung: **1 Punkt pro exakt korrektem, vollständig gekürztem Ergebnis** (max. 4). Richtig gerechnet,
aber nicht gekürzt (z. B. `2/4` statt `1/2`): 0 Punkte für die Teilaufgabe. Teilpunkte erlaubt.

## Exercise 02.3  (explain, 3 pts)
Rubric — pro Kriterium vergeben, max. 3:
- 1 Punkt — erkennt, dass die Brüche **verschiedene Nenner** haben.
- 1 Punkt — erklärt, dass Zähler nur addiert werden dürfen, wenn die Teile **gleich groß** sind
  (gleicher Nenner).
- 1 Punkt — nennt die Lösung: erst auf einen **gemeinsamen Nenner** bringen, dann addieren.
```

Damit ist der Kurs vollständig: 2 Module × 10 Punkte = 20 Gesamtpunkte, Modul-Summen stimmen mit
`total_points` überein, jede Übung hat eine Bewertungsregel. Der Sohn kann sofort loslegen.

---

## 7. Tutorial: Ein kompletter Plan für die Söhne

So geht ein Vater praktisch vor.

### Schritt 1 — Umfang festlegen

Entscheide (nicht: diskutiere) Thema, Level und Umfang. Standard, wenn du unsicher bist:
**4–6 Module à ~20 Punkte**, Bestehensschwelle 80 %, pro Modul 70 %. Für ein kleines Fach reichen
2 Module wie im Beispiel oben.

### Schritt 2 — Ordner und `manifest.json` anlegen

Lege `lehrplan/<slug>/` an und schreibe zuerst die `manifest.json`. Sie ist dein Vertrag — trag hier
Module, Übungen, Punkte und Schwellen ein. Prüfe sofort die Punktarithmetik.

### Schritt 3 — Module schreiben (`modules/`)

Pro Modul: aussagekräftiges `Objective`, ein echter `Learn`-Abschnitt mit durchgerechneten Beispielen,
dann die Übungen — **ohne Lösungen**. Mische Aufgabentypen: mindestens einen prüfbaren Typ
(multiple-choice / short-answer / code) und höchstens eine `explain`-Aufgabe pro Modul.

### Schritt 4 — Lösungsschlüssel schreiben (`answer-key/`)

Pro Modul eine Datei. Für jede Übung: korrekte Antwort + Punktregel; bei `explain` ein punktweiser
Rubric. Für `code`: Referenzlösung + wie geprüft wird.

### Schritt 5 — `curriculum.md` schreiben

Der menschliche Überblick: Fächer-/Modultabelle mit Punkten, Bestehensschwelle, Hausregeln.

### Schritt 6 — Qualitäts-Check vor der Übergabe

Siehe [§8](#8-checkliste-vor-der-übergabe). Danach: **weggehen.** Nicht anbieten, live zu helfen —
das ganze Design geht davon aus, dass der Vater nicht mehr im Raum ist.

### Mehrere Söhne

Die Laufzeit-Dateien `progress.json` und `ledger.md` liegen **im Kurs-Ordner** und gehören genau
**einem Lerner**. Für mehrere Söhne hast du zwei saubere Optionen:

**Option A — ein Ordner pro Sohn (empfohlen).** Kopiere den fertigen Kurs pro Sohn. Jeder Sohn hat
seinen eigenen Fortschritt und sein eigenes prüfbares Ledger:

```text
lehrplan/
├── bruchrechnen-grundlagen-max/     # Max' Exemplar (eigenes progress.json, ledger.md)
└── bruchrechnen-grundlagen-tom/     # Toms Exemplar
```

Setze in jeder `manifest.json` den passenden `slug` (`bruchrechnen-grundlagen-max` bzw. `-tom`), damit
die Ordner eindeutig bleiben. Starten:

```text
/sohn arbeite den Lehrplan unter ./lehrplan/bruchrechnen-grundlagen-max durch
```

**Option B — verschiedene Fächer pro Sohn.** Wenn jeder Sohn ein anderes Fach lernt, ist ohnehin jeder
Kurs-Ordner ein eigenes Fach:

```text
lehrplan/
├── bruchrechnen-grundlagen/     # für den jüngeren Sohn
└── englisch-a1-vokabeln/        # für den älteren Sohn
```

> Willst du **denselben** Kurs für mehrere Söhne pflegen, ohne den Lehrinhalt zu duplizieren, halte die
> `modules/` und `answer-key/` als „Master" und kopiere pro Sohn nur einen Arbeitsordner. Aktuell
> unterstützt das `sohn`-Skill genau **eine** `progress.json`/`ledger.md` pro Ordner — deshalb ist ein
> Ordner pro Sohn der unkomplizierteste Weg.

---

## 8. Checkliste vor der Übergabe

- [ ] `summe(module.points) == total_points` in `manifest.json`.
- [ ] Pro Modul: `summe(exercise.points) == module.points`.
- [ ] Jede Modul-Schwelle (`pass_threshold_pct`) und die Gesamt-Schwelle sind gesetzt.
- [ ] Jede Übung in `modules/` hat ein Gegenstück im `answer-key/`.
- [ ] **Keine** Lösung ist in eine Modul-Datei geleakt.
- [ ] Jede `explain`-Aufgabe hat einen punktweisen Rubric.
- [ ] Jede `code`-Aufgabe nennt, wie man das Ergebnis selbst prüft (Tests/erwartete Ausgabe).
- [ ] `curriculum.md` fasst Fächer/Module, Punkte, Schwelle und Hausregeln lesbar zusammen.
- [ ] Dateipfade in `manifest.json` (`file`, `answer_key`) stimmen mit den echten Dateinamen überein.

---

## 9. Was danach passiert (damit du die Kontrolle verstehst)

Der Sohn (bzw. Claude über `/sohn`) arbeitet so — und genau das erzwingt der Plan:

1. Liest `manifest.json` als Gesetz und legt (falls neu) `progress.json` an.
2. Bearbeitet jede Übung, **schreibt seine Antwort ins `ledger.md`, bevor** er den Schlüssel öffnet.
3. Öffnet dann den `answer-key/`-Eintrag, benotet sich ehrlich, protokolliert den Score.
4. **Gate:** Erreicht er die Modul-Schwelle nicht, darf er nicht weiter — er wiederholt die verlorenen
   Übungen (neuer Versuch), bis die Schwelle fällt.
5. Kurs gilt erst als bestanden, wenn die Gesamtpunkte die Gesamt-Schwelle erreichen.

Deine Kontrolle als Vater danach ist das **`ledger.md`**: eine append-only, zeitgestempelte Spur jedes
Versuchs und jeder Selbstnote. Es ist prüfbar — geschönte Punkte fallen auf, und dann geht der Sohn
ohnehin zurück an die Arbeit. Ein reales Beispiel eines abgeschlossenen Ledgers findest du unter
[../lehrplan/git-basics/ledger.md](../lehrplan/git-basics/ledger.md); den fertigen Referenzkurs unter
[../lehrplan/git-basics/](../lehrplan/git-basics/).

---

## Referenzen

- Autor-Skill (Vater): [.claude/skills/vater/SKILL.md](../.claude/skills/vater/SKILL.md)
- Lerner-Skill (Sohn): [.claude/skills/sohn/SKILL.md](../.claude/skills/sohn/SKILL.md)
- Fertiger Beispielkurs: [../lehrplan/git-basics/](../lehrplan/git-basics/)
- Pugling-App-Study-Plans (andere Variante, per API): [tutorial.md](tutorial.md)

```text
