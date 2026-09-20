# Интеграции и CI/CD — домашнее задание № 6

Статус документа: подготовлена исходная структура; интеграции ДЗ № 6 ещё не
реализованы.  
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

Автоматический production deployment на `192.168.1.26`, OAuth2 через Яндекс ID,
аналитика, JSON-логи и внешний мониторинг пока не настроены.

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

### Текущее состояние

| Workflow | Фактическая функция | Ограничение |
| --- | --- | --- |
| `.github/workflows/ci.yml` | Restore, Release build, Core/UI и API tests | Слушает `master`, тогда как рабочая ветка — `main` |
| `.github/workflows/browser-tests.yml` | Compose, PostgreSQL, Chromium backend smoke | Не является обязательным predecessor publish job |
| `.github/workflows/publish-container.yml` | Buildx, GHCR, SBOM и smoke digest | Публикация не выполняет deployment на внешний стенд |

### Целевая последовательность

```text
format/security → build/tests → backend smoke → publish → deploy → production smoke
```

Pull request выполняет только проверки. Push в `main` после успешных проверок
публикует immutable image и разворачивает его в GitHub Environment `production`.

### Планируемые GitHub secrets и variables

Deployment target и image name зафиксированы. Секреты хранятся в GitHub
Environment `production` либо только на production host.

| Имя | Тип | Назначение |
| --- | --- | --- |
| `DEPLOY_PATH` | Environment variable | Каталог deployment Compose на `192.168.1.26` |
| `APP_URL` | Environment variable | Production URL для smoke после выбора схемы и порта |
| `YANDEX_CLIENT_ID` | Environment secret | Client ID production-приложения Яндекс OAuth |
| `YANDEX_CLIENT_SECRET` | Environment secret | Client Secret production-приложения Яндекс OAuth |

Deployment job отображает `YANDEX_CLIENT_ID` и `YANDEX_CLIENT_SECRET` только в
server-side переменные `Authentication__Yandex__ClientId` и
`Authentication__Yandex__ClientSecret`.

Пароль PostgreSQL и OAuth client secret должны оставаться на deployment host либо
в GitHub Environment `production`. Они не должны передаваться в Blazor
configuration или попадать в вывод `docker compose config`.

### Deployment

Production target — локальная машина `192.168.1.26` с Docker Compose. Защищённый
self-hosted GitHub Actions runner на этой машине получает digest, разворачивает
`ghcr.io/tipz/ht5@<digest>`, выполняет `migrate`, запускает `app` и ждёт успешный
`/health/ready`. GitHub-hosted jobs выполняют проверки и публикацию образа, но не
пытаются обращаться к приватному LAN-адресу. Self-hosted runner используется
только deployment job после push в `main` и никогда не запускает код из pull
request.

До реализации необходимо определить:

- схему и порт production URL на `192.168.1.26`;
- TLS/reverse proxy либо явно зафиксированный режим изолированного LAN-стенда;
- каталог Compose и labels защищённого self-hosted runner;
- доступ Docker host к `ghcr.io/tipz/ht5`, если package является приватным;
- backup и rollback procedure.

## 4. OAuth2

### Статус

Не реализован, но разрешён текущим `AGENTS.md` в рамках ДЗ № 6. Разрешён ровно
один внешний провайдер — Яндекс ID; password login сохраняется.

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

### Планируемая конфигурация backend

```text
Authentication__Yandex__ClientId
Authentication__Yandex__ClientSecret
```

Client Secret является серверным секретом. Client ID допустим в конфигурации, но
для единообразия также поступает в backend через environment configuration.

### Планируемый flow

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

Не реализована.

### Планируемый сервис

Google Analytics 4 для технических page view и ограниченного набора событий.

### Конфиденциальность

Google tag загружается только после явного согласия. До consent приложение не
должно отправлять запросы аналитики.

Разрешённые параметры должны быть закрытым списком технических констант. Нельзя
отправлять:

- идентификатор, email или имя пользователя;
- название, даты и направление поездки;
- состав семьи и возраст детей;
- суммы расходов;
- заметки и URL предложений;
- OAuth query string или exception text.

### Планируемые события

| Событие | Допустимые параметры |
| --- | --- |
| `login_opened` | нет |
| `login_succeeded` | `method=password|yandex` |
| `trip_created` | нет |
| `variant_saved` | `operation=create|update` |
| `comparison_opened` | нет |

Список не является подтверждением реализации. Фактические события и результаты
GA4 DebugView нужно записать после проверки.

## 6. Health checks и мониторинг

### Текущее состояние

- `GET /health` возвращает доступность процесса.
- `GET /health/ready` использует EF Core health check для PostgreSQL.
- Publish workflow уже ожидает `/health/ready` при smoke опубликованного image.

### Планируемый мониторинг

- внешний monitor внутри доступной сети проверяет production URL `/health` на
  `192.168.1.26`;
- deployment smoke проверяет `/health/ready`;
- alert отправляется на согласованный email/канал;
- период и timeout фиксируются после выбора сервиса;
- недоступность аналитики не влияет на readiness приложения.

Сервис мониторинга, URL monitor и проверка тестового alert пока не определены.

## 7. Логирование

### Текущее состояние

Используется стандартное ASP.NET Core console logging с уровнями `Information` и
`Warning`. Централизованное хранение не настроено.

### Целевое состояние

- однострочный JSON в stdout;
- timestamp, level, category, event id и trace id;
- структурированные auth и request outcomes;
- отсутствие request body, cookie, password, OAuth token, connection string,
  email и содержимого поездок;
- retention и просмотр через средства выбранного хостинга либо Docker host.

Промпты для AI-анализа должны работать только с обезличенными фрагментами логов.

## 8. Локальная конфигурация

До реализации остаются актуальными команды ДЗ № 5 из README. OAuth и аналитика
должны быть необязательны для локального запуска: при отсутствии их конфигурации
приложение сохраняет password login, а аналитика выключена.

Реальные Client Secret, password и SSH key нельзя добавлять в `.env.example`,
`deploy.env.example`, appsettings или документацию.

## 9. План проверки интеграций

После реализации требуется зафиксировать фактический результат следующих
проверок:

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
- отсутствие analytics traffic до consent;
- GA4 DebugView после consent;
- production deployment по digest;
- `/health` и `/health/ready`;
- тестовый monitoring alert;
- проверка JSON-логов на отсутствие чувствительных данных.

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
