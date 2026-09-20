# Вместе в путь

Текущая версия — домашнее задание № 5: Blazor WebAssembly-клиент подключён к
ASP.NET Core API и PostgreSQL. Реализованы регистрация, cookie-вход, изоляция поездок
по владельцу, CRUD поездок и вариантов, optimistic concurrency и Docker Compose.

## **Развёртывание готового образа.** Для запуска на другой инфраструктуре используйте
[`docker-compose.deploy.yml`](docker-compose.deploy.yml): достаточно Docker Compose и файла
`.env.deploy`, исходники и .NET SDK на целевом сервере не нужны. Пошаговые команды и
параметры безопасности приведены в разделе [«Развёртывание готового образа»](#развёртывание-готового-образа).

Архитектура, API и развёртывание описаны в [backend_documentation.md](backend_documentation.md),
а требования ДЗ № 5 — в [docs/backend_requirements.md](docs/backend_requirements.md).

## Быстрый запуск ДЗ № 5

```powershell
Copy-Item .env.example .env
# Укажите длинный случайный POSTGRES_PASSWORD в .env
# Для локального http://localhost:8080 установите SECURE_COOKIES=false
docker compose up --build
```

После успешной миграции приложение доступно на `http://localhost:8080`. Файл `.env`
исключён из Git; секреты нельзя добавлять в `appsettings.json` или Blazor bundle.
`SECURE_COOKIES=true` оставляйте для production и HTTPS. Значение `false` необходимо
для локального `http://localhost:8080` и допустимо только на изолированном стенде.

Готовый multi-platform образ публикуется в GitHub Container Registry:

```text
ghcr.io/tipz/ht5:latest
```

`latest` следует за веткой `master`; для воспроизводимого production-деплоя
используйте release-тег `v*` или неизменяемый тег `sha-*`.

Адаптивное приложение для сравнения семейных поездок по бюджету, дороге и удобствам для детей.
Frontend создан в ДЗ № 4 по [исходному ТЗ](docs/technical_specification.md) и расширен backend в ДЗ № 5.

## Возможности

- Несколько независимых поездок: даты, ночи, взрослые и возраст детей.
- Создание, редактирование и удаление вариантов направления и жилья.
- Шесть статей бюджета в рублях за всю семью за всю поездку. Расчёт в целых копейках; пустое значение отличается от нуля.
- Сравнение двух и более вариантов по семи переключаемым критериям.
- Пометка проверки расходов при изменении дат или семьи.
- Серверное сохранение после входа, обработка сетевых ошибок и конфликтов версий.
- Добровольный импорт старых поездок из IndexedDB без автоматического удаления локальной копии.

Основной экран следует концепции [«План поездки»](docs/ui_concepts/03-analytical.html): сине-серая палитра, сравнение перед карточками, боковая навигация на широком экране.

![Основной экран, вымышленные данные](docs/evidence/chromium-1440.png)

## Стек

C# / .NET 10, **Blazor WebAssembly Standalone**, **ASP.NET Core Minimal API**, **PostgreSQL**, **MudBlazor 9.7.0**.
Бизнес-правила и валидация — C#; EF Core/Npgsql — серверные данные; IndexedDB используется только для импорта старой версии.
Тесты: xUnit, bUnit, Playwright .NET.

## Запуск

Нужен .NET SDK **10.0.302** или более новый patch из той же линии (см. global.json).
Node.js и npm для сборки и запуска не нужны.

Для запуска без Docker сначала подготовьте PostgreSQL и примените миграцию, затем из корня репозитория в двух терминалах:

```powershell
dotnet restore Together.slnx
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Together.Api
dotnet run --project src/Together.Api
dotnet run --project src/Together.Client
```

Откройте **http://localhost:5180**, зарегистрируйтесь или войдите. Для новой учётной
записи приложение создаёт вымышленный пример. Найденные поездки IndexedDB не
отправляются до подтверждения импорта и не удаляются автоматически.

Для разработки с hot reload:

```powershell
dotnet watch --project src/Together.Client
```

Сохраняйте один адрес и порт: браузер разделяет данные localhost и 127.0.0.1, а также разных портов.


## Проверки

```powershell
dotnet build Together.slnx -c Release
dotnet test tests/Together.Tests/Together.Tests.csproj -c Release
dotnet test tests/Together.Api.Tests/Together.Api.Tests.csproj -c Release
dotnet run --project tests/Together.BrowserTests -- --install
```

Актуальный end-to-end сценарий ДЗ № 5 выполняется на полном Compose-стенде с
PostgreSQL. После `docker compose up --build -d` запустите:

```powershell
$env:APP_URL = 'http://localhost:8080'
dotnet run --project tests/Together.BrowserTests -- --backend-smoke
Remove-Item Env:APP_URL
```

Сценарий Chromium проверяет регистрацию, загрузку серверного примера, сохранение
после перезагрузки и выход. Он использует вымышленные данные и возвращает ненулевой
код при ошибке. Не запускайте несколько копий одновременно: они записывают одни и
те же файлы отчёта.

Результаты и скриншоты: [docs/evidence](docs/evidence/).
Итог для ДЗ № 5: 32 модульных/компонентных и 4 API-теста прошли; Compose с
PostgreSQL и отдельный backend smoke-сценарий Chromium проверены на Linux-ВМ.
Существующий двухбраузерный набор без `--backend-smoke` относится к исторической
IndexedDB-версии ДЗ № 4 и не является проверкой текущего backend.
Описание проверок, найденных дефектов и ограничений: [development_report.md](development_report.md).

GitHub автоматически выполняет:

- [CI](.github/workflows/ci.yml) — Release-сборку и оба xUnit-проекта;
- [Backend browser smoke](.github/workflows/browser-tests.yml) — Compose, PostgreSQL и Playwright Chromium;
- [Publish container image](.github/workflows/publish-container.yml) — публикацию AMD64/ARM64-образа в GHCR.

## Развёртывание готового образа

Для быстрого запуска без исходников и локальной сборки используйте отдельный
[`docker-compose.deploy.yml`](docker-compose.deploy.yml):

```bash
cp deploy.env.example .env.deploy
# PowerShell: Copy-Item deploy.env.example .env.deploy
# Обязательно замените POSTGRES_PASSWORD и выберите TOGETHER_IMAGE в .env.deploy
docker compose --env-file .env.deploy --file docker-compose.deploy.yml up -d
docker compose --env-file .env.deploy --file docker-compose.deploy.yml ps
```

Compose скачивает готовые образы приложения и PostgreSQL, ждёт готовности БД,
однократно применяет миграции и только после этого запускает приложение. Исходники,
.NET SDK и локальная сборка на целевом сервере не нужны. Успешно завершившийся
контейнер `migrate` со статусом `Exited (0)` — ожидаемое состояние.

Без переопределений `docker-compose.deploy.yml` публикует приложение только на
`http://127.0.0.1:8080`, что подходит для reverse proxy на том же хосте. Значения из
`.env.deploy` переопределяют эти настройки: перед запуском проверьте
`APP_BIND_ADDRESS` и `SECURE_COOKIES`. Для прямого доступа из сети установите
`APP_BIND_ADDRESS=0.0.0.0`; `SECURE_COOKIES=false` допустим только на изолированном
HTTP-стенде. В production используйте `SECURE_COOKIES=true` и завершайте TLS на
HTTPS reverse proxy или ingress.

Образ содержит API и опубликованный Blazor-клиент, слушает внутренний HTTP-порт
`8080` и работает от пользователя `app`. Compose настраивает:

- `ConnectionStrings__Together` — строка подключения к PostgreSQL;
- отдельный одноразовый запуск образа с аргументом `--migrate`;
- постоянные volumes `together-postgres` и `together-data-protection`;
- read-only root filesystem приложения и временный `/tmp`.

Проверки состояния: `/health` — доступность процесса, `/health/ready` — готовность
с подключением к PostgreSQL. Пароли и строку подключения передавайте через secret
store целевой инфраструктуры, а не через Docker build arguments или образ.

Для обновления измените `TOGETHER_IMAGE` на новый `v*` или `sha-*` тег и повторите
`up -d`. Остановка не удаляет данные:

```bash
docker compose --env-file .env.deploy --file docker-compose.deploy.yml down
```

Команда `down --volumes` удалит базу и ключи cookie без возможности восстановления;
используйте её только при намеренном полном сбросе после резервного копирования.

Workflow публикует:

- `latest` и `master` при push в `master`;
- `sha-<короткий SHA>` для каждого опубликованного коммита;
- `v*` при создании соответствующего Git-тега.

Подробности локального Compose-запуска, миграций и резервного копирования приведены
в [документации backend](backend_documentation.md).

## Структура

| Путь | Назначение |
| --- | --- |
| src/Together.Core | Модель, календарные расчёты, бюджет и валидация |
| src/Together.Client/Components | Формы, таблица, карточки и общие элементы |
| src/Together.Client/Pages | Основной экран и управление сохранением |
| src/Together.Contracts | DTO и маршруты API |
| src/Together.Api | Identity, HTTP API, EF Core и миграции PostgreSQL |
| src/Together.Client/Storage | API-клиент и адаптер старой IndexedDB для импорта |
| src/Together.Client/wwwroot/js | Доступ к старой IndexedDB для импорта и управление диалогом |
| tests/Together.Tests | Модульные и компонентные тесты |
| tests/Together.Api.Tests | Интеграционные тесты API и изоляции пользователей |
| tests/Together.BrowserTests | Актуальный backend smoke и исторические IndexedDB-сценарии ДЗ № 4 |
| docker-compose.yml | Локальная сборка и запуск из исходников |
| docker-compose.deploy.yml | Быстрое развёртывание готовых образов приложения и PostgreSQL |
| .github/workflows | CI, backend smoke и публикация multi-platform образа |
| docs | Исходное ТЗ, концепции, план и доказательства проверок |

Зависимости закреплены в .csproj и packages.lock.json.
package.json включён как дополнительная точка входа для команд из формата задания; npm-зависимостей у приложения нет.

## Хранение и границы MVP

Поездки хранятся в PostgreSQL и принадлежат вошедшему пользователю. Секреты находятся
только в переменных окружения. Импорта цен, бронирования и конвертации валют нет.
Копирование, сортировка и отметка выбора семьи отложены согласно ТЗ.

API проверяет revision и возвращает `409`, если запись уже изменена. Интерфейс
предлагает загрузить актуальные данные и сохраняет черновик до подтверждения.
Сохранение и новый вход требуют сети; расчёт открытой формы выполняется локально.

## Материалы для сдачи

- [Документация backend](backend_documentation.md)
- [Требования ДЗ № 5](docs/backend_requirements.md)
- [Материалы сдачи ДЗ № 5](SUBMISSION.md)
- [Отчёт о разработке ДЗ № 4](development_report.md)
- [Адаптированные промпт-шаблоны ДЗ 2](docs/prompt_templates.md)
- [Правила работы агента](AGENTS.md)
- [Результаты браузерных тестов](docs/evidence/browser-results.json)
- [Измерения производительности](docs/evidence/performance.json)

Репозиторий опубликован на [GitHub](https://github.com/Tipz/HT5), готовый образ —
в [GitHub Container Registry](https://github.com/Tipz/HT5/pkgs/container/ht5).
Публичный HTTPS-стенд в рамках репозитория не разворачивается.

## Использованная документация

- [Встроенные шаблоны .NET, включая blazorwasm](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates)
- [MudBlazor: установка](https://mudblazor.com/getting-started/installation)
- [Playwright .NET: установка и запуск](https://playwright.dev/dotnet/docs/intro)
