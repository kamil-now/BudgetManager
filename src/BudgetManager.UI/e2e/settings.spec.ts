import { test, expect, Locator, Page } from '@playwright/test';
import { api, openNew, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

function weekday(): string {
  return new Intl.DateTimeFormat('en-GB', { weekday: 'short' }).format(new Date());
}

test.describe('settings', () => {
  let page: Page;
  const nav = (): Locator => page.locator('nav.nav');
  const saveButton = (name = 'Save settings'): Locator =>
    page.getByRole('button', { name, exact: true });

  async function reloadFromServer(): Promise<void> {
    await page.evaluate(() => localStorage.removeItem('budget-manager.preferences'));
    await page.reload();
  }

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    await registerAndCreateLedger(page);

    const ledgerId = await page.evaluate(() => localStorage.getItem('budget-manager.ledgerId'));
    const ledger = await api<{ accounts: { id: string; name: string }[] }>(
      page,
      'GET',
      `/api/ledgers/${ledgerId}`,
    );
    const main = ledger.accounts.find((a) => a.name === 'Main Account')?.id ?? '';
    await api(page, 'POST', `/api/accounts/${main}/transaction`, {
      accountId: main,
      value: { amount: -12, currency: 'EUR' },
      date: `${today()}T00:00:00.000Z`,
      title: 'Coffee Settings E2E',
    });
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('opens from the sidebar', async () => {
    await page.goto('/dashboard');
    await nav().getByRole('link', { name: 'Settings', exact: true })
      .click();
    await expect(page).toHaveURL(/\/settings/);
    await expect(page.getByRole('heading', { level: 1, name: 'Settings' })).toBeVisible();
    await expect(page.getByLabel('Date format', { exact: true })).toHaveValue('yyyy-MM-dd');
    await expect(page.getByLabel('Language', { exact: true })).toHaveValue('en');
    await expect(page.getByRole('heading', { name: 'Delete account', exact: true })).toBeVisible();
  });

  test('shows the day headers in the chosen date format, also after a reload', async () => {
    await page.goto('/dashboard');
    const transactions = page.locator('section[aria-labelledby="h-transactions"]');
    const [y, m, d] = today().split('-');
    await expect(
      transactions.getByRole('rowheader', { name: `${weekday()}, ${y}-${m}-${d}`, exact: true }),
    ).toBeVisible();

    await page.goto('/settings');
    await page.getByLabel('Date format', { exact: true }).selectOption('dd.MM.yyyy');
    await saveButton().click();
    await expect(page.getByRole('status').filter({ hasText: 'Settings saved.' })).toBeVisible();

    await reloadFromServer();
    await expect(page.getByLabel('Date format', { exact: true })).toHaveValue('dd.MM.yyyy');

    await page.goto('/dashboard');
    await expect(
      transactions.getByRole('rowheader', { name: `${weekday()}, ${d}.${m}.${y}`, exact: true }),
    ).toBeVisible();
  });

  test('lists the favorite currencies first in a currency picker', async () => {
    await page.goto('/settings');
    await page.getByRole('button', { name: 'Remove USD', exact: true }).click();
    await page.getByLabel('Favorite to add', { exact: true }).selectOption('GBP');
    await page.getByRole('button', { name: 'Add favorite', exact: true }).click();
    await saveButton().click();
    await expect(page.getByRole('status').filter({ hasText: 'Settings saved.' })).toBeVisible();

    await reloadFromServer();
    await expect(page.getByRole('button', { name: /^Remove [A-Z]{3}$/ })).toHaveCount(3);
    await expect(page.getByRole('button', { name: 'Remove GBP', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Remove USD', exact: true })).toHaveCount(0);

    await page.goto('/dashboard');
    await openNew(page, 'Expense');
    const dialog = page.getByRole('dialog');
    const currency = dialog.getByLabel('Currency', { exact: true });
    await expect(currency).toHaveValue('EUR');
    await expect(currency.locator('optgroup[label="Favorites"] option')).toHaveText(['EUR', 'PLN', 'GBP']);
    const others = currency.locator('optgroup[label="All currencies"] option');
    await expect(others.first()).not.toHaveText('EUR');
    await expect(currency.locator('optgroup[label="All currencies"] option[value="USD"]')).toHaveCount(1);
    await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
    await expect(dialog).toBeHidden();
  });

  test('switches the language to Polish, also after a reload', async () => {
    await page.goto('/settings');
    await page.getByLabel('Language', { exact: true }).selectOption('pl');
    await saveButton().click();

    await expect(page.getByRole('status').filter({ hasText: 'Ustawienia zostały zapisane.' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 1, name: 'Ustawienia' })).toBeVisible();
    await expect(nav().getByRole('link', { name: 'Pulpit', exact: true })).toBeVisible();
    await expect(page.locator('html')).toHaveAttribute('lang', 'pl');

    await reloadFromServer();
    await expect(page.getByLabel('Język', { exact: true })).toHaveValue('pl');
    await expect(page.getByLabel('Format daty', { exact: true })).toHaveValue('dd.MM.yyyy');

    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { level: 2, name: 'Transakcje' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Nowy', exact: true })).toBeVisible();

    await page.goto('/settings');
    await page.getByLabel('Język', { exact: true }).selectOption('en');
    await saveButton('Zapisz ustawienia').click();
    await expect(page.getByRole('heading', { level: 1, name: 'Settings' })).toBeVisible();
  });
});
