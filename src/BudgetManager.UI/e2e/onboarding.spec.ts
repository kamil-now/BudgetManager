import { test, expect, Page } from '@playwright/test';
import { PASSWORD, uniqueEmail } from './helpers';

test.describe.configure({ mode: 'serial' });

async function fillStepOne(page: Page, email = uniqueEmail('onboard')): Promise<string> {
  await page.goto('/register');
  await page.locator('#name').fill('Onboard Tester');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(PASSWORD);
  await page.locator('#confirmPassword').fill(PASSWORD);
  await page.locator('form button[type="submit"]').click();
  await expect(page.locator('#pf-default-currency')).toBeVisible();
  return email;
}

test.describe('onboarding', () => {
  test('suggests USD, US dates and English for an en-US browser', async ({ page }) => {
    await fillStepOne(page);

    await expect(page.getByText('Step 2 of 2')).toBeVisible();
    await expect(page.locator('#pf-language')).toHaveValue('en');
    await expect(page.locator('#pf-default-currency')).toHaveValue('USD');
    await expect(page.locator('#pf-date-format')).toHaveValue('MM/dd/yyyy');
    await expect(page.locator('.chips .chip')).toHaveText([/^\s*USD/, /^\s*EUR/, /^\s*PLN/]);
  });

  test('puts a newly chosen default currency first among the favorites', async ({ page }) => {
    await fillStepOne(page);

    await page.locator('#pf-default-currency').selectOption('EUR');
    await expect(page.locator('.chips .chip')).toHaveText([/^\s*EUR/, /^\s*USD/, /^\s*PLN/]);
  });

  test('applies the dark theme at once', async ({ page }) => {
    await fillStepOne(page);

    await page.getByRole('group', { name: 'Theme' }).getByRole('button', { name: 'Dark', exact: true }).click();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  });

  test('switching the language keeps what was typed in step 1', async ({ page }) => {
    const email = await fillStepOne(page);

    await page.locator('#pf-language').selectOption('pl');
    await expect(page.locator('html')).toHaveAttribute('lang', 'pl');
    await expect(page.getByText('Krok 2 z 2')).toBeVisible();

    await page.getByRole('button', { name: 'Wstecz' }).click();
    await expect(page.locator('#name')).toHaveValue('Onboard Tester');
    await expect(page.locator('#email')).toHaveValue(email);
    await expect(page.locator('#password')).toHaveValue(PASSWORD);
  });

  test('a duplicate email returns to step 1 with an error', async ({ page, request }) => {
    const email = uniqueEmail('dupe');
    const created = await request.post('/api/auth/register', {
      data: { email, password: PASSWORD, name: null },
    });
    expect(created.ok()).toBeTruthy();

    await fillStepOne(page, email);
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByRole('alert')).toHaveText('An account with this email already exists.');
    await expect(page.locator('#email')).toHaveValue(email);
    await expect(page.getByText('Step 1 of 2')).toBeVisible();
  });
});

test.describe('onboarding in a Polish browser', () => {
  test.use({ locale: 'pl-PL' });

  test('suggests PLN, Polish dates and the Polish language', async ({ page }) => {
    await fillStepOne(page);

    await expect(page.locator('#pf-language')).toHaveValue('pl');
    await expect(page.locator('#pf-default-currency')).toHaveValue('PLN');
    await expect(page.locator('#pf-date-format')).toHaveValue('dd.MM.yyyy');
    await expect(page.locator('.chips .chip')).toHaveText([/^\s*PLN/, /^\s*EUR/, /^\s*USD/]);
  });
});
