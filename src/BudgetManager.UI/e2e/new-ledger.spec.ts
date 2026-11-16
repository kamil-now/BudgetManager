import { test, expect, Page } from '@playwright/test';
import { ledgerNav, registerUser } from './helpers';

test.describe.configure({ mode: 'serial' });

test.describe('new-ledger wizard', () => {
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    await registerUser(page);
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('starts with one budget and can add then remove a second', async () => {
    await page.goto('/new-ledger');
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Set up your budgets' })).toBeVisible();
    await expect(page.locator('.budget-card')).toHaveCount(1);

    await page.getByRole('button', { name: 'Add budget' }).click();
    await expect(page.locator('.budget-card')).toHaveCount(2);

    await page
      .locator('.budget-card')
      .nth(1)
      .getByRole('button', { name: 'Remove budget' })
      .click();
    await expect(page.locator('.budget-card')).toHaveCount(1);
  });

  test('offers a named, removable allocation template per budget', async () => {
    await page.goto('/new-ledger');
    await page.getByRole('button', { name: 'Continue' }).click();

    const card = page.locator('.budget-card').first();
    const templateName = card.getByRole('textbox', { name: 'Template name', exact: true });
    await expect(templateName).toHaveValue('Default');
    await expect(card.locator('#t-ccy-0')).toHaveValue('EUR');
    await expect(card.getByRole('list', { name: 'Steps' }).getByRole('listitem')).toHaveCount(5);

    await card.getByRole('button', { name: 'Remove template' }).click();
    await expect(templateName).toHaveCount(0);
    await card.getByRole('button', { name: 'Add allocation template' }).click();
    await expect(templateName).toBeVisible();
  });

  test('creates a ledger with multiple budgets and shows them on the dashboard', async () => {
    await page.goto('/new-ledger');

    await expect(page.getByRole('heading', { name: 'Create your ledger' })).toBeVisible();
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Set up your budgets' })).toBeVisible();
    await page.getByRole('button', { name: 'Add budget' }).click();
    const second = page.locator('.budget-card').nth(1);
    await second.locator('#b-name-1').fill('Savings Plan');
    await second.getByLabel('Fund name').first().fill('Emergency');
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Add your accounts' })).toBeVisible();
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Allocate your balances' })).toBeVisible();
    await page.getByRole('button', { name: 'Create ledger' }).click();

    await page.waitForURL('**/dashboard');
    await expect(ledgerNav(page).locator('[aria-current="true"]')).toContainText('Default Ledger');

    const budgets = page.locator('section[aria-labelledby="h-budgets"]');
    await expect(budgets.getByText('Main budget')).toBeVisible();
    await expect(budgets.getByText('Savings Plan')).toBeVisible();

    const accounts = page.locator('section[aria-labelledby="h-accounts"]');
    await expect(accounts.getByText('Cash', { exact: true })).toBeVisible();
    await expect(accounts.getByText('Main Account')).toBeVisible();

    const eur = page.getByRole('group', { name: 'Total EUR', exact: true });
    await expect(eur).toContainText('6,400.00');
    await expect(eur).not.toContainText('Unallocated');
  });

  async function toAllocateStep(name: string, accounts?: () => Promise<void>): Promise<void> {
    await page.goto('/new-ledger');
    await page.getByLabel('Name', { exact: true }).fill(name);
    await page.getByRole('button', { name: 'Continue' }).click();
    await expect(page.getByRole('heading', { name: 'Set up your budgets' })).toBeVisible();
    await page.getByRole('button', { name: 'Continue' }).click();
    await expect(page.getByRole('heading', { name: 'Add your accounts' })).toBeVisible();
    if (accounts) await accounts();
    await page.getByRole('button', { name: 'Continue' }).click();
    await expect(page.getByRole('heading', { name: 'Allocate your balances' })).toBeVisible();
  }

  const fundAmount = (label: string) => page.getByLabel(label, { exact: true });

  test('pre-fills the allocation from the template and blocks Create until all is allocated', async () => {
    await toAllocateStep('Allocate Step E2E');

    const eur = page.getByRole('region', { name: /EUR · 6,400\.00\s?EUR to allocate/ });
    await expect(eur).toBeVisible();
    await expect(fundAmount('EUR: Main budget / Food')).toHaveValue('600');
    await expect(fundAmount('EUR: Main budget / Rent')).toHaveValue('800');
    await expect(fundAmount('EUR: Main budget / Utilities')).toHaveValue('200');
    await expect(fundAmount('EUR: Main budget / Entertainment')).toHaveValue('960');
    await expect(fundAmount('EUR: Main budget / Savings')).toHaveValue('3840');
    await expect(eur.locator('.plan-totals')).toContainText(/Left\s*0\.00\s?EUR/);

    await fundAmount('EUR: Main budget / Savings').fill('3000');
    await expect(eur.locator('.plan-totals')).toContainText(/Left\s*840\.00\s?EUR/);
    await page.getByRole('button', { name: 'Create ledger' }).click();
    await expect(page.getByRole('alert')).toHaveText(/^Allocate all of EUR: 840\.00\s?EUR left\.$/);

    await page.getByRole('button', { name: 'Back' }).click();
    await page.getByRole('button', { name: 'Continue' }).click();
    await expect(fundAmount('EUR: Main budget / Savings')).toHaveValue('3000');

    await page
      .getByRole('button', { name: '+ rest into EUR: Main budget / Savings', exact: true })
      .click();
    await expect(fundAmount('EUR: Main budget / Savings')).toHaveValue('3840');
    await expect(eur.locator('.plan-totals')).toContainText(/Left\s*0\.00\s?EUR/);

    await page.getByRole('button', { name: 'Create ledger' }).click();
    await page.waitForURL('**/dashboard');
    await expect(ledgerNav(page).locator('[aria-current="true"]')).toContainText(
      'Allocate Step E2E',
    );
    const total = page.getByRole('group', { name: 'Total EUR', exact: true });
    await expect(total).toContainText('6,400.00');
    await expect(total).not.toContainText('Unallocated');
  });

  test('starts a currency without a template at zero and skips non-positive totals', async () => {
    await toAllocateStep('No Template E2E', async () => {
      const first = page.getByRole('list', { name: 'Accounts' }).getByRole('listitem').first();
      await first.getByLabel('Currency').selectOption('USD');
      await page.locator('#a-bal-0').fill('100');
      await page.locator('#a-bal-1').fill('0');
      await page.locator('#a-bal-2').fill('0');
    });

    await expect(page.getByText(/Nothing to allocate in EUR \(total 0\.00\s?EUR\)\./)).toBeVisible();
    const usd = page.getByRole('region', { name: /USD · 100\.00\s?USD to allocate/ });
    await expect(usd).toContainText('No template fits this USD total');
    await expect(fundAmount('USD: Main budget / Food')).toHaveValue('');
    await expect(usd.locator('.plan-totals')).toContainText(/Left\s*100\.00\s?USD/);

    await page.getByRole('button', { name: 'Create ledger' }).click();
    await expect(page.getByRole('alert')).toHaveText(/^Allocate all of USD: 100\.00\s?USD left\.$/);

    await page
      .getByRole('button', { name: '+ rest into USD: Main budget / Food', exact: true })
      .click();
    await expect(fundAmount('USD: Main budget / Food')).toHaveValue('100');
    await page.getByRole('button', { name: 'Create ledger' }).click();
    await page.waitForURL('**/dashboard');
    await expect(page.getByRole('group', { name: 'Total USD', exact: true })).toContainText(
      '100.00',
    );
  });
});
