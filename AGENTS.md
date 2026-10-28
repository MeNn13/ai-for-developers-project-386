# AGENTS.md

## Проект

Сервис бронирования календаря: один владелец, гости без регистрации, данные в памяти.
Часовой пояс — `Europe/Moscow`, рабочее время — Пн–Пт 10:00–18:00, окно записи — 14 дней.
Полный пользовательский сценарий — в `README.md`.

- Backend: ASP.NET Core 10, Minimal API.
- Frontend: React 19, TypeScript, Vite 8, Mantine 9.
- Контракт: TypeSpec → OpenAPI 3.0 → TypeScript-клиент (Orval).

## Команды

- `mise trust` — разрешить конфигурацию mise.
- `mise install` — установить версии инструментов из `mise.toml`.
- `mise run install` — восстановить зависимости .NET и npm.
- `mise run dev` — запустить API на `:5000` и Vite на `:5173`.
- `mise run check` — линтеры, типы, тесты и сборка. Запускать перед коммитом.
- `mise run test` — тесты backend.
- `mise run lint` — линтеры.
- `mise run build` — production-сборка backend и frontend.
- `mise run generate` — TypeSpec → OpenAPI → клиент.
- `mise run hooks:install` — установить git-хуки.
- `mise daemons status` / `logs` / `stop` — состояние и остановка dev-серверов.

## Структура

- `backend/` — ASP.NET Core API и xUnit-тесты.
- `contract/` — TypeSpec, `contract/openapi/openapi.yaml`, конфигурация Orval.
- `frontend/` — React/Vite/Mantine; сгенерированный клиент в `src/api/generated`.
- `mise.toml` — версии и задачи, `hk.pkl` — git-хуки, `.github/workflows/ci.yml` — CI.

## Правила

- `contract/main.tsp` — источник истины для API. После изменения контракта запустить
  `mise run generate` и коммитить OpenAPI и клиент вместе.
- Не редактировать `frontend/src/api/generated` вручную.
- `.github/workflows/hexlet-check.yml` не удалять и не переименовывать.
- Данные хранятся в памяти: после перезапуска API типы встреч и записи сбрасываются.
  Внешней БД нет.
- Авторизации нет, страница владельца не защищена паролем.
- Бронирование атомарно: пересекающиеся интервалы запрещены, при одновременных запросах
  успешен только один.
- `mise run check` должен быть зелёным до пуша. CI вызывает ту же команду.
- Для git-хуков `mise` должен быть доступен в `PATH` (в PowerShell — через `mise activate pwsh`).

## Коммиты

Формат — Conventional Commits: `<type>(<scope>): <subject>`.
Сообщения коммитов на английском, документация на русском.

- `type`: `feat`, `fix`, `chore`, `docs`, `test`, `refactor`, `ci`, `build`, `perf`,
  `revert`, `style`.
- `scope` необязателен: `api`, `web`, `contract`, `ci`, `docs`.
- `subject`: английский, повелительное наклонение, строчная буква, без точки в конце,
  до 72 символов.
- `body` по желанию, объясняет причину изменения; на английском.
- Breaking change: `!` перед двоеточием и/или футер `BREAKING CHANGE: ...`.
- Один коммит — одно логическое изменение.
- Release-please пока не подключён; формат совместим с ним.

Примеры:

```text
feat(api): add meeting type creation
fix(booking): reject overlapping slots
chore(ci): run checks on push
docs: describe local setup
```
