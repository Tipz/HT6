# План реализации домашнего задания № 6

Статус: принят к реализации.  
Дата анализа: 20 сентября 2026 года.

Этот документ описывает план настройки CI/CD, аудита безопасности, OAuth2,
аналитики, мониторинга и структурированного логирования для проекта «Вместе в
путь». Он не является отчётом о выполненной реализации. Фактические результаты
должны фиксироваться в `integration_documentation.md`, `security_audit.md` и
README только после соответствующих проверок.

## 1. Текущее состояние

### Архитектура

- `Together.Core` содержит предметную модель, расчёты и валидацию.
- `Together.Contracts` содержит DTO и маршруты API.
- `Together.Client` — standalone Blazor WebAssembly с MudBlazor, API-клиентами и
  добровольным импортом старых данных IndexedDB.
- `Together.Api` — ASP.NET Core Minimal API, ASP.NET Core Identity, EF
  Core/Npgsql и PostgreSQL.
- Production-сборка клиента размещается в `wwwroot` API. Основной сценарий
  использует один origin и HttpOnly authentication cookie.
- Стандартная схема Identity уже содержит `AspNetUserLogins`, поэтому для одного
  внешнего OAuth-провайдера отдельная модель пользователя не требуется.

### CI/CD и эксплуатация

В `.github/workflows` уже находятся:

- `ci.yml`: restore, Release-сборка и два xUnit-проекта;
- `browser-tests.yml`: Compose, PostgreSQL, миграция и Chromium backend smoke;
- `publish-container.yml`: multi-platform образ, GHCR, SBOM и smoke
  опубликованного digest.

Проект уже имеет multi-stage `Dockerfile`, `docker-compose.deploy.yml`, отдельный
одноразовый сервис `migrate`, непривилегированного пользователя контейнера,
read-only filesystem и постоянные volumes для PostgreSQL и Data Protection.

API предоставляет:

- `/health` для проверки процесса;
- `/health/ready` для проверки процесса и PostgreSQL.

### Тесты

- `Together.Tests`: предметная логика, валидация и bUnit-компоненты.
- `Together.Api.Tests`: аутентификация, CRUD, владение, concurrency, validation и
  базовый health check.
- `Together.BrowserTests`: Chromium backend smoke с регистрацией, сохранением,
  перезагрузкой и выходом.

### Что можно переиспользовать

- существующие Identity cookie и таблицы Identity;
- `AuthClient`, `ApiHttp` и экран `Login.razor`;
- паттерн Minimal API endpoints из `TripsEndpoints`;
- существующие Dockerfile, deployment Compose и migration job;
- GHCR Buildx-публикацию и backend smoke;
- health endpoints;
- `docs/prompt_templates.md` и `development_report.md` для документирования
  использования AI.

### Чего не хватает

- автоматического деплоя на работающий стенд;
- проверки форматирования и отдельного vulnerability gate;
- SAST/dependency review;
- OAuth2-входа и аналитики;
- consent-механизма для аналитики;
- JSON-логов, внешнего uptime monitoring и alerts;
- завершённых документов `integration_documentation.md` и
  `security_audit.md`.

## 2. Что требуется изменить

1. Реализовать разрешённый текущим scope OAuth 2.0 через Яндекс ID.
2. Исправить триггеры GitHub Actions и выстроить единый порядок quality → test →
   smoke → publish → deploy.
3. Добавить format, NuGet vulnerability audit и SAST.
4. Исправить подтверждённые проблемы безопасности, включая CSRF-защиту
   изменяющих cookie-authenticated запросов.
5. Добавить Яндекс ID как дополнительный вход без удаления password login.
6. Добавить GA4 только после согласия и без данных поездок или пользователя.
7. Перевести backend-логи в JSON и подключить мониторинг существующих health
   endpoints.
8. Развернуть прошедший проверки immutable image через существующий Compose.
9. Выполнить итоговые тесты и актуализировать документацию.

Модель поездок, бизнес-правила `Together.Core`, CRUD и основная схема данных
остаются без изменений.

## 3. Противоречия и вопросы

### OAuth и утверждённый scope

Противоречие устранено: действующий `AGENTS.md` разрешает в рамках ДЗ № 6 ровно
один внешний вход — OAuth 2.0 через Яндекс ID. Он добавляется без ролей и без
удаления существующих регистрации и password flow. Другие OAuth-провайдеры не
входят в scope.

### Несовпадение ветки

Рабочая ветка репозитория называется `main`, а существующие workflow слушают
только `master`. Автоматические push/PR-запуски необходимо перевести на `main`.

### Deployment target

Production target — локальная машина `192.168.1.26` с Docker Compose. Поскольку
это приватный LAN-адрес, deployment job запускается защищённым self-hosted GitHub
Actions runner на этой машине; GitHub-hosted runner используется только для
проверок и публикации образа. Внешнее доменное имя не требуется. Схему и порт
production URL, а также TLS/reverse proxy необходимо зафиксировать до регистрации
Redirect URI в Яндекс OAuth; схема, host, port и path Redirect URI должны
соответствовать настройкам приложения у провайдера.

### Имя образа

Каноническое имя зафиксировано: `ghcr.io/tipz/ht5`. Publish и deploy workflow не
должны вычислять другое имя из `GITHUB_REPOSITORY`.

### Конфиденциальность аналитики

В аналитику нельзя отправлять названия и параметры поездок, направления, бюджеты,
возраст детей, заметки, URL предложений, email и стабильный идентификатор
пользователя. OAuth query string также должен исключаться из page view.

### Платежи

Платежи опциональны в ДЗ № 6 и исключены текущим ТЗ. В эту реализацию они не
входят. Две интеграции в утверждённом scope — Яндекс ID OAuth и аналитика.

## 4. Предлагаемая архитектура

### CI/CD

Существующие workflow следует консолидировать или связать так, чтобы результат
имел один обязательный порядок:

```text
format и security gates
          ↓
Release build и xUnit
          ↓
Compose и backend smoke
          ↓
build/push immutable image
          ↓
production deploy
          ↓
post-deploy smoke
```

Для pull request выполняются только проверки. Для push в `main` после успешных
проверок публикуется и разворачивается образ. Production job использует GitHub
Environment; deployment secrets недоступны остальным jobs.

### OAuth2

- В `Together.Api` регистрируется OAuth 2.0 handler для Яндекс ID с authorization,
  token и user-information endpoints Яндекса.
- Client ID и Client Secret поступают только через серверную конфигурацию.
- Challenge/callback размещаются в отдельном Minimal API endpoint-модуле.
- Callback использует существующие `ApplicationUser`, `UserManager` и
  `SignInManager`.
- Запрашивается только право `login:email`, необходимое для создания локальной
  учётной записи; устойчивый `id` входит в стандартный ответ API Яндекс ID.
- Устойчивый provider key берётся из идентификатора Яндекс ID, а не из email.
- Redirect разрешается только на локальный путь.
- Автоматическое связывание существующего password-аккаунта лишь по совпадению
  email запрещено без дополнительного подтверждения.
- Текущие регистрация, password login, logout и `/api/auth/me` сохраняются.

### CSRF и защита API

- API выдаёт antiforgery token клиенту.
- `ApiHttp` прикладывает token ко всем POST/PUT/PATCH/DELETE.
- Token обновляется после смены состояния входа.
- OAuth использует штатные `state` и correlation cookie провайдера.
- Production запускается только с явно заданной строкой подключения.
- Forwarded headers, HTTPS и security headers настраиваются с учётом reverse
  proxy и Blazor WebAssembly.

### Аналитика

В `Together.Client` добавляется один `AnalyticsClient` и небольшой JS-модуль.
Google tag не загружается до согласия. Публичный Measurement ID не считается
секретом, но аналитика полностью выключается при его отсутствии.

Разрешены только фиксированные технические события, например
`login_succeeded`, `trip_created`, `variant_saved`, `comparison_opened`. Значения
из моделей поездки в параметры событий не передаются.

### Логи и мониторинг

- Используется встроенный `AddJsonConsole`, без новой logging-платформы в коде.
- Логи содержат уровень, event id, timestamp, trace id и технический результат.
- Request body, cookie, OAuth tokens, email и данные поездок не логируются.
- `/health` используется внешним uptime monitor.
- `/health/ready` используется deployment smoke и диагностикой БД.
- Недоступность GA4 не делает приложение `Unhealthy`.

## 5. План реализации

### Этап 1 — Зафиксировать OAuth и production-конфигурацию

**Цель**

Устранить противоречия до изменения прикладного кода.

**Изменения**

`AGENTS.md`, `docs/technical_specification.md`,
`docs/backend_requirements.md`, `integration_documentation.md`, а также внешние
настройки Яндекс OAuth, GA4, локального Docker-host и GitHub Environment.

**Реализация**

- Использовать разрешённый Яндекс ID как единственный OAuth-провайдер.
- Зафиксировать `main`, Docker-host `192.168.1.26` и образ
  `ghcr.io/tipz/ht5`.
- Установить на production-host защищённый self-hosted runner с Docker Compose;
  не использовать его для workflow из pull request.
- Зафиксировать схему/порт production URL, зарегистрировать точный Redirect URI в
  отдельном production-приложении Яндекс OAuth и определить consent-политику.
- Создать environment secrets без помещения значений в Git.

**Проверка**

Сопоставить ДЗ, `AGENTS.md`, техническое задание и deployment settings.

**Критерий готовности**

Яндекс ID разрешён актуальными инструкциями; схема/порт URL и Redirect URI,
ветка, runner и image name однозначно определены.

### Этап 2 — CI, quality gate и security baseline

**Цель**

Блокировать публикацию и деплой при неуспешной проверке.

**Изменения**

Workflow в `.github/workflows`, при необходимости `Directory.Build.props`,
первичная версия `security_audit.md`.

**Реализация**

- Перевести триггеры с `master` на `main`.
- Добавить `dotnet format --verify-no-changes`.
- Добавить `dotnet package list --include-transitive --vulnerable`.
- Сохранить Release build, два xUnit-проекта и backend smoke.
- Подключить CodeQL и dependency review при доступности функций GitHub.
- Ограничить permissions по jobs и закрепить сторонние actions по полному SHA.
- Публиковать образ только после всех проверок.
- Провести AI-assisted OWASP review с ручной проверкой каждой находки.

**Проверка**

Тестовый PR с нарушенным форматированием падает до publish; корректный PR
проходит и сохраняет отчёты.

**Критерий готовности**

Все quality/security/test jobs обязательны, а неуспешный job исключает публикацию.

### Этап 3 — Security hardening, health и JSON-логи

**Цель**

Исправить подтверждённые риски базового приложения.

**Изменения**

`Together.Api/Program.cs`, `Together.Client/Storage/ApiHttp.cs`, auth/API
endpoints, appsettings, env-шаблоны и тесты.

**Реализация**

- Добавить antiforgery flow.
- Усилить password policy для новых паролей без блокировки существующих.
- Проверить rate limit и lockout.
- Убрать production fallback на известный пароль БД.
- Исправить небезопасные deployment defaults.
- Настроить forwarded/security headers и JSON console logs.
- Расширить тесты health/readiness.

**Проверка**

Запрос без token отклоняется, с token проходит; CRUD, ownership и `409` не
регрессируют; логи являются JSON и не содержат чувствительных данных.

**Критерий готовности**

Подтверждённые high/critical findings устранены либо имеют документированное
решение о принятии риска.

### Этап 4 — OAuth 2.0 через Яндекс ID

**Цель**

Добавить внешний вход без замены существующей Identity-модели.

**Изменения**

`Together.Api.csproj`, lock-файл, `Program.cs`, новый endpoint-модуль,
`ApiRoutes.cs`, `AuthClient.cs`, `Login.razor`, конфигурация и тесты.

**Реализация**

- Зарегистрировать отдельное production-приложение Яндекс OAuth типа «для
  авторизации пользователей» и запросить только минимальные права на ID/email.
- Настроить OAuth handler, authorization code flow, challenge, callback, token
  exchange и получение профиля через API Яндекс ID.
- Валидировать локальный return URL.
- Создавать Identity user/login для нового аккаунта.
- Безопасно обрабатывать совпадающий email.
- Добавить русскую кнопку и состояния ошибки/отмены.
- Не передавать provider token в Blazor bundle.

**Проверка**

Integration tests используют тестовую authentication scheme; ручной тест
проверяет Яндекс ID на production URL машины `192.168.1.26` с зарегистрированным
Redirect URI.

**Критерий готовности**

Яндекс ID и password login работают одновременно, а владение поездками не
меняется.

### Этап 5 — Аналитика с согласием

**Цель**

Получать минимальную продуктовую статистику без пользовательских данных.

**Изменения**

`Together.Client/Program.cs`, новый `AnalyticsClient`, JS-модуль, клиентская
конфигурация, consent-компонент и тесты.

**Реализация**

- Не загружать GA4 до согласия.
- Хранить только выбор consent.
- Ограничить имена событий и параметры allowlist-списком.
- Удалять query string из page view.
- Предоставить отзыв согласия.

**Проверка**

До согласия запросов к GA нет; после согласия разрешённые события видны в
DebugView и не содержат данных поездок.

**Критерий готовности**

Аналитика работает opt-in и не принимает произвольные model values.

### Этап 6 — Production deployment и мониторинг

**Цель**

Автоматически разворачивать проверенный immutable image.

**Изменения**

CI/CD workflow, при необходимости deployment Compose/env-шаблон, GitHub
Environment `production`, self-hosted runner машины `192.168.1.26` и
`integration_documentation.md`.

**Реализация**

- Передавать точный image digest из publish в deploy.
- Разрешить production deploy только для push в `main`.
- Выполнять deploy job только на защищённом self-hosted runner машины
  `192.168.1.26`; не запускать на нём pull request jobs.
- Всегда разворачивать `ghcr.io/tipz/ht5@<digest>`.
- Использовать существующие Compose и отдельный `migrate`.
- После deploy ждать `/health/ready` и запускать smoke.
- Настроить uptime monitor для `/health` и alert contact.
- Описать rollback на предыдущий digest.

**Проверка**

Тестовый commit создаёт GitHub Deployment, сервер запускает ожидаемый digest,
миграция завершается успешно, monitor получает `200`.

**Критерий готовности**

Push в `main` автоматически обновляет production только после всех проверок.

### Этап 7 — Итоговая приёмка и документация

**Цель**

Сформировать полный комплект сдачи с фактическими результатами.

**Изменения**

`integration_documentation.md`, `security_audit.md`, `README.md`, `SUBMISSION.md`,
`development_report.md`, `docs/prompt_templates.md` и evidence с вымышленными
данными.

**Реализация**

- Описать CI/CD, secrets, OAuth, аналитику, мониторинг, логи и rollback.
- Закрыть security findings и записать остаточные риски.
- Зафиксировать AI-промпты и способ ручной проверки результатов.
- Выполнить полный набор проверок и записать реальные команды/результаты.

**Проверка**

Инструкции воспроизводимы, ссылки доступны, а документация соответствует коду.

**Критерий готовности**

Все обязательные артефакты ДЗ присутствуют, проверки действительно выполнены,
секретов и реальных пользовательских данных в репозитории нет.

## 6. Стратегия тестирования

### Unit и component tests

- аналитика ничего не отправляет до consent;
- route перед аналитикой очищается от query string;
- allowlist не принимает произвольные параметры;
- login page показывает кнопку Яндекс ID и callback errors;
- OAuth redirect принимает только локальный return URL;
- `ApiHttp` добавляет antiforgery token;
- пользовательский ввод по-прежнему выводится как текст.

### Integration tests

- challenge и успешный/неуспешный callback;
- повторный external login использует того же пользователя;
- совпадение email не перехватывает существующий аккаунт;
- password login остаётся рабочим;
- state-changing API без CSRF token отклоняется;
- ownership, concurrency и транзакции сохраняются;
- health/readiness правильно отражают состояние БД.

### Пользовательские сценарии

- регистрация и password login;
- первый и повторный вход через Яндекс ID, отмена и ошибка;
- сохранение поездки после OAuth login;
- opt-in/opt-out аналитики и проверка GA4 DebugView;
- автоматический deploy и сохранность данных после обновления;
- получение alert при недоступности стенда.

### Затронутые тесты

- `TripsApiTests` потребуется antiforgery token;
- `BackendSmoke` будет использовать обновлённый auth flow;
- `HealthEndpointTests` расширится readiness-сценарием;
- `TogetherApiFactory` получит тестовую external authentication scheme;
- component tests login/consent потребуют JSInterop и NavigationManager mocks.

## 7. Риски

- До регистрации приложения Яндекс OAuth необходимо зафиксировать схему, порт и
  callback path production URL.
- GitHub-hosted runner не маршрутизируется к `192.168.1.26`; deploy зависит от
  доступности и защиты self-hosted runner на production-машине.
- Независимые workflow могут опубликовать образ параллельно с упавшим CI.
- Связывание пользователей только по email создаёт риск захвата аккаунта.
- Ошибочная antiforgery-интеграция может сломать все сохранения клиента.
- Неверные forwarded headers нарушат OAuth callback и Secure cookies.
- Аналитика может раскрыть query/model data при отсутствии жёсткого allowlist.
- Неудачная миграция способна остановить релиз; требуется backup и rollback.
- Усиление password policy не должно инвалидировать существующие пароли.
- `/health` не видит отказ БД, а `/health/ready` нельзя использовать как
  единственный liveness probe.
- Дублирующие workflow и документы увеличат стоимость сопровождения.

## 8. Не входит в текущую задачу

- платежи;
- несколько OAuth-провайдеров;
- роли, MFA и административная панель;
- пользовательские данные в аналитике;
- собственное хранилище аналитики;
- ELK/Loki-кластер при наличии достаточных логов хостинга;
- Kubernetes или замена PostgreSQL/Identity;
- изменение модели поездок и реализация US-11–US-13;
- автоматический rollback схемы БД.

## 9. Критерии полной готовности

- OAuth через Яндекс ID разрешён актуальными проектными инструкциями.
- Workflow работают для `main`.
- Format, vulnerability audit, Release build, оба xUnit-проекта и backend smoke
  обязательны перед публикацией.
- Multi-platform image разворачивается по immutable digest.
- Миграция выполняется отдельным контейнером, production работает через HTTPS.
- Password login и вход через Яндекс ID успешно работают.
- Аналитика отправляет только разрешённые события после consent.
- Health endpoints, внешний monitor и alerts проверены.
- Backend пишет JSON-логи без секретов и пользовательских данных.
- Подтверждённые high/critical findings исправлены.
- `integration_documentation.md` и `security_audit.md` содержат фактические, а не
  предполагаемые результаты.
- Использование AI и ручная проверка его выводов документированы.
- README содержит актуальные инструкции и рабочую deployment-ссылку.
