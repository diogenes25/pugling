import type {
  AchievementDef, UpdateAchievementDto, AchievementStatus, AnswerDto, CategoryResponse, ChildResponse, CreateAchievementDto,
  CreateChildDto, CreateExercisePayload, CreateKlassenarbeitDto, CreateMissionDto, CreatePlanDto, CreateVocabularyDto,
  UpdateChildDto, SupervisorLink, SupervisorRelation, TimetableEntry, CreateTimetableEntryDto,
  ClozeResponse, CreateClozeDto, UpdateClozeDto,
  ExerciseGrant, ExerciseRating, GrantPermission,
  CreateFeedbackDto, ExerciseFeedbackSummary, FeedbackEntry, FeedbackNotice, FeedbackReceipt,
  FeedbackStatus,
  CreateAdultDto, AdultResponse, UpdateAdultDto,
  ExerciseDetail, ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult, ExerciseTypeManifest,
  CreateChildTagDto, ExerciseWriteResult,
  ExerciseSearchParams, ExerciseSharing, ExerciseSummary, MeResponse, TeacherAccount, CreateTeacherDto, UpdateMyAccountDto, ExerciseUsage, KlassenarbeitDetail, KlassenarbeitPractice, KlassenarbeitRepeat,
  KlassenarbeitResponse, KlassenarbeitStatus, LoginResponse, MissionDef, UpdateMissionDto, MissionStatus, PlanResponse,
  ChildrenDashboard, ChildrenOverview, OverviewGranularity, CreatePositionDto, CreateReviewFromWordsDto, PositionResponse, PositionReport, UpdatePositionDto, OverviewResponse, PositionSession, PracticeCard,
  ProgressResponse, ReviewInput, ReviewOutcome, TodayResponse, UpcomingClassTest, ClassTestReadiness,
  SkinState, SubjectResponse,
  TestAttemptResponse, TestNextResponse, TestAnswerAck, TestSubmitResponse,
  ExtraTrainingOverview, TowerGameOverview, ExtraRunResponse, ExtraAnswer, ExtraRunResult, MixedReviewOverview, MixedReviewRunResponse, MixedReviewAnswer, MixedReviewResult, DailyBoxOpened, UpdateKlassenarbeitDto, UpdatePlanDto, UpdateVocabularyDto,
  VocabBatchResult, VocabularyResponse, VocabTagResponse, ChildTagResponse, Wallet, WalletBalance, WeekCoins, WalletEntry, ChildPointsEntry, Currency,
  Paged, VocabularySearchParams, VocabItemInput, VocabItemResponse, VocabLookupResponse,
  SeriesUnitProgress, ExerciseProgress, ItemHistoryEntry, ItemProgressResponse, SubjectProgress, WordMastery,
  SubjectLevel,
  CreateKeyResultRequest, CreateObjectiveRequest, GoalStatus, KeyResult,
  Objective, ObjectiveKind, UpdateKeyResultRequest, UpdateObjectiveRequest,
  ShopArticle, CreateShopArticleDto, UpdateShopArticleDto, ShopListing, CreateShopListingDto, UpdateShopListingDto,
  InventoryItem, MyInventoryItem, ShopPurchase, MyShopPurchase, ActivationRequest, ShopPurchaseStatus, ActivationRequestStatus,
  ShopView, MyActivation,
  ContentRating, MediaOrigin, InterestTagResponse, CreateInterestTagDto, UpdateInterestTagDto,
  GrammarTopicResponse, CreateGrammarTopicDto, UpdateGrammarTopicDto,
  ChildInterestResponse, ChildInterestInput,
  InterestTagOption, MyInterestProfile, MyInterestInput,
  MediaAssetResponse, CreateMediaAssetDto, MediaLinkResponse, MediaUsage, SelectedMediaResponse,
  PublisherResponse, CreatePublisherDto, UpdatePublisherDto,
  TextbookSeriesResponse, CreateTextbookSeriesDto, UpdateTextbookSeriesDto,
  SeriesUnitResponse, CreateSeriesUnitDto, UpdateSeriesUnitDto, SchoolType,
  CreatorProfileResponse, CreateCreatorProfileDto, UpdateCreatorProfileDto, CreatorProfileMatch,
  TextbookResponse, CreateTextbookDto, UpdateTextbookDto,
  Remark, CreateRemarkDto, UpdateRemarkDto, RemarkCategory, RemarkStatus,
  RemarkComment, CreateRemarkCommentDto, SortDir,
} from "./types";
import { recordHttpError, recordNetworkError } from "./remarks";

const TOKEN_KEY = "pugling.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

/**
 * Wer benachrichtigt wird, wenn der Server eine Anfrage mit 401 abweist.
 *
 * Ein abgelaufenes Token merkt man sonst erst daran, dass **jedes** Panel „Unauthorized" anzeigt – der
 * Nutzer sieht eine kaputte Seite statt eines Logins. Der `AuthProvider` hängt sich hier ein und beendet
 * die Sitzung; danach greift der Rollen-Guard und zeigt die Anmeldung.
 */
let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler;
}

/**
 * Fehler mit HTTP-Status, damit die UI 401 (Session weg) von 4xx (Eingabe) trennen kann.
 * `traceId` (aus dem RFC-7807-Body) korreliert die Meldung mit den Server-Logs – im Supportfall
 * kann der Nutzer diese Referenz nennen.
 */
export class ApiError extends Error {
  constructor(public status: number, message: string, public traceId?: string, public code?: string) {
    super(message);
    this.name = "ApiError";
  }
}

/** Menschlich lesbare Fehlermeldung inkl. Trace-Referenz, wo vorhanden. */
/**
 * Deutsche Texte zu **fachlichen** Fehler-Codes.
 *
 * Die `detail`-Texte des Servers sind bewusst englisch (i18n-Vertrag, siehe CLAUDE.md) – im UI liest sich
 * eine englische Zeile aber wie ein Defekt, gerade dort, wo der Nutzer selbst etwas richten kann. Der
 * `code` ist stabiler Vertragsbestandteil, die Formulierung gehört der Oberfläche. Hier stehen die Codes, die einen
 * **Erwachsenen** an einem eigenen Knopf treffen (die deutsche Arcade fällt für Codes ohne `problem.*` ebenfalls hierauf
 * zurück); technische Fälle behalten ihre Rohmeldung. Ein Satz nennt, was sperrt –
 * keinen Ausweg über eine zweite Sperre (Regel 2026-11-07, `pm-loop` Step 5). Dass jeder 409-Code hier steht oder mit
 * Grund ausgenommen ist, hält `ProblemTextGuardTests` (B-411).
 */
const GERMAN_PROBLEM_TEXT: Record<string, string> = {
  exercise_empty:
    "Diese Übung hat noch keine Inhalte.",
  no_checkable_content: "Diese Übung hat keine einzeln prüfbaren Aufgaben.",
  // Bonus-Training: die Sperre nennt, was zuerst kommt – sonst wäre sie ein Schloss ohne Weg hinaus.
  // Ohne Zielart in der Antwort neutral: bei reinen Wochenzielen hängt die Sperre an ihnen (B-612).
  extra_training_locked: "Das Bonus-Training öffnet, sobald deine Ziele erfüllt sind.",
  extra_exercise_done: "Diese Bonus-Übung bringt heute nichts mehr.",
  // Hausaufgaben (B-760): die Sperre nennt, was gebucht ist bzw. worauf sie wartet.
  homework_accepted: "Diese Hausaufgabe ist schon angenommen, die Münzen sind gebucht.",
  homework_not_submitted: "Zu dieser Hausaufgabe liegt keine neue Abgabe vor – annehmen geht erst, wenn das Kind (wieder) abgegeben hat.",
  // B-584: dieselbe Wortmenge zweimal wäre dieselbe Pflicht doppelt – auch, wenn es die Übung ist, aus der die Wörter stammen.
  review_already_in_plan: "Genau diese Wörter übt dieser Plan schon in einer Position.",
  review_subject_mismatch: "Diese Wörter gehören zu einem anderen Fach als dieser Plan.",
  // B-635: nennt nur die Sperre – leichter geht nur die aktuelle Nacharbeits-Karte, und nur bis zur Auswahl.
  cannot_ease: "Diese Karte lässt sich nicht noch leichter schalten.",
  // B-552: nennt nur die Sperre – welche Stufe getippt prüft, steht in der Feldhilfe.
  typed_test_unreachable: "„Nur getippte Tests zählen“ braucht eine getippte Stufe – auf Selbsteinschätzung wäre die Pflicht nie erreichbar.",
  extra_run_completed: "Diese Bonus-Runde ist schon abgegeben.",
  extra_run_stale: "Diese Übung wurde geändert, seit du angefangen hast – fang sie im Bonus-Training neu an.",
  extra_cap_reached: "Dein Bonus-Deckel ist voll – diese Übung würde dir jetzt nur die Lösungen zeigen, ohne Münzen. Darum geht sie gerade nicht.",
  daily_box_none: "Gerade wartet keine Truhe auf dich.",
  mixed_review_empty: "Gerade ist keine Karte fällig, die in eine Mischrunde darf.",
  mixed_review_completed: "Diese Mischrunde ist schon abgegeben.",
  mixed_review_stale: "Diese Mischrunde ist von gestern – starte eine neue.",
  no_tag_matches: "Zu diesen Tags gibt es keine Vokabeln. Die Übung wurde nicht verändert.",
  // Seit B-123 ist das der wahrscheinlichste Fehlschlag im Reihen-Formular: vorher war der 409 über die
  // Oberfläche gar nicht erreichbar, weil Anlegen idempotent ist. Und die Feld-Erklärung bewirbt die
  // Regel ausdrücklich – sie darf dann nicht auf Englisch durchschlagen.
  duplicate_textbook_series: "Eine andere Reihe trägt diesen Namen schon. Wähle einen anderen.",
  // Seit B-141 auch für ein Label, das ein umbenanntes Schlagwort trägt – der Vater trifft es beim Umbenennen in der
  // Schlagwort-Verwaltung. Groß-/Kleinschreibung zählt mit, darum kein „ähnlicher Name“.
  duplicate_interest_tag: "Ein anderes Schlagwort trägt diesen Namen schon. Wähle einen anderen.",
  // Der Deckel aus B-37 trifft ausschließlich das Kind in der Arcade – der einzige Ort, an dem eine rohe
  // englische Zeile einen Elfjährigen erreicht. Der Satz sagt darum auch, wann es weitergeht.
  test_attempts_exhausted: "Für heute sind deine Versuche aufgebraucht. Morgen kannst du es noch mal probieren.",
  // Ebenfalls nur fürs Kind (B-330): sein Plan ist pausiert, abgelaufen oder nicht der aktive – nach einem Reload auf
  // Üben/Klausur steht es sonst vor der englischen Server-Zeile. Der Satz nennt keinen der drei Gründe, weil das Kind
  // keinen davon selbst ändern kann.
  plan_inactive: "Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.",
  // Die beiden Löschsperren aus B-144/B-127. Sie gehören hierher, weil sie beide Hälften der Regel oben
  // erfüllen: der Creator sieht sie an einem Knopf, den er selbst gedrückt hat, und er kann etwas dagegen
  // tun. Ohne Eintrag stünde dort die englische Rohzeile – an genau der Stelle, an der er eine Anleitung
  // braucht. Gefunden hat das der Rollengang, nicht die Tests; seither hält es `errorMessage.test.ts`.
  subject_in_use:
    "Dieses Fach wird in den Zielen oder im Stundenplan eines Kindes verwendet. Entferne erst diese "
    + "Einträge – Lehrwerk-Reihen und Übungen hindern das Löschen dagegen nicht.",
  // Der zweite Halbsatz nennt bewusst auch die eigentümerlose Reihe: Auf einer geseedeten Datenbank ist
  // das der EINZIGE Fall, in dem die Sperre greift (die herrenlose „Green Line 1" hängt an „Klett"). Wer
  // dann nur „anderes Konto" liest, sucht eine fremde Reihe, die es nicht gibt – und findet keinen Ausweg.
  publisher_in_use:
    "An diesem Verlag hängt mindestens eine Reihe, die dir nicht gehört – die eines anderen Kontos oder "
    + "eine herrenlose aus dem geteilten Katalog. Löschen würde auch deren Zuordnung entfernen; es geht "
    + "nur, solange ausschließlich eigene Reihen daran hängen.",
  // Die Sperre beim Fachwechsel einer Reihe (B-291, B-401; Text B-403). Fest übersetzt wie `exercise_in_use`: die
  // Anzahlen stehen nur im englischen `detail`, und den Satz zu zerlegen hieße, den Vertrag an eine Formulierung zu binden.
  series_subject_in_use:
    // B-414: der Satz nennt, was sperrt – kein Ausweg über eine zweite Sperre. Das Löschen einer Übung mit Art ist selbst gesperrt
    // (`exercise_in_use`) und hat seine eigene Meldung; ein anderes Fach zu wählen prüft dieselben Zuordnungen (B-405).
    // Als Bedingung, nicht als Tatsache: beim Entfernen zählt JEDE Übung der Reihe, auch eine ohne Thema und Art (B-401).
    "Diese Reihe hängt noch an ihrem bisherigen Fach. Wechseln lässt es sich nicht, solange Units oder Übungen ein Grammatik-Thema "
    + "dieses Fachs tragen oder Übungen eine Art dieses Fachs; ganz entfernen nicht, solange die Reihe Übungen trägt oder "
    + "eine ihrer Units ein Grammatik-Thema dieses Fachs hat.",
  // Die dritte Löschsperre (B-206), und die einzige, die einen Vater trifft, der gerade NICHTS falsch
  // gemacht hat: der KI-Creator kleidet Übungen ein, ohne dass er es sieht, und die Übungszahl in der
  // Tabelle ist nur so frisch wie die letzte Suche. Der Satz muss darum den Ausweg nennen.
  interest_tag_in_use:
    "Dieses Schlagwort kleidet mindestens eine Übung ein – die Einkleidung ist Autoren-Arbeit und nicht "
    + "wiederherstellbar. Nimm es zuerst im Bearbeiten-Dialog dieser Übungen heraus; bei Übungen, die dir "
    + "nicht gehören, kann das nur ihr Autor.",
  // Die Löschsperre an einer Übung (B-205; seit B-413 nur noch dort). Fest übersetzt verliert der Satz, was der Server
  // dynamisch zählt – darum nennt er die Verwendungen, die der Vater nicht sehen kann. Der Verweis auf „Verwendung“ ist wahr,
  // seit sie alle drei eigenen Sperren zeigt (B-409). Kein „Entferne sie dort zuerst“ (B-414): eine gespielte Position lässt
  // sich nicht herausnehmen (`position_has_data`).
  exercise_in_use:
    "Diese Übung wird noch benutzt – in einem Lehrplan, einer Klassenarbeit oder als Etappe eines Ziels, vielleicht auch "
    + "bei einem Kind, das du nicht betreust. Die Stellen bei deinen Kindern zeigt „Verwendung“ an der Übung.",
  // B-413: Unit und Reihe löschen. Ein eigener Code, weil eine Ziel-Etappe auf der Unit die Verwendung KEINER Übung ist –
  // ein Verweis auf „Verwendung“ schickte den Vater an eine Stelle, die sie nie zeigt. Darum hier kein Verweis.
  unit_content_in_use:
    "Inhalte daraus werden noch benutzt – eine Übung in einem Lehrplan, einer Klassenarbeit oder als Etappe eines Ziels, "
    + "oder ein Ziel zeigt mit einer Etappe auf eine Unit selbst; vielleicht auch bei einem Kind, das du nicht betreust.",
  // B-700: eigener Code neben `exercise_in_use`, weil „Verwendung“ keine Vorstufen zeigt. Der Ausweg ist der Bearbeiten-Dialog
  // der Vorstufe (Abschnitt „Aufbau-Weg“); die Ziel-Übung nennt ihre Vorstufen dort selbst. Fremde Vorstufen nimmt nur ihr Autor
  // heraus – das sagt der Satz, statt einen Weg zu versprechen, den der Vater nicht gehen kann.
  exercise_is_build_target:
    "Diese Übung ist Ziel von Vorstufen und lässt sich darum nicht löschen. Nimm die Vorstufen zuerst im Bearbeiten-Dialog "
    + "aus dem Aufbau-Weg; bei Vorstufen, die dir nicht gehören, kann das nur ihr Autor.",
  // B-848: die offene Runde oder der offene Test des Kindes hält die Stufe; einen Ausweg hat der Vater nicht – er wartet.
  build_level_in_play:
    "Das Kind spielt diese Position heute gerade (eine Runde oder ein Test ist offen). Die Stufe lässt sich ändern, "
    + "sobald es damit fertig ist.",
  // Seit B-205 ein eigener Code: nichts wird gelöscht, und Anhängen bleibt erlaubt – das sagte der Löschtext falsch.
  exercise_items_locked:
    "Diese Übung steckt in einem Lehrplan. Wörter entfernen oder umsortieren würde den gespeicherten "
    + "Lernstand verschieben – neue Wörter ans Ende anhängen geht.",
  // B-411: die übrigen Sperren, die ein Erwachsener an einem eigenen Knopf auslöst. Jeder Satz nennt, was sperrt; „Wähle einen
  // anderen“ nur bei Namensdubletten – dort ist der eigene Name die Sperre.
  // Derselbe Code für den Key eines Lückentexts (im Web erreichbar), eines Bilds oder einer Vokabel (nur über die API) und die
  // Artikelnummer im Familien-Shop – die Oberfläche sagt „Key“, nicht „Schlüssel“.
  duplicate_key: "Diese Kennung (Key bzw. Artikelnummer) trägt schon ein anderer Eintrag. Wähle eine andere.",
  duplicate_achievement: "Für dieses Kind gibt es schon eine Auszeichnung mit derselben Ziel-Metrik und Schwelle.",
  duplicate_vocabulary_in_exercise: "Diese Vokabel steht schon in dieser Übung.",
  duplicate_key_result: "Dieses Ziel hat schon eine Etappe mit demselben Bereich und derselben Messlatte.",
  media_variant_exists: "Dieses Bild hat schon eine Variante für diesen Zweck und dieses Format.",
  media_already_linked: "Dieses Bild ist hier schon verknüpft.",
  duplicate_email: "Diese E-Mail-Adresse nutzt schon ein anderes Konto.",
  // Eindeutig nur unter den EIGENEN Profilen, inaktive eingeschlossen – die Liste blendet sie standardmäßig aus.
  duplicate_profile_name: "Eines deiner Fachlehrer-Profile – auch ein inaktives – trägt diesen Namen schon. Wähle einen anderen.",
  duplicate_category_name: "In diesem Fach gibt es diese Art schon.",
  // Der Server vergleicht auch die abgeleitete Kurzform des Namens – darum „oder einen fast gleichen“.
  duplicate_publisher: "Ein anderer Verlag trägt diesen oder einen fast gleichen Namen schon. Wähle einen anderen.",
  grammar_topic_in_use: "Dieses Grammatik-Thema hängt noch an Übungen oder Units.",
  last_owner: "Eine Übung braucht mindestens jemanden, der sie verwaltet – das letzte Recht „Verwalten“ kannst du nicht entziehen.",
  vocabulary_in_use: "Diese Vokabel wird noch benutzt – in Übungen oder als Grundform anderer Einträge.",
  position_has_data: "An dieser Position wurde schon geübt oder getestet. Entfernen würde diesen Lernstand mitnehmen und geht darum nicht mehr.",
  // B-673: der Spielstand hängt am Plan und gehört über ihn seinem Kind. Kein Ausweg genannt: es gibt keinen, der die
  // Sperre aufhebt, ohne dem ersten Kind etwas zu nehmen.
  // Zwei Sperren, ein Code (B-673 Umhängen, B-659 Löschen): der Satz nennt beide, der Ausweg hängt nur am Löschen –
  // fürs Umhängen gibt es keinen (B-673).
  plan_has_data: "Mit diesem Plan wurde schon geübt oder getestet. Der Lernstand gehört dem Kind, das damit gelernt hat, darum lässt sich der Plan nicht löschen (stattdessen „Deaktivieren“) und keinem anderen Kind mehr geben.",
  timetable_slot_taken: "Dieses Fach steht an diesem Wochentag schon im Stundenplan.",
  purchase_not_open: "Dieser Kauf ist schon storniert.",
  activation_not_pending: "Diese Anfrage ist schon beantwortet.",
  // Trifft den Vater beim Stornieren, Genehmigen oder Ablehnen im Familien-Shop, wenn zwei Speichervorgänge sich kreuzen
  // (ShopService). Dieselbe Handlung noch einmal ist kein Weg über eine zweite Sperre.
  concurrency_conflict: "Das hat sich gerade mit einer anderen Änderung überschnitten und wurde nicht gespeichert. Versuch es noch einmal.",
  // B-415: Sperren, die nur das Kind in der Arcade auslöst. Sie stehen trotzdem hier, weil die deutsche Arcade für jeden
  // eigenen Satz (`problem.*`) wortgleich mit dieser Tabelle sein muss – eine Sperre heißt überall gleich.
  skin_already_unlocked: "Diesen Skin hast du schon freigeschaltet.",
  shop_insufficient_stock: "Davon ist gerade nichts mehr da.",
  media_no_alternative: "Mehr Bilder gibt es dafür nicht 🙂",
  media_not_on_card: "Zu dieser Karte gibt es kein Bild.",
  interest_wish_limit_reached: "Deine Liste mit eigenen Interessen ist voll.",
  // B-499: 400er aus Kauf und Einlösen – das 409-Tor aus B-411 sieht sie nicht, die Wortgleichheit der Arcade schon. Drei
  // erreicht nur das Kind; `insufficient_inventory` auch der Vater beim Genehmigen (ShopController), darum ohne „du“ – das
  // Vater-Web setzt dort einen eigenen Satz mit Artikel und Folge (VaterShop `decide`, B-504).
  insufficient_gems: "Dafür reichen deine Gems noch nicht.",
  insufficient_coins: "Dafür reichen deine Münzen noch nicht.",
  shop_listing_inactive: "Dieses Angebot gibt es gerade nicht.",
  insufficient_inventory: "Von diesem Artikel ist nicht mehr genug im Inventar.",
};

export function errorMessage(e: unknown): string {
  if (e instanceof ApiError) {
    // Bei einem fachlichen Code die deutsche Fassung – und **ohne** Trace-Id: die hilft beim Melden eines
    // Defekts, nicht beim Beheben einer leeren Übung, und macht den Satz nur unverständlicher.
    if (e.code && GERMAN_PROBLEM_TEXT[e.code]) return GERMAN_PROBLEM_TEXT[e.code];
    return e.traceId ? `${e.message} (Ref: ${e.traceId})` : e.message;
  }
  return e instanceof Error ? e.message : String(e);
}

// Ein Request inkl. Token + einheitlicher RFC-7807-Fehlerbehandlung; liefert die rohe Response,
// damit sowohl der Body-Parser (`http`) als auch der paginierte Helfer (`httpPaged`, liest zusätzlich
// den `X-Total-Count`-Header) dieselbe Logik teilen.
async function request(url: string, method: string, body?: unknown): Promise<Response> {
  const token = getToken();
  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (token) headers["Authorization"] = `Bearer ${token}`;

  let res: Response;
  try {
    res = await fetch(url, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch (e) {
    // Der Server wurde gar nicht erreicht (offline, DNS, Abbruch) – für eine spätere Bug-Anmerkung
    // ist gerade das die Information. Nur Methode/Pfad/Meldung, nie der Body.
    recordNetworkError(method, url, e instanceof Error ? e.message : String(e));
    throw e;
  }

  return throwIfFailed(res, method, url);
}

/**
 * Multipart-Variante von {@link request} (Datei-Upload). Setzt bewusst **keinen** `Content-Type`:
 * bei `FormData` muss der Browser ihn samt `boundary` selbst bestimmen – ein eigener Header ließe den
 * Server den Body nicht parsen. Fehlerbehandlung ist dieselbe.
 */
async function requestForm(url: string, form: FormData): Promise<Response> {
  const token = getToken();
  const headers: Record<string, string> = {};
  if (token) headers["Authorization"] = `Bearer ${token}`;
  let res: Response;
  try {
    res = await fetch(url, { method: "POST", headers, body: form });
  } catch (e) {
    recordNetworkError("POST", url, e instanceof Error ? e.message : String(e));
    throw e;
  }
  return throwIfFailed(res, "POST", url);
}

/**
 * Die geteilte RFC-7807-Auswertung: aus einer Fehlerantwort einen {@link ApiError} mit Code/TraceId machen.
 *
 * `url` wird durchgereicht statt aus `res.url` gelesen: Das ist bei opaken und synthetischen Responses
 * leer, der Fehler-Ringpuffer protokollierte dann nur „/" statt des Endpunkts.
 */
async function throwIfFailed(res: Response, method: string, url: string): Promise<Response> {
  if (!res.ok) {
    const raw = await res.text().catch(() => "");
    // Die API antwortet einheitlich als application/problem+json (RFC 7807): detail/title als
    // Klartext, traceId zur Korrelation. Bei Nicht-JSON den Rohtext behalten.
    let message = raw || `${res.status} ${res.statusText}`;
    let traceId: string | undefined;
    let code: string | undefined;
    if (res.headers.get("content-type")?.includes("json") && raw) {
      try {
        const problem = JSON.parse(raw) as { detail?: string; title?: string; traceId?: string; code?: string };
        message = problem.detail || problem.title || message;
        traceId = problem.traceId;
        code = problem.code;
      } catch {
        /* kein valides JSON – Rohtext behalten */
      }
    }
    // Nur wenn überhaupt ein Token im Spiel war: ein 401 auf dem Login-Endpunkt bedeutet „PIN falsch",
    // nicht „Sitzung abgelaufen" – dort soll die Meldung im Formular stehen bleiben.
    if (res.status === 401 && getToken()) onUnauthorized?.();
    // Für eine spätere Bug-Anmerkung festhalten – ausschließlich Metadaten (Methode, Pfad ohne Query,
    // Status, Code). Weder `raw` noch `message` gehen in den Puffer: Sie stammen aus dem Antwort-Body.
    recordHttpError(method, url, res.status, code);
    throw new ApiError(res.status, message, traceId, code);
  }
  return res;
}

async function http<T>(url: string, method = "GET", body?: unknown): Promise<T> {
  const res = await request(url, method, body);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** Wie {@link http}, aber mit `FormData` als Body (Datei-Upload). */
async function httpForm<T>(url: string, form: FormData): Promise<T> {
  const res = await requestForm(url, form);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/**
 * Wie {@link http}, aber für Server-paginierte Listen: Das Backend paginiert per `skip`/`take` und
 * schreibt die Gesamtzahl in den `X-Total-Count`-Header (nicht in den Body). Liefert die Seite plus
 * `total`; fehlt der Header, fällt `total` auf die Seitenlänge zurück.
 */
/** Liest die Gesamtzahl aus dem `X-Total-Count`-Header; fehlt/leer, fällt sie auf `fallback` zurück. */
function totalFrom(res: Response, fallback: number): number {
  const header = res.headers.get("X-Total-Count");
  return header != null && header !== "" ? Number(header) : fallback;
}

async function httpPaged<T>(url: string): Promise<Paged<T>> {
  const res = await request(url, "GET");
  const text = await res.text();
  const items = (text ? JSON.parse(text) : []) as T[];
  return { items, total: totalFrom(res, items.length) };
}

/** Hängt Paginierung/Sortierung an eine URLSearchParams an (nur gesetzte Werte). */
function appendPaging(q: URLSearchParams, p: { sort?: string; dir?: string; skip?: number; take?: number }) {
  if (p.sort) q.set("sort", p.sort);
  if (p.dir) q.set("dir", p.dir);
  if (p.skip != null) q.set("skip", String(p.skip));
  if (p.take != null) q.set("take", String(p.take));
}

// Alle Routen liegen unter dem API-Versionssegment (Backend: ApiRoutes.V1 = "api/v{version}").
// Zentral hier gehalten, damit ein künftiger v2-Umzug nur eine Stelle betrifft.
const V1 = "/api/v1";

export const api = {
  // ---- Auth ----
  loginAdult: (adultId: number, pin: string) =>
    http<LoginResponse>(`${V1}/auth/adult`, "POST", { adultId, pin }),
  loginChild: (childId: number, pin: string) =>
    http<LoginResponse>(`${V1}/auth/child`, "POST", { childId, pin }),

  // ---- Vater: eigenes Konto ----
  // Die Registrierung ist der einzige anonyme Schreibpfad der API: ohne sie könnte ein neuer Vater
  // nur per Seed entstehen. Der Server legt dabei zugleich das Login-Konto (Creator+Supervisor) an.
  registerAdult: (dto: CreateAdultDto) =>
    http<AdultResponse>(`${V1}/supervisor/adults`, "POST", dto),
  /**
   * Registriert ein **Lehrer-Konto**: nur die Creator-Rolle, kein Betreuungsauftrag. Eigener Pfad, weil
   * sich nicht der Datensatz unterscheidet, sondern die Rollen des Kontos – und die entstehen beim Anlegen.
   */
  registerTeacher: (dto: CreateTeacherDto) =>
    http<TeacherAccount>(`${V1}/creator/teacher-accounts`, "POST", dto),
  /** Das eigene Lehrer-Konto (nur der Inhaber). */
  teacherAccount: (creatorId: number) =>
    http<TeacherAccount>(`${V1}/creator/teacher-accounts/${creatorId}`),
  /** Die eigene Identität aus dem Token. */
  me: () => http<MeResponse>(`${V1}/auth/me`),
  /**
   * Selbstverwaltung des eigenen Kontos: Name, E-Mail, PIN. Liegt bei `auth/…`, weil das Konto zu keiner
   * Ebene gehört – **derselbe Mensch** bedient es aus jeder Rolle. Der einzige Weg für ein Lehrer-Konto,
   * dem die Vater-Endpunkte verschlossen sind; hält Konto- und fachlichen Namen zusammen.
   */
  updateMyAccount: (dto: UpdateMyAccountDto) =>
    http<MeResponse>(`${V1}/auth/me`, "PATCH", dto),
  adult: (adultId: number) => http<AdultResponse>(`${V1}/supervisor/adults/${adultId}`),
  updateAdult: (adultId: number, dto: UpdateAdultDto) =>
    http<AdultResponse>(`${V1}/supervisor/adults/${adultId}`, "PATCH", dto),

  // ---- Vater: Kinder (der Vater ergibt sich serverseitig aus dem JWT) ----
  children: () => http<ChildResponse[]>(`${V1}/supervisor/children`),
  child: (childId: number) => http<ChildResponse>(`${V1}/supervisor/children/${childId}`),
  deleteChild: (childId: number) => http<void>(`${V1}/supervisor/children/${childId}`, "DELETE"),

  // Ko-Betreuer: ein Kind hat mehrere Supervisor (Vater/Mutter/Oma) mit gleichen Rechten und
  // gemeinsamem Wallet. Der letzte lässt sich nicht entfernen – das Kind wäre verwaist (400).
  childSupervisors: (childId: number) =>
    http<SupervisorLink[]>(`${V1}/supervisor/children/${childId}/supervisors`),
  addChildSupervisor: (childId: number, supervisorId: number, relation: SupervisorRelation) =>
    http<SupervisorLink>(`${V1}/supervisor/children/${childId}/supervisors`, "POST", { supervisorId, relation }),
  removeChildSupervisor: (childId: number, supervisorId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/supervisors/${supervisorId}`, "DELETE"),

  // Stundenplan: welches Fach an welchem Wochentag. Ein Fach je Wochentag (409 `timetable_slot_taken`).
  childTimetable: (childId: number) =>
    http<TimetableEntry[]>(`${V1}/supervisor/children/${childId}/timetable`),
  addTimetableEntry: (childId: number, dto: CreateTimetableEntryDto) =>
    http<TimetableEntry>(`${V1}/supervisor/children/${childId}/timetable`, "POST", dto),
  removeTimetableEntry: (childId: number, entryId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/timetable/${entryId}`, "DELETE"),
  createChild: (dto: CreateChildDto) => http<ChildResponse>(`${V1}/supervisor/children`, "POST", dto),
  updateChild: (childId: number, dto: UpdateChildDto) =>
    http<ChildResponse>(`${V1}/supervisor/children/${childId}`, "PATCH", dto),

  // ---- Lückentext-Store: Trägertexte als Lerngrundlage (analog zum Vokabel-Store) ----
  // Der `key` ist die stabile Referenz und darum nur beim Anlegen setzbar; PATCH lässt ihn aus.
  clozeTexts: (p: { search?: string; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (p.search) q.set("search", p.search);
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<ClozeResponse>(`${V1}/creator/cloze-texts${qs ? `?${qs}` : ""}`);
  },
  createClozeText: (dto: CreateClozeDto) => http<ClozeResponse>(`${V1}/creator/cloze-texts`, "POST", dto),
  updateClozeText: (id: number, dto: UpdateClozeDto) =>
    http<ClozeResponse>(`${V1}/creator/cloze-texts/${id}`, "PATCH", dto),
  deleteClozeText: (id: number) => http<void>(`${V1}/creator/cloze-texts/${id}`, "DELETE"),

  // ---- Vater: Katalog (Fächer, Lehrwerk-Units, Übungssuche über Metadaten) ----
  subjects: () => http<SubjectResponse[]>(`${V1}/creator/subjects`),
  createSubject: (name: string) => http<SubjectResponse>(`${V1}/creator/subjects`, "POST", { name }),
  updateSubject: (subjectId: number, name: string) =>
    http<SubjectResponse>(`${V1}/creator/subjects/${subjectId}`, "PATCH", { name }),
  /** Löscht das Fach – die Reihen/Übungen selbst hängen seit B-106 an der Lehrwerk-Unit, nicht am Fach. */
  deleteSubject: (subjectId: number) => http<void>(`${V1}/creator/subjects/${subjectId}`, "DELETE"),

  // „Arten" (Kategorien) sind fachabhängig und dienen der Vorfilterung im Katalog.
  createCategory: (subjectId: number, name: string) =>
    http<CategoryResponse>(`${V1}/creator/subjects/${subjectId}/categories`, "POST", { name }),
  updateCategory: (subjectId: number, categoryId: number, name: string) =>
    http<CategoryResponse>(`${V1}/creator/subjects/${subjectId}/categories/${categoryId}`, "PATCH", { name }),
  deleteCategory: (subjectId: number, categoryId: number) =>
    http<void>(`${V1}/creator/subjects/${subjectId}/categories/${categoryId}`, "DELETE"),
  // Fachabhängige Arten ("Kategorien") – zur Vorfilterung im Katalog/Planbau.
  categories: (subjectId: number) =>
    http<CategoryResponse[]>(`${V1}/creator/subjects/${subjectId}/categories`),
  // Übung eines Typs in der Lehrwerk-Unit anlegen (Pflicht seit B-106). Das Routen-Segment
  // (vocabulary/arithmetic/…) bestimmt den Typ.
  createExercise: (seriesId: number, seriesUnitId: number, typeRoute: string, payload: CreateExercisePayload) =>
    http<ExerciseWriteResult>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/${typeRoute}`, "POST", payload),
  /**
   * Typ-Manifest: welche Übungstypen der Server kennt, wie sie heißen und unter welchem Routen-Segment
   * ihre Autoren-CRUD liegt. Einmal laden statt Tabellen im Frontend pflegen (siehe lib/exerciseTypes.ts).
   */
  exerciseTypes: () => http<ExerciseTypeManifest[]>(`${V1}/creator/exercise-types`),
  // Typ-übergreifender Detail-Abruf (mit Config) + „wo verwendet".
  getExercise: (id: number) => http<ExerciseDetail>(`${V1}/creator/exercises/${id}`),
  exerciseUsage: (id: number) => http<ExerciseUsage>(`${V1}/creator/exercises/${id}/usage`),
  /**
   * Übung freigeben oder **zurückziehen** (nur Owner). `false` stoppt neue Zuweisungen durch Fremde;
   * laufende Lehrpläne bleiben unberührt. Der einzige Weg, Material aus dem Verkehr zu nehmen – Löschen
   * verweigert eine benutzte Übung.
   */
  setExerciseSharing: (id: number, executePublic: boolean) =>
    http<ExerciseSharing>(`${V1}/creator/exercises/${id}/sharing`, "PATCH", { executePublic }),
  // Testmodus: eine Übung nebenwirkungsfrei durchspielen (keine Punkte/kein Fortschritt) und bewerten lassen.
  previewExercise: (id: number, stage?: number) =>
    http<ExercisePreviewData>(`${V1}/creator/exercises/${id}/preview${stage != null ? `?stage=${stage}` : ""}`),
  checkPreviewExercise: (id: number, answers: ExercisePreviewAnswer[], stage?: number) =>
    http<ExercisePreviewResult>(`${V1}/creator/exercises/${id}/preview/check`, "POST", { answers, stage }),
  // Ersetzen (PUT) bzw. Löschen laufen über die per-Typ-Route.
  updateExercise: (seriesId: number, seriesUnitId: number, typeRoute: string, id: number, payload: CreateExercisePayload) =>
    http<ExerciseWriteResult>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/${typeRoute}/${id}`, "PUT", payload),
  deleteExercise: (seriesId: number, seriesUnitId: number, typeRoute: string, id: number) =>
    http<void>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/${typeRoute}/${id}`, "DELETE"),

  // RWX-Rechte an einer Übung: mehrere Owner möglich, Write/Execute je Creator. Lesen darf jeder – dafür
  // gibt es kein Grant. Rechte vergeben/entziehen darf nur ein Owner.
  exerciseGrants: (exerciseId: number) =>
    http<ExerciseGrant[]>(`${V1}/creator/exercises/${exerciseId}/grants`),
  addExerciseGrant: (exerciseId: number, creatorId: number, permission: GrantPermission) =>
    http<ExerciseGrant>(`${V1}/creator/exercises/${exerciseId}/grants`, "POST", { creatorId, permission }),
  removeExerciseGrant: (exerciseId: number, creatorId: number, permission: GrantPermission) =>
    http<void>(`${V1}/creator/exercises/${exerciseId}/grants/${creatorId}/${permission}`, "DELETE"),
  // Bewertungen geteilter Übungen (B-12): lesen darf jeder Creator, bewerten nur, wer die Übung nicht verwaltet.
  /** B-628: die gezählten Rückmelde-Gründe einer Übung (nur mit Schreibrecht). */
  exerciseFeedbackSummary: (exerciseId: number) =>
    http<ExerciseFeedbackSummary>(`${V1}/creator/exercises/${exerciseId}/feedback`),
  /** B-628: das Kind meldet eine Karte (oder die ganze Übung) einer Position. */
  reportFeedback: (planId: number, positionId: number, dto: CreateFeedbackDto) =>
    http<FeedbackReceipt>(`${V1}/student/study-plans/${planId}/positions/${positionId}/feedback`, "POST", dto),
  feedbackNotices: (childId: number) => http<FeedbackNotice[]>(`${V1}/student/children/${childId}/feedback-notices`),
  ackFeedbackNotice: (childId: number, feedbackId: number) =>
    http<void>(`${V1}/student/children/${childId}/feedback-notices/${feedbackId}/seen`, "POST"),
  /** B-628: die Rückmeldungen der eigenen Kinder, optional für ein Kind und einen Status. */
  listFeedback: (childId?: number, status?: FeedbackStatus) => {
    const q = new URLSearchParams();
    if (childId !== undefined) q.set("childId", String(childId));
    if (status) q.set("status", status);
    const qs = q.toString();
    return http<FeedbackEntry[]>(`${V1}/supervisor/feedback${qs ? `?${qs}` : ""}`);
  },
  updateFeedback: (feedbackId: number, status: FeedbackStatus) =>
    http<FeedbackEntry>(`${V1}/supervisor/feedback/${feedbackId}`, "PATCH", { status }),
  exerciseRatings: (exerciseId: number) =>
    http<ExerciseRating[]>(`${V1}/creator/exercises/${exerciseId}/ratings`),
  rateExercise: (exerciseId: number, stars: number, comment: string | null) =>
    http<ExerciseRating>(`${V1}/creator/exercises/${exerciseId}/rating`, "PUT", { stars, comment }),
  removeExerciseRating: (exerciseId: number) =>
    http<void>(`${V1}/creator/exercises/${exerciseId}/rating`, "DELETE"),

  // Titelbild der Übung (und die übungslokale Übersteuerung am Item). Genauigkeits-Kaskade: das Item
  // schlägt die Vokabel; das Titelbild ist reine Deko der Übungskachel.
  exerciseMedia: (exerciseId: number) =>
    http<MediaLinkResponse[]>(`${V1}/creator/exercises/${exerciseId}/media`),
  linkExerciseMedia: (exerciseId: number, mediaAssetId: number, weight = 0) =>
    http<MediaLinkResponse>(`${V1}/creator/exercises/${exerciseId}/media`, "POST", { mediaAssetId, weight }),
  unlinkExerciseMedia: (exerciseId: number, linkId: number) =>
    http<void>(`${V1}/creator/exercises/${exerciseId}/media/${linkId}`, "DELETE"),
  exerciseItemMedia: (exerciseId: number, itemId: number) =>
    http<MediaLinkResponse[]>(`${V1}/creator/exercises/${exerciseId}/items/${itemId}/media`),
  linkExerciseItemMedia: (exerciseId: number, itemId: number, mediaAssetId: number, weight = 0) =>
    http<MediaLinkResponse>(`${V1}/creator/exercises/${exerciseId}/items/${itemId}/media`, "POST", { mediaAssetId, weight }),
  unlinkExerciseItemMedia: (exerciseId: number, itemId: number, linkId: number) =>
    http<void>(`${V1}/creator/exercises/${exerciseId}/items/${itemId}/media/${linkId}`, "DELETE"),

  // Vokabelpaare einer Übung: eigene Ebene mit stabilen Ids, daher CRUD statt „ganze Config ersetzen".
  // Genau deshalb lässt sich ein Wort nachtragen, ohne den Lernstand der übrigen Items zu verlieren.
  exerciseItems: (seriesId: number, seriesUnitId: number, exerciseId: number) =>
    http<VocabItemResponse[]>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/vocabulary/${exerciseId}/items`),
  addExerciseItem: (seriesId: number, seriesUnitId: number, exerciseId: number, body: VocabItemInput) =>
    http<VocabItemResponse>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/vocabulary/${exerciseId}/items`, "POST", body),
  patchExerciseItem: (seriesId: number, seriesUnitId: number, exerciseId: number, itemId: number, body: VocabItemInput) =>
    http<VocabItemResponse>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/vocabulary/${exerciseId}/items/${itemId}`, "PATCH", body),
  deleteExerciseItem: (seriesId: number, seriesUnitId: number, exerciseId: number, itemId: number) =>
    http<void>(`${V1}/creator/textbook-series/${seriesId}/units/${seriesUnitId}/vocabulary/${exerciseId}/items/${itemId}`, "DELETE"),
  searchExercises: (p: ExerciseSearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.seriesUnitId != null) q.set("seriesUnitId", String(p.seriesUnitId));
    if (p.seriesId != null) q.set("seriesId", String(p.seriesId));
    if (p.grade != null) q.set("grade", String(p.grade));
    if (p.schoolType && p.schoolType !== "None") q.set("schoolType", p.schoolType);
    if (p.categoryId != null) q.set("categoryId", String(p.categoryId));
    if (p.type) q.set("type", p.type);
    if (p.search) q.set("search", p.search);
    if (p.source) q.set("source", p.source);
    if (p.mineOnly) q.set("mineOnly", "true");
    if (p.grammarTopicId != null) q.set("grammarTopicId", String(p.grammarTopicId));
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<ExerciseSummary>(`${V1}/creator/exercises${qs ? `?${qs}` : ""}`);
  },

  // ---- Vater: Vokabel-Store ----
  // Optional nach Sprachpaar, Wortart und Tags filtern (Store zeigt dann nur die passenden Einträge).
  vocabulary: (p: VocabularySearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.search) q.set("search", p.search);
    if (p.sourceLanguage) q.set("sourceLanguage", p.sourceLanguage);
    if (p.targetLanguage) q.set("targetLanguage", p.targetLanguage);
    if (p.partOfSpeech) q.set("partOfSpeech", p.partOfSpeech);
    for (const t of p.tags ?? []) q.append("tag", t);
    if (p.matchAll) q.set("matchAll", "true");
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<VocabularyResponse>(`${V1}/creator/vocabulary${qs ? `?${qs}` : ""}`);
  },
  createVocabulary: (dto: CreateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/creator/vocabulary`, "POST", dto),
  // Viele Paare in einem Aufruf (idempotent) – für die zeilenweise Paar-Eingabe.
  createVocabularyBatch: (items: CreateVocabularyDto[]) =>
    http<VocabBatchResult[]>(`${V1}/creator/vocabulary/batch`, "POST", items),
  updateVocabulary: (id: number, patch: UpdateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/creator/vocabulary/${id}`, "PATCH", patch),
  deleteVocabulary: (id: number) => http<void>(`${V1}/creator/vocabulary/${id}`, "DELETE"),
  // Dublettenprüfung vor dem Anlegen: gibt es das Wort im Sprachpaar schon? Der Server vergleicht
  // ohne Rücksicht auf Groß-/Kleinschreibung (NOCASE-Kollation).
  vocabularyLookup: (sourceLanguage: string, targetLanguage: string, words: string[]) =>
    http<VocabLookupResponse>(`${V1}/creator/vocabulary/lookup`, "POST",
      { sourceLanguage, targetLanguage, words }),

  // ---- Globale (kindneutrale) Vokabel-Tags ----
  vocabTags: () => http<VocabTagResponse[]>(`${V1}/creator/vocabulary/tags`),
  // Verknüpft eine Vokabel mit Tag-Namen (create-if-missing); liefert die aktuellen Tags der Vokabel.
  attachVocabTags: (vocabId: number, tags: string[]) =>
    http<VocabTagResponse[]>(`${V1}/creator/vocabulary/${vocabId}/tags`, "POST", { tags }),
  detachVocabTag: (vocabId: number, tagId: number) =>
    http<void>(`${V1}/creator/vocabulary/${vocabId}/tags/${tagId}`, "DELETE"),

  // ---- Kind-skopierte Tags (auch an Vokabeln) ----
  childTags: (childId: number) => http<ChildTagResponse[]>(`${V1}/creator/tags?childId=${childId}`),
  createChildTag: (dto: CreateChildTagDto) =>
    http<ChildTagResponse>(`${V1}/creator/tags`, "POST", dto),
  tagsForVocabulary: (vocabId: number, childId: number) =>
    http<ChildTagResponse[]>(`${V1}/creator/tags/for-vocabulary/${vocabId}?childId=${childId}`),
  tagVocabulary: (tagId: number, vocabularyIds: number[]) =>
    http<ChildTagResponse>(`${V1}/creator/tags/${tagId}/vocabulary`, "POST", { vocabularyIds }),
  untagVocabulary: (tagId: number, vocabId: number) =>
    http<void>(`${V1}/creator/tags/${tagId}/vocabulary/${vocabId}`, "DELETE"),

  // ---- Lehrpläne (reiner Container; Ziele/Punkte je Position) ----
  plans: (childId?: number) =>
    http<PlanResponse[]>(`${V1}/supervisor/study-plans${childId ? `?childId=${childId}` : ""}`),
  plan: (planId: number) => http<PlanResponse>(`${V1}/supervisor/study-plans/${planId}`),
  createPlan: (dto: CreatePlanDto) => http<PlanResponse>(`${V1}/supervisor/study-plans`, "POST", dto),
  // Lehrplan nachträglich umbenennen/verlängern/deaktivieren (Inhalte laufen über Positionen).
  updatePlan: (planId: number, dto: UpdatePlanDto) =>
    http<PlanResponse>(`${V1}/supervisor/study-plans/${planId}`, "PATCH", dto),
  // Lehrplan samt Positionen löschen; die Katalog-Übungen bleiben erhalten. Ein gespielter Plan antwortet 409
  // `plan_has_data` (B-659): sein Lernstand und die erfüllten Tage gehören dem Kind.
  deletePlan: (planId: number) => http<void>(`${V1}/supervisor/study-plans/${planId}`, "DELETE"),

  // ---- Lehrplan-Positionen (Plan = Container aus Katalog-Übungen) ----
  positions: (planId: number) =>
    http<PositionResponse[]>(`${V1}/supervisor/study-plans/${planId}/positions`),
  addPosition: (planId: number, dto: CreatePositionDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions`, "POST", dto),
  // B-584: schwache Wörter des Kindes als tägliche Wiederholung – der Server baut die Übung aus genau diesen Store-Einträgen.
  addReviewFromWords: (planId: number, dto: CreateReviewFromWordsDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions/from-words`, "POST", dto),
  updatePosition: (planId: number, positionId: number, dto: UpdatePositionDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}`, "PATCH", dto),
  deletePosition: (planId: number, positionId: number) =>
    http<void>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}`, "DELETE"),
  // Lern-Report der Position: je Inhalt Box/Beherrschung + Test-Trefferquote („sitzt/sitzt nicht").
  // Liegt unter `supervisor/…` und ist `[Authorize(Roles = Supervisor)]` + PlanOwnershipFilter: jede Zeile
  // trägt die Lösung des Inhalts, auch für Karten, die das Kind noch nie gesehen hat. Ein Kind-Token bekommt
  // darum 403 – das ist die Auswertung des Vaters, kein kind-lesbarer Lernstand (der steht darunter).
  positionReport: (planId: number, positionId: number) =>
    http<PositionReport>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}/report`),

  // ---- Lernstand eines Kindes (plan-übergreifend) ----
  // Liegt unter `student/…`, ist aber für beide Rollen gedacht: die Controller sind nur `[Authorize]` +
  // ChildOwnershipFilter, damit der Supervisor den Stand seines Kindes mitlesen darf.
  /** „Schlecht gelernte Wörter": Rollup je Store-Wort über alle Übungen, schwächste zuerst. */
  childWordMastery: (childId: number, p: { onlyWeak?: boolean; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (p.onlyWeak) q.set("onlyWeak", "true");
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<WordMastery>(`${V1}/student/children/${childId}/vocabulary-progress/by-word${qs ? `?${qs}` : ""}`);
  },
  /** Item-Lernstand, schwächste zuerst; optional auf eine Übung, eine Box-Obergrenze oder „schwach" beschränkt. */
  childItemProgress: (childId: number, p: { exerciseId?: number; maxBox?: number; onlyWeak?: boolean; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (p.exerciseId != null) q.set("exerciseId", String(p.exerciseId));
    if (p.maxBox != null) q.set("maxBox", String(p.maxBox));
    if (p.onlyWeak) q.set("onlyWeak", "true");
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<ItemProgressResponse>(`${V1}/student/children/${childId}/vocabulary-progress${qs ? `?${qs}` : ""}`);
  },
  /** Antwort-Historie eines Items, neueste zuerst – zeigt, *wie* falsch geantwortet wurde. */
  childItemHistory: (childId: number, itemId: number, take = 20) =>
    httpPaged<ItemHistoryEntry>(`${V1}/student/children/${childId}/vocabulary-progress/${itemId}/history?take=${take}`),

  // Katalog-hierarchischer Drilldown (Fach → Lehrwerk-Unit → Übung → Item), abgeleitet aus den Lehrplänen.
  childLearnSubjects: (childId: number) =>
    http<SubjectProgress[]>(`${V1}/student/children/${childId}/learn/subjects`),
  childLearnSeriesUnits: (childId: number, subjectId: number) =>
    http<SeriesUnitProgress[]>(`${V1}/student/children/${childId}/learn/subjects/${subjectId}/series-units`),
  childLearnExercises: (childId: number, subjectId: number, seriesUnitId: number) =>
    http<ExerciseProgress[]>(`${V1}/student/children/${childId}/learn/subjects/${subjectId}/series-units/${seriesUnitId}/vocabulary`),
  childLearnItems: (childId: number, subjectId: number, seriesUnitId: number, exerciseId: number) =>
    http<ItemProgressResponse[]>(
      `${V1}/student/children/${childId}/learn/subjects/${subjectId}/series-units/${seriesUnitId}/vocabulary/${exerciseId}/items`),

  // ---- Ziele über dem Lernstand: Objectives (Klammer) mit ihren Etappen (Key Results) ----
  // Sie werden bei jeder Abfrage live aus dem Lernstand ausgewertet – deshalb keine „Fortschritt
  // aktualisieren"-Aktion: es gibt keinen gespeicherten Stand, der veralten könnte.
  // Die frühere zweite Ebene „Lernziel" (`LearnGoal`) ist mit dem DB-Struktur-Umbau E13 gelöscht; ihre
  // Rolle – eine einzelne Messlatte auf einem Stück Katalog – trägt heute das Key Result.

  objectives: (childId: number, p: { status?: GoalStatus; kind?: ObjectiveKind } = {}) => {
    const q = new URLSearchParams();
    if (p.status) q.set("status", p.status);
    if (p.kind) q.set("kind", p.kind);
    const qs = q.toString();
    return httpPaged<Objective>(`${V1}/supervisor/children/${childId}/objectives${qs ? `?${qs}` : ""}`);
  },
  createObjective: (childId: number, dto: CreateObjectiveRequest) =>
    http<Objective>(`${V1}/supervisor/children/${childId}/objectives`, "POST", dto),
  updateObjective: (childId: number, objectiveId: number, dto: UpdateObjectiveRequest) =>
    http<Objective>(`${V1}/supervisor/children/${childId}/objectives/${objectiveId}`, "PATCH", dto),
  deleteObjective: (childId: number, objectiveId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/objectives/${objectiveId}`, "DELETE"),

  createKeyResult: (childId: number, objectiveId: number, dto: CreateKeyResultRequest) =>
    http<KeyResult>(`${V1}/supervisor/children/${childId}/objectives/${objectiveId}/key-results`, "POST", dto),
  updateKeyResult: (childId: number, objectiveId: number, keyResultId: number, dto: UpdateKeyResultRequest) =>
    http<KeyResult>(`${V1}/supervisor/children/${childId}/objectives/${objectiveId}/key-results/${keyResultId}`, "PATCH", dto),
  deleteKeyResult: (childId: number, objectiveId: number, keyResultId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/objectives/${objectiveId}/key-results/${keyResultId}`, "DELETE"),

  // ---- Vater: kindübergreifender Tagesüberblick ----
  childrenDaily: (date?: string) =>
    http<ChildrenDashboard>(`${V1}/supervisor/children/daily-overview${date ? `?date=${date}` : ""}`),
  // B-39: die Kinder über die Zeit – je Tag, Woche oder Monat, höchstens 92 Tage.
  childrenOverview: (q: { from: string; to: string; granularity: OverviewGranularity }) =>
    http<ChildrenOverview>(`${V1}/supervisor/children/overview?from=${q.from}&to=${q.to}&granularity=${q.granularity}`),

  // ---- Tagesmission (Sohn) / Verlauf (Vater) über Positionen ----
  overview: (planId: number) => http<OverviewResponse>(`${V1}/student/study-plans/${planId}/overview`),
  overviewProgress: (planId: number) => http<ProgressResponse>(`${V1}/student/study-plans/${planId}/overview/progress`),

  // ---- Sohn: Position üben (Leitner) ----
  startSession: (planId: number, positionId: number) =>
    http<PositionSession>(`${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions`, "POST", {}),
  heartbeat: (planId: number, positionId: number, sessionId: number, seconds: number, active: boolean) =>
    http<PositionSession>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/heartbeat`, "POST", { seconds, active }),
  cards: (planId: number, positionId: number, sessionId: number) =>
    http<PracticeCard[]>(`${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/cards`),
  // Der Server bewertet serverseitig: das Frontend liefert nur die Antwort (getippt) bzw. bei
  // Anzeige-/Selbsteinschätzungs-Stufen das WasKnown-Flag; die Stufe erzwingt der Server.
  review: (planId: number, positionId: number, sessionId: number, dto: ReviewInput) =>
    http<ReviewOutcome | undefined>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/review`, "POST", dto),
  // B-635: die aktuelle Karte eine Stufe leichter – der Server führt die Stufe und schickt die Karte in ihrer neuen Form.
  easier: (planId: number, positionId: number, sessionId: number, itemIndex: number) =>
    http<PracticeCard>(`${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/easier`,
      "POST", { itemIndex }),
  // B-530: „die nächste Karte ist jetzt sichtbar" – der Server stempelt mit seiner Uhr; der Client meldet nur das Wann
  // und welche Karte. Der Server stempelt nur, wenn sie seine aktuelle Karte ist.
  cardShown: (planId: number, positionId: number, sessionId: number, itemIndex: number) =>
    http<void>(`${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/shown`, "POST",
      { itemIndex }),
  endSession: (planId: number, positionId: number, sessionId: number) =>
    http<PositionSession>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/end`, "POST", {}),

  // ---- Sohn: Position testen (Abschlusstest = Klausur, strikt server-getrieben) ----
  // Der Start liefert nur Metadaten; die Fragen kommen einzeln über nextTest, beantwortet wird über
  // answerTest (ohne Korrektheit – Feedback erst beim Abschluss), submitTest wertet aus.
  startTest: (planId: number, positionId: number) =>
    http<TestAttemptResponse>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests`, "POST", {}),
  nextTest: (planId: number, positionId: number, attemptId: number) =>
    http<TestNextResponse>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/next`),
  answerTest: (planId: number, positionId: number, attemptId: number, dto: AnswerDto) =>
    http<TestAnswerAck>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/answer`, "POST", dto),
  submitTest: (planId: number, positionId: number, attemptId: number, answers: AnswerDto[] = []) =>
    http<TestSubmitResponse>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/submit`, "POST", { answers }),

  // ---- Sohn: Bonus-Training (Extra-Übungen nach der Tagespflicht) ----
  // Eine Runde wird am Stück abgegeben; der Server wählt die Form, wertet und bucht die Münzen.
  extraTraining: (childId: number) =>
    http<ExtraTrainingOverview>(`${V1}/student/children/${childId}/extra-training`),
  // ---- Vokabel-Türme (natives Spiel, B-851): das Web liest nur den Tagesstand für die Vater-Einstellung ----
  towerGame: (childId: number) =>
    http<TowerGameOverview>(`${V1}/student/children/${childId}/tower-game`),
  startExtraRun: (childId: number, exerciseId: number) =>
    http<ExtraRunResponse>(`${V1}/student/children/${childId}/extra-training/${exerciseId}/runs`, "POST"),
  submitExtraRun: (childId: number, runId: number, answers: ExtraAnswer[]) =>
    http<ExtraRunResult>(`${V1}/student/children/${childId}/extra-training/runs/${runId}/submit`, "POST", { answers }),
  // ---- Sohn: Mischrunde (B-766) – fällige Karten aus allen Plänen, hinter der Sperre des Bonus-Trainings ----
  mixedReview: (childId: number) =>
    http<MixedReviewOverview>(`${V1}/student/children/${childId}/mixed-review`),
  startMixedReview: (childId: number) =>
    http<MixedReviewRunResponse>(`${V1}/student/children/${childId}/mixed-review/runs`, "POST"),
  submitMixedReview: (childId: number, runId: number, answers: MixedReviewAnswer[]) =>
    http<MixedReviewResult>(`${V1}/student/children/${childId}/mixed-review/runs/${runId}/submit`, "POST", { answers }),

  // ---- Sohn: Kind-Tagessicht (B-606) ----
  // Die Pflichten aller aktiven Pläne an einer Stelle, mit Serie und Truhe – die Startseite der Arcade.
  today: (childId: number) => http<TodayResponse>(`${V1}/student/children/${childId}/today`),

  // ---- Sohn: Klassenarbeiten (B-750/B-751) ----
  // Nur geplante ab heute, früheste zuerst; die Note bleibt beim Vater.
  upcomingClassTests: (childId: number) =>
    http<UpcomingClassTest[]>(`${V1}/student/children/${childId}/class-tests/upcoming`),
  classTestReadiness: (childId: number, classTestId: number) =>
    http<ClassTestReadiness>(`${V1}/student/children/${childId}/class-tests/${classTestId}/readiness`),

  // ---- Sohn: Tagestruhe (B-527) ----
  // Verdient beim Abschluss der Tagesziele, gebucht erst hier: der Server öffnet die älteste wartende Truhe.
  openDailyBox: (childId: number) =>
    http<DailyBoxOpened>(`${V1}/student/children/${childId}/daily-box/open`, "POST"),

  // ---- Sohn: Wallet ----
  // Kontostand (Salden) und Buchungsverlauf sind getrennt: Salden als Einzelwerte, Buchungen server-paginiert.
  wallet: () => http<WalletBalance>(`${V1}/student/me/points`),
  weekCoins: () => http<WeekCoins>(`${V1}/student/me/points/week`),
  walletEntries: (opts: { skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    appendPaging(q, opts);
    const qs = q.toString();
    return httpPaged<WalletEntry>(`${V1}/student/me/points/entries${qs ? `?${qs}` : ""}`);
  },

  // ---- Sohn: Missionen & Auszeichnungen ----
  missions: () => http<MissionStatus[]>(`${V1}/student/me/missions`),
  achievements: () => http<AchievementStatus[]>(`${V1}/student/me/achievements`),

  // Die eigenen großen Ziele (OKR). Der Server liefert nur **aktive** – ein vom Vater stillgelegtes
  // Ziel soll das Kind gar nicht erst sehen, sonst arbeitet es auf etwas hin, das nicht mehr zählt.
  myObjectives: (p: { skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<Objective>(`${V1}/student/me/objectives${qs ? `?${qs}` : ""}`);
  },

  // Die eigene Notenstufe je Vokabel-Fach (B-742/B-743). Der erste Abruf des Tages bewertet neu (träge, kein Planer).
  mySubjectLevels: () => http<SubjectLevel[]>(`${V1}/student/me/subject-levels`),
  // Dieselben Zahlen für den Vater, je Kind (nie mehrere Kinder nebeneinander, B-682 Entscheidung 6).
  childSubjectLevels: (childId: number) => http<SubjectLevel[]>(`${V1}/supervisor/children/${childId}/subject-levels`),

  // ---- Sohn: Skins (Besitz server-autoritativ; Kauf bucht Münzen ab) ----
  skins: () => http<SkinState>(`${V1}/student/me/skins`),
  purchaseSkin: (skinId: string) => http<SkinState>(`${V1}/student/me/skins/${skinId}/purchase`, "POST", {}),
  equipSkin: (skinId: string) => http<SkinState>(`${V1}/student/me/skins/${skinId}/equip`, "POST", {}),

  // ---- Vater: Missionen (Belohnungsziele) je Kind verwalten ----
  missionsFor: (childId: number) => http<MissionDef[]>(`${V1}/supervisor/children/${childId}/missions`),
  createMission: (childId: number, dto: CreateMissionDto) =>
    http<MissionDef>(`${V1}/supervisor/children/${childId}/missions`, "POST", dto),
  updateMission: (childId: number, missionId: number, dto: UpdateMissionDto) =>
    http<MissionDef>(`${V1}/supervisor/children/${childId}/missions/${missionId}`, "PATCH", dto),
  deleteMission: (childId: number, missionId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/missions/${missionId}`, "DELETE"),

  // ---- Vater: Auszeichnungen (Badges) je Kind verwalten ----
  achievementsFor: (childId: number) => http<AchievementDef[]>(`${V1}/supervisor/children/${childId}/achievements`),
  createAchievement: (childId: number, dto: CreateAchievementDto) =>
    http<AchievementDef>(`${V1}/supervisor/children/${childId}/achievements`, "POST", dto),
  updateAchievement: (childId: number, achievementId: number, dto: UpdateAchievementDto) =>
    http<AchievementDef>(`${V1}/supervisor/children/${childId}/achievements/${achievementId}`, "PATCH", dto),
  deleteAchievement: (childId: number, achievementId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/achievements/${achievementId}`, "DELETE"),

  // ---- Vater: Konto-Übersicht (Punktestand + Buchungsverlauf je Kind) ----
  // Der Buchungsverlauf ist server-paginiert (Einträge in der Hülle + X-Total-Count); die Salden sind
  // über ALLE Zeilen berechnet, bleiben also über die Seiten stabil.
  childPoints: async (childId: number, opts: { skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    appendPaging(q, opts);
    const qs = q.toString();
    const res = await request(`${V1}/supervisor/children/${childId}/points${qs ? `?${qs}` : ""}`, "GET");
    const text = await res.text();
    const body = (text ? JSON.parse(text) : { coins: 0, gems: 0, entries: [] }) as Wallet;
    return { coins: body.coins, gems: body.gems, items: body.entries, total: totalFrom(res, body.entries.length) };
  },
  // Manuelle Vater-Buchung: positiver Betrag = verschenken/gutschreiben, negativ = abziehen; Währung wählbar
  // (auch Gems, das Druckventil gegen zu hohe Malus-Schulden bzw. Belohnung außerhalb der App).
  grantPoints: (childId: number, amount: number, reason: string, currency: Currency) =>
    http<ChildPointsEntry>(`${V1}/supervisor/children/${childId}/points`, "POST", { amount, reason, currency }),

  // ---- Vater: Klassenarbeiten (planen, Übungen zuweisen, benoten, üben/wiederholen) ----
  classTests: (childId: number, opts: { status?: KlassenarbeitStatus; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams({ childId: String(childId) });
    if (opts.status) q.set("status", opts.status);
    appendPaging(q, opts);
    return httpPaged<KlassenarbeitResponse>(`${V1}/supervisor/class-tests?${q.toString()}`);
  },
  classTest: (id: number) => http<KlassenarbeitDetail>(`${V1}/supervisor/class-tests/${id}`),
  createClassTest: (dto: CreateKlassenarbeitDto) =>
    http<KlassenarbeitDetail>(`${V1}/supervisor/class-tests`, "POST", dto),
  updateClassTest: (id: number, dto: UpdateKlassenarbeitDto) =>
    http<KlassenarbeitResponse>(`${V1}/supervisor/class-tests/${id}`, "PATCH", dto),
  deleteClassTest: (id: number) => http<void>(`${V1}/supervisor/class-tests/${id}`, "DELETE"),
  assignClassTestExercises: (id: number, exerciseIds: number[]) =>
    http<KlassenarbeitDetail>(`${V1}/supervisor/class-tests/${id}/exercises`, "POST", { exerciseIds }),
  unassignClassTestExercise: (id: number, exerciseId: number) =>
    http<void>(`${V1}/supervisor/class-tests/${id}/exercises/${exerciseId}`, "DELETE"),
  classTestPractice: (id: number) => http<KlassenarbeitPractice>(`${V1}/supervisor/class-tests/${id}/practice`),
  classTestRepeat: (childId: number, minBadGrade?: number) => {
    const q = new URLSearchParams({ childId: String(childId) });
    if (minBadGrade != null) q.set("minBadGrade", String(minBadGrade));
    return http<KlassenarbeitRepeat>(`${V1}/supervisor/class-tests/repeat?${q.toString()}`);
  },

  // ---- Sohn: Familien-Shop (einziger Münz-Ausgabeweg) ----
  // Die Shop-Sicht bündelt Salden + kaufbare Angebote + Inventar + Kaufhistorie; Kauf und Aktivierung
  // liefern jeweils den frischen Stand zurück, damit der Client nicht separat nachladen muss.
  shopView: () => http<ShopView>(`${V1}/student/me/shop`),
  // The purchase history is paged (B-99, a fixed cutoff used to end it silently): a page of `purchases`
  // plus the REAL total from `X-Total-Count`, read off the same bundled endpoint via its query params.
  shopPurchasesPage: async (skip: number, take: number): Promise<Paged<MyShopPurchase>> => {
    const res = await request(`${V1}/student/me/shop?purchaseSkip=${skip}&purchaseTake=${take}`, "GET");
    const text = await res.text();
    const view = (text ? JSON.parse(text) : undefined) as ShopView;
    return { items: view.purchases, total: totalFrom(res, view.purchases.length) };
  },
  purchaseListing: (listingId: number) =>
    http<ShopView>(`${V1}/student/me/shop/listings/${listingId}/purchase`, "POST", {}),
  myInventory: () => http<MyInventoryItem[]>(`${V1}/student/me/shop/inventory`),
  activateInventory: (articleId: number, quantity: number) =>
    http<MyActivation>(`${V1}/student/me/shop/inventory/${articleId}/activate`, "POST", { quantity }),
  myActivations: () => http<MyActivation[]>(`${V1}/student/me/shop/activations`),

  // ---- Vater: Familien-Shop verwalten ----
  // Artikel = die Belohnungs-*Art* (Preis/Bestand liegen an den Angeboten je Artikel).
  shopArticles: (search?: string) => {
    const q = new URLSearchParams();
    if (search) q.set("search", search);
    const qs = q.toString();
    return http<ShopArticle[]>(`${V1}/supervisor/shop/articles${qs ? `?${qs}` : ""}`);
  },
  createShopArticle: (dto: CreateShopArticleDto) =>
    http<ShopArticle>(`${V1}/supervisor/shop/articles`, "POST", dto),
  updateShopArticle: (articleId: number, dto: UpdateShopArticleDto) =>
    http<ShopArticle>(`${V1}/supervisor/shop/articles/${articleId}`, "PATCH", dto),
  deleteShopArticle: (articleId: number) =>
    http<void>(`${V1}/supervisor/shop/articles/${articleId}`, "DELETE"),

  // Angebote je Artikel (Coin/Gem-Preis, Menge je Kauf, Bestand, optionales Auffüllen).
  shopListings: (articleId: number) =>
    http<ShopListing[]>(`${V1}/supervisor/shop/articles/${articleId}/listings`),
  createShopListing: (articleId: number, dto: CreateShopListingDto) =>
    http<ShopListing>(`${V1}/supervisor/shop/articles/${articleId}/listings`, "POST", dto),
  updateShopListing: (articleId: number, listingId: number, dto: UpdateShopListingDto) =>
    http<ShopListing>(`${V1}/supervisor/shop/articles/${articleId}/listings/${listingId}`, "PATCH", dto),
  deleteShopListing: (articleId: number, listingId: number) =>
    http<void>(`${V1}/supervisor/shop/articles/${articleId}/listings/${listingId}`, "DELETE"),
  // B-645: der Bestand gehört jedem Kind einzeln – „füllen" setzt ihn für genau eines.
  setShopListingStock: (articleId: number, listingId: number, childId: number, currentStock: number) =>
    http<ShopListing>(`${V1}/supervisor/shop/articles/${articleId}/listings/${listingId}/stock/${childId}`, "PUT", { currentStock }),

  // Kindbezogene Sicht: Inventar, Käufe (stornierbar) und Aktivierungsanfragen (genehmigen/ablehnen).
  childInventory: (childId: number) =>
    http<InventoryItem[]>(`${V1}/supervisor/children/${childId}/shop/inventory`),
  // Server-paginiert (B-113): der Vater storniert aus dieser Liste heraus, eine Zeile hinter der
  // Server-Grenze wäre für ihn unerreichbar - siehe ListControls.tsx `Pager`.
  childPurchases: (childId: number, opts: { status?: ShopPurchaseStatus; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (opts.status) q.set("status", opts.status);
    appendPaging(q, opts);
    return httpPaged<ShopPurchase>(`${V1}/supervisor/children/${childId}/shop/purchases?${q.toString()}`);
  },
  // B-665: der Vater löst ausnahmsweise selbst aus dem Inventar ein – sofort freigegeben.
  redeemFromInventory: (childId: number, articleId: number, quantity: number) =>
    http<ActivationRequest>(`${V1}/supervisor/children/${childId}/shop/inventory/${articleId}/redeem`, "POST", { quantity }),
  cancelPurchase: (childId: number, purchaseId: number) =>
    http<ShopPurchase>(`${V1}/supervisor/children/${childId}/shop/purchases/${purchaseId}/cancel`, "POST", {}),
  childActivations: (childId: number, status?: ActivationRequestStatus) => {
    const q = new URLSearchParams();
    if (status) q.set("status", status);
    const qs = q.toString();
    return http<ActivationRequest[]>(`${V1}/supervisor/children/${childId}/shop/activations${qs ? `?${qs}` : ""}`);
  },
  // B-747: die Anfragen aller Kinder in einem Aufruf; `total` ist die gefilterte Gesamtzahl (Zähler an der Navigation).
  allActivations: (p: { status?: ActivationRequestStatus; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (p.status) q.set("status", p.status);
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<ActivationRequest>(`${V1}/supervisor/shop/activations${qs ? `?${qs}` : ""}`);
  },
  approveActivation: (childId: number, requestId: number) =>
    http<ActivationRequest>(`${V1}/supervisor/children/${childId}/shop/activations/${requestId}/approve`, "POST", {}),
  rejectActivation: (childId: number, requestId: number) =>
    http<ActivationRequest>(`${V1}/supervisor/children/${childId}/shop/activations/${requestId}/reject`, "POST", {}),

  // ---- Verlage: geteiltes, slug-idempotentes Vokabular, keine eigene Übungs-Ebene ----
  publishers: (search?: string) =>
    http<PublisherResponse[]>(`${V1}/creator/publishers${search ? `?search=${encodeURIComponent(search)}&take=200` : "?take=200"}`),
  createPublisher: (dto: CreatePublisherDto) =>
    http<PublisherResponse>(`${V1}/creator/publishers`, "POST", dto),
  updatePublisher: (id: number, dto: UpdatePublisherDto) =>
    http<PublisherResponse>(`${V1}/creator/publishers/${id}`, "PATCH", dto),
  deletePublisher: (id: number) => http<void>(`${V1}/creator/publishers/${id}`, "DELETE"),

  // ---- Unterrichtsmaterial: Lehrwerk-Reihen und ihre Units ----
  // Der Katalog ist geteilt: lesen darf jeder Creator, ändern nur der Owner (`isOwn` sagt es der UI).
  // Anlegen ist über den Slug idempotent – derselbe Name liefert die bestehende Reihe zurück.
  textbookSeries: (p: {
    search?: string; subjectId?: number; publisherId?: number; schoolTypes?: SchoolType; grade?: number;
    mineOnly?: boolean;
  } = {}) => {
    const q = new URLSearchParams({ take: "200" });
    if (p.search) q.set("search", p.search);
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.publisherId != null) q.set("publisherId", String(p.publisherId));
    if (p.schoolTypes) q.set("schoolTypes", p.schoolTypes);
    if (p.grade != null) q.set("grade", String(p.grade));
    if (p.mineOnly) q.set("mineOnly", "true");
    return http<TextbookSeriesResponse[]>(`${V1}/creator/textbook-series?${q}`);
  },
  createTextbookSeries: (dto: CreateTextbookSeriesDto) =>
    http<TextbookSeriesResponse>(`${V1}/creator/textbook-series`, "POST", dto),
  updateTextbookSeries: (seriesId: number, dto: UpdateTextbookSeriesDto) =>
    http<TextbookSeriesResponse>(`${V1}/creator/textbook-series/${seriesId}`, "PATCH", dto),
  deleteTextbookSeries: (seriesId: number) =>
    http<void>(`${V1}/creator/textbook-series/${seriesId}`, "DELETE"),

  seriesUnits: (seriesId: number, grade?: number) =>
    http<SeriesUnitResponse[]>(`${V1}/creator/textbook-series/${seriesId}/units${grade != null ? `?grade=${grade}` : ""}`),
  createSeriesUnit: (seriesId: number, dto: CreateSeriesUnitDto) =>
    http<SeriesUnitResponse>(`${V1}/creator/textbook-series/${seriesId}/units`, "POST", dto),
  updateSeriesUnit: (seriesId: number, unitId: number, dto: UpdateSeriesUnitDto) =>
    http<SeriesUnitResponse>(`${V1}/creator/textbook-series/${seriesId}/units/${unitId}`, "PATCH", dto),
  deleteSeriesUnit: (seriesId: number, unitId: number) =>
    http<void>(`${V1}/creator/textbook-series/${seriesId}/units/${unitId}`, "DELETE"),

  // ---- Creator-Profile („Fachlehrer") ----
  creatorProfiles: (p: { subjectId?: number; seriesId?: number; mineOnly?: boolean; includeInactive?: boolean } = {}) => {
    const q = new URLSearchParams();
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.seriesId != null) q.set("seriesId", String(p.seriesId));
    if (p.mineOnly) q.set("mineOnly", "true");
    if (p.includeInactive) q.set("includeInactive", "true");
    const qs = q.toString();
    return http<CreatorProfileResponse[]>(`${V1}/creator/profiles${qs ? `?${qs}` : ""}`);
  },
  createCreatorProfile: (dto: CreateCreatorProfileDto) =>
    http<CreatorProfileResponse>(`${V1}/creator/profiles`, "POST", dto),
  updateCreatorProfile: (profileId: number, dto: UpdateCreatorProfileDto) =>
    http<CreatorProfileResponse>(`${V1}/creator/profiles/${profileId}`, "PATCH", dto),
  deleteCreatorProfile: (profileId: number) =>
    http<void>(`${V1}/creator/profiles/${profileId}`, "DELETE"),
  /**
   * Welcher Fachlehrer passt zu diesem Kind? Deterministisch bewertet (Reihe > Fach > Klassenstufe >
   * Schulart). Liegt auf der Creator-Route, verlangt aber die Betreuung des Kindes.
   */
  matchCreatorProfiles: (childId: number, subjectId?: number) =>
    http<CreatorProfileMatch[]>(
      `${V1}/creator/profiles/match?childId=${childId}${subjectId != null ? `&subjectId=${subjectId}` : ""}`),

  // ---- Lehrbücher des Kindes (Brücke zwischen Kind und geteiltem Lehrwerk-Katalog) ----
  childTextbooks: (childId: number) =>
    http<TextbookResponse[]>(`${V1}/supervisor/children/${childId}/textbooks`),
  createChildTextbook: (childId: number, dto: CreateTextbookDto) =>
    http<TextbookResponse>(`${V1}/supervisor/children/${childId}/textbooks`, "POST", dto),
  updateChildTextbook: (childId: number, textbookId: number, dto: UpdateTextbookDto) =>
    http<TextbookResponse>(`${V1}/supervisor/children/${childId}/textbooks/${textbookId}`, "PATCH", dto),
  deleteChildTextbook: (childId: number, textbookId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/textbooks/${textbookId}`, "DELETE"),

  // ---- Bilder & Interessen ----
  // Die Taxonomie ist EIN Vokabular für zwei Seiten: Bilder tragen die Schlagworte als Eigenschaft,
  // Kinder als gewichtete Vorliebe/Abneigung. Nur deshalb ist die Bildauswahl je Kind berechenbar.
  interestTags: (search?: string) =>
    http<InterestTagResponse[]>(`${V1}/creator/interest-tags${search ? `?search=${encodeURIComponent(search)}&take=200` : "?take=200"}`),
  createInterestTag: (dto: CreateInterestTagDto) =>
    http<InterestTagResponse>(`${V1}/creator/interest-tags`, "POST", dto),
  /** Ändert Label, Facette, Synonyme oder Farbe. Der **Slug** bleibt: an ihm hängen Bilder und Kind-Profile. */
  updateInterestTag: (id: number, dto: UpdateInterestTagDto) =>
    http<InterestTagResponse>(`${V1}/creator/interest-tags/${id}`, "PATCH", dto),
  /**
   * Löscht ein Schlagwort samt seiner Bild- und Kind-Verknüpfungen. Seit B-206 **gesperrt**, solange es
   * Übungen einkleidet (`interest_tag_in_use`) – deren Einkleidung ist Autoren-Arbeit und wäre lautlos weg.
   */
  deleteInterestTag: (id: number) => http<void>(`${V1}/creator/interest-tags/${id}`, "DELETE"),

  // ---- Creator: Grammatik-Themen (B-155) – fachgebundenes Vokabular, von Übungen und Units getragen ----
  grammarTopics: (subjectId?: number, search?: string) => {
    const q = new URLSearchParams();
    if (subjectId != null) q.set("subjectId", String(subjectId));
    if (search) q.set("search", search);
    q.set("take", "500");
    return http<GrammarTopicResponse[]>(`${V1}/creator/grammar-topics?${q}`);
  },
  createGrammarTopic: (dto: CreateGrammarTopicDto) =>
    http<GrammarTopicResponse>(`${V1}/creator/grammar-topics`, "POST", dto),
  updateGrammarTopic: (id: number, dto: UpdateGrammarTopicDto) =>
    http<GrammarTopicResponse>(`${V1}/creator/grammar-topics/${id}`, "PATCH", dto),
  deleteGrammarTopic: (id: number) => http<void>(`${V1}/creator/grammar-topics/${id}`, "DELETE"),

  childInterests: (childId: number) =>
    http<ChildInterestResponse[]>(`${V1}/supervisor/children/${childId}/interests`),
  /** Ersetzt die Menge vollständig – das UI bearbeitet sie als Ganzes. */
  setChildInterests: (childId: number, interests: ChildInterestInput[]) =>
    http<ChildInterestResponse[]>(`${V1}/supervisor/children/${childId}/interests`, "PUT", { interests }),

  /**
   * Die Auswahlliste, aus der das Kind sich bedient: alle für Kinder freigegebenen Schlagworte (B-207). Bewusst die
   * Kind-Route und nicht `creator/interest-tags`: die trägt Nutzungszähler und ist für einen Kind-Token gesperrt.
   * Seitenweise bis `X-Total-Count` statt eines geratenen `take=500`: ab der 501. Kachel fielen sonst lautlos welche
   * heraus, und welche, entschiede die Sortierung.
   */
  interestOptions: async () => {
    const all: InterestTagOption[] = [];
    for (;;) {
      const page = await httpPaged<InterestTagOption>(`${V1}/student/interest-tags?skip=${all.length}&take=500`);
      all.push(...page.items);
      if (page.items.length === 0 || all.length >= page.total) return all;
    }
  },
  /** Das eigene Interessen-Profil des angemeldeten Kindes (Schlagworte + Freitext-Wünsche). */
  myInterests: () => http<MyInterestProfile>(`${V1}/student/me/interests`),
  /** Ersetzt die eigenen Interessen vollständig – nur bestehende `tagId`s, das Kind legt nie eine an. */
  setMyInterests: (interests: MyInterestInput[]) =>
    http<MyInterestProfile>(`${V1}/student/me/interests`, "PUT", { interests }),
  /** Ein kurzer Freitext-Wunsch außerhalb der Taxonomie; landet für den Vater sichtbar am Kind-Profil. */
  addMyInterestWish: (text: string) =>
    http<MyInterestProfile>(`${V1}/student/me/interests/wishes`, "POST", { text }),

  media: (p: { search?: string; tag?: string[]; maxRating?: ContentRating; origin?: MediaOrigin; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (p.search) q.set("search", p.search);
    if (p.origin) q.set("origin", p.origin);
    if (p.maxRating) q.set("maxRating", p.maxRating);
    for (const t of p.tag ?? []) q.append("tag", t);
    q.set("take", String(p.take ?? 100));
    return http<MediaAssetResponse[]>(`${V1}/creator/media?${q}`);
  },
  createMedia: (dto: CreateMediaAssetDto) => http<MediaAssetResponse>(`${V1}/creator/media`, "POST", dto),
  /**
   * Bild-Upload: der Server erzeugt die Auflösungen selbst. Geht bewusst nicht über `http()` – dort setzt
   * der Wrapper `Content-Type: application/json`; bei multipart muss der Browser den Header samt
   * `boundary` selbst bestimmen.
   */
  uploadMedia: (file: File, fields: { description: string; tags?: string; rating?: ContentRating }) => {
    const form = new FormData();
    form.append("file", file);
    form.append("description", fields.description);
    if (fields.tags) form.append("tags", fields.tags);
    if (fields.rating) form.append("rating", fields.rating);
    return httpForm<MediaAssetResponse>(`${V1}/creator/media/upload`, form);
  },
  deleteMedia: (assetId: number) => http<void>(`${V1}/creator/media/${assetId}`, "DELETE"),
  /** Wo ein Bild hängt – vor dem Löschen lesenswert (Löschen ist bewusst nicht gesperrt). */
  mediaUsage: (assetId: number) => http<MediaUsage[]>(`${V1}/creator/media/${assetId}/usage`),
  tagMedia: (assetId: number, tags: string[]) =>
    http<MediaAssetResponse>(`${V1}/creator/media/${assetId}/tags`, "POST", { tags }),

  // Zuordnung an der Store-Vokabel: gilt in JEDER Übung mit diesem Wort. Mehrere sind der Normalfall –
  // erst die Auswahl macht die Individualisierung je Kind möglich.
  vocabularyMedia: (vocabularyId: number) =>
    http<MediaLinkResponse[]>(`${V1}/creator/vocabulary/${vocabularyId}/media`),
  linkVocabularyMedia: (vocabularyId: number, mediaAssetId: number, weight = 0) =>
    http<MediaLinkResponse>(`${V1}/creator/vocabulary/${vocabularyId}/media`, "POST", { mediaAssetId, weight }),
  unlinkVocabularyMedia: (vocabularyId: number, linkId: number) =>
    http<void>(`${V1}/creator/vocabulary/${vocabularyId}/media/${linkId}`, "DELETE"),

  /** „Anderes Bild": lehnt die eingefrorene Wahl ab und zieht neu (409 `media_no_alternative`, wenn keine da ist). */
  reshuffleMedia: (childId: number, vocabularyId: number) =>
    http<SelectedMediaResponse>(`${V1}/student/children/${childId}/media-picks/reshuffle`, "POST", { vocabularyId }),

  /**
   * „Anderes Bild" aus Sicht einer Übungskarte. Bewusst über die Karte adressiert: ob die Wahl an der
   * Vokabel oder an einer übungslokalen Übersteuerung hängt, weiß nur der Server.
   */
  reshuffleCardImage: (planId: number, positionId: number, sessionId: number, itemIndex: number) =>
    http<SelectedMediaResponse>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/cards/${itemIndex}/image/reshuffle`,
      "POST", {}),

  // ---- Anmerkungen beim Testen ----
  // Bewusst tier-neutral (kein creator/supervisor/student-Präfix): Dieselbe Ressource wird aus dem
  // Vater-Web und aus der Sohn-Arcade bedient – die Trennung macht der Server über die Sichtbarkeit.

  createRemark: (dto: CreateRemarkDto) => http<Remark>(`${V1}/remarks`, "POST", dto),
  /** Die eigenen letzten Anmerkungen – `mine` blendet die des Kindes aus, die im Widget nur stören. */
  myRemarks: (take = 10) => httpPaged<Remark>(`${V1}/remarks?mine=true&take=${take}`),
  updateRemark: (id: number, dto: UpdateRemarkDto) => http<Remark>(`${V1}/remarks/${id}`, "PATCH", dto),
  deleteRemark: (id: number) => http<void>(`${V1}/remarks/${id}`, "DELETE"),

  /**
   * Anmerkungen für die Verwaltungsseite. `scope: "all"` hebt die Konten-Grenze auf (God-Mode). Das Gate
   * dafür ist der Server-Schalter `Remarks:GlobalRead` (Vorgabe: nur in der Entwicklung), zusätzlich
   * erlaubt ist die Admin-Rolle; ein Student ist immer ausgeschlossen. Fehlt die Berechtigung, antwortet
   * der Server mit `403 remark_scope_forbidden`.
   */
  remarks: (q: {
    status?: RemarkStatus; category?: RemarkCategory; appArea?: string;
    scope?: "all"; sort?: string; dir?: SortDir; skip?: number; take?: number;
  } = {}) => {
    const p = new URLSearchParams();
    if (q.status) p.set("status", q.status);
    if (q.category) p.set("category", q.category);
    if (q.appArea) p.set("appArea", q.appArea);
    if (q.scope) p.set("scope", q.scope);
    if (q.sort) p.set("sort", q.sort);
    if (q.dir) p.set("dir", q.dir);
    p.set("skip", String(q.skip ?? 0));
    p.set("take", String(q.take ?? 20));
    return httpPaged<Remark>(`${V1}/remarks?${p}`);
  },
  /** Der Verlauf einer Anmerkung, älteste zuerst. */
  remarkComments: (id: number) => http<RemarkComment[]>(`${V1}/remarks/${id}/comments`),
  addRemarkComment: (id: number, dto: CreateRemarkCommentDto) =>
    http<RemarkComment>(`${V1}/remarks/${id}/comments`, "POST", dto),
  deleteRemarkComment: (id: number, commentId: number) =>
    http<void>(`${V1}/remarks/${id}/comments/${commentId}`, "DELETE"),
};
