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