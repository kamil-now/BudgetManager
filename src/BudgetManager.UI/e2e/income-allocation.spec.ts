import { test, expect, Locator, Page } from '@playwright/test';
import { openNew, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

test.describe('income allocation', () => {
  let page: Page;
  let budgets: Locator;
  let transactions: Locator;
  let dialog: Locator;

  const fund = (name: string): Locator =>
    budgets.locator('ul.tiles button.tile').filter({
      has: page.locator('.tile-name', { hasText: new RegExp(`^\\s*${name}\\s*$`) }),
    });
  const mode = (name: string): Locator =>
    dialog
      .getByRole('group', { name: 'Allocation', exact: true })
      .getByRole('button', { name, exact: true });
  const previewLine = (name: string): Locator =>
    dialog
      .getByRole('list', { name: 'Allocation preview' })
      .getByRole('listitem')
      .filter({ hasText: name });
  const lineType = (line: number, type: 'Fixed' | 'Percent'): Locator =>
    dialog
      .getByRole('group', { name: `Line ${line} type` })
      .getByRole('button', { name: type, exact: true });
  const removeButtons = (): Locator => dialog.getByRole('button', { name: /^Remove line/ });
  const takes = (line: number): Locator => dialog.locator('.takes').nth(line - 1);
  const leftOver = (): Locator => dialog.locator('.steps-left');

  async function startIncome(amount: string, title: string): Promise<void> {
    await openNew(page, 'Income');
    const account = dialog.getByLabel('Account');
    await expect(account).not.toHaveValue('');
    await account.selectOption({ label: 'Main Account' });
    await dialog.getByLabel('Amount', { exact: true }).fill(amount);
    await dialog.getByLabel('Title', { exact: true }).fill(title);
  }

  async function useTemplate(): Promise<void> {
    await mode('Use a template').click();
    await expect(dialog.getByLabel('Template', { exact: true })).not.toHaveValue('');
  }

  async function save(label = 'Save transaction'): Promise<void> {
    await dialog.getByRole('button', { name: label }).click();
    await expect(dialog).toBeHidden();
  }

  async function cancel(): Promise<void> {
    await dialog.getByRole('button', { name: 'Cancel' }).click();
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

  test('allocates an income with the budget template', async () => {
    await page.goto('/dashboard');
    await startIncome('3000', 'Salary Template E2E');
    await expect(mode('Don\'t allocate')).toHaveAttribute('aria-pressed', 'true');
    await useTemplate();

    await expect(previewLine('Food')).toContainText('600.00');
    await expect(previewLine('Rent')).toContainText('800.00');
    await expect(previewLine('Utilities')).toContainText('200.00');
    await expect(previewLine('Entertainment')).toContainText('280.00');
    await expect(previewLine('Savings')).toContainText('1,120.00');
    await expect(dialog.locator('.split-left')).toHaveCount(0);
    await expect(previewLine('Unallocated')).toHaveCount(0);
    await save();

    await expect(fund('Food')).toContainText('600.00');
    await expect(fund('Rent')).toContainText('800.00');
    await expect(fund('Utilities')).toContainText('200.00');
    await expect(fund('Entertainment')).toContainText('280.00');
    await expect(fund('Savings')).toContainText('1,120.00');
  });

  test('allocates an income with custom steps', async () => {
    await page.goto('/dashboard');
    await startIncome('500', 'Bonus Custom E2E');
    await mode('Custom').click();

    await dialog.getByLabel('Fund 1', { exact: true }).selectOption({ label: 'Food' });
    await dialog.getByLabel('Fund 1 amount', { exact: true }).fill('600');
    await expect(dialog.getByText(/below the minimum of 600\.00\s?EUR/)).toBeVisible();

    await dialog.getByLabel('Fund 1 amount', { exact: true }).fill('100');
    await dialog.getByRole('button', { name: 'Add step' }).click();
    await dialog.getByLabel('Fund 2', { exact: true }).selectOption({ label: 'Savings' });
    await lineType(2, 'Percent').click();
    await dialog.getByLabel('Fund 2 percent', { exact: true }).fill('50');
    await expect(takes(1)).toContainText('100.00');
    await expect(takes(2)).toContainText('200.00');
    await expect(leftOver()).toContainText('200.00');
    await save();

    await expect(fund('Food')).toContainText('700.00');
    await expect(fund('Savings')).toContainText('1,320.00');
    await expect(fund('Rent')).toContainText('800.00');
  });

  test('pre-fills the allocations when editing and clears them with Don\'t allocate', async () => {
    await page.goto('/dashboard');
    await transactions.getByRole('button', { name: 'Edit transaction: Bonus Custom E2E' }).click();
    await expect(dialog).toBeVisible();

    await expect(mode('Custom')).toHaveAttribute('aria-pressed', 'true');
    await expect(removeButtons()).toHaveCount(2);
    await expect(lineType(1, 'Fixed')).toHaveAttribute('aria-pressed', 'true');
    await expect(lineType(2, 'Fixed')).toHaveAttribute('aria-pressed', 'true');
    const values = dialog.locator('input[aria-label$=" amount"]');
    await expect(values).toHaveCount(2);
    expect(
      (await values.evaluateAll((els) => els.map((el) => (el as HTMLInputElement).value))).sort(),
    ).toEqual(['100', '200']);
    await expect(leftOver()).toContainText('200.00');

    await mode('Don\'t allocate').click();
    await save('Save changes');

    await expect(fund('Food')).toContainText('600.00');
    await expect(fund('Savings')).toContainText('1,120.00');
  });

  test('repeats a fund on several steps and the order matters', async () => {
    await page.goto('/dashboard');
    await startIncome('1000', 'Repeat Fund E2E');
    await mode('Custom').click();

    await dialog.getByLabel('Fund 1', { exact: true }).selectOption({ label: 'Food' });
    await dialog.getByLabel('Fund 1 amount', { exact: true }).fill('100');
    await dialog.getByRole('button', { name: 'Add step' }).click();
    await dialog.getByLabel('Fund 2', { exact: true }).selectOption({ label: 'Food' });
    await lineType(2, 'Percent').click();
    await dialog.getByLabel('Fund 2 percent', { exact: true }).fill('10');

    await expect(takes(1)).toContainText('100.00');
    await expect(takes(2)).toContainText('90.00');
    await expect(leftOver()).toContainText('810.00');

    await dialog.getByRole('button', { name: 'Move line 2 up', exact: true }).click();
    await expect(lineType(1, 'Percent')).toHaveAttribute('aria-pressed', 'true');
    await expect(takes(1)).toContainText('100.00');
    await expect(takes(2)).toContainText('100.00');
    await expect(leftOver()).toContainText('800.00');
    await save();

    await expect(fund('Food')).toContainText('800.00');
  });

  test('copies the template into custom steps', async () => {
    await page.goto('/dashboard');
    await startIncome('2000', 'Customize E2E');
    await useTemplate();

    await expect(previewLine('Savings')).toContainText('320.00');
    await dialog.getByRole('button', { name: 'Customize' }).click();

    await expect(mode('Custom')).toHaveAttribute('aria-pressed', 'true');
    await expect(removeButtons()).toHaveCount(5);
    await expect(dialog.getByLabel('Fund 1 amount', { exact: true })).toHaveValue('800');
    await expect(dialog.getByLabel('Fund 4 percent', { exact: true })).toHaveValue('20');
    await expect(takes(5)).toContainText('320.00');
    await expect(leftOver()).toContainText('0.00');
    await cancel();
  });

  test('disables templates the amount does not reach and templates in another currency', async () => {
    await page.goto('/dashboard');
    await startIncome('100', 'Too Small E2E');
    await mode('Use a template').click();

    const option = dialog.locator('option', { hasText: /needs at least/ });
    await expect(option).toBeDisabled();

    await dialog.getByLabel('Currency').selectOption('USD');
    await expect(dialog.getByText(/No budget has a USD template/)).toBeVisible();

    await dialog.getByLabel('Currency').selectOption('EUR');
    await dialog.getByLabel('Amount', { exact: true }).fill('2000');
    await expect(dialog.getByLabel('Template', { exact: true })).not.toHaveValue('');
    await expect(previewLine('Food')).toBeVisible();
    await cancel();
  });

  test('hides the allocation section for an expense and pays it from a fund', async () => {
    await page.goto('/dashboard');
    await openNew(page, 'Income');
    const heading = dialog.getByRole('heading', { name: 'Allocation', exact: true });
    await expect(heading).toBeVisible();

    await dialog.getByRole('button', { name: 'Expense', exact: true }).click();
    await expect(heading).toBeHidden();
    await cancel();

    await openNew(page, 'Expense');
    await expect(dialog.getByRole('button', { name: 'Expense', exact: true })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    await expect(heading).toBeHidden();
    await dialog.getByLabel('Account').selectOption({ label: 'Main Account' });
    await dialog.getByLabel('Amount', { exact: true }).fill('50');
    await dialog.getByLabel('Title', { exact: true }).fill('Groceries Fund E2E');
    await expect(dialog.getByLabel('Fund', { exact: true })).toContainText('None');
    await dialog.getByLabel('Fund', { exact: true }).selectOption({ label: 'Food' });
    await save();

    await expect(fund('Food')).toContainText('750.00');
    const row = transactions
      .getByRole('row')
      .filter({ has: page.getByRole('button', { name: 'Edit transaction: Groceries Fund E2E' }) });
    await expect(row.locator('.meta')).toContainText('Main budget / Food · Main Account');
  });
});
