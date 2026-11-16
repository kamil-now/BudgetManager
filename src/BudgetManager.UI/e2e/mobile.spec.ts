import { test, expect, Locator, Page } from '@playwright/test';
import {
  PASSWORD,
  api,
  day,
  expectNoHorizontalScroll,
  expectNothingPastTheEdge,
  openNew,
  uniqueEmail,
} from './helpers';

test.describe.configure({ mode: 'serial' });

interface LedgerShape {
  accounts: { id: string; name: string }[];
  budgets: { id: string; name: string }[];
}

test.describe('phone layout', () => {
  let page: Page;
  const transactions = (): Locator => page.locator('section[aria-labelledby="h-transactions"]');
  const menuButton = (): Locator => page.locator('.menu-btn');
  const dialog = (): Locator => page.getByRole('dialog');

  async function openDrawer(): Promise<Locator> {
    await menuButton().click();
    const sidebar = page.locator('aside.sidebar.open');
    await expect(sidebar).toBeVisible();
    return sidebar;
  }

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('registers in two steps without scrolling sideways', async () => {
    await page.goto('/register');
    await expectNoHorizontalScroll(page);

    await page.locator('#name').fill('Phone Tester');
    await page.locator('#email').fill(uniqueEmail('phone'));
    await page.locator('#password').fill(PASSWORD);
    await page.locator('#confirmPassword').fill(PASSWORD);
    await expectNoHorizontalScroll(page);
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Your preferences' })).toBeVisible();
    await page.getByLabel('Default currency', { exact: true }).selectOption('EUR');
    await page.getByLabel('Date format', { exact: true }).selectOption('yyyy-MM-dd');
    await expectNoHorizontalScroll(page);
    await page.getByRole('button', { name: 'Create account' }).click();
    await page.waitForURL('**/new-ledger');
  });

  test('walks the new-ledger wizard without scrolling sideways', async () => {
    await expect(page.getByRole('heading', { name: 'Create your ledger' })).toBeVisible();
    await expectNoHorizontalScroll(page);
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Set up your budgets' })).toBeVisible();
    await expectNoHorizontalScroll(page);
    await page.getByRole('button', { name: 'Add budget' }).click();
    const second = page.locator('.budget-card').nth(1);
    await second.locator('#b-name-1').fill('Phone Plan');
    await second.getByLabel('Fund name').first().fill('Emergency');
    await second.getByRole('button', { name: 'Add allocation template' }).click();
    await expectNoHorizontalScroll(page);
    await second.getByRole('button', { name: 'Remove template' }).click();
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Add your accounts' })).toBeVisible();
    await expectNoHorizontalScroll(page);
    await page.getByRole('button', { name: 'Continue' }).click();

    await expect(page.getByRole('heading', { name: 'Allocate your balances' })).toBeVisible();
    await expectNoHorizontalScroll(page);
    await page.getByRole('button', { name: 'Create ledger' }).click();
    await page.waitForURL('**/dashboard');
  });

  test.describe('with a ledger', () => {
    test.beforeAll(async () => {
      const ledgerId = await page.evaluate(() => localStorage.getItem('budget-manager.ledgerId'));
      const ledger = await api<LedgerShape>(page, 'GET', `/api/ledgers/${ledgerId}`);
      const main = ledger.accounts.find((a) => a.name === 'Main Account')?.id ?? '';
      const cash = ledger.accounts.find((a) => a.name === 'Cash')?.id ?? '';
      const tx = (amount: number, title: string, daysAgo = 0) =>
        api(page, 'POST', `/api/accounts/${main}/transaction`, {
          accountId: main,
          value: { amount, currency: 'EUR' },
          date: `${day(daysAgo)}T00:00:00.000Z`,
          title,
        });
      await tx(500, 'Phone Income E2E');
      await tx(-18.5, 'Phone Lunch E2E');
      await api(page, 'POST', `/api/accounts/${main}/transfer`, {
        accountId: main,
        targetAccountId: cash,
        value: { amount: 40, currency: 'EUR' },
        date: `${day(1)}T00:00:00.000Z`,
        title: 'Phone Move E2E',
      });
    });

    test('has no heading in the topbar and shows the ledger on the menu button', async () => {
      await page.goto('/dashboard');
      await expect(page.locator('.topbar').getByRole('heading')).toHaveCount(0);
      await expect(menuButton()).toContainText('Default Ledger');
      await expectNoHorizontalScroll(page);
    });

    test('puts the balance card on the page with what is unallocated', async () => {
      await page.goto('/dashboard');
      const card = page.getByRole('main').getByRole('region', { name: 'Balance' });
      await expect(card).toBeVisible();
      await expect(card).toContainText('Default Ledger');
      await expect(card).toContainText('Unallocated');
      await expect(card).toContainText('481.50');
      await expect(card).not.toContainText('In funds');
      await expect(page.locator('.topbar').getByRole('group', { name: 'Total EUR' })).toHaveCount(0);
    });

    test('lists the ledgers in the drawer, balance before name, and closes it on a pick', async () => {
      await page.goto('/dashboard');
      const sidebar = await openDrawer();
      const ledgers = sidebar.getByRole('navigation', { name: 'Ledgers' }).locator('button.ledger');
      await expect(ledgers).toHaveCount(1);

      const money = await ledgers.first().locator('.ledger-money').boundingBox();
      const name = await ledgers.first().locator('.ledger-name').boundingBox();
      expect(money && name && money.y < name.y).toBeTruthy();
      await expect(ledgers.first().locator('.ledger-money')).toContainText('EUR');
      await expectNoHorizontalScroll(page);

      await ledgers.first().click();
      await expect(page.locator('aside.sidebar.open')).toHaveCount(0);
      await expect(page).toHaveURL(/\/dashboard/);
    });

    test('shows a 52px row: symbol, then amount, then title, with a details line', async () => {
      await page.goto('/dashboard');
      const row = transactions()
        .getByRole('row')
        .filter({ has: page.getByRole('button', { name: 'Edit transaction: Phone Lunch E2E', exact: true }) });
      await expect(row).toBeVisible();

      const box = await row.boundingBox();
      expect(Math.round(box?.height ?? 0)).toBe(52);

      const sym = await row.locator('.sym').boundingBox();
      const amt = await row.locator('.amt').boundingBox();
      const title = await row.locator('.title-cell').boundingBox();
      expect(sym && amt && title).toBeTruthy();
      expect(sym!.x).toBeLessThan(amt!.x);
      expect(amt!.x).toBeLessThan(title!.x);

      await expect(row.locator('.amt .amount')).toHaveText(/^\d[\d,]*\.\d{2}\s?EUR$/);
      await expect(row.locator('.sym')).toContainText('−');
      await expect(row.locator('.meta')).toHaveText('Main Account');
      await expectNoHorizontalScroll(page);
    });

    test('offers Edit, Copy and Delete on a row and the type filter is a select', async () => {
      await page.goto('/dashboard');
      await expect(transactions().locator('#tx-type')).toBeVisible();
      await expect(transactions().getByRole('group', { name: 'Transaction type' })).toHaveCount(0);

      await transactions().getByRole('button', { name: 'Actions for Phone Lunch E2E', exact: true }).click();
      await expect(page.getByRole('menuitem', { name: 'Edit', exact: true })).toBeVisible();
      await expect(page.getByRole('menuitem', { name: 'Copy', exact: true })).toBeVisible();
      await expect(page.getByRole('menuitem', { name: 'Delete', exact: true })).toBeVisible();
      await page.keyboard.press('Escape');
    });

    test('only renders a window of the rows on a phone (the list is virtual)', async () => {
      await page.goto('/dashboard');
      const region = transactions().getByRole('region', { name: 'Transactions table' });
      await expect(region).toBeVisible();
      const box = await region.boundingBox();
      expect(box?.height ?? 0).toBeGreaterThan(200);
      expect(box?.height ?? 0).toBeLessThan(852);
    });

    test('opens every New-menu dialog without scrolling sideways', async () => {
      await page.goto('/dashboard');
      for (const item of ['Income', 'Expense', 'Transfer', 'Exchange', 'Allocation', 'Account', 'Budget']) {
        await openNew(page, item);
        await expect(dialog()).toBeVisible();
        await expectNoHorizontalScroll(page);
        await expectNothingPastTheEdge(page, dialog());
        await dialog().getByRole('button', { name: 'Cancel', exact: true }).click();
        await expect(dialog()).toBeHidden();
      }
    });

    test('shows the income dialog in custom allocation mode within the screen', async () => {
      await page.goto('/dashboard');
      await openNew(page, 'Income');
      await dialog().getByLabel('Amount', { exact: true }).fill('1000');
      await dialog()
        .getByRole('group', { name: 'Allocation', exact: true })
        .getByRole('button', { name: 'Custom', exact: true })
        .click();
      await dialog().getByRole('button', { name: 'Add step' }).click();
      await expectNoHorizontalScroll(page);
      await expectNothingPastTheEdge(page, dialog());
      await dialog().getByRole('button', { name: 'Cancel', exact: true }).click();
    });

    test('opens the allocation templates dialog within the screen', async () => {
      await page.goto('/dashboard');
      await page
        .locator('section[aria-labelledby="h-budgets"]')
        .getByRole('button', { name: 'Actions for Main budget', exact: true })
        .click();
      await page.getByRole('menuitem', { name: 'Allocation templates', exact: true }).click();
      await expect(dialog().locator('button.tpl').first()).toBeVisible();
      await expectNoHorizontalScroll(page);
      await expectNothingPastTheEdge(page, dialog());
      await dialog().getByRole('button', { name: 'Cancel', exact: true }).click();
    });

    test('statistics does not scroll sideways', async () => {
      await page.goto('/statistics');
      await expect(page.getByRole('heading', { name: 'Fund balances' })).toBeVisible();
      await expectNoHorizontalScroll(page);
    });

    test('settings does not scroll sideways, nor does its delete-account dialog', async () => {
      await page.goto('/settings');
      await expect(page.getByLabel('Date format', { exact: true })).toBeVisible();
      await expectNoHorizontalScroll(page);

      await page.getByRole('button', { name: 'Delete account…' }).click();
      await expectNoHorizontalScroll(page);
      await expectNothingPastTheEdge(page, dialog());
      await dialog().getByRole('button', { name: 'Cancel', exact: true }).click();
    });
  });
});
