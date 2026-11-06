import { expect, test } from '@playwright/test'

test('owner creates a meeting type and sees the guest booking', async ({ page }) => {
  await page.goto('/owner')
  await page.getByLabel('Название', { exact: true }).fill('Browser consultation')
  await page.getByLabel('Описание', { exact: true }).fill('A real browser booking')
  await page.getByLabel('Длительность, мин').fill('30')
  await page.getByRole('button', { name: 'Создать', exact: true }).click()
  await expect(page.getByText('Тип встречи создан', { exact: true })).toBeVisible()

  await page.goto('/book')
  await page.getByRole('button', { name: /Browser consultation/ }).click()
  // The calendar exposes the current booking window. Prefer its last working
  // day so even a run near today's closing time chooses a future slot.
  await expect(page.getByRole('status', { name: 'Загрузка слотов' })).toHaveCount(0)
  const nextMonth = page.getByRole('button', { name: 'Следующий месяц' })
  if (await nextMonth.isEnabled()) {
    await nextMonth.click()
  }
  const days = page.getByRole('table').getByRole('button')
  let day = ''
  for (const candidate of (await days.all()).reverse()) {
    if (await candidate.isEnabled()) {
      day = (await candidate.getAttribute('aria-label')) ?? ''
      await candidate.click()
      break
    }
  }
  const slot = page.getByRole('button', { name: /^\d{2}:\d{2}–\d{2}:\d{2}$/ }).first()
  await expect(slot).toBeEnabled()
  const time = await slot.innerText()
  await slot.click()
  await page.getByLabel('Ваше имя').fill('Browser Guest')
  await page.getByRole('button', { name: 'Продолжить', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Встреча забронирована' })).toBeVisible()
  await expect(page.getByText('Browser consultation', { exact: true })).toBeVisible()
  await expect(page.getByText('Browser Guest', { exact: true })).toBeVisible()
  const confirmation = page.getByText(/\(МСК\)$/)
  await expect(confirmation).toContainText(time)
  expect(day).toBeTruthy()
  await expect(confirmation).toContainText(day.replace(/\s+\d{4}$/, ''))
  const when = (await confirmation.innerText()).replace(' (МСК)', '')

  await page.goto('/owner')
  const meeting = page.getByRole('row').filter({ hasText: 'Browser Guest' })
  await expect(meeting).toContainText('Browser consultation')
  await expect(meeting).toContainText('30 мин')
  await expect(meeting).toContainText(when)
  await expect(meeting).toHaveCount(1)
})
