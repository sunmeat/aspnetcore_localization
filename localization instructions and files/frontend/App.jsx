import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next'; // імпорт хуку useTranslation
import LanguageSwitcher from './LanguageSwitcher'; // перемикач мови як окремий компонент
import './App.css';

const API_BASE_URL = '/api';

export default function App() {
    // i18n містить поточну мову, t — функція для отримання текстових ресурсів з JSON
    const { t, i18n } = useTranslation();

    const [activeTab, setActiveTab] = useState('players');
    const [players, setPlayers] = useState([]);
    const [teams, setTeams] = useState([]);
    const [teamOptions, setTeamOptions] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [playerForm, setPlayerForm] = useState({ id: 0, name: '', age: '', position: '', teamId: '' });
    const [teamForm, setTeamForm] = useState({ id: 0, name: '', coach: '' });
    const [isEditing, setIsEditing] = useState(false);
    const [confirmDialog, setConfirmDialog] = useState(null);

    // допоміжна функція для запитів із передачею поточної мови (Accept-Language) на бекенд
    const apiFetch = useCallback((url, options = {}) => {
        return fetch(url, {
            ...options,
            headers: {
                // синхронізація з бекендом (Accept-Language): передаємо поточну мову інтерфейсу для локалізації помилок/валідацій на сервері
                'Accept-Language': i18n.language,
                ...options.headers,
            },
        });
    }, [i18n.language]);

    // форматування дат/чисел — без бібліотеки, через Intl: форматування чисел за допомогою нативного Intl.NumberFormat під поточну мову
    const formatNumber = (value) => {
        if (value === null || value === undefined || isNaN(value)) return '';
        return new Intl.NumberFormat(i18n.language).format(value);
    };

    // форматування дати та часу за допомогою Intl.DateTimeFormat під поточну мову
    const formatDateTime = (dateValue) => {
        if (!dateValue) return '';
        return new Intl.DateTimeFormat(i18n.language, {
            dateStyle: 'medium',
            timeStyle: 'short',
        }).format(new Date(dateValue));
    };

    // index.html: динамічне оновлення document.title при зміні активної вкладки або мови (t) програмно замість статичного значення з index.html
    useEffect(() => {
        const pageTitle = t(activeTab === 'players' ? 'nav.players' : 'nav.teams');
        const appTitle = t('header.title');
        document.title = `${pageTitle} | ${appTitle}`;
    }, [activeTab, t]);

    const resetForm = useCallback(() => {
        setPlayerForm({ id: 0, name: '', age: '', position: '', teamId: '' });
        setTeamForm({ id: 0, name: '', coach: '' });
        setIsEditing(false);
    }, []);

    const refreshTeamOptions = useCallback(async () => {
        try {
            const response = await apiFetch(`${API_BASE_URL}/teams`);
            if (!response.ok) return;
            const data = await response.json();
            setTeamOptions(data);
        } catch {
            setTeamOptions([]);
        }
    }, [apiFetch]);

    const fetchData = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const endpoint = activeTab === 'players' ? 'players' : 'teams';
            const response = await apiFetch(`${API_BASE_URL}/${endpoint}`);
            if (!response.ok) throw new Error(`${t('errors.load')}: ${response.statusText}`);
            const data = await response.json();
            if (activeTab === 'players') setPlayers(data);
            else setTeams(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }, [activeTab, apiFetch, t]);

    useEffect(() => {
        let ignore = false;
        const load = async () => {
            resetForm();
            setLoading(true);
            setError(null);
            try {
                const endpoint = activeTab === 'players' ? 'players' : 'teams';
                const response = await apiFetch(`${API_BASE_URL}/${endpoint}`);
                if (!response.ok) throw new Error(`${t('errors.load')}: ${response.statusText}`);
                const data = await response.json();

                if (activeTab === 'players') {
                    const teamsResponse = await apiFetch(`${API_BASE_URL}/teams`);
                    const teamsData = teamsResponse.ok ? await teamsResponse.json() : [];
                    if (!ignore) {
                        setPlayers(data);
                        setTeamOptions(teamsData);
                    }
                } else {
                    if (!ignore) {
                        setTeams(data);
                        setTeamOptions(data);
                    }
                }
            } catch (err) {
                if (!ignore) setError(err.message);
            } finally {
                if (!ignore) setLoading(false);
            }
        };
        load();
        return () => { ignore = true; };
    }, [activeTab, apiFetch, resetForm, t]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        const isPlayer = activeTab === 'players';
        const endpoint = isPlayer ? 'players' : 'teams';
        const formData = isPlayer
            ? { ...playerForm, age: Number(playerForm.age), teamId: playerForm.teamId ? Number(playerForm.teamId) : null }
            : teamForm;
        const url = isEditing ? `${API_BASE_URL}/${endpoint}/${formData.id}` : `${API_BASE_URL}/${endpoint}`;
        const method = isEditing ? 'PUT' : 'POST';

        try {
            const response = await apiFetch(url, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData),
            });
            if (!response.ok) throw new Error(t('errors.save'));
            resetForm();
            fetchData();
            if (!isPlayer) refreshTeamOptions();
        } catch (err) {
            setError(err.message);
        }
    };

    const handleEdit = (item) => {
        setIsEditing(true);
        if (activeTab === 'players') {
            setPlayerForm({
                id: item.id,
                name: item.name || '',
                age: item.age ?? '',
                position: item.position || '',
                teamId: item.teamId ?? '',
            });
        } else {
            setTeamForm({
                id: item.id,
                name: item.name || '',
                coach: item.coach || '',
            });
        }
    };

    const requestDelete = (id, name) => {
        setConfirmDialog({ id, name });
    };

    const cancelDelete = () => {
        setConfirmDialog(null);
    };

    const confirmDelete = async () => {
        if (!confirmDialog) return;
        const id = confirmDialog.id;
        setConfirmDialog(null);
        const endpoint = activeTab === 'players' ? 'players' : 'teams';
        try {
            const response = await apiFetch(`${API_BASE_URL}/${endpoint}/${id}`, { method: 'DELETE' });
            if (!response.ok) throw new Error(t('errors.delete'));
            fetchData();
            if (activeTab === 'teams') refreshTeamOptions();
        } catch (err) {
            setError(err.message);
        }
    };

    return (
        <div className="app">
            <header className="header">
                <div className="header-inner">
                    <div className="brand">
                        <span className="brand-icon">⚽</span>
                        <div className="brand-text">
                            {/* використання в компонентах: заміна всіх захардкожених текстів на виклик t(...) з файлів JSON */}
                            <span className="brand-title">{t('header.title')}</span>
                            <span className="brand-sub">{t('header.subtitle')}</span>
                        </div>
                    </div>

                    <div className="header-controls">
                        <nav className="nav">
                            <button
                                className={`nav-btn ${activeTab === 'players' ? 'active' : ''}`}
                                onClick={() => setActiveTab('players')}
                            >
                                {t('nav.players')}
                            </button>
                            <button
                                className={`nav-btn ${activeTab === 'teams' ? 'active' : ''}`}
                                onClick={() => setActiveTab('teams')}
                            >
                                {t('nav.teams')}
                            </button>
                        </nav>

                        <div className="lang-selector">
                            {/* компонент <LanguageSwitcher /> */}
                            <LanguageSwitcher />
                        </div>
                    </div>
                </div>
            </header>

            <main className="main">
                {error && <div className="alert">{error}</div>}

                <div className="layout">
                    <section className="panel form-panel">
                        <div className="panel-head">
                            <h2>
                                {/* параметризований переклад із вкладеним викликом t() */}
                                {t('form.' + (isEditing ? 'editTitle' : 'addTitle'), {
                                    entity: t(activeTab === 'players' ? 'player.entity' : 'team.entity')
                                })}
                            </h2>
                        </div>
                        <form onSubmit={handleSubmit} className="form">
                            {activeTab === 'players' ? (
                                <>
                                    <div className="field">
                                        <label>{t('player.fields.name')}</label>
                                        <input
                                            type="text"
                                            required
                                            value={playerForm.name}
                                            onChange={(e) => setPlayerForm({ ...playerForm, name: e.target.value })}
                                            placeholder={t('player.placeholders.name')}
                                        />
                                    </div>
                                    <div className="field">
                                        <label>{t('player.fields.age')}</label>
                                        <input
                                            type="number"
                                            required
                                            min="15"
                                            max="50"
                                            value={playerForm.age}
                                            onChange={(e) => setPlayerForm({ ...playerForm, age: e.target.value })}
                                            placeholder={t('player.placeholders.age')}
                                        />
                                    </div>
                                    <div className="field">
                                        <label>{t('player.fields.position')}</label>
                                        <input
                                            type="text"
                                            required
                                            value={playerForm.position}
                                            onChange={(e) => setPlayerForm({ ...playerForm, position: e.target.value })}
                                            placeholder={t('player.placeholders.position')}
                                        />
                                    </div>
                                    <div className="field">
                                        <label>{t('player.fields.team')}</label>
                                        <select
                                            value={playerForm.teamId}
                                            onChange={(e) => setPlayerForm({ ...playerForm, teamId: e.target.value })}
                                        >
                                            <option value="">{t('player.fields.noTeam')}</option>
                                            {teamOptions.map((team) => (
                                                <option key={team.id} value={team.id}>
                                                    {team.name}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                </>
                            ) : (
                                <>
                                    <div className="field">
                                        <label>{t('team.fields.name')}</label>
                                        <input
                                            type="text"
                                            required
                                            value={teamForm.name}
                                            onChange={(e) => setTeamForm({ ...teamForm, name: e.target.value })}
                                            placeholder={t('team.placeholders.name')}
                                        />
                                    </div>
                                    <div className="field">
                                        <label>{t('team.fields.coach')}</label>
                                        <input
                                            type="text"
                                            required
                                            value={teamForm.coach}
                                            onChange={(e) => setTeamForm({ ...teamForm, coach: e.target.value })}
                                            placeholder={t('team.placeholders.coach')}
                                        />
                                    </div>
                                </>
                            )}

                            <div className="form-actions">
                                <button type="submit" className="btn btn-primary">
                                    {t(isEditing ? 'common.save' : 'common.create')}
                                </button>
                                {isEditing && (
                                    <button type="button" className="btn btn-ghost" onClick={resetForm}>
                                        {t('common.cancel')}
                                    </button>
                                )}
                            </div>
                        </form>
                    </section>

                    <section className="panel list-panel">
                        <div className="panel-head">
                            <h2>
                                {t('player.fields.nameShort')} {t(activeTab === 'players' ? 'player.titleGenitive' : 'team.titleGenitive')}
                            </h2>
                            <span className="count">
                                {formatNumber((activeTab === 'players' ? players : teams).length)} {t('common.records')}
                            </span>
                        </div>

                        {loading ? (
                            <div className="loader">
                                <div className="spinner"></div>
                                <span>{t('common.loading')}</span>
                            </div>
                        ) : (
                            <div className="table-wrap">
                                <table>
                                    <thead>
                                        <tr>
                                            <th>ID</th>
                                            <th>{activeTab === 'players' ? t('player.fields.nameShort') : t('team.fields.nameShort')}</th>
                                            {activeTab === 'players' ? (
                                                <>
                                                    <th>{t('player.fields.age')}</th>
                                                    <th>{t('player.fields.position')}</th>
                                                    <th>{t('player.fields.team')}</th>
                                                </>
                                            ) : (
                                                <th>{t('team.fields.coach')}</th>
                                            )}
                                            <th>{t('common.actions')}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {(activeTab === 'players' ? players : teams).length === 0 ? (
                                            <tr>
                                                <td colSpan={activeTab === 'players' ? 6 : 4} className="empty">
                                                    {t('common.noData')}
                                                </td>
                                            </tr>
                                        ) : (
                                            (activeTab === 'players' ? players : teams).map((item) => (
                                                <tr key={item.id}>
                                                    <td className="id">#{formatNumber(item.id)}</td>
                                                    <td className="name">{item.name}</td>
                                                    {activeTab === 'players' ? (
                                                        <>
                                                            <td>{formatNumber(item.age)}</td>
                                                            <td>
                                                                <span className="badge">{item.position}</span>
                                                            </td>
                                                            <td className="team-name">{item.team || '—'}</td>
                                                        </>
                                                    ) : (
                                                        <td className="coach">{item.coach}</td>
                                                    )}
                                                    <td className="actions">
                                                        <button className="icon-btn edit" onClick={() => handleEdit(item)} title={t('common.edit')}>
                                                            ✎
                                                        </button>
                                                        <button className="icon-btn delete" onClick={() => requestDelete(item.id, item.name)} title={t('common.delete')}>
                                                            ✕
                                                        </button>
                                                    </td>
                                                </tr>
                                            ))
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </section>
                </div>
            </main>

            <footer className="footer">
                <div className="footer-inner">
                    <div className="footer-brand">
                        <span className="brand-icon">⚽</span>
                        <span>{t('header.title')}</span>
                    </div>
                    <p>{t('footer.demoText')}</p>
                    {/* форматування дати та часу за допомогою Intl.DateTimeFormat */}
                    <p className="footer-copy">
                        © {formatNumber(new Date().getFullYear())} | {t('footer.updated')}: {formatDateTime(new Date())}
                    </p>
                </div>
            </footer>

            {confirmDialog && (
                <div className="modal-overlay" onClick={cancelDelete}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-icon">⚠</div>
                        <h3 className="modal-title">{t('modal.deleteTitle')}</h3>
                        <p className="modal-text">
                            {t('modal.deleteConfirm', {
                                entity: t(activeTab === 'players' ? 'player.entityAccusative' : 'team.entityAccusative'),
                                name: confirmDialog.name
                            })}
                        </p>
                        <div className="modal-actions">
                            <button className="btn btn-ghost" onClick={cancelDelete}>
                                {t('common.cancel')}
                            </button>
                            <button className="btn btn-danger" onClick={confirmDelete}>
                                {t('common.delete')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}