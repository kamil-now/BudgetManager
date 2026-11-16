import { test, expect, Locator, Page } from '@playwright/test';
import { registerAndCreateLedger } from './helpers';

test.describe.configure({ mode: 'serial' });

test.describe('allocation templates', () => {
  let page: Page;
  const dialog = (): Locator => page.getByRole('dialog');
  const templates = (): Locator => dialog().getByRole('list', { name: 'Templates' }).getByRole('listitem');
  const template = (name: string): Locator =>
    dialog().locator('button.tpl').filter({ has: page.locator('.tpl-name', { hasText: name }) });
  const fundOf = (line: number): Locator =>
    dialog().getByLabel(`Fund ${line}`, { exact: true });
  const lineType = (line: number, type: 'Fixed' | 'Percent'): Locator =>
    dialog()
      .getByRole('group', { name: `Line ${line} type` })
      .getByRole('button', { name: type, exact: true });
  const amount = (line: number): Locator =>
    dialog().getByLabel(`Fund ${line} amount`, { exact: true });
  const percent = (line: number): Locator =>
    dialog().getByLabel(`Fund ${line} percent`, { exact: true });
  const takes = (line: number): Locator => dialog().locator('.takes').nth(line - 1);
  const addStep = (): Promise<void> =>
    dialog().getByRole('button', { name: 'Add step', exact: true }).click();
  const save = (): Promise<void> =>
    dialog().getByRole('button', { name: 'Save templates', exact: true }).click();
  const cancel = async (): Promise<void> => {
    await dialog().getByRole('button', { name: 'Cancel', exact: true }).click();
    await expect(dialog()).toBeHidden();
  };

  async function openEditor(): Promise<void> {
    await page.goto('/dashboard');
    await page
      .locator('section[aria-labelledby="h-budgets"]')
      .getByRole('button', { name: 'Actions for Main budget', exact: true })
      .click();
    await page.getByRole('menuitem', { name: 'Allocation templates', exact: true }).click();
    await expect(dialog()).toBeVisible();
    await expect(template('Default')).toBeVisible();
  }

  test.beforeAll(async ({ browser }) => {
    page = await browser.newPage();
    await registerAndCreateLedger(page);
  });

  test.afterAll(async () => {
    await page.close();
  });

  test('shows the Default EUR template of the default budget', async () => {
    await openEditor();

    await expect(templates()).toHaveCount(1);
    await expect(template('Default')).toContainText('EUR');
    await expect(template('Default')).toHaveAttribute('aria-pressed', 'true');
    await expect(dialog().getByLabel('Name', { exact: true })).toHaveValue('Default');
    await expect(dialog().getByText('The currency cannot be changed once the template is saved.')).toBeVisible();
    await expect(amount(1)).toHaveValue('800');
    await expect(amount(2)).toHaveValue('200');
    await expect(amount(3)).toHaveValue('600');
    await expect(percent(4)).toHaveValue('20');
    await expect(percent(5)).toHaveValue('100');
    await expect(dialog().locator('.steps-min')).toContainText(/Minimum 1,600\.0[01]\s?EUR/);
    await cancel();
  });

  test('previews how a sample amount runs through the steps', async () => {
    await openEditor();
    await dialog().getByLabel('Try an amount', { exact: true }).fill('3000');

    await expect(takes(1)).toContainText('800.00');
    await expect(takes(4)).toContainText('280.00');
    await expect(takes(5)).toContainText('1,120.00');
    await expect(dialog().locator('.steps-left')).toContainText('0.00');

    await dialog().getByRole('button', { name: 'Remove line 2', exact: true }).click();
    await expect(dialog().getByLabel('Fund 5 amount')).toHaveCount(0);
    await expect(takes(3)).toContainText('320.00');
    await expect(takes(4)).toContainText('1,280.00');
    await cancel();
  });

  test('saves an edited template', async () => {
    await openEditor();
    await percent(5).fill('50');
    await dialog().getByLabel('Try an amount', { exact: true }).fill('3000');
    await expect(takes(5)).toContainText('560.00');
    await expect(dialog().locator('.steps-left')).toContainText('560.00');

    await save();
    await expect(dialog()).toBeHidden();

    await openEditor();
    await expect(percent(5)).toHaveValue('50');
    await cancel();
  });

  test('rejects an invalid percent', async () => {
    await openEditor();
    await percent(4).fill('120');
    await expect(dialog().getByText('A percent must be above 0 and at most 100.')).toBeVisible();

    await save();
    await expect(dialog()).toBeVisible();
    await expect(dialog().getByRole('alert')).toBeVisible();
    await cancel();
  });

  test('reorders the steps', async () => {
    await openEditor();
    await expect(fundOf(1).locator('option:checked')).toHaveText('Rent');

    await dialog().getByRole('button', { name: 'Move line 2 up', exact: true }).click();
    await expect(fundOf(1).locator('option:checked')).toHaveText('Utilities');
    await expect(dialog().getByRole('button', { name: 'Move line 1 up', exact: true })).toBeDisabled();
    await save();
    await expect(dialog()).toBeHidden();

    await openEditor();
    await expect(fundOf(1).locator('option:checked')).toHaveText('Utilities');
    await expect(fundOf(2).locator('option:checked')).toHaveText('Rent');
    await cancel();
  });

  test('adds a second, named template in another currency', async () => {
    await openEditor();
    await dialog().getByRole('button', { name: 'New template', exact: true }).click();
    await dialog().getByLabel('Name', { exact: true }).fill('Dollar plan');
    await dialog().getByLabel('Currency', { exact: true }).selectOption('USD');

    await save();
    await expect(dialog().getByRole('alert')).toContainText('Add at least one line.');

    if ((await dialog().getByLabel('Fund 1', { exact: true }).count()) === 0) await addStep();
    await fundOf(1).selectOption({ label: 'Food' });
    await amount(1).fill('100');
    await addStep();
    await fundOf(2).selectOption({ label: 'Food' });
    await lineType(2, 'Percent').click();
    await percent(2).fill('10');
    await dialog().getByLabel('Try an amount', { exact: true }).fill('300');
    await expect(takes(2)).toContainText('20.00');
    await expect(dialog().locator('.steps-left')).toContainText('180.00');
    await expect(dialog().locator('.steps-min')).toContainText(/Minimum 100\.0[01]\s?USD/);
    await save();
    await expect(dialog()).toBeHidden();

    await openEditor();
    await expect(templates()).toHaveCount(2);
    await template('Dollar plan').click();
    await expect(template('Dollar plan')).toContainText('USD');
    await expect(amount(1)).toHaveValue('100');
    await expect(percent(2)).toHaveValue('10');
    await cancel();
  });

  test('removes a template after a confirmation', async () => {
    await openEditor();
    await template('Dollar plan').click();

    const remove = dialog().getByRole('button', { name: 'Remove template', exact: true });
    await remove.click();
    await dialog().getByRole('button', { name: 'Keep it', exact: true }).click();
    await expect(template('Dollar plan')).toBeVisible();

    await remove.click();
    await dialog().getByRole('button', { name: 'Yes, remove', exact: true }).click();
    await expect(template('Dollar plan')).toHaveCount(0);
    await expect(templates()).toHaveCount(1);
    await cancel();

    await openEditor();
    await expect(templates()).toHaveCount(1);
    await cancel();
  });
});
