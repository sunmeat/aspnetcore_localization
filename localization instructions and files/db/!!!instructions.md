# Локалізація текстового контенту в БД

> Стосується **БД** застосунку ASP.NET Core Web API + React (чи будь-якому іншому стеку), коли **сам контент** є багатомовним — на відміну від UI (frontend) чи повідомлень API (backend), які локалізуються окремо, поза БД.

---

## 0. Головне правило

> Локалізувати в БД треба не весь текст, а лише той, що є **мовним контентом предметної області** — назви, описи, заголовки тощо, які реально відрізняються залежно від мови користувача.

Технічні значення (email, статуси, коди, ID) **ніколи** не перекладаються і не дублюються по мовах у БД.

---

## 1. Що переклад в БД — а що ні

### 1.1. Перекладати ТРЕБА (мовний контент)

Приклади полів, значення яких залежать від мови:

- `Name` / `Title` (назва товару, фільму, категорії, команди, гравця-псевдоніма тощо);
- `Description` (опис);
- `Content` / `Body` (текст статті, новини);
- `ShortSummary`, `Tagline`, `Slogan`;
- SEO-поля: `MetaTitle`, `MetaDescription`;
- назви категорій, тегів, довідників, які показуються користувачу.

### 1.2. Перекладати НЕ ТРЕБА

| Тип даних | Приклад | Чому |
|---|---|---|
| Ідентифікатори | `Id`, `GUID`, `ExternalId` | технічні, не мовні |
| Контактні дані | `Email`, `Phone` | однакові для всіх мов |
| Дати/час | `DateOfBirth`, `CreatedAt` | форматуються на UI за культурою, а не перекладаються |
| Числа/гроші | `Price`, `Amount` | форматуються (`1 234,50 ₴` vs `$1,234.50`), значення саме число не дублюється |
| Статуси/enum | `Status = "Active"` | системне значення; переклад робиться на frontend (`active → Активний`) |
| Коди | `CountryCode`, `CurrencyCode`, `StatusCode` | стандартизовані ISO-подібні коди |
| URL / slug (як правило) | `/products/laptop-15` | якщо slug мовний — це окремий випадок, див. п. 6 |

**Не робити так** (антипатерн — окремі колонки під кожну мову):

```
Movie
├── TitleUk
├── TitleEn
├── TitleFr
├── DescriptionUk
├── DescriptionEn
└── DescriptionFr
```

Проблеми: при додаванні нової мови треба міняти схему таблиці (`ALTER TABLE`), важко масштабувати, багато `NULL`-полів.

---

## 2. Правильний підхід: таблиця/колекція перекладів (Translation table)

Замість колонок під кожну мову — окрема сутність-переклад, зв'язана 1-до-багатьох з основною сутністю:

```
Movie                       MovieTranslation
-----                       -----------------
Id                          Id
ReleaseDate                 MovieId (FK)
Price                       Culture (uk-UA / en-US / fr-FR)
...                         Title
                            Description
```

Один рядок в `Movie` — один фільм. Кілька рядків в `MovieTranslation` — по одному на кожну підтримувану культуру.

Переваги:
- нова мова додається без зміни схеми базової таблиці;
- немає обмеження на кількість мов;
- контент централізований, легко вибрати потрібну локаль (`WHERE Culture = @culture`);
- легко визначити fallback-мову, якщо перекладу ще немає.

---

## 3. Реалізація для реляційної СУБД (SQL Server)

### 3.1. Кроки

1. **Виділити сутності, що потребують перекладу** (наприклад: `Movie`, `Category`, `Team`, `Player`).
2. Для кожної такої сутності **прибрати мовні поля з основної таблиці** та перенести в окрему таблицю-переклад.
3. Створити таблицю-довідник культур `Culture` (необов'язково, але зручно для валідації та адміністрування).
4. Створити таблицю перекладів `<Entity>Translation` з унікальним ключем `(EntityId, CultureId)`.
5. Додати індекси для швидкого пошуку за культурою.
6. У бекенді (Entity Framework Core) описати зв'язок `Movie 1 ─── * MovieTranslation`.
7. При читанні даних — приєднувати переклад для поточної культури запиту (`Accept-Language` → `CultureInfo.CurrentUICulture`), з fallback на дефолтну мову, якщо перекладу немає.
8. При записі/редагуванні контенту — адмінка/форма повинна дозволяти вводити переклад окремо для кожної підтримуваної мови (upsert рядка в таблиці перекладів).

### 3.2. Приклад схеми (DDL)

```sql
-- Довідник культур (опційно, але рекомендовано)
CREATE TABLE Culture (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Code     NVARCHAR(10)  NOT NULL UNIQUE,   -- 'uk-UA', 'en-US', 'fr-FR'
    IsDefault BIT NOT NULL DEFAULT 0
);

INSERT INTO Culture (Code, IsDefault) VALUES
('uk-UA', 1),
('en-US', 0),
('fr-FR', 0);

-- Базова сутність: тільки НЕ мовні поля
CREATE TABLE Movie (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ReleaseDate DATE          NOT NULL,
    Price       DECIMAL(10,2) NULL,
    Status      NVARCHAR(30)  NOT NULL DEFAULT 'active'  -- НЕ перекладається
);

-- Таблиця перекладів
CREATE TABLE MovieTranslation (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    MovieId     INT NOT NULL,
    CultureId   INT NOT NULL,
    Title       NVARCHAR(300) NOT NULL,
    Description NVARCHAR(MAX) NULL,

    CONSTRAINT FK_MovieTranslation_Movie
        FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MovieTranslation_Culture
        FOREIGN KEY (CultureId) REFERENCES Culture(Id),

    CONSTRAINT UQ_MovieTranslation_Movie_Culture
        UNIQUE (MovieId, CultureId)   -- один переклад на мову для одного фільму
);

CREATE INDEX IX_MovieTranslation_CultureId ON MovieTranslation(CultureId);
```

Приклад даних:

```sql
INSERT INTO Movie (ReleaseDate, Price, Status) VALUES ('2026-09-01', 199.99, 'active'); -- Id = 1

INSERT INTO MovieTranslation (MovieId, CultureId, Title, Description) VALUES
(1, 1, N'Інтерстеллар', N'Науково-фантастичний фільм...'),   -- uk-UA
(1, 2, 'Interstellar', 'A science fiction film...'),          -- en-US
(1, 3, 'Interstellar', 'Un film de science-fiction...');      -- fr-FR
```

### 3.3. Типовий запит із fallback на дефолтну мову

```sql
SELECT
    m.Id,
    m.ReleaseDate,
    m.Price,
    m.Status,
    COALESCE(mt_req.Title, mt_def.Title)             AS Title,
    COALESCE(mt_req.Description, mt_def.Description) AS Description
FROM Movie m
LEFT JOIN MovieTranslation mt_req
    ON mt_req.MovieId = m.Id AND mt_req.CultureId = @requestedCultureId
LEFT JOIN MovieTranslation mt_def
    ON mt_def.MovieId = m.Id AND mt_def.CultureId = @defaultCultureId
WHERE m.Id = @movieId;
```

### 3.4. Entity Framework Core (модель)

```csharp
public class Movie
{
    public int Id { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public decimal? Price { get; set; }
    public string Status { get; set; } = "active"; // не локалізується

    public ICollection<MovieTranslation> Translations { get; set; } = new List<MovieTranslation>();
}

public class MovieTranslation
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string Culture { get; set; } = null!; // "uk-UA", "en-US"...
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public Movie Movie { get; set; } = null!;
}
```

```csharp
// Fluent API
modelBuilder.Entity<MovieTranslation>()
    .HasIndex(t => new { t.MovieId, t.Culture })
    .IsUnique();

modelBuilder.Entity<Movie>()
    .HasMany(m => m.Translations)
    .WithOne(t => t.Movie)
    .HasForeignKey(t => t.MovieId)
    .OnDeleteCascade();
```

### 3.5. Практичні поради для SQL Server

- Використовувати `NVARCHAR` (не `VARCHAR`) для мовних полів — підтримка Unicode (кирилиця, французькі діакритики тощо).
- Унікальний ключ `(EntityId, CultureId)` захищає від дублікатів перекладу.
- Якщо перекладів багато сутностей — можна винести спільний шаблон через generic-репозиторій/базовий клас `ITranslatable<T>`, але кожна сутність має власну таблицю перекладів (а не одну "універсальну" таблицю з `EntityType` + `EntityId` — це ускладнює FK-зв'язки і індексацію; допустимо лише для дуже простих довідників).
- Для довідників (наприклад, `Category`) — той самий підхід: `Category` / `CategoryTranslation`.

---

## 4. Реалізація для NoSQL (Firebase — Firestore / Realtime Database)

У Firebase немає жорсткої схеми і немає JOIN, тому переклади зберігаються **вкладеним об'єктом (map) прямо в документі сутності**, а не окремою "таблицею" з FK.

### 4.1. Кроки

1. Визначити ті самі сутності й поля, що і для SQL (п. 1–2) — принцип "що перекладати" не залежить від типу БД.
2. У документі сутності створити поле `translations` (map), де ключ — код культури (`uk-UA`, `en-US`, `fr-FR`), значення — об'єкт з мовними полями.
3. Не мовні поля (ціна, статус, дати) лишаються на верхньому рівні документа, поза `translations`.
4. Визначити дефолтну культуру (наприклад, зберігати константу в конфігу застосунку, а не в кожному документі) для fallback, якщо перекладу немає.
5. На бекенді/клієнті при читанні документа брати `translations[currentCulture]`, і якщо відсутній — `translations[defaultCulture]`.
6. Для пошуку/фільтрації за перекладеним текстом враховувати обмеження Firestore (див. 4.4).

### 4.2. Firestore — приклад структури документа

Колекція `movies`, документ `movieId`:

```json
{
  "releaseDate": "2026-09-01",
  "price": 199.99,
  "status": "active",
  "translations": {
    "uk-UA": {
      "title": "Інтерстеллар",
      "description": "Науково-фантастичний фільм..."
    },
    "en-US": {
      "title": "Interstellar",
      "description": "A science fiction film..."
    },
    "fr-FR": {
      "title": "Interstellar",
      "description": "Un film de science-fiction..."
    }
  }
}
```

Читання конкретної локалі (JS SDK, приклад):

```javascript
const docSnap = await getDoc(doc(db, "movies", movieId));
const data = docSnap.data();

const t = data.translations[currentCulture]
       ?? data.translations[defaultCulture]; // fallback

const title = t.title;
const description = t.description;
```

### 4.3. Realtime Database — аналогічна структура

```json
{
  "movies": {
    "movie1": {
      "releaseDate": "2026-09-01",
      "price": 199.99,
      "status": "active",
      "translations": {
        "uk-UA": { "title": "Інтерстеллар", "description": "..." },
        "en-US": { "title": "Interstellar", "description": "..." },
        "fr-FR": { "title": "Interstellar", "description": "..." }
      }
    }
  }
}
```

### 4.4. Особливості та обмеження Firebase, які треба врахувати

- **Немає JOIN** — переклад завжди зберігається разом із документом (denormalized). Це нормально для NoSQL: дублювання структури заради швидкості читання.
- **Пошук за перекладеним текстом**: Firestore не підтримує повнотекстовий пошук і пошук по вкладених полях зі складною логікою "мова + текст" ефективно. Для пошуку по назвах різними мовами варто:
  - або тримати окреме "плоске" поле-індекс, наприклад `searchIndex: { "uk-UA": "інтерстеллар", "en-US": "interstellar" }` з нормалізованим (lowercase) текстом;
  - або використовувати зовнішній пошуковий сервіс (Algolia, ElasticSearch, Typesense) поруч з Firestore.
- **Розмір документа**: ліміт документа Firestore — 1 MB. Якщо мов і текстів багато (наприклад, довгий `content` статті на 10+ мовах), варто винести переклади в підколекцію:
  ```
  movies/{movieId}/translations/{cultureCode}
  ```
  Це дає змогу читати лише потрібну мову одним запитом (`getDoc`), не завантажуючи весь документ з усіма мовами.
- **Валідація культур**: оскільки Firestore не має enum/foreign key, список підтримуваних культур варто валідувати на рівні бекенд-коду (наприклад, `CultureInfo`/enum на сервері), а не покладатись на структуру БД.
- **Правила безпеки (Security Rules)**: якщо клієнт пише переклади напряму, обмежити запис полем `translations.*` окремими правилами, щоб не зіпсувати не мовні поля.

### 4.5. Варіант із підколекцією (рекомендовано для великого контенту)

```
movies (collection)
  └── movieId (document: releaseDate, price, status)
        └── translations (subcollection)
              ├── uk-UA (document: { title, description })
              ├── en-US (document: { title, description })
              └── fr-FR (document: { title, description })
```

```javascript
// Отримати переклад лише для потрібної мови — без завантаження всього документа
const tSnap = await getDoc(doc(db, "movies", movieId, "translations", currentCulture));
const t = tSnap.exists() ? tSnap.data() : await getDoc(doc(db, "movies", movieId, "translations", defaultCulture));
```

---

## 5. Спільна логіка вибору мови (SQL і NoSQL однаково)

Незалежно від типу БД, порядок визначення культури запиту той самий (як і на рівні API):

```
1. явний вибір користувача (query/param)
2. профіль користувача (збережена мова в акаунті)
3. URL (/uk/movies/...)
4. Cookie
5. Accept-Language заголовок
6. дефолтна культура застосунку (fallback)
```

Fallback на дефолтну мову контенту (якщо перекладу конкретною мовою ще немає в БД) обов'язковий і для SQL Server (COALESCE у запиті), і для Firebase (перевірка наявності ключа в `translations`).

---

## 6. Особливі випадки

- **Slug/URL для SEO** (`/uk/movies/interstellar`, `/en/movies/interstellar`): якщо потрібен мовний slug — його теж варто зберігати в таблиці/об'єкті перекладу (`Slug` поруч з `Title`), а не генерувати на льоту.
- **Enum-подібні довідники, що показуються користувачу** (наприклад, жанри фільмів `Genre`): якщо список жанрів фіксований і має власні назви різними мовами — це теж окрема сутність-переклад (`Genre` / `GenreTranslation`), а не hardcode на frontend, якщо жанри керуються через адмінку.
- **Автопереклад через LLM "на льоту"** — не рекомендується як основний механізм для продакшена: додає затримку (сотні мс — секунди на запит), витрати на токени при кожному перегляді, ризик неконсистентного перекладу однієї й тієї ж назви та залежність від зовнішнього сервісу. LLM можна використовувати як **допоміжний інструмент для одноразового заповнення** таблиці/колекції перекладів (наприклад, скрипт для первинного наповнення `MovieTranslation` чи `translations`), після чого контент редагується вручну.

---

## 7. Побажання: якщо планується швидкий перехід SQL Server ↔ Firebase

Якщо є ймовірність, що проєкт мігрує з SQL Server на Firebase (чи навпаки), варто із самого початку проєктувати архітектуру перекладів так, щоб ця заміна торкалась **лише Infrastructure**, а не Domain/Application. Нижче — що саме зміниться, а що має лишитись незмінним.

### 7.1. Що НЕ повинно змінитися (якщо архітектура побудована правильно)

- **Domain-модель** (`Movie`, `MovieTranslation` як бізнес-поняття, метод `GetTranslation(culture)`, `AddOrUpdateTranslation(...)`) — див. розділ 3.4.1. Domain не знає, що таке SQL Server чи Firestore, тому заміна БД його не торкається.
- **Application-логіка** (use case'и `GetMovie`, `CreateMovie`, вибір культури запиту, порядок fallback з розділу 5) — працює з абстракціями (`IMovieRepository`), а не з конкретною БД.
- **Контракт API** (DTO, які повертає Web API у відповідь на запит з `Accept-Language`) — формат відповіді для frontend лишається тим самим незалежно від того, звідки дані фізично прийшли.
- **Frontend** — взагалі не повинен відчути різниці, оскільки він працює з тим самим API-контрактом.
- **Принцип "що перекладати, а що ні"** (розділ 1) — не залежить від типу БД.

### 7.2. Що ЗМІНИТЬСЯ при переході SQL Server → Firebase

| Було (SQL Server) | Стає (Firebase) |
|---|---|
| Таблиці `Movie` + `MovieTranslation` з FK | Документ `movies/{id}` з полем/підколекцією `translations` |
| `DbContext`, `IEntityTypeConfiguration<T>` | Firebase Admin SDK / клієнтський SDK, ручна серіалізація в/з JSON |
| SQL-запит з `JOIN` + `COALESCE` для fallback (розділ 3.3) | Читання документа + перевірка наявності ключа `translations[culture]`, ручний fallback у коді (розділ 4.2) |
| Реалізація `IMovieRepository` через EF Core | Нова реалізація `IMovieRepository` через Firestore SDK (той самий інтерфейс з Application-шару!) |
| Унікальність `(MovieId, CultureId)` гарантує `UNIQUE`-констрейнт БД | Унікальність гарантує сама структура документа (одна мова = один ключ мапи) — контроль тільки на рівні коду |
| Транзакційна цілісність через `SaveChanges()` в одній транзакції | Firestore-транзакції/batched writes — інша модель узгодженості, треба переглянути код запису перекладів |
| Міграції схеми (`Add-Migration`, `Update-Database`) | Немає міграцій схеми як таких — потрібен окремий скрипт-міграції даних (експорт з SQL → трансформація в JSON → імпорт у Firestore) |
| Індекси (`CREATE INDEX`) | Composite/single-field індекси Firestore налаштовуються окремо (`firestore.indexes.json`), особливо якщо потрібна фільтрація/сортування за перекладеними полями |
| Повнотекстовий пошук можливий через `LIKE`/`CONTAINS`/full-text index SQL Server | Треба або власне поле `searchIndex` (розділ 4.4), або зовнішній пошуковий сервіс (Algolia тощо) — це нова інфраструктурна залежність |

### 7.3. Що ЗМІНИТЬСЯ при переході Firebase → SQL Server

Дзеркально до попереднього пункту:

- Дані з `translations` (map/підколекція) потрібно розкласти на реляційну структуру `Entity` / `EntityTranslation` (нормалізація).
- Для кожної раніше "вільної" структури документа треба спроєктувати чітку схему з типами колонок, довжинами (`NVARCHAR(300)` тощо) та обмеженнями (`NOT NULL`, `UNIQUE`).
- З'являється потреба в міграціях схеми (EF Core Migrations або SQL-скрипти) — тепер зміна структури контенту (наприклад, додавання нового перекладеного поля) вимагає `ALTER TABLE`.
- Транзакційність і зв'язки (FK, каскадне видалення) стають природними — можна прибрати ручні перевірки цілісності, які були потрібні на рівні коду в Firebase.
- Повнотекстовий пошук можна перенести на вбудовані можливості SQL Server (Full-Text Search) замість зовнішнього пошукового сервіса — за потреби.

### 7.4. Практичні рекомендації, щоб перехід був дешевим

1. **Ізолювати доступ до даних через інтерфейс** (`IMovieRepository`, `ITranslationRepository`) в Application-шарі. Заміна БД = нова реалізація інтерфейсу в Infrastructure, без змін у контролерах чи use case'ах.
2. **Не "протікати" деталі конкретної БД в DTO/Domain** — уникати того, щоб Domain/Application-код напряму формував SQL-запити чи знав про структуру Firestore-документа.
3. **Тримати логіку вибору культури та fallback в Application**, а не в Infrastructure — тоді вона одна, спільна, незалежно від БД (розділ 5 уже написаний саме так).
4. **Проєктувати формат "проміжного" DTO для перекладів** (наприклад, `Dictionary<string, TranslationDto>`), який однаково легко змапити і з SQL-рядків (`GROUP BY MovieId`), і з Firestore map/підколекції.
5. Якщо перехід дійсно ймовірний найближчим часом — розглянути **репозиторій-адаптер**, що на старті абстрагує обидва варіанти зберігання за одним контрактом, і перемикати конкретну реалізацію через DI (`builder.Services.AddScoped<IMovieRepository, SqlMovieRepository>()` ↔ `FirestoreMovieRepository`).
6. Написати окремий **скрипт міграції даних** (одноразовий консольний застосунок/функція), що читає з джерела і записує в ціль, а не покладатись на ручне перенесення — особливо важливо, якщо перекладів багато мов і записів.

---

## 8. Короткий чекліст перед тим, як перекладати поле в БД

1. Чи це поле є мовним контентом предметної області (назва/опис/текст), а не технічним значенням? → якщо ні, **не перекладати**.
2. Чи значення поля дійсно відрізняється залежно від мови (а не просто по-різному форматується)? Дата/число/валюта → **форматування на UI**, не переклад у БД.
3. Чи це enum/статус/код? → зберігати одне канонічне значення, локалізувати на frontend.
4. Якщо так — створити для сутності окрему таблицю перекладів (SQL Server) або поле/підколекцію `translations` (Firebase).
5. Передбачити fallback на дефолтну мову для випадків, коли перекладу ще немає.
6. Не змішувати мовний контент з не мовними полями в одній "перекладній" структурі.
7. Якщо перехід між БД можливий — переконатись, що доступ до перекладів ізольований через інтерфейс репозиторію (розділ 8.4), а не "розмазаний" по контролерах чи Domain.
