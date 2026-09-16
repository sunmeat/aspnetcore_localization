# Локалізація бекенду на JSON (без .resx) для aspnetcore_localization

Мета — замінити стандартний механізм `.resx` на власну реалізацію `IStringLocalizer`,
яка читає переклади з `.json`-файлів і підключається програмно через DI, без будь-яких
візуальних resx-редакторів чи `.Designer.cs`.

---

## 0. Головний принцип

> Локалізувати потрібно не весь код, а ті дані, які є мовними або залежать від культури.

Для бекенду це означає:

**Перекладаємо:**
- повідомлення валідації (`[Required]`, `[StringLength]`, `[Range]` тощо);
- бізнес-помилки, які повертаються користувачу (`PlayerNotFound`, `TeamAlreadyExists`);
- HTTP error responses (`400`, `404`, `409` з текстовим message);
- нотифікації / повідомлення, що йдуть користувачу (email-шаблони, якщо вони формуються на бекенді).

**НЕ перекладаємо:**
- технічні логи, exception-и, stack trace, SQL-помилки — це для розробника, не для користувача;
- значення enum/status (`"active"`, `"pending"`) — вони мають лишатись машинно-читаними, переклад робить frontend;
- ідентифікатори, GUID, email, URL, коди валют, ISO-коди;
- весь текст UI (кнопки, заголовки, підказки) — це відповідальність `react.client` + `i18next`, бекенд туди не лізе;
- multilingual-контент сутностей (назви команд, описи матчів) — це окрема історія (таблиці `*Translation` в БД), не файли `.json` на диску. Про це — коротко в розділі 9.

---

## 1. Яку структуру папок створити

```
Soccer.sln
│
├── Soccer.Domain
│   └── (без змін — Domain нічого не знає про локалізацію)
│
├── Soccer.Application
│   └── Common
│       └── Localization
│           └── ILocalizationKeys.cs        # (опційно) константи ключів
│
├── Soccer.Infrastructure
│   └── Localization
│       ├── JsonStringLocalizer.cs          # реалізація IStringLocalizer
│       └── JsonStringLocalizerFactory.cs   # реалізація IStringLocalizerFactory
│
└── Soccer.WebAPI
    ├── Resources
    │   ├── SharedResource.uk-UA.json       # переклади
    │   ├── SharedResource.en-US.json
    │   └── SharedResource.fr-FR.json
    ├── SharedResource.cs                   # порожній маркер-клас
    ├── Controllers
    └── Program.cs
```

Пояснення розміщення:

- **`Soccer.Infrastructure/Localization`** — сюди йде технічна реалізація читання JSON
  (це "інфраструктурна" відповідальність, так само як робота з БД чи файловою системою).
  Це прямо відповідає структурі, яку рекомендує README проєкту (`Infrastructure/Localization`).
- **`Soccer.WebAPI/Resources`** — сюди йдуть самі файли перекладів, поряд із рівнем API,
  бо саме API — власник цих текстів (помилки й валідація — це контракт API, а не Domain чи Application).
- **`SharedResource.cs`** — порожній клас-маркер, потрібен лише для типізації
  `IStringLocalizer<SharedResource>` (стандартний патерн ASP.NET Core, працює і без resx).

---

## 2. Формат JSON-файлів перекладів

Один файл на культуру, іменування `{ResourceName}.{culture}.json`, вкладена структура (як у `i18next` на фронтенді — це навмисно, щоб ключі були людяними):

**`Soccer.WebAPI/Resources/SharedResource.uk-UA.json`:**
```json
{
  "Validation": {
    "NameRequired": "Поле «Ім'я» є обов'язковим.",
    "NameMaxLength": "Поле «Ім'я» не може перевищувати {0} символів.",
    "EmailInvalid": "Некоректний формат електронної пошти."
  },
  "Errors": {
    "PlayerNotFound": "Гравця не знайдено.",
    "TeamNotFound": "Команду не знайдено.",
    "TeamAlreadyExists": "Команда з такою назвою вже існує."
  },
  "Notifications": {
    "PlayerCreated": "Гравця успішно створено."
  }
}
```

**`Soccer.WebAPI/Resources/SharedResource.en-US.json`:**
```json
{
  "Validation": {
    "NameRequired": "The Name field is required.",
    "NameMaxLength": "The Name field must not exceed {0} characters.",
    "EmailInvalid": "Invalid email format."
  },
  "Errors": {
    "PlayerNotFound": "Player was not found.",
    "TeamNotFound": "Team was not found.",
    "TeamAlreadyExists": "A team with this name already exists."
  },
  "Notifications": {
    "PlayerCreated": "Player has been created successfully."
  }
}
```

Аналогічно `SharedResource.fr-FR.json`.

Ключ у коді використовується через крапкову нотацію: `localizer["Errors.PlayerNotFound"]`.

> Файли — обов'язково `Copy to Output Directory`, інакше в рантаймі їх не буде поруч зі скомпільованим API. Це налаштовується в `.csproj` (див. розділ 6).

---

## 3. Реалізація `JsonStringLocalizer`

`Soccer.Infrastructure/Localization/JsonStringLocalizer.cs`:

```csharp
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace Soccer.Infrastructure.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly string _resourcesPath;
    private readonly string _resourceName;
    private readonly string _defaultCulture;

    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> Cache = new();

    public JsonStringLocalizer(string resourcesPath, string resourceName, string defaultCulture = "en-US")
    {
        _resourcesPath = resourcesPath;
        _resourceName = resourceName;
        _defaultCulture = defaultCulture;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var result = this[name];
            if (result.ResourceNotFound)
                return result;

            return new LocalizedString(name, string.Format(result.Value, arguments), false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var resources = GetResourcesForCulture(culture);

        foreach (var pair in resources)
            yield return new LocalizedString(pair.Key, pair.Value, false);
    }

    private string? GetString(string key)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var resources = GetResourcesForCulture(culture);

        if (resources.TryGetValue(key, out var value))
            return value;

        // fallback на дефолтну культуру, якщо ключ відсутній у поточній
        if (culture != _defaultCulture)
        {
            var fallback = GetResourcesForCulture(_defaultCulture);
            if (fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;
        }

        return null;
    }

    private IReadOnlyDictionary<string, string> GetResourcesForCulture(string culture)
    {
        var cacheKey = $"{_resourceName}.{culture}";
        return Cache.GetOrAdd(cacheKey, _ => LoadResources(culture));
    }

    private IReadOnlyDictionary<string, string> LoadResources(string culture)
    {
        var result = new Dictionary<string, string>();
        var filePath = Path.Combine(_resourcesPath, $"{_resourceName}.{culture}.json");

        if (!File.Exists(filePath))
            return result;

        using var stream = File.OpenRead(filePath);
        using var document = JsonDocument.Parse(stream);

        Flatten(document.RootElement, prefix: string.Empty, result);
        return result;
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
                Flatten(property.Value, key, result);
            else
                result[key] = property.Value.GetString() ?? string.Empty;
        }
    }
}
```

Ключові моменти:
- переклади кешуються в `ConcurrentDictionary` — файл читається з диску один раз на культуру за час життя процесу;
- є fallback на дефолтну культуру (`en-US`), якщо ключ не знайдено в поточній — це рятує від "дірок" у перекладах;
- вкладені JSON-об'єкти автоматично "розгортаються" в ключі через крапку (`Errors.PlayerNotFound`).

---

## 4. Реалізація `JsonStringLocalizerFactory`

`Soccer.Infrastructure/Localization/JsonStringLocalizerFactory.cs`:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Soccer.Infrastructure.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly string _resourcesPath;

    public JsonStringLocalizerFactory(IOptions<LocalizationOptions> localizationOptions, IHostEnvironment env)
    {
        _resourcesPath = Path.Combine(env.ContentRootPath, localizationOptions.Value.ResourcesPath ?? "Resources");
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var resourceName = resourceSource.Name; // напр. "SharedResource"
        return new JsonStringLocalizer(_resourcesPath, resourceName);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        var resourceName = Path.GetFileNameWithoutExtension(baseName);
        return new JsonStringLocalizer(_resourcesPath, resourceName);
    }
}
```

Цей factory повністю замінює стандартний `ResourceManagerStringLocalizerFactory`, який ASP.NET Core використовує для `.resx`. Реєструючи цей клас у DI (крок 6), ви автоматично й безболісно вимикаєте resx у всьому застосунку — жодних `.resx`-файлів створювати не потрібно взагалі.

---

## 5. Маркер-клас `SharedResource`

`Soccer.WebAPI/SharedResource.cs`:

```csharp
namespace Soccer.WebAPI;

/// <summary>
/// Порожній клас-маркер, потрібен лише щоб типізувати
/// IStringLocalizer&lt;SharedResource&gt; і прив'язати його до файлів
/// SharedResource.{culture}.json у папці Resources.
/// </summary>
public class SharedResource
{
}
```

---

## 6. Реєстрація в `Program.cs`

```csharp
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Soccer.Infrastructure.Localization;

var builder = WebApplication.CreateBuilder(args);

// 1. Вказуємо папку з ресурсами (там лежать наші .json)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 2. Підміняємо стандартну resx-фабрику на власну json-фабрику
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

// 3. Локалізація DataAnnotations (валідація моделей)
builder.Services
    .AddControllers()
    .AddDataAnnotationsLocalization(options =>
    {
        // всі валідаційні атрибути шукатимуть ключі саме в SharedResource,
        // незалежно від того, до якої моделі належить атрибут
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SharedResource));
    });

// 4. Підтримувані культури
var supportedCultures = new[] { "uk-UA", "en-US", "fr-FR" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0]);
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);

    // визначення культури з заголовка Accept-Language — природний вибір для API
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var app = builder.Build();

// 5. Middleware має стояти якомога раніше в пайплайні
app.UseRequestLocalization();

app.MapControllers();
app.Run();
```

І обов'язково в `Soccer.WebAPI.csproj`, щоб json-файли копіювались у `bin/`:

```xml
<ItemGroup>
  <None Update="Resources\**\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## 7. Використання в контролері

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Soccer.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PlayersController(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var player = _repository.GetById(id); // приклад

        if (player is null)
            return NotFound(new { message = _localizer["Errors.PlayerNotFound"].Value });

        return Ok(player);
    }
}
```

Клієнт викликає:
```
GET /api/players/999
Accept-Language: uk-UA
```
і отримує:
```json
{ "message": "Гравця не знайдено." }
```

З `Accept-Language: fr-FR` — відповідно `"Joueur introuvable."` (за умови, що переклад є у `fr-FR.json`).

Використання з параметрами (форматування рядка):
```csharp
_localizer["Validation.NameMaxLength", 50].Value
// uk-UA → "Поле «Ім'я» не може перевищувати 50 символів."
```

---

## 8. Локалізація валідації моделей (DataAnnotations)

У `Soccer.Application/DTO` (або де у вас лежать моделі запитів):

```csharp
using System.ComponentModel.DataAnnotations;

public class CreatePlayerRequest
{
    [Required(ErrorMessage = "Validation.NameRequired")]
    [StringLength(50, ErrorMessage = "Validation.NameMaxLength")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Validation.EmailInvalid")]
    public string? Email { get; set; }
}
```

Важливо: у `ErrorMessage` вказується **ключ** з JSON (`"Validation.NameRequired"`), а не сам текст. Завдяки кроку 6 (`DataAnnotationLocalizerProvider`) ASP.NET Core автоматично прожене цей ключ через `IStringLocalizer<SharedResource>` і підставить переклад відповідно до `CurrentUICulture`, включно з параметром `{0}` для `StringLength` (максимальна довжина підставиться автоматично).

Це саме той механізм, про який пише README у розділі "8. Validation" — тільки замість `.resx` тепер `.json`.

---

## 9. Що НЕ входить у цю json-схему (і чому)

- **Multilingual-контент сутностей** (назва команди трьома мовами, опис матчу тощо) — це **не** файли на диску, а таблиці `EntityTranslation` в БД (`TeamTranslation`, `MovieTranslation` — патерн з розділу 12 README). JSON-локалізатор із цієї інструкції призначений тільки для *текстів API* (помилки, валідація, нотифікації), а не для бізнес-даних. Плутати ці два механізми не варто — за design README це принципово різні шари відповідальності.
- **Технічні логи й винятки** — залишаються англійською/машинним текстом, у `.json` не виносяться.
- **Enum/статус-значення** — лишаються як є (`"active"`), не перекладаються на бекенді.

---

## 10. Опційно: hot-reload перекладів без перезапуску API

Якщо хочеться редагувати `.json` і бачити зміни без рестарту сервісу — додайте `FileSystemWatcher`, який скидає кеш:

```csharp
private void WatchForChanges(string culture)
{
    var watcher = new FileSystemWatcher(_resourcesPath, $"{_resourceName}.{culture}.json");
    watcher.Changed += (_, _) => Cache.TryRemove($"{_resourceName}.{culture}", out _);
    watcher.EnableRaisingEvents = true;
}
```
(Викликати один раз при першому завантаженні культури в `LoadResources`, з захистом від дублювання watcher-ів.) Для production це не обов'язково — там простіше передеплоїти сервіс.

---

## 11. Чеклист впровадження

- [ ] Створено `Soccer.Infrastructure/Localization/JsonStringLocalizer.cs`
- [ ] Створено `Soccer.Infrastructure/Localization/JsonStringLocalizerFactory.cs`
- [ ] Створено `Soccer.WebAPI/SharedResource.cs` (маркер-клас)
- [ ] Створено `Soccer.WebAPI/Resources/SharedResource.uk-UA.json`, `.en-US.json`, `.fr-FR.json`
- [ ] У `.csproj` додано `CopyToOutputDirectory` для `Resources/**/*.json`
- [ ] У `Program.cs`: `AddLocalization`, `AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>`, `AddDataAnnotationsLocalization`, `RequestLocalizationOptions`, `UseRequestLocalization()`
- [ ] Контролери отримують `IStringLocalizer<SharedResource>` через DI, більше ніде немає хардкоду текстів
- [ ] Моделі запитів (`DTO`) використовують ключі (`ErrorMessage = "Validation.NameRequired"`), а не готовий текст
- [ ] Жодного `.resx`-файлу в проєкті немає
- [ ] Технічні логи й exception-и залишаються нелокалізованими
- [ ] Enum/status-значення в API-відповідях лишаються машинними, без перекладу

Після цього бекенд повністю відповідає принципу:
> API errors / validation → локалізація на бекенді, через ключ, а не через `if culture == "uk"`.
