import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api, ApiError } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ActivationRequest } from "../lib/types";
import { ChildShopView } from "./VaterShop";

/*
 * B-504: scheitert die Freigabe an zu wenig Inventar, bleibt die Anfrage offen (ShopService.ApproveActivationAsync). Der
 * Banner steht über der Tabelle – ohne Artikel hätte „Davon" kein Bezugswort.
 */
const request = {
  id: 7, articleTitle: "Bildschirmzeit", requestedQuantity: 30, unitType: "Minute", status: "Pending",
  requestedAt: "2026-11-12T10:00:00Z", canApprove: true, canReject: true,
} as unknown as ActivationRequest;

describe("Vater-Shop – Freigabe scheitert am Inventar", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 4. Rot, wenn der geteilte Satz durchkommt (kein Artikel, keine Menge, keine Folge) oder Inventar bzw. Anfragen
  // danach nicht neu kommen.
  it("nennt Artikel, offene Anfrage und Ausweg und lädt Inventar und Anfragen neu", async () => {
    const activations = vi.spyOn(api, "childActivations").mockResolvedValue([request]);
    vi.spyOn(api, "childPurchases").mockResolvedValue({ items: [], total: 0 });
    const inventory = vi.spyOn(api, "childInventory").mockResolvedValue([]);
    vi.spyOn(api, "approveActivation").mockRejectedValue(
      new ApiError(400, "Not enough units.", "trace-1", "insufficient_inventory"));
    render(<AuthProvider><ChildShopView childId={1} /></AuthProvider>);

    fireEvent.click(await screen.findByRole("button", { name: "Freigeben" }));

    const text = (await screen.findByText(/lässt sich nicht freigeben/)).textContent ?? "";
    expect(text).toMatch(/„Bildschirmzeit"/);
    expect(text).toMatch(/weniger als 30 Min/);
    expect(text).toMatch(/bleibt offen/);
    expect(text).toMatch(/ablehnen/);
    await waitFor(() => expect(inventory).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(activations).toHaveBeenCalledTimes(2));
  });

  // Rot, wenn die Übersetzung auch beim Ablehnen greift: dort entsteht der Code nie, ein anderer Fehler muss bleiben.
  it("übersetzt beim Ablehnen nichts", async () => {
    vi.spyOn(api, "childActivations").mockResolvedValue([request]);
    vi.spyOn(api, "childPurchases").mockResolvedValue({ items: [], total: 0 });
    vi.spyOn(api, "childInventory").mockResolvedValue([]);
    vi.spyOn(api, "rejectActivation").mockRejectedValue(
      new ApiError(400, "Not enough units.", "trace-2", "insufficient_inventory"));
    render(<AuthProvider><ChildShopView childId={1} /></AuthProvider>);

    fireEvent.click(await screen.findByRole("button", { name: "Ablehnen" }));

    expect(await screen.findByText("Von diesem Artikel ist nicht mehr genug im Inventar.")).toBeTruthy();
    expect(screen.queryByText(/lässt sich nicht freigeben/)).toBeNull();
  });

  // Gegenprobe: ein anderer Fehler behält seinen Satz – die Übersetzung gilt nur für `insufficient_inventory`. Ein
  // `ApiError` mit anderem Code, sonst trüge die `instanceof`-Prüfung den Fall allein. Rot ohne die Prüfung des Codes.
  it("lässt andere Fehler der Freigabe unverändert", async () => {
    vi.spyOn(api, "childActivations").mockResolvedValue([request]);
    vi.spyOn(api, "childPurchases").mockResolvedValue({ items: [], total: 0 });
    vi.spyOn(api, "childInventory").mockResolvedValue([]);
    vi.spyOn(api, "approveActivation").mockRejectedValue(
      new ApiError(409, "This activation request is not pending.", undefined, "activation_not_pending"));
    render(<AuthProvider><ChildShopView childId={1} /></AuthProvider>);

    fireEvent.click(await screen.findByRole("button", { name: "Freigeben" }));

    expect(await screen.findByText("Diese Anfrage ist schon beantwortet.")).toBeTruthy();
    expect(screen.queryByText(/lässt sich nicht freigeben/)).toBeNull();
  });
});
