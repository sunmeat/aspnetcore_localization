## Локалізація додатків ASP.NET Core Web API + React

Локалізація — це не просто переклад кнопок на іншу мову.

У сучасному веб-застосунку локалізація може стосуватися:

- інтерфейсу користувача;
- повідомлень Web API;
- помилок валідації;
- дат, часу, чисел і валют;
- назв та описів даних;
- навіть контенту, який зберігається в базі даних.

ASP.NET Core має вбудовану підтримку globalization та localization, зокрема `CultureInfo`, `IStringLocalizer`, `.resx`-ресурси та вибір культури для HTTP-запиту.

Головний принцип:

> Локалізувати потрібно не весь код, а ті дані, які є мовними або залежать від культури.

---

## 1. Чому локалізація важлива

Якщо застосунок підтримує декілька мов, локалізацію краще закласти в архітектуру на початку розробки.

Інакше виникає типова ситуація:

```text
"Save"
"Delete"
"Invalid email"
"Player not found"
```

розкидані по:

```text
React
Controller
Service
Entity
SQL
JavaScript
```

Після цього додавання другої мови перетворюється на пошук рядків по всьому solution.

Правильний підхід:

```text
                    ┌──────────────────┐
                    │      User        │
                    └────────┬─────────┘
                             │
                       selected culture
                             │
          ┌──────────────────┴──────────────────┐
          ▼                                     ▼
┌─────────────────────┐               ┌─────────────────────┐
│      Frontend       │               │      Web API        │
│                     │               │                     │
│ UI translations     │               │ errors/messages     │
│ formatting          │               │ validation          │
│ dates/numbers       │               │ culture-aware data  │
└──────────┬──────────┘               └──────────┬──────────┘
           │                                     │
           │                                     ▼
           │                            ┌──────────────────┐
           │                            │    Database      │
           │                            │                  │
           │                            │ multilingual     │
           │                            │ content          │
           │                            └──────────────────┘
           │
           └──────────── API ────────────────►
```

---

# 2. Globalization vs Localization

Ці поняття часто плутають.

## Globalization

Globalization означає, що програма технічно готова працювати з різними культурами.

Наприклад:

```text
10/03/2026
03/10/2026
10.03.2026
```

можуть представляти одну й ту саму дату залежно від культури.

Також культура впливає на:

- формат дат;
- формат часу;
- десятковий роздільник;
- формат чисел;
- валюту;
- сортування;
- порівняння рядків;
- регіональні правила.

## Localization

Localization означає адаптацію застосунку до конкретної мови та культури.

Наприклад:

```text
en-US → English
uk-UA → Ukrainian
fr-FR → French
```

ASP.NET Core розділяє:

```text
CurrentCulture
```

і

```text
CurrentUICulture
```

`CurrentCulture` використовується для culture-dependent операцій:

```text
DateTime
numbers
currency
sorting
```

`CurrentUICulture` використовується для пошуку локалізованих ресурсів.

---

# 3. Де повинна бути локалізація

Для типового застосунку з React + ASP.NET Core Web API + Database можна виділити три рівні.

| Рівень | Що локалізуємо | Обов'язковість |
|---|---|---|
| Frontend | UI, кнопки, меню, повідомлення, форматування | Обов'язково для multilingual UI |
| Web API | validation, errors, messages, culture-dependent output | Обов'язково для user-facing API |
| Database | назви, описи, каталоги, контент | Тільки якщо сам контент багатомовний |

Важлива різниця:

> Не кожен шар повинен перекладати один і той самий текст.

---

# 4. Frontend

Frontend відповідає за те, що бачить користувач.

Наприклад:

```text
Players
Teams
Add player
Delete
Save
Cancel
```

це UI, тому локалізація **повинна** бути на frontend.

## Що локалізувати

### Обов'язково

- меню;
- кнопки;
- заголовки;
- лейбли елементів форм;
- підказки в текстових полях;
- повідомлення;
- модальні вікна;
- текст помилок валідації;
- текст всіх інших помилок на боці клієнта;
- accessibility-тексти.

### Також важливо

Форматування:

```text
date
time
number
currency
```

Наприклад:

```text
1 234,50 €
```

і

```text
€1,234.50
```

це різні culture-dependent представлення одного значення.

## Чим локалізувати React

Найпоширеніший підхід:

```text
i18next - базовий двіжок JS
react-i18next - спец-обгортка для середовища React / React Native (враховується нтеграція з життєвим циклом компонентів, перемальовка тощо)
```

Приклад структури:

```text
src/
└── locales/
    ├── en/
    │   └── translation.json
    ├── uk/
    │   └── translation.json
    └── fr/
        └── translation.json
```

Наприклад:

```json
{
  "player": {
    "title": "Players",
    "add": "Add player",
    "delete": "Delete"
  }
}
```

Frontend використовує ключ:

```javascript
t("player.add")
```

а не:

```javascript
"Add player"
```

Це важливо, тому що код не залежить від конкретної мови.

---

# 5. Backend Web API

Backend не повинен містити UI всього застосунку.

Наприклад, Web API не повинен повертати щось типу:

```json
{
  "message": "Click the green button to continue"
}
```

Це вже **відповідальність** frontend.

Але API повинен локалізувати повідомлення, які є частиною його контракту або помилок.

Наприклад:

```json
{
  "message": "Player was not found"
}
```

або:

```json
{
  "message": "The name field is required"
}
```

## Що локалізувати на backend

### Обов'язково

- validation messages;
- business errors, які показуються користувачу;
- error responses;
-  notification messages;
- повідомлення authentication/authorization, якщо вони повертаються користувачу.
- API response messages (якщо це текст, що може зрозуміти людина);
- server-generated documents.

### Не потрібно локалізувати

Внутрішні технічні повідомлення (той же console.log на продакшені зазвичай не повинен бути присутнім, або його використання має бути суворо обмеженим та контрольованим):

```text
NullReferenceException
SQL timeout
repository implementation details
debug logs
stack traces
```

Логи повинні бути технічними та придатними для розробника - "What happens on the server, stays on the server." :)

---

# 6. ASP.NET Core Localization

Для всіх моделей розробки ASP.NET Core стандартним інструментом є:

```text
Microsoft.Extensions.Localization
```

Основні компоненти:

```text
IStringLocalizer<T>
IStringLocalizerFactory
.resx
CultureInfo
RequestLocalizationMiddleware
```

Типова конфігурація:

```csharp
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
```

Далі визначаються підтримувані культури:

```text
en-US
uk-UA
fr-FR
```

і механізм визначення культури HTTP-запиту.

Наприклад:

```text
URL
Cookie
Accept-Language
```

ASP.NET Core підтримує `SupportedCultures` та `SupportedUICultures`.

---

# 7. Resource files

Для backend локалізації зручно використовувати файли `.resx` (або .json)

Наприклад:

```text
Resources/
├── SharedResource.uk-UA.resx
├── SharedResource.en-US.resx
└── SharedResource.fr-FR.resx
```

У коді:

```csharp
localizer["PlayerNotFound"]
```

Результат залежить від `CurrentUICulture`.

Наприклад:

```text
uk-UA → Гравця не знайдено
en-US → Player was not found
fr-FR → Joueur introuvable
```

Перевага такого підходу:

```text
код
  ↓
resource key
  ↓
localized resource
```

а не:

```text
if language == "uk"
    ...
else if language == "fr"
    ...
```

---

# 8. Validation

Окремо потрібно локалізувати validation.

Наприклад:

```csharp
[Required]
public string Name { get; set; }
```

Повідомлення:

```text
The Name field is required.
```

може бути різним для різних культур.

ASP.NET Core підтримує локалізацію DataAnnotations через:

```csharp
AddDataAnnotationsLocalization()
```

Це особливо важливо, коли validation виконується на backend.

Правило:

> Backend validation є джерелом істини для бізнес-правил. Frontend може дублювати просту validation для UX, але не повинен бути єдиним місцем перевірки.

---

# 9. Database

Ось тут починається найцікавіша частина.

Не кожне текстове поле в базі даних потрібно перекладати.

Наприклад:

```text
Player.Email
Player.Phone
Player.DateOfBirth
```

не є multilingual content.

А ось:

```text
Player.Name
Team.Name
Team.Description
Movie.Title
Movie.Description
Category.Name
```

можуть бути локалізованим контентом.

---

# 10. Коли локалізація в БД НЕ потрібна

Якщо значення однакове для всіх мов:

```text
Email
Phone
DateOfBirth
Price
StatusCode
ExternalId
```

зберігаємо одне значення.

Наприклад:

```text
price = 1500.50
```

а форматування виконуємо на frontend/backend залежно від culture.

Не потрібно створювати:

```text
price_uk
price_en
price_fr
```

---

# 11. Коли локалізація в БД дуже потрібна

Якщо самі дані є мовним контентом:

```text
description
title
name
content
```

і різні мови повинні мати різний текст.

Наприклад:

```text
Movie
├── Id
├── ReleaseDate
└── Translations
      ├── uk-UA
      ├── en-US
      └── fr-FR
```

---

# 12. Найкращий підхід для multilingual database

Замість:

```text
Movie
├── Id
├── TitleUk
├── TitleEn
├── TitleFr
├── DescriptionUk
├── DescriptionEn
└── DescriptionFr
```

краще використовувати таблицю перекладів.

Наприклад:

```text
Movie
----
Id
ReleaseDate


MovieTranslation
----------------
Id
MovieId
Culture
Title
Description
```

Дані:

```text
Movie
1 | 2026-09-01
```

```text
MovieTranslation

1 | 1 | uk-UA | Інтерстеллар | ...
2 | 1 | en-US | Interstellar | ...
3 | 1 | fr-FR | Interstellar | ...
```

Переваги:

- можна додати нову мову без зміни схеми `Movie`;
- кількість мов не зашивається в структуру таблиці;
- контент зберігається централізовано;
- легко вибирати потрібну локалізацію.

---

# 13. Де НЕ треба локалізувати Domain

У Clean Architecture це особливо важливо.

`Domain` не повинен знати про:

```text
ASP.NET Core
HTTP
React
IStringLocalizer
.resx / .json
SQL Server
```

Не варто робити:

```csharp
public class Player
{
    public string UkrainianName { get; set; }
    public string EnglishName { get; set; }
}
```

лише через те, що frontend має дві мови. Бо там де дві, там потім буде і двадцять дві.

**Domain повинен описувати предметну область, а не конкретний UI.**

Якщо multilingual content є бізнесовою частиною домену, тоді модель домену може містити концепцію `Translation`, але сама механіка перекладу не повинна бути прив'язана до ASP.NET Core.

---

# 14. Application layer

Application повинен працювати з даними, необхідними use case.

Наприклад:

```text
GetPlayer
CreatePlayer
UpdatePlayer
```

Application може:

- передати culture до потрібного сервісу;
- запросити localized data;
- сформувати DTO;
- повернути локалізовану бізнес-помилку.

Але Application не повинен перетворюватися на купу іфів

```text
if (culture == "uk")
else if (culture == "en")
else if (culture == "fr")
else if ще 10+ варіантів мов
```

для кожного UI-тексту.

Для повідомлень краще використовувати abstraction:

```text
IStringLocalizer
```

або власний application-level abstraction, якщо архітектура цього вимагає.

---

# 15. Хто за що відповідає

Зручно запам'ятати так:

```text
┌──────────────────────────────────────────────┐
│ FRONTEND                                     │
│                                              │
│ "Save"                                       │
│ "Delete"                                     │
│ "Players"                                    │
│ UI validation                                │
│ Date/number formatting                       │
└──────────────────────┬───────────────────────┘
                       │
                       │ API
                       ▼
┌──────────────────────────────────────────────┐
│ WEB API                                      │
│                                              │
│ Validation messages                          │
│ Business errors                              │
│ HTTP error messages                          │
│ Culture-aware formatting                     │
└──────────────────────┬───────────────────────┘
                       │
                       │ data
                       ▼
┌──────────────────────────────────────────────┐
│ DATABASE                                     │
│                                              │
│ Multilingual content                         │
│ Titles                                       │
│ Descriptions                                 │
│ Names                                        │
└──────────────────────────────────────────────┘
```

---

# 16. Що повинно залишатися універсальним

Є дані, які не треба перекладати взагалі.

Наприклад:

```text
Id
GUID
Email
URL
API endpoint
status code
database key
enum value
ISO code
```

Не можна перетворювати:

```text
Status = "Active"
```

на:

```text
Status = "Активний"
```

якщо `"Active"` є системним значенням.

Краще:

```text
Status = Active
```

а frontend локалізує:

```text
uk-UA → Активний
en-US → Active
fr-FR → Actif
```

---

# 17. API contract не повинен залежати від мови

Погано:

```json
{
  "status": "Активний"
}
```

Краще:

```json
{
  "status": "Active"
}
```

Або ще краще, якщо це частина строго визначеного контракту:

```json
{
  "status": "active"
}
```

Frontend вже визначає:

```text
active
  ↓
uk-UA → Активний
en-US → Active
fr-FR → Actif
```

Це робить API стабільним незалежно від мови користувача.

---

# 18. Як передавати culture

Один із практичних варіантів:

```http
Accept-Language: uk-UA
```

Наприклад:

```http
GET /api/players
Accept-Language: uk-UA
```

або:

```http
Accept-Language: fr-FR
```

Backend визначає культуру запиту.

Для web-застосунків також можуть використовуватися:

```text
URL:
    api/v1/**uk**/players

Cookie:
    culture=uk-UA

Accept-Language:
    uk-UA
```

Вибір механізму залежить від архітектури.

Для API природним є саме `Accept-Language`.

---

# 19. Де краще зберігати вибрану мову

Для frontend зазвичай використовується гібридний підхід:

```text
URL - основне джерело правди та золотий стандарт для SEO та UX, посилання накшталт https://amazonclone.com/uk/about
localStorage та Cookie - при першому візиті, якщо в URL мови немає
browser language - якщо і в кукі порожньо, береться navigator.language
user profile - збереження мови в базі даних на сервері, прив'язане до облікового запису користувача
```

Наприклад:

```text
browser language
       ↓
default culture
       ↓
user selects language
       ↓
save preference
       ↓
send culture to API
```

У production-застосунку варто визначити єдине правило пріоритетів.

Наприклад:

```text
explicit user choice
        ↓
user profile
        ↓
URL
        ↓
Cookie
        ↓
Accept-Language
        ↓
default culture (останній захисний шар -fallback, коли жодне з попередніх джерел не повернуло підтримувану мову або коли запит віддає невідому мову)
```

---

# 20. Найефективніші інструменти

## Frontend

Для React:

```text
i18next
react-i18next
```

Для форматування:

```text
Intl
Intl.DateTimeFormat
Intl.NumberFormat
```

Це дозволяє не тільки перекладати текст, але й правильно форматувати culture-dependent дані.

---

## ASP.NET Core Web API

Основні стандартні інструменти:

```text
Microsoft.Extensions.Localization
IStringLocalizer<T>
IStringLocalizerFactory
.resx / .json
RequestLocalizationMiddleware
CultureInfo
DataAnnotations localization
```

Для ASP.NET Core це базовий і добре інтегрований механізм.

---

## Database

Універсального `i18next для SQL` немає.

Найчастіше використовують:

```text
Translation table
```

наприклад:

```text
Entity
EntityTranslation
```

або окремі translation entities.

Для EF Core це природно моделюється через relationship:

```text
Movie 1 ─────── * MovieTranslation
```

Доречі, ідея автоматичного перекладу контенту з БД за допомогою нейромереж (LLM) у реальному часі виглядає привабливо, оскільки вона повністю знімає потребу розробляти класичну таблицю MovieTranslation чи вручну заповнювати дублікати для кожної мови.

Проте, у production-системах реальний час (on-the-fly) для кожної HTML/API-сесії майже ніколи не використовують як основне рішення. Чому:
- Генерація відповіді через LLM додає від 300ms до кількох секунд до кожного HTTP-запиту. Користувачі очікують відповіді БД за <50ms
- Кожен перегляд сторінки тисячами користувачів миттєво спалюватиме API-токени (OpenAI, Claude тощо) за один і той самий текст
- Нейромережа може перекласти одну й ту саму назву фільму чи категорії по-різному на двох сусідніх сторінках або навіть при кожному оновленні
- Додаток стає залежним від зовнішнього API. Якщо сервіс LLM упаде або перевищить ліміти (rate limit), база даних "втратить" усі мови, крім дефолтної!

---

# 21. !!! Що НЕ варто робити !!!

## 1. Hardcode тексту в коді

Погано:

```javascript
<button>Delete</button>
```

Краще:

```javascript
<button>{t("common.delete")}</button>
```

---

## 2. Перекладати на бекенді весь UI

Погано:

```json
{
  "message": "Натисніть зелену кнопку"
}
```

API не повинен керувати UI!

---

## 3. Зберігати переклади в окремих колонках

Погано:

```text
title_uk
title_en
title_fr
title_de
...
```

Краще:

```text
Entity
EntityTranslation
```

---

## 4. Зберігати переклад enum у БД

Погано:

```text
status = "Активний"
```

Краще:

```text
status = "active"
```

і локалізувати при відображенні.

---

## 5. Локалізувати технічні логи

Погано:

```text
[uk-UA] Не вдалося підключитися до бази даних
[en-US] Unable to connect to database
```

Логи призначені для технічного аналізу, не для кінцевих користувачів сайту.
Краще мати стабільні machine-readable повідомлення та структуровані поля.

---

# 22. Рекомендована архітектура для цього проєкту

Як варіант, можна побудувати таку структуру:

```text
Soccer.sln
│
├── Soccer.Domain
│   ├── Entities
│   └── Interfaces
│
├── Soccer.Application
│   ├── DTO
│   ├── Services
│   ├── Interfaces
│   └── Mapping
│
├── Soccer.Infrastructure
│   ├── Persistence
│   ├── Repositories
│   └── Localization !!!
│
├── Soccer.WebAPI
│   ├── Controllers
│   ├── Resources
│   │   ├── SharedResource.uk-UA.resx !!!
│   │   ├── SharedResource.en-US.resx !!!
│   │   └── SharedResource.fr-FR.resx !!!
│   └── Program.cs
│
└── react.client
    └── src
        ├── locales !!!
        │   ├── uk  !!!
        │   ├── en  !!!
        │   └── fr  !!!
        ├── components
        └── services
```

---

# 23. Підсумкова схема

```text
                         USER
                          │
                    selected language
                          │
            ┌─────────────┴─────────────┐
            │                           │
            ▼                           ▼
       React Client                 Web API
            │                           │
            │ i18next                   │ IStringLocalizer
            │                           │ CultureInfo
            │                           │
            │                           ▼
            │                    Application / Domain
            │                           │
            │                           ▼
            │                    Infrastructure
            │                           │
            │                           ▼
            │                      Database
            │
            └──────────── API ────────────────┘
```

Головне правило:

```text
UI text:
    → локалізація на фронтенді

API errors / validation:
    → локалізація на бекенді

Multilingual business content:
    → локалізація на рівні таблиць / колекцій БД

Dates / numbers / currency
    → Culture-aware formatting (відображення дат, часу, чисел та валют
      відповідно до регіональних стандартів і культурних норм конкретної країни чи мови
      JS:
      new Intl.NumberFormat('uk-UA').format(1234567.89);
      new Intl.NumberFormat('uk-UA', { style: 'currency', currency: 'UAH' }).format(100);
      C#:
      decimal amount = 1234.56m;
      string ukText = amount.ToString("C", new CultureInfo("uk-UA"));

Technical identifiers / enum values:
    → ніколи не перекладається
```

---

# 24. Практичне правило для розробника

Перед тим як локалізувати будь-який текст, потрібно поставити питання:

> Хто є власником цього тексту?

### Якщо це UI:

```text
Frontend
```

### Якщо це повідомлення API:

```text
Backend
```

### Якщо це контент предметної області:

```text
Database
```

### Якщо це технічне значення:

```text
Не локалізувати
```

Саме так локалізація залишається частиною архітектури, а не перетворюється на хаотичний пошук `if language == ...` по всіх папках проєкту.

---

## Корисні ресурси

- ASP.NET Core Localization:
  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization

- .NET Localization:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/localization

- .NET Resources:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/resources

- Globalization and Localization:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization-and-localization

- React i18next:
  https://react.i18next.com/

- i18next:
  https://www.i18next.com/

---

## Висновок

Локалізація повинна бути **розподілена за відповідальністю**, а не реалізована одним глобальним механізмом.

```text
Frontend
    ↓
локалізує інтерфейс

Web API
    ↓
локалізує validation та user-facing errors

Database
    ↓
зберігає multilingual business content

Culture
    ↓
визначає форматування дат, чисел та інших culture-dependent значень
```

При цьому внутрішні шари Clean Architecture не повинні ставати залежними від конкретного UI або конкретної мови!

Це дозволяє додати `uk-UA`, `en-US`, `fr-FR` або іншу культуру без переписування бізнес-логіки.
