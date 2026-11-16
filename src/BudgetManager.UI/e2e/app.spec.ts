import { test, expect, Locator, Page } from '@playwright/test';
import { openNew, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

function isoDay(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function daysAgo(days: number): string {
  const now = new Date();
  return isoDay(new Date(now.getFullYear(), now.getMonth(), now.getDate() - days));
}

test.describe('dashboard', () => {
  let page: Page;
  const section = (id: string): Locator => page.locator(`section[aria-labelledby="${id}"]`);
  const accounts = () => section('h-accounts');
  const budgets = () => section('h-budgets');
  const transactions = () => section('h-transactions');
  const typeFilter = (name: string) =>
    transactions()
      .getByRole('group', { name: 'Transaction type' })
      .getByRole('button', { name, exact: true });
  const total = (currency: string): Locator =>
    page.getByRole('group', { name: `Total ${currency}`, exact: true });

  async function addTransaction(opts: {
    kind?: 'Income' | 'Expense';
    account: string;
    amount: string;
    title: string;
    date?: string;
  }): Promise<void> {
    await openNew(page, opts.kind ?? 'Income');
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByLabel('Account')).not.toHaveValue('');
    const kind = dialog
      .getByRole('group', { name: 'Transaction type' })
      .getByRole('button', { name: opts.kind ?? 'Income', exact: true });
    await expect(kind).toHaveAttribute('aria-pressed', 'true');
    await dialog.getByLabel('Account').selectOption({ label: opts.account });
    await dialog.getByLabel('Amount', { exact: true }).fill(opts.amount);
    if (opts.date) await dialog.getByLabel('Date', { exact: true }).fill(opts.date);
    await dialog.getByLabel('Title', { exact: true }).fill(opts.title);
    await dialog.getByRole('button', { name: 'Save transaction' }).click();
    await expect(dialog).toBeHidden();
  }

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    await registerAndCreateLedger(page);
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('shows the topbar totals for a fresh ledger', async () => {
    await page.goto('/dashboard');

    const eur = total('EUR');
    await expect(eur).toContainText('6,400.00');
    await expect(eur).toContainText('EUR');
    await expect(eur).not.toContainText('In funds');
    await expect(eur).not.toContainText('Unallocated');
    await expect(eur.getByRole('button')).toHaveCount(0);
    await expect(page.getByRole('main').getByText(/[€$£]|zł/)).toHaveCount(0);
  });

  test('records an income and an expense', async () => {
    await page.goto('/dashboard');
    await addTransaction({ account: 'Main Account', amount: '2000', title: 'Salary E2E' });
    await addTransaction({
      kind: 'Expense',
      account: 'Main Account',
      amount: '800',
      title: 'Rent E2E',
    });

    await expect(transactions().getByText('Salary E2E')).toBeVisible();
    await expect(transactions().getByText('Rent E2E')).toBeVisible();
    await expect(total('EUR')).toContainText('7,600.00');
    await expect(total('EUR')).toContainText('Unallocated');
    await expect(total('EUR')).toContainText('1,200.00');
  });

  test('edits a transaction', async () => {
    await page.goto('/dashboard');
    await transactions().getByRole('button', { name: 'Edit transaction: Salary E2E' })
      .click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await dialog.getByLabel('Amount', { exact: true }).fill('2500');
    await dialog.getByRole('button', { name: 'Save changes' }).click();
    await expect(dialog).toBeHidden();

    const row = transactions()
      .getByRole('row')
      .filter({ has: page.getByRole('button', { name: 'Edit transaction: Salary E2E' }) });
    await expect(row.locator('.amt .amount')).toHaveText(/^2,500\.00\s?EUR$/);
    await expect(row.locator('.sym')).toContainText('+');
  });

  test('filters by type', async () => {
    await page.goto('/dashboard');

    await typeFilter('Expense').click();
    await expect(typeFilter('Expense')).toHaveAttribute('aria-pressed', 'true');
    await expect(transactions().getByText('Rent E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeHidden();

    await typeFilter('Income').click();
    await expect(transactions().getByText('Salary E2E')).toBeVisible();
    await expect(transactions().getByText('Rent E2E')).toBeHidden();

    await typeFilter('All').click();
    await expect(transactions().getByText('Rent E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeVisible();
  });

  test('searches the transactions on the server and clears the filters', async () => {
    await page.goto('/dashboard');

    await transactions().getByLabel('Search').fill('rent e2e');
    await expect(transactions().getByText('Rent E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeHidden();
    await expect(transactions().locator('.foot .count')).toHaveText('1 transaction');

    await transactions().getByLabel('Search').fill('MAIN ACCOUNT');
    await expect(transactions().getByText('Rent E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeVisible();

    await transactions().getByLabel('Search').fill('no such transaction');
    await expect(transactions().getByText('No transactions match these filters.')).toBeVisible();

    await transactions().getByRole('button', { name: 'Clear', exact: true }).click();
    await expect(transactions().getByLabel('Search')).toHaveValue('');
    await expect(transactions().getByText('Salary E2E')).toBeVisible();
  });

  test('scopes the transactions to an account and clears the scope chip', async () => {
    await page.goto('/dashboard');
    await addTransaction({ kind: 'Expense', account: 'Cash', amount: '4.50', title: 'Coffee E2E' });

    const cash = accounts().locator('button.tile').filter({ hasText: 'Cash' });
    await cash.click();
    await expect(cash).toHaveAttribute('aria-pressed', 'true');
    await expect(transactions().getByText('Account: Cash')).toBeVisible();
    await expect(transactions().getByText('Coffee E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeHidden();

    await transactions().getByRole('button', { name: 'Clear scope Account: Cash' }).click();
    await expect(cash).toHaveAttribute('aria-pressed', 'false');
    await expect(transactions().getByText('Account: Cash')).toBeHidden();
    await expect(transactions().getByText('Salary E2E')).toBeVisible();

    await cash.click();
    await expect(transactions().getByText('Salary E2E')).toBeHidden();
    await cash.click();
    await expect(transactions().getByText('Salary E2E')).toBeVisible();

    const mainBudget = budgets().locator('button.budget-tile').filter({ hasText: 'Main budget' });
    await mainBudget.click();
    await expect(transactions().getByText('Budget: Main budget')).toBeVisible();
    await expect(transactions().getByText('Coffee E2E')).toBeHidden();
    await mainBudget.click();
    await expect(transactions().getByText('Coffee E2E')).toBeVisible();
  });

  test('filters by period', async () => {
    await page.goto('/dashboard');
    await addTransaction({
      account: 'Main Account',
      amount: '300',
      title: 'Old Bonus E2E',
      date: daysAgo(60),
    });

    const period = transactions().getByLabel('Period');
    await expect(period).toHaveValue('all');
    await expect(transactions().getByText('Salary E2E')).toBeVisible();
    await expect(transactions().getByText('Old Bonus E2E')).toBeVisible();

    await period.selectOption({ label: 'Last 30 days' });
    await expect(transactions().getByText('Salary E2E')).toBeVisible();
    await expect(transactions().getByText('Old Bonus E2E')).toBeHidden();

    await period.selectOption({ label: 'Last 90 days' });
    await expect(transactions().getByText('Old Bonus E2E')).toBeVisible();

    await period.selectOption({ label: 'Last 7 days' });
    await expect(transactions().getByText('Old Bonus E2E')).toBeHidden();

    await period.selectOption({ label: 'Custom' });
    await transactions().getByLabel('From', { exact: true }).fill(daysAgo(61));
    await transactions().getByLabel('To', { exact: true }).fill(daysAgo(59));
    await expect(transactions().getByText('Old Bonus E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeHidden();

    await transactions().getByLabel('From', { exact: true }).fill(daysAgo(1));
    await expect(transactions().getByRole('alert')).toHaveText(
      'The From date must be on or before the To date.',
    );

    await period.selectOption({ label: 'All time' });
    await expect(transactions().getByText('Old Bonus E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeVisible();
  });

  test('adds an account', async () => {
    await page.goto('/dashboard');
    await openNew(page, 'Account');
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Name', { exact: true }).fill('Travel Fund');
    await dialog.getByLabel('Initial balance', { exact: true }).fill('300');
    await dialog.getByRole('button', { name: 'Add account' }).click();
    await expect(dialog).toBeHidden();

    await expect(accounts().getByText('Travel Fund')).toBeVisible();
  });

  test('adds an account from the accounts panel', async () => {
    await page.goto('/dashboard');
    await accounts().getByRole('button', { name: 'Add account', exact: true })
      .click();
    const dialog = page.getByRole('dialog', { name: 'Add account' });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
    await expect(dialog).toBeHidden();
  });

  test('creates a budget from the dashboard', async () => {
    await page.goto('/dashboard');
    await openNew(page, 'Budget');
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Name', { exact: true }).fill('Holiday E2E');
    await dialog.getByLabel('Fund name').first().fill('Flights');
    await dialog.getByRole('button', { name: 'Create budget' }).click();
    await expect(dialog).toBeHidden();

    await expect(budgets().getByRole('heading', { name: 'Holiday E2E' })).toBeVisible();
    await expect(budgets().getByText('Flights')).toBeVisible();
    await expect(
      budgets().getByRole('button', { name: 'Actions for Holiday E2E' }),
    ).toBeVisible();
  });

  test('transfers between two accounts', async () => {
    await page.goto('/dashboard');
    await openNew(page, 'Transfer');
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('From', { exact: true }).selectOption({ index: 0 });
    await dialog.getByLabel('To', { exact: true }).selectOption({ index: 1 });
    await dialog.getByLabel('Amount', { exact: true }).fill('50');
    await dialog.getByLabel('Title', { exact: true }).fill('Transfer E2E');
    await dialog.getByRole('button', { name: 'Transfer' }).click();
    await expect(dialog).toBeHidden();

    await expect(transactions().getByText('Transfer E2E')).toBeVisible();
    await typeFilter('Transfers').click();
    await expect(transactions().getByText('Transfer E2E')).toBeVisible();
    await expect(transactions().getByText('Salary E2E')).toBeHidden();
  });

  test('exchanges one currency for another', async () => {
    await page.goto('/dashboard');
    await openNew(page, 'Exchange');
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Sell currency').selectOption('EUR');
    await dialog.getByLabel('You sell').fill('100');
    await dialog.getByLabel('Buy currency').selectOption('USD');
    await dialog.getByLabel('You buy').fill('110');
    await dialog.getByLabel('Title', { exact: true }).fill('Exchange E2E');
    await dialog.getByRole('button', { name: 'Save exchange' }).click();
    await expect(dialog).toBeHidden();

    await expect(transactions().getByText('Exchange E2E')).toBeVisible();
    await typeFilter('Exchanges').click();
    await expect(transactions().getByText('Exchange E2E')).toBeVisible();
    await expect(transactions().getByText('Transfer E2E')).toBeHidden();

    await expect(total('USD')).toContainText('110.00');
    await expect(total('USD')).toContainText('USD');
  });

  test('navigates between the main sections', async () => {
    await page.goto('/dashboard');
    const nav = page.getByRole('navigation', { name: 'Main' });
    await expect(nav.getByRole('link')).toHaveCount(3);

    await nav.getByRole('link', { name: 'Statistics', exact: true }).click();
    await expect(page).toHaveURL(/\/statistics/);
    await nav.getByRole('link', { name: 'Settings', exact: true }).click();
    await expect(page).toHaveURL(/\/settings/);
    await nav.getByRole('link', { name: 'Dashboard', exact: true }).click();
    await expect(page).toHaveURL(/\/dashboard/);

    await page.goto('/transactions');
    await expect(page).toHaveURL(/\/dashboard/);
    await page.goto('/budgets');
    await expect(page).toHaveURL(/\/dashboard/);
  });
});
