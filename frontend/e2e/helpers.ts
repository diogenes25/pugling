import { expect, type Page } from "@playwright/test";

/** Login credentials for the seeded father (`fid`/`pin`) or a child (`childId`/`pin`). */
export interface LoginCredentials {
  id: string;
  pin: string;
}

/** Logs a supervisor in via id/PIN and waits for the dashboard to render. */
export async function vaterLogin(page: Page, credentials: LoginCredentials) {
  await page.goto("/vater");
  await page.locator("#fid").fill(credentials.id);
  await page.locator("#pin").fill(credentials.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Logs a child in via id + on-screen PIN pad and starts the session ("▶ LOS"). */
export async function sohnLogin(page: Page, credentials: LoginCredentials) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(credentials.id);
  for (const d of credentials.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
}
