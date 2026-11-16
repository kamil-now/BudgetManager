import { test, expect, Page } from '@playwright/test';
import { api, day, expectNoHorizontalScroll, registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

interface LedgerShape {
  accounts: { id: string; name: string }[];
  budgets: { id: string; name: string; funds: { id: string; name: string }[] }[];
}

test.describe('statistics', () => {
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    await registerAndCreateLedger(page, { zeroBalances: true });

    const ledgerId = await page.evaluate(() => localStorage.getItem('budget-manager.ledgerId'));
    const ledger = await api<LedgerShape>(page, 'GET', `/api/ledgers/${ledgerId}`);
    const main = ledger.accounts.find((a) => a.name === 'Main Account')?.id ?? '';
    const budget = ledger.budgets.find((b) => b.name === 'Main budget');
    const food = budget?.funds.find((f) => f.name === 'Food')?.id ?? '';
    const templates = await api<{ id: string }[]>(
      page,
      'GET',
      `/api/budgets/${budget?.id}/allocationTemplates`,
    );
    const date = `${day(0)}T00:00:00.000Z`;

    await api(page, 'POST', `/api/accounts/${main}/transaction`, {
      accountId: main,
      value: { amount: 3000, currency: 'EUR' },
      date,
      title: 'Salary Stats E2E',
      allocation: { templateId: templates[0].id },
    });
    await api(page, 'POST', `/api/accounts/${main}/transaction`, {
      accountId: main,
      value: { amount: -50, currency: 'EUR' },
      date,
      title: 'Lunch Stats E2E',
      fundId: food,
    });
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('shows the fund chart and the fund balances', async () => {
    await page.goto('/statistics');

    await expect(page.getByRole('heading', { name: 'By fund', exact: true })).toBeVisible();
    const balances = page.getByRole('region', { name: 'Fund balances' });
    await expect(balances).toBeVisible();
    await expect(balances.getByRole('rowheader', { name: 'Main budget' })).toBeVisible();

    const food = balances.getByRole('row').filter({ has: page.getByRole('rowheader', { name: 'Food', exact: true }) });
    await expect(food).toContainText('+600.00');
    await expect(food).toContainText('−50.00');
    await expect(food).toContainText('550.00');
    await expect(food).toContainText('EUR');

    await expect(page.getByRole('main').getByText(/[€$£]|zł/)).toHaveCount(0);
  });

  test('has the same fund figures in the chart description', async () => {
    await page.goto('/statistics');
    const chart = page.getByRole('img', { name: /^By fund\./ });
    await expect(chart).toHaveAttribute('aria-label', /Food.*allocated.*600\.00.*EUR.*spent/);
  });

  test('does not scroll sideways', async () => {
    await page.goto('/statistics');
    await expect(page.getByRole('heading', { name: 'Fund balances' })).toBeVisible();
    await expectNoHorizontalScroll(page);
  });
});
