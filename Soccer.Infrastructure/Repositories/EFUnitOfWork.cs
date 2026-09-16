using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;
using Soccer.Infrastructure.Persistence;

namespace Soccer.Infrastructure.Repositories
{
    /* патерн unit of work спрощує роботу з різними репозиторіями та гарантує,
     * що всі репозиторії використовують один і той же контекст даних. */
    public class EFUnitOfWork : IUnitOfWork
    {
        private SoccerContext db;
        private IRepository<Player>? playerRepository; // поле для кешування репозиторію гравців
        private IRepository<Team>? teamRepository;     // поле для кешування репозиторію команд

        public EFUnitOfWork(SoccerContext context) // конструктор, що приймає готовий контекст
        {
            db = context; // ініціалізація поля контекстом, переданим через di
        }

        public IRepository<Team> Teams // властивість для доступу до репозиторію команд
        {
            get
            {
                if (teamRepository == null) // перевірка, чи вже створено репозиторій
                    teamRepository = new TeamRepository(db); // створення репозиторію команд за потреби (lazy initialization)
                return teamRepository; // повернення єдиного екземпляра репозиторію
            }
        }

        public IRepository<Player> Players // властивість для доступу до репозиторію гравців
        {
            get
            {
                if (playerRepository == null) // перевірка, чи вже створено репозиторій
                    playerRepository = new PlayerRepository(db); // створення репозиторію гравців за потреби
                return playerRepository; // повернення єдиного екземпляра репозиторію
            }
        }

        public async Task Save() // асинхронний метод для збереження всіх змін
        {
            await db.SaveChangesAsync(); // виклик асинхронного збереження змін у контексті бази даних
        }
    }
}