import {
  Alert,
  Anchor,
  Button,
  Card,
  Container,
  Paper,
  SimpleGrid,
  Skeleton,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { DatePicker, DatesProvider } from '@mantine/dates'
import dayjs from 'dayjs'
import 'dayjs/locale/ru'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { listMeetingTypes, listSlots } from '../api/generated/booking'
import type { MeetingType, Slot, SlotDay, SlotsResponse } from '../api/generated/booking'

type TypesState =
  { status: 'loading' } | { status: 'error' } | { status: 'ready'; types: MeetingType[] }

type SlotsState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ready'; response: SlotsResponse }

async function fetchTypes(): Promise<TypesState> {
  try {
    const response = await listMeetingTypes()
    if (response.status === 200) {
      return { status: 'ready', types: response.data }
    }

    return { status: 'error' }
  } catch {
    return { status: 'error' }
  }
}

// Окно записи начинается сегодня по Москве, поэтому первый день ответа — сегодня.
function isWorkingDay(date: string): boolean {
  const weekday = dayjs(date).day()

  return weekday !== 0 && weekday !== 6
}

function defaultDay(days: SlotDay[]): string | null {
  const today = days[0]?.date
  if (!today) {
    return null
  }

  if (isWorkingDay(today)) {
    return today
  }

  return days.find((day) => isWorkingDay(day.date))?.date ?? null
}

function hasFreeSlot(response: SlotsResponse): boolean {
  return response.days.some((day) => day.slots.some((slot) => slot.available))
}

// Время форматируется по timezone из ответа, а не по часовому поясу браузера.
function formatTime(value: string, timeZone: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    timeZone,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(value))
}

export function BookingPage() {
  const [typesState, setTypesState] = useState<TypesState>({ status: 'loading' })
  const [selectedType, setSelectedType] = useState<MeetingType | null>(null)
  const [slotsState, setSlotsState] = useState<SlotsState>({ status: 'idle' })
  const [selectedDay, setSelectedDay] = useState<string | null>(null)
  const [visibleMonth, setVisibleMonth] = useState<string | null>(null)
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null)

  async function loadSlots(type: MeetingType) {
    setSlotsState({ status: 'loading' })
    setSelectedSlot(null)

    try {
      const response = await listSlots({ meetingTypeId: type.id })
      if (response.status === 200) {
        const day = defaultDay(response.data.days)
        setSlotsState({ status: 'ready', response: response.data })
        setSelectedDay(day)
        setVisibleMonth(day)
        return
      }

      setSlotsState({ status: 'error' })
    } catch {
      setSlotsState({ status: 'error' })
    }
  }

  useEffect(() => {
    void fetchTypes().then(setTypesState)
  }, [])

  function refreshTypes() {
    setTypesState({ status: 'loading' })
    void fetchTypes().then(setTypesState)
  }

  function selectType(type: MeetingType) {
    setSelectedType(type)
    setSelectedDay(null)
    void loadSlots(type)
  }

  function changeType() {
    setSelectedType(null)
    setSlotsState({ status: 'idle' })
    setSelectedDay(null)
    setVisibleMonth(null)
    setSelectedSlot(null)
  }

  function selectDay(day: string | null) {
    setSelectedDay(day)
    setSelectedSlot(null)
  }

  function refreshSlots() {
    if (selectedType) {
      void loadSlots(selectedType)
    }
  }

  const bookingWindow = slotsState.status === 'ready' ? slotsState.response.window : null
  const today = bookingWindow?.from ?? null
  const timeZone = slotsState.status === 'ready' ? slotsState.response.timezone : null
  const calendarActive = selectedType !== null && slotsState.status === 'ready'
  const daySlots =
    slotsState.status === 'ready'
      ? (slotsState.response.days.find((day) => day.date === selectedDay)?.slots ?? [])
      : []

  return (
    <Container size="lg" py="xl">
      <Stack gap="lg">
        <Title order={1}>Бронирование</Title>

        <SimpleGrid cols={{ base: 1, md: 3 }} spacing="lg">
          <Paper withBorder p="md">
            <Stack gap="md">
              <Title order={2}>Типы встреч</Title>

              {typesState.status === 'loading' && (
                <Stack gap="xs" role="status" aria-label="Загрузка типов встреч">
                  <Skeleton height={72} radius="sm" />
                  <Skeleton height={72} radius="sm" />
                </Stack>
              )}

              {typesState.status === 'error' && (
                <Alert color="red" withCloseButton={false}>
                  <Stack gap="xs" align="flex-start">
                    <Text>Не удалось загрузить типы встреч</Text>
                    <Button variant="outline" size="xs" onClick={refreshTypes}>
                      Повторить
                    </Button>
                  </Stack>
                </Alert>
              )}

              {typesState.status === 'ready' && typesState.types.length === 0 && (
                <Alert color="blue" withCloseButton={false}>
                  <Stack gap="xs" align="flex-start">
                    <Text>Типы встреч ещё не созданы</Text>
                    <Anchor component={Link} to="/owner" size="sm">
                      Страница владельца
                    </Anchor>
                    <Button variant="outline" size="xs" onClick={refreshTypes}>
                      Обновить
                    </Button>
                  </Stack>
                </Alert>
              )}

              {typesState.status === 'ready' && typesState.types.length > 0 && !selectedType && (
                <Stack gap="sm">
                  {typesState.types.map((type) => (
                    <Card
                      key={type.id}
                      component="button"
                      type="button"
                      withBorder
                      p="md"
                      onClick={() => selectType(type)}
                      style={{ textAlign: 'left', cursor: 'pointer' }}
                    >
                      <Stack gap={4}>
                        <Text fw={600}>{type.title}</Text>
                        {type.description && (
                          <Text fz="sm" c="dimmed">
                            {type.description}
                          </Text>
                        )}
                        <Text fz="sm">{type.durationMinutes} мин</Text>
                      </Stack>
                    </Card>
                  ))}
                </Stack>
              )}

              {selectedType && (
                <Paper withBorder p="md">
                  <Stack gap="xs">
                    <Title order={3}>{selectedType.title}</Title>
                    {selectedType.description && (
                      <Text fz="sm" c="dimmed">
                        {selectedType.description}
                      </Text>
                    )}
                    <Text fz="sm">{selectedType.durationMinutes} мин</Text>
                    <Text fz="sm" c="dimmed">
                      Время указано по Москве (UTC+3)
                    </Text>
                    <Button variant="light" onClick={changeType}>
                      Изменить
                    </Button>
                  </Stack>
                </Paper>
              )}
            </Stack>
          </Paper>

          <Paper withBorder p="md">
            <Stack gap="md">
              {slotsState.status === 'ready' && !hasFreeSlot(slotsState.response) && (
                <Alert color="blue" withCloseButton={false}>
                  <Stack gap="xs" align="flex-start">
                    <Text>В ближайшие 14 дней свободных слотов нет</Text>
                    <Button variant="outline" size="xs" onClick={refreshSlots}>
                      Обновить
                    </Button>
                  </Stack>
                </Alert>
              )}

              <DatesProvider settings={{ locale: 'ru', firstDayOfWeek: 1 }}>
                <DatePicker
                  value={selectedDay}
                  onChange={selectDay}
                  date={visibleMonth ?? undefined}
                  onDateChange={setVisibleMonth}
                  minDate={bookingWindow?.from}
                  maxDate={bookingWindow?.to}
                  excludeDate={(date) => !calendarActive || !isWorkingDay(date)}
                  firstDayOfWeek={1}
                  highlightToday={false}
                  renderDay={(date) => (
                    <span
                      style={{
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        gap: 2,
                      }}
                    >
                      <span>{dayjs(date).date()}</span>
                      {date === today && (
                        <span style={{ fontSize: 9, lineHeight: 1 }}>сегодня</span>
                      )}
                    </span>
                  )}
                />
              </DatesProvider>
            </Stack>
          </Paper>

          <Paper withBorder p="md">
            <Stack gap="md">
              <Title order={2}>Слоты</Title>

              {!selectedType && typesState.status === 'ready' && (
                <Text c="dimmed">Выберите тип встречи</Text>
              )}

              {selectedType && slotsState.status === 'loading' && (
                <Stack gap="xs" role="status" aria-label="Загрузка слотов">
                  <Skeleton height={36} radius="sm" />
                  <Skeleton height={36} radius="sm" />
                  <Skeleton height={36} radius="sm" />
                </Stack>
              )}

              {selectedType && slotsState.status === 'error' && (
                <Alert color="red" withCloseButton={false}>
                  <Stack gap="xs" align="flex-start">
                    <Text>Не удалось загрузить слоты</Text>
                    <Button
                      variant="outline"
                      size="xs"
                      onClick={() => void loadSlots(selectedType)}
                    >
                      Повторить
                    </Button>
                  </Stack>
                </Alert>
              )}

              {slotsState.status === 'ready' && (
                <Stack gap="sm">
                  {!daySlots.some((slot) => slot.available) && (
                    <Text c="dimmed">На этот день свободных слотов нет</Text>
                  )}

                  {daySlots.map((slot) => {
                    const label = timeZone
                      ? `${formatTime(slot.start, timeZone)}–${formatTime(slot.end, timeZone)}`
                      : slot.start

                    return (
                      <Button
                        key={slot.start}
                        variant={selectedSlot?.start === slot.start ? 'filled' : 'default'}
                        disabled={!slot.available}
                        aria-pressed={selectedSlot?.start === slot.start}
                        aria-label={slot.available ? label : `${label} занято`}
                        onClick={() => setSelectedSlot(slot)}
                        justify="space-between"
                        fullWidth
                      >
                        <span>{label}</span>
                        {!slot.available && <span>занято</span>}
                      </Button>
                    )
                  })}
                </Stack>
              )}
            </Stack>
          </Paper>
        </SimpleGrid>
      </Stack>
    </Container>
  )
}
