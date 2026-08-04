---
tags: [typ/story, status/geschaetzt, bereich/katalog]
aliases: [Externe Übungstyp-Plugins]
status: geschaetzt
prio: P3
art: Wunsch
groesse: L
wo: backend
migration: nein
vertragsbruch: nein
quelle: memory/uebungstyp-plugin-contract.md
grund: ""
ersetzt_durch: []
---

# B-23 · Übungstyp-Plugins als externe DLLs (Stufe 2)

Stufe 1 steht: `IExerciseType` + Registry mit String-Key, ein Typ = eine Klasse. Ein neuer Übungstyp
bedeutet heute aber immer noch: Code in **diesem** Repo, kompiliert, per Pull Request. Stufe 2 wäre, einen
Typ als externe, zur Laufzeit geladene Assembly einzubringen, ohne `Pugling.Api` neu zu kompilieren.

## User Story

Als *Creator/Betreiber* möchte ich einen neuen Übungstyp als eigenständig kompilierte DLL bereitstellen,
damit ein Lernverfahren ergänzt werden kann, ohne den Kern-Code von `Pugling.Api` zu ändern oder neu zu
bauen.

## Ist-Stand am Code

- **Ein Typ = eine Klasse, aber vollständig kompiliert in `Pugling.Api`:** `IExerciseType`
  ([backend/Pugling.Api/Exercises/IExerciseType.cs](../../backend/Pugling.Api/Exercises/IExerciseType.cs))
  wird über `ExerciseTypeRegistry` aufgelöst
  ([backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs:14-15](../../backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs)),
  gebaut aus **DI-registrierten Singletons**: `AddExerciseTypes` zählt jeden eingebauten Typ **einzeln** per
  `services.AddSingleton<IExerciseType, X>()` auf
  ([ExerciseTypeRegistry.cs:43-59](../../backend/Pugling.Api/Exercises/ExerciseTypeRegistry.cs)). Die
  Registry wirft schon heute beim Bau, wenn zwei Typen denselben `Key` tragen (`ToDictionary` mit
  `StringComparer.Ordinal` löst bei Duplikaten eine `ArgumentException` aus) – ein Kollisions-Schutz ist
  also implizit bereits da, nur die Fehlermeldung ist die generische von `ToDictionary`.
- **Kein Lademechanismus existiert:** Weder `AssemblyLoadContext` noch MEF noch ein Plugin-Verzeichnis
  kommen im Backend vor (Volltextsuche über `backend/` ohne Treffer). Alles, was heute „Übungstyp" heißt,
  liegt in [Exercises/BuiltInExerciseTypes.cs](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)
  bzw. eigenen Dateien wie `VocabularyExerciseType.cs`.
- **Die generischen Spielpfade sind bereits plugin-tauglich:** `PositionPracticeController`,
  `PositionTestsController`, `ExercisePreviewService` und `ExerciseContentProvider` sprechen ausschließlich
  gegen `IExerciseType`/`ExerciseTypeRegistry`, nie gegen eine konkrete Typklasse (bestätigt durch die
  Fundstellen der Registry-Suche). Ein extern geladener Typ würde hier **ohne Codeänderung** mitspielen.
- **Die Creator-CRUD-Seite ist es nicht:** Jeder Typ trägt einen **eigenen, kompilierten Controller**, der
  von `ExerciseControllerBase<TConfig>` erbt – ein **generischer Basistyp mit Compile-Zeit-`TConfig`**
  ([backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:24-25](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs)).
  Die Checkliste für einen neuen Typ verlangt ausdrücklich Schritt 5 „Controller … erbt CRUD"
  ([.claude/commands/neuer-uebungstyp.md:47-52](../../.claude/commands/neuer-uebungstyp.md)). ASP.NET Core
  entdeckt Controller-Typen über den `ApplicationPartManager` normalerweise nur in der Einstiegs-Assembly und
  ihren Projektreferenzen – eine zur Laufzeit nachgeladene DLL bräuchte eine explizite
  `ApplicationPart`-Registrierung, sonst bliebe ihr Controller unerreichbar, obwohl der Typ selbst in der
  Registry stünde.
- **Kein In-Process-Sandboxing:** `AssemblyLoadContext` isoliert Assemblies nur für **Entladbarkeit**, nicht
  für **Sicherheit** – .NET Core hat kein Nachfolger für die alte CAS-/AppDomain-Sandbox. Eine geladene DLL
  läuft mit denselben Rechten wie `Pugling.Api` selbst: vollem DB-Zugriff (inkl. Wallet-Buchungen, Kinddaten),
  demselben JWT-Signierschlüssel, demselben Prozessspeicher. Echte Isolation gegenüber **nicht**-vertrauten
  Autoren bräuchte einen separaten Prozess (IPC/gRPC) – eine ganz andere Größenordnung.

## Die echte Lücke

Nicht „lädt eine Assembly zur Laufzeit" – das ist der leichte Teil (`Assembly.LoadFrom` + Reflection ist
Bordmittel). Die echte Lücke ist zweigeteilt:

1. **Die heutige Ein-Typ-eine-Klasse-Architektur ist nur für den *Inhalt* fertig, nicht für die *CRUD-Seite*.**
   Ein Plugin bräuchte nicht nur eine `IExerciseType`-Implementierung, sondern auch einen eigenen,
   kompilierten `ExerciseControllerBase<TConfig>`-Ableger – der Autor schreibt also weiterhin echten
   ASP.NET-Code, nur in einem separaten Projekt statt in diesem Repo. „Ein Typ = eine Klasse" wird zu
   „ein Typ = eine Klasse + ein Controller + eine DLL".
2. **Es gibt kein Vertrauensmodell für ausführbaren Fremdcode**, weil .NET keins mitliefert. Jede Antwort auf
   „wie laden wir das sicher" muss darum zuerst beantworten: **wessen** Code ist das? Für Code, den der
   Betreiber selbst kompiliert und lokal bereitstellt, ist das Vertrauensniveau identisch mit dem der
   Kern-App – Laden ist dann ein reines Deployment-Feature. Für Code eines **dritten** Autors bräuchte es
   Isolation, die es in-process nicht gibt.

Und die im Idee-Text gestellte Frage bleibt der Dreh- und Angelpunkt: **Ohne einen konkreten zweiten
Plugin-Autor** außerhalb dieses Repos ist Fall 2 (Fremd-Autor, Sandbox) reine Spekulation – gebaut wird hier
ausschließlich Fall 1 (Betreiber lädt eigene, selbst kompilierte DLLs nach).

## Offene Punkte

1. ~~Gibt es je einen Plugin-Autor außer diesem Repo?~~ → siehe Entscheidung 1.
2. ~~Wie werden CRUD-Controller für einen Plugin-Typ erreichbar, wenn die Basis heute Compile-Zeit-generisch
   ist?~~ → siehe Entscheidung 3.
3. ~~Wie wird Vertrauen zu ausführbarem Fremdcode hergestellt (Sandbox)?~~ → siehe Entscheidung 1.
4. ~~Woher bezieht eine Plugin-DLL die Typen (`IExerciseType`, `ExerciseTypeManifest`, `ContentItem`, …),
   ohne das ausführbare `Pugling.Api`-Projekt selbst zu referenzieren?~~ → siehe Entscheidung 2.
5. ~~Was passiert bei einer Schlüssel-Kollision zwischen Plugin und eingebautem Typ (oder zwei Plugins)?~~ →
   siehe Entscheidung 6 (im Kern schon durch `ToDictionary` gelöst).
6. ~~Greifen die reflexiven Wächter (`ExerciseTypeManifestTests`, Endpunkt-Abdeckungs-Wächter) auch auf
   Plugin-Typen?~~ → siehe Entscheidung 4.
7. ~~Laden zur Laufzeit (Hot-Reload/Upload durch den Creator) oder nur beim Start?~~ → siehe Entscheidung 1.

## Entscheidungen

1. **Vertrauensgrenze: nur betreiber-eigene DLLs, geladen beim Start – kein Marktplatz, kein Runtime-Upload,
   keine Fremd-Sandbox.** Begründung: .NET Core bietet keine In-Process-Sandbox für nicht-vertrauten Code
   (siehe Ist-Stand); echte Isolation bräuchte einen separaten Prozess mit IPC – ein komplett anderes,
   deutlich größeres Vorhaben, das hier niemand angefragt hat. Die Idee selbst zweifelt zu Recht, ob es
   überhaupt einen externen Plugin-Autor gibt. Kosten: die „Plugin-Marktplatz"-Fantasie (Creator lädt fremden
   Code über die UI hoch) ist damit **ausdrücklich außerhalb** dieser Story – falls sie je gebraucht wird,
   ist das eine eigene, deutlich größere Story mit Prozess-Isolation. Gewonnen wird dafür ein reines
   Deployment-Feature: ein neuer Typ kann als separates Projekt/DLL neben `Pugling.Api` gebaut und ohne
   Merge in dieses Repo ausgeliefert werden – der eigentliche, belegbare Wunsch hinter der Idee.
2. **Ein schmales Plugin-SDK-Projekt trägt die referenzierbaren Typen.** `Pugling.Api` ist ein ausführbares
   Web-Projekt (kein Class-Library-Ziel für Dritte); ein Plugin-Projekt kann es nicht sinnvoll referenzieren,
   ohne `Program.cs`, `PuglingDbContext` & Co. mitzuziehen. Begründung: die Registry, die
   Spiel-/Test-/Preview-Pfade und die Config-Deserialisierung brauchen nur eine Handvoll Typen
   (`IExerciseType`, `IGeneratingExerciseType`, `ExerciseTypeManifest`, `ContentItem`, `StoreResolution`,
   `CheckResult`/`ItemCheck`/`GivenAnswer`). Kosten: ein neues, kleines Projekt (Arbeitstitel
   `Pugling.ExercisePlugins.Abi`), das `Pugling.Api` und jedes Plugin gemeinsam referenzieren – ein weiterer
   Eintrag in der Liste der Backend-Projekte samt eigener `CLAUDE.md`-Notiz, plus die einmalige Verschiebung
   der genannten Typen dorthin (mechanischer Umbau, keine Verhaltensänderung).
3. **Ein Plugin liefert seinen eigenen, kompilierten CRUD-Controller mit; der Host registriert dessen
   Assembly als `ApplicationPart`.** Begründung: `ExerciseControllerBase<TConfig>` bleibt Compile-Zeit-generisch
   (kein Umbau auf ein untypisiertes `JsonElement`-Config-Modell, das würde die Typsicherheit für *alle*
   bestehenden Typen aufweichen, nicht nur für Plugins). Ein Plugin-Autor schreibt also weiterhin einen
   echten `ExerciseControllerBase<XConfig>`-Ableger nach demselben Muster wie ein eingebauter Typ – nur in
   seinem eigenen Projekt. `Program.cs` ruft vor `builder.Build()` für jede geladene Plugin-Assembly
   `builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(assembly))` auf.
   Kosten: „ein Typ = eine Klasse" wird für Plugins zu „ein Typ = eine Klasse + ein Controller + ein
   Projekt" – ehrlicher, aber schwerer als der ursprüngliche Ein-Zeiler-Eintrag in `AddExerciseTypes`.
4. **Die reflexiven Wächter dieses Repos (`ExerciseTypeManifestTests`, Endpunkt-Abdeckungs-Wächter) prüfen
   Plugin-Typen nicht mit.** Begründung: sie laufen in `Pugling.Api.Tests` gegen `factory.Services` – ein
   Plugin wird im normalen Testlauf gar nicht geladen (kein Plugin-Verzeichnis konfiguriert). Kosten: das ist
   ein bewusster blinder Fleck – ein fehlerhafter Plugin-Typ (z. B. Manifest ohne `PlayRoute` bei
   `StudyPlanTest`) fällt nicht mechanisch auf, sondern erst im Betrieb. Konsistent mit Entscheidung 1: wer
   die DLL bereitstellt, ist derselbe Betreiber, der auch die Tests dafür verantwortet (in seinem eigenen
   Projekt/CI) – kein zusätzliches Risiko gegenüber dem bestehenden Vertrauensmodell.
5. **Laden ausschließlich beim App-Start, kein Hot-Reload, kein Entladen.** Begründung: `AssemblyLoadContext`
   böte technisch Entladbarkeit, aber ein Austausch zur Laufzeit reißt laufende Requests/Registry-Referenzen
   und bräuchte einen Re-Registrierungs-Mechanismus für DI-Singletons und `ApplicationPart`s, den ASP.NET
   Core nicht vorsieht. Kosten: ein geänderter Plugin-Stand braucht einen App-Neustart – akzeptabel, weil
   Stufe 2 laut Entscheidung 1 ohnehin kein Runtime-Feature für den Creator ist, sondern ein
   Deployment-Schritt des Betreibers.
6. **Schlüssel-Kollision bricht den Start hart ab, mit einer sprechenden Meldung statt der generischen
   `ToDictionary`-Exception.** Begründung: `ExerciseTypeRegistry`s Konstruktor wirft bei doppeltem `Key`
   bereits heute (`ToDictionary` mit `StringComparer.Ordinal`) – das Verhalten „laut scheitern statt still
   überschreiben" ist also schon Konvention (vgl. `Require` im selben Typ). Kosten: eine kleine, gezielte
   Änderung (eigene Schleife mit klarer `InvalidOperationException`-Meldung statt der Framework-Exception),
   kein neues Konzept.
7. **Priorität bleibt niedrig (P3), Bau erst bei einem konkreten zweiten Plugin-Autor.** Begründung: `geschätzt`
   heißt „baubereit, wenn priorisiert" – nicht „jetzt bauen". Ohne einen belegten Bedarf (siehe Idee-Text)
   wäre ein Bau jetzt Architektur um ihrer selbst willen. Kosten: keine – die Story liegt fertig geschätzt,
   bis ein echter Anlass die Priorität hebt.

## Akzeptanzkriterien

1. Eine als eigenes Projekt kompilierte DLL, die das Plugin-SDK referenziert und einen `IExerciseType` plus
   einen `ExerciseControllerBase<TConfig>`-Ableger enthält, taucht nach einem App-Neustart mit
   konfiguriertem Plugin-Verzeichnis in `GET api/v1/creator/exercise-types` auf – ohne Codeänderung an
   `Pugling.Api`.
2. Der mitgelieferte Controller des Plugins ist über seine eigene Route erreichbar; Standard-CRUD (List,
   Get, Create, Update, Delete) funktioniert für Übungen dieses Typs.
3. Die generischen Spielpfade (`PositionPracticeController`, `PositionTestsController`,
   `ExercisePreviewService`) spielen den Plugin-Typ korrekt aus – ohne Sonderfall-Code für Plugins.
4. Zwei Typen (Plugin↔eingebaut oder Plugin↔Plugin) mit demselben `Key` lassen den Start mit einer klaren,
   geloggten Fehlermeldung abbrechen – nie eine stille Überschreibung.
5. Kein konfiguriertes oder ein leeres Plugin-Verzeichnis verhält sich **byte-identisch** zu heute (nur die
   eingebauten Typen) – Stufe 2 ist rein additiv.
6. Die Vertrauensgrenze steht dokumentiert (README/CLAUDE.md des Bereichs): Plugins sind
   betreiber-vertrauter Code, geladen aus einem lokalen, für die Creator-Rolle nicht beschreibbaren
   Verzeichnis, ausschließlich beim Start. Ein Runtime-Upload-Endpunkt oder eine Fremd-Autor-Sandbox sind
   ausdrücklich nicht Teil dieser Story (Entscheidung 1).

## Schätzung

**Größe: L** — bewusst **nicht** XL, weil der Umfang über Entscheidung 1 hart begrenzt ist: kein
Marktplatz, kein Runtime-Upload, keine Fremd-Sandbox (das wäre eine andere, XL-große Story mit
Prozess-Isolation). Innerhalb dieser Grenze bleibt es eine **einzelne, bounded Architektur-Änderung**
(SDK-Extraktion + Start-Zeit-Lademechanismus + `ApplicationPart`-Registrierung) – vergleichbar mit dem
L-Anker „eine DB-Umbau-Etappe wie E6": mehrere zusammenhängende, aber klar geschnittene Schritte in einer
Sitzungsfolge, kein Split in eigenständige Stories nötig, weil kein Schritt für sich allein einen
Endnutzen trägt.

**Risiken:**

- **Sicherheit (zentral):** ein geladenes Plugin läuft mit vollem Prozess-Vertrauen – DB, JWT-Schlüssel,
  Wallet-Mutationen. Mitigiert **nicht** durch Technik (die gibt es in .NET Core in-process nicht), sondern
  durch die Entscheidung, nur betreiber-eigene DLLs zu laden. Eine spätere Erweiterung auf Fremd-Autoren
  bräuchte eine komplett neue Sicherheitsarchitektur, keinen Ausbau dieser.
- **Startup-Robustheit:** eine defekte/inkompatible Plugin-DLL (falsches Ziel-Framework, fehlende
  Abhängigkeiten) darf den App-Start nicht unkontrolliert crashen lassen – Entscheidung nötig beim Bau:
  hart abbrechen (konsistent mit der Kollisions-Regel) oder überspringen-und-loggen (resilienter, aber
  verdeckt einen kaputten Typ im Betrieb). Für Stufe 2 wird **hart abbrechen** empfohlen, konsistent mit
  Entscheidung 6.
- **Versions-Drift:** das Plugin-SDK-Projekt muss mit `Pugling.Api` im Ziel-Framework/den
  EF-/ASP.NET-Core-Paketversionen kompatibel bleiben – ein Plugin, das gegen eine ältere/neuere SDK-Version
  kompiliert wurde, lädt im schlimmsten Fall mit einem kryptischen `MissingMethodException` statt einer
  klaren Meldung.
- **Blinder Fleck bei den Wächtern** (Entscheidung 4, bewusst in Kauf genommen): reflexive Tests dieses
  Repos sehen Plugin-Code nicht.

**Angriffsplan** (rein Backend, in dieser Reihenfolge):

1. Plugin-SDK-Projekt anlegen, die in Entscheidung 2 genannten Typen dorthin verschieben (mechanisch, keine
   Verhaltensänderung – eigener Commit, damit der Diff prüfbar bleibt).
2. Lademechanismus in `Program.cs`: `ExercisePlugins:Directory`-Konfiguration lesen, `*.dll` per
   `Assembly.LoadFrom` laden, per Reflection auf `IExerciseType`-Implementierungen scannen und als
   Singleton registrieren, Assembly zusätzlich als `ApplicationPart` eintragen – alles vor `builder.Build()`.
3. `ExerciseTypeRegistry`s Kollisions-Meldung schärfen (Entscheidung 6).
4. Eine Test-Plugin-DLL (eigenes kleines Projekt unter `backend/`, nicht Teil der Solution-weiten
   Produktionskette) als Fixture bauen, gegen die der Testweg unten läuft.
5. Dokumentation: `.claude/commands/neuer-uebungstyp.md` um einen „Stufe 2: externes Plugin"-Absatz
   erweitern, [wiki/08-erweitern.md](../../wiki/08-erweitern.md) ergänzen, Vertrauensgrenze in
   `backend/Pugling.Api/CLAUDE.md` dokumentieren.

**Testweg:** ein neuer Integrationstest (`PluginLoadingTests` in `Pugling.Api.Tests`, Muster wie
`ExerciseTypeManifestTests`), der `ExercisePlugins:Directory` per Test-Factory-Konfiguration auf einen
Fixture-Ordner mit der vorgebauten Test-Plugin-DLL zeigen lässt und prüft: (a) der Plugin-Typ erscheint
genau einmal im Manifest, (b) sein Controller antwortet über den In-Memory-`WebApplicationFactory`-Client,
(c) eine zweite Fixture mit kollidierendem `Key` lässt den Factory-Aufbau mit der erwarteten Meldung
scheitern. Ergänzend ein manueller `/smoke-test`-Lauf mit echtem Plugin-Verzeichnis vor der Abnahme.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen `IExerciseType`/`ExerciseTypeRegistry`/
  `ExerciseControllerBase<TConfig>` recherchiert – kein Lademechanismus vorhanden, generische Spielpfade
  bereits plugin-tauglich, CRUD-Controller aber Compile-Zeit-generisch; echte Lücke geschärft (Autoren-
  vs. Sandbox-Frage).
- **2026-08-03** — gegrillt: sieben offene Punkte in nummerierte Entscheidungen überführt, autonom
  getroffen (Nutzerauftrag 2026-08-04) — Kernentscheidung: nur betreiber-eigene DLLs beim Start, kein
  Marktplatz, keine Fremd-Sandbox (die gibt es in .NET Core in-process ohnehin nicht).
- **2026-08-03** — geschätzt: Größe **L** (bewusst nicht XL, Umfang durch Entscheidung 1 begrenzt),
  `wo: backend`, `migration: nein`, `vertragsbruch: nein`, Angriffsplan und Testweg festgelegt, autonom
  getroffen (Nutzerauftrag 2026-08-04).
