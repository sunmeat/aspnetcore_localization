# Локалізація фронтенду react.client (i18next + react-i18next)

## Крок 1. Встановити бібліотеки

У папці `react.client`:

```bash
npm install i18next react-i18next i18next-browser-languagedetector i18next-http-backend
```

Призначення кожного пакета:
- **i18next** — базовий рушій перекладу (не залежить від React)
- **react-i18next** — біндинг для React (хук `useTranslation`, компонент `Trans`, реагує на зміну мови без ручного форс-рендеру)
- **i18next-browser-languagedetector** — автоматично визначає мову з URL, cookie, localStorage, `navigator.language`
- **i18next-http-backend** — необов'язково; дозволяє підвантажувати JSON-файли перекладів асинхронно (lazy-load), а не бандлити все в JS. Корисно для великого проєкту, для навчального прикладу можна обійтись без нього

## Крок 2. Створити папки

```
react.client/
└── src/
    ├── locales/
    │   ├── uk/
    │   │   └── translation.json
    │   ├── en/
    │   │   └── translation.json
    │   └── fr/
    │       └── translation.json
    ├── i18n/
    │   └── index.js          ← конфігурація i18next
    ├── components/
    │   └── LanguageSwitcher.jsx
    └── main.jsx               ← вже існує, сюди додаємо import './i18n'
```

Це саме та структура, яку README вказує у розділі 22 (`src/locales/{uk,en,fr}`).

## Крок 3. Формат перекладів — JSON з вкладеними ключами за доменом

`src/locales/uk/translation.json`:

```json
{
  "common": {
    "save": "Зберегти",
    "cancel": "Скасувати",
    "delete": "Видалити",
    "add": "Додати"
  },
  "player": {
    "title": "Гравці",
    "add": "Додати гравця",
    "notFound": "Гравця не знайдено"
  },
  "team": {
    "title": "Команди",
    "description": "Опис команди"
  },
  "validation": {
    "required": "Поле обов'язкове",
    "invalidEmail": "Некоректний email"
  },
  "status": {
    "active": "Активний",
    "inactive": "Неактивний"
  }
}
```

Аналогічно для `en/translation.json` (`"save": "Save"` тощо) і `fr/translation.json`.

Чому саме так:
- Namespace-подібна вкладеність (`player.*`, `common.*`) відповідає принципу — "локалізувати те, що є UI-текстом", і дозволяє групувати переклади по фічах, а не мати один плоский список на 300 рядків.
- `status.active` — це переклад **відображення** enum-значення, а не саме значення в БД/API (див. Крок 11, "що НЕ перекладати").

## Крок 4. Конфігурація i18next

`src/i18n/index.js`:

```javascript
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import uk from '../locales/uk/translation.json';
import en from '../locales/en/translation.json';
import fr from '../locales/fr/translation.json';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      uk: { translation: uk },
      en: { translation: en },
      fr: { translation: fr },
    },
    fallbackLng: 'en',
    supportedLngs: ['uk', 'en', 'fr'],
    interpolation: {
      escapeValue: false, // React вже екранує XSS
    },
    detection: {
      // порядок пошуку мови, узгоджений з README (розділ 19)
      order: ['querystring', 'localStorage', 'navigator'],
      lookupQuerystring: 'lng',
      caches: ['localStorage'],
    },
  });

export default i18n;
```

## Крок 5. Підключити в `main.jsx`

```javascript
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './i18n'; // просто імпорт — виконує ініціалізацію побічним ефектом

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

## Крок 6. Використання в компонентах

Замість:

```jsx
<button>Add player</button>
```

Пишемо:

```jsx
import { useTranslation } from 'react-i18next';

function PlayerList() {
  const { t } = useTranslation();
  return (
    <div>
      <h1>{t('player.title')}</h1>
      <button>{t('player.add')}</button>
    </div>
  );
}
```

## Крок 7. Перемикач мови

`src/components/LanguageSwitcher.jsx`:

```jsx
import { useTranslation } from 'react-i18next';

const LANGS = [
  { code: 'uk', label: 'Українська' },
  { code: 'en', label: 'English' },
  { code: 'fr', label: 'Français' },
];

export default function LanguageSwitcher() {
  const { i18n } = useTranslation();

  const changeLanguage = (code) => {
    i18n.changeLanguage(code);
    document.documentElement.lang = code; // синхронізуємо <html lang="">
  };

  return (
    <select value={i18n.language} onChange={(e) => changeLanguage(e.target.value)}>
      {LANGS.map((l) => (
        <option key={l.code} value={l.code}>{l.label}</option>
      ))}
    </select>
  );
}
```

## Крок 8. `index.html`

У `react.client/index.html` є статичний `<html lang="en">` і `<title>`. Це теж frontend-локалізація:

```html
<!doctype html>
<html lang="en">  <!-- буде оновлюватись через JS: document.documentElement.lang -->
  <head>
    <meta charset="UTF-8" />
    <title>react.client</title>  <!-- fallback, який браузер показує до того, як React встигне змонтуватись і виконати useEffect, зміна программно робиться -->
  </head>
  ...
```

`lang` атрибут важливий для accessibility (screen readers) і SEO — оновлюється при зміні мови (див. код `LanguageSwitcher` вище).

## Крок 9. Форматування дат/чисел — без бібліотеки, через `Intl`

Не тримайте формат дат/валют у JSON-перекладах — це не текст, а форматування. Використовуйте нативний `Intl`, узгоджений з поточною мовою i18next:

```javascript
import { useTranslation } from 'react-i18next';

function Price({ amount }) {
  const { i18n } = useTranslation();
  const formatted = new Intl.NumberFormat(i18n.language, {
    style: 'currency',
    currency: 'UAH',
  }).format(amount);
  return <span>{formatted}</span>;
}
```

## Крок 10. Синхронізація з бекендом (Accept-Language)

Якщо фронтенд робить запити до Soccer.WebAPI, додайте interceptor (наприклад для `fetch`/`axios`), щоб бекенд теж локалізував validation-помилки:

```javascript
fetch('/api/players', {
  headers: {
    'Accept-Language': i18n.language, // напр. 'uk-UA'
  },
});
```

## Крок 11. Що перекладати, а що — ні (стисло за README)

**Перекладати (у `locales/*/translation.json`):**
- меню, кнопки, заголовки, лейбли, плейсхолдери;
- повідомлення, модальні вікна, тексти помилок валідації на клієнті;
- accessibility-тексти (aria-label, alt);
- **відображення** enum/статусів (`status.active` → "Активний"), але не саме значення.

**НЕ перекладати:**
- `Id`, GUID, email, URL, ISO-коди, ключі БД;
- значення статусів/enum, які йдуть в API-запитах (`status: "active"` завжди залишається англійською константою — переклад тільки на рівні відображення);
- дати/числа/валюти як текст у JSON — це форматування через `Intl`, а не переклад;
- технічні console.log/помилки — вони не для кінцевого користувача.
