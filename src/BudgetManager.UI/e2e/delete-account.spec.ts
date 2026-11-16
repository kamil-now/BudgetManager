import { test, expect } from '@playwright/test';
import { PASSWORD, registerAndCreateLedger } from './helpers';

test.describe('delete account', () => {
  test('refuses a wrong password, then deletes the account and everything local', async ({ page }) => {
    test.setTimeout(120_000);
    const email = await registerAndCreateLedger(page);
    // Deleting the account and signing in again count against the auth rate limit (10 per 60s),
    // which the earlier specs' registrations may have used up.
    await page.waitForTimeout(20_000);

    await page.goto('/settings');
    await page.getByRole('button', { name: 'Delete account…' }).click();
    const dialog = page.getByRole('dialog', { name: 'Delete your account?' });
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText('This cannot be undone.');

    await dialog.getByLabel('Enter your password to confirm').fill('not-the-password');
    await dialog.getByRole('button', { name: 'Delete account', exact: true }).click();
    await expect(dialog.getByRole('alert')).toHaveText('The password is not correct.');
    await expect(dialog).toBeVisible();

    await dialog.getByLabel('Enter your password to confirm').fill(PASSWORD);
    await dialog.getByRole('button', { name: 'Delete account', exact: true }).click();

    await page.waitForURL(/\/$/);
    await page.goto('/dashboard');
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
    const stored = await page.evaluate(() => ({
      preferences: localStorage.getItem('budget-manager.preferences'),
      ledgerId: localStorage.getItem('budget-manager.ledgerId'),
    }));
    expect(stored.preferences).toBeNull();
    expect(stored.ledgerId).toBeNull();

    await page.getByLabel('Email', { exact: true }).fill(email);
    await page.getByLabel('Password', { exact: true }).fill(PASSWORD);
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page.getByRole('alert')).toHaveText('Wrong email or password.');
  });
});
