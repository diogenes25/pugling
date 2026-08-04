// Der Vertrag der Pugling-API als TypeScript – **erzeugt**, nicht von Hand gepflegt.
//
// Jeder Alias unten zeigt auf ein Schema in [contract.ts](contract.ts), das `npm run gen:contract` aus
// docs/openapi/v1.json erzeugt (das Dokument schreibt `ContractDocumentTests`, ein CI-Tor hält es aktuell).
// Ein umbenanntes oder entferntes Feld im Backend bricht damit `tsc -b` – vorher fiel es erst in Playwright
// oder im Betrieb als `400 unknown_field` auf.
//
// **Neuer Endpunkt, neues DTO?** Nichts hier von Hand ergänzen: Backend bauen, Testlauf schreibt das
// Dokument, `npm run gen:contract`, dann eine Alias-Zeile. Der Name links darf vom Schema-Namen abweichen
// (die Oberfläche hat eigene Vokabeln), rechts steht die Wahrheit.
//
// Was **nicht** aus dem Dokument kommen kann, liegt in [uiTypes.ts](uiTypes.ts) – elf Typen, je mit Grund.
//
// Nach einem Zweigwechsel mit geändertem Dokument hält der Editor die alte `contract.ts`, bis etwas baut:
// dann `npm run gen:contract`. CI kann das nicht treffen (`postinstall` läuft dort immer zuerst).

import type { components } from "./contract";

type S = components["schemas"];

export type * from "./uiTypes";

export type PartOfSpeech = S["PartOfSpeech"];

/** Grammatikalisches Geschlecht eines Substantivs. */
export type Genus = S["Genus"];

/** Substantiv-spezifische Angaben (Teil des komplexen Vokabel-Datensatzes). */
export type NounInfo = S["NounInfo"];

/** Verb-spezifische Angaben / Konjugations-Metadaten. */
export type VerbInfo = S["VerbInfo"];

/** Geschlecht des Kindes – Teil des Profils, aus dem der KI-Creator seine Anrede/Einkleidung ableitet. */
export type Gender = S["Gender"];

/**
 * Eine **einzelne** Schulart. Serverseitig ist `SchoolTypes` ein `[Flags]`-Enum, das Schema listet aber die
 * Einzelnamen – für Auswahl und Filter ist genau das richtig. Die **Kombination** („Realschule, Gymnasium")
 * reist als freier String und ist im Dokument nicht ausdrückbar (B-60).
 */
export type SchoolType = S["SchoolTypes"];

export type LoginResponse = S["LoginResponse"];

// ---- Vater: eigenes Konto (Registrierung + Selbstauskunft) ----

/** Der eigene Erwachsenen-Datensatz; die PIN liefert der Server nie aus. */
export type AdultResponse = S["AdultResponse"];

/** Registrierung eines Vaters – der einzige Weg, ohne Anmeldung ein Konto zu erzeugen. */
export type CreateAdultDto = S["CreateAdultDto"];

/** Partielle Änderung des eigenen Kontos; weggelassene Felder bleiben unverändert. */
export type UpdateAdultDto = S["UpdateAdultDto"];

// ---- Vater: Kinder & Vokabel-Store ----

export type ChildResponse = S["ChildResponse"];

// ---- Unterrichtsmaterial: Lehrwerk-Reihe → Unit → Lehrbuch des Kindes ----

/**
 * Eine Lehrwerk-Reihe („Access") – bewusst ein **geteilter** Katalog-Eintrag: das Lehrbuch des Kindes
 * und das Creator-Profil zeigen auf denselben Datensatz. Nur dadurch ist die Frage „welcher Creator
 * kennt das Material dieses Kindes?" berechenbar statt ein Namensvergleich.
 */
export type TextbookSeriesResponse = S["TextbookSeriesResponse"];

export type CreateTextbookSeriesDto = S["CreateTextbookSeriesDto"];

/** Partielle Änderung einer Reihe; der Slug bleibt fest. */
export type UpdateTextbookSeriesDto = S["UpdateTextbookSeriesDto"];

/**
 * Eine Unit der Reihe samt Band (`grade`). `topics`/`grammar`/`vocabularyNotes` sind der eigentliche
 * Gewinn: sie sind der Stoff, den ein KI-Creator liest, statt ihn zu erfinden.
 */
export type SeriesUnitResponse = S["SeriesUnitResponse"];

export type CreateSeriesUnitDto = S["CreateSeriesUnitDto"];

export type UpdateSeriesUnitDto = S["UpdateSeriesUnitDto"];

/**
 * Ein vom Kind benutztes Lehrbuch. `seriesId`/`currentUnitId` sind die katalogisierte Form von Titel
 * und Kapitel – erst sie machen aus „irgendein Buch" den auffindbaren Stoff.
 */
export type TextbookResponse = S["TextbookResponse"];

export type CreateTextbookDto = S["CreateTextbookDto"];

/**
 * Änderung eines Lehrbuchs. Die `clear…`-Schalter sind nötig, weil `null` im PATCH „nicht angegeben"
 * heißt: ohne sie ließe sich ein einmal gesetztes Fach, eine Klasse, eine Reihe oder eine Unit nie
 * wieder loswerden (der Server würde das `null` überlesen und still den alten Wert behalten).
 */
export type UpdateTextbookDto = S["UpdateTextbookDto"];

// ---- Creator-Profile („Fachlehrer") ----

/**
 * Ein Creator-Profil ist der *Lehrer*, in dessen Namen der KI-Creator entwirft: ein Fach, ein
 * Schulzweig, ein Klassenstufen-Bereich, optional eine Buchreihe. `persona`/`didactics` prägen seine
 * Rolle im Sprachmodell – die festen Inhalts-Regeln des Agenten weichen sie nie auf.
 */
export type CreatorProfileResponse = S["CreatorProfileResponse"];

export type CreateCreatorProfileDto = S["CreateCreatorProfileDto"];

/** Änderung eines Profils; `clear…` leert ein Feld (ein `null` gilt als „nicht angegeben"). */
export type UpdateCreatorProfileDto = S["UpdateCreatorProfileDto"];

/**
 * Ein Treffer der Profil-Suche zu einem Kind. `score` entsteht deterministisch (Reihe wiegt am
 * schwersten), `reasons` sind **stabile Codes** – die Formulierung macht die Oberfläche
 * (siehe `matchReasonLabel`).
 */
export type CreatorProfileMatch = S["CreatorProfileMatch"];

// ---- Bilder & Interessen (Individualisierung der Lerninhalte) ----

/**
 * Eignung eines Bildes. Aufsteigend geordnet – die Auswahl liefert einem Kind nie ein Asset über
 * seiner Freigabe.
 */
export type ContentRating = S["ContentRating"];

/** Semantischer Auslieferungs-Slot einer Auflösung (der Client fragt nach Zweck, nicht nach Pixeln). */
export type MediaPurpose = S["MediaPurpose"];

export type MediaKind = S["MediaKind"];
export type MediaOrigin = S["MediaOrigin"];

/**
 * Facette eines Interessen-Schlagworts. `Style` (Comic/Foto/Pixel-Art) liegt bewusst im selben
 * Vokabular wie die Themen – bei der Bildauswahl verhält es sich gleich, nur schwächer gewichtet.
 */
export type InterestFacet = S["InterestFacet"];

/** Ein Schlagwort der geteilten Taxonomie – Bilder *und* Kinder referenzieren dieselben Einträge. */
export type InterestTagResponse = S["InterestTagResponse"];

export type CreateInterestTagDto = S["CreateInterestTagDto"];

/** Eine Lücke im Trägertext: `index` zeigt auf den Platzhalter `{{index}}` im Text. */
export type Gap = S["Gap"];

/** Ein Trägertext des Lückentext-Stores – Lerngrundlage, aus der Übungen schöpfen. */
export type ClozeResponse = S["ClozeResponse"];

/** Anlegen eines Trägertexts; `key` muss eindeutig sein, mindestens eine Lücke ist Pflicht. */
export type CreateClozeDto = S["CreateClozeDto"];

/**
 * Änderung eines Trägertexts; der `key` bleibt (stabile Referenz). `null` heißt „unverändert" –
 * geleert werden Übersetzung und Wortpool darum über die beiden `clear`-Schalter.
 */
export type UpdateClozeDto = S["UpdateClozeDto"];

/**
 * Änderung eines Schlagworts. Der `slug` fehlt **absichtlich**: er ist die stabile Referenz, an der Bilder
 * und Kind-Profile hängen – ihn zu ändern hieße, beide Seiten der geteilten Taxonomie zu entkoppeln.
 * `synonyms` ersetzt die Liste vollständig.
 */
export type UpdateInterestTagDto = S["UpdateInterestTagDto"];

/** Ein gewichtetes Interesse des Kindes; negatives Gewicht = Abneigung (schließt Bilder hart aus). */
export type ChildInterestResponse = S["ChildInterestResponse"];

/** Eingabe beim Setzen: entweder eine bestehende `tagId` oder ein Label (wird bei Bedarf angelegt). */
export type ChildInterestInput = S["ChildInterestInput"];

/** Eine technische Ausprägung eines Bildes (dieselbe Darstellung, andere Auflösung/Format). */
export type MediaVariantResponse = S["MediaVariantResponse"];

/** Eine *Darstellung* eines Motivs („laufendes Einhorn im Comic-Stil") samt Auflösungen und Tags. */
export type MediaAssetResponse = S["MediaAssetResponse"];

export type CreateMediaVariantDto = S["CreateMediaVariantDto"];

export type CreateMediaAssetDto = S["CreateMediaAssetDto"];

/** Eine Bild-Zuordnung an einem Träger (Vokabel/Item/Übung) samt des zugeordneten Assets. */
export type MediaLinkResponse = S["MediaLinkResponse"];

/** Wo ein Asset zugeordnet ist (Rückrichtung; `carrier` = vocabulary | item | exercise). */
export type MediaUsage = S["MediaUsage"];

/** Das nach „anderes Bild" gültige Bild. */
export type SelectedMediaResponse = S["SelectedMediaResponse"];

export type CreateChildDto = S["CreateChildDto"];

/**
 * RWX-Recht an einer Übung. Hierarchie `Owner` ⊃ `Write` ⊃ `Execute`: Owner darf zusätzlich löschen,
 * die Ausführ-Sichtbarkeit umschalten und selbst Rechte vergeben. **Lesen darf jeder Creator** – das
 * regelt kein Grant.
 */
export type GrantPermission = S["GrantPermission"];

/** Ein vergebenes Recht an einer Übung. */
export type ExerciseGrant = S["GrantResponse"];

/**
 * Verwandtschaft eines Betreuers zum Kind. Sie ist reine Beschreibung – die Rechte sind für alle
 * Betreuer gleich; nur die **Einlösung** ist ausstellergebunden (wer den Artikel angelegt hat, gibt frei).
 */
export type SupervisorRelation = S["SupervisorRelation"];

/** Eine Betreuungs-Beziehung: wer betreut dieses Kind seit wann. */
export type SupervisorLink = S["SupervisorLinkResponse"];

/** Wochentage, wie der Server sie serialisiert (`System.DayOfWeek` als Name). */
export type Weekday = S["DayOfWeek"];

/** Ein Stundenplan-Eintrag: welches Fach an welchem Wochentag (optional mit Uhrzeit). */
/** Anlegen eines Stundenplan-Eintrags. */
export type CreateTimetableEntryDto = S["CreateEntryDto"];

export type TimetableEntry = S["EntryResponse"];

/** Änderung eines Kindes; `clear…` leert ein Feld (ein `null` gilt im PATCH als „nicht angegeben"). */
export type UpdateChildDto = S["UpdateChildDto"];

// ---- Lernstand eines Kindes (plan-übergreifend, nicht an eine Position gebunden) ----

/**
 * Aggregierter Lernstand über eine Menge Vokabel-Items – auf jeder Katalog-Ebene identisch aufgebaut.
 * `weakItems` sind die Kandidaten für gezielte Wiederholung (Beherrschung unter 50 %).
 */
export type MasteryRollup = S["MasteryRollup"];

/** `active` = enthält mindestens eine über einen aktiven Plan zugewiesene Übung (sonst nur Historie). */
export type SubjectProgress = S["SubjectProgressResponse"];

export type ChapterProgress = S["ChapterProgressResponse"];

export type ExerciseProgress = S["ExerciseProgressResponse"];

/** Lernstand des Kindes zu einem einzelnen Übungs-Item. */
export type ItemProgressResponse = S["ItemProgressResponse"];

/** Beherrschung eines Store-Wortes über **alle** Übungen, die es benutzen – die „schlecht gelernten Wörter". */
export type WordMastery = S["WordMasteryResponse"];

/** Ein protokolliertes Antwort-Ereignis (Übung oder Test). */
export type ItemHistoryEntry = S["HistoryResponse"];

// ---- Ziele über dem Lernstand: Objectives (OKR-Klammer über messbaren Etappen) ----

/**
 * Art eines Objectives – sie bestimmt die **Währung** der Belohnung: `Committed` ist verbindlich und zahlt
 * Münzen (real einlösbar), `Stretch` ist ein Dehnungsziel und zahlt Gems (Skins).
 */
export type ObjectiveKind = S["ObjectiveKind"];

/** Metrik einer Etappe; `ClassTestGrade` ist die Note ×10 als Höchstwert (20 = „mindestens 2,0"). */
export type KeyResultMetric = S["KeyResultMetric"];

/** Eine ausgewertete Etappe eines Objectives. */
export type KeyResult = S["KeyResultResponse"];

/** Ein Objective mit Etappen und Roll-up; `rewarded` = die Belohnung ist bereits geflossen. */
export type Objective = S["ObjectiveResponse"];

export type CreateKeyResultRequest = S["CreateKeyResultRequest"];

export type CreateObjectiveRequest = S["CreateObjectiveRequest"];

export type UpdateObjectiveRequest = S["UpdateObjectiveRequest"];

/** Teil-Update einer Etappe; der Scope bleibt fix. */
export type UpdateKeyResultRequest = S["UpdateKeyResultRequest"];

// ---- Katalog: Fächer, Kapitel, Übungssuche ----

export type SubjectResponse = S["SubjectResponse"];

export type ChapterResponse = S["ChapterResponse"];

/** Partielle Änderung eines Kapitels (Name/Reihenfolge). */
export type UpdateChapterDto = S["UpdateChapterDto"];

/** Vollständige Sicht einer Übung inkl. roher Config + Metadaten (zum Anzeigen/Bearbeiten). */
export type ExerciseDetail = S["ExerciseDetail"];

/**
 * Bonus-Vorschlag einer Übung. Er gehört an die Übung, weil er inhaltsabhängig ist (kurze Vokabeln
 * vertragen straffere Zeitfenster als lange Sätze); die Position erbt ihn und darf übersteuern.
 */
export type SuggestedBonus = S["SuggestedBonus"];

/** Wo eine Übung verwendet wird (nur eigene Kinder). */
export type PlanUsage = S["PlanUsage"];
export type ClassTestUsage = S["ClassTestUsage"];
/**
 * Wo eine Übung verwendet wird. `plans`/`classTests` nennen nur die **eigenen** Kinder;
 * `otherLearnersCount` ist die Zahl der **Kinder** (nicht Stellen) fremder Betreuer, die sie einsetzen.
 *
 * Für einen Creator ohne eigene Kinder – einen Lehrer oder eine KI-Creator-App – sind die beiden Listen
 * dauerhaft leer, und diese Zahl ist die einzige Antwort auf „wird mein Material benutzt?".
 */
export type ExerciseUsage = S["UsageResponse"];

// ---- Testmodus („Ausprobieren"): Vater spielt eine Übung nebenwirkungsfrei durch ----

/** Eine im Testmodus vorgelegte Aufgabe. `reveal` ist nur bei Selbsteinschätzung gesetzt (Lösung aufgedeckt). */
export type ExercisePreviewItem = S["PreviewItem"];
/** Eine im Testmodus umschaltbare Abfrageform (Stufenwert + Anzeigename). */
export type ExercisePreviewStage = S["StageOption"];
/**
 * Spielbarer Zustand einer Übung im Testmodus. `typed` = Antwort wird getippt (sonst Selbsteinschätzung);
 * `stages` = die für diesen Übungstyp durchprobierbaren Abfrageformen (leer, wenn nur eine sinnvoll ist).
 */
export type ExercisePreviewData = S["PreviewData"];
/** Eine Antwort im Testmodus: getippt (`givenAnswer`) oder Selbsteinschätzung (`wasKnown`). */
export type ExercisePreviewAnswer = S["PreviewAnswer"];
/** Einzelauswertung im Testmodus (die Lösung `expected` wird hier immer offengelegt). */
export type ExercisePreviewOutcome = S["PreviewItemOutcome"];
/** Gesamtergebnis eines Testmodus-Durchlaufs. */
export type ExercisePreviewResult = S["PreviewResult"];

/** Partielle Vokabel-Änderung (nur gesetzte Felder). */
export type UpdateVocabularyDto = S["UpdateVocabularyDto"];

/** Schlanke Trefferzeile der Übungssuche (Metadaten-Filter über den Katalog). */
export type ExerciseSummary = S["ExerciseSummary"];

/**
 * Ein **Lehrer-Konto**: ein Erwachsener, der Inhalte erstellt und kein Kind betreut. `roles` enthält darum
 * nur `"Creator"` – genau das unterscheidet es von einem Vater-Konto. `creatorId` ist der Login-Name.
 */
export type TeacherAccount = S["TeacherAccountResponse"];

/** Anlegen eines Lehrer-Kontos (Creator ohne Betreuungsauftrag). */
export type CreateTeacherDto = S["CreateTeacherDto"];

/** Die eigene Identität (`GET auth/me`) – Konto, alle Rollen, fachliche Ids. */
export type MeResponse = S["MeResponse"];

/**
 * Selbstverwaltung des eigenen Kontos. `null`/weggelassen heißt „unverändert"; die E-Mail ist das einzige
 * löschbare Feld und braucht dafür `clearEmail` – ein leeres Textfeld allein löscht nichts.
 */
export type UpdateMyAccountDto = S["UpdateMyAccountDto"];

/** Der Freigabe-Stand einer Übung nach dem Umschalten (Antwort von `setExerciseSharing`). */
export type ExerciseSharing = S["ExerciseSharingResponse"];

/** Fachabhängige Übungs-Art ("Kategorie") zur Vorfilterung. */
export type CategoryResponse = S["CategoryResponse"];

export type VocabularyResponse = S["VocabularyResponse"];

/** Globaler, kindneutraler Vokabel-Tag (learn/vocabulary/tags). */
export type VocabTagResponse = S["VocabTagResponse"];

/**
 * Kind-skopierter Tag (api/v1/creator/tags) – markiert Übungen UND Vokabeln als für ein Kind relevant.
 * Nicht verwechseln mit dem globalen VocabTagResponse.
 */
export type ChildTagResponse = S["TagResponse"];

/** Anlegen eines kind-skopierten Tags. */
export type CreateChildTagDto = S["CreateTagDto"];

/** Ergebnis eines einzelnen Batch-Elements (POST /learn/vocabulary/batch). */
export type VocabBatchResult = S["BatchItemResult"];

export type CreateVocabularyDto = S["CreateVocabularyDto"];

/** Antwort der Dublettenprüfung (POST creator/vocabulary/lookup): je gesuchtem Wort die Treffer. */
export type VocabLookupResponse = S["LookupResponse"];

// ---- Katalog: Übungen anlegen (Authoring) ----

/**
 * Selbstbeschreibung eines Übungstyps vom Server. Das Frontend liest sie einmal und verdrahtet daraus
 * Routen und Beschriftungen; die Editor-/Render-Komponente bleibt handgebaut je Typ (aus JSON allein
 * lässt sich kein Formular erzeugen, das die Fachlichkeit trifft).
 */
export type ExerciseTypeManifest = S["ExerciseTypeManifest"];

/** Rechenarten des Rechen-Drills (der Server wählt je Aufgabe zufällig eine der erlaubten). */
export type ArithmeticOperation = S["ArithmeticOperation"];

// ---- Vokabel-Items einer Übung (eigene Ebene, stabil identifiziert) ----

/** Ein positioniertes Vokabelpaar einer Übung; Front/Back kommen live aus dem Store. */
export type VocabItemResponse = S["VocabItemResponse"];

/** Anlegen/Ändern eines Items: per Store-Id **oder** inline (`front`/`back` werden dann angelegt/gefunden). */
export type VocabItemInput = S["VocabItemInput"];

// ---- Lehrpläne ----

/**
 * Lehrplan = reiner Container aus referenzierten Katalog-Übungen (Positionen). Ziele, Punkte, Stufen und
 * Leitner-Einstellungen leben an der jeweiligen {@link PositionResponse}, nicht mehr am Plan.
 */
export type PlanResponse = S["PlanResponse"];

export type CreatePlanDto = S["CreatePlanDto"];

// ---- Lehrplan-Positionen (neues, verfahrens-gemischtes Modell) ----

/** Ziel-Rhythmus einer Position: kein Pflichtziel / Tagesziel / Wochenziel. */
export type GoalCadence = S["GoalCadence"];
/** Umfang der Inhaltsauswahl einer Position aus dem Übungs-Pool. */
export type ItemScope = S["ItemScope"];
/** Server-Reihenfolge, in der fällige Inhalte ausgespielt werden. */
export type PracticeOrder = S["PracticeOrder"];

/** Stufen-Fahrplan-Eintrag (Tag → Stufe) einer Leitner-Position. */
export type StageStep = S["StageStep"];

/**
 * Ein Zeitfenster mit Punkte-Faktor. Dieselbe Form trägt der Server global (Konfiguration) und je Position –
 * beide werden zusammen betrachtet, das engste Fenster gewinnt.
 */
export type ScoringTimeSlot = S["ScoringTimeSlot"];

/** Eine Position eines Lehrplans: Verweis auf eine Katalog-Übung + eigene Ziele/Punkte/Leitner. */
export type PositionResponse = S["PositionResponse"];

/** Tagesstand eines Kindes im Vater-Dashboard (aggregiert über seine aktiven Lehrpläne). */
export type ChildDay = S["ChildDay"];

/** Kindübergreifender Tagesüberblick des Vaters („wer hat heute was geschafft?"). */
export type ChildrenDashboard = S["Dashboard"];

/** Eine Report-Zeile: wie gut ein einzelner Inhalt der Position „sitzt". */
export type ItemReport = S["ItemReport"];

/** Lern-Report einer Position: „welche Vokabel sitzt/sitzt nicht" (Box/Beherrschung + Test-Trefferquote). */
export type PositionReport = S["Report"];

/** Anlegen einer Position. Leere Felder erben den Vorschlag der Übung (Hybrid-Prinzip). */
export type CreatePositionDto = S["CreatePositionDto"];

/** Partielle Änderung einer Position (nur gesetzte Felder). */
export type UpdatePositionDto = S["UpdatePositionDto"];

// ---- Tagesmission / Fortschritt (über Positionen) ----

/** Prüf-/Spieloberfläche eines Übungstyps (aus dem Typ-Manifest). */
export type ExerciseCheckMode = S["ExerciseCheckMode"];

/** Status einer Position für einen Tag – steuert, welche Aktion der Sohn-Client anbietet. */
export type PositionStatus = S["PositionStatus"];

/** Tages-Rollup eines Lehrplans über seine Positionen. */
export type DayOverview = S["DayOverview"];

/** Tagesmission des Sohns bzw. Ein-Blick-Status eines Plans. */
export type OverviewResponse = S["OverviewResponse"];

/** Ein Tag im Verlauf (Vater-Auswertung). */
export type ProgressDay = S["ProgressDay"];

export type ProgressResponse = S["ProgressResponse"];

// ---- Positions-Üben (Leitner) ----

export type PlayMode = S["PlayMode"];

export type PositionSession = S["SessionResponse"];

/**
 * Eine Übungskarte einer Position. `reveal` ist bei Anzeige-/Selbsteinschätzungs-Stufen die aufgedeckte
 * Lösung (Flip-Karte); bei getippten Stufen ist es `null` (Eingabefeld). `answerLength` nur bei
 * Vokabel-Buchstabenkästchen, `hint` nur bei getippten Stufen.
 */
export type PracticeCard = S["PracticeCard"];

/** Antwort zu einer Übungskarte: getippt (`givenAnswer`) oder Selbsteinschätzung (`wasKnown`). */
export type ReviewInput = S["ReviewDto"];

export type ReviewOutcome = S["ReviewOutcome"];

// ---- Missionen & Auszeichnungen (Gamification) ----

export type ProgressMetric = S["ProgressMetric"];
export type MissionPeriod = S["MissionPeriod"];

export type MissionStatus = S["MissionStatus"];

export type AchievementStatus = S["AchievementStatus"];

// ---- Vater: Missionen & Auszeichnungen verwalten (Definitionen) ----

/** Missions-Definition zur Verwaltung durch den Vater. */
export type MissionDef = S["MissionDto"];

/** Änderung einer Mission – **nicht** `Partial<MissionDef>`: die Antwort trägt Felder, die kein PATCH nimmt. */
export type UpdateMissionDto = S["UpdateMissionDto"];
export type CreateMissionDto = S["CreateMissionDto"];

/** Auszeichnungs-Definition zur Verwaltung durch den Vater. */
export type AchievementDef = S["AchievementDto"];

/** Änderung einer Auszeichnung; dieselbe Trennung wie bei `UpdateMissionDto`. */
export type UpdateAchievementDto = S["UpdateAchievementDto"];
export type CreateAchievementDto = S["CreateAchievementDto"];

// Hinweis: Das frühere Angebots-System (Reward/Redemption/OfferPeriod) wurde entfernt – der
// Familien-Shop (siehe unten) ist der einzige Münz-Ausgabeweg.

// ---- Familien-Shop (einziger Münz-Ausgabeweg) ----
// Enums werden serverseitig als Strings serialisiert (JsonStringEnumConverter).

/** Maßeinheit eines Shop-Artikels (z. B. Minuten Fernsehen, Gramm Süßigkeiten). */
export type UnitType = S["UnitType"];
/** Art der Belohnung, die ein Artikel repräsentiert (kategorisiert + bebildert). */
export type ActionType = S["ActionType"];
/** Automatische Auffüll-Regel eines Angebots. */
export type ShopRefillKind = S["ShopRefillKind"];
export type ShopPurchaseStatus = S["ShopPurchaseStatus"];
export type ActivationRequestStatus = S["ActivationRequestStatus"];
/** Wochentag (C# DayOfWeek, string-serialisiert) – nur für wöchentliches Auffüllen relevant. */
export type DayOfWeek = S["DayOfWeek"];

/** Katalog-Artikel des Vaters: die *Art* der Belohnung (Preis/Bestand liegen an den Angeboten). */
export type ShopArticle = S["ShopArticleDto"];
export type CreateShopArticleDto = S["CreateShopArticleDto"];
export type UpdateShopArticleDto = S["UpdateShopArticleDto"];

/** Ein konkretes Angebot zu einem Artikel: Preis (Coin/Gem), Menge je Kauf und Bestand. */
export type ShopListing = S["ShopListingDto"];
export type CreateShopListingDto = S["CreateShopListingDto"];
export type UpdateShopListingDto = S["UpdateShopListingDto"];

/**
 * Aggregierter Inventar-Eintrag eines Kindes: Artikel-Typ → verfügbare Gesamtmenge.
 * `shopArticleId` ist `null`, wenn der Artikel nach dem Kauf gelöscht wurde – bezahlte Einheiten
 * überleben Katalogpflege, Titel und Einheit kommen dann aus der Momentaufnahme am Inventar. Ein
 * solcher Posten ist nicht einlösbar (die Aktivierung adressiert die Artikel-Id).
 */
export type InventoryItem = S["InventoryItemDto"];

/**
 * Dasselbe von der Sohn-Seite (`student/me/shop/inventory`). Strukturgleich mit `InventoryItem`, aber ein
 * eigenes Schema – ein Alias für beide hätte eine Drift auf **einer** der zwei Seiten verschwiegen.
 */
export type MyInventoryItem = S["MyInventoryItemResponse"];

/** Kaufbuchung eines Kindes (Vater-Sicht, mit Stornier-Affordance). */
export type ShopPurchase = S["ShopPurchaseDto"];

/** Aktivierungsanfrage eines Kindes (Vater-Sicht, mit Genehmigen/Ablehnen-Affordance). */
export type ActivationRequest = S["ActivationRequestDto"];

// ---- Familien-Shop: Sohn-Sicht ----

/** Ein kaufbares Angebot aus Sohn-Sicht (`affordable` = reicht das aktuelle Guthaben?). */
export type ShopAvailableListing = S["ShopListingResponse"];
/** Eigene Kaufbuchung aus Sohn-Sicht (Kassenbuch). */
export type MyShopPurchase = S["MyShopPurchaseResponse"];
/** Eigene Aktivierungsanfrage aus Sohn-Sicht. */
export type MyActivation = S["MyActivationResponse"];
/** Gebündelte Shop-Sicht des Sohns (Salden + kaufbare Angebote + Inventar + Kaufhistorie). */
export type ShopView = S["ShopViewResponse"];

// ---- Vater: Klassenarbeiten ----

export type KlassenarbeitStatus = S["KlassenarbeitStatus"];

export type TagRef = S["TagRef"];

/** Kurzform einer Übung aus dem Katalog (für Zuweisung/Üben). */
export type ExerciseBrief = S["ExerciseBrief"];

export type KlassenarbeitResponse = S["KlassenarbeitResponse"];

export type KlassenarbeitDetail = S["KlassenarbeitDetail"];

export type CreateKlassenarbeitDto = S["CreateClassTestDto"];

export type UpdateKlassenarbeitDto = S["UpdateClassTestDto"];

export type KlassenarbeitPractice = S["PracticeResponse"];

export type KlassenarbeitRepeat = S["RepeatResponse"];

/** Partielle Lehrplan-Änderung durch den Vater (Datumsfelder als "YYYY-MM-DD"). */
export type UpdatePlanDto = S["UpdatePlanDto"];

// ---- Positions-Test (Abschlusstest einer Übung) ----

/**
 * Eine im Positions-Test vorgelegte Aufgabe. `reveal` = aufgedeckte Lösung bei Anzeige-/Selbsteinschätzung,
 * `null` bei getippten Stufen; `answerLength` nur bei Vokabel-Buchstabenkästchen, `hint` nur getippt.
 */
export type TestItem = S["TestItem"];

/**
 * Antwort des Test-Starts. Der Klausur-Modus ist strikt server-getrieben: es kommen KEINE Aufgaben im Bulk,
 * nur die Metadaten. Die Fragen holt der Client einzeln über `nextTest` (kein Zurück).
 */
export type TestAttemptResponse = S["AttemptResponse"];

/** Die nächste Prüfungsfrage (oder `done`), server-geführt über den Attempt-Cursor. */
export type TestNextResponse = S["TestNextResponse"];

/** Bestätigung einer abgegebenen Prüfungsantwort – bewusst OHNE Korrektheit (Feedback erst beim Abschluss). */
export type TestAnswerAck = S["AnswerAck"];

export type AnswerDto = S["AnswerDto"];

export type ItemOutcome = S["ItemOutcome"];

export type TestSubmitResponse = S["SubmitResponse"];

// ---- Sohn-Wallet ----

export type PointKind = S["PointKind"];

/** Die beiden Währungen der App (Münzen fürs echte Leben, Gems für Kosmetik). */
export type Currency = S["Currency"];

export type WalletEntry = S["MyPointsEntryResponse"];

/** Dieselbe Zeile aus Vater-Sicht: sie nennt zusätzlich das Kind (`grantPoints`). */
export type ChildPointsEntry = S["PointsEntryResponse"];

/** Reiner Kontostand des Sohns (GET me/points). Die Buchungen liegen unter me/points/entries. */
export type WalletBalance = S["WalletResponse"];

/** Kombinierte Konto-Sicht des Vaters (GET children/{id}/points): Salden + eingebettete Buchungen. */
export type Wallet = S["ChildPointsResponse"];

// ---- Sohn-Skins (server-autoritativer Besitz) ----

/** Skin-Zustand des Kindes vom Server: Gem-Stand, ausgerüsteter und freigeschaltete Skins. */
export type SkinState = S["SkinStateResponse"];

// ---- Anmerkungen beim Testen (api/v1/remarks) ----

/** Einordnung einer Anmerkung. `Unspecified` ist der Regelfall – der Nachbereitungs-Skill zieht sie nach. */
export type RemarkCategory = S["RemarkCategory"];

/** Bearbeitungsstand einer Anmerkung. */
export type RemarkStatus = S["RemarkStatus"];

/** Der Kontext-Schnappschuss, den das Widget automatisch mitschickt. */
export type RemarkContext = S["RemarkContextDto"];

/** Eine Anmerkung, wie der Server sie ausliefert. */
export type Remark = S["RemarkDto"];

/** Herkunft eines Beitrags im Verlauf: der Mensch oder Claude. */
export type RemarkCommentAuthor = S["RemarkCommentAuthor"];

/**
 * Ein Beitrag im Verlauf einer Anmerkung. Ergänzt `answer` (die eine belegte Auflösung), ersetzt sie nicht:
 * Analyse, Rückfrage und Umsetzungsnotiz stehen nebeneinander.
 */
export type RemarkComment = S["RemarkCommentDto"];

/**
 * Einen Beitrag hinzufügen. **Nebenwirkung mit Absicht:** Ein `Human`-Beitrag zu einer erledigten oder
 * verworfenen Anmerkung holt sie zurück auf `Open` – so legt der Nachbereitungs-Skill sie wieder vor.
 */
export type CreateRemarkCommentDto = S["CreateRemarkCommentDto"];

/** Was beim Erfassen zum Server geht. Pflicht ist allein der Text. */
export type CreateRemarkDto = S["CreateRemarkDto"];

/** Änderungen an einer Anmerkung. `null`/weglassen = „nicht angegeben"; geleert wird über `clear…`. */
export type UpdateRemarkDto = S["UpdateRemarkDto"];
