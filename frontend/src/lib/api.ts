import type {
  AchievementDef, UpdateAchievementDto, AchievementStatus, AnswerDto, CategoryResponse, ChapterResponse, ChildResponse, CreateAchievementDto,
  CreateChildDto, CreateExercisePayload, CreateKlassenarbeitDto, CreateMissionDto, CreatePlanDto, CreateVocabularyDto,
  UpdateChildDto, SupervisorLink, SupervisorRelation, TimetableEntry, CreateTimetableEntryDto,
  ClozeResponse, CreateClozeDto, UpdateClozeDto,
  ExerciseGrant, GrantPermission,
  CreateAdultDto, AdultResponse, UpdateAdultDto,
  ExerciseDetail, ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult, ExerciseTypeManifest,
  UpdateChapterDto, CreateChildTagDto, ExerciseWriteResult,
  ExerciseSearchParams, ExerciseSharing, ExerciseSummary, MeResponse, TeacherAccount, CreateTeacherDto, UpdateMyAccountDto, ExerciseUsage, KlassenarbeitDetail, KlassenarbeitPractice, KlassenarbeitRepeat,
  KlassenarbeitResponse, KlassenarbeitStatus, LoginResponse, MissionDef, UpdateMissionDto, MissionStatus, PlanResponse,
  ChildrenDashboard, CreatePositionDto, PositionResponse, PositionReport, UpdatePositionDto, OverviewResponse, PositionSession, PracticeCard,
  ProgressResponse, ReviewInput, ReviewOutcome,
  SkinState, SubjectResponse,
  TestAttemptResponse, TestNextResponse, TestAnswerAck, TestSubmitResponse, UpdateKlassenarbeitDto, UpdatePlanDto, UpdateVocabularyDto,
  VocabBatchResult, VocabularyResponse, VocabTagResponse, ChildTagResponse, Wallet, WalletBalance, WalletEntry, ChildPointsEntry, Currency,
  Paged, VocabularySearchParams, VocabItemInput, VocabItemResponse,
  ChapterProgress, ExerciseProgress, ItemHistoryEntry, ItemProgressResponse, SubjectProgress, WordMastery,
  CreateKeyResultRequest, CreateObjectiveRequest, GoalStatus, KeyResult,
  Objective, ObjectiveKind, UpdateKeyResultRequest, UpdateObjectiveRequest,
  ShopArticle, CreateShopArticleDto, UpdateShopArticleDto, ShopListing, CreateShopListingDto, UpdateShopListingDto,
  InventoryItem, MyInventoryItem, ShopPurchase, ActivationRequest, ShopPurchaseStatus, ActivationRequestStatus,
  ShopView, MyActivation,
  ContentRating, InterestTagResponse, CreateInterestTagDto, UpdateInterestTagDto,
  ChildInterestResponse, ChildInterestInput,
  MediaAssetResponse, CreateMediaAssetDto, MediaLinkResponse, MediaUsage, SelectedMediaResponse,
  TextbookSeriesResponse, CreateTextbookSeriesDto, UpdateTextbookSeriesDto,
  SeriesUnitResponse, CreateSeriesUnitDto, UpdateSeriesUnitDto,
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
 * `code` ist stabiler Vertragsbestandteil, die Formulierung gehört der Oberfläche. Hier stehen nur Codes,
 * die einen **Nutzer** treffen und ihm sagen, was zu tun ist; technische Fälle behalten ihre Rohmeldung.
 */
const GERMAN_PROBLEM_TEXT: Record<string, string> = {
  exercise_empty:
    "Diese Übung hat noch keine Inhalte. Füge erst Wörter hinzu – danach lässt sie sich durchspielen und zuweisen.",
  no_checkable_content: "Diese Übung hat keine einzeln prüfbaren Aufgaben.",
  no_tag_matches: "Zu diesen Tags gibt es keine Vokabeln. Die Übung wurde nicht verändert.",
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

  // ---- Vater: Katalog (Fächer, Kapitel, Übungssuche über Metadaten) ----
  subjects: () => http<SubjectResponse[]>(`${V1}/creator/subjects`),
  createSubject: (name: string) => http<SubjectResponse>(`${V1}/creator/subjects`, "POST", { name }),
  updateSubject: (subjectId: number, name: string) =>
    http<SubjectResponse>(`${V1}/creator/subjects/${subjectId}`, "PATCH", { name }),
  /** Löscht das Fach **samt Kapiteln und Übungen** – scheitert, solange eine Übung in einem Plan steckt. */
  deleteSubject: (subjectId: number) => http<void>(`${V1}/creator/subjects/${subjectId}`, "DELETE"),
  updateChapter: (subjectId: number, chapterId: number, dto: UpdateChapterDto) =>
    http<ChapterResponse>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}`, "PATCH", dto),
  deleteChapter: (subjectId: number, chapterId: number) =>
    http<void>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}`, "DELETE"),

  // „Arten" (Kategorien) sind fachabhängig und dienen der Vorfilterung im Katalog.
  createCategory: (subjectId: number, name: string) =>
    http<CategoryResponse>(`${V1}/creator/subjects/${subjectId}/categories`, "POST", { name }),
  updateCategory: (subjectId: number, categoryId: number, name: string) =>
    http<CategoryResponse>(`${V1}/creator/subjects/${subjectId}/categories/${categoryId}`, "PATCH", { name }),
  deleteCategory: (subjectId: number, categoryId: number) =>
    http<void>(`${V1}/creator/subjects/${subjectId}/categories/${categoryId}`, "DELETE"),
  chapters: (subjectId: number) =>
    http<ChapterResponse[]>(`${V1}/creator/subjects/${subjectId}/chapters`),
  createChapter: (subjectId: number, name: string, orderIndex: number) =>
    http<ChapterResponse>(`${V1}/creator/subjects/${subjectId}/chapters`, "POST", { name, orderIndex }),
  // Fachabhängige Arten ("Kategorien") – zur Vorfilterung im Katalog/Planbau.
  categories: (subjectId: number) =>
    http<CategoryResponse[]>(`${V1}/creator/subjects/${subjectId}/categories`),
  // Übung eines Typs im Kapitel anlegen. Das Routen-Segment (vocabulary/arithmetic/…) bestimmt den Typ.
  createExercise: (subjectId: number, chapterId: number, typeRoute: string, payload: CreateExercisePayload) =>
    http<ExerciseWriteResult>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}`, "POST", payload),
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
  updateExercise: (subjectId: number, chapterId: number, typeRoute: string, id: number, payload: CreateExercisePayload) =>
    http<ExerciseWriteResult>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}/${id}`, "PUT", payload),
  deleteExercise: (subjectId: number, chapterId: number, typeRoute: string, id: number) =>
    http<void>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}/${id}`, "DELETE"),

  // RWX-Rechte an einer Übung: mehrere Owner möglich, Write/Execute je Creator. Lesen darf jeder – dafür
  // gibt es kein Grant. Rechte vergeben/entziehen darf nur ein Owner.
  exerciseGrants: (exerciseId: number) =>
    http<ExerciseGrant[]>(`${V1}/creator/exercises/${exerciseId}/grants`),
  addExerciseGrant: (exerciseId: number, creatorId: number, permission: GrantPermission) =>
    http<ExerciseGrant>(`${V1}/creator/exercises/${exerciseId}/grants`, "POST", { creatorId, permission }),
  removeExerciseGrant: (exerciseId: number, creatorId: number, permission: GrantPermission) =>
    http<void>(`${V1}/creator/exercises/${exerciseId}/grants/${creatorId}/${permission}`, "DELETE"),

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
  exerciseItems: (subjectId: number, chapterId: number, exerciseId: number) =>
    http<VocabItemResponse[]>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/vocabulary/${exerciseId}/items`),
  addExerciseItem: (subjectId: number, chapterId: number, exerciseId: number, body: VocabItemInput) =>
    http<VocabItemResponse>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/vocabulary/${exerciseId}/items`, "POST", body),
  patchExerciseItem: (subjectId: number, chapterId: number, exerciseId: number, itemId: number, body: VocabItemInput) =>
    http<VocabItemResponse>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/vocabulary/${exerciseId}/items/${itemId}`, "PATCH", body),
  deleteExerciseItem: (subjectId: number, chapterId: number, exerciseId: number, itemId: number) =>
    http<void>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/vocabulary/${exerciseId}/items/${itemId}`, "DELETE"),
  searchExercises: (p: ExerciseSearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.chapterId != null) q.set("chapterId", String(p.chapterId));
    if (p.grade != null) q.set("grade", String(p.grade));
    if (p.schoolType && p.schoolType !== "None") q.set("schoolType", p.schoolType);
    if (p.categoryId != null) q.set("categoryId", String(p.categoryId));
    if (p.type) q.set("type", p.type);
    if (p.search) q.set("search", p.search);
    if (p.mineOnly) q.set("mineOnly", "true");
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
  // Lehrplan samt Positionen/Fortschritt löschen (Kaskade); die Katalog-Übungen bleiben erhalten.
  deletePlan: (planId: number) => http<void>(`${V1}/supervisor/study-plans/${planId}`, "DELETE"),

  // ---- Lehrplan-Positionen (Plan = Container aus Katalog-Übungen) ----
  positions: (planId: number) =>
    http<PositionResponse[]>(`${V1}/supervisor/study-plans/${planId}/positions`),
  addPosition: (planId: number, dto: CreatePositionDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions`, "POST", dto),
  updatePosition: (planId: number, positionId: number, dto: UpdatePositionDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}`, "PATCH", dto),
  deletePosition: (planId: number, positionId: number) =>
    http<void>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}`, "DELETE"),
  // Lern-Report der Position: je Inhalt Box/Beherrschung + Test-Trefferquote („sitzt/sitzt nicht").
  positionReport: (planId: number, positionId: number) =>
    http<PositionReport>(`${V1}/student/study-plans/${planId}/positions/${positionId}/report`),

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

  // Katalog-hierarchischer Drilldown (Fach → Kapitel → Übung → Item), abgeleitet aus den Lehrplänen.
  childLearnSubjects: (childId: number) =>
    http<SubjectProgress[]>(`${V1}/student/children/${childId}/learn/subjects`),
  childLearnChapters: (childId: number, subjectId: number) =>
    http<ChapterProgress[]>(`${V1}/student/children/${childId}/learn/subjects/${subjectId}/chapters`),
  childLearnExercises: (childId: number, subjectId: number, chapterId: number) =>
    http<ExerciseProgress[]>(`${V1}/student/children/${childId}/learn/subjects/${subjectId}/chapters/${chapterId}/vocabulary`),
  childLearnItems: (childId: number, subjectId: number, chapterId: number, exerciseId: number) =>
    http<ItemProgressResponse[]>(
      `${V1}/student/children/${childId}/learn/subjects/${subjectId}/chapters/${chapterId}/vocabulary/${exerciseId}/items`),

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

  // ---- Sohn: Wallet ----
  // Kontostand (Salden) und Buchungsverlauf sind getrennt: Salden als Einzelwerte, Buchungen server-paginiert.
  wallet: () => http<WalletBalance>(`${V1}/student/me/points`),
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

  // Kindbezogene Sicht: Inventar, Käufe (stornierbar) und Aktivierungsanfragen (genehmigen/ablehnen).
  childInventory: (childId: number) =>
    http<InventoryItem[]>(`${V1}/supervisor/children/${childId}/shop/inventory`),
  childPurchases: (childId: number, status?: ShopPurchaseStatus) => {
    const q = new URLSearchParams();
    if (status) q.set("status", status);
    const qs = q.toString();
    return http<ShopPurchase[]>(`${V1}/supervisor/children/${childId}/shop/purchases${qs ? `?${qs}` : ""}`);
  },
  cancelPurchase: (childId: number, purchaseId: number) =>
    http<ShopPurchase>(`${V1}/supervisor/children/${childId}/shop/purchases/${purchaseId}/cancel`, "POST", {}),
  childActivations: (childId: number, status?: ActivationRequestStatus) => {
    const q = new URLSearchParams();
    if (status) q.set("status", status);
    const qs = q.toString();
    return http<ActivationRequest[]>(`${V1}/supervisor/children/${childId}/shop/activations${qs ? `?${qs}` : ""}`);
  },
  approveActivation: (childId: number, requestId: number) =>
    http<ActivationRequest>(`${V1}/supervisor/children/${childId}/shop/activations/${requestId}/approve`, "POST", {}),
  rejectActivation: (childId: number, requestId: number) =>
    http<ActivationRequest>(`${V1}/supervisor/children/${childId}/shop/activations/${requestId}/reject`, "POST", {}),

  // ---- Unterrichtsmaterial: Lehrwerk-Reihen und ihre Units ----
  // Der Katalog ist geteilt: lesen darf jeder Creator, ändern nur der Owner (`isOwn` sagt es der UI).
  // Anlegen ist über den Slug idempotent – derselbe Name liefert die bestehende Reihe zurück.
  textbookSeries: (p: { search?: string; subjectId?: number; mineOnly?: boolean } = {}) => {
    const q = new URLSearchParams({ take: "200" });
    if (p.search) q.set("search", p.search);
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
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
  /** Löscht ein Schlagwort samt Verknüpfungen – bewusst ohne Sperre: es trägt keine Inhalte. */
  deleteInterestTag: (id: number) => http<void>(`${V1}/creator/interest-tags/${id}`, "DELETE"),

  childInterests: (childId: number) =>
    http<ChildInterestResponse[]>(`${V1}/supervisor/children/${childId}/interests`),
  /** Ersetzt die Menge vollständig – das UI bearbeitet sie als Ganzes. */
  setChildInterests: (childId: number, interests: ChildInterestInput[]) =>
    http<ChildInterestResponse[]>(`${V1}/supervisor/children/${childId}/interests`, "PUT", { interests }),

  media: (p: { search?: string; tag?: string[]; maxRating?: ContentRating; take?: number } = {}) => {
    const q = new URLSearchParams();
    if (p.search) q.set("search", p.search);
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
