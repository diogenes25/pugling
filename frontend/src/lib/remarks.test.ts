/**
 * Der Fehler-Ringpuffer – und vor allem seine **Sicherheitsregel**: Es dürfen ausschließlich Metadaten
 * hineingelangen. Der Login-Request trägt die PIN im Body; käme sie in den Puffer, stünde sie im Klartext
 * in der Datenbank und würde über den Markdown-Export ins Repo getragen.
 *
 * Der wichtigste Test unten fährt darum den **echten** API-Client gegen einen abweisenden `fetch` und
 * durchsucht den Puffer nach der PIN – er prüft die Regel dort, wo sie gebrochen werden könnte, statt
 * nur die Puffer-Funktionen für sich zu betrachten.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  MAX_MESSAGE,
  RING_SIZE,
  clearRecentErrors,
  recentErrors,
  recentErrorsJson,
  recordHttpError,
  recordJsError,
  recordNetworkError,
} from "./remarks";

beforeEach(() => clearRecentErrors());

describe("Ringpuffer", () => {
  it("hält nur die letzten RING_SIZE Einträge", () => {
    for (let i = 0; i < RING_SIZE + 5; i++) recordHttpError("GET", `/api/v1/x/${i}`, 500);

    const entries = recentErrors();
    expect(entries).toHaveLength(RING_SIZE);
    // Die ältesten fallen hinten raus – der Puffer soll „was war gerade" zeigen, nicht die Historie.
    expect(entries[0].path).toBe("/api/v1/x/5");
    expect(entries[entries.length - 1].path).toBe(`/api/v1/x/${RING_SIZE + 4}`);
  });

  it("ist leer serialisiert null statt eines leeren Arrays", () => {
    expect(recentErrorsJson()).toBeNull();
    recordJsError("kaputt");
    expect(recentErrorsJson()).not.toBeNull();
  });

  it("gibt eine Kopie heraus, die den Puffer nicht verändert", () => {
    recordHttpError("GET", "/api/v1/a", 404, "not_found");
    recentErrors().push({ kind: "js", message: "eingeschmuggelt", at: "" });
    expect(recentErrors()).toHaveLength(1);
  });

  it("kürzt lange Meldungen", () => {
    recordJsError("x".repeat(MAX_MESSAGE + 100));
    const [entry] = recentErrors();
    expect(entry.message!.length).toBeLessThanOrEqual(MAX_MESSAGE + 1); // +1 für das Auslassungszeichen
  });
});

describe("Sicherheitsregel: keine Inhalte im Puffer", () => {
  it("wirft die Query weg – sie trägt IDs, Filter und Suchbegriffe", () => {
    recordHttpError("GET", "/api/v1/creator/vocabulary?word=geheim&childId=3", 500);
    const [entry] = recentErrors();

    expect(entry.path).toBe("/api/v1/creator/vocabulary");
    expect(JSON.stringify(entry)).not.toContain("geheim");
  });

  it("wirft die Query auch bei unparsbarer URL weg", () => {
    recordNetworkError("GET", "::kaputt::?pin=1234", "Failed to fetch");
    expect(JSON.stringify(recentErrors())).not.toContain("1234");
  });

  it("hält bei HTTP-Fehlern nur Metadaten fest", () => {
    recordHttpError("PATCH", "/api/v1/remarks/7", 403, "forbidden");
    const [entry] = recentErrors();

    expect(entry).toEqual({
      kind: "http",
      method: "PATCH",
      path: "/api/v1/remarks/7",
      status: 403,
      code: "forbidden",
      at: expect.any(String),
    });
    // Kein Feld für Bodies oder Header – strukturell, nicht nur per Konvention.
    expect(Object.keys(entry).sort()).toEqual(["at", "code", "kind", "method", "path", "status"]);
  });
});

describe("Sicherheitsregel am echten API-Client", () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("schreibt die PIN eines fehlgeschlagenen Logins NICHT in den Puffer", async () => {
    const pin = "9137";

    // Der Server weist ab – genau der Pfad, auf dem der Puffer greift.
    globalThis.fetch = vi.fn(async () =>
      new Response(JSON.stringify({ detail: "Invalid adult ID or PIN.", code: "invalid_credentials" }), {
        status: 401,
        headers: { "content-type": "application/problem+json" },
      }),
    ) as unknown as typeof fetch;

    const { api } = await import("./api");
    await expect(api.loginAdult(1, pin)).rejects.toThrow();

    const dump = recentErrorsJson()!;
    // Der Puffer hat den Fehlschlag festgehalten …
    expect(dump).toContain("invalid_credentials");
    expect(dump).toContain("/api/v1/auth/adult");
    // … aber weder die PIN noch den Antworttext.
    expect(dump).not.toContain(pin);
    expect(dump).not.toContain("Invalid adult ID or PIN");

    const [entry] = recentErrors();
    expect(Object.keys(entry).sort()).toEqual(["at", "code", "kind", "method", "path", "status"]);
  });

  it("schreibt bei einem Netzwerkfehler weder Body noch Token in den Puffer", async () => {
    localStorage.setItem("pugling.token", "geheimes-jwt");
    globalThis.fetch = vi.fn(async () => {
      throw new TypeError("Failed to fetch");
    }) as unknown as typeof fetch;

    const { api } = await import("./api");
    await expect(api.loginAdult(1, "4242")).rejects.toThrow();

    const dump = recentErrorsJson()!;
    expect(dump).toContain("Failed to fetch");
    expect(dump).not.toContain("4242");
    expect(dump).not.toContain("geheimes-jwt");
  });
});
