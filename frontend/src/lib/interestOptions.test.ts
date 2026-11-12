import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "./api";

/*
 * B-207, Entscheidung 5: Die Auswahlliste des Kindes lädt seitenweise bis `X-Total-Count`, statt einen Deckel zu raten.
 * Gefahren wird der echte API-Client gegen ein nachgebautes `fetch` (Vorbild `remarks.test.ts`) – die Schleife ist die
 * Logik, die die E2E mit ihren wenigen Schlagworten nie über eine Seite hinaus treibt.
 */

const originalFetch = globalThis.fetch;
afterEach(() => {
  globalThis.fetch = originalFetch;
});

function tags(from: number, count: number) {
  return Array.from({ length: count }, (_, i) => ({ id: from + i, slug: `t${from + i}`, label: `T${from + i}`, facet: "Other" }));
}

/** Liefert je Aufruf die nächste Seite; `total` steht im Header, wenn gesetzt. */
function stubPages(pages: unknown[][], total: number | null) {
  const urls: string[] = [];
  globalThis.fetch = vi.fn(async (url: RequestInfo | URL) => {
    urls.push(String(url));
    const body = pages[urls.length - 1] ?? [];
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (total != null) headers["X-Total-Count"] = String(total);
    return new Response(JSON.stringify(body), { status: 200, headers });
  }) as unknown as typeof fetch;
  return urls;
}

describe("interestOptions", () => {
  // Rot, wenn die Schleife nach der ersten Seite aufhört (die 501. Kachel fehlte lautlos) oder `skip` nicht mitzählt.
  it("lädt über die erste Seite hinaus, bis die Gesamtzahl erreicht ist", async () => {
    const urls = stubPages([tags(1, 500), tags(501, 1)], 501);

    const all = await api.interestOptions();

    expect(all).toHaveLength(501);
    expect(urls).toHaveLength(2);
    expect(urls[0]).toContain("skip=0");
    expect(urls[1]).toContain("skip=500");
  });

  // Rot, wenn ein fehlender Header die Schleife endlos weiterfragen ließe: ohne Gesamtzahl ist die erste Seite alles.
  it("endet ohne Gesamtzahl nach einer Seite", async () => {
    const urls = stubPages([tags(1, 3)], null);

    expect(await api.interestOptions()).toHaveLength(3);
    expect(urls).toHaveLength(1);
  });
});
