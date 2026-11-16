import { test, expect, Locator, Page } from '@playwright/test';
import { api, day, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

interface LedgerShape {
  accounts: { id: string; name: string }[];
  budgets: { id: string; name: string }[];
}

interface TemplateShape {
  id: string;
  name: string;
}

function dayHeader(days = 0): string {
  const [y, m, d] = day(days).split('-').map(Number);
  const weekday = new Intl.DateTimeFormat('en-US', { weekday: 'short' }).format(new Date(y, m - 1, d));
  return `${weekday}, ${day(days)}`;
}

test.describe('transactions list', () => {
  let page: Page;
  let transactions: Locator;
  let dialog: Locator;
  const BULK = 60;

  const editButton = (title: string): Locator =>
    transactions.getByRole('button', { name: `Edit transaction: ${title}`, exact: true });
  const rowOf = (title: string): Locator =>
    transactions
      .getByRole('row')
      .filter({ has: page.getByRole('button', { name: `Edit transaction: ${title}`, exact: true }) });
  const allocationRows = (income: string): Locator =>
    transactions.getByRole('row', { name: `from income: ${income}` });
  const amountOf = (title: string): Locator => rowOf(title).locator('.amt .amount');

  async function pick(title: string, action: string): Promise<void> {
    await transactions.getByRole('button', { name: `Actions for ${title}`, exact: true }).click();
    await page.getByRole('menuitem', { name: action, exact: true }).click();
  }

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    transactions = page.locator('section[aria-labelledby="h-transactions"]');
    dialog = page.getByRole('dialog');
    await registerAndCreateLedger(page);

    const ledgerId = await page.evaluate(() => localStorage.getItem('budget-manager.ledgerId'));
    const ledger = await api<LedgerShape>(page, 'GET', `/api/ledgers/${ledgerId}`);
    const account = (name: string) => ledger.accounts.find((a) => a.name === name)?.id ?? '';
    const main = account('Main Account');
    const cash = account('Cash');
    const budget = ledger.budgets.find((b) => b.name === 'Main budget')?.id ?? '';
    const templates = await api<TemplateShape[]>(page, 'GET', `/api/budgets/${budget}/allocationTemplates`);
    const tx = (accountId: string, amount: number, title: string, daysAgo: number, extra = {}) =>
      api(page, 'POST', `/api/accounts/${accountId}/transaction`, {
        accountId,
        value: { amount, currency: 'EUR' },
        date: `${day(daysAgo)}T00:00:00.000Z`,
        title,
        ...extra,
      });

    for (let i = 1; i <= BULK; i++) {
      await tx(main, -1, `Bulk ${String(i).padStart(2, '0')} E2E`, 20);
    }
    await tx(main, -45, 'Rent List E2E', 3);
    await api(page, 'POST', `/api/accounts/${main}/transfer`, {
      accountId: main,
      targetAccountId: cash,
      value: { amount: 75, currency: 'EUR' },
      date: `${day(2)}T00:00:00.000Z`,
      title: 'Move List E2E',
    });
    await tx(main, 3000, 'Salary List E2E', 0, { allocation: { templateId: templates[0].id } });
    await tx(main, -20, 'Groceries List E2E', 0);
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('shows a header with the net for every day and a row per transaction', async () => {
    await page.goto('/dashboard');

    const today = transactions.getByRole('row').filter({
      has: page.getByRole('rowheader', { name: dayHeader(0), exact: true }),
    });
    await expect(today).toBeVisible();
    await expect(today).toContainText(/\+2,980\.00\s?EUR/);
    await expect(transactions.getByRole('rowheader', { name: dayHeader(3), exact: true })).toBeVisible();

    const rent = rowOf('Rent List E2E');
    await expect(rent.locator('.sym')).toContainText('−');
    await expect(amountOf('Rent List E2E')).toHaveText(/^45\.00\s?EUR$/);
    await expect(rent.locator('.meta')).toHaveText('Main Account');
    await expect(transactions.getByText(/[€$£]|zł/)).toHaveCount(0);

    const salary = rowOf('Salary List E2E');
    await expect(salary.locator('.sym')).toContainText('+');
    await expect(salary.locator('.meta')).toContainText('Main budget · Main Account');
    const move = rowOf('Move List E2E');
    await expect(move.locator('.sym')).toContainText('→');
    await expect(move.locator('.meta')).toHaveText('Main Account → Cash');
  });

  test('collapses the allocations of an income by default and expands them', async () => {
    await page.goto('/dashboard');
    const toggle = rowOf('Salary List E2E').locator('button.toggle');

    await expect(toggle).toHaveAccessibleName('Show allocations');
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    await expect(allocationRows('Salary List E2E')).toHaveCount(0);

    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-expanded', 'true');
    await expect(toggle).toHaveAccessibleName('Hide allocations');
    await expect(allocationRows('Salary List E2E')).toHaveCount(5);
    const food = transactions.getByRole('row', { name: 'Food from income: Salary List E2E' });
    await expect(food).toContainText('600.00');
    await expect(food.locator('.sym')).toContainText('↓');

    await toggle.click();
    await expect(allocationRows('Salary List E2E')).toHaveCount(0);
  });

  test('opens the edit dialog from a row and from its actions menu', async () => {
    await page.goto('/dashboard');
    await editButton('Rent List E2E').click();
    await expect(dialog.getByLabel('Amount', { exact: true })).toHaveValue('45');
    await dialog.getByRole('button', { name: 'Cancel' }).click();
    await expect(dialog).toBeHidden();

    await pick('Rent List E2E', 'Edit');
    await dialog.getByLabel('Amount', { exact: true }).fill('50');
    await dialog.getByRole('button', { name: 'Save changes' }).click();
    await expect(dialog).toBeHidden();
    await expect(amountOf('Rent List E2E')).toHaveText(/^50\.00\s?EUR$/);
  });

  test('edits a transfer in its own dialog', async () => {
    await page.goto('/dashboard');
    await pick('Move List E2E', 'Edit');
    const edit = page.getByRole('dialog', { name: 'Edit transfer' });
    await expect(edit.getByLabel('Amount', { exact: true })).toHaveValue('75');
    await expect(edit.getByLabel('Title', { exact: true })).toHaveValue('Move List E2E');
    await edit.getByLabel('Amount', { exact: true }).fill('80');
    await edit.getByRole('button', { name: 'Save changes' }).click();
    await expect(edit).toBeHidden();

    await expect(amountOf('Move List E2E')).toHaveText(/^80\.00\s?EUR$/);
  });

  test('copies an expense and a transfer, dated today', async () => {
    await page.goto('/dashboard');
    await pick('Groceries List E2E', 'Copy');
    await expect(dialog.getByLabel('Title', { exact: true })).toHaveValue('Groceries List E2E');
    await expect(dialog.getByLabel('Amount', { exact: true })).toHaveValue('20');
    await expect(dialog.getByLabel('Date', { exact: true })).toHaveValue(day(0));
    await dialog.getByRole('button', { name: 'Save transaction' }).click();
    await expect(dialog).toBeHidden();
    await expect(editButton('Groceries List E2E')).toHaveCount(2);

    await pick('Move List E2E', 'Copy');
    const copy = page.getByRole('dialog', { name: 'New transfer' });
    await expect(copy.getByLabel('Amount', { exact: true })).toHaveValue('80');
    await expect(copy.getByLabel('Date', { exact: true })).toHaveValue(day(0));
    await copy.getByRole('button', { name: 'Transfer', exact: true }).click();
    await expect(copy).toBeHidden();
    await expect(editButton('Move List E2E')).toHaveCount(2);
  });

  test('deletes a transaction after confirming', async () => {
    await page.goto('/dashboard');
    await pick('Rent List E2E', 'Delete');
    const confirm = page.getByRole('dialog', { name: 'Delete transaction?' });
    await expect(confirm).toContainText('Rent List E2E');
    await expect(confirm).toContainText(/50\.00\s?EUR/);
    await confirm.getByRole('button', { name: 'Cancel' }).click();
    await expect(confirm).toBeHidden();
    await expect(editButton('Rent List E2E')).toBeVisible();

    await pick('Rent List E2E', 'Delete');
    await confirm.getByRole('button', { name: 'Delete', exact: true }).click();
    await expect(confirm).toBeHidden();
    await expect(editButton('Rent List E2E')).toHaveCount(0);
  });

  test('offers Allocate only for incomes that are not fully allocated', async () => {
    await page.goto('/dashboard');
    await transactions.getByRole('button', { name: 'Actions for Salary List E2E', exact: true }).click();
    await expect(page.getByRole('menuitem', { name: 'Edit', exact: true })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Copy', exact: true })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Delete', exact: true })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Allocate', exact: true })).toHaveCount(0);
    await page.keyboard.press('Escape');
  });

  test('loads more than one page while scrolling and ends with the opening balances', async () => {
    await page.goto('/dashboard');
    const count = transactions.locator('.foot .count');
    await expect(count).toHaveText(/^\d+ transactions$/);
    const total = Number((await count.textContent())?.trim().split(' ')[0]);
    expect(total).toBeGreaterThan(BULK);

    const bulk = transactions.getByRole('button', { name: /^Edit transaction: Bulk \d+ E2E$/ });
    expect(await bulk.count()).toBeLessThan(BULK);

    const nextPage = page.waitForRequest((r) => r.url().includes('/feed') && r.url().includes('offset=50'));
    const region = transactions.getByRole('region', { name: 'Transactions table' });
    await expect(async () => {
      await region.evaluate((el) => el.scrollTo({ top: el.scrollHeight }));
      await expect(transactions.getByRole('rowheader', { name: 'Opening balances' })).toBeVisible({
        timeout: 1_000,
      });
    }).toPass();
    await nextPage;

    await expect(editButton('Bulk 01 E2E')).toBeVisible();
    await expect(editButton('Groceries List E2E')).toHaveCount(0);

    await region.evaluate((el) => el.scrollTo({ top: el.scrollHeight }));
    await expect(transactions.getByText('Initial balance', { exact: true }).first()).toBeVisible();
    await expect(transactions.locator('.sym').filter({ hasText: '=' }).first()).toBeVisible();
    await expect(transactions.getByText('Initial allocation', { exact: true }).first()).toBeVisible();
  });
});
