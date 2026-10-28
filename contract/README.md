# Контракт API

Источник истины для API — TypeSpec. Из него генерируются OpenAPI и TypeScript-клиент.

```bash
mise run generate
```

Результат:

- `openapi/openapi.yaml` — OpenAPI 3.0;
- `../frontend/src/api/generated/booking.ts` — клиент для фронтенда.

Сгенерированные файлы вручную не редактируются: меняйте `main.tsp` и запускайте генерацию.
