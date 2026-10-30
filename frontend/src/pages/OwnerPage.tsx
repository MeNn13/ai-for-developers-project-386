import {
  Alert,
  Button,
  Container,
  Group,
  List,
  NumberInput,
  Skeleton,
  Stack,
  Text,
  TextInput,
  Textarea,
  Title,
} from '@mantine/core'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { createMeetingType, listMeetingTypes } from '../api/generated/booking'
import type { MeetingType, MeetingTypeCreateRequest } from '../api/generated/booking'
import { transliterate } from '../lib/transliterate'

// Подсказка под полем id, пока id не правили руками (спека #23, решение #17).
const ID_HINT = 'подставлено из названия — можно изменить'

// Ограничения зеркалят контракт (contract/main.tsp): длительность 15–480 минут шагом 5,
// поля формы по умолчанию: длительность 30, остальные пустые.
const DURATION_MIN_MINUTES = 15
const DURATION_MAX_MINUTES = 480
const DURATION_STEP_MINUTES = 5
const DURATION_DEFAULT_MINUTES = 30
const TITLE_MAX_LENGTH = 120
const DESCRIPTION_MAX_LENGTH = 500
const ID_MAX_LENGTH = 64

type TypesState =
  | { status: 'loading'; types: [] }
  | { status: 'error'; types: [] }
  | { status: 'ready'; types: MeetingType[] }

async function fetchTypes(): Promise<TypesState> {
  try {
    const response = await listMeetingTypes()
    if (response.status === 200) {
      return { status: 'ready', types: response.data }
    }
    return { status: 'error', types: [] }
  } catch {
    return { status: 'error', types: [] }
  }
}

export function OwnerPage() {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [duration, setDuration] = useState<number | string>(DURATION_DEFAULT_MINUTES)
  const [id, setId] = useState('')
  // id отвязан от названия после первой ручной правки; пустое значение возвращает связь.
  const [idDetached, setIdDetached] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[] | undefined>>({})
  const [createError, setCreateError] = useState<string | null>(null)
  const [created, setCreated] = useState(false)
  const [saving, setSaving] = useState(false)
  const [typesState, setTypesState] = useState<TypesState>({ status: 'loading', types: [] })

  useEffect(() => {
    fetchTypes().then(setTypesState)
  }, [])

  function handleTitleChange(value: string) {
    setTitle(value)
    if (!idDetached) {
      setId(transliterate(value))
    }
  }

  function handleIdChange(value: string) {
    setId(value)
    setIdDetached(value !== '')
  }

  async function createType() {
    setSaving(true)
    setCreated(false)
    setCreateError(null)
    setFieldErrors({})

    const request: MeetingTypeCreateRequest = {
      id,
      title,
      description,
      durationMinutes: Number(duration),
    }

    try {
      const response = await createMeetingType(request)
      if (response.status === 201) {
        setTitle('')
        setDescription('')
        setDuration(DURATION_DEFAULT_MINUTES)
        setId('')
        setIdDetached(false)
        setCreated(true)
        setTypesState(await fetchTypes())
        return
      }

      if (response.status === 400) {
        setFieldErrors(response.data.errors ?? {})
        return
      }

      if (response.status === 409) {
        setFieldErrors({ id: [response.data.detail] })
        return
      }

      setCreateError('Не удалось создать тип встречи')
    } catch {
      setCreateError('Не удалось создать тип встречи')
    } finally {
      setSaving(false)
    }
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    void createType()
  }

  return (
    <Container size="lg" py="xl">
      <Stack gap="xl">
        <Title order={1}>Страница владельца</Title>

        <form onSubmit={submit}>
          <Stack gap="md">
            <Title order={2}>Новый тип встречи</Title>

            {created && (
              <Alert color="green" withCloseButton={false}>
                Тип встречи создан
              </Alert>
            )}

            {createError && (
              <Alert color="red" withCloseButton={false}>
                {createError}
              </Alert>
            )}

            <TextInput
              label="Название"
              value={title}
              onChange={(event) => handleTitleChange(event.currentTarget.value)}
              maxLength={TITLE_MAX_LENGTH}
              error={fieldErrors.title?.[0]}
            />

            <Textarea
              label="Описание"
              value={description}
              onChange={(event) => setDescription(event.currentTarget.value)}
              maxLength={DESCRIPTION_MAX_LENGTH}
              error={fieldErrors.description?.[0]}
            />

            <NumberInput
              label="Длительность, мин"
              value={duration}
              onChange={setDuration}
              min={DURATION_MIN_MINUTES}
              max={DURATION_MAX_MINUTES}
              step={DURATION_STEP_MINUTES}
              error={fieldErrors.durationMinutes?.[0]}
            />

            <TextInput
              label="Идентификатор"
              value={id}
              onChange={(event) => handleIdChange(event.currentTarget.value)}
              maxLength={ID_MAX_LENGTH}
              description={idDetached ? undefined : ID_HINT}
              inputWrapperOrder={['label', 'input', 'description', 'error']}
              error={fieldErrors.id?.[0]}
            />

            <Group>
              <Button type="submit" loading={saving}>
                Создать
              </Button>
            </Group>
          </Stack>
        </form>

        <Stack gap="md">
          <Title order={2}>Типы встреч</Title>

          {typesState.status === 'loading' && (
            <Stack gap="xs">
              <Skeleton height={22} radius="sm" />
              <Skeleton height={22} radius="sm" width="80%" />
            </Stack>
          )}

          {typesState.status === 'error' && (
            <Alert color="red" withCloseButton={false}>
              Не удалось загрузить типы встреч{' '}
              <Button variant="outline" size="xs" onClick={() => fetchTypes().then(setTypesState)}>
                Повторить
              </Button>
            </Alert>
          )}

          {typesState.status === 'ready' && typesState.types.length === 0 && (
            <Stack gap={0}>
              <Text c="dimmed">Типы встреч ещё не созданы</Text>
              <Text c="dimmed" fz="sm">
                Заполните форму «Новый тип встречи», чтобы создать первый тип.
              </Text>
            </Stack>
          )}

          {typesState.status === 'ready' && typesState.types.length > 0 && (
            <List withPadding={false}>
              {typesState.types.map((type) => (
                <List.Item key={type.id}>
                  <Group justify="space-between" gap="md">
                    <Stack gap={0}>
                      <Text fw={600}>{type.title}</Text>
                      <Text fz="sm" c="dimmed">
                        {type.id}
                      </Text>
                    </Stack>
                    <Text fz="sm">{type.durationMinutes} мин</Text>
                  </Group>
                </List.Item>
              ))}
            </List>
          )}
        </Stack>
      </Stack>
    </Container>
  )
}
