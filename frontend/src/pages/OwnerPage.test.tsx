import { MantineProvider } from '@mantine/core'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { OwnerPage } from './OwnerPage'

vi.mock('../api/generated/booking', () => ({
  listMeetingTypes: vi.fn(),
  createMeetingType: vi.fn(),
}))

import { createMeetingType, listMeetingTypes } from '../api/generated/booking'
import type { MeetingType } from '../api/generated/booking'

function renderOwnerPage() {
  return render(
    <MantineProvider defaultColorScheme="light">
      <OwnerPage />
    </MantineProvider>,
  )
}

const firstType: MeetingType = {
  id: 'znakomstvo',
  title: 'Знакомство',
  description: 'Короткий первый созвон.',
  durationMinutes: 30,
}

const secondType: MeetingType = {
  id: '-razbor-1-1',
  title: 'Разбор 1:1',
  description: '',
  durationMinutes: 45,
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('owner page', () => {
  it('lists created meeting types in creation order', async () => {
    vi.mocked(listMeetingTypes).mockResolvedValue({
      data: [firstType, secondType],
      status: 200,
      headers: new Headers(),
    })

    renderOwnerPage()

    const items = await screen.findAllByRole('listitem')
    expect(items).toHaveLength(2)

    expect(items[0]).toHaveTextContent('Знакомство')
    expect(items[0]).toHaveTextContent('znakomstvo')
    expect(items[0]).toHaveTextContent('30 мин')
    expect(items[1]).toHaveTextContent('Разбор 1:1')
    expect(items[1]).toHaveTextContent('-razbor-1-1')
    expect(items[1]).toHaveTextContent('45 мин')
  })

  it('creates a meeting type with manual id and refreshes the list', async () => {
    const user = userEvent.setup()

    vi.mocked(listMeetingTypes)
      .mockResolvedValueOnce({ data: [], status: 200, headers: new Headers() })
      .mockResolvedValueOnce({ data: [firstType], status: 200, headers: new Headers() })
    vi.mocked(createMeetingType).mockResolvedValue({
      data: firstType,
      status: 201,
      headers: new Headers(),
    })

    renderOwnerPage()

    expect(await screen.findByText('Типы встреч ещё не созданы')).toBeVisible()

    await user.type(screen.getByLabelText('Название'), 'Знакомство')
    await user.type(screen.getByLabelText('Идентификатор'), 'znakomstvo')
    await user.click(screen.getByRole('button', { name: 'Создать' }))

    await waitFor(() =>
      expect(createMeetingType).toHaveBeenCalledWith({
        id: 'znakomstvo',
        title: 'Знакомство',
        description: '',
        durationMinutes: 30,
      }),
    )

    expect(await screen.findByText('Тип встречи создан')).toBeVisible()
    expect(listMeetingTypes).toHaveBeenCalledTimes(2)
    expect(await screen.findByText('Знакомство')).toBeVisible()

    // После успешного создания форма возвращается к значениям по умолчанию.
    expect(screen.getByLabelText('Название')).toHaveValue('')
    expect(screen.getByLabelText('Идентификатор')).toHaveValue('')
    expect(screen.getByLabelText('Длительность, мин')).toHaveValue('30')
  })
})
