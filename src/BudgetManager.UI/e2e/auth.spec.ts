import { test, expect } from '@playwright/test';
import { PASSWORD, registerUser } from './helpers';

test.describe('authentication', () => {
  test('registers, lands on the new-ledger wizard, signs out and back in', async ({ page }) => {
    const email = await registerUser(page);

    await expect(page).toHaveURL(/\/new-ledger/);
    await expect(page.getByRole('heading', { name: 'Create your ledger' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Cancel', exact: true })).toHaveCount(0);

    await page.getByRole('button', { name: 'Sign out' }).click();
    await page.waitForURL(/\/(login)?$/);

    await page.goto('/dashboard');
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();

    await page.getByLabel('Email', { exact: true }).fill(email);
    await page.getByLabel('Password', { exact: true }).fill(PASSWORD);
    await page.getByRole('button', { name: 'Sign in' }).click();
    await page.waitForURL(/\/(new-ledger|dashboard)/);
  });

  test('refuses a wrong password', async ({ page }) => {
    await page.goto('/');
    await page.getByLabel('Email', { exact: true }).fill('nobody@example.com');
    await page.getByLabel('Password', { exact: true }).fill('wrong-password');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('alert')).toHaveText('Wrong email or password.');
  });
});
