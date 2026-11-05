---
tags: [typ/story, status/idee, bereich/backend, rolle/creator, rolle/student]
aliases: [Inline-Typen ohne Sperre, Wort-Tausch per PATCH]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: B-323 (Lauf J, docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-328 · Sollen die Inhaltslisten gespielter Übungen anderer Typen (und der Wort-Tausch) gesperrt sein?

[B-323](B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md) hat die Wörter-Sperre einer **Vokabelübung** auf jeden
schreibenden Weg gezogen. Zwei verwandte Wege bleiben offen, und beide sind eine Produktentscheidung, keine
Reparatur – darum `Frage`:

- **Inline-Typen** (Übersetzung, Grammatik, Lückentext ohne Store-Bezug …): ihr Inhalt ist die Config, und der
  `PUT` ersetzt sie ohne jede Prüfung (`ExerciseControllerBase.UpdateConflictAsync` liefert für sie `null`).
  `PositionItemProgress.ItemIndex` folgt der Listenposition (`ExerciseContentProvider.ItemsOf`), also verschiebt ein
  gestrichener Satz den Lernstand aller späteren. Eine Sperre wie bei Vokabeln würde aber auch das Korrigieren eines
  Tippfehlers in einem gespielten Satz verbieten – bei Vokabeln geschieht das im Store, hier gibt es keinen Store.
- **Wort-Tausch per `PATCH …/items/{id}`** mit neuer `vocabularyId`: der Index bleibt, das Wort wechselt, der
  Lernstand gehört danach zum neuen Wort. Die Sperre nennt nur Löschen und Umsortieren (`ExerciseControllers.cs`
  `PatchItem`), der Test `ItemMutation_BeiInPlanUebung_BlocktIndexVerschiebung_ErlaubtAnhaengen` deckt den Tausch nicht.

Zu entscheiden: Ist ein Tausch/eine Textänderung an gespielter Stelle eine **Korrektur** (erlaubt, Lernstand bleibt)
oder ein **Ersatz** (gesperrt)? Bei den Inline-Typen ggf. nur Länge/Reihenfolge sperren, Text erlauben.

## Verlauf

- **2026-10-28** — angelegt beim Bau von B-323 in Lauf J ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code
  gelesen, nicht gemessen. Bewusst nicht in B-323 genommen: `Frage`, im Nachtlauf nicht grillbar (Freigabe 1).
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) Inline-Typen: Länge und Reihenfolge gespielter Inhaltslisten gesperrt, Textänderung an einer Stelle erlaubt (Korrektur); (2) Wort-Tausch per `PATCH …/items/{id}` mit neuer `vocabularyId` ist Ersatz und gesperrt, korrigiert wird im Store. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
