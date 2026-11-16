import { test, expect, Locator, Page } from '@playwright/test';
import { createLedger, ledgerNav, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

test.describe('menus', () => {
  let page: Page;
  const ledgers = (): Locator => ledgerNav(page).getByRole('button');
  const current = (): Locator => ledgerNav(page).locator('[aria-current="true"]');
  const newButton = (): Locator => page.getByRole('button', { name: 'New', exact: true });
  const menu = (): Locator => page.getByRole('menu');
  const item = (name: string): Locator => page.getByRole('menuitem', { name, exact: true });

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    await registerAndCreateLedger(page);
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('lists the ledgers in the sidebar and opens the new-ledger wizard from there', async () => {
    await page.goto('/dashboard');
    await expect(ledgers()).toHaveCount(1);
    await expect(current()).toContainText('Default Ledger');
    await expect(current()).toContainText('6,400.00');
    await expect(current()).toContainText('EUR');

    await ledgerNav(page).getByRole('link', { name: 'New ledger', exact: true }).click();
    await page.waitForURL('**/new-ledger');
    await expect(page.getByRole('heading', { name: 'Create a new ledger' })).toBeVisible();
    await expect(page.getByLabel('Name', { exact: true })).toHaveValue('');

    await page.getByRole('link', { name: 'Cancel', exact: true }).click();
    await page.waitForURL('**/dashboard');
    await expect(current()).toContainText('Default Ledger');
  });

  test('creates a second ledger and switches between both', async () => {
    await createLedger(page, { name: 'Second Ledger E2E' });

    await expect(ledgers()).toHaveCount(2);
    await expect(current()).toContainText('Second Ledger E2E');

    await ledgerNav(page).getByRole('button', { name: /Default Ledger/ }).click();
    await expect(current()).toContainText('Default Ledger');
    await expect(page.getByRole('group', { name: 'Total EUR', exact: true })).toContainText(
      '6,400.00',
    );
  });

  test('navigates the New menu with the keyboard', async () => {
    await page.goto('/dashboard');
    await expect(newButton()).toBeEnabled();
    await newButton().focus();

    await page.keyboard.press('ArrowDown');
    await expect(menu()).toBeVisible();
    await expect(newButton()).toHaveAttribute('aria-expanded', 'true');
    await expect(item('Income')).toBeFocused();
    await page.keyboard.press('ArrowDown');
    await expect(item('Expense')).toBeFocused();
    await page.keyboard.press('End');
    await expect(item('Budget')).toBeFocused();
    await page.keyboard.press('ArrowDown');
    await expect(item('Income')).toBeFocused();
    await page.keyboard.press('ArrowUp');
    await expect(item('Budget')).toBeFocused();
    await page.keyboard.press('Home');
    await expect(item('Income')).toBeFocused();

    await page.keyboard.press('t');
    await expect(item('Transfer')).toBeFocused();
    await page.keyboard.press('a');
    await expect(item('Allocation')).toBeFocused();
    await expect(item('Allocation')).toHaveAttribute('aria-disabled', 'true');

    await page.keyboard.press('Escape');
    await expect(menu()).toBeHidden();
    await expect(newButton()).toBeFocused();

    await page.keyboard.press('ArrowUp');
    await expect(item('Budget')).toBeFocused();
    await page.keyboard.press('ArrowUp');
    await expect(item('Account')).toBeFocused();
    await page.keyboard.press('Enter');
    const dialog = page.getByRole('dialog', { name: 'Add account' });
    await expect(dialog).toBeVisible();
    await expect(menu()).toBeHidden();

    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(newButton()).toBeFocused();
  });
});
