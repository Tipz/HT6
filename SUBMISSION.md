# Материалы домашнего задания № 6

Проект «Вместе в путь» — standalone Blazor WebAssembly, ASP.NET Core Minimal API,
Identity, EF Core/Npgsql и PostgreSQL. ДЗ № 6 добавляет CI/CD, security hardening,
Яндекс ID, Яндекс Метрику с согласием, JSON logging и monitoring.

## Состав сдачи

| Требование | Реализация |
| --- | --- |
| CI/CD | `.github/workflows/ci.yml`: format/audit → build/tests → backend smoke → publish → smoke опубликованного образа |
| Container | `ghcr.io/tipz/ht6`, `linux/amd64` и `linux/arm64`, deployment только по digest |
| Deployment | `docker-compose.deploy.yml`, отдельный `migrate`, persistent PostgreSQL и Data Protection |
| OAuth2 | Стандартный ASP.NET Core OAuth handler для Яндекс ID; password login сохранён |
| Аналитика | Opt-in Яндекс Метрика, очищенные SPA paths, две фиксированные цели |
| Безопасность | Antiforgery, lockout/rate limit, safe config defaults, CSP/security headers, pinned actions |
| Monitoring | `/health`, `/health/ready` и Docker healthcheck |
| Логи | Однострочный JSON stdout с timestamp, level, category, event id и trace id |
| Документация | `docs/integration_documentation.md`, `docs/security_audit.md`, `docs/prompt_templates.md`, `docs/log_analysis.md` |

## Локально подтверждено 20 сентября 2026 года

- `dotnet format Together.slnx --verify-no-changes --no-restore` — успешно;
- Release build — успешно без предупреждений;
- `Together.Tests` — 36/36;
- `Together.Api.Tests` — 15/15;
- NuGet direct/transitive vulnerability report — уязвимые пакеты не найдены;
- `/health` — HTTP 200; JSON stdout содержит `Timestamp`, `LogLevel`,
  `Category`, `EventId` и scope с `TraceId`;
- fake OAuth integration tests покрывают успех, отмену, повторный вход,
  конфликт email, внешний return URL и сохранность password login;
- тесты аналитики покрывают отсутствие отправок до consent, очищенный URL,
  allowlist целей/параметров и отзыв согласия.

## Pending внешние проверки

- Docker/Compose/Chromium backend smoke: Docker CLI отсутствует на текущей машине;
- CodeQL, dependency review, publish и workflow run: требуют push в GitHub;
- ручной production deploy/smoke требует настройки production-хоста и `APP_URL`;
- реальный Яндекс ID: требуется зарегистрировать Redirect URI после выбора схемы
  и порта production URL;
- реальная Яндекс Метрика: требуется номер счётчика и доступ к её отчётам.

Шаблоны не выдаются за выполненные проверки. Production URL и ссылка на новый
workflow run будут добавлены после внешней настройки владельцем.
