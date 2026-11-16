import { Locator, Page, expect } from '@playwright/test';

/** Meets the API's minimum password length (Constants.MinPasswordLength = 8). */
export const PASSWORD = 'Password123!';

export function uniqueEmail(prefix = 'e2e'): string {
  return `${prefix}+${Date.now()}_${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

export async function registerUser(page: Page, email = uniqueEmail()): Promise<string> {
  await page.goto('/register');
  await page.getByLabel('Name', { exact: true }).fill('E2E Tester');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(PASSWORD);
  await page.getByLabel('Confirm password', { exact: true }).fill(PASSWORD);
  await page.getByRole('button', { name: 'Continue' }).click();

  // Step 2 suggests the preferences from the browser locale (en-US: USD, MM/dd/yyyy); the specs
  // expect EUR amounts and ISO dates, so pick those.
  await expect(page.getByRole('heading', { name: 'Your preferences' })).toBeVisible();
  await page.getByLabel('Default currency', { exact: true }).selectOption('EUR');
  await page.getByLabel('Date format', { exact: true }).selectOption('yyyy-MM-dd');
  // The auth endpoints allow 10 requests per 60s per IP and the specs run back to back: when the
  // limiter answers, wait for the window to pass and submit again.
  for (let attempt = 0; attempt < 8; attempt++) {
    await page.getByRole('button', { name: 'Create account' }).click();
    const limited = page.getByText('Too many attempts');
    const outcome = await Promise.race([
      page.waitForURL('**/new-ledger', { timeout: 20_000 }).then(() => 'done'),
      limited.waitFor({ timeout: 20_000 }).then(() => 'limited'),
      page.getByText('signing in failed').waitFor({ timeout: 20_000 }).then(() => 'signin'),
    ]);
    if (outcome === 'done') return email;
    await page.waitForTimeout(10_000);
    if (outcome === 'signin') {
      for (let signIn = 0; signIn < 6; signIn++) {
        await page.goto('/');
        if (page.url().endsWith('/new-ledger')) return email;
        await page.getByLabel('Email', { exact: true }).fill(email);
        await page.getByLabel('Password', { exact: true }).fill(PASSWORD);
        await page.getByRole('button', { name: 'Sign in' }).click();
        try {
          await page.waitForURL('**/new-ledger', { timeout: 15_000 });
          return email;
        } catch {
          await page.waitForTimeout(10_000);
        }
      }
      throw new Error('could not sign in after registering');
    }
  }
  throw new Error('registration kept hitting the auth rate limit');
}

export async function openNew(page: Page, item: string): Promise<void> {
  await page.getByRole('button', { name: 'New', exact: true }).click();
  await page.getByRole('menuitem', { name: item, exact: true }).click();
}

export function ledgerNav(page: Page): Locator {
  return page.getByRole('navigation', { name: 'Ledgers' });
}

export interface CreateLedgerOptions {
  secondBudget?: string;
  name?: string;
  zeroBalances?: boolean;
}

export async function createLedger(page: Page, opts: CreateLedgerOptions = {}): Promise<void> {
  await page.goto('/new-ledger');
  await expect(
    page.getByRole('heading', { name: /^Create (your|a new) ledger$/ }),
  ).toBeVisible();
  if (opts.name) await page.getByLabel('Name', { exact: true }).fill(opts.name);
  await page.getByRole('button', { name: 'Continue' }).click();

  await expect(page.getByRole('heading', { name: 'Set up your budgets' })).toBeVisible();
  if (opts.secondBudget) {
    await page.getByRole('button', { name: 'Add budget' }).click();
    const card = page.locator('.budget-card').nth(1);
    await card.locator('#b-name-1').fill(opts.secondBudget);
    await card.getByLabel('Fund name').first().fill('Emergency');
  }
  await page.getByRole('button', { name: 'Continue' }).click();

  await expect(page.getByRole('heading', { name: 'Add your accounts' })).toBeVisible();
  if (opts.zeroBalances) {
    const balances = page.locator('input[id^="a-bal-"]');
    const count = await balances.count();
    for (let i = 0; i < count; i++) await balances.nth(i).fill('0');
  }
  await page.getByRole('button', { name: 'Continue' }).click();

  await expect(page.getByRole('heading', { name: 'Allocate your balances' })).toBeVisible();
  await page.getByRole('button', { name: 'Create ledger' }).click();
  await page.waitForURL('**/dashboard');
  await expect(ledgerNav(page).locator('[aria-current="true"]')).toContainText(
    opts.name ?? 'Default Ledger',
  );
}

export async function registerAndCreateLedger(
  page: Page,
  opts: CreateLedgerOptions = {},
): Promise<string> {
  const email = await registerUser(page);
  await createLedger(page, opts);
  return email;
}

export async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => {
    const doc = document.documentElement;
    return { scroll: doc.scrollWidth, client: doc.clientWidth, body: document.body.scrollWidth };
  });
  expect(overflow.scroll, `document ${overflow.scroll}px wider than ${overflow.client}px`).toBeLessThanOrEqual(
    overflow.client,
  );
  expect(overflow.body).toBeLessThanOrEqual(overflow.client);
}

export async function expectNothingPastTheEdge(page: Page, root: Locator): Promise<void> {
  const offenders = await root.evaluate((el) => {
    const edge = window.innerWidth + 1;
    return Array.from(el.querySelectorAll('*'))
      .filter((child) => {
        const box = child.getBoundingClientRect();
        return box.width > 1 && box.height > 1 && box.right > edge;
      })
      .slice(0, 5)
      .map((child) => `${child.tagName.toLowerCase()}.${(child as HTMLElement).className}`);
  });
  expect(offenders, `elements past the right edge of the ${page.viewportSize()?.width}px viewport`).toEqual([]);
}

export async function api<T = unknown>(
  page: Page,
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  const result = await page.evaluate(
    async (args) => {
      const key = 'budget-manager.token';
      const token = localStorage.getItem(key) ?? sessionStorage.getItem(key);
      const response = await fetch(args.path, {
        method: args.method,
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: args.body === undefined ? undefined : JSON.stringify(args.body),
      });
      if (!response.ok) throw new Error(`${args.method} ${args.path} answered ${response.status}`);
      const text = await response.text();
      return text ? JSON.parse(text) : null;
    },
    { method, path, body },
  );
  return result as T;
}

export function day(days = 0): string {
  const now = new Date();
  const date = new Date(now.getFullYear(), now.getMonth(), now.getDate() - days);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${dd}`;
}
