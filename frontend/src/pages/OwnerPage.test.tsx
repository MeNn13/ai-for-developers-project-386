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

function mockEmptyTypes() {
  vi.mocked(listMeetingTypes).mockResolvedValue({
    data: [],
    status: 200,
    headers: new Headers(),
  })
}

const ID_HINT = 'подставлено из названия — можно изменить'
const ID_TAKEN = 'Такой id уже занят — измените его'

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
    // Ручная правка id: сначала отцепить значение от подсказки, затем ввести своё.
    const idInput = screen.getByLabelText('Идентификатор')
    await user.clear(idInput)
    await user.type(idInput, 'znakomstvo')
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

  it('suggests id from the title and shows the hint while id is untouched', async () => {
    mockEmptyTypes()
    const user = userEvent.setup()
    renderOwnerPage()

    await user.type(screen.getByLabelText('Название'), 'Знакомство')

    expect(screen.getByLabelText('Идентификатор')).toHaveValue('znakomstvo')
    expect(screen.getByText(ID_HINT)).toBeVisible()
  })

  it('detaches id from the title after the first manual edit', async () => {
    mockEmptyTypes()
    const user = userEvent.setup()
    renderOwnerPage()

    const title = screen.getByLabelText('Название')
    const id = screen.getByLabelText('Идентификатор')

    await user.type(title, 'Знакомство')
    await user.type(id, '2026')

    expect(id).toHaveValue('znakomstvo2026')
    expect(screen.queryByText(ID_HINT)).not.toBeInTheDocument()

    // Последующие изменения названия больше не трогают id.
    await user.type(title, '!')

    expect(id).toHaveValue('znakomstvo2026')
  })

  it('re-couples id to the title when id is cleared to empty', async () => {
    mockEmptyTypes()
    const user = userEvent.setup()
    renderOwnerPage()

    const title = screen.getByLabelText('Название')
    const id = screen.getByLabelText('Идентификатор')

    await user.type(title, 'Знакомство')
    await user.type(id, '2026')
    await user.clear(id)

    expect(screen.getByText(ID_HINT)).toBeVisible()

    await user.clear(title)
    await user.type(title, 'Воркшоп')

    expect(id).toHaveValue('vorkshop')
  })

  it('clears the suggested id when the title is cleared', async () => {
    mockEmptyTypes()
    const user = userEvent.setup()
    renderOwnerPage()

    const title = screen.getByLabelText('Название')

    await user.type(title, 'Знакомство')
    expect(screen.getByLabelText('Идентификатор')).toHaveValue('znakomstvo')

    await user.clear(title)

    expect(screen.getByLabelText('Идентификатор')).toHaveValue('')
  })

  it('shows the collision warning for an existing id without adding a suffix and blocks submit', async () => {
    vi.mocked(listMeetingTypes).mockResolvedValue({
      data: [firstType],
      status: 200,
      headers: new Headers(),
    })
    const user = userEvent.setup()
    renderOwnerPage()

    await screen.findByRole('listitem')
    await user.type(screen.getByLabelText('Название'), 'Знакомство')

    // Подсказка не додумывает суффиксы вида «-2»: id остаётся как есть.
    expect(screen.getByLabelText('Идентификатор')).toHaveValue('znakomstvo')
    expect(screen.getByText(ID_TAKEN)).toBeVisible()
    expect(screen.getByRole('button', { name: 'Создать' })).toBeDisabled()
  })

  it('shows the same collision text when the server answers 409 meeting-type-exists', async () => {
    mockEmptyTypes()
    vi.mocked(createMeetingType).mockResolvedValue({
      data: {
        type: 'meeting-type-exists',
        title: 'Конфликт',
        status: 409,
        detail: 'Тип встречи с таким id уже существует.',
        instance: '/api/meeting-types',
      },
      status: 409,
      headers: new Headers(),
    })
    const user = userEvent.setup()
    renderOwnerPage()

    const title = screen.getByLabelText('Название')
    await user.type(title, 'Знакомство')
    await user.click(screen.getByRole('button', { name: 'Создать' }))

    expect(await screen.findByText(ID_TAKEN)).toBeVisible()

    // Остальные значения сохраняются.
    expect(title).toHaveValue('Знакомство')
    expect(screen.getByLabelText('Идентификатор')).toHaveValue('znakomstvo')
  })
})
