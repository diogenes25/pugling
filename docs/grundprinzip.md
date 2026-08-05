---
tags: [typ/konzept, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Grundprinzip, Drei-Ebenen-Modell, Creator-Supervisor-Student]
---

# Grundprinzip von Pugling

Pugling ist eine Lern-App mit Punktesystem (Leitner-Prinzip). Das fachliche Grundmodell teilt die
Beteiligten in **drei Ebenen**, die sauber getrennte Aufgaben haben. Wer diese Aufteilung verinnerlicht,
versteht, warum die API so geschnitten ist: Inhalt, Steuerung und Lernen liegen bewusst in verschiedenen
Händen.

← [Zurück zum Wiki-Index](../README.md) · Technische Landkarte: [wiki/01 · Überblick & Architektur](../wiki/01-ueberblick-architektur.md)

---

## Die drei Ebenen im Überblick

| Ebene | Rolle | Analogie | Verantwortet |
| --- | --- | --- | --- |
| **1. Creator** | Ersteller von Inhalten | Lehrer / Schulbuchautor | Baut **Übungen** für Fächer und Kapitel. Liefert den reinen Lernstoff. |
| **2. Supervisor** | Steuerung | Lehrer / Erziehungsberechtigter | Baut aus vorhandenen Übungen **Lehrpläne**, setzt **Ziele** (Tag/Woche/Hausaufgabe) und **Punkte**, betreibt den **Shop** mit Artikeln, Preisen und Bestand. |
| **3. Student** | Lernen | Schüler | Sieht die Lehrpläne, **erreicht die Ziele**, verdient dabei **Münzen/Gems** und gibt sie im **Shop** aus. |

Der rote Faden: **Der Creator liefert den Stoff, der Supervisor macht daraus eine verbindliche
Lernaufgabe mit Belohnung, der Student lernt und wird belohnt.** Reale Belohnungen (Taschengeld,
Bildschirmzeit) werden außerhalb der App eingelöst – die App hält nur den Vertrag und den Kontostand.

---

## Ebene 1 · Creator — der Inhalt

Der Creator ist vergleichbar mit einem **Lehrer oder Schulbuchautor**. Er erstellt Übungen für
unterschiedliche Fächer (`Subject`) und Kapitel (`Chapter`) – zum Beispiel einen Satz von 200 Vokabeln
für „Englisch, Unit 3". Diese Übungen sind **kindneutral**: Der Creator weiß nichts von einzelnen
Kindern, Zielen oder Punkten.[^1] Er füllt allein die **globale Übungs-Bibliothek** (den Lern-Katalog). Die
Bibliothek ist für alle Creator **lesbar**; ändern/löschen darf nur ein **Owner** der Übung, und über
**RWX-Grants** (Owner/Write/Execute) kann ein Owner einzelnen Creatorn gezielt Mit-Pflege oder das Zuweisen
erlauben bzw. das Zuweisen für alle abschalten (`executePublic`).

- Der Creator entscheidet über den **Übungstyp** (Vokabeltraining, Lückentext, Rechnen …) und den
  konkreten Inhalt.
- Er sagt **nichts** darüber, wie oft, für wie viele Punkte oder mit welchem Ziel geübt wird – das ist
  Sache der zweiten Ebene.

> **Umsetzung:** Die drei Ebenen sind **Rollen** (`ProfileRole` Creator/Supervisor/Student), entkoppelt
> vom Login. Ein `Account` trägt eine oder mehrere Rollen über `AccountProfile`-Zeilen; ein Vater ist
> zugleich Creator **und** Supervisor (ein Token, beide Ebenen). Die REST-API ist nach den Ebenen
> geschnitten: `api/v1/creator/…`, `api/v1/supervisor/…`, `api/v1/student/…`. Die dedizierte, vom
> Vater getrennte Creator-/Lehrer-Rolle (eigenes Konto, Beitrittscode) ist damit vorbereitet; heute
> pflegt der Vater den Katalog. Fachlich gilt: **Inhalt ≠ Lehrplan.**

[^1]: Gilt für die Katalog-**Entität** (`Exercise` trägt keine `ChildId`) — nicht mehr uneingeschränkt
    für den Creator-**Arbeitsplatz**: `GET creator/profiles/match?childId=` liest bewusst kindbezogen und
    ist ownership-geprüft. Die fachliche Neuverhandlung dieses Grenzfalls ist
    [B-46](backlog/B-46-interessenbasierte-uebungen.md) zugewiesen.

---

## Ebene 2 · Supervisor — die Steuerung

Der Vater ist der **Supervisor**. Er greift **nicht** in den Lernstoff selbst ein, sondern arrangiert
ihn und legt die Spielregeln fest. Er hat drei Werkzeuge:

### a) Lehrpläne bauen

Aus den vorhandenen Creator-Übungen stellt der Supervisor **Lehrpläne** (`StudyPlan`) zusammen. Ein
Lehrplan ist ein Container aus vielen **Positionen** – jede Position verweist auf eine Katalog-Übung und
trägt ihr **eigenes Ziel und ihre eigenen Punkte**.

### b) Ziele und Punkte definieren

Zu jeder Position setzt der Supervisor ein Ziel – vergleichbar mit **Hausaufgaben, Wochenaufgaben oder
Tagesaufgaben**, die erreicht werden müssen (Rhythmus `None|Daily|Weekly` + Bestehensschwelle). Für das
Üben und das Erreichen der Ziele vergibt er **Punkte** (Münzen und/oder Gems).

### c) Den Shop bestücken

Der Supervisor erstellt **Artikel** und gibt ihnen **Preise**. Diese Artikel sind in der Regel
**immaterielle Dinge**, die der Student später einlösen kann: Spielzeit, Naschzeit, Taschengeld. Der Shop
hat einen **Bestand / ein Kontingent**: So kann etwa der Artikel „Taschengeld" nur **einmal pro Woche**
eingelöst werden. Der Student bezahlt mit den Münzen, die er sich zuvor durch die Übungen erlernt hat –
zu Preisen, die ebenfalls der Supervisor festgelegt hat.

---

## Ebene 3 · Student — das Lernen

Der Student (der Schüler) sieht die **Lehrpläne des Supervisors**, in denen die **Übungen des Creators**
stecken, und muss die vom Supervisor gesetzten **Ziele erreichen**. Das ist der Pflichtteil.

Darüber hinaus **verdient** der Student beim Üben **Münzen und/oder Gems**. Damit kann er im Shop, den
der Supervisor bereitstellt, **Dinge kaufen**. Gekaufte Artikel kann er anschließend **aktivieren**. Die
Aktivierung erzeugt eine **Meldung an den Supervisor**, der die gekauften und aktivierten Artikel
seinerseits **einlösen / bestätigen** muss. Der Supervisor hält das im Backend fest – die reale Handlung
dahinter (das Kind darf 10 Minuten fernsehen) passiert **außerhalb** der App.

---

## Der Loop an einem konkreten Beispiel

1. **Creator** stellt eine Übung mit **200 Vokabeln** bereit (Fach + Kapitel).
2. **Vater** nimmt diese Übung in einen Lehrplan auf, setzt das Ziel „lerne diese 200 Vokabeln" und
   verspricht dafür **150 Münzen**. Im Shop legt er den Artikel **„10 Minuten Computerzeit"** für
   **100 Münzen** an.
3. **Kind** lernt die 200 Vokabeln, erreicht das Ziel und bekommt die **150 Münzen** gutgeschrieben.
4. **Kind** kauft im Shop für **100 Münzen** die „10 Minuten Computerzeit" (Saldo danach: 50 Münzen)
   und **aktiviert** den Artikel.
5. **Vater** erhält die Aktivierungs-Meldung und **löst sie ein / bestätigt** sie im Backend.
6. **Außerhalb der App:** Der Vater sorgt dafür, dass das Kind tatsächlich 10 Minuten fernsehen /
   am Computer sein darf. Die App kümmert sich nicht um die reale Einlösung – nur um den Vertrag,
   die Punkte und den bestätigten Status.

---

## Technische Umsetzung (Kurzabriss)

- **Identität ≠ Rolle:** `Account` (Login + PIN-Hash) → `AccountProfile[]` (`Role` → `Adult`/`Child`-Profil).
  Das JWT trägt `aid` und **mehrere** Rollen-Claims; `AuthAccess` prüft Eigentum OR-verknüpft je Rolle.
  Login über `api/v1/auth/{adult|child}` oder konto-zentrisch `api/v1/auth/login`.
- **Ein Student, mehrere Supervisor:** `SupervisorLink` (Supervisor ⇢ Student) ersetzt die frühere
  1:1-Bindung. Betreuung verwalten: `…/supervisor/children/{id}/supervisors`.
- **Gemeinsames Wallet, ausstellergebundene Einlösung:** Der Punkte-Ledger (`ChildPointsEntry`) ist rein
  student-skopiert – ein Saldo über alle Supervisor. Die Zuordnung „wer löst ein" ist eine **Momentaufnahme**
  (`SupervisorId`) auf `ShopPurchase`/`ActivationRequest`: nur der ausstellende
  Supervisor sieht und erfüllt/genehmigt den Kauf; die Student-Shop-Sicht aggregiert alle Supervisor.
- **API nach Ebenen** (`ApiRoutes.Creator/Supervisor/Student`); Code ebenso in `Controllers/{Tier}` und
  `Services/{Creator,Supervisor,Student,Shared}`. Das URL-Präfix ist Taxonomie, nicht die Auth-Wand –
  einzelne Routen sind bewusst dual (z. B. liest der Supervisor eine Student-getaggte Report-Route).

## Warum diese Trennung wichtig ist

- **Inhalt ist wiederverwendbar.** Dieselbe Creator-Übung kann in beliebig vielen Lehrplänen
  verschiedener Supervisor auftauchen, jeweils mit eigenen Zielen und Preisen.
- **Steuerung ist familienspezifisch.** Ziele, Punkte und Shop-Preise passt jeder Supervisor an seinen
  Student an, ohne den Lernstoff zu verändern.
- **Lernen ist gekapselt und geschützt.** Der Student sieht nur seine eigenen Pläne, und der Server
  bestimmt autoritativ, ob ein Ziel erreicht ist und wie viele Punkte fließen – das verhindert
  Selbstbetrug.
- **Die Belohnungs-Ökonomie hat zwei Enden.** Münzen fürs Lernen (Supervisor-Angebote, Shop-Artikel) und
  Gems aus Boni (Skins) trennen „Pflicht wird belohnt" von „Extra-Motivation". Reale Belohnungen bleiben
  eine bewusste **manuelle** Handlung des Supervisors – die App erzwingt nur die Buchführung.

---

**Verwandt:** [wiki/01 · Überblick & Architektur](../wiki/01-ueberblick-architektur.md) ·
[wiki/04 · Lernplan bauen](../wiki/04-lernplan-bauen.md) ·
[wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md) ·
[docs/endpunkt-beziehungen.md](endpunkt-beziehungen.md) · [CLAUDE.md](../CLAUDE.md)
