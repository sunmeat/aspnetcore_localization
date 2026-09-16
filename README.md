# Soccer — Clean Architecture

Навчальний проєкт на **ASP.NET Core MVC**, який демонструє основні принципи **Clean Architecture** Роберта С. Мартіна.

Мета проєкту — показати, як організувати код так, щоб:

- бізнес-логіка була незалежною від фреймворків і конкретної бази даних;
- зміни в одній частині системи мінімально впливали на інші;
- код було легко тестувати, розширювати й підтримувати;
- кожен шар мав чітку відповідальність;
- залежності були спрямовані всередину, до ядра застосунку.

> **Важливо:** це навчальна реалізація Clean Architecture для CRUD-застосунку. Вона демонструє основні архітектурні принципи, але не претендує на використання всіх можливих практик DDD, CQRS або Enterprise Architecture.

---

## 🧠 Чому саме Clean Architecture?

У класичній тришаровій архітектурі часто використовують таку схему:

```text
Presentation → BLL → DAL
```

Такий підхід може працювати добре, але з часом бізнес-логіка іноді починає залежати від Entity Framework, SQL Server, HTTP, MVC або інших деталей реалізації.

Наприклад, сервіс може напряму використовувати `DbContext`, а бізнес-правила можуть опинитися всередині контролерів чи SQL-запитів. У результаті зміна технології зберігання даних або веб-фреймворку стає складнішою.

**Clean Architecture** пропонує інший підхід:

> Усі залежності спрямовані **всередину** — до ядра системи. Зовнішні шари можуть залежати від внутрішніх, але внутрішні шари не повинні знати про зовнішні деталі.

Це дає такі переваги:

1. **Domain** можна тестувати без бази даних і веб-сервера.
2. **Application** не залежить від Entity Framework Core, SQL Server або конкретної ORM.
3. Можна замінити SQL Server на PostgreSQL, Dapper, інше сховище або in-memory реалізацію, не змінюючи основну логіку застосунку.
4. Presentation можна замінити з MVC на Web API, Minimal API, Blazor або інший інтерфейс.
5. Код має зрозумілі межі відповідальності.
6. Залежності легко підміняти під час модульного тестування.

---

## 🏗️ Структура рішення

```text
Soccer.sln
│
├── Soccer.Domain                 ← Ядро системи
│   ├── Entities/
│   │   ├── Player.cs
│   │   └── Team.cs
│   └── Interfaces/
│       ├── IRepository.cs
│       └── IUnitOfWork.cs
│
├── Soccer.Common                 ← Спільні типи
│   └── Exceptions/
│       └── ValidationException.cs
│
├── Soccer.Application            ← Сценарії використання
│   ├── DTO/
│   ├── Interfaces/
│   ├── Services/
│   ├── Mapping/
│   └── DependencyInjection/
│
├── Soccer.Infrastructure         ← Технічні деталі
│   ├── Persistence/
│   ├── Repositories/
│   └── DependencyInjection/
│
└── Soccer.Presentation           ← ASP.NET Core MVC
    ├── Controllers/
    ├── Views/
    ├── wwwroot/
    ├── Program.cs
    └── appsettings.json
```

### Напрямок залежностей

```text
                    ┌─────────────────────┐
                    │     Presentation    │
                    │     ASP.NET MVC     │
                    └──────────┬──────────┘
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
                 ▼                           ▼
       ┌──────────────────┐       ┌────────────────────┐
       │   Application    │       │   Infrastructure   │
       │  Use Cases, DTO  │       │ EF Core, Repos     │
       └────────┬─────────┘       └──────────┬─────────┘
                │                            │
                │                            │
                └─────────────┬──────────────┘
                              ▼
                    ┌─────────────────────┐
                    │        Domain       │
                    │ Entities, Contracts │
                    └─────────────────────┘
```

`Presentation` може знати про `Application` та `Infrastructure` у композиційному корені. `Application` і `Domain` не повинні залежати від `Presentation` або конкретної реалізації `Infrastructure`.

---

## 📦 Детальний опис шарів

### 1. Soccer.Domain — ядро системи

`Domain` — найвнутрішній шар застосунку. Він містить основні сутності та контракти, які не залежать від зовнішніх технологій.

#### Що тут знаходиться

- Сутності `Player` і `Team`.
- Контракти репозиторіїв.
- Контракт `IUnitOfWork`.

Сутності є звичайними C#-класами. Вони не повинні залежати від:

- Entity Framework Core;
- `DbContext`;
- SQL Server;
- ASP.NET Core;
- MVC;
- HTTP;
- конкретних ORM або баз даних.

#### Чому це важливо

Якщо зміниться спосіб зберігання даних, сутності та основні правила предметної області не повинні змінюватися лише через цю технічну заміну.

Контракти в `Domain` демонструють **Dependency Inversion Principle**: внутрішній код залежить від абстракцій, а зовнішні реалізації ці абстракції реалізують.

> У цьому проєкті використовується спрощений generic Repository-підхід. Він зручний для демонстрації CRUD, але не є єдиним і завжди найкращим варіантом для production-застосунків.

---

### 2. Soccer.Common — спільні типи

`Common` містить типи, які використовуються кількома шарами, але не є сутностями предметної області.

Зараз тут розташований `ValidationException`.

Він може:

- виникати під час виконання операцій у `Application`;
- оброблятися в `Presentation`;
- перетворюватися на відповідний HTTP-відповідь.

#### Чому винесено в окремий проєкт

Окремий `Common` дозволяє уникнути дублювання типів між шарами.

Водночас наявність `Common` не є обов'язковою вимогою Clean Architecture. У невеликому проєкті спільний виняток можна розмістити інакше, залежно від його призначення.

Наприклад:

- винятки, пов'язані зі сценаріями застосунку, можна розмістити в `Application`;
- доменні винятки, що описують порушення бізнес-правил, можуть знаходитися в `Domain`;
- HTTP-специфічні винятки або middleware повинні залишатися в `Presentation`.

Також варто розрізняти різні ситуації:

```text
ValidationException  → некоректні вхідні дані
NotFoundException    → сутність не знайдена
BusinessRuleException → порушене бізнес-правило
```

У поточному навчальному прикладі використовується спрощений варіант із `ValidationException`.

---

### 3. Soccer.Application — сценарії використання

`Application` координує виконання операцій, які потрібні користувачеві або іншій частині системи.

Тут знаходиться прикладна логіка:

- DTO;
- сервіси;
- інтерфейси сервісів;
- mapping;
- реєстрація залежностей Application.

#### Основні компоненти

##### DTO

DTO використовуються для передавання даних між Presentation та Application.

Наприклад:

```text
PlayerDTO
TeamDTO
```

DTO не повинні автоматично ототожнюватися з Domain Entities. Вони формують контракт прикладного рівня і можуть містити лише ті дані, які потрібні конкретному сценарію.

##### Services

`PlayerService` і `TeamService` реалізують операції на кшталт:

- отримання всіх гравців;
- отримання гравця за ідентифікатором;
- створення гравця;
- оновлення гравця;
- видалення гравця;
- аналогічні операції для команд.

Application працює через абстракції та не звертається безпосередньо до `DbContext`.

##### Mapping

У проєкті використовується AutoMapper, налаштований через профіль.

Важливо, щоб mapping був послідовним. Для невеликого навчального проєкту також цілком допустимий ручний mapping, наприклад через приватні методи `ToDto()` і `ToEntity()`.

Ручний mapping часто навіть краще демонструє студентам:

- які поля переносяться;
- де відбувається перетворення;
- які дані дозволено змінювати;
- чим DTO відрізняється від Entity.

##### Dependency Injection

Метод `AddApplication()` реєструє Application-сервіси та AutoMapper.

Application не повинна знати, які конкретні класи Infrastructure будуть використані під час запуску.

---

### 4. Soccer.Infrastructure — реалізація технічних деталей

`Infrastructure` містить конкретні технології, необхідні для роботи застосунку.

#### Що тут знаходиться

- `SoccerContext` — контекст Entity Framework Core;
- реалізації репозиторіїв;
- `EFUnitOfWork`;
- налаштування persistence;
- метод `AddInfrastructure(connectionString)`.

#### Чому Infrastructure залежить від Domain

Infrastructure реалізує контракти, оголошені у внутрішніх шарах:

```text
IRepository<T>  ←  PlayerRepository
IUnitOfWork     ←  EFUnitOfWork
```

Тому Application може працювати з абстракціями, не знаючи про EF Core.

Якщо в майбутньому потрібно буде замінити EF Core на Dapper, PostgreSQL, MongoDB або інше сховище, основна логіка Application не повинна залежати від цієї заміни.

---

### 5. Soccer.Presentation — зовнішній шар

`Presentation` є ASP.NET Core MVC-застосунком.

Тут знаходяться:

- контролери;
- Razor Views;
- статичні файли;
- конфігурація;
- `Program.cs`;
- обробка HTTP-запитів і HTTP-відповідей.

Presentation відповідає за веб-рівень, але не повинна містити основну бізнес-логіку.

---

## 🧩 Composition Root і Program.cs

`Program.cs` є **композиційним коренем** застосунку.

Це місце, де конкретні реалізації зв'язуються з абстракціями через Dependency Injection.

```csharp
builder.Services.AddInfrastructure(connection);
builder.Services.AddApplication();
builder.Services.AddControllersWithViews();
```

Саме тут Presentation може одночасно знати про Application та Infrastructure.

Інші шари не повинні самостійно створювати залежності через `new`, якщо ці залежності мають надходити через DI.

### Чому це правильний підхід

Наприклад:

```text
Application потребує IUnitOfWork
             ↓
Infrastructure надає EFUnitOfWork
             ↓
Program.cs реєструє відповідність
```

Application не знає, що фактично буде використано `EFUnitOfWork`.

---

## 🔄 Потік виконання запиту

Розглянемо відкриття сторінки `/Teams/Index`.

```text
1. Користувач відкриває /Teams/Index
                    ↓
2. TeamsController
                    ↓
3. IEntityService<TeamDTO>
                    ↓
4. TeamService
                    ↓
5. IUnitOfWork / IRepository
                    ↓
6. EFUnitOfWork / TeamRepository
                    ↓
7. SoccerContext
                    ↓
8. SQL Server
                    ↓
9. Entity повертається в Application
                    ↓
10. Entity перетворюється на DTO
                    ↓
11. DTO передається в Controller
                    ↓
12. Controller передає модель у View
                    ↓
13. Razor View формує HTML-відповідь
```

Основна ідея полягає в тому, що кожен шар виконує свою роль:

- `Presentation` працює з HTTP та UI;
- `Application` координує сценарій;
- `Domain` містить сутності й абстракції;
- `Infrastructure` працює з базою даних.

---

## ✨ Що реалізовано

- CRUD для гравців і команд.
- Repository Pattern.
- Unit of Work.
- Entity Framework Core.
- DTO для прикладного рівня.
- AutoMapper із централізованим профілем.
- Dependency Injection.
- Окремі extension-методи для реєстрації Application та Infrastructure.
- Розділення Domain, Application, Infrastructure і Presentation.
- Обробка ситуації, коли сутність не знайдена.
- Демонстрація Dependency Rule та Dependency Inversion Principle.

---

## ⚖️ Архітектурні рішення та можливі альтернативи

Clean Architecture не означає, що існує лише один правильний спосіб організації кожного файлу. Важливо розуміти причини рішень, їхні переваги та компроміси.

### Repository Pattern: generic чи спеціалізований?

У поточному проєкті використовується:

```csharp
IRepository<T>
```

Це зручно для невеликого CRUD-прикладу, оскільки дозволяє повторно використовувати базові операції.

Однак generic repository має обмеження. Наприклад, метод:

```csharp
Task<T?> Get(string name);
```

припускає, що будь-яка сутність має властивість `Name`. Це вже робить інтерфейс не повністю універсальним.

Для більш складного застосунку можна використати спеціалізовані контракти:

```csharp
public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(int id);
    Task<IReadOnlyList<Player>> GetAllAsync();
    Task<IReadOnlyList<Player>> GetByTeamIdAsync(int teamId);
}
```

Переваги спеціалізованих репозиторіїв:

- методи виражають конкретні потреби предметної області;
- немає зайвих CRUD-методів;
- легше оптимізувати запити;
- інтерфейси стають зрозумілішими.

Отже:

```text
Generic Repository       → простіше для навчального CRUD
Specialized Repository   → часто виразніше для складного production-коду
```

Жоден із варіантів не є універсально правильним для всіх систем.

---

### Unit of Work чи без нього?

У проєкті використовується власний:

```csharp
IUnitOfWork
EFUnitOfWork
```

Це корисно для демонстрації координації кількох репозиторіїв і спільного збереження змін.

Однак Entity Framework Core вже має власний Unit of Work у вигляді `DbContext`. Тому додаткова обгортка не завжди потрібна.

Можливі варіанти:

```text
Application → IUnitOfWork → EFUnitOfWork → DbContext
```

або:

```text
Application → спеціалізовані абстракції → DbContext через Infrastructure
```

Вибір залежить від складності застосунку. Якщо власний Unit of Work не додає додаткової цінності, він може бути зайвою абстракцією.

Для навчального проєкту він залишається корисним, оскільки дозволяє продемонструвати патерн і принцип інверсії залежностей.

---

### Update через detached entity чи завантаження існуючої?

Спрощений підхід може виглядати так:

```csharp
var player = new Player
{
    Id = dto.Id,
    Name = dto.Name,
    Age = dto.Age,
    Position = dto.Position,
    TeamId = dto.TeamId
};

db.Entry(player).State = EntityState.Modified;
```

Для простого CRUD це може працювати, але в production-коді такий підхід має ризики:

- усі поля позначаються як змінені;
- можна випадково перезаписати дані;
- користувач може змінити поле, яке не повинен змінювати;
- складніше реалізувати часткове оновлення;
- не вирішується concurrency-контроль.

Безпечніший підхід:

```text
1. Отримати існуючу сутність.
2. Перевірити, що вона існує.
3. Перевірити права та бізнес-правила.
4. Змінити дозволені поля.
5. Зберегти зміни.
```

Приклад:

```csharp
var player = await unitOfWork.Players.GetByIdAsync(dto.Id);

if (player is null)
    throw new NotFoundException("Гравця не знайдено.");

player.Name = dto.Name;
player.Age = dto.Age;
player.Position = dto.Position;
player.TeamId = dto.TeamId;

await unitOfWork.SaveChangesAsync();
```

Для складніших систем також можуть знадобитися:

- optimistic concurrency;
- `RowVersion`;
- перевірка прав доступу;
- окремі команди для оновлення;
- часткове оновлення через PATCH.

---

### AutoMapper чи ручний mapping?

У поточному проєкті використовується AutoMapper.

Це зручно, коли:

- DTO багато;
- моделі мають схожу структуру;
- mapping повторюється;
- правила перетворення централізовані.

Але для невеликого CRUD-проєкту ручний mapping часто є простішим:

```csharp
private static PlayerDTO ToDto(Player player)
{
    return new PlayerDTO
    {
        Id = player.Id,
        Name = player.Name,
        Age = player.Age,
        Position = player.Position,
        TeamId = player.TeamId,
        Team = player.Team?.Name
    };
}
```

Переваги ручного mapping:

- очевидно, які поля переносяться;
- немає прихованої конфігурації;
- простіше відлагоджувати;
- не потрібна додаткова бібліотека.

Для навчання ручний mapping може бути навіть кращим. AutoMapper доречний, якщо він справді зменшує дублювання і не приховує важливу логіку.

---

### Exceptions чи Result-підхід?

У поточному проєкті для окремих помилкових ситуацій використовується exception-підхід.

Наприклад:

```text
Application → ValidationException
Presentation → HTTP 404
```

Це допустимо, але винятки не завжди є найкращим способом передавання очікуваних результатів.

Альтернативою може бути Result-підхід:

```csharp
public record Result<T>(
    bool IsSuccess,
    T? Value,
    string? Error);
```

Тоді Application повертає результат:

```text
Success → DTO
Failure → NotFound / Validation / BusinessRule
```

Переваги Result-підходу:

- очікувані помилки є частиною контракту;
- менше винятків для звичайного потоку виконання;
- зручніше будувати складні сценарії;
- легше повертати кілька типів помилок.

Водночас для невеликого MVC-проєкту exceptions можуть бути простішими для пояснення.

---

### Де повинні знаходитися інтерфейси?

У поточному проєкті інтерфейси репозиторіїв і Unit of Work розміщені в `Domain`.

Це допустимий варіант Clean Architecture: внутрішній шар визначає контракти, а Infrastructure їх реалізує.

Але існує й інший поширений підхід:

```text
Soccer.Application
└── Interfaces
    ├── IPlayerRepository.cs
    ├── ITeamRepository.cs
    └── IUnitOfWork.cs
```

У цьому варіанті Application визначає саме ті контракти, які потрібні його сценаріям.

Порівняння:

```text
Контракти в Domain
→ підходить, коли вони є частиною моделі та абстракції предметної області.

Контракти в Application
→ підходить, коли вони описують потреби конкретних use cases.
```

Важливо не механічно переміщувати інтерфейси, а розуміти, хто є власником контракту.

---

### Чи обов'язково мати Common?

Ні. `Common` не є обов'язковим шаром Clean Architecture.

Він може бути корисним, якщо там знаходяться справді спільні, незалежні від конкретного шару типи.

Але надто великий `Common` часто перетворюється на «ящик для всього»:

```text
Helpers
Utils
Extensions
Constants
Exceptions
DTO
Services
...
```

Це погіршує структуру.

Краще розміщувати тип там, де він має найбільш природну відповідальність:

- доменні правила → `Domain`;
- сценарії та application-контракти → `Application`;
- EF Core та зовнішні інтеграції → `Infrastructure`;
- HTTP-специфічні речі → `Presentation`.

---

## 🚀 Як запустити

Клонуйте репозиторій:

```bash
git clone https://github.com/sunmeat/aspnetcore_clean.git
cd aspnetcore_clean
```

Відновіть залежності:

```bash
dotnet restore
```

Запустіть Presentation-проєкт:

```bash
dotnet run --project Soccer.Presentation
```

Перед запуском перевірте файл:

```text
Soccer.Presentation/appsettings.json
```

Рядок підключення `DefaultConnection` має вказувати на доступний SQL Server.

Після запуску відкрийте в браузері адресу, яку буде виведено в консолі, наприклад:

```text
https://localhost:xxxx/Teams/Index
```

---

## 🛠️ Технологічний стек

| Технологія | Призначення |
|---|---|
| ASP.NET Core MVC | Presentation layer |
| Entity Framework Core | Робота з базою даних |
| SQL Server | Сховище даних |
| AutoMapper | Перетворення Entity ↔ DTO |
| Dependency Injection | Керування залежностями |
| Repository Pattern | Абстракція доступу до даних |
| Unit of Work | Координація збереження змін |
| .NET | Платформа виконання |

---

## 🎯 Для кого цей проєкт

Проєкт призначений для:

- тих, хто вивчає Clean Architecture;
- студентів, які хочуть зрозуміти Dependency Rule;
- розробників, які переходять від монолітного CRUD до шаруватої архітектури;
- тих, хто хоче побачити практичне розділення Domain, Application, Infrastructure та Presentation;
- тих, хто вивчає Repository Pattern, Unit of Work і Dependency Injection.

Проєкт навмисно залишається невеликим. Його мета — не продемонструвати максимальну кількість патернів, а показати зрозумілу архітектурну основу.

> Хороша архітектура — це не найбільша кількість проєктів, інтерфейсів і папок. Це зрозумілі межі, контроль залежностей і рішення, які відповідають складності системи.
