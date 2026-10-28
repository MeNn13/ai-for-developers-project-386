import { Container, Stack, Text, Title } from '@mantine/core'

export function OwnerPage() {
  return (
    <Container size="sm" py="xl">
      <Stack gap="md">
        <Title order={1}>Страница владельца</Title>
        <Text c="dimmed">Раздел в разработке</Text>
      </Stack>
    </Container>
  )
}
