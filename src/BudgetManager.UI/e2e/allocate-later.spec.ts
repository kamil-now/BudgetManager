import { test, expect, Locator, Page } from '@playwright/test';
import { day, openNew, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

test.describe('allocate later', () => {
  let page: Page;
  let budgets: Locator;
  let transactions: Locator;
  let dialog: Locator;

  const total = (): Locator => page.getByRole('group', { name: 'Total EUR', exact: true });
  const fund = (name: string): Locator =>
    budgets.locator('ul.tiles button.tile').filter({
      has: page.locator('.tile-name', { hasText: new RegExp(`^\\s*${name}\\s*$`) }),
    });
  const mode = (name: string): Locator =>
    dialog.getByRole('group', { name: 'Split' }).getByRole('button', { name, exact: true });
  const previewLine = (name: string): Locator =>
    dialog
      .getByRole('list', { name: 'Allocation preview' })
      .getByRole('listitem')
      .filter({ hasText: name });
  const editButton = (title: string): Locator =>
    transactions.getByRole('button', { name: `Edit transaction: ${title}`, exact: true });

  async function pick(title: string, action: string): Promise<void> {
    await transactions.getByRole('button', { name: `Actions for ${title}`, exact: true }).click();
    await page.getByRole('menuitem', { name: action, exact: true }).click();
  }

  async function addIncome(amount: string, title: string): Promise<void> {
    await openNew(page, 'Income');
    await expect(dialog.getByLabel('Account')).not.toHaveValue('');
    await dialog.getByLabel('Account').selectOption({ label: 'Main Account' });
    await dialog.getByLabel('Amount', { exact: true }).fill(amount);
    await dialog.getByLabel('Title', { exact: true }).fill(title);
    await dialog.getByRole('button', { name: 'Save transaction' }).click();
    await expect(dialog).toBeHidden();
  }

  async function allocate(): Promise<void> {
    await dialog.getByRole('button', { name: 'Allocate', exact: true }).click();
    await expect(dialog).toBeHidden();
  }

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    budgets = page.locator('section[aria-labelledby="h-budgets"]');
    transactions = page.locator('section[aria-labelledby="h-transactions"]');
    dialog = page.getByRole('dialog');
    await registerAndCreateLedger(page, { zeroBalances: true });
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('allocates everything unallocated with the template from the New menu', async () => {
    await page.goto('/dashboard');
    await addIncome('2000', 'Unallocated Income E2E');
    await expect(total()).toContainText('Unallocated');
    await expect(total()).toContainText('2,000.00');

    await openNew(page, 'Allocation');
    await expect(dialog.getByRole('heading', { name: 'Allocation', exact: true })).toBeVisible();
    await expect(dialog.locator('.af-left')).toContainText('2,000.00');
    await expect(dialog.getByLabel('Amount', { exact: true })).toHaveValue('2000');
    await expect(mode('Use a template')).toHaveAttribute('aria-pressed', 'true');

    await expect(previewLine('Rent')).toContainText('800.00');
    await expect(previewLine('Food')).toContainText('600.00');
    await expect(previewLine('Entertainment')).toContainText('80.00');
    await expect(previewLine('Savings')).toContainText('320.00');
    await expect(dialog.locator('.split-left')).toHaveCount(0);
    await allocate();

    await expect(fund('Rent')).toContainText('800.00');
    await expect(fund('Food')).toContainText('600.00');
    await expect(fund('Savings')).toContainText('320.00');
    await expect(total()).not.toContainText('Unallocated');
    await expect(total()).not.toContainText('In funds');
  });

  test('allocates part of it with custom steps', async () => {
    await page.goto('/dashboard');
    await addIncome('500', 'Second Income E2E');

    await openNew(page, 'Allocation');
    await expect(dialog.locator('.af-left')).toContainText('500.00');

    await dialog.getByLabel('Amount', { exact: true }).fill('600');
    await dialog.getByRole('button', { name: 'Allocate', exact: true }).click();
    await expect(dialog.getByRole('alert')).toHaveText(/^The amount can be at most 500\.00\s?EUR\.$/);

    await dialog.getByLabel('Amount', { exact: true }).fill('200');
    await mode('Custom').click();
    await dialog.getByLabel('Fund 1', { exact: true }).selectOption({ label: 'Savings' });
    await dialog
      .getByRole('group', { name: 'Line 1 type' })
      .getByRole('button', { name: 'Percent', exact: true })
      .click();
    await dialog.getByLabel('Fund 1 percent', { exact: true }).fill('50');
    await expect(dialog.locator('.takes').first()).toContainText('100.00');
    await expect(dialog.locator('.steps-left')).toContainText('100.00');
    await dialog.getByLabel('Title', { exact: true }).fill('Savings top-up E2E');
    await allocate();

    await expect(fund('Savings')).toContainText('420.00');
    await expect(total()).toContainText('Unallocated');
    await expect(total()).toContainText('400.00');
  });

  test('allocates an existing income from its row', async () => {
    await page.goto('/dashboard');
    await addIncome('300', 'Row Income E2E');
    await expect(total()).toContainText('700.00');

    await pick('Row Income E2E', 'Allocate');

    await expect(dialog.getByRole('heading', { name: 'Edit transaction' })).toBeVisible();
    const allocation = dialog.getByRole('group', { name: 'Allocation', exact: true });
    await expect(allocation.getByRole('button', { name: 'Don\'t allocate' })).not.toHaveAttribute(
      'aria-pressed',
      'true',
    );
    await allocation.getByRole('button', { name: 'Custom', exact: true }).click();
    await dialog.getByLabel('Fund 1', { exact: true }).selectOption({ label: 'Food' });
    await dialog.getByLabel('Fund 1 amount', { exact: true }).fill('300');
    await dialog.getByRole('button', { name: 'Save changes' }).click();
    await expect(dialog).toBeHidden();

    await expect(fund('Food')).toContainText('900.00');
    await expect(total()).toContainText('400.00');
  });

  test('shows an allocation in the feed and edits it', async () => {
    await page.goto('/dashboard');
    const row = transactions
      .getByRole('row')
      .filter({ has: page.getByRole('button', { name: 'Edit transaction: Savings top-up E2E' }) });
    await expect(row.locator('.sym')).toContainText('↓');
    await expect(row.locator('.amt .amount')).toHaveText(/^100\.00\s?EUR$/);
    await expect(row.locator('.meta')).toHaveText('Main budget / Savings');

    await pick('Savings top-up E2E', 'Edit');
    await expect(dialog.getByRole('heading', { name: 'Edit allocation' })).toBeVisible();
    await expect(dialog.getByLabel('Title', { exact: true })).toHaveValue('Savings top-up E2E');
    await dialog.getByLabel('Title', { exact: true }).fill('Savings edited E2E');
    await dialog.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(dialog).toBeHidden();

    await expect(editButton('Savings edited E2E')).toBeVisible();
    await expect(editButton('Savings top-up E2E')).toHaveCount(0);
    await expect(fund('Savings')).toContainText('420.00');
  });

  test('copies an allocation, dated today, then deletes the copy', async () => {
    await page.goto('/dashboard');
    await pick('Savings edited E2E', 'Copy');
    await expect(dialog.getByRole('heading', { name: 'Allocation', exact: true })).toBeVisible();
    await expect(dialog.getByLabel('Title', { exact: true })).toHaveValue('Savings edited E2E');
    await expect(dialog.getByLabel('Date', { exact: true })).toHaveValue(day(0));
    await dialog.getByLabel('Title', { exact: true }).fill('Savings copy E2E');
    await allocate();

    await expect(editButton('Savings copy E2E')).toBeVisible();
    await expect(fund('Savings')).toContainText('520.00');

    await pick('Savings copy E2E', 'Delete');
    const confirm = page.getByRole('dialog', { name: 'Delete transaction?' });
    await expect(confirm).toContainText('Savings copy E2E');
    await confirm.getByRole('button', { name: 'Delete', exact: true }).click();
    await expect(confirm).toBeHidden();

    await expect(editButton('Savings copy E2E')).toHaveCount(0);
    await expect(fund('Savings')).toContainText('420.00');
  });
});
