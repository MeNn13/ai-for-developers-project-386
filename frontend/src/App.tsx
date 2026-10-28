import { Badge, Container, Paper, Stack, Text, Title } from '@mantine/core'
import { useEffect, useState } from 'react'
import { getHealth } from './api/generated/booking'

type HealthState = 'loading' | 'ok' | 'error'

const healthLabel: Record<HealthState, string> = {
  loading: 'Проверяем API',
  ok: 'API доступен',
  error: 'API недоступен',
}

const healthColor: Record<HealthState, string> = {
  loading: 'gray',
  ok: 'green',
  error: 'red',
}

function App() {
  const [health, setHealth] = useState<HealthState>('loading')

  useEffect(() => {
    let cancelled = false

    getHealth()
      .then(({ status, data }) => {
        if (!cancelled) {
          setHealth(status === 200 && data.status === 'ok' ? 'ok' : 'error')
        }
      })
      .catch(() => {
        if (!cancelled) {
          setHealth('error')
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  return (
    <Container size="sm" py="xl">
      <Stack gap="lg">
        <div>
          <Title order={1}>Календарь звонков</Title>
          <Text c="dimmed">Каркас приложения: React, Vite и ASP.NET Core.</Text>
        </div>

        <Paper withBorder p="md" radius="md">
          <Stack gap="xs">
            <Text fw={500}>Состояние backend</Text>
            <Badge color={healthColor[health]} variant="light" size="lg">
              {healthLabel[health]}
            </Badge>
          </Stack>
        </Paper>
      </Stack>
    </Container>
  )
}

export default App
