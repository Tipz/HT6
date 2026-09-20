# Интеграции и CI/CD — домашнее задание № 6

Статус документа: локальная реализация завершена; внешняя production-настройка и
проверки отмечены `Pending`.
Последнее обновление: 20 сентября 2026 года.

Фактические результаты следует добавлять сюда только после выполнения и проверки
соответствующего этапа. Принятый порядок работ описан в
[IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

## 1. Исходное состояние

Приложение состоит из standalone Blazor WebAssembly, ASP.NET Core Minimal API и
PostgreSQL. Клиент публикуется в `wwwroot` API, а аутентификация выполняется через
ASP.NET Core Identity cookie.

До начала ДЗ № 6 в репозитории присутствуют:

- CI с Release build и двумя xUnit-проектами;
- Compose/Chromium backend smoke;
- публикация AMD64/ARM64-образа в GHCR;
- Docker Compose с отдельным migration job;
- `/health` и `/health/ready`;
- текстовые console logs ASP.NET Core.

В репозитории реализованы OAuth2 через Яндекс ID, opt-in Яндекс Метрика,
antiforgery, JSON-логи, связанный pipeline и deployment/monitoring workflows.
Production runner, URL, OAuth-приложение, счётчик и alert ещё не настроены.

## 2. Принятые ограничения

- Поездки, направления, бюджеты, возраст детей, заметки и URL предложений не
  передаются сторонним сервисам.
- Секреты не помещаются в Git, Docker image, логи или Blazor bundle.
- PostgreSQL остаётся основным хранилищем.
- Миграции выполняются отдельным одноразовым контейнером до запуска приложения.
- Data Protection keys сохраняются вне writable layer контейнера.
- Password login сохраняется для обратной совместимости.
- Платежная интеграция в текущий объём не входит.

## 3. CI/CD

### Реализованное состояние

| Workflow | Фактическая функция | Ограничение |
| --- | --- | --- |
| `.github/workflows/ci.yml` | Format/NuGet audit → build/xUnit → Compose/Chromium → publish digest → deploy/smoke | Внешний workflow run Pending |
| `.github/workflows/uptime-monitor.yml` | `/health` каждые 15 минут и ручная имитация отказа | Runner/APP_URL/alert test Pending |

### Целевая последовательность

```text
format/security → build/tests → backend smoke → publish → deploy → production smoke
```

Pull request выполняет только проверки. Push в `main` после успешных проверок
публикует immutable image и разворачивает его в GitHub Environment `production`.

### GitHub secrets и variables без значений

Deployment target и image name зафиксированы. Секреты хранятся в GitHub
Environment `production` либо только на production host.

| Имя | Тип | Назначение |
| --- | --- | --- |
| `DEPLOY_PATH` | Environment variable | Каталог deployment Compose на `192.168.1.26` |
| `APP_URL` | Environment variable | Production URL для smoke после выбора схемы и порта |
| `YANDEX_CLIENT_ID` | Environment variable | Client ID production-приложения Яндекс OAuth |
| `YANDEX_CLIENT_SECRET` | Environment secret | Client Secret production-приложения Яндекс OAuth |
| `YANDEX_METRIKA_COUNTER_ID` | Environment variable | Публичный номер счётчика |

Deployment job передаёт `YANDEX_CLIENT_ID` и `YANDEX_CLIENT_SECRET` только как
server-side environment values, которые Compose отображает в
`Authentication__Yandex__ClientId` и `Authentication__Yandex__ClientSecret`.

Пароль PostgreSQL остаётся в `.env.deploy` на deployment host, OAuth client secret
— в GitHub Environment `production`. Они не передаются в Blazor configuration и
не печатаются командой `docker compose config --quiet`.

### Deployment

Production target — локальная машина `192.168.1.26` с Docker Compose. Защищённый
self-hosted GitHub Actions runner на этой машине получает digest, разворачивает
`ghcr.io/tipz/ht6@<digest>`, выполняет `migrate`, запускает `app` и ждёт успешный
`/health/ready`. GitHub-hosted jobs выполняют проверки и публикацию образа, но не
пытаются обращаться к приватному LAN-адресу. Self-hosted runner используется
только deployment job после push в `main` и никогда не запускает код из pull
request.

До первого deploy необходимо определить:

- схему и порт production URL на `192.168.1.26`;
- TLS/reverse proxy либо явно зафиксированный режим изолированного LAN-стенда;
- каталог Compose и labels защищённого self-hosted runner;
- доступ Docker host к `ghcr.io/tipz/ht6`, если package является приватным;
- backup и rollback procedure.

### Логи и rollback на Docker host

Production Compose использует Docker `json-file` с rotation: приложение и БД —
до пяти файлов по 10 MiB, migration job — до двух. Просмотр без раскрытия env:

```bash
cd "$DEPLOY_PATH"
docker compose --env-file .env.deploy --file docker-compose.deploy.yml logs --since 1h --tail 200 app
```

Ручной rollback не удаляет volumes. Возьмите предыдущий успешный digest из
GitHub run/package history, задайте его только для команды и обновите приложение:

```bash
export TOGETHER_IMAGE='ghcr.io/tipz/ht6@sha256:<previous-digest>'
docker compose --env-file .env.deploy --file docker-compose.deploy.yml pull app
docker compose --env-file .env.deploy --file docker-compose.deploy.yml up -d --no-deps app
curl --fail "$APP_URL/health/ready"
```

Схема БД автоматически назад не откатывается. Rollback приложения допустим только
при совместимости предыдущего образа с уже применённой схемой; PostgreSQL volume
и Data Protection keys не удаляются.

## 4. OAuth2

### Статус

Реализован стандартным ASP.NET Core OAuth handler. При отсутствии Client ID или
Client Secret приложение запускается с password login, а кнопка Яндекс ID скрыта.
Реальный provider smoke остаётся Pending до регистрации приложения.

### Планируемый провайдер

Яндекс ID OAuth 2.0 через стандартный ASP.NET Core OAuth handler. Backend
выполняет authorization code flow, обменивает code на token и отдельно получает
профиль из API Яндекс ID.

Официальная документация:

- [регистрация приложения для авторизации](https://yandex.com/dev/id/doc/en/register-auth);
- [получение OAuth token](https://yandex.com/dev/id/doc/en/access);
- [authorization code flow](https://yandex.com/dev/id/doc/en/codes/code-url);
- [получение профиля пользователя](https://yandex.com/dev/id/doc/en/user-information);
- [рекомендации безопасности](https://yandex.com/dev/id/doc/en/tips).

Для production и тестирования создаются отдельные приложения Яндекс OAuth.
Production-приложение запрашивает только `login:email`: устойчивый `id` входит в
стандартный ответ, а email нужен для создания локальной Identity-записи.

### Конфигурация backend

```text
Authentication__Yandex__ClientId
Authentication__Yandex__ClientSecret
```

Client Secret является серверным секретом. Client ID допустим в конфигурации, но
для единообразия также поступает в backend через environment configuration.

### Реализованный flow

1. Клиент выполняет полную навигацию на backend challenge endpoint.
2. Backend создаёт authentication properties и correlation/state cookie и
   перенаправляет пользователя на Yandex OAuth authorization endpoint.
3. Яндекс возвращает браузер с authorization code на зарегистрированный Redirect
   URI. Его scheme, host, port и path совпадают с настройками production-app.
4. Backend проверяет state и обменивает code на token серверным запросом.
5. Backend получает минимальный профиль через API Яндекс ID. Token не передаётся
   в Blazor и не сохраняется после завершения входа.
6. Существующий Identity user находится по устойчивому provider key Яндекс ID
   либо безопасно создаётся. Совпадение email само по себе не связывает аккаунты.
7. Backend выдаёт существующую application cookie и возвращает браузер на
   локальный клиентский маршрут.
8. Клиент получает пользователя через `/api/auth/me`.

Password login, регистрация и logout остаются доступными.

### Обработка ошибок

- отмена входа пользователем;
- provider error;
- отсутствующий или неподтверждённый email;
- конфликт email с существующим password account;
- неверный/внешний return URL;
- несоответствие correlation/state;
- временная недоступность провайдера.

Токены и полные provider responses не логируются и не передаются в Blazor.

## 5. Аналитика

### Статус

Реализована и выключена при отсутствии `YandexMetrika__CounterId`. Тег загружается
динамически только после `granted`; хранится только строка consent в localStorage.

### Сервис

Яндекс Метрика для технических SPA page view и ограниченного набора целей.
Публичный номер счётчика передаётся в клиентскую конфигурацию, например через
`YandexMetrika__CounterId`; при его отсутствии аналитика выключена.

### Конфиденциальность

Тег Яндекс Метрики загружается только после явного согласия. До consent
приложение не должно загружать `tag.js` или отправлять запросы к
`mc.yandex.ru`. Вебвизор, карты, e-commerce, user parameters и session parameters
не включаются.

Разрешённые параметры должны быть закрытым списком технических констант. Нельзя
отправлять:

- идентификатор, email или имя пользователя;
- название, даты и направление поездки;
- состав семьи и возраст детей;
- суммы расходов;
- заметки и URL предложений;
- OAuth query string или exception text.

### Просмотры и цели

| Просмотр/цель | Допустимые параметры |
| --- | --- |
| `login_succeeded` | `method=password|yandex` |
| `trip_created` | нет |

SPA-навигация отправляется методом `hit` только с очищенным путём без query string
и fragment. Цели отправляются методом `reachGoal`. Unit-тесты подтверждают
allowlist и очистку URL. Фактические запросы и появление целей в реальном счётчике
остаются Pending.

## 6. Health checks и мониторинг

### Текущее состояние

- `GET /health` возвращает доступность процесса.
- `GET /health/ready` использует EF Core health check для PostgreSQL.
- Publish workflow уже ожидает `/health/ready` при smoke опубликованного image.

### Мониторинг

- внешний monitor внутри доступной сети проверяет production URL `/health` на
  `192.168.1.26`;
- deployment smoke проверяет `/health/ready`;
- alert отправляется на согласованный email/канал;
- период и timeout фиксируются после выбора сервиса;
- недоступность аналитики не влияет на readiness приложения.

В качестве минимального доступного uptime monitor выбран scheduled GitHub Actions
job на том же защищённом runner, поскольку GitHub-hosted runner не видит LAN.
Красный workflow run использует штатные GitHub notifications. Ручной запуск с
`simulate_failure=true` предназначен для тестового alert; результат Pending.

## 7. Логирование

### Текущее состояние

Используется стандартное ASP.NET Core console logging с уровнями `Information` и
`Warning`. Централизованное хранение не настроено.

### Реализованное состояние

- однострочный JSON в stdout;
- timestamp, level, category, event id и trace id;
- структурированные auth и request outcomes;
- отсутствие request body, cookie, password, OAuth token, connection string,
  email и содержимого поездок;
- retention и просмотр через средства выбранного хостинга либо Docker host.

Промпты для AI-анализа работают только с обезличенными фрагментами логов.
Фактический синтетический пример и ручная проверка: [log_analysis.md](log_analysis.md).

## 8. Локальная конфигурация

OAuth и аналитика необязательны для локального запуска: при отсутствии их
конфигурации приложение сохраняет password login, а аналитика выключена.

Реальные Client Secret, password и SSH key нельзя добавлять в `.env.example`,
`deploy.env.example`, appsettings или документацию.

## 9. План проверки интеграций

Фактический локальный набор проверок:

```powershell
dotnet format Together.slnx --verify-no-changes
dotnet restore Together.slnx --locked-mode
dotnet build Together.slnx -c Release --no-restore
dotnet test tests/Together.Tests/Together.Tests.csproj -c Release
dotnet test tests/Together.Api.Tests/Together.Api.Tests.csproj -c Release
dotnet package list --project Together.slnx --include-transitive --vulnerable
```

Кроме того:

- Compose backend smoke;
- успешный и отменённый OAuth flow;
- отсутствие запросов к Яндекс Метрике до consent;
- SPA-просмотры и цели Яндекс Метрики после consent;
- production deployment по digest;
- `/health` и `/health/ready`;
- тестовый monitoring alert;
- проверка JSON-логов на отсутствие чувствительных данных.

20 сентября 2026 года выполнены format, locked restore, Release build, оба
xUnit-проекта, NuGet audit, `/health` и просмотр JSON-лога. Docker CLI отсутствует,
поэтому Compose/backend smoke и `docker compose config` локально не выполнялись.
Реальные OAuth, Метрика, production deployment и monitoring alert требуют внешних
параметров и отмечены Pending. Ссылка на новый GitHub workflow run отсутствует,
поскольку commit/push не выполнялись.

## 10. Использование AI

На этапе планирования AI использован для:

- сопоставления требований ДЗ № 6 с текущей архитектурой;
- поиска конфликтов scope;
- предварительного обзора CI/CD и security controls;
- формирования последовательного плана.

После реализации здесь необходимо указать фактические промпты для:

- генерации/ревью GitHub Actions;
- OWASP-аудита;
- анализа обезличенных JSON-логов;

а также команды и тесты, которыми были проверены предложения AI.
